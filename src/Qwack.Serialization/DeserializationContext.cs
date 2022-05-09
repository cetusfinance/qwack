using System.Collections.Generic;

namespace Qwack.Serialization
{
    public class DeserializationContext
    {
        private readonly List<object> _currentObjects = new();

        public object GetObjectById(int objectId)
        {
            if (objectId == -1) return null;
            return _currentObjects[objectId];
        }

        public int GetObjectId(object value)
        {
            if (value == null) return -1;
            return (_currentObjects.IndexOf(value) + 1);
        }

        public void AddObjectToContext(object objectToAdd) => _currentObjects.Add(objectToAdd);
    }
}
