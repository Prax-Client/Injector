using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Injector;

public static partial class Inject
{

    // Matthews, C.M. (2023) JiayiLauncher/JiayiLauncher/Utils/Imports.cs, GitHub. Available at: https://github.com/JiayiSoftware/JiayiLauncher/blob/66d3099060685ca69406bdec8b2910613c0420d0/JiayiLauncher/Utils/Imports.cs#L31-L52 (Accessed: 27 December 2023). 
    
    
    
    // (JiayiLauncher/JiayiLauncher/Utils/Imports.cs 2023)
    [LibraryImport("kernel32.dll")]
    public static partial nint OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll")]
    public static partial nint GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

    [LibraryImport("kernel32", SetLastError = true)]
    public static partial nint GetProcAddress(nint hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint VirtualAllocEx(nint hProcess, nint lpAddress, uint dwSize,
        uint flAllocationType, uint flProtect);
	
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, uint nSize,
        out nuint lpNumberOfBytesWritten);

    [LibraryImport("kernel32.dll")]
    public static partial nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, uint dwStackSize,
        nint lpStartAddress, nint lpParameter, uint dwCreationFlags, nint lpThreadId);
    
    private static string Path => Environment.ExpandEnvironmentVariables("%temp%\\Prax.dll");
    private const string Url = "https://github.com/Prax-Client/Releases/releases/latest/download/Prax.dll";

    public static async Task<bool> CheckHash(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.Log("CheckHash", "Prax.dll does not exist", Logger.LType.Error);
            return false;
        } 
        
        // Send a head request to get the hash of the latest release
        var headResponse = await Program.Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, Url));
                
        var otherHash = headResponse.Content.Headers.ContentMD5;

        if (otherHash == null)
        {
            Logger.Log("CheckHash", "Failed to get hash of latest release", Logger.LType.Error);
            return false;
        }
        
        using var hasher = MD5.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await hasher.ComputeHashAsync(stream);
        
        return hash.SequenceEqual(otherHash);
    }
    
    public static async Task<bool> Download()
    {
        try
        {
            // If the file already exists, and compare hashes if it does
            if (File.Exists(Path))
            {
                Logger.Log("Download", "Prax.dll already exists, checking hashes");
               
                // If the hashes are the same, return true
                if (await CheckHash(Path))
                {
                    Logger.Log("Download", "Hashes match, skipping download");
                    return true;
                }
            }

            // Create new http client instance
            Logger.Log("Download", "Downloading Prax.dll");
            var response = await Program.Client.GetAsync(Url);
            
            // If the response is not successful, return false
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log("Download", "Failed to download Prax.dll", Logger.LType.Error);
                return false;
            }
            
            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(Path, content);

            return true;
        }
        catch (Exception e)
        {
            Logger.Log("Download", "Failed to download Prax.dll " + e, Logger.LType.Error);
            return false;
        }
    }
    
    public static void LaunchMinecraft()
    {
        if (Process.GetProcessesByName("Minecraft.Windows").Length > 0) return;
        
        Logger.Log("Inject", "Launching Minecraft");
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "shell:appsFolder\\Microsoft.MinecraftUWP_8wekyb3d8bbwe!App",
            UseShellExecute = true
        };
        Process.Start(startInfo);
        Process? mcProcess = null;
        
        while (mcProcess == null)
        {
            mcProcess = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
            Thread.Sleep(100);
        }
        
        Logger.Log("Inject", "Minecraft launched");
        // Wait for the module count to be more than 120 (the amount of modules loaded when the game is fully loaded)
        bool msgShown = false;
        while (mcProcess.Modules.Count < 120)
        {
            mcProcess.Refresh();
            Logger.LogWrite("Inject", $"Waiting for Minecraft to load... ({mcProcess.Modules.Count}/120)    \r");
            Thread.Sleep(100);
        }

        Console.WriteLine();
        Logger.Log("Inject", "Minecraft loaded");
    }

    private static void ApplyAppPackages(string dllPath)
    {
        var infoFile = new FileInfo(dllPath);
        var fSecurity = infoFile.GetAccessControl();
        fSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-15-2-1"),
            FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit,
            AccessControlType.Allow));
        infoFile.SetAccessControl(fSecurity);
    }
    
    public static bool InjectDLL()
    {

        Logger.Log("Inject", "Injecting " + Path);

        ApplyAppPackages(Path);
        var target = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
        if (target == null)
        {
            Logger.Log("Inject", "Failed to find Minecraft.Windows process", Logger.LType.Error);
            return false;
        }
        
        // Check if the dll is already injected
        var modules = target.Modules.Cast<ProcessModule>().ToList();
        if (modules.Any(module => module.FileName == Path))
        {
            Logger.Log("Inject", "Prax.dll is already injected", Logger.LType.Error);
            return false;
        }

        Logger.Log("Inject", "Injecting Prax.dll");

        var hProc = OpenProcess(0xFFFF, false, target.Id);
        var loadLibraryProc = GetProcAddress(GetModuleHandleW("kernel32.dll"), "LoadLibraryA");
        var allocated = VirtualAllocEx(hProc, IntPtr.Zero, (uint)Path.Length + 1, 0x00001000 | 0x00002000,
            0x40);
        WriteProcessMemory(hProc, allocated, Encoding.UTF8.GetBytes(Path), (uint)Path.Length + 1, out _);
        Logger.Log("Inject", "Allocated memory");
        CreateRemoteThread(hProc, IntPtr.Zero, 0, loadLibraryProc, allocated, 0, IntPtr.Zero);
        Logger.Log("Inject", "Remote thread created");
        return true;
    }

    public static string GetLatestSupportedVersion()
    {
        // https://raw.githubusercontent.com/Prax-Client/Releases/main/latest.txt
        try
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Prax Injector");
            var response = client.GetAsync("https://raw.githubusercontent.com/Prax-Client/Releases/main/latest.txt").Result;
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log("GetLatestSupportedVersion", "Failed to get latest supported version", Logger.LType.Error);
                return string.Empty;
            }

            return response.Content.ReadAsStringAsync().Result;
        }
        catch (Exception e)
        {
            Logger.Log("GetLatestSupportedVersion", "Failed to get latest supported version " + e, Logger.LType.Error);
            return string.Empty;
        }
    }

    public static string GetMinecraftVersion()
    {
        // Get version of Microsoft.MinecraftUWP appx using powershell
        // Get-AppxPackage -Name Microsoft.MinecraftUWP | Select-Object -ExpandProperty Version
        try
        {
            /*ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "Get-AppxPackage -Name Microsoft.MinecraftUWP | Select-Object -ExpandProperty Version",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Process process = new Process {StartInfo = startInfo};
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            // Remove all whitespace and newlines
            output = output.Replace("\n", "").Replace("\r", "").Replace(" ", "");*/
            
            // Get file version of Minecraft.Windows.exe
            // Get the minecraft folder
            LaunchMinecraft();
            
            string mcExecutable = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault()?.MainModule.FileName;
            
            // Get the file version
            string mcVersion = FileVersionInfo.GetVersionInfo(mcExecutable).FileVersion;
            
            
            return mcVersion;
        }
        catch (Exception e)
        {
            Logger.Log("GetMinecraftVersion", "Failed to get Minecraft version " + e, Logger.LType.Error);
            return string.Empty;
        }
    }
}
