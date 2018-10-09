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

        public void PrepareObjectGraph(object objectGraph)
        {
            if (objectGraph == null) return;
            if (!objectGraph.GetType().Namespace.StartsWith("Qwack")) return;

            var privateFields = objectGraph.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach(var f in privateFields)
            {
                if (f.FieldType.IsEnum) continue;
                var value = f.GetValue(objectGraph);
                AddField(value);
            }
            if(!_typeNumbers.ContainsKey(objectGraph.GetType()))
            {
                _typeNumbers.Add(objectGraph.GetType(), _number);
                _number++;
            }
            _objectsCrawled.Add(objectGraph);
        }

        public void SerializeObjectGraph(PipeWriter pipeWriter)
        {
            var span = pipeWriter.GetSpan();
            // Serialize all objects
            foreach (var o in _objectsCrawled)
            {
                var type = o.GetType();
                var number = _typeNumbers[type];
                span.Write((ushort)number);
                //Now save all of the fields
                var privateFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach(var pField in privateFields)
                {
                    if (pField.FieldType.IsEnum)
                    {
                        //Need to write the enum straight to the stream
                        var enumType = pField.FieldType.GetEnumUnderlyingType();
                        if(enumType == typeof(int))
                        {
                            span.Write((int)pField.GetValue(o));
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else if (pField.FieldType.Namespace.StartsWith("Qwack"))
                    {
                        //One of our objects so we need to look up the object number in the list and replace with that
                        throw new NotImplementedException();
                    }
                    else
                    {
                        //Look up our common types
                        //If we don't find it there then throw
                        throw new NotImplementedException();
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
