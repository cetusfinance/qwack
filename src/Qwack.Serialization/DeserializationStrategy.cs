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

        protected override Expression BuildSimpleArrayExpression(Expression field, ParameterExpression buffer, Type elementType, ParameterExpression context)
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

        protected override Expression BuildQwackExpression(ParameterExpression buffer, ParameterExpression context, Expression fieldExp)
        {
            var idValue = Expression.Call(null, GetSimpleMethod("ReadInt"), buffer);
            var lookupId = Expression.Call(context, typeof(DeserializationContext).GetMethod("GetObjectById"), idValue);
            var assignObject = Expression.Assign(fieldExp, Expression.Convert(lookupId, fieldExp.Type));
            return assignObject;
        }

        protected override Expression BuildSimpleHashsetOrListExpression(Expression field, ParameterExpression buffer, Type elementType, ParameterExpression context)
        {
            var length = Expression.Parameter(typeof(int));
            var getlength = BuildExpression(typeof(int), length, buffer);
            var array = Expression.Parameter(field.Type);
            var createAnArray = Expression.New(field.Type);
            var assignArray = Expression.Assign(array, createAnArray);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var increment = Expression.AddAssign(index, Expression.Constant(1));
            var value = Expression.Parameter(elementType);
            var addMethod = Expression.Call(array, array.Type.GetMethod("Add"), value);
            var label = Expression.Label();
            var ifThenExit = Expression.IfThen(Expression.Equal(index, length), Expression.Break(label));
            var readArrayValue = GetExpressionForType(value, buffer, context);
            var expressionLoop = Expression.Loop(Expression.Block(ifThenExit, readArrayValue, addMethod, increment), label);
            var finalAssignArray = Expression.Assign(field, array);

            var ifNull = Expression.IfThen(Expression.NotEqual(length, Expression.Constant(-1)),
                Expression.Block(assignArray, assignZero, expressionLoop, finalAssignArray));

            var returnBlock = Expression.Block(new[] { value, length, array, index }, getlength, ifNull);

            return returnBlock;
        }

        protected override Expression BuildSimpleDictionaryExpression(Expression field, ParameterExpression buffer, Type keyType, Type valueType, ParameterExpression context)
        {
            var length = Expression.Parameter(typeof(int));
            var getlength = BuildExpression(typeof(int), length, buffer);
            var array = Expression.Parameter(field.Type);
            var createAnArray = Expression.New(field.Type);
            var assignArray = Expression.Assign(array, createAnArray);
            var index = Expression.Parameter(typeof(int), "index");
            var assignZero = Expression.Assign(index, Expression.Constant(0));
            var increment = Expression.AddAssign(index, Expression.Constant(1));
            var value = Expression.Parameter(valueType);
            var key = Expression.Parameter(keyType);
            var addMethod = Expression.Call(array, array.Type.GetMethod("Add"), key, value);
            var label = Expression.Label();
            var ifThenExit = Expression.IfThen(Expression.Equal(index, length), Expression.Break(label));
            var readKey = BuildExpression(keyType, key, buffer);
            var readValue = BuildExpression(valueType, value, buffer);
            var expressionLoop = Expression.Loop(Expression.Block(ifThenExit, readKey, readValue, addMethod, increment), label);
            var finalAssignArray = Expression.Assign(field, array);

            var ifNull = Expression.IfThen(Expression.NotEqual(length, Expression.Constant(-1)),
                Expression.Block(assignArray, assignZero, expressionLoop, finalAssignArray));

            var returnBlock = Expression.Block(new[] { key, value, length, array, index }, getlength, ifNull);

            return returnBlock;
        }
    }
}
