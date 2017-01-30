using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Random.Sobol
{
    public class SobolDirectionNumbers
    {
        private SobolDirectionInfo[] _allDimensions;
        private static readonly char[] _splitArray = new char[] { ' ' };
        private int _dimensionToArrayOffset = 2;

        public void LoadFromFile(string fileName)
        {

            var allLines = File.ReadAllLines(fileName);

            _allDimensions = new SobolDirectionInfo[allLines.Length - 1];

            for (int i = 1; i < allLines.Length; i++)
            {
                string[] lineSplit = allLines[i].Split(_splitArray, StringSplitOptions.RemoveEmptyEntries);
                var dim = int.Parse(lineSplit[0]);
                if (dim != (i + 1))
                    throw new DataMisalignedException("The dimensions in the sobol numbers should be sequential and start at 2 on the second line!");
                SobolDirectionInfo info = new SobolDirectionInfo()
                {
                    Dimension = dim,
                    A = uint.Parse(lineSplit[2]),
                    S = uint.Parse(lineSplit[1]),
                    DirectionNumbers = new uint[lineSplit.Length - 3]
                };
                for (int x = 0; x < info.DirectionNumbers.Length; x++)
                {
                    info.DirectionNumbers[x] = uint.Parse(lineSplit[x + 3]);
                }
                _allDimensions[dim - _dimensionToArrayOffset] = info;
            }
        }

        public SobolDirectionInfo GetInfoForDimension(int dimension)
        {
            return _allDimensions[dimension - _dimensionToArrayOffset];
        }
    }
}
