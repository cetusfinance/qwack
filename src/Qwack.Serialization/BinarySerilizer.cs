using System;
using System.Collections;
using System.Collections.Generic;
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

        public void SerializeObjectGraph()
        {

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
