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
        protected List<ParameterExpression> _parameters = new List<ParameterExpression>();
        protected Dictionary<Type, string> _methodMapping = new Dictionary<Type, string>
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

                var block = GetExpressionForType(field, source, buffer, deserializationContext);
                if (block != null)
                {
                    expressions.Add(block);
                }
            }
            return expressions;
        }

        protected Expression GetExpressionForType(FieldInfo field, Expression objForWork, ParameterExpression buffer, ParameterExpression context)
        {
            var fieldExp = Expression.Field(objForWork, field);
            if (_methodMapping.ContainsKey(field.FieldType)) return BuildExpression(field.FieldType, fieldExp, buffer);
            else if (field.FieldType.IsEnum)
            {
                var enumType = field.FieldType.GetEnumUnderlyingType();
                if (_methodMapping.ContainsKey(enumType)) return BuildExpression(enumType, fieldExp, buffer, field.FieldType);
                throw new NotSupportedException($"Unsupported enum of type{field.FieldType}");
            }
            else if (field.FieldType.IsArray)
            {
                var elementType = field.FieldType.GetElementType();
                if (_methodMapping.ContainsKey(elementType)) return BuildSimpleArrayExpression(fieldExp, buffer, elementType);
                throw new NotSupportedException(elementType.Name);
            }
            else if (field.FieldType.Namespace.StartsWith("Qwack"))
            {
                if (field.FieldType.IsValueType)
                {
                    var fieldParam = Expression.Parameter(field.FieldType);
                    var fieldParamAssign = Expression.Assign(fieldParam, fieldExp);
                    _parameters.Add(fieldParam);
                    var exps = new List<Expression> { fieldParamAssign };
                    exps.AddRange(SetupObject(field.FieldType, fieldParam, buffer, context));
                    exps.Add(Expression.Assign(fieldExp, fieldParam));
                    var block = Expression.Block(exps);
                    return block;
                }
                else
                {
                    return BuildQwackExpression(field, buffer, context, fieldExp);
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
                    var genArgument = field.FieldType.GenericTypeArguments[0];
                    if (_methodMapping.ContainsKey(genArgument)) return BuildSimpleHashsetExpression(fieldExp, buffer, genArgument);
                    throw new NotImplementedException("HashSet");
                }
                else if (genType == typeof(List<>))
                {
                    var genArgument = field.FieldType.GenericTypeArguments[0];
                    if (_methodMapping.ContainsKey(genArgument)) return BuildSimpleListExpression(fieldExp, buffer, genArgument);
                    throw new NotImplementedException($"List of type {genArgument}");
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

        protected static MethodInfo GetSimpleMethod(string methodName) => typeof(SpanExtensions).GetMethod(methodName);

        protected abstract Expression BuildQwackExpression(FieldInfo field, ParameterExpression buffer, ParameterExpression context, MemberExpression fieldExp);
        protected abstract Expression BuildSimpleArrayExpression(Expression field, ParameterExpression buffer, Type elementType);
        protected abstract Expression BuildSimpleListExpression(Expression field, ParameterExpression buffer, Type elementType);
        protected abstract Expression BuildSimpleHashsetExpression(Expression field, ParameterExpression buffer, Type elementType);
        protected abstract Expression BuildExpression(Type fieldType, Expression field, ParameterExpression buffer, Type convertType = null);
    }
}
