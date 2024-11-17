namespace Thorium.API.Parsing;

using Lexing;
public interface StmtVisitor<R> {
	 R VisitExpressionStmt (Expression stmt);
	 R VisitVarStmt (Var stmt);
	 R VisitPrintStmt (Print stmt);
}

public abstract class Stmt {

	public abstract R Accept<R>(StmtVisitor<R> visitor);
}

public class Expression(Expr expr) : Stmt {
	public Expr Expr { get; } = expr;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitExpressionStmt(this);
	}
}

public class Var(Type typ, Token name, Expr initializer) : Stmt {
	public Type Typ { get; } = typ;
	public Token Name { get; } = name;
	public Expr Initializer { get; } = initializer;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitVarStmt(this);
	}
}

public class Print(Expr expr) : Stmt {
	public Expr Expr { get; } = expr;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitPrintStmt(this);
	}
}

