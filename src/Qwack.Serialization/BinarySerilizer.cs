using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;

namespace Qwack.Serialization
{
    public class BinarySerilizer
    {
        private List<object> _objectsCrawled = new List<object>();
        private Dictionary<Type, int> _typeNumbers = new Dictionary<Type, int>();
        private int _number = 1;

        private const byte ArrayType_ULong = 1;

        public void PrepareObjectGraph(object objectGraph)
        {
            if (objectGraph == null) return;
            if (!objectGraph.GetType().Namespace.StartsWith("Qwack")) return;

            var privateFields = objectGraph.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in privateFields)
            {
                if (f.FieldType.IsEnum) continue;
                var value = f.GetValue(objectGraph);
                AddField(value);
            }
            if (!_typeNumbers.ContainsKey(objectGraph.GetType()))
            {
                _typeNumbers.Add(objectGraph.GetType(), _number);
                _number++;
            }
            _objectsCrawled.Add(objectGraph);
        }

        public void SerializeObjectGraph(PipeWriter pipeWriter)
        {
            var span = pipeWriter.GetSpan();
            var spanLength = span.Length;
            // Serialize all objects
            foreach (var o in _objectsCrawled)
            {
                var type = o.GetType();
                var number = _typeNumbers[type];
                span.Write((ushort)number);
                //Now save all of the fields
                var privateFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var pField in privateFields)
                {
                    pipeWriter.Advance(spanLength - span.Length);
                    span = pipeWriter.GetSpan(2048);
                    spanLength = span.Length;
                    if (pField.FieldType.IsEnum)
                    {
                        //Need to write the enum straight to the stream
                        var enumType = pField.GetValue(o);
                        switch (enumType)
                        {
                            case byte b:
                                span.Write(b);
                                break;
                            case short s:
                                span.Write(s);
                                break;
                            case int i:
                                span.Write(i);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                    else if (pField.FieldType.Namespace.StartsWith("Qwack"))
                    {
                        //One of our objects so we need to look up the object number in the list and replace with that
                        var qo = pField.GetValue(o);
                        if (qo == null)
                        {
                            span.Write((ushort)0);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        if(pField.FieldType.IsGenericType)
                        {
                            //Is probably a collection
                            throw new NotImplementedException();
                        }
                        else if(pField.FieldType.IsArray)
                        {
                            //Deal with arrays
                            var array = pField.GetValue(o);
                            switch(array)
                            {
                                case ulong[] ul:
                                    span.Write(ArrayType_ULong);
                                    span.Write(ul.Length);
                                    for(var i = 0; i < ul.Length;i++)
                                    {
                                        if(span.Length < sizeof(ulong))
                                        {
                                            pipeWriter.Advance(spanLength - span.Length);
                                            span = pipeWriter.GetSpan(512);
                                            spanLength = span.Length;
                                        }
                                        span.Write(ul[i]);
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            //Look up our common types
                            //If we don't find it there then throw
                            var simpleType = pField.GetValue(o);
                            switch(simpleType)
                            {
                                case uint ui:
                                    span.Write(ui);
                                    break;
                                case ulong ui64:
                                    span.Write(ui64);
                                    break;
                                case long l:
                                    span.Write(l);
                                    break;
                                case int i:
                                    span.Write(i);
                                    break;
                                case bool b:
                                    span.Write((byte)(b ? 1 : 0));
                                    break;
                                case DateTime d:
                                    span.Write(d.Ticks);
                                    break;
                                case float f:
                                    span.Write(f);
                                    break;
                                case double dd:
                                    span.Write(dd);
                                    break;
                                case string s:
                                    throw new NotImplementedException();
                                case null:
                                    if(pField.FieldType == typeof(string))
                                    {
                                        span.Write((uint)0);
                                    }
                                    else
                                    {
                                        throw new NotImplementedException();
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                    }
                }
            }
        }

        private void AddField(object value)
        {
            if (value == null) return;
            var type = value.GetType();
            if (type.IsEnum) return;
            if (type.Namespace.StartsWith("Qwack"))
            {
                if (!_objectsCrawled.Contains(value))
                {
                    //Its one of ours so we need to loop around this
                    PrepareObjectGraph(value);
                }
            }
            else if (typeof(string) != type && typeof(IEnumerable).IsAssignableFrom(type))
            {
                //Collection we need to think about how to deal with that
                foreach (var i in (IEnumerable)value)
                {
                    PrepareObjectGraph(i);
                }
            }
            else
            {
                //Just a system object we should be able to serilize this so nothing else to do
            }
        }
    }
}
