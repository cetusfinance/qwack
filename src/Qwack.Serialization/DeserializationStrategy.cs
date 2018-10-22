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
        private DeserializerDelegate _deserializer;

        private List<ParameterExpression> _parameters = new List<ParameterExpression>();
        private List<Expression> _expressions = new List<Expression>();

        public DeserializationStrategy(Type objectType)
        {
            _objectType = objectType;

            var buffer = Expression.Parameter(typeof(Span<byte>).MakeByRefType(), "buffer");
            var deserContext = Expression.Parameter(typeof(DeserializationContext), "deserializationContext");
            var objectForWork = Expression.Parameter(typeof(object), "obj");
            var objWorkConverted = Expression.Parameter(objectType);
            var convert = Expression.Convert(objectForWork, objectType);
            _parameters.Add(objWorkConverted);
            _expressions.Add(Expression.Assign(objWorkConverted, convert));

            SetupObject(objectType, objWorkConverted, buffer, deserContext);
            var finalBlock = Expression.Block(_parameters, _expressions);
            _deserializer = Expression.Lambda<DeserializerDelegate>(finalBlock, objectForWork, buffer, deserContext).Compile();
        }

        private void SetupObject(Type objectType, ParameterExpression source, ParameterExpression buffer, ParameterExpression deserializationContext)
        {
            var privateFields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in privateFields)
            {
                if (field.GetCustomAttribute(typeof(SkipSerializationAttribute)) != null) continue;

                var block = GetExpressionForType(field, source, buffer, deserializationContext);
                if (block != null)
                {
                    _expressions.Add(block);
                }
            }
        }

        delegate void DeserializerDelegate(object objectForWork, ref Span<byte> buffer, DeserializationContext context);

        private Expression GetExpressionForType(FieldInfo field, Expression objForWork, ParameterExpression buffer, ParameterExpression context)
        {
            var fieldExp = Expression.Field(objForWork, field);
            if (field.FieldType == typeof(int)) return BuildReadExpression("ReadInt", fieldExp, buffer);
            else if (field.FieldType == typeof(uint)) return BuildReadExpression("ReadUInt", fieldExp, buffer);
            else if (field.FieldType == typeof(long)) return BuildReadExpression("ReadLong", fieldExp, buffer);
            else if (field.FieldType == typeof(ulong)) return BuildReadExpression("ReadULong", fieldExp, buffer);
            else if (field.FieldType == typeof(ushort)) return BuildReadExpression("ReadUShort", fieldExp, buffer);
            else if (field.FieldType == typeof(short)) return BuildReadExpression("ReadShort", fieldExp, buffer);
            else if (field.FieldType == typeof(bool)) return BuildReadExpression("ReadBool", fieldExp, buffer);
            else if (field.FieldType == typeof(DateTime)) return BuildReadExpression("ReadDateTime", fieldExp, buffer);
            else if (field.FieldType == typeof(string)) return BuildReadExpression("ReadString", fieldExp, buffer);
            else if (field.FieldType == typeof(double)) return BuildReadExpression("ReadDouble", fieldExp, buffer);
            else if (field.FieldType == typeof(float)) return BuildReadExpression("ReadFloat", fieldExp, buffer);
            else if (field.FieldType.IsEnum)
            {
                var enumType = field.FieldType.GetEnumUnderlyingType();
                if (enumType == typeof(int)) return BuildReadExpression("ReadInt", fieldExp, buffer, field.FieldType);
                if (enumType == typeof(short)) return BuildReadExpression("ReadShort", fieldExp, buffer, field.FieldType);
                if (enumType == typeof(long)) return BuildReadExpression("ReadLong", fieldExp, buffer, field.FieldType);
                throw new NotImplementedException();
            }
            else if (field.FieldType.IsArray)
            {
                var elementType = field.FieldType.GetElementType();
                if (elementType == typeof(uint)) return ReadSimpleArray("ReadUInt", fieldExp, buffer, elementType);
                else if (elementType == typeof(int)) return ReadSimpleArray("ReadInt", fieldExp, buffer, elementType);
                else if (elementType == typeof(ulong)) return ReadSimpleArray("ReadULong", fieldExp, buffer, elementType);
                else if (elementType == typeof(double)) return ReadSimpleArray("ReadDouble", fieldExp, buffer, elementType);
                else if (elementType == typeof(DateTime)) return ReadSimpleArray("ReadDateTime", fieldExp, buffer, elementType);
                else if (elementType == typeof(bool)) return ReadSimpleArray("ReadBool", fieldExp, buffer, elementType);
                else if (elementType == typeof(byte)) return ReadSimpleArray("ReadByte", fieldExp, buffer, elementType);
                else if (elementType == typeof(ushort)) return ReadSimpleArray("ReadUShort", fieldExp, buffer, elementType);
                else if (elementType == typeof(short)) return ReadSimpleArray("ReadShort", fieldExp, buffer, elementType);
                else if (elementType == typeof(float)) return ReadSimpleArray("ReadFloat", fieldExp, buffer, elementType);
                else if (elementType == typeof(string)) return ReadSimpleArray("ReadString", fieldExp, buffer, elementType);
                else
                {
                    throw new Exception(elementType.Name);
                }
            }
            else if (field.FieldType.Namespace.StartsWith("Qwack"))
            {
                if (field.FieldType.IsValueType)
                {
                    var fieldParam = Expression.Parameter(field.FieldType);
                    var fieldParamAssign = Expression.Assign(fieldParam, fieldExp);
                    _parameters.Add(fieldParam);
                    _expressions.Add(fieldParamAssign);
                    SetupObject(field.FieldType, fieldParam, buffer, context);
                    _expressions.Add(Expression.Assign(fieldExp, fieldParam));
                    return null;
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
                    return null;
                    //throw new NotImplementedException("Dictionary");
                }
                else if (genType == typeof(HashSet<>))
                {
                    return null;
                    //throw new NotImplementedException("HashSet");
                }
                else if (genType == typeof(List<>))
                {
                    return null;
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
            _deserializer(instance, ref buffer, context);
            return instance;
        }
    }
}
