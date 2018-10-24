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

        protected override Expression BuildSimpleArrayExpression(Expression fieldExp, ParameterExpression buffer, Type elementType)
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

        protected override Expression BuildSimpleListExpression(Expression fieldExp, ParameterExpression buffer, Type elementType)
        {
            var size = Expression.Call(fieldExp, fieldExp.Type.GetMethod("get_Count"));
            var compareToNull = Expression.Equal(fieldExp, Expression.Default(fieldExp.Type));
            var writeNull = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, Expression.Constant(-1));

            var writeSize = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, size);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var label = Expression.Label();
            var arrayValue = Expression.Call(fieldExp, fieldExp.Type.GetMethod("get_Item"), index);
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

        protected override Expression BuildQwackExpression(FieldInfo field, ParameterExpression buffer, ParameterExpression context, MemberExpression fieldExp)
        {
            var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectId"), fieldExp);
            var writeInt = Expression.Call(null, GetSimpleMethod("WriteInt"), buffer, lookupId);
            return writeInt;
        }
    }
}
