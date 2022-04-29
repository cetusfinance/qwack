using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Transport.TransportObjects.Cubes;

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

        public ResultCubeRow(TO_ResultCubeRow transportObject, Type[] types)
        {
            MetaData = transportObject.MetaData.Select((x, ix) => Convert.ChangeType(x, types[ix])).ToArray();
            Value = transportObject.Value;
        }

        public Dictionary<string, object> ToDictionary(string[] fieldNames)
        {
            if (fieldNames.Length != MetaData.Length)
                throw new DataMisalignedException();

            return fieldNames
                .Select((x, ix) => new KeyValuePair<string, object>(x, MetaData[ix]))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public TO_ResultCubeRow ToTransportObject() =>
            new()
            {
                MetaData = MetaData.Select(x => (string)Convert.ChangeType(x, typeof(string))).ToArray(),
                Value = Value
            };
    }

    public class ResultCube : ICube
    {
        private readonly object _locker = new();

        private readonly Type[] _numericalTypes = { typeof(double) };
        private List<ResultCubeRow> _rows;
        private Dictionary<string, Type> _types;
        private List<string> _fieldNames;

        public ResultCube() { }

        public ResultCube(TO_ResultCube transportObject)
        {
            _fieldNames = transportObject.FieldNames;
            _types = transportObject.Types.ToDictionary(x => x.Key, x => Type.GetType(x.Value));
            var types = _fieldNames.Select(x => _types[x]).ToArray();
            _rows = transportObject.Rows.Select(x => new ResultCubeRow(x, types)).ToList();
        }

        public void Initialize(Dictionary<string, Type> dataTypes)
        {
            _types = new Dictionary<string, Type>(dataTypes);
            _fieldNames = dataTypes.Keys.ToList();
            _rows = new List<ResultCubeRow>();
        }

        public Dictionary<string, Type> DataTypes => _types;

        public double SumOfAllRows => _rows.Sum(x => x.Value);

        public int GetColumnIndex(string columnName) => _fieldNames.IndexOf(columnName);

        public void AddRow(Dictionary<string, object> data, double value)
        {
            var row = new object[_fieldNames.Count];
            foreach (var d in data)
            {
                if (!_types.ContainsKey(d.Key))
                    throw new Exception($"Could not map field {d.Key}");

                if (d.Value == null && _types[d.Key] == typeof(string))
                {

                }
                else
                {
                    var valAsType = Convert.ChangeType(d.Value, _types[d.Key]);
                    if (valAsType == null)
                        throw new Exception($"Could not convert field {d.Key} value {d.Value} as type {_types[d.Key]}");
                }

                var ix = _fieldNames.IndexOf(d.Key);
                row[ix] = d.Value;
            }
            lock (_locker)
            {
                _rows.Add(new ResultCubeRow(row, value));
            }
        }

        bool IsOfNullableType<T>(T o)
        {
            var type = typeof(T);
            return Nullable.GetUnderlyingType(type) != null;
        }

        public void AddRow(object[] data, double value)
        {
            lock (_locker)
            {
                _rows.Add(new ResultCubeRow(data, value));
            }
        }


        public ResultCubeRow[] GetAllRows()
        {
            lock (_locker)
            {
                return _rows.ToArray();
            }
        }

        public TO_ResultCube ToTransportObject() =>
            new()
            {
                FieldNames = _fieldNames,
                Rows = _rows.Select(x => x.ToTransportObject()).ToList(),
                Types = _types.ToDictionary(x => x.Key, x => x.Value.AssemblyQualifiedName)
            };
    }
}
