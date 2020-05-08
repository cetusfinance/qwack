using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;
using Qwack.Transport.TransportObjects.MarketData.Correlations;

namespace Qwack.Core.Basic.Correlation
{
    public static class CorrelationMatrixFactory
    {
        public static ICorrelationMatrix GetCorrelationMatrix(this TO_CorrelationMatrix transportObject) =>
            transportObject.IsTimeVector ?
            (ICorrelationMatrix) new CorrelationTimeVector(transportObject.LabelsX[0], transportObject.LabelsY, transportObject.CorrelationsTime, transportObject.Times, transportObject.InterpolatorType) :
            (ICorrelationMatrix) new CorrelationMatrix(transportObject.LabelsX, transportObject.LabelsY, transportObject.Correlations);

        public static TO_CorrelationMatrix GetTransportObject(this ICorrelationMatrix matrix)
        {
            switch(matrix)
            {
                case CorrelationMatrix cm:
                    return cm.GetTransportObject();
                case CorrelationTimeVector ctv:
                    return ctv.GetTransportObject();
                default:
                    throw new Exception("Unable to serialize correlation object");
            }
        }
    }
}
