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
        private Dictionary<Type, (int number, SerializationStrategy strategy)> _serializationStrategies = new Dictionary<Type, (int number, SerializationStrategy strategy)>();
        int typeNumber = 0;
        private const byte ArrayType_ULong = 1;

        public void PrepareObjectGraph(object objectGraph)
        {
            if (objectGraph == null) return;
            if (!objectGraph.GetType().Namespace.StartsWith("Qwack")) return;
            if (objectGraph.GetType().IsValueType) return;
            var objType = objectGraph.GetType();

            if (!_serializationStrategies.TryGetValue(objType, out var strat))
            {
                _serializationStrategies.Add(objType, (typeNumber++, new SerializationStrategy(objType)));
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

        public Span<byte> SerializeObjectGraph()
        {
            var context = new DeserializationContext();
            var buffer = (Span<byte>)new byte[1024 * 1024 * 50];
            var originalSpan = buffer;

            buffer.WriteInt(_serializationStrategies.Count);
            foreach (var s in _serializationStrategies)
            {
                buffer.WriteInt(s.Value.number);
                buffer.WriteString(s.Value.strategy.FullName);
            }

            foreach (var o in _objectsCrawled)
            {
                var (number, strategy) = _serializationStrategies[o.GetType()];
                buffer.WriteInt(number);
                strategy.Serialize(o, ref buffer, context);
            }

            return originalSpan.Slice(0, originalSpan.Length - buffer.Length);
        }
    }
}
