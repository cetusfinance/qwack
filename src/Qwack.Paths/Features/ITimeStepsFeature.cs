using System;

namespace Qwack.Paths.Features
{
    public interface ITimeStepsFeature
    {
        int TimeStepCount { get; }

        void AddDate(DateTime date);
    }
}