namespace Thorium;

using API.Emit;
using API.Errors;
using API.Lexing;
using API.Parsing;
using static API.Lexing.TokenType;

public class Thorium {
    private static readonly ILRunner runner = new();
    private static bool HadError;
    private static bool HadRuntimeError;

    public static double timer;
    public static bool timing = false;
    public static void Main(string[] args)
    {
        if (args.Length >= 1) {
            timing = bool.Parse(args[1]);
            string source = File.ReadAllText(args[0]);
            Run(source);
            Console.WriteLine("\nCompleted... Press any key to exit.");
            Console.ReadKey();
        }
        else {
            REPL.Start();
        }
    }
    
    public static void Run(string source) {
        InitTimer();
        Lexer lexer = new Lexer(source);
        List<Token> tokens = lexer.LexSource();
        Time($"Lexing done: {tokens.Count} tokens found.");
        Parser parser = new Parser(tokens);
        List<Stmt> stmts = parser.Parse();
        Time($"Parsing done: {stmts.Count} statements made");
        if (HadError) return;

        try {
            runner.CompileAndRun(stmts);
        }
        catch (Exception e) {
            Console.WriteLine($"Runtime Error: {e.Message}");
        }

        HadError = false;
    }
    
    public static void RuntimeError(RuntimeError error) {
        Console.Error.WriteLine($"[Line {error.Token.Line}]\n{error.Message}");
        HadRuntimeError = true;
    }
    public static void Error(int line, string message) => Report(line, "", message);
    public static void Error(Token token, string message) => Report(token.Line, token.Type == EOF ? "at end" : $"at '{token.Lexeme}'", message);
    private static void Report(int line, string where, string message) {
        Console.Error.WriteLine($"Error [Line {line}]: {(where.Length > 0 ? $" {where}" : "")}: {message}");
        HadError = true;
    }

    public static void Time(string message) {
        if (!timing) return;
        Console.WriteLine($"{message}\nCompleted in {(double) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0 - timer} seconds.");
        timer = (double) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0;
    }

    public static void InitTimer() {
        timer = (double) DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 1000.0;
    }
}
