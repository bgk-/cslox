namespace cslox;

public class Environment
{
    public readonly Environment? Enclosing;
    private readonly Dictionary<string, object?> _values = new();

    public Environment(Environment? enclosing = null)
    {
        Enclosing = enclosing;
    }
    
    public void Define(string name, object? value)
    {
        _values.TryAdd(name, value);
    }

    public object? Get(Token token)
    {
        if (_values.TryGetValue(token.Lexeme, out var value))
            return value;
        if (Enclosing != null) return Enclosing.Get(token);
        throw new RuntimeException(token, $"Undefined variable {token.Lexeme}.");
    }

    public void Assign(Token token, object? value)
    {
        if (_values.ContainsKey(token.Lexeme))
        {
            _values[token.Lexeme] = value;
            return;
        }

        if (Enclosing != null)
        {
            Enclosing.Assign(token, value);
            return;
        }

        throw new RuntimeException(token, $"Undefined variables {token.Lexeme}.");
    }

    public object? GetAt(int dist, string name)
    {
        var ancestor = Ancestor(dist);
        if (ancestor != null && ancestor._values.TryGetValue(name, out var value)) return value;
        return null;
    }

    private Environment? Ancestor(int dist)
    {
        var env = this;
        for (var i = 0; i < dist; i++)
        {
            env = env?.Enclosing;
        }

        return env;
    }

    public void AssignAt(int dist, Token token, object? obj)
    {
        var values = Ancestor(dist)?._values;
        if (values == null) return;
        values[token.Lexeme] = obj;
    }
}