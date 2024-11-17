namespace Thorium.API.Emit;

using System.Linq.Expressions;
using Lexing;
using static Lexing.TokenType;
using static System.Linq.Expressions.ExpressionType;
public partial class Emitter {
    private readonly Stack<Dictionary<string, ParameterExpression>> scopes = new();

    public List<ParameterExpression> GlobalVars => scopes.ToArray()[0].Values.ToList();
    
    private static readonly Dictionary<TokenType, ExpressionType> BinaryOperatorMap = new() {
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

    public Emitter() {
        scopes.Push(new Dictionary<string, ParameterExpression>());
    }
}