using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.Extensions.Logging.File;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class ZeroBond : IFundingInstrument
    {
        public ZeroBond() { }
        public ZeroBond(double price, DateTime maturityDate, string discountCurve):base()
        {
            Price = price;
            MaturityDate = maturityDate;
            DiscountCurve = discountCurve;

            SolveCurve = DiscountCurve;
            PillarDate = MaturityDate;
        }
        public string PortfolioName { get; set; }
        public double Notional { get; set; } = 1.0;
        public double Price { get; set; }
        public DateTime MaturityDate { get; set; }
        public Currency Currency { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public DateTime LastSensitivityDate => MaturityDate;

        public double Pv(IFundingModel model, bool updateState) => Notional * (model.Curves[DiscountCurve].GetDf(model.BuildDate, MaturityDate) - Price);

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting only
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, MaturityDate);
            var p = model.Curves[DiscountCurve].GetDf(model.BuildDate, MaturityDate);
            discountDict.Add(MaturityDate, -t * Notional * p);

            return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
            };
        }

        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();

        public double CalculateParRate(IFundingModel model)
        {
            var parRate = model.Curves[DiscountCurve].GetDf(model.BuildDate, MaturityDate);
            return parRate;
        }

        public IFundingInstrument Clone() => new ZeroBond
        {
            Currency = Currency,
            Counterparty = Counterparty,
            DiscountCurve = DiscountCurve,
            MaturityDate = MaturityDate,
            Notional = Notional,
            PillarDate = PillarDate,
            Price = Price,
            SolveCurve = SolveCurve,
            TradeId = TradeId
        };

        public IFundingInstrument SetParRate(double parRate)
        {
            var newIns = (ZeroBond)Clone();
            newIns.Price = parRate;
            return newIns;
        }

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new List<CashFlow>
            { new CashFlow()
                {
                    Currency = Currency,
                    SettleDate = MaturityDate,
                    Notional = Notional,
                    Fv = Notional
                }
        };

        public double SuggestPillarValue(IFundingModel model)
        {
            var discountCurve = model.Curves[SolveCurve];
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, MaturityDate);
            return -System.Math.Log(Price) / t;
        }
    }
}
