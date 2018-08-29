using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Qwack.Core.Cubes
{
    public class ResultCubeRow
    {
        public double Value { get; set; }
        public object[] MetaData { get; set; }

        public ResultCubeRow() { }

        public ResultCubeRow(object[] metaData, double value)
        {
            MetaData = metaData;
            Value = value;
        }
    }

    public class ResultCube : ICube
    {
        private Type[] _numericalTypes = { typeof(double) };
        private List<ResultCubeRow> _rows;
        private Dictionary<string, Type> _types;
        private List<string> _fieldNames;

        public void Initialize(Dictionary<string, Type> dataTypes)
        {
            _types = dataTypes;
            _fieldNames = dataTypes.Keys.ToList();
            _rows = new List<ResultCubeRow>();
        }

        public Dictionary<string, Type> DataTypes { get { return _types; } }

        public int GetColumnIndex(string columnName) => _fieldNames.IndexOf(columnName);


        public void AddRow(Dictionary<string, object> data, double value)
        {
            var row = new object[data.Count];
            foreach (var d in data)
            {
                if (!_types.ContainsKey(d.Key))
                    throw new Exception($"Could not map field {d.Key}");

                var valAsType = Convert.ChangeType(d.Value, _types[d.Key]);
                if (valAsType == null)
                    throw new Exception($"Could not convert field {d.Key} value {d.Value} as type {_types[d.Key]}");

                var ix = _fieldNames.IndexOf(d.Key);
                row[ix] = d.Value;
            }
            _rows.Add(new ResultCubeRow(row,value));
        }

        public void AddRow(object[] data, double value)
        {
            _rows.Add(new ResultCubeRow(data, value));
        }

        public ICube Pivot(string fieldToAggregateBy, AggregationAction aggregationAction)
        {
            //for now, aggregate only works on numerical fields and performs a sum
            if (!_types.ContainsKey(fieldToAggregateBy))
                throw new Exception($"Cannot aggregate on field {fieldToAggregateBy} as it is not present");

            var ix = _fieldNames.IndexOf(fieldToAggregateBy);

            var distinctValues = _rows.Select(x => x.MetaData[ix]).Distinct();

            var outCube = new ResultCube();
            outCube.Initialize(new Dictionary<string, Type> { { fieldToAggregateBy, _types[fieldToAggregateBy] } });
            var aggData = new Dictionary<object, double>();
            var aggDataCount = new Dictionary<object, int>();
            foreach (var row in _rows)
            {
                var rowKey = row.MetaData[ix];
                if (!aggData.ContainsKey(rowKey))
                {
                    aggData[rowKey] = 0;
                    aggDataCount[rowKey] = 0;
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
                rowDict.Add(fieldToAggregateBy, rowKey);
                if (aggregationAction == AggregationAction.Average)
                    aggData[rowKey] /= aggDataCount[rowKey];

                outCube.AddRow(rowDict, aggData[rowKey]);
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

            var indexes = fieldsToFilterOn.Keys.ToDictionary(x => x, x => _fieldNames.IndexOf(x));

            foreach (var row in _rows)
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
                    outCube.AddRow(row.MetaData,row.Value);
            }
            
            return outCube;
        }

        public ResultCubeRow[] GetAllRows()
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
