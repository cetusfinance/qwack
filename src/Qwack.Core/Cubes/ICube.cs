using System;
using System.Collections.Generic;

namespace Qwack.Core.Cubes
{
    public interface ICube
    {
        Dictionary<string, Type> DataTypes { get; }
        void Initialize(Dictionary<string, Type> dataTypes);
        void AddRow(Dictionary<string, object> descriptiveData, double value);
        ResultCubeRow[] GetAllRows();
        double SumOfAllRows { get; }
        int GetColumnIndex(string columnName);
    }
}
