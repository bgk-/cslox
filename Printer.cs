using System.Text;

namespace cslox;

public class Printer : Expr.IVisitor<string>
{
    public string Print(Expr expr) => expr.Accept(this);

    private string Parenthesize(string name, params Expr[] exprs)
    {
        var builder = new StringBuilder();
        builder.Append('(').Append(name);
        foreach (var expr in exprs)
        {
            builder.Append(' ');
            builder.Append(expr.Accept(this));
        }
        builder.Append(')');
        return builder.ToString();
    }

    public string VisitAssignExpr(Expr.Assign expr) => Parenthesize($"{expr.Name} =", expr);
    public string VisitCallExpr(Expr.Call expr)
    {
        throw new NotImplementedException();
    }

    public string VisitGetExpr(Expr.Get expr)
    {
        throw new NotImplementedException();
    }

    public string VisitBinaryExpr(Expr.Binary expr) => Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
    public string VisitGroupingExpr(Expr.Grouping expr) => Parenthesize("group", expr.Expression);
    public string VisitLiteralExpr(Expr.Literal expr) => expr.Value?.ToString() ?? "Nil";
    public string VisitLogicalExpr(Expr.Logical expr) => Parenthesize(expr.Op.Lexeme, expr.Left, expr.Right);
    public string VisitSetExpr(Expr.Set expr)
    {
        throw new NotImplementedException();
    }

    public string VisitSuperExpr(Expr.Super expr)
    {
        throw new NotImplementedException();
    }

    public string VisitThisExpr(Expr.This expr)
    {
        throw new NotImplementedException();
    }

    public string VisitUnaryExpr(Expr.Unary expr) => Parenthesize(expr.Op.Lexeme, expr.Right);
    public string VisitVariableExpr(Expr.Variable expr) => Parenthesize(expr.Name.Lexeme, expr);
}