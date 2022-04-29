using System;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Transport.TransportObjects.MarketData.Correlations;

namespace Qwack.Core.Basic.Correlation
{
    public static class CorrelationMatrixFactory
    {
        public static ICorrelationMatrix GetCorrelationMatrix(this TO_CorrelationMatrix transportObject) =>
            transportObject.Children != null ?
            (ICorrelationMatrix)new CorrelationMatrixCollection(transportObject.Children.Select(x => x.GetCorrelationMatrix()).ToArray()) :
            transportObject.IsTimeVector ?
            (ICorrelationMatrix)new CorrelationTimeVector(transportObject.LabelsX[0], transportObject.LabelsY, transportObject.Correlations, transportObject.Times, transportObject.InterpolatorType) :
            (ICorrelationMatrix)new CorrelationMatrix(transportObject.LabelsX, transportObject.LabelsY, transportObject.Correlations);

        public static TO_CorrelationMatrix GetTransportObject(this ICorrelationMatrix matrix) => matrix switch
        {
            CorrelationMatrix cm => cm.GetTransportObject(),
            CorrelationTimeVector ctv => ctv.GetTransportObject(),
            CorrelationMatrixCollection ctc => ctc.GetTransportObject(),
            _ => throw new Exception("Unable to serialize correlation object"),
        };
    }
}
