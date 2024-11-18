namespace Thorium.API.Emit;

using Errors;
using Lexing;
using System.Linq.Expressions;

public partial class Emitter {

    private static MethodCallExpression HandlePowerOperator(Expression left, Expression right) {
        left = EnsureType(left, typeof(double));
        right = EnsureType(right, typeof(double));
        return Expression.Call(PowMethodInfo, left, right);
    }

    private static MethodCallExpression HandleStringConcatenation(Expression left, Expression right) {
        left = EnsureType(left, typeof(object));
        right = EnsureType(right, typeof(object));
        return Expression.Call(ConcatMethodInfo, left, right);
    }

    private static BinaryExpression HandleLogicalAnd(Expression left, Expression right) {
        left = EnsureBooleanType(left);
        right = EnsureBooleanType(right);
        return Expression.AndAlso(left, right);
    }

    private static BinaryExpression HandleLogicalOr(Expression left, Expression right) {
        left = EnsureBooleanType(left);
        right = EnsureBooleanType(right);
        return Expression.OrElse(left, right);
    }

    private static BinaryExpression HandleBinaryOperation(ExpressionType expressionType, Expression left, Expression right) {
        Type promotedType = DeterminePromotedType(left.Type, right.Type, expressionType);
        left = EnsureType(left, promotedType);
        right = EnsureType(right, promotedType);
        return Expression.MakeBinary(expressionType, left, right);
    }

    private static RuntimeError Error(Token token, string message) {
        var error = new RuntimeError(token, message);
        Thorium.RuntimeError(error);
        return error;
    }
}