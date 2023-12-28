namespace Injector;

public class Debug
{
    static bool ConnectionToServer()
    {
        // Send get to https://prax.wtf/
        // If the response is not 200, return false
        
        var request = new HttpRequestMessage(HttpMethod.Get, "https://prax.wtf/");
        
        var response = Program.Client.Send(request);
        
        return response.IsSuccessStatusCode;
    }

    static bool ConnectionToGithub()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://github.com");
        
        var response = Program.Client.Send(request);
        
        
        if (!response.IsSuccessStatusCode)
        {
            Logger.Log("Debug", "Failed to GET Github", Logger.LType.Error);
            return false;
        }

        return true;
    }
    
    // Check C:\Users\Flash\AppData\Local\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe\RoamingState
    static bool CheckDirectories()
    {
        var praxDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe\RoamingState\Prax");
        
        if (!Directory.Exists(praxDir)) return false;

        // Create tree of all subdirectories and files
        
        var tree = new List<string>();

        Logger.Log("Debug", "Checking files...");
        foreach (var dir in Directory.GetDirectories(praxDir))
        {
            Logger.Log("Debug", "Directory found: " + dir);
            foreach (var file in Directory.GetFiles(dir))
            {
                Logger.Log("Debug", "File found: " + file);
            }
        }


        return true;
    }
    
    static bool IsGameUpToDate()
    {
        string latestSupportedVersion = Inject.GetLatestSupportedVersion();
        string mcVersion = Inject.GetMinecraftVersion();
        
        Logger.Log("Debug", $"Latest supported version: {latestSupportedVersion}");
        Logger.Log("Debug", $"Minecraft version: {mcVersion}");


        return mcVersion.StartsWith(latestSupportedVersion) && latestSupportedVersion != string.Empty;
    }
    
    public static void PerformChecks()
    {
        Logger.Log("Debug", "Performing checks");

        Logger.Log("Debug", "Checking connection to server...");
        bool connectionToServer = ConnectionToServer();
        Logger.Log("Debug", connectionToServer ? "Connection to server successful" : "Connection to server failed", connectionToServer ? Logger.LType.Info : Logger.LType.Error);

        Logger.Log("Debug", "Checking connection to Github...");
        bool connectionToGithub = ConnectionToGithub();
        Logger.Log("Debug", connectionToGithub ? "Connection to Github successful" : "Connection to Github failed", connectionToGithub ? Logger.LType.Info : Logger.LType.Error);

        Logger.Log("Debug", "Checking directories...");
        bool checkDirectories = CheckDirectories();
        Logger.Log("Debug", checkDirectories ? "Directories exist" : "Directories do not exist", checkDirectories ? Logger.LType.Info : Logger.LType.Error);

        Logger.Log("Debug", "Checking if game is up to date...");
        bool isGameUpToDate = IsGameUpToDate();
        Logger.Log("Debug", isGameUpToDate ? "Game is up to date" : "It's possible the game version is not supported", isGameUpToDate ? Logger.LType.Info : Logger.LType.Error);
        
        
        Logger.Log("Debug", "Checks complete, Send a picture of this in your support ticket and make sure all text is readable.");
        
        
    }
}