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
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1000);

            using var writer = new StreamWriter(client) { AutoFlush = true };
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
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                await server.WaitForConnectionAsync();

                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync() ?? string.Empty;

                OnPipeMessageReceived?.Invoke(message.Split(ArgumentSeparator));
            }
        });
    }
}
