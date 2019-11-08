using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Math;
using Qwack.Math.Interpolation;

namespace Qwack.Models.Risk
{
    public class XVACalculator
    {
        public static double CVA(DateTime originDate, DateTime[] EPEDates, double[] EPEExposures, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD)
        {
            if (EPEDates.Length != EPEExposures.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");
            
            var lastDate = originDate;
            var cva = 0.0;

            for(var i=0;i<EPEDates.Length;i++)
            {
                if (EPEDates[i] < originDate)
                    continue;

                var pDefault = hazzardCurve.GetDefaultProbability(lastDate, EPEDates[i]);
                var df = discountCurve.GetDf(originDate, EPEDates[i]);
                cva += EPEExposures[i] * pDefault * df * LGD;

                lastDate = EPEDates[i];
            }
            
            return cva;
        }

        public static double CVA(DateTime originDate, ICube EPE, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD)
        {
            if (!EPE.DataTypes.TryGetValue("ExposureDate", out var type) || type != typeof(DateTime))
                throw new Exception("EPE cube input not valid");

            var rows = EPE.GetAllRows();
            var dateIx = EPE.GetColumnIndex("ExposureDate");
            var epeDates = new DateTime[rows.Length];
            var epeValues = new double[rows.Length];
            for(var i=0;i<rows.Length;i++)
            {
                epeDates[i] = (DateTime)rows[i].MetaData[dateIx];
                epeValues[i] = rows[i].Value;
            }
            return CVA(originDate, epeDates, epeValues, hazzardCurve, discountCurve, LGD);
        }

        public static (double FBA, double FCA) FVA(DateTime originDate, DateTime[] ExEDates, double[] EPEExposures, double[] ENEExposures, HazzardCurve hazzardCurve, IIrCurve discountCurve, IIrCurve fundingCurve)
        {
            if (ExEDates.Length != EPEExposures.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");

            if (ExEDates.Length != ENEExposures.Length)
                throw new Exception("Number of ENE dates and ENE values must be equal");


            var lastDate = originDate;
            var fba = 0.0;
            var fca = 0.0;
            for (var i = 0; i < ExEDates.Length; i++)
            {
                if (ExEDates[i] < originDate)
                    continue;

                var pSurvival = hazzardCurve.GetSurvivalProbability(lastDate, ExEDates[i]);
                var fwdDf = fundingCurve.GetDf(lastDate, ExEDates[i]) / discountCurve.GetDf(lastDate, ExEDates[i]);
                var df = fundingCurve.GetDf(originDate, ExEDates[i]);
                fba += ENEExposures[i] * pSurvival / fwdDf * df;
                fca += EPEExposures[i] * pSurvival / fwdDf * df;

                lastDate = ExEDates[i];
            }
            return (fba, fca);
        }

        public static (double FBA, double FCA) FVA(DateTime originDate, ICube EPE, ICube ENE, HazzardCurve hazzardCurve, IIrCurve discountCurve, IIrCurve fundingCurve)
        {
            if (!EPE.DataTypes.TryGetValue("ExposureDate", out var type) || type != typeof(DateTime))
                throw new Exception("EPE cube input not valid");

            if (!ENE.DataTypes.TryGetValue("ExposureDate", out var type2) || type2 != typeof(DateTime))
                throw new Exception("ENE cube input not valid");

            var rowsEPE = EPE.GetAllRows();
            var rowsENE = ENE.GetAllRows();
            if (rowsEPE.Length != rowsENE.Length)
                throw new Exception("EPE and ENE curves not of same size");

            var dateIx = EPE.GetColumnIndex("ExposureDate");
            var epeDates = new DateTime[rowsEPE.Length];
            var epeValues = new double[rowsEPE.Length];
            var eneValues = new double[rowsENE.Length];
            for (var i = 0; i < rowsENE.Length; i++)
            {
                epeDates[i] = (DateTime)rowsEPE[i].MetaData[dateIx];
                epeValues[i] = rowsEPE[i].Value;
                eneValues[i] = rowsENE[i].Value;
            }
            return FVA(originDate, epeDates, epeValues, eneValues, hazzardCurve, discountCurve, fundingCurve);
        }

        public static double KVA(DateTime originDate, DateTime[] ExEDates, double[] CapExposures, IIrCurve fundingCurve)
        {
            if (ExEDates.Length != CapExposures.Length)
                throw new Exception("Number of exposure dates and values must be equal");
            
            var lastDate = originDate;
            var kva = 0.0;
            for (var i = 0; i < ExEDates.Length; i++)
            {
                if (ExEDates[i] < originDate)
                    continue;

                var fwdDf = fundingCurve.GetDf(lastDate, ExEDates[i]);
                var df = fundingCurve.GetDf(originDate, ExEDates[i]);
                kva += CapExposures[i] / fwdDf * df;
                
                lastDate = ExEDates[i];
            }
            return kva;
        }

        public static double KVA(DateTime originDate, ICube ExpectedCapital, IIrCurve fundingCurve)
        {
            if (!ExpectedCapital.DataTypes.TryGetValue("ExposureDate", out var type) || type != typeof(DateTime))
                throw new Exception("ExpectedCapital cube input not valid");

            var rows = ExpectedCapital.GetAllRows();
            var dateIx = ExpectedCapital.GetColumnIndex("ExposureDate");
            var epeDates = new DateTime[rows.Length];
            var epeValues = new double[rows.Length];
            for (var i = 0; i < rows.Length; i++)
            {
                epeDates[i] = (DateTime)rows[i].MetaData[dateIx];
                epeValues[i] = rows[i].Value;
            }
            return KVA(originDate, epeDates, epeValues, fundingCurve);
        }
    }
}
