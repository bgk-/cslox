namespace cslox;

public abstract class Stmt
{
    public abstract T Accept<T>(IVisitor<T> visitor);
    public interface IVisitor<out T>
     {
        T VisitBlockStmt(Block stmt);
        T VisitClassStmt(Class stmt);
        T VisitExpressionStmt(Expression stmt);
        T VisitFunctionStmt(Function stmt);
        T VisitIfStmt(If stmt);
        T VisitPrintStmt(Print stmt);
        T VisitReturnStmt(Return stmt);
        T VisitVarStmt(Var stmt);
        T VisitWhileStmt(While stmt);
    }
    public class Block : Stmt
    {
        public readonly List<Stmt?> Statements;
        public Block(List<Stmt?> statements)
        {
            Statements = statements;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }
    }

    public class Class : Stmt
    {
        public readonly Token Name;
        public readonly Expr.Variable? Superclass;
        public readonly List<Stmt.Function> Methods;
        public Class(Token name, Expr.Variable? superclass, List<Stmt.Function> methods)
        {
            Name = name;
            Superclass = superclass;
            Methods = methods;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitClassStmt(this);
        }
    }

    public class Expression : Stmt
    {
        public readonly Expr Express;
        public Expression(Expr express)
        {
            Express = express;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }
    }

    public class Function : Stmt
    {
        public readonly Token Name;
        public readonly List<Token> Parameters;
        public readonly List<Stmt?> Body;
        public Function(Token name, List<Token> parameters, List<Stmt?> body)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitFunctionStmt(this);
        }
    }

    public class If : Stmt
    {
        public readonly Expr Condition;
        public readonly Stmt ThenBranch;
        public readonly Stmt? ElseBranch;
        public If(Expr condition, Stmt thenbranch, Stmt? elsebranch)
        {
            Condition = condition;
            ThenBranch = thenbranch;
            ElseBranch = elsebranch;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitIfStmt(this);
        }
    }

    public class Print : Stmt
    {
        public readonly Expr Express;
        public Print(Expr express)
        {
            Express = express;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }
    }

    public class Return : Stmt
    {
        public readonly Token Key;
        public readonly Expr? Value;
        public Return(Token key, Expr? value)
        {
            Key = key;
            Value = value;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitReturnStmt(this);
        }
    }

    public class Var : Stmt
    {
        public readonly Token Name;
        public readonly Expr? Initializer;
        public Var(Token name, Expr? initializer)
        {
            Name = name;
            Initializer = initializer;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitVarStmt(this);
        }
    }

    public class While : Stmt
    {
        public readonly Expr Condition;
        public readonly Stmt Body;
        public While(Expr condition, Stmt body)
        {
            Condition = condition;
            Body = body;
        }
        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }
    }

}
