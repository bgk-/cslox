using cslox.tools;

namespace cslox;

public class Lox
{
    public struct Void {}

    public static readonly Void LoxVoid = new(); 
    private static bool hadError;
    private static bool hadRuntimeError;
    private static readonly Interpreter Interpreter = new();
    public static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.Contains("generateAst"))
            {
                GenerateAst.Generate(new[]{ arg });
                break;
            }
            if (arg.Contains("file"))
            {
                var file = arg.Split('=')[1];
                var path = $"/Users/bgk/RiderProjects/cslox/{file}";
                RunFile(path);
            }
        }
    }

    private static void RunFile(string path)
    {
        var text = File.ReadAllText(path);
        Run(text);
    }

    private static void Run(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);
        var statements = parser.Parse();
        if (hadError) System.Environment.Exit(65);
        var resolver = new Resolver(Interpreter);
        resolver.Resolve(statements);
        if (hadError) System.Environment.Exit(65);
        Interpreter.Interpret(statements);
        if (hadRuntimeError) System.Environment.Exit(70);
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    private static void Report(int line, string where, string message)
    {
        hadError = true;
        Console.WriteLine($"{line:0000}] Error {where}: {message}");
    }

    public static void Error(Token token, string message)
    {
        if (token.Type == TokenType.Eof) Report(token.Line, " at end", message);
        else Report(token.Line, $" at '{token.Lexeme}'", message);
    }

    public static void RuntimeError(RuntimeException error)
    {
        Console.WriteLine($"{error.Message}\n[line {error.Token.Line}");
        hadRuntimeError = true;
    }
}