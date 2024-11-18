namespace Thorium.API.Emit;

using System.Linq.Expressions;

public partial class Emitter {
    private void BeginScope() => scopes.Push(new Dictionary<string, ParameterExpression>());

    private void EndScope() => scopes.Pop();

    private void DeclareVariable(string name, ParameterExpression variable) {
        var currentScope = scopes.Peek();
        if (!currentScope.TryAdd(name, variable)) {
            throw new Exception($"Variable {name} already declared in this scope.");
        }
    }

    private bool TryResolveVariable(string name, out ParameterExpression variable) {
        foreach (var scope in scopes) {
            if (scope.TryGetValue(name, out variable)) {
                return true;
            }
        }
        variable = null;
        return false;
    }
}