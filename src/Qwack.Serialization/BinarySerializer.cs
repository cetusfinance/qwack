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
            var context = new DeserializationContext();
            var buffer =(Span<byte>) new byte[1024 * 1024 * 50];
            foreach(var o in _objectsCrawled)
            {
                _serializationStrategies[o.GetType()].Serialize(o, ref buffer, context);
            }
        }
    }
}
