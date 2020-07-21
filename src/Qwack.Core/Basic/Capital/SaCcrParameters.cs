using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Basic.Capital
{
    public static class SaCcrParameters
    {
        public static Dictionary<SaCcrAssetClass, double> SupervisoryFactors = new Dictionary<SaCcrAssetClass, double>()
        {
            { SaCcrAssetClass.InterestRate, 0.005 },
            { SaCcrAssetClass.Fx, 0.04 },
            { SaCcrAssetClass.CreditSingleAAA, 0.0038 },
            { SaCcrAssetClass.CreditSingleAA, 0.0038 },
            { SaCcrAssetClass.CreditSingleA, 0.0042 },
            { SaCcrAssetClass.CreditSingleBBB, 0.0054 },
            { SaCcrAssetClass.CreditSingleBB, 0.0106 },
            { SaCcrAssetClass.CreditSingleB, 0.016 },
            { SaCcrAssetClass.CreditSingleCCC, 0.06 },
            { SaCcrAssetClass.CreditIndexIG, 0.0038 },
            { SaCcrAssetClass.CreditIndexSG, 0.0106},
            { SaCcrAssetClass.EquitySingle, 0.32 },
            { SaCcrAssetClass.EquityIndex, 0.20 },
            { SaCcrAssetClass.CommoPower, 0.4 },
            { SaCcrAssetClass.CommoOilGas, 0.18 },
            { SaCcrAssetClass.CommoMetals, 0.18 },
            { SaCcrAssetClass.CommoAgri, 0.18 },
            { SaCcrAssetClass.CommoOther, 0.18 },
        };

        public static Dictionary<SaCcrAssetClass, double> Correlations = new Dictionary<SaCcrAssetClass, double>()
        {
            { SaCcrAssetClass.CreditSingleAAA, 0.5 },
            { SaCcrAssetClass.CreditSingleAA, 0.5 },
            { SaCcrAssetClass.CreditSingleA, 0.5 },
            { SaCcrAssetClass.CreditSingleBBB, 0.5 },
            { SaCcrAssetClass.CreditSingleBB, 0.5 },
            { SaCcrAssetClass.CreditSingleB, 0.5 },
            { SaCcrAssetClass.CreditSingleCCC, 0.5 },
            { SaCcrAssetClass.CreditIndexIG, 0.8 },
            { SaCcrAssetClass.CreditIndexSG, 0.8},
            { SaCcrAssetClass.EquitySingle, 0.5 },
            { SaCcrAssetClass.EquityIndex, 0.8 },
            { SaCcrAssetClass.CommoPower, 0.4 },
            { SaCcrAssetClass.CommoOilGas, 0.4 },
            { SaCcrAssetClass.CommoMetals, 0.4 },
            { SaCcrAssetClass.CommoAgri, 0.4 },
            { SaCcrAssetClass.CommoOther, 0.4 },
        };

        public static Dictionary<SaCcrAssetClass, double> SupervisoryOptionVols = new Dictionary<SaCcrAssetClass, double>()
        {
            { SaCcrAssetClass.InterestRate, 0.5 },
            { SaCcrAssetClass.Fx, 0.15 },
            { SaCcrAssetClass.CreditSingleAAA, 1.0 },
            { SaCcrAssetClass.CreditSingleAA, 1.0 },
            { SaCcrAssetClass.CreditSingleA, 1.0 },
            { SaCcrAssetClass.CreditSingleBBB, 1.0 },
            { SaCcrAssetClass.CreditSingleBB, 1.0 },
            { SaCcrAssetClass.CreditSingleB, 1.0 },
            { SaCcrAssetClass.CreditSingleCCC, 1.0 },
            { SaCcrAssetClass.CreditIndexIG, 0.8 },
            { SaCcrAssetClass.CreditIndexSG, 0.8},
            { SaCcrAssetClass.EquitySingle, 1.2 },
            { SaCcrAssetClass.EquityIndex, 0.75 },
            { SaCcrAssetClass.CommoPower, 1.5 },
            { SaCcrAssetClass.CommoOilGas, 0.7 },
            { SaCcrAssetClass.CommoMetals, 0.7 },
            { SaCcrAssetClass.CommoAgri, 0.7 },
            { SaCcrAssetClass.CommoOther, 0.7 },
        };
    }
}
