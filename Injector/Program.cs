using System.Diagnostics;

namespace Injector;

class Program
{
    
    public static HttpClient Client = new HttpClient();
    
    public static async Task Main(string[] args)
    {
        Program.Client.DefaultRequestHeaders.Add("User-Agent", "Prax Injector");
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

        // Check for --debug flag
        if (args.Length > 0 && args[0] == "--debug")
        {
            Debug.PerformChecks();
            
            Logger.Log("Main", "Press any key to exit.");
            Console.ReadKey(true);
            return;
        }
        
        Inject.LaunchMinecraft();
        
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