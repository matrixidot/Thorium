
namespace Tools;

public static class Extensions {
    public static string CFL(this string s) {
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}