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
            SetupObject(objectType, _deserializers);
        }

        private static void SetupObject(Type objectType, List<DeserializerDelegate> deserializers)
        {
            var privateFields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in privateFields)
            {
                if (field.GetCustomAttribute(typeof(SkipSerializationAttribute)) != null) continue;
                var buffer = Expression.Parameter(typeof(Span<byte>).MakeByRefType(), "buffer");
                var deserContext = Expression.Parameter(typeof(DeserializationContext), "deserializationContext");
                var objectForWork = Expression.Parameter(typeof(object), "obj");
                var convert = Expression.Convert(objectForWork, objectType);
                var fieldSource = Expression.Field(convert, field);
                var block = GetExpressionForType(field.FieldType, fieldSource, buffer, deserContext, deserializers);
                if (block != null)
                {
                    var deser = Expression.Lambda<DeserializerDelegate>(block, objectForWork, buffer, deserContext).Compile();
                    deserializers.Add(deser);
                }
            }
        }

        delegate void DeserializerDelegate(object objectForWork, ref Span<byte> buffer, DeserializationContext context);

        private static Expression GetExpressionForType(Type type, Expression source, ParameterExpression buffer, ParameterExpression context, List<DeserializerDelegate> deserializers)
        {
            Expression expression;

            if (type == typeof(int)) return BuildReadExpression("ReadInt", source, buffer);
            else if (type == typeof(uint)) return BuildReadExpression("ReadUInt", source, buffer);
            else if (type == typeof(long)) return BuildReadExpression("ReadLong", source, buffer);
            else if (type == typeof(ulong)) return BuildReadExpression("ReadULong", source, buffer);
            else if (type == typeof(ushort)) return BuildReadExpression("ReadUShort", source, buffer);
            else if (type == typeof(short)) return BuildReadExpression("ReadShort", source, buffer);
            else if (type == typeof(bool)) return BuildReadExpression("ReadBool", source, buffer);
            else if (type == typeof(DateTime)) return BuildReadExpression("ReadDateTime", source, buffer);
            else if (type == typeof(string)) return BuildReadExpression("ReadString", source, buffer);
            else if (type == typeof(double)) return BuildReadExpression("ReadDouble", source, buffer);
            else if (type == typeof(float)) return BuildReadExpression("ReadFloat", source, buffer);
            else if (type.IsEnum)
            {
                var enumType = type.GetEnumUnderlyingType();
                if (enumType == typeof(int)) return BuildReadExpression("ReadInt", source, buffer, type);
                if (enumType == typeof(short)) return BuildReadExpression("ReadShort", source, buffer, type);
                if (enumType == typeof(long)) return BuildReadExpression("ReadLong", source, buffer, type);
                throw new NotImplementedException();
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == typeof(uint)) return ReadSimpleArray("ReadUInt", source, buffer, elementType);
                else if (elementType == typeof(int)) return ReadSimpleArray("ReadInt", source, buffer, elementType);
                else if (elementType == typeof(ulong)) return ReadSimpleArray("ReadULong", source, buffer, elementType);
                else if (elementType == typeof(double)) return ReadSimpleArray("ReadDouble", source, buffer, elementType);
                else if (elementType == typeof(DateTime)) return ReadSimpleArray("ReadDateTime", source, buffer, elementType);
                else if (elementType == typeof(bool)) return ReadSimpleArray("ReadBool", source, buffer, elementType);
                else if (elementType == typeof(byte)) return ReadSimpleArray("ReadByte", source, buffer, elementType);
                else if (elementType == typeof(ushort)) return ReadSimpleArray("ReadUShort", source, buffer, elementType);
                else if (elementType == typeof(short)) return ReadSimpleArray("ReadShort", source, buffer, elementType);
                else if (elementType == typeof(float)) return ReadSimpleArray("ReadFloat", source, buffer, elementType);
                else if (elementType == typeof(string)) return ReadSimpleArray("ReadString", source, buffer, elementType);
                else
                {
                    throw new Exception(elementType.Name);
                }
            }
            else if (type.Namespace.StartsWith("Qwack"))
            {
                if (type.IsValueType)
                {
                    SetupObject(type, deserializers);
                    expression = null;
                }
                else
                {
                    var idValue = Expression.Call(null, GetReadMethod("ReadInt"), buffer);
                    var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectById"), idValue);
                    var assignObject = Expression.Assign(source, Expression.Convert(lookupId, type));
                    return assignObject;
                }
            }
            else if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
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
                    throw new NotSupportedException(type.Name);
                }
            }
            else
            {
                throw new NotImplementedException(type.Name);
            }
            return expression;
        }

        private static MethodInfo GetReadMethod(string methodName) => typeof(SpanExtensions).GetMethod(methodName);

        private static Expression ReadSimpleArray(string readMethod, Expression field, ParameterExpression buffer, Type elementType)
        {
            var length = Expression.Parameter(typeof(int));
            var getlength = BuildReadExpression("ReadInt", length, buffer);
            var array = Expression.Parameter(elementType.MakeArrayType());
            var createAnArray = Expression.NewArrayBounds(elementType, length);
            var assignArray = Expression.Assign(array, createAnArray);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var increment = Expression.AddAssign(index, Expression.Constant(1));
            var arrayValue = Expression.ArrayAccess(array, index);
            var label = Expression.Label();
            var ifThenExit = Expression.IfThen(Expression.Equal(index, length), Expression.Break(label));
            var readArrayValue = BuildReadExpression(readMethod, arrayValue, buffer);
            var expressionLoop = Expression.Loop(Expression.Block(ifThenExit, readArrayValue, increment), label);
            var finalAssignArray = Expression.Assign(field, array);

            var ifNull = Expression.IfThen(Expression.NotEqual(length, Expression.Constant(-1)),
                Expression.Block(assignArray, assignZero, expressionLoop, finalAssignArray));

            var returnBlock = Expression.Block(new[] { length, array, index },
                getlength, ifNull);

            return returnBlock;
        }

        private static Expression BuildReadExpression(string methodName, Expression field, ParameterExpression buffer, Type convertType = null)
        {
            var method = GetReadMethod(methodName);
            var readValue = Expression.Call(null, method, buffer);
            if (convertType == null)
            {
                return Expression.Assign(field, readValue);
            }
            else
            {
                return Expression.Assign(field, Expression.Convert(readValue, convertType));
            }
        }

        public object Deserialize(ref Span<byte> buffer, DeserializationContext context)
        {
            var instance = Activator.CreateInstance(_objectType);
            foreach (var exp in _deserializers)
            {
                exp(instance, ref buffer, context);
            }
            return instance;
        }
    }
}
