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

            foreach (var row in _rows)
            {

            }

            throw new NotImplementedException();
        }

        public Dictionary<string, object>[] GetAllRows()
        {
            return _rows.ToArray();
        }

        
    }
}
