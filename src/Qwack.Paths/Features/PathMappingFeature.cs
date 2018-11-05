using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features
{
    public class PathMappingFeature : IPathMappingFeature
    {
        private object _locker = new object();
        private List<string> _dimensionNames = new List<string>();

        public int NumberOfDimensions => _dimensionNames.Count;

        public int AddDimension(string dimensionName)
        {
            lock (_locker)
            {
                var index = _dimensionNames.Count;
                _dimensionNames.Add(dimensionName);
                return index;
            }
        }

        public int GetDimension(string dimensionName)
        {
            lock (_locker)
            {
                return _dimensionNames.IndexOf(dimensionName);
            }
        }

        public List<string> GetDimensionNames() => _dimensionNames;
    }
}
