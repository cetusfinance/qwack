using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Qwack.Serialization
{
    public class DeserializationStrategy
    {
        private Type _objectType;
        private List<DeserializerDelegate> _deserializers = new List<DeserializerDelegate>();

        public DeserializationStrategy(Type objectType)
        {
            _objectType = objectType;
            var privateFields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in privateFields)
            {
                if (field.GetCustomAttribute(typeof(SkipSerializationAttribute)) != null) continue;
                var buffer = Expression.Parameter(typeof(Span<byte>).MakeByRefType(), "buffer");
                var deserContext = Expression.Parameter(typeof(DeserializationContext), "deserializationContext");
                var objectForWork = Expression.Parameter(typeof(object), "obj");
                var convert = Expression.Convert(objectForWork, _objectType);

                var block = GetExpressionForType(field, convert, buffer, deserContext);
                if (block != null)
                {
                    var deser = Expression.Lambda<DeserializerDelegate>(block, objectForWork, buffer, deserContext).Compile();
                    _deserializers.Add(deser);
                }
            }
        }

        delegate void DeserializerDelegate(object objectForWork, ref Span<byte> buffer, DeserializationContext context);

        private Expression GetExpressionForType(FieldInfo field, Expression objForWork, ParameterExpression buffer, ParameterExpression context)
        {
            var fieldExp = Expression.Field(objForWork, field);
            Expression expression;

            if (field.FieldType == typeof(int)) return BuildReadExpression("ReadInt", fieldExp, buffer);
            else if (field.FieldType == typeof(uint)) return BuildReadExpression("ReadUInt", fieldExp, buffer);
            else if (field.FieldType == typeof(bool)) return BuildReadExpression("ReadBool", fieldExp, buffer);
            else if (field.FieldType == typeof(DateTime)) return BuildReadExpression("ReadDateTime", fieldExp, buffer);
            else if (field.FieldType == typeof(string)) return BuildReadExpression("ReadString", fieldExp, buffer);
            else if (field.FieldType == typeof(double)) return BuildReadExpression("ReadDouble", fieldExp, buffer);
            else if (field.FieldType.IsArray)
            {
                var elementType = field.FieldType.GetElementType();
                if (elementType == typeof(uint)) return ReadSimpleArray("ReadUInt", fieldExp, buffer, elementType);
                else if (elementType == typeof(int)) return ReadSimpleArray("ReadInt", fieldExp, buffer, elementType);
                else if (elementType == typeof(ulong)) return ReadSimpleArray("ReadULong", fieldExp, buffer, elementType);
                else if (elementType == typeof(double)) return ReadSimpleArray("ReadDouble", fieldExp, buffer, elementType);
                else if (elementType == typeof(DateTime)) return ReadSimpleArray("ReadDateTime", fieldExp, buffer, elementType);
                else
                {
                    throw new Exception();
                }
            }
            else if (field.FieldType.Namespace.StartsWith("Qwack"))
            {
                if (field.FieldType.IsValueType)
                {
                    expression = null;
                    //throw new NotImplementedException();
                }
                else
                {
                    var idValue = Expression.Call(null, GetReadMethod("ReadInt"), buffer);
                    var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectById"), idValue);
                    var assignObject = Expression.Assign(fieldExp, Expression.Convert(lookupId, field.FieldType));
                    return assignObject;
                }
            }
            else if (field.FieldType.IsGenericType)
            {
                var genType = field.FieldType.GetGenericTypeDefinition();
                if (genType == typeof(Dictionary<,>))
                {
                    expression = null;
                    //throw new NotImplementedException("Dictionary");
                }
                else if (genType == typeof(HashSet<>))
                {
                    expression = null;
                    //throw new NotImplementedException("HashSet");
                }
                else if (genType == typeof(List<>))
                {
                    expression = null;
                    //throw new NotImplementedException("List");
                }
                else if (genType == typeof(Func<,>))
                {
                    throw new NotImplementedException("Func<,>");
                }
                else
                {
                    throw new NotSupportedException(field.FieldType.Name);
                }
            }
            else
            {
                throw new NotImplementedException(field.FieldType.Name);
            }
            return expression;
        }

        private MethodInfo GetReadMethod(string methodName) => typeof(SpanExtensions).GetMethod(methodName);

        private Expression ReadSimpleArray(string readMethod, Expression field, ParameterExpression buffer, Type elementType)
        {
            var length = Expression.Parameter(typeof(int));
            var getlength = BuildReadExpression("ReadInt", length, buffer);

            var createAnArray = Expression.NewArrayBounds(elementType, length);
            var ifNull = Expression.IfThen(Expression.NotEqual(length, Expression.Constant(-1)),
                createAnArray
                );


            var returnBlock = Expression.Block(new[] { length },
                getlength,
                ifNull
                );

            return returnBlock;
        }

        private Expression BuildReadExpression(string methodName, Expression field, ParameterExpression buffer)
        {
            var method = GetReadMethod(methodName);
            var readValue = Expression.Call(null, method, buffer);
            var assignObject = Expression.Assign(field, readValue);
            return assignObject;
        }

        public object Deserialize(ref Span<byte> buffer, DeserializationContext context)
        {
            var instance = Activator.CreateInstance(_objectType);
            throw new NotImplementedException();
        }
    }
}
