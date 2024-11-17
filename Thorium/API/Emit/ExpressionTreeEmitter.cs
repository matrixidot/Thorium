namespace Thorium.API.Emit;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Errors;
using Lexing;
using Parsing;
using Tools;
using static System.Linq.Expressions.ExpressionType;
using static Lexing.TokenType;
using Convert = Convert;
using EmitExpr = System.Linq.Expressions.Expression;
using Expression = Parsing.Expression;
using Void = Tools.Void;

public class ExpressionTreeEmitter : ExprVisitor<EmitExpr>, StmtVisitor<EmitExpr> {
    private readonly Dictionary<string, ParameterExpression> variables = new();
    public List<ParameterExpression> VarList => variables.Values.ToList();
    // Mapping of binary operators to ExpressionTypes
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

    public EmitExpr VisitExpressionStmt(Expression stmt) {
        return stmt.Expr.Accept(this);
    }

    public EmitExpr VisitPrintStmt(Print stmt) {
        var expr = stmt.Expr.Accept(this);
        var writeLine = typeof(Console).GetMethod("WriteLine", [typeof(object)]);
        var convertedExpr = EmitExpr.Convert(expr, typeof(object));
        return EmitExpr.Call(writeLine, convertedExpr);
    }

    public EmitExpr VisitVarStmt(Var stmt) {
        string varName = stmt.Name.Lexeme;
        if (variables.ContainsKey(varName)) {
            throw new Exception($"Variable '{varName}' is already declared.");
        }
        Type varType = stmt.Typ;
        ParameterExpression variable = EmitExpr.Variable(varType, varName);
        variables[varName] = variable;

        if (stmt.Initializer != null) {
            EmitExpr initializerExpr = stmt.Initializer.Accept(this);
            
            if (initializerExpr.Type != varType) {
                initializerExpr = EmitExpr.Convert(initializerExpr, varType);
            }
            return EmitExpr.Assign(variable, initializerExpr);
        }
        return EmitExpr.Assign(variable, EmitExpr.Default(varType));
    }

    public EmitExpr VisitVariableExpr(Variable expr) {
        if (variables.TryGetValue(expr.Name.Lexeme, out ParameterExpression variable)) {
            return variable;
        }
        throw Error(expr.Name, $"Undefined variable {expr.Name.Lexeme}");
    }

    public EmitExpr VisitAssignExpr(Assign expr)
    {
        string varName = expr.Name.Lexeme;
        
        if (!variables.TryGetValue(varName, out ParameterExpression variable)) {
            throw new Exception($"Undefined variable '{varName}'.");
        }
        
        EmitExpr valueExpr = expr.Value.Accept(this);
        
        Type varType = variable.Type;
        if (valueExpr.Type != varType) {
            if (CanConvert(valueExpr.Type, varType)) {
                valueExpr = EmitExpr.Convert(valueExpr, varType);
            }
            else {
                throw new Exception($"Cannot assign value of type '{valueExpr.Type}' to variable '{varName}' of type '{varType}'.");
            }
        }

        // Create the assignment expression
        return EmitExpr.Assign(variable, valueExpr);
    }
    
    public EmitExpr VisitBinaryExpr(Binary expr) {
        EmitExpr left = expr.Left.Accept(this);
        EmitExpr right = expr.Right.Accept(this);

        switch (expr.Op.Type) {
            // Handle special operators separately
            case POW: return HandlePowerOperator(left, right);
            case PLUS when left.Type == typeof(string) || right.Type == typeof(string): return HandleStringConcatenation(left, right);
            case AND: return HandleLogicalAnd(left, right);
            case OR: return HandleLogicalOr(left, right);
        }
        
        if (BinaryOperatorMap.TryGetValue(expr.Op.Type, out ExpressionType expressionType)) {
            return HandleBinaryOperation(expressionType, left, right);
        }
        throw Error(expr.Op, $"Unsupported binary operator: {expr.Op.Type}");
    }

    public EmitExpr VisitGroupingExpr(Grouping expr) {
        return expr.Expr.Accept(this);
    }

    public EmitExpr VisitLiteralExpr(Literal expr) {
        object? value = expr.Value;

        Type type = value switch {
            int => typeof(int),
            long => typeof(long),
            double => typeof(double),
            bool => typeof(bool),
            string => typeof(string),
            null => typeof(object),
            _ => throw Error(expr.Tkn, $"Unsupported literal type: {value?.GetType()}"),
        };

        return EmitExpr.Constant(value, type);
    }

    public EmitExpr VisitUnaryExpr(Unary expr)
    {
        EmitExpr operand = expr.Right.Accept(this);

        switch (expr.Op.Type) {
            case MINUS:
                operand = EnsureNumericType(operand);
                return EmitExpr.Negate(operand);
            case BANG:
                operand = EnsureBooleanType(operand);
                return EmitExpr.Not(operand);
            case BIN_NOT:
                operand = EnsureIntegerType(operand);
                return EmitExpr.OnesComplement(operand);
            default: throw Error(expr.Op, $"Unsupported unary operator: {expr.Op.Type}");
        }
    }

    private MethodCallExpression HandlePowerOperator(EmitExpr left, EmitExpr right)
    {
        Type leftType = left.Type;
        Type rightType = right.Type;

        MethodCallExpression powCall;
        if (typeof(int) == leftType && typeof(int) == rightType) {
            powCall = EmitExpr.Call(typeof(IntMath).GetMethod("IPow", [leftType, rightType]), left, right);
        } else if (leftType == typeof(long) && rightType == typeof(long) || leftType == typeof(int) && rightType == typeof(long) || leftType == typeof(long) && rightType == typeof(int)) {
            left = EnsureType(left, typeof(long));
            right = EnsureType(right, typeof(long));
            powCall = EmitExpr.Call(typeof(IntMath).GetMethod("LPow", [leftType, rightType]), left, right);
        }
        else {
            left = EnsureType(left, typeof(double));
            right = EnsureType(right, typeof(double));
            powCall = EmitExpr.Call(typeof(Math).GetMethod("Pow", [typeof(double), typeof(double)]), left, right);
            long a = 2;
            int b = 2;
        }
        return powCall;
    }

    private MethodCallExpression HandleStringConcatenation(EmitExpr left, EmitExpr right)
    {
        left = EnsureType(left, typeof(object));
        right = EnsureType(right, typeof(object));

        return EmitExpr.Call(typeof(string).GetMethod("Concat", [typeof(object), typeof(object)]), left, right);
    }

    private BinaryExpression HandleLogicalAnd(EmitExpr left, EmitExpr right)
    {
        left = EnsureBooleanType(left);
        right = EnsureBooleanType(right);
        return EmitExpr.AndAlso(left, right);
    }

    private BinaryExpression HandleLogicalOr(EmitExpr left, EmitExpr right)
    {
        left = EnsureBooleanType(left);
        right = EnsureBooleanType(right);
        return EmitExpr.OrElse(left, right);
    }

    private BinaryExpression HandleBinaryOperation(ExpressionType expressionType, EmitExpr left, EmitExpr right)
    {
        // Determine the operand types and promote them if necessary
        Type promotedType = DeterminePromotedType(left.Type, right.Type, expressionType);

        left = EnsureType(left, promotedType);
        right = EnsureType(right, promotedType);

        return EmitExpr.MakeBinary(expressionType, left, right);
    }

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
    private EmitExpr EnsureType(EmitExpr expr, Type targetType) {
        if (expr.Type != targetType) {
            return EmitExpr.Convert(expr, targetType);
        }
        return expr;
    }

    private EmitExpr EnsureNumericType(EmitExpr expr) {
        if (IsNumericType(expr.Type)) {
            return expr;
        }
        if (expr.Type == typeof(bool)) {
            // Convert bool to int (true = 1, false = 0)
            return EmitExpr.Condition(expr, EmitExpr.Constant(1), EmitExpr.Constant(0));
        }
        throw new Exception($"Operand is not a numeric type: {expr.Type}");
    }

    private EmitExpr EnsureIntegerType(EmitExpr expr) {
        if (expr.Type == typeof(int) || expr.Type == typeof(long)) {
            return expr;
        }
        if (expr.Type == typeof(double)) {
            // Convert double to int (possible loss of precision)
            return EmitExpr.Convert(expr, typeof(int));
        }
        if (expr.Type == typeof(bool)) {
            // Convert bool to int (true = 1, false = 0)
            return EmitExpr.Condition(expr, EmitExpr.Constant(1), EmitExpr.Constant(0));
        }
        throw new Exception($"Operand is not an integer type: {expr.Type}");
    }

    private EmitExpr EnsureBooleanType(EmitExpr expr) {
        if (expr.Type == typeof(bool)) {
            return expr;
        }
        if (IsNumericType(expr.Type)) {
            // Consider non-zero values as true
            ConstantExpression zero = EmitExpr.Constant(Convert.ChangeType(0, expr.Type), expr.Type);
            return EmitExpr.NotEqual(expr, zero);
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

    private Type PromoteIntegerType(Type leftType, Type rightType)
    {
        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);
        if (leftType == typeof(int) || rightType == typeof(int))
            return typeof(int);
        throw new Exception($"Cannot promote types {leftType} and {rightType} to an integer type");
    }

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
               (from == typeof(int) && to == typeof(long)) ||
               (from == typeof(int) && to == typeof(double)) ||
               (from == typeof(long) && to == typeof(double));
    }
    
    private RuntimeError Error(Token token, string message) {
        RuntimeError error = new RuntimeError(token, message);
        Thorium.RuntimeError(error);
        return error;
    }
}
