namespace Thorium.API.Lexing;

using System.Text.RegularExpressions;
using Tools;
using static TokenType;

public class Lexer(string Source) {
    private readonly List<Token> Tokens = [];
    private int Start;
    private int Current;
    private int Line = 1;

    private static readonly Dictionary<string, TokenType> keywords = new() {
        ["if"] = IF,
        ["elif"] = ELIF,
        ["else"] = ELSE,
        ["while"] = WHILE,
        ["for"] = FOR,
        ["continue"] = CONTINUE,
        ["break"] = BREAK,
        ["return"] = RETURN,

        ["class"] = CLASS,
        ["super"] = SUPER,
        ["this"] = THIS,
        ["null"] = NULL,
        ["true"] = TRUE,
        ["false"] = FALSE,
        
        ["int"] = INT,
        ["long"] = LONG,
        ["double"] = DOUBLE,
        ["bool"] = BOOL,
        ["string"] = STRING_TYPE,
        ["char"] = CHAR_TYPE,
        ["object"] = OBJECT,
        
        ["print"] = PRINT,
    };
    
    public List<Token> LexSource() {
        while (!IsAtEnd) {
            Start = Current;
            Lex();
        }
        Tokens.Add(new Token(EOF, "", null, Line));
        return Tokens;
    }

    private void Lex() {
        char c = Advance();
        switch (c) {
            // Easy Ones
            case '(':
                HandleLeftParen(); break;
            case ')': AddToken(R_PAREN); break;
            case '{': AddToken(L_BRACE); break;
            case '}': AddToken(R_BRACE); break;
            case '[': AddToken(L_BRACKET); break;
            case ']': AddToken(R_BRACKET); break;
            case '~': AddToken(BIN_NOT); break;
            case '&': AddToken(Match("&") ? AND : BIN_AND); break;
            case '|': AddToken(Match("|") ? OR : BIN_OR); break;
            case '^': AddToken(BIN_XOR); break;
            case ',': AddToken(COMMA); break;
            case ';': AddToken(SEMICOLON); break;
            case '"': String(); break;
            case '\'': Char(); break;
            case ' ' or '\r' or '\t': break;
            case '\n': Line++; break;
            case '.': 
                if (char.IsDigit(Peek)) Number(true); 
                else AddToken(DOT); 
                break;
            // Math Operators
            case '+': CompositeMatch("+ | =", INCREMENT, PLUS_EQUAL, PLUS); break;
            case '-': CompositeMatch("- | =", DECREMENT, MINUS_EQUAL, MINUS); break;
            case '*': CompositeMatch("*= | * | =", POW_EQUAL, POW, MULT_EQUAL, MULT); break;
            case '/':
                if (Match("/")) while (Peek != '\n' && !IsAtEnd) Advance();
                else AddToken(Match("=") ? DIV_EQUAL : DIV);
                break;
            case '%': AddToken(Match("=") ? MOD_EQUAL : MOD); break;
            // Relational Operators
            case '>': CompositeMatch("= | >", GREATER_EQUAL, RIGHT_SHIFT, GREATER); break;
            case '<': CompositeMatch("= | <", LESS_EQUAL, LEFT_SHIFT, LESS); break;
            // Logical Operators
            case '!': AddToken(Match("=") ? BANG_EQUAL : BANG); break;
            case '=': AddToken(Match("=") ? EQUAL_EQUAL : EQUAL); break;
            
            default:
                if (char.IsDigit(c))
                    Number(false);
                else if (char.IsLetter(c))
                    Identifier();
                else
                    Thorium.Error(Line, $"Unexpected Character {c}");
                break;
        }
    }


    private void CompositeMatch(string matches, params TokenType[] types) {
        string[] arr = matches.Split('|');
        for (int i = 0; i < arr.Length; i++) {
            if (!Match(arr[i].Trim())) continue;
            AddToken(types[i]);
            return;
        }
        AddToken(types[^1]);
    }
    private bool Match(string expected) {
        if (Current + expected.Length >= Source.Length) return false;
        if (Source.Substring(Current, expected.Length) != expected) return false;
        
        Current += expected.Length;
        return true;
    }

    private void Identifier() {
        while (char.IsLetterOrDigit(Peek)) Advance();
        string text = Source.Substring(Start, Current - Start);
        AddToken(keywords.GetValueOrDefault(text, IDENTIFIER), text);
    }
    private void Number(bool alreadyDecimal) {
        while (char.IsDigit(Peek)) Advance();

        if (Peek== '.' && !alreadyDecimal) {
            if (PeekNChar(2) != '\0' && char.IsDigit(PeekNChar(2))) {
                Advance();

                while (char.IsDigit(Peek)) Advance();
            } else {
                Thorium.Error(Line, "Invalid number format (trailing decimal point).");
                return;
            }
        }

        if (Peek== '.') {
            Thorium.Error(Line, "Invalid number format (multiple decimal points).");
            return;
        }

        string numberStr = Source.Substring(Start, Current - Start);
        if (int.TryParse(numberStr, out int iin)) {
            AddToken(NUMBER, iin);
        } else if (long.TryParse(numberStr, out long ln)) {
            AddToken(NUMBER, ln);
        } else if (double.TryParse(numberStr, out double dn)) {
            AddToken(NUMBER, dn);
        } else {
            Thorium.Error(Line, $"Invalid number format: {numberStr}");
        }
    }

    private void HandleLeftParen() {
        int tempStart = Current;
        
        while (!IsAtEnd && char.IsLetterOrDigit(Peek)) {
            Advance();
        }

        if (!IsAtEnd && Peek == ')') {
            string typeName = Source.Substring(tempStart, Current - tempStart).Trim();

            if (IsType(typeName)) {
                Tokens.Add(new Token(TYPECAST, typeName, null, Line));
                Advance();
            } else {
                Current = tempStart;
                AddToken(L_PAREN);
            }
        } else {
            Current = tempStart;
            AddToken(L_PAREN);
        }
    }

    // Helper to check if a string is a valid type
    private bool IsType(string identifier) {
        // Built-in types or user-defined types (expand as needed)
        string[] validTypes = ["int", "long", "double", "string", "char", "object"];
        return validTypes.Contains(identifier) || IsUserDefinedType(identifier);
    }

    // Extendable method for user-defined types
    private bool IsUserDefinedType(string typeName) {
        return Type.GetType(typeName) != null;
    }
    
    private void String() {
        while (!IsAtEnd) {
            if (Peek == '"' && (Current == Start + 1 || Source[Current - 1] != '\\')) {
                break;
            }

            if (Peek == '\n') Line++;
            Advance();
        }

        if (IsAtEnd) {
            Thorium.Error(Line, "Unterminated string.");
            return;
        }

        Advance();
        string value = Source.Substring(Start + 1, Current - Start - 2);
        AddToken(STRING_LIT, Regex.Unescape(value));
    }

    private void Char() {
        if (IsAtEnd) {
            Thorium.Error(Line, "Unterminated character literal.");
            return;
        }

        char value;
        if (Peek == '\\') {
            Advance();
            if (IsAtEnd) {
                Thorium.Error(Line, "Unterminated character literal with escape sequence.");
                return;
            }

            switch (Advance()) {
                case 't': value = '\t'; break;
                case 'n': value = '\n'; break;
                case 'r': value = '\r'; break;
                case '0': value = '\0'; break;
                case '\'': value = '\''; break;
                case '\\': value = '\\'; break;
                default:
                    Thorium.Error(Line, "Invalid escape sequence in character literal.");
                    return;
            }
        } else {
            value = Advance();
        }

        if (IsAtEnd || Advance() != '\'') {
            Thorium.Error(Line, "Unterminated character literal.");
            return;
        }

        AddToken(CHAR_LIT, value);
    }
    
    private char PeekNChar(int amount) {
        int targetIndex = Current + amount - 1;
        return targetIndex < Source.Length ? Source[targetIndex] : '\0';
    }

    private void AddToken(TokenType type, object literal = null) {
        string text = Source.Substring(Start, Current - Start);
        Tokens.Add(new Token(type, text, literal, Line));
    }

    private char Advance() => Source[Current++];
    private bool IsAtEnd => Current >= Source.Length;
    private char Peek => IsAtEnd ? '\0' : Source[Current];
}