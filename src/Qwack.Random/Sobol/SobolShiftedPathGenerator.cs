using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;
using Qwack.Paths;

namespace Qwack.Random.Sobol
{
    public class SobolShiftedPathGenerator : SobolPathGenerator
    {
        private double[] _dimensionShifts;

        public SobolShiftedPathGenerator(SobolDirectionNumbers dirNumbers, int seed)
            :base(dirNumbers, seed)
        {

        }

        public override void Finish(IFeatureCollection collection)
        {
            base.Finish(collection);
            SetupShifts();
        }

        protected override double ConvertRandToDouble(uint rand, int dimension)
        {
            var returnNumber = rand * s_convertToDoubleConstant;
            returnNumber += _dimensionShifts[dimension + Seed];
            if (returnNumber >= 1.0)
                returnNumber -= 1.0;

            //fix 1.0 and 0.0
            if (returnNumber == 0.0) returnNumber = 0.000000001;
            if (returnNumber == 1.0) returnNumber = 0.999999999;

            if (UseNormalInverse)
            {
                returnNumber = Math.Statistics.NormInv(returnNumber);
            }

            return returnNumber;
        }

        public void SetupShifts()
        {
            _dimensionShifts = new double[Dimensions + Seed];

            for (var i = 1; i <= _dimensionShifts.Length; i++)
            {
                _dimensionShifts[i - 1] = (i - 1.0) / i;
            }
        }
    }
}
