using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SkipSerializationAttribute:Attribute
    {
    }
}
