namespace Thorium.API.Parsing;

using Errors;
using Lexing;
using static Lexing.TokenType;

public class Parser(List<Token> Tokens) {
    private int current = 0;

    public List<Stmt> Parse() {
        List<Stmt> statements = new();
        while (!IsAtEnd) {
            statements.Add(Declaration());
        }
        return statements;
    }

    private Stmt Declaration() {
        try {
            if (Match(INT)) return VarDeclaration(typeof(int));
            if (Match(LONG)) return VarDeclaration(typeof(long));
            if (Match(DOUBLE)) return VarDeclaration(typeof(double));
            if (Match(BOOL)) return VarDeclaration(typeof(bool));
            if (Match(STRING_TYPE)) return VarDeclaration(typeof(string));
            if (Match(CHAR_TYPE)) return VarDeclaration(typeof(char));
            if (Match(OBJECT)) return VarDeclaration(typeof(object));

            return Statement();
        }
        catch (ParseError error) {
            Synchronize();
            return null;
        }
    }
    
    private Stmt Statement() {
        if (Match(PRINT)) return PrintStatement();
        if (Match(L_BRACE)) return new Block(Block());
        return ExpressionStatement();
    }

    private List<Stmt> Block() {
        List<Stmt> statements = [];
        while (!Check(R_BRACE) && !IsAtEnd) {
            statements.Add(Declaration());
        }
        Consume(R_BRACE, "Expect '}' after block.");
        return statements;
    }

    private Print PrintStatement() {
        Expr value = Expression();
        Consume(SEMICOLON, "Expected ';' after value.");
        return new Print(value);
    }
    
    private ExprStmt ExpressionStatement() {
        Expr expr = Expression();
        Consume(SEMICOLON, "Expect ';' after expression.");
        return new ExprStmt(expr);

    }

    private Var VarDeclaration(Type type) {
        Token name = Consume(IDENTIFIER, "Expect variable name.");
        Expr initializer = null;
        if (Match(EQUAL)) {
            initializer = Expression();
        }
        
        Consume(SEMICOLON, "Expect ';' after variable declaration.");
        return new Var(type, name, initializer);
    }
    
    private Expr Expression() {
        return Assignment();
    }

    private Expr Assignment() {
        Expr expr = Equality();
        
        if (Match(EQUAL, PLUS_EQUAL, MINUS_EQUAL, MULT_EQUAL, DIV_EQUAL, MOD_EQUAL, POW_EQUAL)) {
            Token op = Previous;
            Expr value = Assignment();

            if (expr is Variable var) {
                Token name = var.Name;

                if (op.Type != EQUAL) {
                    TokenType baseOpType = op.Type switch {
                        PLUS_EQUAL => PLUS,
                        MINUS_EQUAL => MINUS,
                        MULT_EQUAL => MULT,
                        DIV_EQUAL => DIV,
                        MOD_EQUAL => MOD,
                        POW_EQUAL => POW,
                        _ => throw Error(op, "Unknown compound assignment operator."),
                    };

                    Expr compoundValue = new Binary(var, op with { Type = baseOpType, Literal = null }, value);
                    return new Assign(name, compoundValue);
                }
                return new Assign(name, value);
            }
            Error(op, "Invalid assignment target.");
        }
        return expr;
    }

    private Expr Equality() {
        Expr expr = Comparison();

        while (Match(BANG_EQUAL, EQUAL_EQUAL)) {
            Token op = Previous;
            Expr right = Comparison();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }

    private Expr Comparison() {
        Expr expr = BitwiseOr();

        while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL)) {
            Token op = Previous;
            Expr right = BitwiseOr();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr BitwiseOr() {
        Expr expr = BitwiseXor();

        while (Match(BIN_OR)) {
            Token op = Previous;
            Expr right = BitwiseXor();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr BitwiseXor() {
        Expr expr = BitwiseAnd();

        while (Match(BIN_XOR)) {
            Token op = Previous;
            Expr right = BitwiseAnd();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr BitwiseAnd() {
        Expr expr = Shift();

        while (Match(BIN_AND)) {
            Token op = Previous;
            Expr right = Shift();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr Shift() {
        Expr expr = Term();

        while (Match(LEFT_SHIFT, RIGHT_SHIFT)) {
            Token op = Previous;
            Expr right = Term();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }
    
    private Expr Term() {
        Expr expr = Factor();

        while (Match(MINUS, PLUS)) {
            Token op = Previous;
            Expr right = Factor();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr Factor() {
        Expr expr = Exponent();
        while (Match(MULT, DIV, MOD)) {
            Token op = Previous;
            Expr right = Exponent();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr Exponent() {
        Expr expr = Unary();

        while (Match(POW)) {
            Token op = Previous;
            Expr right = Unary();
            expr = new Binary(expr, op, right);
        }
        return expr;
    }
    
    private Expr Unary() {
        if (Match(INCREMENT, DECREMENT)) {
            Token op = Previous;
            Expr right = Unary();

            if (right is Variable var) {
                return new IncDec(op, var, isPrefix: true);
            }
            throw Error(op, "Invalid increment/decrement target.");
        }
        
        if (!Match(BANG, MINUS, BIN_NOT)) return Postfix(); {
            Token op = Previous;
            Expr right = Unary();
            return new Unary(op, right);
        }
    }
    
    private Expr Postfix() {
        Expr expr = Primary();
        
        if (!Match(INCREMENT, DECREMENT)) return expr;
        Token op = Previous;
        
        if (expr is Variable var) {
            return new IncDec(op, var, isPrefix: false);
        }
        
        throw Error(op, "Invalid increment/decrement target.");
    }

    private Expr Primary() {
        if (Match(FALSE)) return new Literal(Previous, false);
        if (Match(TRUE)) return new Literal(Previous, true);
        if (Match(NULL)) return new Literal(Previous, null);
        if (Match(NUMBER, STRING_LIT, CHAR_LIT)) return new Literal(Previous, Previous.Literal);
        if (Match(IDENTIFIER)) return new Variable(Previous);
        


        if (Match(L_PAREN)) {
            Expr expr = Expression();
            Consume(R_PAREN, "Expect ')' after expression.");
            return new Grouping(expr);
        }
        throw Error(Peek, "Expect expression.");
    }

    private Token Consume(TokenType type, string text) {
        if (Check(type)) return Advance();

        throw Error(Peek, text);
    }
    
    private bool Match(params TokenType[] types) {
        foreach (TokenType type in types) {
            if (Check(type)) {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool Check(TokenType type) {
        if (IsAtEnd) return false;
        return Peek.Type == type;
    }

    private Token Advance() {
        if (!IsAtEnd) current++;
        return Previous;
    }

    private ParseError Error(Token token, string message) {
        Thorium.Error(token, message);
        return new ParseError();
    }

    private void Synchronize() {
        Advance();

        while (!IsAtEnd) {
            if (Previous.Type == SEMICOLON) return;

            switch (Peek.Type) {
                case IF or ELIF or ELSE or FOR or WHILE or CLASS or RETURN or BREAK or CONTINUE or INT or LONG or DOUBLE or OBJECT or STRING_TYPE or CHAR_TYPE or BOOL: return;
                    
            }
            Advance();
        }
    }
    
    private bool IsAtEnd => Peek.Type == EOF;
    private Token Peek => Tokens[current];
    private Token Previous => Tokens[current - 1];
    
}