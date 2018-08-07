using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Futures
{
    public interface IFutureSettingsProvider
    {
        FutureSettings this[string futureName] { get; }
        bool TryGet(string futureName, out FutureSettings futureSettings);
    }
}
