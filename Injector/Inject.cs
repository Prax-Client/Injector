using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
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

    
    public static async Task<bool> Download()
    {
        try
        {
            // If the file already exists, delete it
            if (File.Exists("Prax.dll"))
            {
                Logger.Log("Deleting old Prax.dll");
                File.Delete("Prax.dll");
            }
            
            
            // Create new http client instance
            HttpClient client = new HttpClient();
        
            // Set user agent to "Prax Injector"
            client.DefaultRequestHeaders.Add("User-Agent", "Prax Injector");
        
            // send get to https://api.github.com/repos/Prax-Client/Releases/releases/latest
            var response = await client.GetAsync("https://api.github.com/repos/Prax-Client/Releases/releases/latest");
            Logger.Log("Got response from github");

            // read response as string
            var responseString = await response.Content.ReadAsStringAsync();
        
            // Get the download url from the response
            var downloadUrl = responseString.Split("\"browser_download_url\":")[1].Split(",")[0].Replace("\"", "");
        
            // Remove the last two characters from the download url
            downloadUrl = downloadUrl.Remove(downloadUrl.Length - 2);
        
            Logger.Log("Download URL: " + downloadUrl);
        
            // send get to download url
            var downloadResponse = await client.GetAsync(downloadUrl);
        
            // Stream the response to a file (DLL)
            await downloadResponse.Content.ReadAsStreamAsync().Result.CopyToAsync(File.Create("Prax.dll"));


            return true;
        }
        catch (Exception e)
        {
            Logger.Log("Failed to download Prax.dll " + e, Logger.LType.Error);
            return false;
        }
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
    
    public static void InjectDLL()
    {
        string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\Prax.dll";
        
        Logger.Log("Injecting " + path);

        ApplyAppPackages(path);
        var target = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
        if (target == null)
        {
            Logger.Log("Failed to find Minecraft.Windows process", Logger.LType.Error);
            return;
        }

        Logger.Log("Injecting Prax.dll");

        var hProc = OpenProcess(0xFFFF, false, target.Id);
        var loadLibraryProc = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        var allocated = VirtualAllocEx(hProc, IntPtr.Zero, (uint)path.Length + 1, 0x00001000 | 0x00002000,
            0x40);
        WriteProcessMemory(hProc, allocated, Encoding.UTF8.GetBytes(path), (uint)path.Length + 1, out _);
        Logger.Log("Allocated memory");
        CreateRemoteThread(hProc, IntPtr.Zero, 0, loadLibraryProc, allocated, 0, IntPtr.Zero);
        Logger.Log("Remote thread created");
    }
}