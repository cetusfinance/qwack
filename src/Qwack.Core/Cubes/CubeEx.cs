using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static ICube Filter(this ICube cube, List<KeyValuePair<string, object>> fieldsToFilterOn)
        {
            foreach (var fieldToFilterOn in fieldsToFilterOn.Select(x=>x.Key))
                if (!cube.DataTypes.ContainsKey(fieldToFilterOn))
                    throw new Exception($"Cannot filter on field {fieldToFilterOn} as it is not present");

            var outCube = new ResultCube();
            outCube.Initialize(cube.DataTypes);

            var fieldNames = cube.DataTypes.Keys.ToList();
            var indexes = fieldsToFilterOn
                .Select(x => x.Key)
                .Distinct()
                .ToDictionary(x => x, x => fieldNames.IndexOf(x));
            var values = new Dictionary<string, List<object>>();
            foreach(var kv in fieldsToFilterOn)
            {
                if (!values.ContainsKey(kv.Key))
                    values[kv.Key] = new List<object> { kv.Value };
                else
                    values[kv.Key].Add(kv.Value);
            }

            foreach (var row in cube.GetAllRows())
            {
                var rowIsRelevant = true;
                foreach (var kv in values)
                {
                    if (!kv.Value.Any(v=>IsEqual(row.MetaData[indexes[kv.Key]], v)))
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

        public static ICube MergeQuick(this ICube baseCube, ICube otherCube)
        {
            if (!Enumerable.SequenceEqual(baseCube.DataTypes.Keys, otherCube.DataTypes.Keys) ||
                !Enumerable.SequenceEqual(baseCube.DataTypes.Values, otherCube.DataTypes.Values))
                throw new Exception("Cubes must be of same type to be merged");

            var o = new ResultCube();
            o.Initialize(baseCube.DataTypes);
            var baseRows = baseCube.GetAllRows().ToArray();
            var otherRows = otherCube.GetAllRows().ToArray();

            for (var i = 0; i < baseRows.Length; i++)
            {
                var br = baseRows[i];
                o.AddRow(br.MetaData, br.Value);
            }

            for (var i = 0; i < otherRows.Length; i++)
            {
                var br = otherRows[i];
                o.AddRow(br.MetaData, br.Value);
            }

            return o;
        }

        private static object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }

        public static ICube Merge(this ICube baseCube, ICube otherCube)
        {
            //add check that common fields are of them same type...
            foreach (var kv in baseCube.DataTypes.Where(x => otherCube.DataTypes.Keys.Contains(x.Key)))
                if (kv.Value != otherCube.DataTypes[kv.Key])
                    throw new Exception($"Data types dont match for field {kv.Key}");


            var newDataTypes = new Dictionary<string, Type>();
            foreach (var kv in baseCube.DataTypes)
                newDataTypes[kv.Key] = kv.Value;
            foreach (var kv in otherCube.DataTypes)
                newDataTypes[kv.Key] = kv.Value;


            var o = new ResultCube();
            o.Initialize(newDataTypes);
            var baseRows = baseCube.GetAllRows().ToArray();
            var otherRows = otherCube.GetAllRows().ToArray();

            var baseIx = baseCube.DataTypes.Keys.Select(k => o.GetColumnIndex(k)).ToArray();
            var otherIx = otherCube.DataTypes.Keys.Select(k => o.GetColumnIndex(k)).ToArray();

            var cleanRow = o.DataTypes.Select(kv => GetDefaultValue(kv.Value)).ToArray();

            for (var i = 0; i < baseRows.Length; i++)
            {
                var row = new object[cleanRow.Length];
                Array.Copy(cleanRow, row, row.Length);
                var br = baseRows[i];
                for(var j=0;j<baseIx.Length;j++)
                {
                    row[baseIx[j]] = br.MetaData[j];
                }
                o.AddRow(row, br.Value);
            }

            for (var i = 0; i < otherRows.Length; i++)
            {
                var br = otherRows[i];
                var row = new object[cleanRow.Length];
                Array.Copy(cleanRow, row, row.Length);
                for (var j = 0; j < baseIx.Length; j++)
                {
                    row[baseIx[j]] = br.MetaData[j];
                }
                o.AddRow(row, br.Value);
            }

            return o;
        }


        public static T[] KeysForField<T>(this ICube cube, string fieldName)
        {
            if(!cube.DataTypes.ContainsKey(fieldName))
                throw new Exception($"Cubes does contain field {fieldName}");
            if (cube.DataTypes[fieldName] != typeof(T))
                throw new Exception($"Field type does not match");


            var o = new List<T>();
            var ix = cube.DataTypes.Keys.ToList().IndexOf(fieldName);
            foreach (var r in cube.GetAllRows())
            {
                o.Add((T)r.MetaData[ix]);
            }

            return o.Distinct().ToArray();
        }

        public static object[,] ToMatrix(this ICube cube, string verticalField, string horizontalField, bool sort)
        {
            if (!cube.DataTypes.ContainsKey(verticalField))
                throw new Exception($"Cubes does contain field {verticalField}");

            if (!cube.DataTypes.ContainsKey(horizontalField))
                throw new Exception($"Cubes does contain field {horizontalField}");

            var aggregated = cube.Pivot(new string[] { verticalField, horizontalField }, AggregationAction.Sum);

            var distinctV = cube.KeysForField(verticalField);
            var distinctH = cube.KeysForField(horizontalField);

            if (sort)
            {
                //catch date labels
                if (distinctH.All(x => DateTime.TryParseExact(x as string, new string[] { "MMMyy", "MMM-yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)))
                    distinctH = distinctH.OrderBy(x => DateTime.ParseExact(x as string, new string[] { "MMMyy", "MMM-yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None)).ToArray();
                else
                    distinctH = distinctH.OrderBy(x => x).ToArray();

                if (distinctV.All(x => DateTime.TryParseExact(x as string, new string[] { "MMMyy", "MMM-yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)))
                    distinctV = distinctV.OrderBy(x => DateTime.ParseExact(x as string, new string[] { "MMMyy", "MMM-yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None)).ToArray();
                else
                    distinctV = distinctV.OrderBy(x => x).ToArray();
            }

            var o = new object[distinctV.Length+1, distinctH.Length+1];
            for(var r=0;r<o.GetLength(0);r++)
            {
                for(var c=0;c<o.GetLength(1);c++)
                {
                    if (r == 0 && c == 0)
                        continue;
                    else if (r == 0) //titles
                    {
                        o[r, c] = distinctH[c - 1];
                    }
                    else if (c == 0)
                    {
                        o[r, c] = distinctV[r - 1];
                    }
                    else
                    {
                        var data = aggregated.Filter(new Dictionary<string, object> {
                            { verticalField, distinctV[r - 1] },
                            { horizontalField, distinctH[c - 1] }
                        }).GetAllRows();
                        if(data.Any())
                        {
                            o[r, c] = data.First().Value;
                        }
                    }
                }
            }
            return o;
        }

        public static ICube Merge(this ICube baseCube, ICube otherCube, Dictionary<string,object> fieldsToAdd, Dictionary<string, object> fieldsToOverride = null)
        {
            var o = new ResultCube();
            o.Initialize(baseCube.DataTypes);
            var baseRows = baseCube.GetAllRows().ToArray();
            var otherRows = otherCube.GetAllRows().ToArray();
            var otherFieldNames = otherCube.DataTypes.Keys.ToArray();

            for (var i = 0; i < baseRows.Length; i++)
            {
                var br = baseRows[i];
                o.AddRow(br.MetaData, br.Value);
            }

            for (var i = 0; i < otherRows.Length; i++)
            {
                var br = otherRows[i];
                var rowDict = new Dictionary<string, object>();
                for(var j=0;j<br.MetaData.Length;j++)
                {
                    rowDict.Add(otherFieldNames[j], br.MetaData[j]);
                }
                foreach(var fa in fieldsToAdd)
                {
                    rowDict[fa.Key] = fa.Value;
                }
                if(fieldsToOverride!=null)
                {
                    foreach (var fa in fieldsToOverride)
                    {
                        rowDict[fa.Key] = fa.Value;
                    }
                }
                o.AddRow(rowDict, br.Value);
            }

            return o;
        }

        public static bool IsEqual(object v1, object v2)
        {
            switch (v1)
            {
                case string vs:
                    return v2 is string && (string)v1 == (string)v2;
                case double vd:
                    return v2 is double && (double)v1 == (double)v2;
                case bool vb:
                    return v2 is bool && (bool)v1 == (bool)v2;
                case decimal vq:
                    return v2 is decimal && (decimal)v1 == (decimal)v2;
                case DateTime vt:
                    return v2 is DateTime && (DateTime)v1 == (DateTime)v2;
                case char vc:
                    return v2 is char && (char)v1 == (char)v2;
                case int vi:
                    return v2 is int && (int)v1 == (int)v2;
            }
            return false;
        }

        public static object[] KeysForField(this ICube cube, string fieldName)
        {
            if (!cube.DataTypes.ContainsKey(fieldName))
                throw new Exception($"Cubes does contain field {fieldName}");

            switch(Type.GetTypeCode(cube.DataTypes[fieldName]))
            {
                case TypeCode.String:
                    return cube.KeysForField<string>(fieldName);
                case TypeCode.DateTime:
                    return cube.KeysForField<DateTime>(fieldName).Select(x=>(object)x).ToArray();
                case TypeCode.Int32:
                    return cube.KeysForField<int>(fieldName).Select(x => (object)x).ToArray();
                case TypeCode.Boolean:
                    return cube.KeysForField<bool>(fieldName).Select(x => (object)x).ToArray();
                default:
                    throw new Exception("Unknown type");
            }
        }

        public static void ToCSVFile(this ICube cube, string fileName)
        {
            var output = new List<string>
            {
                string.Join(",", cube.DataTypes.Keys.Select(x=>x).Concat(new [] {"Value" }))
            };

            foreach(var row in cube.GetAllRows())
            {
                output.Add(string.Join(",", 
                    row.MetaData.Select(x => Convert.ToString(x))
                    .Concat(new[] { Convert.ToString(row.Value) })));
            }

            System.IO.File.WriteAllLines(fileName, output.ToArray());
        }
    }
}
