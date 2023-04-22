using System.Reflection.Metadata;

namespace cslox;

public class RuntimeException : Exception
{
    public readonly Token Token;

    public RuntimeException(Token token, string message) : base(message)
    {
        Token = token;
    }
}

public class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<Lox.Void>
{
    public readonly Environment Globals = new();
    private readonly Dictionary<Expr, int> _locals = new();
    private Environment _environment;

    public Interpreter()
    {
        _environment = Globals;
        Globals.Define("clock", new LoxCallable
        (
            (_, __) => DateTime.Now.Ticks / (double)TimeSpan.TicksPerMillisecond,
            () => 0,
            () => "<native fn>"
        ));
    }
    public void Interpret(List<Stmt?> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeException e)
        {
            Lox.RuntimeError(e);
        }
    }

    private void Execute(Stmt? statement)
    {
        statement?.Accept(this);
    }

    public void Resolve(Expr expr, int depth)
    {
        _locals.Add(expr, depth);
    }
    
    private string Stringify(object? obj)
    {
        if (obj is null) return "nil";
        if (obj is double)
        {
            var text = obj.ToString() ?? string.Empty;
            if (text.EndsWith(".0"))
            {
                text = text[..^2];
            }
            return text;
        }

        return obj.ToString()!;
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        var value = Evaluate(expr.Value);

        if (_locals.TryGetValue(expr, out var dist))
            _environment.AssignAt(dist, expr.Name, value);
        else
            Globals.Assign(expr.Name, value);

        return value;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        var callee = Evaluate(expr.Callee);
        var arguments = new List<object?>();
        foreach (var arg in expr.Arguments)
        {
            arguments.Add(Evaluate(arg));
        }

        if (callee is not ILoxCallable function)
        {
            throw new RuntimeException(expr.Paren, "Can only call functions and classes");
        }

        if (arguments.Count != function.Arity)
        {
            throw new RuntimeException(expr.Paren, $"Expected {function.Arity} arguments but got {arguments.Count}");
        }
        return function.Call(this, arguments);
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        var obj = Evaluate(expr.Obj);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }

        throw new RuntimeException(expr.Name, "Only instances have properties.");
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);
        if (left == null || right == null) return null;
        switch (expr.Op.Type)
        {
            case TokenType.Greater:
                CheckNumbers(expr.Op, left, right);
                return (double)left > (double)right;
            case TokenType.GreaterEqual:
                CheckNumbers(expr.Op, left, right);
                return (double)left >= (double)right;
            case TokenType.Less:
                CheckNumbers(expr.Op, left, right);
                return (double)left < (double)right;
            case TokenType.LessEqual:
                CheckNumbers(expr.Op, left, right);
                return (double)left <= (double)right;
            case TokenType.BangEqual:
                return !IsEqual(left, right);
            case TokenType.Equal: return IsEqual(left, right);
            case TokenType.Minus:
            {
                CheckNumber(expr.Op, right);
                return (double)left - (double)right;
            }
            case TokenType.Slash:
                CheckNumbers(expr.Op, left, right);
                return (double)left / (double)right;
            case TokenType.Star: 
                CheckNumbers(expr.Op, left, right);
                return (double)left * (double)right;
            case TokenType.Plus:
                if (left is double ld && right is double rd) return ld + rd;
                if (left is string ls && right is string rs) return ls + rs;
                throw new RuntimeException(expr.Op, "Operands must be two numbers or two strings"); 
        }

        return null;
    }

    private void CheckNumber(Token op, object? right)
    {
        if (right is double) return;
        throw new RuntimeException(op, "Operant must be a number.");
    }

    private void CheckNumbers(Token op, object left, object right)
    {
        if (left is double && right is double) return;
        throw new RuntimeException(op, "Operands must be numbers.");
    }

    public object? VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

    private object? Evaluate(Expr? expr) => expr?.Accept(this);

    public object? VisitLiteralExpr(Expr.Literal expr) => expr.Value;
    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        var left = Evaluate(expr.Left);
        if (expr.Op.Type == TokenType.Or)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(expr.Right);
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
        var obj = Evaluate(expr.Obj);
        if (obj is not LoxInstance instance)
        {
            throw new RuntimeException(expr.Name, "Only instances have fields.");
        }

        var value = Evaluate(expr.Value);
        instance.Set(expr.Name, value);
        return value;
    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
        if (!_locals.TryGetValue(expr, out var dist)) return null;
        var superclass = _environment.GetAt(dist, "super") as LoxClass;
        var obj = _environment.GetAt(dist - 1, "this") as LoxInstance;
        var method = superclass.FindMethod(expr.Method.Lexeme);
        if (method == null) throw new RuntimeException(expr.Method, $"Undefined property {expr.Method.Lexeme}.");
        return method.Bind(obj);
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        return LookUpVariable(expr.Key, expr);
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        var right = Evaluate(expr.Right);
        if (right == null) return null;
        return expr.Op.Type switch
        {
            TokenType.Bang => IsTruthy(right),
            TokenType.Minus => -(double)right,
            _ => null
        };
    }

    public object? VisitVariableExpr(Expr.Variable expr) => LookUpVariable(expr.Name, expr);

    private object? LookUpVariable(Token name, Expr expr)
    {
        if (_locals.TryGetValue(expr, out var dist)) return _environment.GetAt(dist, name.Lexeme);
        return Globals.Get(name);

    }

    private bool IsTruthy(object? obj)
    {
        if (obj is null) return false;
        if (obj is bool b) return b;
        return true;
    }

    private bool IsEqual(object? a, object? b)
    {
        return a switch
        {
            null when b == null => true,
            null => false,
            _ => a.Equals(b)
        };
    }

    public Lox.Void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return Lox.LoxVoid;
    }

    public Lox.Void VisitClassStmt(Stmt.Class stmt)
    {
        object? superclass = null;
        if (stmt.Superclass != null)
        {
            superclass = Evaluate(stmt.Superclass);
            if (superclass is not LoxClass)
            {
                throw new RuntimeException(stmt.Superclass.Name, "Superclass must be a class.");
            }
        }
        
        _environment.Define(stmt.Name.Lexeme, null);

        if (stmt.Superclass != null)
        {
            _environment = new Environment(_environment);
            _environment.Define("super", superclass);
        }
        
        var methods = new Dictionary<string, LoxFunction>();
        foreach (var method in stmt.Methods)
        {
            var function = new LoxFunction(method, _environment, method.Name.Lexeme.Equals("init"));
            methods.Add(method.Name.Lexeme, function);
        }
        var klass = new LoxClass(stmt.Name.Lexeme, superclass as LoxClass, methods);

        if (superclass != null)
        {
            _environment = _environment.Enclosing!;
        }
        _environment.Assign(stmt.Name, klass);
        return Lox.LoxVoid;
    }

    public void ExecuteBlock(List<Stmt?> stmtStatements, Environment environment)
    {
        var previous = _environment;
        try
        {
            _environment = environment;
            foreach (var statement in stmtStatements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }


    public Lox.Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Express);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitFunctionStmt(Stmt.Function stmt)
    {
        var function = new LoxFunction(stmt, _environment, false);
        _environment.Define(stmt.Name.Lexeme, function);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        } else if (stmt.ElseBranch != null)
        {
            Execute(stmt.ThenBranch);
        }

        return Lox.LoxVoid;
    }

    public Lox.Void VisitPrintStmt(Stmt.Print stmt)
    {
        var value = Evaluate(stmt.Express);
        Console.WriteLine(Stringify(value));
        return Lox.LoxVoid;
    }

    public Lox.Void VisitReturnStmt(Stmt.Return stmt)
    {
        object? value = null;
        if (stmt.Value != null) value = Evaluate(stmt.Value);
        throw new Return(value);
    }

    public Lox.Void VisitVarStmt(Stmt.Var stmt)
    {
        object? value = null;
        if (stmt.Initializer != null)
        {
            value = Evaluate(stmt.Initializer);
        }
        _environment.Define(stmt.Name.Lexeme, value);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }

        return Lox.LoxVoid;
    }
}

public class LoxCallable : ILoxCallable
{
    private readonly Func<Interpreter, List<object?>, object> _callFunc;
    private readonly Func<int> _arityFunc;
    private readonly Func<string> _toStringFunc;

    public LoxCallable(Func<Interpreter, List<object?>, object> callFunc, Func<int> arityFunc, Func<string> toStringFunc)
    {
        _callFunc = callFunc;
        _arityFunc = arityFunc;
        _toStringFunc = toStringFunc;
    }

    public int Arity => _arityFunc(); 
    public object Call(Interpreter interpreter, List<object?> arguments) => _callFunc(interpreter, arguments);
    public override string ToString() => _toStringFunc();
}

public interface ILoxCallable
{
    int Arity { get; }
    object? Call(Interpreter interpreter, List<object?> arguments);
}