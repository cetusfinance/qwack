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
            return _store.ContainsKey(name.Split('¬')[0]);
        }

        public ISessionItem<T> GetObject(string name)
        {
            return _store[name.Split('¬')[0]];
        }

        public void PutObject(string name, ISessionItem<T> obj)
        {
            _store.AddOrUpdate(name.Split('¬')[0], obj, (n, o) =>
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
            var cleanName = name.Split('¬')[0];
            return _store.TryGetValue(cleanName, out obj);
        }
    }
}
