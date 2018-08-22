using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Cubes
{
    public interface ICube
    {
        Dictionary<string, Type> DataTypes { get; }
        void Initialize(Dictionary<string, Type> dataTypes);
        void AddRow(Dictionary<string, object> data);
        Dictionary<string, object>[] GetAllRows();
        ICube Pivot(string fieldToAggregateBy, Dictionary<string, AggregationAction> aggregatedFields);

    }
}
