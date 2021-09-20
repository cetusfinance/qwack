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
        public static void ThrowException(ExceptionType exceptionType, string extraMessage = null) => throw exceptionType switch
        {
            ExceptionType.InvalidFileInput => new System.IO.InvalidDataException($"{SR.ResourceManager.GetString(exceptionType.ToString())}-{extraMessage}"),
            ExceptionType.InvalidDataAlignment => new DataMisalignedException(extraMessage),
            _ => new InvalidOperationException($"Unknown exception type {exceptionType}"),
        };
    }
}
