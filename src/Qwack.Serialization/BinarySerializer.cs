using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Reflection;

namespace Qwack.Serialization
{
    public class BinarySerializer
    {
        private List<object> _objectsCrawled = new List<object>();
        private Dictionary<Type, SerializationStrategy> _serializationStrategies = new Dictionary<Type, SerializationStrategy>();

        private const byte ArrayType_ULong = 1;

        public void PrepareObjectGraph(object objectGraph)
        {
            if (objectGraph == null) return;
            if (!objectGraph.GetType().Namespace.StartsWith("Qwack")) return;
            var objType = objectGraph.GetType();

            if (!_serializationStrategies.TryGetValue(objType, out var strat))
            {
                _serializationStrategies.Add(objType, new SerializationStrategy(objType));
            }

            var privateFields = objectGraph.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in privateFields)
            {
                if (f.FieldType.IsEnum) continue;
                var value = f.GetValue(objectGraph);
                AddField(value);
            }
            _objectsCrawled.Add(objectGraph);
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
                foreach (var i in (IEnumerable)value)
                {
                    PrepareObjectGraph(i);
                }
            }
        }

        public void SerializeObjectGraph(PipeWriter pipeWriter)
        {
            var buffer =(Span<byte>) new byte[1024 * 1024 * 50];
            foreach(var o in _objectsCrawled)
            {
                _serializationStrategies[o.GetType()].Serialize(o, ref buffer);
            }
            //var span = pipeWriter.GetSpan();
            //var spanLength = span.Length;
            //// Serialize all objects
            //foreach (var o in _objectsCrawled)
            //{
            //    var type = o.GetType();
            //    var number = _typeNumbers[type];
            //    span.WriteUShort((ushort)number);
            //    //Now save all of the fields
            //    var privateFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            //    foreach (var pField in privateFields)
            //    {
            //        pipeWriter.Advance(spanLength - span.Length);
            //        span = pipeWriter.GetSpan(2048);
            //        spanLength = span.Length;
            //        if (pField.FieldType.IsEnum)
            //        {
            //            //Need to write the enum straight to the stream
            //            var enumType = pField.GetValue(o);
            //            switch (enumType)
            //            {
            //                case byte b:
            //                    span.WriteByte(b);
            //                    break;
            //                case short s:
            //                    span.WriteShort(s);
            //                    break;
            //                case int i:
            //                    span.WriteInt(i);
            //                    break;
            //                default:
            //                    throw new NotSupportedException();
            //            }
            //        }
            //        else if (pField.FieldType.Namespace.StartsWith("Qwack"))
            //        {
            //            //One of our objects so we need to look up the object number in the list and replace with that
            //            var qo = pField.GetValue(o);
            //            if (qo == null)
            //            {
            //                span.WriteUShort(0);
            //            }
            //            else
            //            {
            //                throw new NotImplementedException();
            //            }
            //        }
            //        else
            //        {
            //            if (pField.FieldType.IsGenericType)
            //            {
            //                if (pField.FieldType == typeof(HashSet<>))
            //                {
            //                    throw new NotImplementedException();
            //                }
            //                //Is probably a collection
            //                throw new NotImplementedException();
            //            }
            //            else if (pField.FieldType.IsArray)
            //            {
            //                //Deal with arrays
            //                var array = pField.GetValue(o);
            //                switch (array)
            //                {
            //                    case ulong[] ul:
            //                        span.WriteByte(ArrayType_ULong);
            //                        span.WriteInt(ul.Length);
            //                        for (var i = 0; i < ul.Length; i++)
            //                        {
            //                            if (span.Length < sizeof(ulong))
            //                            {
            //                                pipeWriter.Advance(spanLength - span.Length);
            //                                span = pipeWriter.GetSpan(512);
            //                                spanLength = span.Length;
            //                            }
            //                            span.WriteULong(ul[i]);
            //                        }
            //                        break;
            //                    default:
            //                        throw new NotImplementedException();
            //                }
            //            }
            //            else
            //            {
            //                //Look up our common types
            //                //If we don't find it there then throw
            //                var simpleType = pField.GetValue(o);
            //                switch (simpleType)
            //                {
            //                    case bool b:
            //                        span.WriteByte((byte)(b ? 1 : 0));
            //                        break;
            //                    case DateTime d:
            //                        span.WriteLong(d.Ticks);
            //                        break;
            //                    case string s:
            //                        throw new NotImplementedException();
            //                    case null:
            //                        if (pField.FieldType == typeof(string))
            //                        {
            //                            span.WriteUInt((uint)0);
            //                        }
            //                        else
            //                        {
            //                            throw new NotImplementedException();
            //                        }
            //                        break;
            //                    default:
            //                        throw new NotImplementedException();
            //                }
            //            }
            //        }
            //    }
            //}
        }

        
    }
}
