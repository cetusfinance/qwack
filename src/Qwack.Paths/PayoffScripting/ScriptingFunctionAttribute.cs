using System;

namespace Qwack.Paths.PayoffScripting
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ScriptingFunctionAttribute : Attribute
    {
        public ScriptingFunctionAttribute()
        {
        }

        public string Name { get; set; }
        public bool IsVector { get; set; }
    }
}
