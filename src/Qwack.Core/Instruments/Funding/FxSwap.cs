using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments.Funding
{
    public class FxSwap : IFundingInstrument
    {
        public FxSwap(double swapPoints, DateTime nearDate, DateTime farDate, double notional, Currency domesticCcy, Currency foreignCcy)
        {
            PillarDate = farDate;
            NearDate = nearDate;
            FarDate = farDate;
            Notional = notional;
            DomesticCcy = domesticCcy;
            ForeignCcy = foreignCcy;
            SwapPoints = swapPoints;
        }
        public double SwapPoints { get; set; }
        public DateTime NearDate { get; set; }
        public DateTime FarDate { get; set; }
        public double Notional { get; set; }
        public Currency DomesticCcy { get; }
        public Currency ForeignCcy { get; }
        public DateTime PillarDate { get; set; }

        public Currency Currency => DomesticCcy;

        public string SolveCurve { get; set; }
        public double Divisor { get; set; } = 10000.0;

        public string TradeId => throw new NotImplementedException();

        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }

        public DateTime LastSensitivityDate => FarDate;

        public double CalculateParRate(IFundingModel model)
        {
            var farFx = model.GetFxRate(FarDate, DomesticCcy, ForeignCcy);
            var nearFx = model.GetFxRate(NearDate, DomesticCcy, ForeignCcy);
            return (farFx - nearFx) * Divisor;
        }

        public IFundingInstrument Clone() => new FxSwap(SwapPoints, NearDate, FarDate, Notional, DomesticCcy, ForeignCcy)
        {
            SolveCurve = SolveCurve,
            PillarDate = PillarDate,
        };
        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { matrix.DiscountCurveMap[DomesticCcy], matrix.DiscountCurveMap[ForeignCcy] };
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
        {
            throw new NotImplementedException();
        }

        public double Pv(IFundingModel model, bool updateState)
        {
            var discName = model.FxMatrix.GetDiscountCurve(ForeignCcy);
            var discCurve = model.GetCurve(discName);
            var farFx = model.GetFxRate(FarDate, DomesticCcy, ForeignCcy);
            var nearFx = model.GetFxRate(NearDate, DomesticCcy, ForeignCcy);
            var farStrike = nearFx + SwapPoints/ Divisor;
            var farPv = -(farFx - farStrike) * Notional * discCurve.GetDf(model.BuildDate, FarDate);
            return farPv;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            throw new NotImplementedException();
        }

        public IFundingInstrument SetParRate(double parRate)
        {
            throw new NotImplementedException();
        }

        public double SuggestPillarValue(IFundingModel assetFxModel)
        {
            return 0.05;
        }
    }
}
