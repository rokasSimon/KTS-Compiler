using System;
using System.Collections.Generic;
using System.IO;
using KTS_Compiler.Extensions;
using System.Text.RegularExpressions;

namespace KTS_Compiler
{
    class KTUSharpScanner
    {
        private readonly Dictionary<string, TokenType> ReservedKeywords = new Dictionary<string, TokenType>
        {
            {"if", TokenType.IF},
            {"else", TokenType.ELSE},
            {"true", TokenType.TRUE},
            {"false", TokenType.FALSE},
            {"for", TokenType.FOR},
            {"while", TokenType.WHILE},
            {"return", TokenType.RETURN},
            {"read", TokenType.READ},
            {"write", TokenType.WRITE},
            {"any", TokenType.ANY},
            {"ref", TokenType.REF},
            {"new", TokenType.NEW},
            {"in", TokenType.IN},
            {"on", TokenType.ON},
            {"i8", TokenType.TYPE_SPECIFIER},
            {"i16", TokenType.TYPE_SPECIFIER},
            {"i32", TokenType.TYPE_SPECIFIER},
            {"i64", TokenType.TYPE_SPECIFIER},
            {"f32", TokenType.TYPE_SPECIFIER},
            {"f64", TokenType.TYPE_SPECIFIER},
            {"bool", TokenType.TYPE_SPECIFIER},
            {"string", TokenType.TYPE_SPECIFIER},
            {"void", TokenType.TYPE_SPECIFIER},
            {"cast", TokenType.CAST}
        };

        public List<Token> Tokens;
        private int Start;
        private int Current;
        private int Line;

        public string Source { get; }
        public bool Failed { get; private set; }

        public KTUSharpScanner(string source)
        {
            Tokens = new List<Token>();
            Source = File.ReadAllText(source);

            Start = Current = 0;
            Line = 1;
        }

        private bool EndOfFile() => Current >= Source.Length;

        public List<Token> ScanTokens()
        {
            while (!EndOfFile())
            {
                Start = Current;
                ScanToken();
            }

            Tokens.Add(new Token(Line, "", null, TokenType.EOF));

            return Tokens;
        }

        private void ScanToken()
        {
            var c = Advance();

            switch (c)
            {
                case ' ':
                case '\r':
                case '\t': break;
                case '\n': Line++; break;

                case '(': AddToken(TokenType.LEFT_PARENTH); break;
                case ')': AddToken(TokenType.RIGHT_PARENTH); break;
                case '{': AddToken(TokenType.LEFT_BRACE); break;
                case '}': AddToken(TokenType.RIGHT_BRACE); break;
                case '[': AddToken(TokenType.LEFT_BRACKET); break;
                case ']': AddToken(TokenType.RIGHT_BRACKET); break;
                case ',': AddToken(TokenType.COMMA); break;
                case '-': AddToken(TokenType.MINUS); break;
                case '+': AddToken(TokenType.PLUS); break;
                case ';': AddToken(TokenType.SEMICOLON); break;
                case '*': AddToken(TokenType.MULT); break;
                case '%': AddToken(TokenType.PERCENT); break;
                case '?': AddToken(TokenType.QUESTION); break;
                case ':': AddToken(TokenType.COLON); break;
                case '.':
                    {
                        if (Match('.'))
                        {
                            AddToken(TokenType.RANGE);
                        }
                        else
                        {
                            Console.WriteLine($"Line {Line}: Single dot given");
                            return;
                        }
                        break;
                    }
                case '!': AddToken(Match('=') ? TokenType.NOT_EQUAL : TokenType.NOT); break;
                case '=': AddToken(Match('=') ? TokenType.EQUAL : TokenType.ASSIGN); break;
                case '<': AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                case '>': AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;

                case '|': AddToken(TokenType.OR); break;
                case '&': AddToken(TokenType.AND); break;

                case '/':
                {
                    if (Match('/'))
                    {
                        while (LookAhead1() != '\n' && !EndOfFile())
                        {
                            Advance();
                        }
                    }
                    else if (Match('*'))
                    {
                        while (LookAhead1() != '*' && LookAhead2() != '/' && !EndOfFile())
                        {
                            if (LookAhead1() == '\n')
                            {
                                Line++;
                            }
                            Advance();
                        }

                        Advance();
                        Advance();
                    }
                    else
                    {
                        AddToken(TokenType.DIV);
                    }
                    break;
                }

                case '"': HandleString(); break;
                case '\'': HandleChar(); break;

                default:
                {
                    if (c.IsDigit())
                    {
                        HandleNumbers();
                    }
                    else if (c.IsAlpha())
                    {
                        HandleIdentifier();
                    }
                    else
                    {
                        Console.WriteLine($"Line {Line}: Unknown character: '{c}'");
                        Failed = true;
                    }

                    break;
                }
            }
        }

        private char LookAhead1()
        {
            if (EndOfFile())
            {
                return '\0';
            }

            return Source[Current];
        }

        private char LookAhead2()
        {
            if (Current >= Source.Length + 1)
            {
                return '\0';
            }

            return Source[Current + 1];
        }

        private char Advance()
        {
            Current++;

            return Source[Current - 1];
        }

        private void AddToken(TokenType type, object literal = null)
        {
            Tokens.Add(new Token(Line, Source.RangeSubstring(Start, Current), literal, type));
        }

        private bool Match(char expected)
        {
            if (EndOfFile() || Source[Current] != expected)
            {
                return false;
            }

            Current++;
            return true;
        }

        private void HandleString()
        {
            while (LookAhead1() != '"' && !EndOfFile())
            {
                if (LookAhead1() == '\n')
                {
                    Line++;
                }

                Advance();
            }

            if (EndOfFile())
            {
                Console.WriteLine($"Line {Line}: Unterminated string.");
                return;
            }

            Advance(); // Skip ending quote
            AddToken(TokenType.STRING, Source.RangeSubstring(Start + 1, Current - 1));
        }

        private void HandleChar()
        {
            while (LookAhead1() != '\'' && !EndOfFile())
            {
                if (LookAhead1() == '\n')
                {
                    Console.WriteLine($"Line {Line}: Newline in char constant");
                    return;
                }

                Advance();
            }

            if (EndOfFile())
            {
                Console.WriteLine($"Line {Line}: Unterminated char constant.");
                return;
            }

            Advance();

            var str = Source.RangeSubstring(Start + 1, Current - 1);
            var escaped = Regex.Unescape(str);

            if (escaped.Length > 1)
            {
                Console.WriteLine($"Line {Line}: Char constant has more than 1 symbol.");
                return;
            }

            AddToken(TokenType.CHAR, escaped[0]);

            /*if (LookAhead1() == '\'')
            {
                Console.WriteLine($"Line {Line}: Empty char constant.");
                return;
            }

            if (LookAhead1() == '\\')
            {
                Advance(); // Skip the \

                var cur = Advance();
                switch (cur)
                {
                    case 'n': AddToken(TokenType.CHAR, '\n'); break;
                    case '\\': AddToken(TokenType.CHAR, '\\'); break;
                    case '\'': AddToken(TokenType.CHAR, '\''); break;
                    case '"': AddToken(TokenType.CHAR, '"'); break;
                    default: Console.WriteLine($"Line {Line}: Unknown escape character '{cur}'."); return;
                }
            }

            if (EndOfFile())
            {
                Console.WriteLine($"Line {Line}: Unterminated char sequence.");
                return;
            }

            if (LookAhead2() != '\'')
            {
                Console.WriteLine($"Line {Line}: Too many characters in char constant.");
                return;
            }

            Advance();
            Advance();
            AddToken(TokenType.CHAR, Source[Current - 2]);*/
        }

        private void HandleNumbers()
        {
            while (LookAhead1().IsDigit())
            {
                Advance();
            }

            if (LookAhead1() == '.' && LookAhead2().IsDigit())
            {
                Advance();

                while (LookAhead1().IsDigit())
                {
                    Advance();
                }

                var floatToParse = Source.RangeSubstring(Start, Current);
                floatToParse = floatToParse.Replace('.', ',');

                AddToken(TokenType.FLOAT, Double.Parse(floatToParse));
            }
            else
            {
                var intToParse = Source.RangeSubstring(Start, Current);

                AddToken(TokenType.INTEGER, long.Parse(intToParse));
            }
        }

        private void HandleIdentifier()
        {
            while (LookAhead1().IsAlphaNumeric())
            {
                Advance();
            }

            var text = Source.RangeSubstring(Start, Current);
            var type = ReservedKeywords.ContainsKey(text) ? ReservedKeywords[text] : TokenType.IDENTIFIER;

            if (type == TokenType.TYPE_SPECIFIER)
            {
                AddToken(type, text);
            }
            else
            {
                AddToken(type);
            }
        }
    }
}
