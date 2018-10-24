using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class EnumSerializationFacts
    {
        [Fact]
        public void CanSerializeEnums()
        {
            var ser = new BinarySerializer();
            var deser = new BinaryDeserializer();

            var obj = Create();
            ser.PrepareObjectGraph(obj);
            var span = ser.SerializeObjectGraph();

            var newObj = (EnumObject)deser.DeserializeObjectGraph(span);

            Assert.Equal(obj.IntEnum1, newObj.IntEnum1);
            Assert.Equal(obj.IntEnum2, newObj.IntEnum2);
            Assert.Equal(obj.ShortEnum1, newObj.ShortEnum1);
            Assert.Equal(obj.ShortEnum2, newObj.ShortEnum2);
            Assert.Equal(obj.LongEnum1, newObj.LongEnum1);
            Assert.Equal(obj.LongEnum2, newObj.LongEnum2);
        }

        public static EnumObject Create() => new EnumObject()
        {
            IntEnum1 = IntEnum.Test1,
            IntEnum2 = IntEnum.Test2,
            LongEnum1 = LongEnum.Test1,
            LongEnum2 = LongEnum.Test2,
            ShortEnum1 = ShortEnum.Test1,
            ShortEnum2 = ShortEnum.Test2,
        };

        public class EnumObject
        {
            public IntEnum IntEnum1 { get; set; }
            public IntEnum IntEnum2 { get; set; }
            public LongEnum LongEnum1 { get; set; }
            public LongEnum LongEnum2 { get; set; }
            public ShortEnum ShortEnum1 { get; set; }
            public ShortEnum ShortEnum2 { get; set; }
        }

        public enum IntEnum : int
        {
            Test1,
            Test2
        }

        public enum LongEnum : long
        {
            Test1,
            Test2,
        }

        public enum ShortEnum : short
        {
            Test1,
            Test2,
        }
    }
}
