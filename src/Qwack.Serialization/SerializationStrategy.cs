using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Qwack.Serialization
{
    public class SerializationStrategy
    {
        private Type _objectType;
        public SerializationStrategy(Type objectType)
        {
            _objectType = objectType;
            var privateFields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in privateFields)
            {
                if (field.GetCustomAttribute(typeof(SkipSerializationAttribute)) != null) continue;
                var buffer = Expression.Parameter(typeof(Span<byte>).MakeByRefType(), "buffer");
                var objectForWork = Expression.Parameter(typeof(object), "obj");

                var block = GetExpressionForType(field, objectForWork, buffer);
                if (block != null)
                {
                    Expression ex = Expression.Lambda<Deserializer>(block, objectForWork, buffer);
                    var deser = Expression.Lambda<Deserializer>(block, objectForWork, buffer).Compile();
                    _serializers.Add(deser);
                }
            }
        }
                
        delegate void Deserializer(object objForWork, ref Span<byte> buffer);

        private List<Deserializer> _serializers = new List<Deserializer>();

        public void Serialize(object obj, ref Span<byte> buffer)
        {
            var origanlSpan = buffer;
            foreach (var ser in _serializers)
            {
                ser(obj, ref buffer);
            }
        }
        
        private Expression GetExpressionForType(FieldInfo field, ParameterExpression objForWork, ParameterExpression buffer)
        {
            Expression expression;
            if (field.FieldType == typeof(int)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteInt"), field, objForWork, buffer);
            else if (field.FieldType == typeof(uint)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteUInt"), field, objForWork, buffer);
            else if (field.FieldType == typeof(long)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteLong"), field, objForWork, buffer);
            else if (field.FieldType == typeof(ulong)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteULong"), field, objForWork, buffer);
            else if (field.FieldType == typeof(byte)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteByte"), field, objForWork, buffer);
            else if (field.FieldType == typeof(ushort)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteUShort"), field, objForWork, buffer);
            else if (field.FieldType == typeof(short)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteShort"), field, objForWork, buffer);
            else if (field.FieldType == typeof(double)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteDouble"), field, objForWork, buffer);
            else if (field.FieldType == typeof(float)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteFloat"), field, objForWork, buffer);
            else if (field.FieldType == typeof(bool)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteBool"), field, objForWork, buffer);
            else if (field.FieldType == typeof(DateTime)) expression = BuildExpression(typeof(SpanExtensions).GetMethod("WriteDateTime"), field, objForWork, buffer);
            else if (field.FieldType.Namespace.StartsWith("Qwack"))
            {
                expression = null;
                //throw new NotSupportedException(field.FieldType.Name);
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
                    return null;
                    throw new NotImplementedException("Func<,>");
                }
                else
                {
                    expression = null;
                    throw new NotSupportedException(field.FieldType.Name);
                }
            }
            else if (field.FieldType.IsArray)
            {
                var elementType = field.FieldType.GetElementType();
                if (elementType == typeof(ulong)) return WriteSimpleArray(field, objForWork, buffer, "WriteULong");
                else if (elementType == typeof(double)) return WriteSimpleArray(field, objForWork, buffer, "WriteDouble");
                else if (elementType == typeof(int)) return WriteSimpleArray(field, objForWork, buffer, "WriteInt");
                else if (elementType == typeof(DateTime)) return WriteSimpleArray(field, objForWork, buffer, "WriteDateTime");
                else
                {
                    throw new NotImplementedException($"Array of {elementType.Name} is not supported");
                }
            }
            else if (field.FieldType == typeof(string))
            {
                return null;
                throw new NotImplementedException();
            }
            else
            {
                expression = null;
                throw new NotSupportedException(field.FieldType.Name);
            }
            return expression;
        }

        private Expression WriteSimpleArray(FieldInfo field, ParameterExpression objForWork, ParameterExpression buffer, string writeMethod)
        {
            var convert = Expression.Convert(objForWork, _objectType);
            var fieldExp = Expression.Field(convert, field);
            var size = Expression.ArrayLength(fieldExp);
            var writeSize = Expression.Call(null, typeof(SpanExtensions).GetMethod("WriteInt"), buffer, size);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var label = Expression.Label();
            var arrayValue = Expression.ArrayAccess(fieldExp, index);
            var writeArrayValue = Expression.Call(null, typeof(SpanExtensions).GetMethod(writeMethod), buffer, arrayValue);
            var increment = Expression.Increment(index);
            var ifThenExit = Expression.IfThen(Expression.Equal(index, size), Expression.Break(label));
            var expressionLoop = Expression.Loop(Expression.Block(
              writeArrayValue,
              increment,
              ifThenExit
                ), label);

            var block = Expression.Block(new[] { index }, writeSize, assignZero, expressionLoop);
            return block;
        }

        private Expression BuildExpression(MethodInfo writeMethod, FieldInfo field, ParameterExpression objectForWork, ParameterExpression buffer)
        {
            var convert = Expression.Convert(objectForWork, _objectType);
            var f = Expression.Field(convert, field);
            var call = Expression.Call(null, writeMethod, buffer, f);
            var exp = Expression.Block(call);
            
            return exp;
        }
    }
}
