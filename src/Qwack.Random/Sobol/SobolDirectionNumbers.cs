using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Utils.Exceptions;

namespace Qwack.Random.Sobol
{
    public class SobolDirectionNumbers
    {
        private SobolDirectionInfo[] _allDimensions;
        private static readonly char[] _splitArray = new char[] { ' ' };
        private readonly int _dimensionToArrayOffset = 2;

        public SobolDirectionNumbers(string fileName) => LoadFromFile(fileName);

        private void LoadFromFile(string fileName)
        {
            var allLines = File.ReadAllLines(fileName);
            _allDimensions = new SobolDirectionInfo[allLines.Length - 1];
            for (var i = 1; i < allLines.Length; i++)
            {
                var lineSplit = allLines[i].Split(_splitArray, StringSplitOptions.RemoveEmptyEntries);
                var dim = int.Parse(lineSplit[0]);
                if (dim != (i + 1))
                {
                    ExceptionHelper.ThrowException(ExceptionType.InvalidFileInput, "The dimensions in the sobol numbers should be sequential and start at 2 on the second line");
                }
                var info = new SobolDirectionInfo()
                {
                    Dimension = dim,
                    A = uint.Parse(lineSplit[2]),
                    S = uint.Parse(lineSplit[1]),
                    DirectionNumbers = new uint[lineSplit.Length - 3]
                };
                for (var x = 0; x < info.DirectionNumbers.Length; x++)
                {
                    info.DirectionNumbers[x] = uint.Parse(lineSplit[x + 3]);
                }
                _allDimensions[dim - _dimensionToArrayOffset] = info;
            }
        }

        public SobolDirectionInfo GetInfoForDimension(int dimension) => _allDimensions[dimension - _dimensionToArrayOffset];
    }
}
