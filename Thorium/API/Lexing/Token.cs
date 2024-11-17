namespace Thorium.API.Lexing;

public record Token(TokenType Type, string Lexeme, object Literal, int Line) {
    public override string ToString() {
        return $"[Type:{Type}, Text:{Lexeme}, Value:{Literal}]";
    }
}