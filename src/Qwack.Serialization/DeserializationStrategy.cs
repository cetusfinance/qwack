using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Qwack.Serialization
{
    public class DeserializationStrategy : BaseStrategy
    {
        public DeserializationStrategy(Type objectType) : base(objectType)
        {
        }

        protected override Expression BuildSimpleArrayExpression(Expression field, ParameterExpression buffer, Type elementType)
        {
            var length = Expression.Parameter(typeof(int));
            var getlength = BuildExpression(typeof(int), length, buffer);
            var array = Expression.Parameter(elementType.MakeArrayType());
            var createAnArray = Expression.NewArrayBounds(elementType, length);
            var assignArray = Expression.Assign(array, createAnArray);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var increment = Expression.AddAssign(index, Expression.Constant(1));
            var arrayValue = Expression.ArrayAccess(array, index);
            var label = Expression.Label();
            var ifThenExit = Expression.IfThen(Expression.Equal(index, length), Expression.Break(label));
            var readArrayValue = BuildExpression(elementType, arrayValue, buffer);
            var expressionLoop = Expression.Loop(Expression.Block(ifThenExit, readArrayValue, increment), label);
            var finalAssignArray = Expression.Assign(field, array);

            var ifNull = Expression.IfThen(Expression.NotEqual(length, Expression.Constant(-1)),
                Expression.Block(assignArray, assignZero, expressionLoop, finalAssignArray));

            var returnBlock = Expression.Block(new[] { length, array, index }, getlength, ifNull);

            return returnBlock;
        }

        protected override Expression BuildExpression(Type fieldType, Expression field, ParameterExpression buffer, Type convertType = null)
        {
            var method = GetSimpleMethod($"Read{_methodMapping[fieldType]}");
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
            _serializer(instance, ref buffer, context);
            return instance;
        }

        protected override Expression BuildQwackExpression(FieldInfo field, ParameterExpression buffer, ParameterExpression context, MemberExpression fieldExp)
        {
            var idValue = Expression.Call(null, GetSimpleMethod("ReadInt"), buffer);
            var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectById"), idValue);
            var assignObject = Expression.Assign(fieldExp, Expression.Convert(lookupId, field.FieldType));
            return assignObject;
        }
    }
}
