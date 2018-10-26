using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Qwack.Serialization
{
    public class SerializationStrategy : BaseStrategy
    {
        public SerializationStrategy(Type objectType) : base(objectType) { }

        public void Serialize(object obj, ref Span<byte> buffer, DeserializationContext context)
        {
            var origanlSpan = buffer;
            _serializer(obj, ref buffer, context);
        }

        protected override Expression BuildSimpleArrayExpression(Expression fieldExp, ParameterExpression buffer, Type elementType, ParameterExpression context)
        {
            var size = Expression.ArrayLength(fieldExp);
            var compareToNull = Expression.Equal(fieldExp, Expression.Default(elementType.MakeArrayType()));
            var writeNull = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, Expression.Constant(-1));

            var writeSize = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, size);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var label = Expression.Label();
            var arrayValue = Expression.ArrayAccess(fieldExp, index);
            var writeArrayValue = Expression.Call(null, GetSimpleMethod($"Write{_methodMapping[elementType]}"), buffer, arrayValue);
            var increment = Expression.AddAssign(index, Expression.Constant(1));
            var ifThenExit = Expression.IfThen(Expression.Equal(index, size), Expression.Break(label));
            var expressionLoop = Expression.Loop(
                Expression.Block(ifThenExit, writeArrayValue, increment), label);

            var block = Expression.Block(new[] { index }, writeSize, assignZero, expressionLoop);
            var compareIf = Expression.IfThenElse(compareToNull, writeNull, block);
            return compareIf;
        }

        protected override Expression BuildExpression(Type fieldType, Expression field, ParameterExpression buffer, Type convertType = null)
        {
            var writeMethod = $"Write{_methodMapping[fieldType]}";
            if (field.Type != fieldType)
            {
                field = Expression.Convert(field, fieldType);
            }
            var call = Expression.Call(null, GetSimpleMethod(writeMethod), buffer, field);
            return call;
        }

        protected override Expression BuildQwackExpression(ParameterExpression buffer, ParameterExpression context, Expression fieldExp)
        {
            var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectId"), fieldExp);
            var writeInt = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, lookupId);
            return writeInt;
        }

        protected override Expression BuildSimpleHashsetOrListExpression(Expression fieldExp, ParameterExpression buffer, Type elementType, ParameterExpression context)
        {
            var iEnumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var iEnumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
            var size = Expression.Call(fieldExp, fieldExp.Type.GetMethod("get_Count"));
            var compareToNull = Expression.Equal(fieldExp, Expression.Default(fieldExp.Type));
            var writeNull = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, Expression.Constant(-1));

            var writeSize = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, size);
            var genEnumerable = Expression.Convert(fieldExp, iEnumerableType);
            var getEnumerator = Expression.Call(genEnumerable, iEnumerableType.GetMethod("GetEnumerator"));
            var enumParam = Expression.Parameter(iEnumeratorType);
            var assignEnumParm = Expression.Assign(enumParam, getEnumerator);

            var label = Expression.Label();
            var ifThenExit = Expression.IfThen(Expression.IsFalse(Expression.Call(enumParam, iEnumeratorType.GetInterface("IEnumerator").GetMethod("MoveNext"))), Expression.Break(label));
            var writeValue = Expression.Call(null, GetSimpleMethod($"Write{_methodMapping[elementType]}"), buffer, Expression.Call(enumParam, iEnumeratorType.GetMethod("get_Current")));
            var loop = Expression.Loop(Expression.Block(ifThenExit, writeValue), label);

            var block = Expression.Block(new[] { enumParam }, writeSize, assignEnumParm, loop);

            var compareIf = Expression.IfThenElse(compareToNull, writeNull, block);
            return compareIf;
        }

        protected override Expression BuildSimpleDictionaryExpression(Expression fieldExp, ParameterExpression buffer, Type keyType, Type valueType, ParameterExpression context)
        {
            var kvType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var iEnumerableType = typeof(IEnumerable<>).MakeGenericType(kvType);
            var iEnumeratorType = typeof(IEnumerator<>).MakeGenericType(kvType);
            var size = Expression.Call(fieldExp, fieldExp.Type.GetMethod("get_Count"));
            var compareToNull = Expression.Equal(fieldExp, Expression.Default(fieldExp.Type));
            var writeNull = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, Expression.Constant(-1));

            var writeSize = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, size);
            var genEnumerable = Expression.Convert(fieldExp, iEnumerableType);
            var getEnumerator = Expression.Call(genEnumerable, iEnumerableType.GetMethod("GetEnumerator"));
            var enumParam = Expression.Parameter(iEnumeratorType);
            var assignEnumParm = Expression.Assign(enumParam, getEnumerator);

            var label = Expression.Label();
            var ifThenExit = Expression.IfThen(Expression.IsFalse(Expression.Call(enumParam, iEnumeratorType.GetInterface("IEnumerator").GetMethod("MoveNext"))), Expression.Break(label));
            var getCurrent = Expression.Call(enumParam, iEnumeratorType.GetMethod("get_Current"));
            var getKey = Expression.Call(getCurrent, kvType.GetMethod("get_Key"));
            var getValue = Expression.Call(getCurrent, kvType.GetMethod("get_Value"));
            var writeKey = Expression.Call(null, GetSimpleMethod($"Write{_methodMapping[keyType]}"), buffer, getKey);
            var writeValue = Expression.Call(null, GetSimpleMethod($"Write{_methodMapping[valueType]}"), buffer, getValue);
            var loop = Expression.Loop(Expression.Block(ifThenExit, writeKey, writeValue), label);

            var block = Expression.Block(new[] { enumParam }, writeSize, assignEnumParm, loop);

            var compareIf = Expression.IfThenElse(compareToNull, writeNull, block);
            return compareIf;
        }
    }
}
