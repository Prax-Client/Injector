using System.Diagnostics;

namespace Injector;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = "Prax Injector";
        
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
██████╗ ██████╗  █████╗ ██╗  ██╗    ██╗███╗   ██╗     ██╗███████╗ ██████╗████████╗ ██████╗ ██████╗ 
██╔══██╗██╔══██╗██╔══██╗╚██╗██╔╝    ██║████╗  ██║     ██║██╔════╝██╔════╝╚══██╔══╝██╔═══██╗██╔══██╗
██████╔╝██████╔╝███████║ ╚███╔╝     ██║██╔██╗ ██║     ██║█████╗  ██║        ██║   ██║   ██║██████╔╝
██╔═══╝ ██╔══██╗██╔══██║ ██╔██╗     ██║██║╚██╗██║██   ██║██╔══╝  ██║        ██║   ██║   ██║██╔══██╗
██║     ██║  ██║██║  ██║██╔╝ ██╗    ██║██║ ╚████║╚█████╔╝███████╗╚██████╗   ██║   ╚██████╔╝██║  ██║
╚═╝     ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝    ╚═╝╚═╝  ╚═══╝ ╚════╝ ╚══════╝ ╚═════╝   ╚═╝    ╚═════╝ ╚═╝  ╚═╝");
        Console.ForegroundColor = ConsoleColor.Magenta;
        
        bool downloaded = await Inject.Download();

        if (!downloaded)
        {
            Logger.Log("Main", "Failed to download Prax.dll", Logger.LType.Error);
            Logger.Log("Main", "Press any key to exit.");
            Console.ReadKey(true);
            return;
        }
        
        bool result = Inject.InjectDLL();
        if (result) Thread.Sleep(2000);
        else
        {
            Logger.Log("Main", "Failed to inject Prax.dll", Logger.LType.Error);
            Logger.Log("Main", "Press any key to exit.");
            Console.ReadKey(true);
        }
        if (Debugger.IsAttached) Console.ReadLine();
    }
}