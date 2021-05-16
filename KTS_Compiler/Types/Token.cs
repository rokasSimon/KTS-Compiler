namespace KTS_Compiler
{
    public enum TokenType
    {
        UNKNOWN, EOF,

        LEFT_PARENTH, LEFT_BRACE, LEFT_BRACKET, RIGHT_PARENTH, RIGHT_BRACE, RIGHT_BRACKET,
        COMMA, RANGE, MINUS, PLUS, SEMICOLON, DIV, MULT, PERCENT,

        NOT, NOT_EQUAL, ASSIGN, EQUAL, GREATER, GREATER_EQUAL, LESS, LESS_EQUAL,
        AND, OR, COLON, QUESTION,

        IDENTIFIER, STRING, CHAR, INTEGER, FLOAT,

        IF, ELSE, TRUE, FALSE, FOR, WHILE, PRINT, RETURN, READ, WRITE, ANY, REF, NEW, IN, ON,
        TYPE_SPECIFIER, VOID, CAST, LENGTH
    }

    public struct Token
    {
        public int Line { get; }
        public string Source { get; }
        public object Value { get; }
        public TokenType Type { get; }

        public Token(int line, string source, object value, TokenType type)
        {
            Line = line;
            Source = source;
            Value = value;
            Type = type;
        }

        public override string ToString()
        {
            var value = Value ?? "";

            return $"{Type}(Line: {Line} | Source: {Source} | Value: {value})";
        }
    }
}
