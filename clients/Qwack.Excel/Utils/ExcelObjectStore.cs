using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    public class ExcelObjectStore <T> : IObjectStore<T>
    {
        private readonly ConcurrentDictionary<string, ISessionItem<T>> _store;

        public ExcelObjectStore()
        {
            _store = new ConcurrentDictionary<string, ISessionItem<T>>();
        }

        public bool Exists(string name)
        {
            return _store.ContainsKey(name);
        }

        public ISessionItem<T> GetObject(string name)
        {
            return _store[name];
        }

        public void PutObject(string name, ISessionItem<T> obj)
        {
            _store.AddOrUpdate(name, obj, (n, o) =>
            {
                if (o == obj)
                    obj.Version++;
                else
                    obj.Version = 1;
                return obj;
            });
        }

        public bool TryGetObject(string name, out ISessionItem<T> obj)
        {
            return _store.TryGetValue(name, out obj);
        }
    }
}
