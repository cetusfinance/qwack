using System;

namespace Qwack.Paths.Features
{
    public interface ITimeStepsFeature
    {
        int TimeStepCount { get; }
        double[] TimeSteps { get; }

        void AddDate(DateTime date);
    }
}