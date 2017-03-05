using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    public interface IObjectStore<T>
    {
        ISessionItem<T> GetObject(string name);
        void PutObject(string name, ISessionItem<T> obj);
        bool TryGetObject(string name, out ISessionItem<T> obj);
        bool Exists(string name);
    }
}
