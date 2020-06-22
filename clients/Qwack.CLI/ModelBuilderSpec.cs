using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Instruments.Funding;
using Qwack.Transport.TransportObjects.Instruments.Funding;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.CLI
{
    public class ModelBuilderSpec
    {
        public Dictionary<string, TO_FloatRateIndex> RateIndices { get; set; }
        public List<ModelBuilderSpecNymex> NymexSpecs { get; set; }
        public List<ModelBuilderSpecCmeBaseCurve> CmeBaseCurveSpecs { get; set; }
        public List<ModelBuilderSpecCmeBasisCurve> CmeBasisCurveSpecs { get; set; }
        public List<ModelBuilderSpecFxFuture> CmeFxFutureSpecs { get; set; }
        public List<TO_FxPair> FxPairs { get; set; }
    }

    public class ModelBuilderSpecNymex
    {
        public string NymexCodeFuture { get; set; }
        public string NymexCodeOption { get; set; }
        public string QwackCode { get; set; }
    }

    public class ModelBuilderSpecCmeBaseCurve
    {
        public string CmeCode { get; set; }
        public string QwackCode { get; set; }
        public string CurveName { get; set; }
        public string FloatRateIndex { get; set; }
        public bool IsCbot { get; set; }
    }

    public class ModelBuilderSpecCmeBasisCurve
    {
        public string FxPair { get; set; }
        public string CmeFxPair { get; set; }
        public string Currency { get; set; }
        public string CurveName { get; set; }
        public string BaseCurveName { get; set; }
    }

    public class ModelBuilderSpecFxFuture
    {
        public string FxPair { get; set; }
        public string CmeCodeFut { get; set; }
        public string CmeCodeOpt { get; set; }
        public string Currency { get; set; }
    }

}
