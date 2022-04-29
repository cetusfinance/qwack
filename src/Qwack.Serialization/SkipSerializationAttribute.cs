using System;

namespace Qwack.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SkipSerializationAttribute : Attribute
    {
    }
}
