namespace Thorium.API.Emit;

using System.Linq.Expressions;
using Errors;
using Lexing;
using static System.Linq.Expressions.ExpressionType;
public partial class Emitter {
    private bool IsNumericType(Type type) {
        return type == typeof(int) || type == typeof(long) || type == typeof(double);
    }

    private bool IsBitwiseExpressionType(ExpressionType expressionType) {
        return expressionType is And or Or or ExclusiveOr or LeftShift or RightShift;
    }

    private bool IsArithmeticExpressionType(ExpressionType expressionType) {
        return expressionType is Add or Subtract or Multiply or Divide or Modulo;
    }

    private bool IsComparisonExpressionType(ExpressionType expressionType) {
        return expressionType is Equal or NotEqual or LessThan or LessThanOrEqual or GreaterThan or GreaterThanOrEqual;
    }
    
    private bool CanConvert(Type from, Type to) {
        return to.IsAssignableFrom(from) ||
               from == typeof(int) && to == typeof(long) ||
               from == typeof(int) && to == typeof(double) ||
               from == typeof(long) && to == typeof(double);
    }
    
    private RuntimeError Error(Token token, string message) {
        RuntimeError error = new RuntimeError(token, message);
        Thorium.RuntimeError(error);
        return error;
    }

    private void BeginScope() => scopes.Push(new());
    private void EndScope() => scopes.Pop();

    private void DeclareVariable(string name, ParameterExpression variable) {
        Dictionary<string, ParameterExpression> currentScope = scopes.Peek();
        if (!currentScope.TryAdd(name, variable)) {
            throw new Exception($"Variable {name} already declared in this scope.");
        }
    }

    private bool TryResolveVariable(string name, out ParameterExpression variable) {
        foreach (Dictionary<string, ParameterExpression> scope in scopes) {
            if (scope.TryGetValue(name, out variable)) {
                return true;
            }
        }
        variable = null;
        return false;
    }
}