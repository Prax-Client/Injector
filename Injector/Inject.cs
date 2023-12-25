using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Injector;

public static class Inject
{
    
    [DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true,
        SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", EntryPoint = "VirtualAllocEx", SetLastError = true, ExactSpelling = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
        uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", EntryPoint = "WriteProcessMemory", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer,
        uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", EntryPoint = "CreateRemoteThread")]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter,
        uint dwCreationFlags, IntPtr lpThreadId);

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
        Thread.Sleep(2000);
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

        Logger.Log("InjectDLL", "Injecting " + Path);

        ApplyAppPackages(Path);
        var target = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
        if (target == null)
        {
            Logger.Log("InjectDLL", "Failed to find Minecraft.Windows process", Logger.LType.Error);
            return false;
        }
        
        // Check if the dll is already injected
        var modules = target.Modules.Cast<ProcessModule>().ToList();
        if (modules.Any(module => module.FileName == Path))
        {
            Logger.Log("InjectDLL", "Prax.dll is already injected", Logger.LType.Error);
            return false;
        }

        Logger.Log("InjectDLL", "Injecting Prax.dll");

        var hProc = OpenProcess(0xFFFF, false, target.Id);
        var loadLibraryProc = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        var allocated = VirtualAllocEx(hProc, IntPtr.Zero, (uint)Path.Length + 1, 0x00001000 | 0x00002000,
            0x40);
        WriteProcessMemory(hProc, allocated, Encoding.UTF8.GetBytes(Path), (uint)Path.Length + 1, out _);
        Logger.Log("InjectDLL", "Allocated memory");
        CreateRemoteThread(hProc, IntPtr.Zero, 0, loadLibraryProc, allocated, 0, IntPtr.Zero);
        Logger.Log("InjectDLL", "Remote thread created");
        Logger.Log("InjectDLL", "Prax.dll injected");
        return true;
    }

    public static string GetLatestSupportedVersion()
    {
        // https://raw.githubusercontent.com/Prax-Client/Releases/main/latest_supported.txt
        try
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Prax Injector");
            var response = client.GetAsync("https://raw.githubusercontent.com/Prax-Client/Releases/main/latest_supported.txt").Result;
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
            ProcessStartInfo startInfo = new ProcessStartInfo
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
            output = output.Replace("\n", "").Replace("\r", "").Replace(" ", "");
            return output;
        }
        catch (Exception e)
        {
            Logger.Log("GetMinecraftVersion", "Failed to get Minecraft version " + e, Logger.LType.Error);
            return string.Empty;
        }
    }
}