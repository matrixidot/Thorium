namespace Thorium.API.Emit;

using System.Linq.Expressions;
using Tools;

public partial class Emitter {
    private MethodCallExpression HandlePowerOperator(Expression left, Expression right) {
        left = EnsureType(left, typeof(double));
        right = EnsureType(right, typeof(double));
        return Expression.Call(typeof(Math).GetMethod("Pow", [typeof(double), typeof(double)]), left, right);
    }

    private MethodCallExpression HandleStringConcatenation(Expression left, Expression right) {
        left = EnsureType(left, typeof(object));
        right = EnsureType(right, typeof(object));

        return Expression.Call(typeof(string).GetMethod("Concat", [typeof(object), typeof(object)]), left, right);
    }

    private BinaryExpression HandleLogicalAnd(Expression left, Expression right) {
        left = EnsureBooleanType(left);
        right = EnsureBooleanType(right);
        return Expression.AndAlso(left, right);
    }

    private BinaryExpression HandleLogicalOr(Expression left, Expression right) {
        left = EnsureBooleanType(left);
        right = EnsureBooleanType(right);
        return Expression.OrElse(left, right);
    }

    private BinaryExpression HandleBinaryOperation(ExpressionType expressionType, Expression left, Expression right) {
        // Determine the operand types and promote them if necessary
        Type promotedType = DeterminePromotedType(left.Type, right.Type, expressionType);

        left = EnsureType(left, promotedType);
        right = EnsureType(right, promotedType);

        return Expression.MakeBinary(expressionType, left, right);
    }
}