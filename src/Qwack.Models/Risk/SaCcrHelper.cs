using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;
using static System.Math;
using static Qwack.Core.Basic.Capital.SaCcrParameters;

namespace Qwack.Models.Risk
{
    public static class SaCcrHelper
    {
        public const double DefaultAlpha = 1.4;
        public const double MultiplerFloor = 0.05;

        public static double SaCcrEad(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency, Dictionary<string, string> assetIdToTypeMap, Dictionary<string, SaCcrAssetClass> typeToAssetClassMap, ICurrencyProvider currencyProvider, double? pv = null, double alpha = DefaultAlpha)
        {
            foreach (ISaCcrEnabledCommodity ins in portfolio.Instruments)
            {
                ins.CommodityType = assetIdToTypeMap[(ins as IAssetInstrument).AssetIds.First()];
                ins.AssetClass = typeToAssetClassMap[ins.CommodityType];
            }
            IAssetFxModel m;
            if (model.FundingModel.FxMatrix.BaseCurrency != reportingCurrency)
            {
                var newFm = FundingModel.RemapBaseCurrency(model.FundingModel, reportingCurrency, currencyProvider);
                m = model.Clone(newFm);
            }
            else
            {
                m = model.Clone();
            }

            return SaCcrEad(portfolio, m, pv, alpha);
        }
        public static double SaCcrEad(this Portfolio portfolio, IAssetFxModel model, double? pv = null, double alpha = DefaultAlpha) => alpha * (Max(0, pv ?? Rc(portfolio, model)) + Pfe(portfolio, model, pv));
        public static double SaCcrEad_Margined(this Portfolio portfolio, IAssetFxModel model, SaCcrCollateralSpec collateralSpec, double? pv = null, double alpha = DefaultAlpha)
            => alpha * (Max(0, collateralSpec.Rc(portfolio, model, pv)) + Pfe(portfolio, model, pv, collateralSpec.MPOR, collateralSpec.Collateral));
        public static double Rc(Portfolio portfolio, IAssetFxModel model) => Max(0, portfolio.PV(model).SumOfAllRows);
        public static double Pfe(Portfolio portfolio, IAssetFxModel model, double? pv = null, double? MPOR = null, double? collateral = null)
        {
            var addon = AggregateAddOn(portfolio, model, MPOR);
            return addon == 0 ? 0.0 : Multipler(portfolio, model, pv, addon, collateral) * addon;
        }

        public static double Multipler(Portfolio portfolio, IAssetFxModel model, double? pv = null, double? addOn = null, double? collateral = null)
        {
            var v = (pv ?? Rc(portfolio, model));
            var vMinusC = collateral.HasValue ? v - collateral.Value : v;
            var multiplier = Min(1.0, MultiplerFloor + (1 - MultiplerFloor) * Exp(vMinusC / (2.0 * (1 - MultiplerFloor) * (addOn ?? AggregateAddOn(portfolio, model)))));
            return multiplier;
        }
        public static double AggregateAddOn(Portfolio portfolio, IAssetFxModel model, double? MPOR = null) => AddOnIr(portfolio, model, MPOR) + AddOnCredit(portfolio, model, MPOR) + AddOnCommodity(portfolio, model, MPOR) + AddOnFx(portfolio, model, MPOR) + AddOnEquity(portfolio, model, MPOR);
        public static double AddOnIr(Portfolio portfolio, IAssetFxModel model, double? MPOR = null)
        {
            var population = portfolio.Instruments.Where(x => x is ISaCcrEnabledIR).GroupBy(x => x.Currency);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var addOnIr = 0.0;
            foreach (var ccyGroup in population)
            {
                var fxToBase = baseCcy == null ? 1.0 : model.FundingModel.GetFxRate(model.BuildDate, $"{ccyGroup.Key}/{baseCcy.Ccy}");
                var irTrades = ccyGroup.Select(ir => ir as ISaCcrEnabledIR).ToArray();
                var irTradeBuckets = irTrades.GroupBy(x => x.MaturityBucket(model.BuildDate));
                var buckets = new[] { 1, 2, 3 };
                var D = new double[buckets.Max() + 1];
                foreach (var bucket in buckets)
                {
                    var tradesThisBucket = irTradeBuckets.FirstOrDefault(irtb => irtb.Key == bucket);
                    if (tradesThisBucket != null)
                        D[bucket] = tradesThisBucket.Sum(t =>
                            t.SupervisoryDelta(model) * fxToBase * t.SupervisoryDuration(model.BuildDate) * t.TradeNotional * t.MaturityFactor(model.BuildDate, MPOR));
                }

                var effNotional = Sqrt(D[1] * D[1] + D[2] * D[2] + D[3] * D[3] + 1.4 * D[1] * D[2] + 1.4 * D[2] * D[3] + 0.6 * D[1] * D[3]);
                var addOnForCcy = effNotional * SupervisoryFactors[SaCcrAssetClass.InterestRate];
                addOnIr += addOnForCcy;
            }
            return addOnIr;
        }
        public static double AddOnFx(Portfolio portfolio, IAssetFxModel model, double? MPOR = null)
        {
            var population = portfolio.Instruments.Select(x => x as ISaccrEnabledFx).Where(x => x != null).GroupBy(x => x.Pair);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var addOnFx = 0.0;
            foreach (var pairGroup in population)
            {
                var pair = pairGroup.Key;
                var fCccy = pair.Substring(pair.Length - 3, 3);
                var fxToBase = baseCcy == null ? 1.0 : model.FundingModel.GetFxRate(model.BuildDate, $"{fCccy}/{baseCcy.Ccy}");
                var effNotional = pairGroup.Sum(t => t.SupervisoryDelta(model) * t.ForeignNotional * fxToBase * t.MaturityFactor(model.BuildDate, MPOR));
                var addOnForPair = Abs(effNotional) * SupervisoryFactors[SaCcrAssetClass.Fx];
                addOnFx += addOnForPair;
            }

            return addOnFx;
        }

        public static double AddOnCredit(Portfolio portfolio, IAssetFxModel model, double? MPOR = null)
        {
            var population = portfolio.Instruments.Select(x => x as ISaccrEnabledCredit).Where(x => x != null).GroupBy(x => x.ReferenceName);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var addOnByName = new Dictionary<string, double>();
            var classByName = new Dictionary<string, SaCcrAssetClass>();
            foreach (var entityGroup in population)
            {
                var name = entityGroup.Key;
                var addOnForName = 0.0;
                var ccyGroups = entityGroup.GroupBy(e => (e as IInstrument).Currency);
                foreach (var ccyGroup in ccyGroups)
                {
                    var fCccy = ccyGroup.Key.Ccy;
                    var fxToBase = baseCcy == null ? 1.0 : model.FundingModel.GetFxRate(model.BuildDate, $"{fCccy}/{baseCcy.Ccy}");
                    var effNotional = ccyGroup.Sum(t => t.SupervisoryDelta(model) * t.TradeNotional * fxToBase * t.SupervisoryDuration(model.BuildDate) * t.MaturityFactor(model.BuildDate, MPOR));
                    var ratingGroup = ccyGroup.Select(e => e.ReferenceRating).Distinct();
                    if (ratingGroup.Count() > 1)
                        throw new Exception($"{ratingGroup.Count()} distict ratings found for entity {name}");

                    var aClass = RatingToClass(ratingGroup.Single());
                    addOnForName += effNotional * SupervisoryFactors[aClass];
                    classByName[name] = aClass;
                }

                addOnByName[name] = addOnForName;
            }

            var addOnCreditA = 0.0;
            var addOnCreditB = 0.0;

            foreach (var kv in addOnByName)
            {
                var aClass = classByName[kv.Key];
                var correlation = Correlations[aClass];
                var addOn = addOnByName[kv.Key];
                addOnCreditA += correlation * addOn;
                addOnCreditB += (1 - correlation * correlation) * addOn * addOn;
            }

            var addOnCredit = Sqrt(addOnCreditA * addOnCreditA + addOnCreditB);
            return addOnCredit;
        }

        private static SaCcrAssetClass RatingToClass(string rating)
        {
            if (rating == "IG")
                return SaCcrAssetClass.CreditIndexIG;
            if (rating == "SG")
                return SaCcrAssetClass.CreditIndexSG;

            var trialName = $"CreditSingle{rating.ToUpper()}";
            if (Enum.TryParse<SaCcrAssetClass>(trialName, out var aClass))
                return aClass;
            else
                return SaCcrAssetClass.CreditSingleCCC;
        }

        public static double AddOnCommodity(Portfolio portfolio, IAssetFxModel model, double? MPOR = null)
        {
            var population = portfolio.Instruments.Select(x => x as ISaCcrEnabledCommodity).Where(x => x != null).GroupBy(x => x.AssetClass);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var addOnByClass = new Dictionary<SaCcrAssetClass, Dictionary<string, double>>();
            foreach (var classGroup in population)
            {
                var aClass = classGroup.Key;
                addOnByClass[aClass] = new Dictionary<string, double>();
                var byType = classGroup.GroupBy(c => c.CommodityType);
                foreach (var typeGroup in byType)
                {
                    var addOnForType = 0.0;
                    var ccyGroups = typeGroup.GroupBy(e => (e as IInstrument).Currency);
                    foreach (var ccyGroup in ccyGroups)
                    {
                        var fCccy = ccyGroup.Key.Ccy;
                        var fxToBase = baseCcy == null ? 1.0 : model.FundingModel.GetFxRate(model.BuildDate, $"{fCccy}/{baseCcy.Ccy}");
                        var effNotional = ccyGroup.Sum(t => t.SupervisoryDelta(model) * t.TradeNotional(model) * fxToBase * t.MaturityFactor(model.BuildDate, MPOR));
                        addOnForType += effNotional * SupervisoryFactors[aClass];
                    }

                    addOnByClass[aClass][typeGroup.Key] = addOnForType;
                }
            }

            var addOnCommodity = 0.0;
            foreach (var cl in addOnByClass)
            {
                var addOnCommoA = 0.0;
                var addOnCommoB = 0.0;

                foreach (var kv in cl.Value)
                {
                    var aClass = cl.Key;
                    var correlation = Correlations[aClass];
                    var addOn = kv.Value;
                    addOnCommoA += correlation * addOn;
                    addOnCommoB += (1 - correlation * correlation) * addOn * addOn;
                }

                addOnCommodity += Sqrt(addOnCommoA * addOnCommoA + addOnCommoB);
            }

            return addOnCommodity;
        }


        public static double AddOnEquity(Portfolio portfolio, IAssetFxModel model, double? MPOR = null)
        {
            var population = portfolio.Instruments.Select(x => x as ISaccrEnabledEquity).Where(x => x != null).GroupBy(x => x.ReferenceName);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var addOnByName = new Dictionary<string, double>();
            var classByName = new Dictionary<string, SaCcrAssetClass>();
            foreach (var entityGroup in population)
            {
                var name = entityGroup.Key;
                var addOnForName = 0.0;
                var ccyGroups = entityGroup.GroupBy(e => (e as IInstrument).Currency);
                foreach (var ccyGroup in ccyGroups)
                {
                    var fCccy = ccyGroup.Key.Ccy;
                    var fxToBase = model.FundingModel.GetFxRate(model.BuildDate, $"{baseCcy.Ccy}/{fCccy}");
                    var effNotional = ccyGroup.Sum(t => t.SupervisoryDelta(model) * t.TradeNotional * fxToBase * t.MaturityFactor(model.BuildDate, MPOR));
                    var ratingGroup = ccyGroup.Select(e => e.IsIndex).Distinct();
                    if (ratingGroup.Count() != 1)
                        throw new Exception($"Inconsistent index/single stock distict ratings found for entity {name}");
                    var aClass = ratingGroup.Single() ? SaCcrAssetClass.EquityIndex : SaCcrAssetClass.EquitySingle;
                    addOnForName += effNotional * SupervisoryFactors[aClass];
                    classByName[name] = aClass;
                }

                addOnByName[name] = addOnForName;
            }

            var addOnEquityA = 0.0;
            var addOnEquityB = 0.0;

            foreach (var kv in addOnByName)
            {
                var aClass = classByName[kv.Key];
                var correlation = Correlations[aClass];
                var addOn = addOnByName[kv.Key];
                addOnEquityA += correlation * addOn;
                addOnEquityB += (1 - correlation * correlation) * addOn * addOn;
            }

            var addOnEquity = Sqrt(addOnEquityA * addOnEquityA + addOnEquityB);
            return addOnEquity;
        }
    }



    public class SaCcrCollateralSpec
    {
        public double Collateral { get; set; }
        public Currency CollateralCrurrency { get; set; }
        /// <summary>
        /// Variation margin (i.e. daily margining) is applicable
        /// </summary>
        public bool HasVm { get; set; }
        /// <summary>
        /// Minimum Transfer Amount
        /// </summary>
        public double MTA { get; set; }
        public double Threshold { get; set; }
        /// <summary>
        /// Net independent collateral amount
        /// </summary>
        public double NICA { get; set; }
        /// <summary>
        /// Margin Period of Risk, in business days
        /// </summary>
        public double? MPOR { get; set; }

        public double Rc(Portfolio portfolio, IAssetFxModel model, double? pv = null) => HasVm ?
            Max(0, (pv ?? portfolio.PV(model).SumOfAllRows) - Collateral) :
            Max(0, Max((pv ?? portfolio.PV(model).SumOfAllRows) - Collateral, MTA + Threshold - NICA));

        public void SetMPOR(int frequencyInBusinessDays) => MPOR = 10 + frequencyInBusinessDays - 1;
    }
}
