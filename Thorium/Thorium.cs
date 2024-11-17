namespace Thorium;

using System.Diagnostics;
using API.Emit;
using API.Errors;
using API.Lexing;
using API.Parsing;
using static API.Lexing.TokenType;

public class Thorium {
    private static readonly ILRunner runner = new();
    private static bool HadError;
    private static bool HadRuntimeError;

    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            RunScript(args[0]);
        }
        else if (args.Length == 0)
        {
            StartRepl();
        }
        else
        {
            Console.WriteLine("Usage: Thorium [script]");
            Environment.Exit(64);
        }
    }

    private static void StartRepl()
    {
        Console.WriteLine("Thorium REPL. Type 'exit' to quit.");
        while (true)
        {
            Console.Write("th> ");
            string line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            if (line.ToLower() == "exit")
            {
                break;
            }
            if (line.StartsWith(".run "))
            {
                string path = line.Substring(5).Trim();
                RunScriptInNewWindow(path);
            }
            else if (line.ToLower() == ".clear")
            {
                Console.Clear();
            }
            else
            {
                Run(line);
            }
            HadError = false;
        }
    }

    private static void RunScript(string path)
    {
        if (!Path.Exists(path))
        {
            Console.WriteLine("Invalid path. Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(65);
        }
        string script = File.ReadAllText(path);
        Run(script);

        if (HadError)
        {
            Console.WriteLine("Errored while running script, press any key to continue");
            Console.ReadKey();
            Environment.Exit(65);
        }
        if (HadRuntimeError)
        {
            Console.WriteLine("Errored while running script, press any key to continue");
            Console.ReadKey();
            Environment.Exit(70);
        }

        Console.WriteLine("\n\nFinished, press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static void Run(string source)
    {
        Lexer lexer = new Lexer(source);
        List<Token> tokens = lexer.LexSource();
        Parser parser = new Parser(tokens);
        List<Stmt> stmts = parser.Parse();

        if (HadError) return;

        // Generate and run the IL code from the parsed expression
        try {
            runner.Run(stmts);
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

    }

    private static void RunScriptInNewWindow(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: Could not find file '{path}'");
            return;
        }

        try
        {
            // Start a new process with the current executable and the script path
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Environment.ProcessPath;
            startInfo.Arguments = $"\"{path}\"";
            startInfo.UseShellExecute = true;  // This opens in a new window
            startInfo.WindowStyle = ProcessWindowStyle.Normal;

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching script: {ex.Message}");
        }
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
}
