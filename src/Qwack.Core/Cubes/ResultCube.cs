using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Qwack.Core.Cubes
{
    public class ResultCube : ICube
    {
        private Type[] _numericalTypes = { typeof(double) };
        private List<Dictionary<string, object>> _rows;
        private Dictionary<string, Type> _types;

        public void Initialize(Dictionary<string, Type> dataTypes)
        {
            _types = dataTypes;
            _rows = new List<Dictionary<string, object>>();
        }

        public Dictionary<string, Type> DataTypes { get { return _types; } }

        public void AddRow(Dictionary<string, object> data)
        {
            _rows.Add(data);
        }

        public ICube Pivot(string fieldToAggregateBy, Dictionary<string, AggregationAction> aggregatedFields)
        {
            //for now, aggregate only works on numerical fields and performs a sum
            if (!_types.ContainsKey(fieldToAggregateBy))
                throw new Exception($"Cannot aggregate on field {fieldToAggregateBy} as it is not present");

            foreach(var f in aggregatedFields)
            {
                if (!_types.ContainsKey(f.Key))
                    throw new Exception($"Cannot aggregate field {f.Key} as it is not present");
            }

            var numericalFields = _types.Where(x => _numericalTypes.Contains(x.Value));

            var outputTypes = new Dictionary<string, Type>();
            outputTypes.Add(fieldToAggregateBy, _types[fieldToAggregateBy]);

            foreach (var kv in aggregatedFields)
            {
                if(!numericalFields.Any(x=>x.Key==kv.Key))
                    throw new Exception($"Cannot aggregate field {kv.Key} as it is not numeric");
                outputTypes.Add(kv.Key, _types[kv.Key]);
            }

            var distinctValues = _rows.Select(x => x[fieldToAggregateBy]).Distinct();

            var outCube = new ResultCube();
            outCube.Initialize(outputTypes);
            var aggData = new Dictionary<object, Dictionary<string,object>>();
            foreach (var row in _rows)
            {
                var rowKey = row[fieldToAggregateBy];
                if (!aggData.ContainsKey(rowKey))
                    aggData[rowKey] = new Dictionary<string, object>();

                foreach (var av in aggregatedFields)
                {
                    switch (av.Value)
                    {
                        case AggregationAction.Sum:
                            if (!aggData[rowKey].ContainsKey(av.Key))
                                aggData[rowKey][av.Key] = 0.0;

                            aggData[rowKey][av.Key] = (double)aggData[rowKey][av.Key] + (double)row[av.Key];
                            break;
                        case AggregationAction.Average:
                            if (!aggData[rowKey].ContainsKey(av.Key))
                                aggData[rowKey][av.Key] = new List<double>();
                            ((List<double>)aggData[rowKey][av.Key]).Add((double)row[av.Key]);
                            break;
                        case AggregationAction.Min:
                            if (!aggData[rowKey].ContainsKey(av.Key))
                                aggData[rowKey][av.Key] = double.MaxValue;
                            aggData[rowKey][av.Key] = System.Math.Min((double)aggData[rowKey][av.Key], (double)row[av.Key]);
                            break;
                        case AggregationAction.Max:
                            if (!aggData[rowKey].ContainsKey(av.Key))
                                aggData[rowKey][av.Key] = double.MinValue;
                            aggData[rowKey][av.Key] = System.Math.Min((double)aggData[rowKey][av.Key], (double)row[av.Key]);
                            break;
                    }
                }
            }

            //final post-processing for average
            foreach(var aggRow in aggData)
            {
                var rowDict = new Dictionary<string, object>();
                var rowKey = aggRow.Key;
                rowDict.Add(fieldToAggregateBy, rowKey);
                foreach (var av in aggregatedFields)
                {
                    switch (av.Value)
                    {
                        case AggregationAction.Average:
                            aggData[rowKey][av.Key] = ((List<double>)aggData[rowKey][av.Key]).Average();
                            break;
                    }

                    rowDict.Add(av.Key, aggData[rowKey][av.Key]);
                }

                outCube.AddRow(rowDict);
            }


            return outCube;
        }

        public ICube Filter(Dictionary<string,object> fieldsToFilterOn)
        {
            foreach (var fieldToFilterOn in fieldsToFilterOn.Keys)
            if (!_types.ContainsKey(fieldToFilterOn))
                throw new Exception($"Cannot filter on field {fieldToFilterOn} as it is not present");

            var outCube = new ResultCube();
            outCube.Initialize(_types);

            foreach (var row in _rows)
            {
                var rowIsRelevant = true;
                foreach (var kv in fieldsToFilterOn)
                {
                    if (!IsEqual(row[kv.Key], kv.Value))
                    {
                        rowIsRelevant = false;
                        break;
                    }
                }
                if (rowIsRelevant)
                    outCube.AddRow(row);
            }
            
            return outCube;
        }

        public Dictionary<string, object>[] GetAllRows()
        {
            return _rows.ToArray();
        }

        
        private bool IsEqual(object v1, object v2)
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
