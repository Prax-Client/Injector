using System.Diagnostics;

namespace Injector;

public static class Logger
{
    
    public enum LType
    {
        Info,
        Warning,
        Error
    }

    public static void Log(string sender, string message, LType lType = LType.Info)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"[{time}] ");

        switch (lType)
        {
            case LType.Info:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case LType.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LType.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
        }

        Console.Write($"[{sender}] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
    }

    public static void Log(string message, LType lType = LType.Info)
    {
        string sender = new StackTrace().GetFrame(1)!.GetMethod()!.DeclaringType?.Name;
        Log(sender, message, lType);
    }
}