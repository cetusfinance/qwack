using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.PayoffScripting
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ScriptingFunctionAttribute : Attribute
    {
        public ScriptingFunctionAttribute()
        {
        }

        public string Name { get; set; }
        public bool IsVector { get; set; }
    }
}
