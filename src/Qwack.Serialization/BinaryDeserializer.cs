using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Serialization
{
    public class BinaryDeserializer
    {

        public object DeserializeObjectGraph(Span<byte> buffer)
        {
            var strategyCount = buffer.ReadInt();
            var strategies = new Dictionary<int, DeserializationStrategy>(strategyCount);
            for(var i = 0; i < strategyCount;i++)
            {
                var id = buffer.ReadInt();
                var s = buffer.ReadString();
                var type = Type.GetType(s);
                strategies.Add(id, new DeserializationStrategy(type));
            }

            var context = new DeserializationContext();

            while(buffer.Length > 0)
            {
                var strategy = strategies[buffer.ReadInt()];
                context.AddObjectToContext(strategy.Deserialize(ref buffer, context));
            }
            throw new NotImplementedException();
        }
    }
}
