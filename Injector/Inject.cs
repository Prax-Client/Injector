﻿using System.Diagnostics;
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
    public static async Task<bool> Download()
    {
        try
        {
            // If the file already exists, and compare hashes if it does
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Prax Injector");

            if (File.Exists(Path))
            {
                Logger.Log("Download", "Prax.dll already exists, checking hashes");
                using var hasher = MD5.Create();
                await using var stream = File.OpenRead(Path);
                var hash = await hasher.ComputeHashAsync(stream);
                
                // Send a head request to get the hash of the latest release
                var headResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, Url));
                
                var otherHash = headResponse.Content.Headers.ContentMD5;
                
                // If the hashes are the same, return true
                if (hash.SequenceEqual(otherHash))
                {
                    Logger.Log("Download", "Hashes match, skipping download");
                    return true;
                }
            }

            // Create new http client instance
            Logger.Log("Download", "Downloading Prax.dll");
            var response = await client.GetAsync(Url);
            
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
}