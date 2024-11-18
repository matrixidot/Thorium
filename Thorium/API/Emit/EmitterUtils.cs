namespace Thorium.API.Emit;

using System.Linq.Expressions;

public partial class Emitter {


    private static Type DeterminePromotedType(Type leftType, Type rightType, ExpressionType expressionType) {
        return PromotionStrategies.TryGetValue(expressionType, out var promoteFunc) ? promoteFunc(leftType, rightType) : leftType; // Default to left type
    }

    private static Type PromoteComparisonType(Type leftType, Type rightType) {
        if (leftType == rightType) return leftType;
        if (PromotionRules.TryGetValue((leftType, rightType), out Type result)) {
            return result;
        }
        throw new Exception($"Cannot compare types {leftType} and {rightType}");
    }

    private static Expression EnsureType(Expression expr, Type targetType) {
        return expr.Type != targetType ? Expression.Convert(expr, targetType) : expr;
    }

    // Combine Ensure functions to reduce redundant type checks
    private static Expression EnsureType(Expression expr, Func<Type, bool> typeValidator, string errorMessage) {
        if (!typeValidator(expr.Type)) {
            throw new Exception(errorMessage);
        }
        return expr;
    }

    private static Expression EnsureNumericType(Expression expr) {
        return EnsureType(expr, IsNumericType, $"Operand is not a numeric type: {expr.Type}");
    }

    private static Expression EnsureIntegerType(Expression expr) {
        return EnsureType(expr, t => t == typeof(int) || t == typeof(long), $"Operand is not an integer type: {expr.Type}");
    }

    private static Expression EnsureBooleanType(Expression expr) {
        return EnsureType(expr, t => t == typeof(bool), $"Cannot convert type {expr.Type} to bool");
    }
    

    private static bool IsNumericType(Type type) {
        return NumericTypes.Contains(type);
    }

    private static Type PromoteNumericType(Type leftType, Type rightType) {
        if (PromotionRules.TryGetValue((leftType, rightType), out Type result)) {
            return result;
        }
        throw new Exception($"Cannot promote types {leftType} and {rightType} to a numeric type.");
    }

    private static Type PromoteIntegerType(Type leftType, Type rightType) {
        if (PromotionRules.TryGetValue((leftType, rightType), out Type result) && NumericTypes.Contains(result)) {
            return result;
        }
        throw new Exception($"Cannot promote types {leftType} and {rightType} to an integer type.");
    }

    private static bool CanConvert(Type from, Type to) {
        return to.IsAssignableFrom(from) ||
               (from == typeof(int) && (to == typeof(long) || to == typeof(double))) ||
               (from == typeof(long) && to == typeof(double));
    }

    private static Type ResolveType(string typeName) {
        if (TypeCache.TryGetValue(typeName, out Type resolvedType)) return resolvedType;
        return Type.GetType(typeName) ?? throw new Exception($"Cannot resolve type {typeName}");
    }
    
    // Keep the stack-based scoping mechanism as per your request
    private readonly Stack<Dictionary<string, ParameterExpression>> scopes = new Stack<Dictionary<string, ParameterExpression>>();

    // Return the variables from the first (bottom) scope in the stack
    public List<ParameterExpression> GlobalVars => scopes.ToArray()[scopes.Count - 1].Values.ToList();

    public Emitter() {
        scopes.Push(new Dictionary<string, ParameterExpression>());
    }
}