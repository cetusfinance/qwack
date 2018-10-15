using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Serialization
{
    public class DeserializationContext
    {
        private List<object> _currentObjects = new List<object>();

        public object GetObjectById(int objectId)
        {
            if (objectId == -1) return null;
            return _currentObjects[objectId - 1];
        }

        public int GetObjectId(object value)
        {
            if (value == null) return -1;
            return (_currentObjects.IndexOf(value) + 1);
        }

        public void AddObjectToContext(object objectToAdd) => _currentObjects.Add(objectToAdd);
    }
}
