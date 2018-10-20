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
        private List<SerializerDelegate> _serializers = new List<SerializerDelegate>();

        public SerializationStrategy(Type objectType)
        {
            _objectType = objectType;
            SetupObject(objectType, _serializers);
        }

        public string FullName => _objectType.AssemblyQualifiedName;

        delegate void SerializerDelegate(object objForWork, ref Span<byte> buffer, DeserializationContext context);

        private static void SetupObject(Type objectType, List<SerializerDelegate> serializers)
        {
            var privateFields = objectType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in privateFields)
            {
                if (field.GetCustomAttribute(typeof(SkipSerializationAttribute)) != null) continue;
                var buffer = Expression.Parameter(typeof(Span<byte>).MakeByRefType(), "buffer");
                var deserContext = Expression.Parameter(typeof(DeserializationContext), "deserializationContext");
                var objectForWork = Expression.Parameter(typeof(object), "obj");
                var convert = Expression.Convert(objectForWork, objectType);

                var block = GetExpressionForType(field, convert, buffer, deserContext, serializers);
                if (block != null)
                {
                    var deser = Expression.Lambda<SerializerDelegate>(block, objectForWork, buffer, deserContext).Compile();
                    serializers.Add(deser);
                }
            }
        }

        public void Serialize(object obj, ref Span<byte> buffer, DeserializationContext context)
        {
            var origanlSpan = buffer;
            foreach (var ser in _serializers)
            {
                ser(obj, ref buffer, context);
            }
            Console.WriteLine($"Total Bytes Written {origanlSpan.Length - buffer.Length}");
        }

        private static MethodInfo GetWriteMethod(string methodName) => typeof(SpanExtensions).GetMethod(methodName);

        private static Expression GetExpressionForType(FieldInfo field, Expression objForWork, ParameterExpression buffer, ParameterExpression context, List<SerializerDelegate> serializers)
        {
            var fieldExp = Expression.Field(objForWork, field);
            Expression expression;
            if (field.FieldType == typeof(int)) expression = BuildExpression(GetWriteMethod("WriteInt"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(uint)) expression = BuildExpression(GetWriteMethod("WriteUInt"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(long)) expression = BuildExpression(GetWriteMethod("WriteLong"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(ulong)) expression = BuildExpression(GetWriteMethod("WriteULong"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(byte)) expression = BuildExpression(GetWriteMethod("WriteByte"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(ushort)) expression = BuildExpression(GetWriteMethod("WriteUShort"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(short)) expression = BuildExpression(GetWriteMethod("WriteShort"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(double)) expression = BuildExpression(GetWriteMethod("WriteDouble"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(float)) expression = BuildExpression(GetWriteMethod("WriteFloat"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(bool)) expression = BuildExpression(GetWriteMethod("WriteBool"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(DateTime)) expression = BuildExpression(GetWriteMethod("WriteDateTime"), fieldExp, objForWork, buffer);
            else if (field.FieldType == typeof(string)) expression = BuildExpression(GetWriteMethod("WriteString"), fieldExp, objForWork, buffer);
            else if (field.FieldType.IsEnum)
            {
                var enumType = field.FieldType.GetEnumUnderlyingType();
                var fieldExpConverted = Expression.Convert(fieldExp, enumType);
                if (enumType == typeof(short)) return BuildExpression(GetWriteMethod("WriteShort"), fieldExpConverted , objForWork, buffer);
                else if (enumType == typeof(long)) return BuildExpression(GetWriteMethod("WriteLong"), fieldExpConverted, objForWork, buffer);
                else if (enumType == typeof(int)) return BuildExpression(GetWriteMethod("WriteInt"), fieldExpConverted, objForWork, buffer);
                throw new NotImplementedException($"Enum type of {enumType.Name} not supported");
            }
            else if (field.FieldType.Namespace.StartsWith("Qwack"))
            {
                if (field.FieldType.IsValueType)
                {
                    SetupObject(field.FieldType, serializers);
                    expression = null;
                }
                else
                {
                    var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectId"), fieldExp);
                    var writeInt = Expression.Call(null, GetWriteMethod("WriteInt"), buffer, lookupId);
                    return writeInt;
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
            else if (field.FieldType.IsArray)
            {
                var elementType = field.FieldType.GetElementType();
                if (elementType == typeof(ulong)) return WriteSimpleArray(field, objForWork, buffer, "WriteULong");
                else if (elementType == typeof(double)) return WriteSimpleArray(field, objForWork, buffer, "WriteDouble");
                else if (elementType == typeof(int)) return WriteSimpleArray(field, objForWork, buffer, "WriteInt");
                else if (elementType == typeof(uint)) return WriteSimpleArray(field, objForWork, buffer, "WriteUInt");
                else if (elementType == typeof(DateTime)) return WriteSimpleArray(field, objForWork, buffer, "WriteDateTime");
                else if (elementType == typeof(bool)) return WriteSimpleArray(field, objForWork, buffer, "WriteBool");
                else if (elementType == typeof(byte)) return WriteSimpleArray(field, objForWork, buffer, "WriteByte");
                else if (elementType == typeof(short)) return WriteSimpleArray(field, objForWork, buffer, "WriteShort");
                else if (elementType == typeof(ushort)) return WriteSimpleArray(field, objForWork, buffer, "WriteUShort");
                else if (elementType == typeof(float)) return WriteSimpleArray(field, objForWork, buffer, "WriteFloat");
                else if (elementType == typeof(string)) return WriteSimpleArray(field, objForWork, buffer, "WriteString");
                else
                {
                    throw new NotImplementedException($"Array of {elementType.Name} is not supported");
                }
            }
            else
            {
                throw new NotSupportedException(field.FieldType.Name);
            }
            return expression;
        }

        private static Expression WriteSimpleArray(FieldInfo field, Expression objForWork, ParameterExpression buffer, string writeMethod)
        {
            var fieldExp = Expression.Field(objForWork, field);
            var size = Expression.ArrayLength(fieldExp);
            var compareToNull = Expression.Equal(fieldExp, Expression.Default(field.FieldType));
            var writeNull = Expression.Call(null,GetWriteMethod("WriteInt"), buffer, Expression.Constant(-1));

            var writeSize = Expression.Call(null, GetWriteMethod("WriteInt"), buffer, size);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var label = Expression.Label();
            var arrayValue = Expression.ArrayAccess(fieldExp, index);
            var writeArrayValue = Expression.Call(null, GetWriteMethod(writeMethod), buffer, arrayValue);
            var increment = Expression.AddAssign(index, Expression.Constant(1));
            var ifThenExit = Expression.IfThen(Expression.Equal(index, size), Expression.Break(label));
            var expressionLoop = Expression.Loop(
                Expression.Block(ifThenExit, writeArrayValue, increment), label);

            var block = Expression.Block(new[] { index }, writeSize, assignZero, expressionLoop);
            var compareIf = Expression.IfThenElse(compareToNull, writeNull, block);
            return compareIf;
        }

        private static Expression BuildExpression(MethodInfo writeMethod, Expression field, Expression objectForWork, ParameterExpression buffer)
        {
            var call = Expression.Call(null, writeMethod, buffer, field);
            return call;
        }
    }
}
