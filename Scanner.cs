using System.Diagnostics;

namespace cslox;

public class Scanner
{
    private readonly string _source;
    private readonly List<Token> _tokens;

    private int _start;
    private int _current;
    private int _line = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "and", TokenType.And },
        { "class", TokenType.Class },
        { "else", TokenType.Else },
        { "false", TokenType.False },
        { "for", TokenType.For },
        { "if", TokenType.If },
        { "nil", TokenType.Nil },
        { "or", TokenType.Or },
        { "print", TokenType.Print },
        { "return", TokenType.Return },
        { "super", TokenType.Super },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "var", TokenType.Var },
        { "while", TokenType.While },
        { "fun", TokenType.Fun }
    };

    public Scanner(string source)
    {
        _source = source;
        _tokens = new List<Token>();
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.Eof, "", null, _line));
        return _tokens;
    }

    private bool IsAtEnd() => _current >= _source.Length;

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            case '(':
                AddToken(TokenType.LeftParen);
                break;
            case ')':
                AddToken(TokenType.RightParen);
                break;
            case '{':
                AddToken(TokenType.LeftBrace);
                break;
            case '}':
                AddToken(TokenType.RightBrace);
                break;
            case ',':
                AddToken(TokenType.Comma);
                break;
            case '.':
                AddToken(TokenType.Dot);
                break;
            case '-':
                AddToken(TokenType.Minus);
                break;
            case '+':
                AddToken(TokenType.Plus);
                break;
            case ';':
                AddToken(TokenType.SemiColon);
                break;
            case '*':
                AddToken(TokenType.Star);
                break;
            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '/':
                if (Match('/'))
                {
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                    break;
                }

                AddToken(TokenType.Slash);
                break;
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                _line++;
                break;
            case '"':
                String();
                break;
            default:
                if (IsDigit(c))
                {
                    Number();
                    break;
                }

                if (IsAlpha(c))
                {
                    Identifier();
                    break;
                }

                Lox.Error(_line, "Unexpected character.");
                break;
        }
    }

    private char Advance() => _source[_current++];

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;
        _current++;
        return true;
    }

    private bool IsDigit(char c) => c is >= '0' and <= '9';
    private bool IsAlpha(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];
    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd())
        {
            Lox.Error(_line, "Unterminated string.");
            return;
        }

        Advance();
        var value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.String, value);
    }

    private void Number()
    {
        while (IsDigit(Peek())) Advance();
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance();
            while (IsDigit(Peek())) Advance();
        }

        AddToken(TokenType.Number, double.Parse(_source.Substring(_start, _current - _start)));
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();
        var text = _source.Substring(_start, _current - _start);
        AddToken(Keywords.TryGetValue(text, out var type) ? type : TokenType.Identifier);
    }

    private void AddToken(TokenType type, object? literal = null)
    {
        var text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line));
    }
}