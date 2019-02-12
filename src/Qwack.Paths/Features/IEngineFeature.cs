using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Paths.Features
{
    public interface IEngineFeature
    {
        int NumberOfPaths { get; }
        int RoundedNumberOfPaths { get; }
    }
}
