using System.Collections.Generic;

namespace Qwack.Paths.Features
{
    public class PathMappingFeature : IPathMappingFeature
    {
        private readonly object _locker = new();
        private readonly List<string> _dimensionNames = new();

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
