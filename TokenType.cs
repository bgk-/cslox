namespace cslox;
public enum TokenType
{
     LeftParen,
     RightParen,
     LeftBrace,
     RightBrace,
     Comma,
     Dot,
     Minus,
     Plus,
     SemiColon,
     Slash,
     Star,

     Bang,
     BangEqual,
     Equal,
     EqualEqual,
     Greater,
     GreaterEqual,
     Less,
     LessEqual,

     Identifier,
     String,
     Number,

     And,
     Class,
     Else,
     False,
     Fun,
     For,
     If,
     Nil,
     Or,
     Print,
     Return,
     Super,
     This,
     True,
     Var,
     While,

     Illegal,
     Eof
}