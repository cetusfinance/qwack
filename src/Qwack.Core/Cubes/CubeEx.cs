using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;

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
            var distinctValues = rows.Select(x => string.Join("~", ixs.Select(ix => x.MetaData[ix]?.ToString()??string.Empty))).Distinct();

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
                var rowKey = string.Join("~", ixs.Select(i => row.MetaData[i]?.ToString()??string.Empty));
                if (!aggData.ContainsKey(rowKey))
                {
                    if (aggregationAction == AggregationAction.Min)
                        aggData[rowKey] = double.MaxValue;
                    else if (aggregationAction == AggregationAction.Max)
                        aggData[rowKey] = double.MinValue;
                    else
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
                        aggData[rowKey] += row.Value;
                        break;
                    case AggregationAction.Average:
                        aggData[rowKey] += row.Value;
                        aggDataCount[rowKey]++;
                        break;
                    case AggregationAction.Min:
                        aggData[rowKey] = System.Math.Min(aggData[rowKey], row.Value);
                        break;
                    case AggregationAction.Max:
                        aggData[rowKey] = System.Math.Max(aggData[rowKey], row.Value);
                        break;
                }

            }

            //final post-processing for average
            foreach (var rowKey in aggData.Keys.ToList())
            {
                var rowDict = new Dictionary<string, object>();
                if (aggregationAction == AggregationAction.Average)
                    aggData[rowKey] /= aggDataCount[rowKey];

                outCube.AddRow(metaDict[rowKey], aggData[rowKey]);
            }


            return outCube;
        }

        public static ICube Filter(this ICube cube, Dictionary<string, object> fieldsToFilterOn, bool filterOut=false)
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
                    if (!IsEqual(row.MetaData[indexes[kv.Key]], Convert.ChangeType(kv.Value,cube.DataTypes[kv.Key])))
                    {
                        rowIsRelevant = false;
                        break;
                    }
                }
                if (filterOut)
                    rowIsRelevant = !rowIsRelevant;

                if (rowIsRelevant)
                    outCube.AddRow(row.MetaData, row.Value);
            }

            return outCube;
        }

        public static ICube BucketTimeAxis(this ICube cube, string timeFieldName, string bucketedFieldName, Dictionary<DateTime,string> bucketBoundaries)
        {
            if (!cube.DataTypes.ContainsKey(timeFieldName))
                throw new Exception($"Cannot filter on field {timeFieldName} as it is not present");

            var outCube = new ResultCube();
            var newTypes = new Dictionary<string, Type>(cube.DataTypes);
            newTypes.Add(bucketedFieldName, typeof(string));
            outCube.Initialize(newTypes);

            var buckets = bucketBoundaries.Keys.OrderBy(x => x).ToList();
            var bucketFieldIx = cube.GetColumnIndex(timeFieldName);

            foreach (var row in cube.GetAllRows())
            {
                var date = (DateTime)row.MetaData[bucketFieldIx];
                var bucket = buckets.BinarySearch(date);
                if(bucket<0) bucket = ~bucket;
                var bucketLabel = bucketBoundaries[buckets[bucket]];

                var metaList = new List<object>(row.MetaData)
                {
                    bucketLabel
                };
                outCube.AddRow(metaList.ToArray(), row.Value);
            }

            return outCube;
        }

        public static Dictionary<object,List<ResultCubeRow>> ToDictionary(this ICube cube, string keyField)
        {
                if (!cube.DataTypes.ContainsKey(keyField))
                    throw new Exception($"Cannot filter on field {keyField} as it is not present");

            var output = new Dictionary<object, List<ResultCubeRow>>();

            var fieldNames = cube.DataTypes.Keys.ToList();
            var ix = fieldNames.IndexOf(keyField);

            foreach (var row in cube.GetAllRows())
            {
                if (!output.ContainsKey(row.MetaData[ix]))
                    output.Add(row.MetaData[ix], new List<ResultCubeRow>());

                output[row.MetaData[ix]].Add(row);
            }

            return output;
        }

        public static ICube Filter(this ICube cube, List<KeyValuePair<string, object>> fieldsToFilterOn, bool filterOut=false)
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
                if (filterOut)
                    rowIsRelevant = !rowIsRelevant;

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
            var indexes = fieldsToSortOn.Select(x => fieldNames.IndexOf(x)).Reverse();

            var rows = new List<ResultCubeRow>(cube.GetAllRows());
            foreach (var ix in indexes)
            {
                rows = rows.OrderBy(x => x.MetaData[ix]).ToList();
            }

            foreach (var row in rows)
            {
                outCube.AddRow(row.MetaData, row.Value);
            }

            return outCube;
        }

        public static ICube Sort(this ICube cube)
        {
            var outCube = new ResultCube();
            outCube.Initialize(cube.DataTypes);

            var fieldNames = cube.DataTypes.Keys.ToList();
            var indexes = Enumerable.Range(0, fieldNames.Count).Reverse().ToArray();
            var rows = new List<ResultCubeRow>(cube.GetAllRows());
            foreach(var ix in indexes)
            {
                rows = rows.OrderBy(x => x.MetaData[ix]).ToList();
            }

            foreach (var row in rows)
            {
                outCube.AddRow(row.MetaData, row.Value);
            }

            return outCube;
        }

        public static ICube ScalarMultiply(this ICube cube, double scalar)
        {
            var outCube = new ResultCube();
            outCube.Initialize(cube.DataTypes);
            var rows = new List<ResultCubeRow>(cube.GetAllRows());
            foreach (var row in rows)
                outCube.AddRow(row.MetaData, row.Value * scalar);

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
                for (var j = 0; j < otherIx.Length; j++)
                {
                    row[otherIx[j]] = br.MetaData[j];
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

        public static ICube Merge(this ICube baseCube, ICube otherCube, Dictionary<string,object> fieldsToAdd, Dictionary<string, object> fieldsToOverride = null, bool mergeTypes = false)
        {
            var o = new ResultCube();

            if (mergeTypes)
            {
                var mergedTypes = otherCube.DataTypes.Select(kv => kv).Concat(baseCube.DataTypes.Select(kvv => kvv)).Distinct().ToDictionary(x => x.Key, x => x.Value);
                o.Initialize(mergedTypes);
            }
            else
                o.Initialize(baseCube.DataTypes);

            var baseRows = baseCube.GetAllRows().ToArray();
            var otherRows = otherCube.GetAllRows().ToArray();
            var baseFieldNames = baseCube.DataTypes.Keys.ToArray();
            var otherFieldNames = otherCube.DataTypes.Keys.ToArray();

            for (var i = 0; i < baseRows.Length; i++)
            {
                var br = baseRows[i];
                if (mergeTypes)
                {
                    var rowDict = br.ToDictionary(baseFieldNames);
                    o.AddRow(rowDict, br.Value);
                }
                else
                {
                    o.AddRow(br.MetaData, br.Value);
                }
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
                case TypeCode.Double:
                    return cube.KeysForField<double>(fieldName).Select(x => (object)x).ToArray();
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

        [ExcludeFromCodeCoverage]
        public static void ToCSVFile(this ICube cube, string fileName)
        {
            var output = new List<string>
            {
                string.Join(",", cube.DataTypes.Keys.Select(x=>x).Concat(new [] {"Value" }))
            };

            output.Add(string.Join(",", cube.DataTypes.Values.Select(x => x.ToString()).Concat(new[] { "System.Double" })));

            foreach (var row in cube.GetAllRows())
            {
                output.Add(string.Join(",", 
                    row.MetaData.Select(x => Convert.ToString(x))
                    .Concat(new[] { Convert.ToString(row.Value) })));
            }

            System.IO.File.WriteAllLines(fileName, output.ToArray());
        }

        [ExcludeFromCodeCoverage]
        public static ICube FromCSVFile(string fileName, bool hasHeaderRow=true, bool hasValue=true)
        {
            var rawData = System.IO.File.ReadAllLines(fileName);
            var rawSplit = rawData.Select(x => x.Split(',')).ToArray();

            var cube = new ResultCube();
            var fieldNames = rawData[0].Split(',');
            var fieldTypesStr = rawData[1].Split(',');
            var fieldTypes = new Type[fieldTypesStr.Length - 1];
            var types = new Dictionary<string, Type>();

            if (!hasHeaderRow)
            {
                var maxWidth = rawSplit.Max(x => x.Length);
                fieldNames = Enumerable.Range(0,maxWidth).Select((x, ix) => ix.ToString()).ToArray();
                fieldTypes = fieldNames.Select(x => typeof(string)).ToArray();
                types = fieldNames.ToDictionary(x => x, x => typeof(string));
            }
            else
            {
                for (var i = 0; i < fieldNames.Length - 1; i++)
                {
                    fieldTypes[i] = Type.GetType(fieldTypesStr[i]);
                    types.Add(fieldNames[i], fieldTypes[i]);
                }
            }

            cube.Initialize(types);
            for (var i = hasHeaderRow ? 2 : 1; i < rawData.Length; i++)
            {
                var rawRow = rawSplit[i];
                var rowMeta = hasValue ? new object[fieldNames.Length - 1] : new object[fieldNames.Length];
                for (var j = 0; j < System.Math.Min(rowMeta.Length, rawRow.Length); j++)
                {
                    rowMeta[j] = Convert.ChangeType(rawRow[j], fieldTypes[j]);
                }
                var rowValue = hasValue ? Convert.ToDouble(rawRow.Last()) : 0.0;
                cube.AddRow(rowMeta, rowValue);
            }

            return cube;
        }

        [ExcludeFromCodeCoverage]
        public static ICube FromCSVFileRaw(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            using var csv = new CsvReader(sr);

            csv.Configuration.HasHeaderRecord = false;
            csv.Configuration.BadDataFound = null;

            var rawSplit = new List<string[]>();
            while(csv.Read())
            {
                var row = new List<string>();
                var i = 0;
                while (csv.TryGetField(i, out string val))
                {
                    i++;
                    row.Add(val);
                }
                rawSplit.Add(row.ToArray());
            }
            
            var cube = new ResultCube();

            var maxWidth = rawSplit.Max(x => x.Length);
                var fieldNames = Enumerable.Range(0, maxWidth).Select((x, ix) => ix.ToString()).ToArray();
                var fieldTypes = fieldNames.Select(x => typeof(string)).ToArray();
            var types = fieldNames.ToDictionary(x => x, x => typeof(string));
            
            cube.Initialize(types);
            for (var i = 0; i < rawSplit.Count; i++)
            {
                var rawRow = rawSplit[i];
                var rowMeta = new object[fieldNames.Length];
                for (var j = 0; j < System.Math.Min(rowMeta.Length, rawRow.Length); j++)
                {
                    rowMeta[j] = Convert.ChangeType(rawRow[j], fieldTypes[j]);
                }
                var rowValue = 0.0;
                cube.AddRow(rowMeta, rowValue);
            }

            return cube;
        }
    }
}
