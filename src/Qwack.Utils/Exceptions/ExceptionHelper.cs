using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Qwack.Utils.Exceptions
{
    public class ExceptionHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowException(ExceptionType exceptionType, string extraMessage = null)
        {
            switch(exceptionType)
            {
                case ExceptionType.InvalidFileInput:
                    throw new System.IO.InvalidDataException($"{SR.ResourceManager.GetString(exceptionType.ToString())}-{extraMessage}");
                default:
                    throw new InvalidOperationException($"Unknown exception type {exceptionType}");
            }
        }
    }
}
