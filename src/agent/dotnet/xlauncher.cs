using System;
using Microsoft.ServiceProcess;

public class IoTAgent
{
    public static void Main(string[] args)
    {
        // Check the platform.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // Install the Windows Service.
            ServiceInstaller installer = new ServiceInstaller();

            // Set the service name.
            installer.ServiceName = "IoTAgent";

            // Set the service description.
            installer.Description = "IoT Agent Service";

            // Set the service start type.
            installer.StartType = ServiceStartMode.Automatic;

            // Register the service.
            installer.Install();
        }
        else
        {
            // Run the console application in a container.
            RunConsoleApplicationInContainer();

            // Get the connection string from the env var.
            string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        }
    }

    private static void InstallWindowsService()
    {
        // Get the registry key.
        RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\IoTAgent");

        // Set the connection string value.
        key.SetValue("ConnectionString", "connection_string");
    }

    private static void RunConsoleApplicationInContainer()
    {
        // Create a new container.
        var container = new Container();

        // Set the container image.
        container.Image = "microsoft/dotnet:core-sdk";

        // Set the container command.
        container.Command = "dotnet run";

        // Start the container.
        container.Start();

        // Wait for the container to exit.
        container.WaitForExit();
    }
}
