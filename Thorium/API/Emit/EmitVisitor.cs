namespace Thorium.API.Emit;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Parsing;
using static Lexing.TokenType;
using Expression = System.Linq.Expressions.Expression;
using ExpressionStmt = Parsing.Expression;

public partial class Emitter : ExprVisitor<Expression>, StmtVisitor<Expression> {
    public Expression VisitExpressionStmt(ExpressionStmt stmt) {
        return stmt.Expr.Accept(this);
    }

    public Expression VisitBlockStmt(Block block) {
        BeginScope();
        List<Expression> expressions = [];
        List<ParameterExpression> blockVars = [];

        foreach (Stmt stmt in block.Statements) {
            Expression expr = stmt.Accept(this);

            if (stmt is Var varDec) {
                if (TryResolveVariable(varDec.Name.Lexeme, out ParameterExpression variable)) {
                    blockVars.Add(variable);
                }
            }
            expressions.Add(expr);
        }
        EndScope();
        return Expression.Block(blockVars, expressions);
    }

    public Expression VisitPrintStmt(Print stmt) {
        Expression expr = stmt.Expr.Accept(this);
        MethodInfo writeLine = typeof(Console).GetMethod("WriteLine", [typeof(object)]);
        UnaryExpression convertedExpr = Expression.Convert(expr, typeof(object));
        return Expression.Call(writeLine, convertedExpr);
    }

    public Expression VisitVarStmt(Var stmt) {
        string varName = stmt.Name.Lexeme;
        Type varType = stmt.Typ;
        ParameterExpression variable = Expression.Variable(varType, varName);
        
        DeclareVariable(varName, variable);

        if (stmt.Initializer == null) 
            return Expression.Assign(variable, Expression.Default(varType));
        
        Expression initializerExpr = stmt.Initializer.Accept(this);
            
        if (initializerExpr.Type != varType) {
            initializerExpr = Expression.Convert(initializerExpr, varType);
        }
        return Expression.Assign(variable, initializerExpr);
    }

    public Expression VisitVariableExpr(Variable expr) {
        if (TryResolveVariable(expr.Name.Lexeme, out ParameterExpression variable)) {
            return variable;
        }
        throw Error(expr.Name, $"Undefined variable {expr.Name.Lexeme}");
    }

    public Expression VisitAssignExpr(Assign expr)
    {
        string varName = expr.Name.Lexeme;
        
        if (!TryResolveVariable(varName, out ParameterExpression variable)) {
            throw new Exception($"Undefined variable '{varName}'.");
        }
        
        Expression valueExpr = expr.Value.Accept(this);
        
        Type varType = variable.Type;
        if (valueExpr.Type != varType) {
            if (CanConvert(valueExpr.Type, varType)) {
                valueExpr = Expression.Convert(valueExpr, varType);
            }
            else {
                throw new Exception($"Cannot assign value of type '{valueExpr.Type}' to variable '{varName}' of type '{varType}'.");
            }
        }

        // Create the assignment expression
        return Expression.Assign(variable, valueExpr);
    }
    
    public Expression VisitBinaryExpr(Binary expr) {
        Expression left = expr.Left.Accept(this);
        Expression right = expr.Right.Accept(this);

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

    public Expression VisitGroupingExpr(Grouping expr) {
        return expr.Expr.Accept(this);
    }

    public Expression VisitLiteralExpr(Literal expr) {
        object? value = expr.Value;

        Type type = value switch {
            int => typeof(int),
            long => typeof(long),
            double => typeof(double),
            bool => typeof(bool),
            string => typeof(string),
            char => typeof(char),
            null => typeof(object),
            _ => throw Error(expr.Tkn, $"Unsupported literal type: {value?.GetType()}"),
        };

        return Expression.Constant(value, type);
    }

    public Expression VisitUnaryExpr(Unary expr)
    {
        Expression operand = expr.Right.Accept(this);

        switch (expr.Op.Type) {
            case MINUS:
                operand = EnsureNumericType(operand);
                return Expression.Negate(operand);
            case BANG:
                operand = EnsureBooleanType(operand);
                return Expression.Not(operand);
            case BIN_NOT:
                operand = EnsureIntegerType(operand);
                return Expression.OnesComplement(operand);
            default: throw Error(expr.Op, $"Unsupported unary operator: {expr.Op.Type}");
        }
    }

    public Expression VisitTypeCastExpr(TypeCast expr) {
        Expression inner = expr.Expr.Accept(this);
        Type targetType = ResolveType(expr.Type.Lexeme);

        return Expression.Convert(inner, targetType);
    }
}
