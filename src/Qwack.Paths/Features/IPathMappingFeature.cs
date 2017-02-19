using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features
{
    public interface IPathMappingFeature
    {
        int NumberOfDimensions { get; }
        /// <summary>
        /// Adds a dimension to the MC run
        /// </summary>
        /// <param name="dimensionName">The reference name of the dimension eg the underlying name</param>
        /// <returns>The index of the dimension which can be used later for indexing the path*dimension arrays</returns>
        int AddDimension(string dimensionName);
    }
}
