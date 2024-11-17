namespace Thorium.API.Emit;

using System.Linq.Expressions;

public partial class Emitter {
        private Type DeterminePromotedType(Type leftType, Type rightType, ExpressionType expressionType) {
        if (IsBitwiseExpressionType(expressionType)) {
            return PromoteIntegerType(leftType, rightType);
        }
        if (IsArithmeticExpressionType(expressionType)) {
            return PromoteNumericType(leftType, rightType);
        }
        if (IsComparisonExpressionType(expressionType)) {
            return PromoteComparisonType(leftType, rightType);
        }
        // Default to the left operand's type
        return leftType;
    }

    private Type PromoteComparisonType(Type leftType, Type rightType) {
        if (leftType == rightType) {
            // No promotion needed if types are the same
            return leftType;
        }
        if (IsNumericType(leftType) && IsNumericType(rightType)) {
            // Promote to a common numeric type
            return PromoteNumericType(leftType, rightType);
        }
        throw new Exception($"Cannot compare types {leftType} and {rightType}");
    }

    // Helper methods
    private Expression EnsureType(Expression expr, Type targetType) {
        if (expr.Type != targetType) {
            return Expression.Convert(expr, targetType);
        }
        return expr;
    }

    private Expression EnsureNumericType(Expression expr) {
        if (IsNumericType(expr.Type)) {
            return expr;
        }
        if (expr.Type == typeof(bool)) {
            // Convert bool to int (true = 1, false = 0)
            return Expression.Condition(expr, Expression.Constant(1), Expression.Constant(0));
        }
        throw new Exception($"Operand is not a numeric type: {expr.Type}");
    }

    private Expression EnsureIntegerType(Expression expr) {
        if (expr.Type == typeof(int) || expr.Type == typeof(long)) {
            return expr;
        }
        if (expr.Type == typeof(double)) {
            // Convert double to int (possible loss of precision)
            return Expression.Convert(expr, typeof(int));
        }
        if (expr.Type == typeof(bool)) {
            // Convert bool to int (true = 1, false = 0)
            return Expression.Condition(expr, Expression.Constant(1), Expression.Constant(0));
        }
        throw new Exception($"Operand is not an integer type: {expr.Type}");
    }

    private Expression EnsureBooleanType(Expression expr) {
        if (expr.Type == typeof(bool)) {
            return expr;
        }
        if (IsNumericType(expr.Type)) {
            // Consider non-zero values as true
            ConstantExpression zero = Expression.Constant(Convert.ChangeType(0, expr.Type), expr.Type);
            return Expression.NotEqual(expr, zero);
        }
        throw new Exception($"Cannot convert type {expr.Type} to bool");
    }

    private Type PromoteNumericType(Type leftType, Type rightType) {
        if (leftType == typeof(double) || rightType == typeof(double))
            return typeof(double);
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);
        if (leftType == typeof(int) || rightType == typeof(int))
            return typeof(int);
        throw new Exception($"Cannot promote types {leftType} and {rightType} to a numeric type");
    }

    private Type PromoteIntegerType(Type leftType, Type rightType) {
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);
        if (leftType == typeof(int) || rightType == typeof(int))
            return typeof(int);
        throw new Exception($"Cannot promote types {leftType} and {rightType} to an integer type");
    }

    private Type ResolveType(string typeName) {
        return typeName switch {
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "bool" => typeof(bool),
            "string" => typeof(string),
            "char" => typeof(char),
            "object" => typeof(object),
            _ => Type.GetType(typeName) ?? throw new Exception($"Cannot resolve type {typeName}"),
        };
    }
}