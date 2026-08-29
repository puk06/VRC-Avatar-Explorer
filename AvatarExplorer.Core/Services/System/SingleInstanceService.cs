using System.IO.Pipes;

namespace AvatarExplorer.Core.Services.System;

/// <summary>
/// 名前付きパイプを使用してアプリケーションの単一インスタンス化を実現するサービス。
/// 既に起動しているサーバーインスタンスへコマンドライン引数を転送します。
/// </summary>
public static class SingleInstanceService
{
    /// <summary>サーバー側でパイプメッセージ（コマンドライン引数の配列）を受信したときに発生するイベント。</summary>
    public static event Action<string[]>? OnPipeMessageReceived = null;

    private const string PipeName = "AvatarExplorerV2.Pipe";
    private const char ArgumentSeparator = '|';

    /// <summary>既に起動しているサーバーインスタンスへコマンドライン引数をパイプ経由で送信します。接続に失敗した場合は無視されます。</summary>
    /// <param name="args">転送するコマンドライン引数。</param>
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

    /// <summary>
    /// パイプサーバーをバックグラウンドで開始し、クライアントからの接続を待ち受けます。
    /// 受信したメッセージは引数配列に分割され、<see cref="OnPipeMessageReceived"/> イベントで通知されます。
    /// </summary>
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
