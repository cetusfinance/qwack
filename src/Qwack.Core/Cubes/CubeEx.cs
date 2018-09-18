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

        public static ICube Pivot(this ICube cube, string fieldToAggregateBy, AggregationAction aggregationAction)
        {
            return cube.Pivot(new[] { fieldToAggregateBy }, aggregationAction);
        }


        public static ICube Pivot(this ICube cube, string[] fieldsToAggregateBy, AggregationAction aggregationAction)
        {
            //for now, aggregate only works on numerical fields and performs a sum
            foreach (var fieldToAggregateBy in fieldsToAggregateBy)
                if (!cube.DataTypes.ContainsKey(fieldToAggregateBy))
                    throw new Exception($"Cannot aggregate on field {fieldToAggregateBy} as it is not present");

            var types = cube.DataTypes.Keys.ToList();
            var ixs = fieldsToAggregateBy.Select(f => types.IndexOf(f)).ToArray();

            var rows = cube.GetAllRows();
            var distinctValues = rows.Select(x => string.Join("~", ixs.Select(ix => x.MetaData[ix].ToString()))).Distinct();

            var outCube = new ResultCube();
            var oT = new Dictionary<string, Type>();
            foreach (var fieldToAggregateBy in fieldsToAggregateBy)
                oT.Add(fieldToAggregateBy, cube.DataTypes[fieldToAggregateBy]);
            outCube.Initialize(oT);
            var aggData = new Dictionary<string, double>();
            var aggDataCount = new Dictionary<string, int>();
            var metaDict = new Dictionary<string, object[]>();
            foreach (var row in rows)
            {
                var rowKey = string.Join("~", ixs.Select(i => row.MetaData[i].ToString()));
                if (!aggData.ContainsKey(rowKey))
                {
                    aggData[rowKey] = 0;
                    aggDataCount[rowKey] = 0;
                    var filetedMetaData = new object[ixs.Length];
                    for (var i = 0; i < ixs.Length; i++)
                        filetedMetaData[i] = row.MetaData[ixs[i]];
                    metaDict[rowKey] = filetedMetaData;
                }
                switch (aggregationAction)
                {
                    case AggregationAction.Sum:
                        if (!aggData.ContainsKey(rowKey))
                        {
                            aggData[rowKey] = 0;
                            aggDataCount[rowKey] = 0;
                        }
                        aggData[rowKey] += row.Value;
                        break;
                    case AggregationAction.Average:
                        if (!aggData.ContainsKey(rowKey))
                        {
                            aggData[rowKey] = 0;
                            aggDataCount[rowKey] = 0;
                        }
                        aggData[rowKey] += row.Value;
                        aggDataCount[rowKey]++;
                        break;
                    case AggregationAction.Min:
                        if (!aggData.ContainsKey(rowKey))
                            aggData[rowKey] = double.MaxValue;
                        aggData[rowKey] = System.Math.Min(aggData[rowKey], row.Value);
                        break;
                    case AggregationAction.Max:
                        if (!aggData.ContainsKey(rowKey))
                            aggData[rowKey] = double.MinValue;
                        aggData[rowKey] = System.Math.Max(aggData[rowKey], row.Value);
                        break;
                }

            }

            //final post-processing for average
            foreach (var aggRow in aggData)
            {
                var rowDict = new Dictionary<string, object>();
                var rowKey = aggRow.Key;
                if (aggregationAction == AggregationAction.Average)
                    aggData[rowKey] /= aggDataCount[rowKey];

                outCube.AddRow(metaDict[rowKey], aggData[rowKey]);
            }


            return outCube;
        }

        public static ICube Filter(this ICube cube, Dictionary<string, object> fieldsToFilterOn)
        {
            foreach (var fieldToFilterOn in fieldsToFilterOn.Keys)
                if (!cube.DataTypes.ContainsKey(fieldToFilterOn))
                    throw new Exception($"Cannot filter on field {fieldToFilterOn} as it is not present");

            var outCube = new ResultCube();
            outCube.Initialize(cube.DataTypes);

            var fieldNames = cube.DataTypes.Keys.ToList();
            var indexes = fieldsToFilterOn.Keys.ToDictionary(x => x, x => fieldNames.IndexOf(x));

            foreach (var row in cube.GetAllRows())
            {
                var rowIsRelevant = true;
                foreach (var kv in fieldsToFilterOn)
                {
                    if (!IsEqual(row.MetaData[indexes[kv.Key]], kv.Value))
                    {
                        rowIsRelevant = false;
                        break;
                    }
                }
                if (rowIsRelevant)
                    outCube.AddRow(row.MetaData, row.Value);
            }

            return outCube;
        }

        public static ICube Sort(this ICube cube, List<string> fieldsToSortOn)
        {
            foreach (var fieldToSortOn in fieldsToSortOn)
                if (!cube.DataTypes.ContainsKey(fieldToSortOn))
                    throw new Exception($"Cannot sort on field {fieldToSortOn} as it is not present");

            var outCube = new ResultCube();
            outCube.Initialize(cube.DataTypes);

            var fieldNames = cube.DataTypes.Keys.ToList();
            var indexes = fieldsToSortOn.Select(x => fieldNames.IndexOf(x));

            var rows = cube.GetAllRows();
            var rowKeys = new Dictionary<string, int>();

            var c = 0;
            foreach (var row in rows)
            {
                var key = string.Join("~", indexes.Select(f => row.MetaData[f]));
                rowKeys.Add(key, c);
                c++;
            }

            foreach (var kv in rowKeys.OrderBy(x => x.Key))
            {
                outCube.AddRow(rows[kv.Value].MetaData, rows[kv.Value].Value);
            }

            return outCube;
        }

        public static bool IsEqual(object v1, object v2)
        {
            switch (v1)
            {
                case string vs:
                    return (string)v1 == (string)v2;
                case double vd:
                    return (double)v1 == (double)v2;
                case bool vb:
                    return (bool)v1 == (bool)v2;
                case decimal vq:
                    return (decimal)v1 == (decimal)v2;
                case DateTime vt:
                    return (DateTime)v1 == (DateTime)v2;
                case char vc:
                    return (char)v1 == (char)v2;
                case int vi:
                    return (int)v1 == (int)v2;
            }
            return false;

        }
    }
}
