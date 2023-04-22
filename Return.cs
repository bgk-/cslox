namespace cslox;

public class Return : RuntimeException
{
    public object? Value;

    public Return(object? value): base(null!, null!)
    {
        Value = value;
    }
}