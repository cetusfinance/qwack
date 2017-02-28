using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    public interface IObjectStore<T>
    {
        SessionItem<T> GetObject(string name);
        void PutObject(string name, SessionItem<T> obj);
        bool TryGetObject(string name, out SessionItem<T> obj);
        bool Exists(string name);
    }
}
