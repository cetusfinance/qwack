using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Qwack.Core.Models
{
    public interface IPathBlock
    {
        double this[int index] { get; set; }

        int Factors { get; }
        int GlobalPathIndex { get; }
        int NumberOfPaths { get; }
        int NumberOfSteps { get; }
        double[] RawData { get; }
        int TotalBlockSize { get; }

        void Dispose();
        Span<double> GetEntirePath(int pathId);
        int GetIndexOfPathStart(int pathId, int factorId);
        Span<Vector<double>> GetStepsForFactor(int pathId, int factorId);
        Span<double> GetStepsForFactorSingle(int pathId, int factorId);
    }
}
