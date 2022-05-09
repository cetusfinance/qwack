using System;
using System.Collections.Generic;

namespace Qwack.Serialization
{
    public class BinaryDeserializer
    {

        public object DeserializeObjectGraph(Span<byte> buffer)
        {
            var strategyCount = buffer.ReadInt();
            var strategies = new Dictionary<int, DeserializationStrategy>(strategyCount);
            for (var i = 0; i < strategyCount; i++)
            {
                var id = buffer.ReadInt();
                var s = buffer.ReadString();
                var type = Type.GetType(s);
                strategies.Add(id, new DeserializationStrategy(type));
            }

            var context = new DeserializationContext();
            object lastObject = null;
            while (buffer.Length > 0)
            {
                var strategy = strategies[buffer.ReadInt()];
                lastObject = strategy.Deserialize(ref buffer, context);
                context.AddObjectToContext(lastObject);
            }
            return lastObject;
        }
    }
}
