using System.Linq.Expressions;
using System.Reflection;
using Thorium.API.Lexing;
using static System.Linq.Expressions.ExpressionType;
using static Thorium.API.Lexing.TokenType;
namespace Thorium.API.Emit;

public partial class Emitter {
    // Cached Methodinfos
    private static readonly MethodInfo PowMethodInfo = 
        typeof(Math).GetMethod("Pow", new[] { typeof(double), typeof(double) })
        ?? throw new InvalidOperationException("Math.Pow method not found");
    
    private static readonly MethodInfo ConcatMethodInfo = 
        typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) })
        ?? throw new InvalidOperationException("String.Concat method not found");
    
    private static readonly MethodInfo writeLine = 
        typeof(Console).GetMethod("WriteLine", new[] { typeof(object) })
        ?? throw new InvalidOperationException("Console.WriteLine method not found");
    // Lookup Tables
    private static readonly HashSet<Type> NumericTypes = [typeof(int), typeof(long), typeof(double)];
    private static readonly HashSet<Type> IntegerTypes = [typeof(int), typeof(long)];
    private static readonly HashSet<Type> DecimalTypes = [typeof(double)];
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

    private static readonly Dictionary<string, Type> TypeCache = new()
    {
        { "int", typeof(int) },
        { "long", typeof(long) },
        { "double", typeof(double) },
        { "bool", typeof(bool) },
        { "string", typeof(string) },
        { "char", typeof(char) },
        { "object", typeof(object) }
    };

    private static readonly Dictionary<(Type, Type), Type> PromotionRules = new()
    {
        { (typeof(int), typeof(int)), typeof(int) },
        { (typeof(int), typeof(long)), typeof(long) },
        { (typeof(long), typeof(int)), typeof(long) },
        { (typeof(long), typeof(long)), typeof(long) },
        { (typeof(int), typeof(double)), typeof(double) },
        { (typeof(double), typeof(int)), typeof(double) },
        { (typeof(long), typeof(double)), typeof(double) },
        { (typeof(double), typeof(long)), typeof(double) },
        { (typeof(double), typeof(double)), typeof(double) },
    };
    
    private static readonly Dictionary<ExpressionType, Func<Type, Type, Type>> PromotionStrategies = new() {
        { And, PromoteIntegerType },
        { Or, PromoteIntegerType },
        { ExclusiveOr, PromoteIntegerType },
        { LeftShift, PromoteIntegerType },
        { RightShift, PromoteIntegerType },
        { Add, PromoteNumericType },
        { Subtract, PromoteNumericType },
        { Multiply, PromoteNumericType },
        { Divide, PromoteNumericType },
        { Modulo, PromoteNumericType },
        { Equal, PromoteComparisonType },
        { NotEqual, PromoteComparisonType },
        { LessThan, PromoteComparisonType },
        { LessThanOrEqual, PromoteComparisonType },
        { GreaterThan, PromoteComparisonType },
        { GreaterThanOrEqual, PromoteComparisonType },
    };
    // Useful Expressions
    private static readonly Expression one = Expression.Constant(1, typeof(int));

}