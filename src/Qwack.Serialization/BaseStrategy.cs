using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Qwack.Serialization
{
    public abstract class BaseStrategy
    {
        protected readonly Type _objectType;
        protected readonly SerializerDelegate _serializer;
        protected List<ParameterExpression> _parameters = new();
        protected Dictionary<Type, string> _methodMapping = new()
        {
            [typeof(int)] = "Int",
            [typeof(uint)] = "UInt",
            [typeof(short)] = "Short",
            [typeof(ushort)] = "UShort",
            [typeof(byte)] = "Byte",
            [typeof(double)] = "Double",
            [typeof(float)] = "Float",
            [typeof(long)] = "Long",
            [typeof(ulong)] = "ULong",
            [typeof(bool)] = "Bool",
            [typeof(string)] = "String",
            [typeof(DateTime)] = "DateTime",
        };

        protected delegate void SerializerDelegate(object objectForWork, ref Span<byte> buffer, DeserializationContext context);

        public BaseStrategy(Type objectType)
        {
            _objectType = objectType;

            var buffer = Expression.Parameter(typeof(Span<byte>).MakeByRefType(), "buffer");
            var deserContext = Expression.Parameter(typeof(DeserializationContext), "deserializationContext");
            var objectForWork = Expression.Parameter(typeof(object), "obj");
            var objWorkConverted = Expression.Parameter(objectType);
            var convert = Expression.Convert(objectForWork, objectType);
            _parameters.Add(objWorkConverted);
            var exp = new List<Expression> { Expression.Assign(objWorkConverted, convert) };

            exp.AddRange(SetupObject(objectType, objWorkConverted, buffer, deserContext));
            var finalBlock = Expression.Block(_parameters, exp);
            _serializer = Expression.Lambda<SerializerDelegate>(finalBlock, objectForWork, buffer, deserContext).Compile();
        }

        public string FullName => _objectType.AssemblyQualifiedName;

        private IEnumerable<Expression> SetupObject(Type objectType, ParameterExpression source, ParameterExpression buffer, ParameterExpression deserializationContext)
        {
            var privateFields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var expressions = new List<Expression>();
            foreach (var field in privateFields)
            {
                if (field.GetCustomAttribute(typeof(SkipSerializationAttribute)) != null) continue;
                var fieldExp = Expression.Field(source, field);
                            
                var block = GetExpressionForType(fieldExp, buffer, deserializationContext);
                if (block != null)
                {
                    expressions.Add(block);
                }
            }
            return expressions;
        }

        protected Expression GetExpressionForType(Expression fieldExp, ParameterExpression buffer, ParameterExpression context)
        {
            if (_methodMapping.ContainsKey(fieldExp.Type)) return BuildExpression(fieldExp.Type, fieldExp, buffer);
            else if (fieldExp.Type.IsEnum)
            {
                var enumType = fieldExp.Type.GetEnumUnderlyingType();
                if (_methodMapping.ContainsKey(enumType)) return BuildExpression(enumType, fieldExp, buffer, fieldExp.Type);
                throw new NotSupportedException($"Unsupported enum of type{fieldExp.Type}");
            }
            else if (fieldExp.Type.IsArray)
            {
                var elementType = fieldExp.Type.GetElementType();
                if (elementType.IsArray)
                {
                    var subElementType = elementType.GetElementType();
                    if (_methodMapping.ContainsKey(subElementType)) return BuildJaggedArrayExpression(fieldExp, buffer, elementType, context);
                    throw new NotSupportedException(subElementType.Name);
                }
                else
                {
                    if (_methodMapping.ContainsKey(elementType)) return BuildSimpleArrayExpression(fieldExp, buffer, elementType, context);
                    throw new NotSupportedException(elementType.Name);
                }
            }
            else if (fieldExp.Type.Namespace.StartsWith("Qwack"))
            {
                if (fieldExp.Type.IsValueType)
                {
                    var fieldParam = Expression.Parameter(fieldExp.Type);
                    var fieldParamAssign = Expression.Assign(fieldParam, fieldExp);
                    _parameters.Add(fieldParam);
                    var exps = new List<Expression> { fieldParamAssign };
                    exps.AddRange(SetupObject(fieldExp.Type, fieldParam, buffer, context));
                    exps.Add(Expression.Assign(fieldExp, fieldParam));
                    var block = Expression.Block(exps);
                    return block;
                }
                else
                {
                    return BuildQwackExpression(buffer, context, fieldExp);
                }
            }
            else if (fieldExp.Type.IsGenericType)
            {
                var genType = fieldExp.Type.GetGenericTypeDefinition();
                if (genType == typeof(Dictionary<,>))
                {
                    var genKey = fieldExp.Type.GenericTypeArguments[0];
                    var genValue = fieldExp.Type.GenericTypeArguments[1];
                    if (_methodMapping.ContainsKey(genKey) && _methodMapping.ContainsKey(genValue)) return BuildSimpleDictionaryExpression(fieldExp, buffer, genKey, genValue, context);
                    throw new NotImplementedException("Dictionary");
                }
                else if (genType == typeof(HashSet<>) || genType == typeof(List<>))
                {
                    var genArgument = fieldExp.Type.GenericTypeArguments[0];
                    return BuildSimpleHashsetOrListExpression(fieldExp, buffer, genArgument, context);
                }
                else
                {
                    throw new NotSupportedException(fieldExp.Type.Name);
                }
            }
            else
            {
                throw new NotImplementedException(fieldExp.Type.Name);
            }
        }

        protected static MethodInfo GetSimpleMethod(string methodName) => typeof(SpanExtensions).GetMethod(methodName);

        protected abstract Expression BuildQwackExpression(ParameterExpression buffer, ParameterExpression context, Expression fieldExp);
        protected abstract Expression BuildSimpleArrayExpression(Expression field, ParameterExpression buffer, Type elementType, ParameterExpression context);
        protected abstract Expression BuildSimpleDictionaryExpression(Expression field, ParameterExpression buffer, Type keyType, Type valueType, ParameterExpression context);
        protected abstract Expression BuildSimpleHashsetOrListExpression(Expression field, ParameterExpression buffer, Type elementType, ParameterExpression context);
        protected abstract Expression BuildExpression(Type fieldType, Expression field, ParameterExpression buffer, Type convertType = null);
        protected abstract Expression BuildJaggedArrayExpression(Expression field, ParameterExpression buffer, Type elementType, ParameterExpression context);
    }
}
