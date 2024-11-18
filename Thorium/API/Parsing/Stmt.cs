namespace Thorium.API.Parsing;

using Lexing;
public interface StmtVisitor<R> {
	 R VisitBlockStmt (Block stmt);
	 R VisitExprStmtStmt (ExprStmt stmt);
	 R VisitVarStmt (Var stmt);
	 R VisitPrintStmt (Print stmt);
	 R VisitIfStmt (If stmt);
	 R VisitElifStmt (Elif stmt);
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

public class If(Expr condition, Stmt thenBranch, List<Elif> elifBranches, Stmt elseBranch) : Stmt {
	public Expr Condition { get; } = condition;
	public Stmt ThenBranch { get; } = thenBranch;
	public List<Elif> ElifBranches { get; } = elifBranches;
	public Stmt ElseBranch { get; } = elseBranch;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitIfStmt(this);
	}
}

public class Elif(Expr condition, Stmt branch) : Stmt {
	public Expr Condition { get; } = condition;
	public Stmt Branch { get; } = branch;

	public override R Accept<R>(StmtVisitor<R> visitor) {
		return visitor.VisitElifStmt(this);
	}
}

