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

        public Dictionary<string,object> ToDictionary(string[] fieldNames)
        {
            if (fieldNames.Length != MetaData.Length)
                throw new DataMisalignedException();

            return fieldNames
                .Select((x, ix) => new KeyValuePair<string, object>(x, MetaData[ix]))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public class ResultCube : ICube
    {
        private readonly Type[] _numericalTypes = { typeof(double) };
        private List<ResultCubeRow> _rows;
        private Dictionary<string, Type> _types;
        private List<string> _fieldNames;

        public void Initialize(Dictionary<string, Type> dataTypes)
        {
            _types = dataTypes;
            _fieldNames = dataTypes.Keys.ToList();
            _rows = new List<ResultCubeRow>();
        }

        public Dictionary<string, Type> DataTypes => _types;

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

        public void AddRow(object[] data, double value) => _rows.Add(new ResultCubeRow(data, value));


        public ResultCubeRow[] GetAllRows() => _rows.ToArray();

    }
}
