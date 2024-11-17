namespace Thorium.API.Emit;

using System.Linq.Expressions;
using Errors;
using Parsing;
using Expression = System.Linq.Expressions.Expression;

public class ILRunner {
    private readonly Emitter emitter = new Emitter();
    private readonly List<Expression> expressions = [];
    public void Run(List<Stmt> statements) {
        try {
            foreach (Stmt statement in statements) {
                Expression expr = Execute(statement, emitter);
                expressions.Add(expr);
            }

            List<ParameterExpression> variables = emitter.GlobalVars;
            BlockExpression block = Expression.Block(variables, expressions);
            
            if (block.Type == typeof(void))
            {
                Expression<Action> lambda = Expression.Lambda<Action>(block);
                Action compiled = lambda.Compile();
                compiled();
            }
            else
            {
                Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(Expression.Convert(block, typeof(object)));
                Func<object> compiled = lambda.Compile();
                object result = compiled();
                Console.WriteLine(result);
            }
        }
        catch (Exception e)
        {
            if (e is RuntimeError re)
            {
                Thorium.RuntimeError(re);
            }
            else
            {
                Console.WriteLine($"Runtime Error: {e.Message}");
            }
        }
    }
    
    private Expression Execute(Stmt stmt, Emitter emitter)
    {
        return stmt.Accept(emitter);
    }
}


