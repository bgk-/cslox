namespace cslox;

public class ParseException : Exception
{
}

public class Parser
{
    private readonly List<Token> _tokens;
    private int _current;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public List<Stmt?> Parse()
    {
        var statements = new List<Stmt?>();
        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }
        return statements;
    }

    private Stmt? Declaration()
    {
        try
        {
            if (Match(TokenType.Class)) return ClassDeclaration();
            if (Match(TokenType.Fun)) return Function("function");
            if (Match(TokenType.Var)) return VarDeclaration();
            return Statement();
        }
        catch (ParseException)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt ClassDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expect class name.");
        Expr.Variable? superclass = null;
        if (Match(TokenType.Less))
        {
            Consume(TokenType.Identifier, "Expect superclass name.");
            superclass = new Expr.Variable(Previous());
        }
        Consume(TokenType.LeftBrace, "Expect '{' before class body.");

        var methods = new List<Stmt.Function>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            methods.Add(Function("method"));
        }

        Consume(TokenType.RightBrace, "Expect '}' after class body");
        return new Stmt.Class(name, superclass, methods);
    }

    private Stmt.Function Function(string kind)
    {
        var name = Consume(TokenType.Identifier, $"Expect {kind} name.");
        Consume(TokenType.LeftParen, $"Expect '(' after {kind} name.");
        var parameter = new List<Token>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (parameter.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 parameters");
                }

                parameter.Add(Consume(TokenType.Identifier, "Expect parameter name."));
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expect ')' after parameters");
        Consume(TokenType.LeftBrace, $"Expect '\\{{' before {kind} body");
        var body = Block();
        return new Stmt.Function(name, parameter, body);
    }

    private Stmt VarDeclaration()
    {
        var name = Consume(TokenType.Identifier, "Expected variable name.");
        Expr? initializer = null;
        if (Match(TokenType.Equal))
        {
            initializer = Expression();
        }

        Consume(TokenType.SemiColon, "Expected ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    private Stmt Statement()
    {
        if (Match(TokenType.For)) return ForStatement();
        if (Match(TokenType.If)) return IfStatement();
        if (Match(TokenType.Print)) return PrintStatement();
        if (Match(TokenType.Return)) return ReturnStatement();
        if (Match(TokenType.While)) return WhileStatement();
        if (Match(TokenType.LeftBrace)) return new Stmt.Block(Block());
        return ExpressionStatement();
    }

    private Stmt ReturnStatement()
    {
        var key = Previous();
        Expr? value = null;
        if (!Check(TokenType.SemiColon))
        {
            value = Expression();
        }

        Consume(TokenType.SemiColon, "Expect ';' after return value.");
        return new Stmt.Return(key, value);
    }

    private Stmt ForStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'for'");
        Stmt? initializer;
        if (Match(TokenType.SemiColon))
        {
            initializer = null;
        } else if (Match(TokenType.Var))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }

        Expr? condition = null;
        if (!Check(TokenType.SemiColon))
        {
            condition = Expression();
        }

        Consume(TokenType.SemiColon, "Expect ';' after loop condition.");

        Expr? increment = null;
        if (!Check(TokenType.RightParen))
        {
            increment = Expression();
        }

        Consume(TokenType.RightParen, "Expect ')' after for clauses.");

        var body = Statement();
        if (increment != null)
        {
            body = new Stmt.Block(new List<Stmt?> { body, new Stmt.Expression(increment) });
        }

        if (condition == null) condition = new Expr.Literal(true);
        body = new Stmt.While(condition, body);
        
        if (initializer != null)
        {
            body = new Stmt.Block(new List<Stmt?> { initializer, body });
        }

        return body;
    }

    private Stmt WhileStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'while'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after condition.");
        var body = Statement();
        return new Stmt.While(condition, body);
    }

    private Stmt IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'.");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after if condition.");
        var thenBranch = Statement();
        Stmt? elseBranch = null;
        if (Match(TokenType.Else))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private List<Stmt?> Block()
    {
        var statements = new List<Stmt?>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private Stmt ExpressionStatement()
    {
        var expr = Expression();
        Consume(TokenType.SemiColon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    private Stmt PrintStatement()
    {
        var expr = Expression();
        Consume(TokenType.SemiColon, "Expect ';' after value.");
        return new Stmt.Print(expr);
    }

    private void Synchronize()
    {
        Advance();
        while (!IsAtEnd())
        {
            if (Previous().Type == TokenType.SemiColon) return;
            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }
            Advance();
        }
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        var expr = Or();
        if (Match(TokenType.Equal))
        {
            var equals = Previous();
            var value = Assignment();
            if (expr is Expr.Variable variable)
            {
                var name = variable.Name;
                return new Expr.Assign(name, value);
            }
            if (expr is Expr.Get get)
                return new Expr.Set(get.Obj, get.Name, value);

            Error(equals, "Invalid assignment target.");
        }
        return expr;
    }

    private Expr Or()
    {
        var expr = And();
        while (Match(TokenType.Or))
        {
            var op = Previous();
            var right = And();
            expr = new Expr.Logical(expr, op, right);
        }

        return expr;
    }

    private Expr And()
    {
        var expr = Equality();
        while (Match(TokenType.And))
        {
            var op = Previous();
            var right = Equality();
            expr = new Expr.Logical(expr, op, right);
        }

        return expr;
    }

    private Expr Equality()
    {
        var expr = Comparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var op = Previous();
            var right = Comparison();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }


    private Expr Comparison()
    {
        var expr = Term();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous();
            var right = Term();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Expr Term()
    {
        var expr = Factor();
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            var op = Previous();
            var right = Factor();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    private Expr Factor()
    {
        var expr = Unary();
        while (Match(TokenType.Slash, TokenType.Star))
        {
            var op = Previous();
            var right = Unary();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    private Expr Call()
    {
        var expr = Primary();
        while (true)
        {
            if (Match(TokenType.LeftParen))
                expr = FinishCall(expr);
            else if (Match(TokenType.Dot))
            {
                var name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else
                break;
        }

        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        var arguments = new List<Expr?>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                if (arguments.Count >= 255)
                {
                    Error(Peek(), "Can't have more than 255 arguments.");
                }
                arguments.Add(Expression());
            } while (Match(TokenType.Comma));
        }

        var paren = Consume(TokenType.RightParen, "Expect ')' after arguments.");
        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var op = Previous();
            var right = Unary();
            return new Expr.Unary(op, right);
        }

        return Call();
    }

    private Expr Primary()
    {
        if (Match(TokenType.False)) return new Expr.Literal(false);
        if (Match(TokenType.True)) return new Expr.Literal(true);
        if (Match(TokenType.Nil)) return new Expr.Literal(null);
        if (Match(TokenType.Number, TokenType.String))
            return new Expr.Literal(Previous().Literal!);
        if (Match(TokenType.Super))
        {
            var key = Previous();
            Consume(TokenType.Dot, "Expect '.' after 'super'.");
            var method = Consume(TokenType.Identifier, "Expect superclass method name.");
            return new Expr.Super(key, method);
        }
        if (Match(TokenType.This)) return new Expr.This(Previous());
        if (Match(TokenType.Identifier))
            return new Expr.Variable(Previous());
        if (Match(TokenType.LeftParen))
        {
            var expr = Expression();
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }
        throw Error(Peek(), "Expected expression.");
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(Peek(), message);
    }

    private ParseException Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseException();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.Eof;
    private Token Peek() => _tokens[_current];
    private Token Previous() => _tokens[_current - 1];
}