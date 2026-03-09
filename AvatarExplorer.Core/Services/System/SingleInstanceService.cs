using System.IO.Pipes;

namespace AvatarExplorer.Core.Services.System;

public static class SingleInstanceService
{
    public static event Action<string[]>? OnPipeMessageReceived = null;

    private const string PipeName = "AvatarExplorerV2.Pipe";
    private const char ArgumentSeparator = '|';

    public static void SendToServer(string[] args)
    {
        try
        {
            using NamedPipeClientStream client = new(".", PipeName, PipeDirection.Out);
            client.Connect(1000);

            using StreamWriter writer = new(client) { AutoFlush = true };
            writer.WriteLine(string.Join(ArgumentSeparator, args));
        }
        catch
        {
            // Ignored
        }
    }

    public static void StartServer()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                using NamedPipeServerStream server = new(PipeName, PipeDirection.In);

                await server.WaitForConnectionAsync();

                using StreamReader reader = new(server);
                string? message = await reader.ReadLineAsync() ?? string.Empty;

                OnPipeMessageReceived?.Invoke(message.Split(ArgumentSeparator));
            }
        });
    }
}
