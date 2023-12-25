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
        Console.ForegroundColor = ConsoleColor.White;

        
        // Check for --debug flag
        if (args.Length > 0 && args[0] == "--debug")
        {
            Debug.PerformChecks();
            
            Logger.Log("Main", "Press any key to exit.");
            Console.ReadKey(true);
            return;
        }
        
        Logger.Log("Main", "Checking version...");
        string latestSupportedVersion = Inject.GetLatestSupportedVersion();  
        string mcVersion = Inject.GetMinecraftVersion();
        if (!mcVersion.StartsWith(latestSupportedVersion) && latestSupportedVersion != string.Empty)
        {
            Logger.Log("Main", $"Minecraft version {mcVersion} is not supported.", Logger.LType.Error);
            Logger.Log("Main", $"Latest supported version is {latestSupportedVersion}, please update!", Logger.LType.Error);
            Logger.Log("Main", "If this version is a hotfix, it may be safe to continue.", Logger.LType.Warning);
            Logger.Log("Main", "Do you want to continue anyway? (y/n)");
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Y) return;
            
            Logger.Log("Main", "Continuing...");
        } else if (latestSupportedVersion == string.Empty) Logger.Log("Main", "Failed to get latest supported version!", Logger.LType.Error);
        else Logger.Log("Main", $"Minecraft version {mcVersion} is supported.", Logger.LType.Info);

        
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