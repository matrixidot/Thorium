namespace Thorium.API.Parsing;

using Lexing;
public interface ExprVisitor<R> {
	 R VisitAssignExpr (Assign expr);
	 R VisitBinaryExpr (Binary expr);
	 R VisitGroupingExpr (Grouping expr);
	 R VisitLiteralExpr (Literal expr);
	 R VisitUnaryExpr (Unary expr);
	 R VisitVariableExpr (Variable expr);
}

public abstract class Expr {

	public abstract R Accept<R>(ExprVisitor<R> visitor);
}

public class Assign(Token name, Expr value) : Expr {
	public Token Name { get; } = name;
	public Expr Value { get; } = value;

	public override R Accept<R>(ExprVisitor<R> visitor) {
		return visitor.VisitAssignExpr(this);
	}
}

public class Binary(Expr left, Token op, Expr right) : Expr {
	public Expr Left { get; } = left;
	public Token Op { get; } = op;
	public Expr Right { get; } = right;

	public override R Accept<R>(ExprVisitor<R> visitor) {
		return visitor.VisitBinaryExpr(this);
	}
}

public class Grouping(Expr expr) : Expr {
	public Expr Expr { get; } = expr;

	public override R Accept<R>(ExprVisitor<R> visitor) {
		return visitor.VisitGroupingExpr(this);
	}
}

public class Literal(Token tkn, object value) : Expr {
	public Token Tkn { get; } = tkn;
	public object Value { get; } = value;

	public override R Accept<R>(ExprVisitor<R> visitor) {
		return visitor.VisitLiteralExpr(this);
	}
}

public class Unary(Token op, Expr right) : Expr {
	public Token Op { get; } = op;
	public Expr Right { get; } = right;

	public override R Accept<R>(ExprVisitor<R> visitor) {
		return visitor.VisitUnaryExpr(this);
	}
}

public class Variable(Token name) : Expr {
	public Token Name { get; } = name;

	public override R Accept<R>(ExprVisitor<R> visitor) {
		return visitor.VisitVariableExpr(this);
	}
}

