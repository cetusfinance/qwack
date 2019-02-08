using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;

namespace Qwack.Models.Risk
{
    public class CVACalculator
    {
        public static double CVA(DateTime originDate, DateTime[] EPEDates, double[] EPEExposures, HazzardCurve hazzardCurve, IIrCurve discountCurve)
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
                cva += EPEExposures[i] * pDefault * df;
            }
            return cva;
        }

        public static double CVA(DateTime originDate, ICube EPE, HazzardCurve hazzardCurve, IIrCurve discountCurve)
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
            return CVA(originDate, epeDates, epeValues, hazzardCurve, discountCurve);
        }
    }
}
