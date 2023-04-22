namespace cslox;

public class Resolver : Expr.IVisitor<Lox.Void>, Stmt.IVisitor<Lox.Void>
{
    private readonly Interpreter _interpreter;
    private readonly List<Dictionary<string, bool>> _scopes = new();

    private enum FunctionType
    {
        None,
        Function,
        Initializer,
        Method
    }

    private enum ClassType
    {
        None,
        Class,
        Subclass
    }
    
    private FunctionType _currentFunction = FunctionType.None;
    private ClassType _currentClass = ClassType.None;
    public Resolver(Interpreter interpreter)
    {
        _interpreter = interpreter;
    }
    
    public void Resolve(List<Stmt?> statements)
    {
        foreach (var stmt in statements) Resolve(stmt);
    }

    private void Resolve(Stmt? stmt) => stmt?.Accept(this);
    private void Resolve(Expr? expr) => expr?.Accept(this);

    private void BeginScope()
    {
        _scopes.Add(new Dictionary<string, bool>());
    }

    private void EndScope()
    {
        _scopes.RemoveAt(_scopes.Count - 1);
    }

    private void Declare(Token token)
    {
        if (_scopes.Count == 0) return;
        var scope = _scopes[^1];
        if (scope.ContainsKey(token.Lexeme)) Lox.Error(token, "Already a variable with this name in this scope.");
        scope.Add(token.Lexeme, false);
    }

    private void Define(Token token)
    {
        if (_scopes.Count == 0) return;
        var scope = _scopes[^1];
        if (scope.ContainsKey(token.Lexeme)) scope[token.Lexeme] = true;
        else Lox.Error(token, "Variable has not been declared.");
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].ContainsKey(name.Lexeme))
            {
                _interpreter.Resolve(expr, _scopes.Count - 1 - i);
                return;
            }
        }
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        var enclosingFunction = _currentFunction;
        _currentFunction = type;
        
        BeginScope();
        foreach (var param in function.Parameters)
        {
            Declare(param);
            Define(param);
        }
        Resolve(function.Body);
        EndScope();
        _currentFunction = enclosingFunction;
    }
    
    public Lox.Void VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);
        foreach (var arg in expr.Arguments) Resolve(arg);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Obj);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitLiteralExpr(Expr.Literal expr)
    {
        return Lox.LoxVoid;
    }

    public Lox.Void VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Obj);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitSuperExpr(Expr.Super expr)
    {
        if (_currentClass == ClassType.None)
        {
            Lox.Error(expr.Key, "Can't use 'super' outside of a class.");
        } else if (_currentClass != ClassType.Subclass)
        {
            Lox.Error(expr.Key, "Can't use 'super' in a class with no superclass.");
        }
        ResolveLocal(expr, expr.Key);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitThisExpr(Expr.This expr)
    {
        if (_currentClass == ClassType.None)
        {
            Lox.Error(expr.Key, "Can't use 'this' outside of a class.");
            return Lox.LoxVoid;
        }
        ResolveLocal(expr, expr.Key);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitVariableExpr(Expr.Variable expr)
    {
        if (_scopes.Count != 0)
        {
            if (_scopes[^1].TryGetValue(expr.Name.Lexeme, out var value) && value == false)
                Lox.Error(expr.Name, "Can't read local variable in its own initializer.");
        }
        ResolveLocal(expr, expr.Name);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return Lox.LoxVoid;
    }

    public Lox.Void VisitClassStmt(Stmt.Class stmt)
    {
        var enclosingClass = _currentClass;
        _currentClass = ClassType.Class;
        Declare(stmt.Name);
        Define(stmt.Name);

        if (stmt.Superclass != null)
        {
            _currentClass = ClassType.Subclass;
            if (stmt.Name.Lexeme.Equals(stmt.Superclass.Name.Lexeme)) Lox.Error(stmt.Superclass.Name, "A class can't inherit from itself.");
            Resolve(stmt.Superclass);
            BeginScope();
            _scopes[^1]["super"] = true;
        }
        
        BeginScope();
        _scopes[^1]["this"] = true;
        
        foreach (var method in stmt.Methods)
        {
            var decl = FunctionType.Method;
            if (method.Name.Lexeme.Equals("init"))
            {
                decl = FunctionType.Initializer;
            }
            ResolveFunction(method, decl);
        }
        EndScope();

        if (stmt.Superclass != null) EndScope();
        _currentClass = enclosingClass;
        return Lox.LoxVoid;
    }

    public Lox.Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Express);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);
        ResolveFunction(stmt, FunctionType.Function);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch != null) Resolve(stmt.ElseBranch);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Express);
        return Lox.LoxVoid;
    }

    public Lox.Void VisitReturnStmt(Stmt.Return stmt)
    {
        if (_currentFunction == FunctionType.None) Lox.Error(stmt.Key, "Can't return from top-level code.");
        if (stmt.Value != null)
        {
            if (_currentFunction == FunctionType.Initializer)
            {
                Lox.Error(stmt.Key, "Can't return a value from an initializer.");
            }
            Resolve(stmt.Value);
        }
        return Lox.LoxVoid;
    }

    public Lox.Void VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer != null)
        {
            Resolve(stmt.Initializer);
        }
        Define(stmt.Name);
        return Lox.LoxVoid;
    }


    public Lox.Void VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return Lox.LoxVoid;
    }
    
}