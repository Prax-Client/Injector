namespace Injector;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.Title = "Prax Injector";

        bool downloaded = await Inject.Download();

        if (!downloaded)
        {
            Logger.Log("Failed to download Prax.dll", Logger.LType.Error);
            return;
        }


        Inject.InjectDLL();
    }
}