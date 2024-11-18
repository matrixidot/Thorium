namespace Thorium.API.Parsing;

using Lexing;
public interface StmtVisitor<R> {
	 R VisitBlockStmt (Block stmt);
	 R VisitExprStmtStmt (ExprStmt stmt);
	 R VisitVarStmt (Var stmt);
	 R VisitPrintStmt (Print stmt);
}

public abstract class Stmt {

	public abstract R Accept<R>(StmtVisitor<R> visitor);
}

public class Block(List<Stmt> statements) : Stmt {
	public List<Stmt> Statements { get; } = statements;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitBlockStmt(this);
	}
}

public class ExprStmt(Expr expr) : Stmt {
	public Expr Expr { get; } = expr;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitExprStmtStmt(this);
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

