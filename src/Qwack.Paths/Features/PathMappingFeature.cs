using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features
{
    public class PathMappingFeature : IPathMappingFeature
    {
        private List<string> _dimensionNames = new List<string>();

        public int NumberOfDimensions => _dimensionNames.Count;

        public int AddDimension(string dimensionName)
        {
            var index = _dimensionNames.Count;
            _dimensionNames.Add(dimensionName);
            return index;
        }
    }
}
