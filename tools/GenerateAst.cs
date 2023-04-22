namespace cslox.tools;

public class GenerateAst
{
    private static readonly List<string> Expressions = new()
    {
        "Assign     : Token Name, Expr? Value",
        "Call       : Expr Callee, Token Paren, List<Expr?> Arguments",
        "Get        : Expr Obj, Token Name",
        "Binary     : Expr Left, Token Op, Expr Right",
        "Grouping   : Expr Expression",
        "Literal    : object? Value",
        "Logical    : Expr Left, Token Op, Expr Right",
        "Set        : Expr Obj, Token Name, Expr Value",
        "Super      : Token Key, Token Method",
        "This       : Token Key",
        "Unary      : Token Op, Expr Right",
        "Variable   : Token Name"
    };

    private static readonly List<string> Statements = new()
    {
        "Block      : List<Stmt?> Statements",
        "Class      : Token Name, Expr.Variable? Superclass, List<Stmt.Function> Methods",
        "Expression : Expr Express",
        "Function   : Token Name, List<Token> Parameters, List<Stmt?> Body",
        "If         : Expr Condition, Stmt ThenBranch, Stmt? ElseBranch",
        "Print      : Expr Express",
        "Return     : Token Key, Expr? Value",
        "Var        : Token Name, Expr? Initializer",
        "While      : Expr Condition, Stmt Body"
    };

    public static void Generate(string[] args)
    {
        if (args.Length != 1) System.Environment.Exit(64);
        var outputDir = args[0];
        DefineAst(outputDir, "Expr", Expressions);
        DefineAst(outputDir, "Stmt", Statements);
    }

    private static void DefineAst(string outputDir, string baseName, List<string> list)
    {
        var path = $"/Users/bgk/RiderProjects/cslox/{baseName}.cs";
        var writer = new StringWriter();
        
        writer.WriteLine("namespace cslox;");
        writer.WriteLine();
        writer.WriteLine($"public abstract class {baseName}");
        writer.WriteLine("{");
        writer.WriteLine($"    public abstract T Accept<T>(IVisitor<T> visitor);");
        writer.WriteLine($"    public interface IVisitor<out T>");
        writer.WriteLine("     {");
        foreach (var str in list)
        {
            var className = str.Split(':')[0].Trim();
            writer.WriteLine($"        T Visit{className}{baseName}({className} {baseName.ToLower()});");
        }
        writer.WriteLine("    }");
        
        foreach (var str in list)
        {
            var className = str.Split(':')[0].Trim();
            var fields = str.Split(':')[1].Trim();
            DefineType(writer, baseName, className, fields);
        }
        
        writer.WriteLine("}");
        File.WriteAllText(path, writer.ToString());
        writer.Close();
    }

    private static void DefineType(StringWriter writer, string baseName, string className, string fields)
    {
        writer.WriteLine($"    public class {className} : {baseName}");
        writer.WriteLine("    {");
        var fieldList = fields.Split(',');
        foreach (var field in fieldList)
        {
            writer.WriteLine($"        public readonly {field.Trim()};");
        }
        writer.WriteLine($"        public {className}({string.Join(", ", fields.Split(',').Select(f => $"{f.Trim().Split(' ')[0]} {f.Trim().Split(' ')[1].ToLower()}"))})");
        writer.WriteLine("        {");
        foreach (var field in fieldList)
        {
            var name = field.Trim().Split(' ')[1];
            writer.WriteLine($"            {name} = {name.ToLower()};");
        }
        writer.WriteLine("        }");
        writer.WriteLine($"        public override T Accept<T>(IVisitor<T> visitor)");
        writer.WriteLine("        {");
        writer.WriteLine($"            return visitor.Visit{className}{baseName}(this);");
        writer.WriteLine("        }");
        writer.WriteLine("    }");
        writer.WriteLine();
    }
}