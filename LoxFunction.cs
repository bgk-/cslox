namespace cslox;

public class LoxFunction : ILoxCallable
{
    private readonly Stmt.Function _declaration;
    private readonly Environment _closure;
    private readonly bool _isInitializer;
    public int Arity => _declaration.Parameters.Count;

    public LoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer)
    {
        _declaration = declaration;
        _closure = closure;
        _isInitializer = isInitializer;
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        var env = new Environment(_closure);
        env.Define("this", instance);
        return new LoxFunction(_declaration, env, _isInitializer);
    }
    
    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var env = new Environment(_closure);
        for (var i = 0; i < _declaration.Parameters.Count; i++)
        {
            env.Define(_declaration.Parameters[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(_declaration.Body, env);
        }
        catch (Return returnValue)
        {
            if (_isInitializer) return _closure.GetAt(0, "this");
            return returnValue.Value;
        }

        if (_isInitializer) return _closure.GetAt(0, "this");
        return null;
    }

    public override string ToString() => $"<fn {_declaration.Name.Lexeme}>";
}