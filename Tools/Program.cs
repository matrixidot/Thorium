namespace Tools;

class Program {
    private static string previousPath = string.Empty;

    public static void Main(string[] args) {
        DefineAsts.Run(Console.ReadLine());
    }
}