using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Qwack.Utils.Exceptions;

namespace Qwack.Random.Sobol
{
    public class SobolDirectionNumbers
    {
        private SobolDirectionInfo[] _allDimensions;
        private static readonly char[] _splitArray = new char[] { ' ' };
        private readonly int _dimensionToArrayOffset = 2;

        public SobolDirectionNumbers(string fileName) => LoadFromFile(fileName);
        public SobolDirectionNumbers() => LoadFromResource();

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

            var sb = new StringBuilder();
            sb.AppendLine("namespace Qwack.Random.Sobol");
            sb.AppendLine("{");
            sb.AppendLine("public class SobolDirectionNumbersEncoded");
            sb.AppendLine("{");
            sb.AppendLine("public SobolDirectionInfo[] AllDimensions = new [] {");
            for (var i = 0; i < _allDimensions.Length; i++)
            {
                var info = _allDimensions[i];
                if(i != 0)
                {
                    sb.Append(",");
                }
                sb.Append("new SobolDirectionInfo() { Dimension = " + info.Dimension + ", A = " + info.A + ", S = " + info.S + ", DirectionNumbers = [");
                for(var x = 0; x < info.DirectionNumbers.Length;x++)
                {
                    if (x != 0) sb.Append(",");
                    sb.Append(info.DirectionNumbers[x]);
                }
                sb.AppendLine("]}");
            }
            sb.AppendLine("};");
            sb.AppendLine("}");
            sb.AppendLine("}");

            System.IO.File.WriteAllText("C:\\code\\SobolDirectionNumbersEncoded.cs", sb.ToString());
        }

        private string GetEmbeddedResource(string namespacename, string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = namespacename + "." + filename;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();
                return result;
            }
        }

        private void LoadFromResource()
        {
            var allLines = GetEmbeddedResource("Qwack.Random.Sobol", "SobolDirectionNumbers.txt").Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            _allDimensions = new SobolDirectionInfo[allLines.Length - 1];
            for (var i = 1; i < allLines.Length; i++)
            {
                if (string.IsNullOrEmpty(allLines[i]))
                    continue;
                    
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
