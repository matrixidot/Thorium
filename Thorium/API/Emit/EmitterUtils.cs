namespace Thorium.API.Emit;

using System.Linq.Expressions;
using Errors;
using Lexing;
using static System.Linq.Expressions.ExpressionType;
using static Lexing.TokenType;

public partial class Emitter {
    private static Type DeterminePromotedType(Type leftType, Type rightType, ExpressionType expressionType) {
        return expressionType switch {
            _ when IsBitwiseExpressionType(expressionType) => PromoteIntegerType(leftType, rightType),
            _ when IsArithmeticExpressionType(expressionType) => PromoteNumericType(leftType, rightType),
            _ when IsComparisonExpressionType(expressionType) => PromoteComparisonType(leftType, rightType),
            _ => leftType, // Default to left type
        };
    }

    private static Type PromoteComparisonType(Type leftType, Type rightType) {
        if (leftType == rightType) return leftType;
        if (IsNumericType(leftType) && IsNumericType(rightType))
            return PromoteNumericType(leftType, rightType);
        
        throw new Exception($"Cannot compare types {leftType} and {rightType}");
    }
    
    private static Expression EnsureType(Expression expr, Type targetType) {
        return expr.Type != targetType ? Expression.Convert(expr, targetType) : expr;
    }

    private static Expression EnsureNumericType(Expression expr) {
        if (IsNumericType(expr.Type)) return expr;
        if (expr.Type == typeof(bool)) {
            return Expression.Condition(expr, ConstTrue, ConstFalse);
        }
        throw new Exception($"Operand is not a numeric type: {expr.Type}");
    }

    private static Expression EnsureIntegerType(Expression expr) {
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

    private static Expression EnsureBooleanType(Expression expr) {
        if (expr.Type == typeof(bool)) return expr;

        if (IsNumericType(expr.Type)) {
            ConstantExpression zero = Expression.Constant(0, expr.Type);
            return Expression.NotEqual(expr, zero);
        }
        throw new Exception($"Cannot convert type {expr.Type} to bool");
    }

    private static Type PromoteNumericType(Type leftType, Type rightType) {
        if (PromotionRules.TryGetValue((leftType, rightType), out Type result) ||
            PromotionRules.TryGetValue((rightType, leftType), out result)) {
            return result;
        }
        throw new Exception($"Cannot promote types {leftType} and {rightType} to a numeric type");
    }

    private static Type PromoteIntegerType(Type leftType, Type rightType) {
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);
        if (leftType == typeof(int) || rightType == typeof(int))
            return typeof(int);
        throw new Exception($"Cannot promote types {leftType} and {rightType} to an integer type");
    }
    
    private static Type ResolveType(string typeName) {
        if (TypeCache.TryGetValue(typeName, out Type resolvedType)) return resolvedType;
        return Type.GetType(typeName) ?? throw new Exception($"Cannot resolve type {typeName}");
    }
    
    private static bool IsNumericType(Type type) => NumericTypes.Contains(type);

    private static bool IsBitwiseExpressionType(ExpressionType expressionType) {
        return expressionType == And || expressionType == Or || expressionType == ExclusiveOr || expressionType == LeftShift || expressionType == RightShift;
    }

    private static bool IsArithmeticExpressionType(ExpressionType expressionType) {
        return expressionType == Add || expressionType == Subtract || expressionType == Multiply || expressionType == Divide || expressionType == Modulo;
    }

    private static bool IsComparisonExpressionType(ExpressionType expressionType) {
        return expressionType == Equal || expressionType == NotEqual || 
               expressionType == LessThan || expressionType == LessThanOrEqual || 
               expressionType == GreaterThan || expressionType == GreaterThanOrEqual;
    }
    
    private static bool CanConvert(Type from, Type to) {
        return to.IsAssignableFrom(from) ||
               from == typeof(int) && to == typeof(long) ||
               from == typeof(int) && to == typeof(double) ||
               from == typeof(long) && to == typeof(double);
    }
    
    private static RuntimeError Error(Token token, string message) {
        RuntimeError error = new RuntimeError(token, message);
        Thorium.RuntimeError(error);
        return error;
    }
    
    private static readonly Dictionary<TokenType, ExpressionType> BinaryOperatorMap = new Dictionary<TokenType, ExpressionType> {
        { PLUS, Add },
        { MINUS, Subtract },
        { MULT, Multiply },
        { DIV, Divide },
        { MOD, Modulo },
        { EQUAL_EQUAL, Equal },
        { BANG_EQUAL, NotEqual },
        { LESS, LessThan },
        { LESS_EQUAL, LessThanOrEqual },
        { GREATER, GreaterThan },
        { GREATER_EQUAL, GreaterThanOrEqual },
        { BIN_AND, And },
        { BIN_OR, Or },
        { BIN_XOR, ExclusiveOr },
        { LEFT_SHIFT, LeftShift },
        { RIGHT_SHIFT, RightShift },
    };
    private static readonly HashSet<Type> NumericTypes = [typeof(int), typeof(long), typeof(double),];
    private static readonly ConstantExpression ConstTrue = Expression.Constant(1);
    private static readonly ConstantExpression ConstFalse = Expression.Constant(0);
    private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type> {
        { "int", typeof(int) },
        { "long", typeof(long) },
        { "double", typeof(double) },
        { "bool", typeof(bool) },
        { "string", typeof(string) },
        { "char", typeof(char) },
        { "object", typeof(object) }
    };
    private static readonly Dictionary<(Type, Type), Type> PromotionRules = new Dictionary<(Type, Type), Type> {
        { (typeof(int), typeof(int)), typeof(int) },
        { (typeof(int), typeof(long)), typeof(long) },
        { (typeof(long), typeof(int)), typeof(long) },
        { (typeof(long), typeof(long)), typeof(long) },
        
        { (typeof(long), typeof(double)), typeof(double) },
        { (typeof(double), typeof(long)), typeof(double) },        
        { (typeof(int), typeof(double)), typeof(double) },
        { (typeof(double), typeof(int)), typeof(double) },
        { (typeof(double), typeof(double)), typeof(double) },
        // Add other combinations as needed
    };
    
    private readonly Stack<Dictionary<string, ParameterExpression>> scopes = new Stack<Dictionary<string, ParameterExpression>>();
    public List<ParameterExpression> GlobalVars => scopes.ToArray()[0].Values.ToList();

    public Emitter() {
        scopes.Push(new Dictionary<string, ParameterExpression>());
    }
}