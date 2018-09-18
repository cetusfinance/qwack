using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Core.Cubes
{
    public static class CubeEx
    {
        public static ICube Difference(this ICube baseCube, ICube cubeToSubtract)
        {
            if (!Enumerable.SequenceEqual(baseCube.DataTypes.Keys, cubeToSubtract.DataTypes.Keys) ||
               !Enumerable.SequenceEqual(baseCube.DataTypes.Values, cubeToSubtract.DataTypes.Values))
                throw new Exception("Cubes must be of same type to be differenced");

            var o = new ResultCube();
            o.Initialize(baseCube.DataTypes);
            var baseRows = baseCube.GetAllRows().ToList();
            var subRows = cubeToSubtract.GetAllRows().ToList();
            foreach(var br in baseRows)
            {
                var rowFound = false;
                foreach(var sr in subRows)
                {
                    if(Enumerable.SequenceEqual(br.MetaData,sr.MetaData))
                    {
                        o.AddRow(br.MetaData, br.Value - sr.Value);
                        subRows.Remove(sr);
                        rowFound = true;
                        break;
                    }
                }

                if(!rowFound) //zero to subtract
                {
                    o.AddRow(br.MetaData, br.Value);
                }
            }

            //look at what is left in subrows
            foreach (var sr in subRows)
            {
                o.AddRow(sr.MetaData, -sr.Value);
            }

            return o;
        }

        /// <summary>
        /// Differences two cubes, assuming same number and same order of rows in both
        /// </summary>
        /// <param name="baseCube"></param>
        /// <param name="cubeToSubtract"></param>
        /// <returns></returns>
        public static ICube QuickDifference(this ICube baseCube, ICube cubeToSubtract)
        {
            if (!Enumerable.SequenceEqual(baseCube.DataTypes.Keys, cubeToSubtract.DataTypes.Keys) ||
               !Enumerable.SequenceEqual(baseCube.DataTypes.Values, cubeToSubtract.DataTypes.Values))
                throw new Exception("Cubes must be of same type to be differenced");

            var o = new ResultCube();
            o.Initialize(baseCube.DataTypes);
            var baseRows = baseCube.GetAllRows().ToArray();
            var subRows = cubeToSubtract.GetAllRows().ToArray();

            if(baseRows.Length!=subRows.Length)
                throw new Exception("Cubes must have same number of rows to quick-diff them");

            for(var i=0;i<baseRows.Length;i++)
            {
                var br = baseRows[i];
                var sr = subRows[i];
                o.AddRow(br.MetaData, br.Value - sr.Value);
            }

            return o;
        }
    }
}
