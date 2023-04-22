namespace cslox;

public class LoxClass : ILoxCallable
{
    public readonly string Name;
    public readonly LoxClass? Superclass;
    private readonly Dictionary<string, LoxFunction> _methods;
    public LoxClass(string name, LoxClass? superclass, Dictionary<string, LoxFunction> methods)
    {
        Name = name;
        Superclass = superclass;
        _methods = methods;
    }

    public override string ToString() => Name;
    public int Arity => FindMethod("init")?.Arity ?? 0;

    public object Call(Interpreter interpreter, List<object?> arguments)
    {
        var instance = new LoxInstance(this);
        var initializer = FindMethod("init");
        initializer?.Bind(instance).Call(interpreter, arguments);
        return instance;
    }

    public LoxFunction? FindMethod(string name)
    {
        return _methods.TryGetValue(name, out var method) ? method : Superclass?.FindMethod(name);
    }
}

public class LoxInstance
{
    private LoxClass _loxClass;
    private readonly Dictionary<string, object?> _fields = new();
    public LoxInstance(LoxClass loxClass)
    {
        _loxClass = loxClass;
    }

    public override string ToString() => $"{_loxClass.Name} instance";

    public object? Get(Token token)
    {
        if (_fields.TryGetValue(token.Lexeme, out var value)) return value;
        var method = _loxClass.FindMethod(token.Lexeme);
        if (method != null) return method.Bind(this);
        throw new RuntimeException(token, $"Undefined property {token.Lexeme}.");
    }

    public void Set(Token token, object? value)
    {
        _fields[token.Lexeme] = value;
    }
}