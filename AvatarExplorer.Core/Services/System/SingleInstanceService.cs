using System.IO.Pipes;

namespace AvatarExplorer.Core.Services.System;

/// <summary>
/// 名前付きパイプを使用してアプリケーションの単一インスタンス化を実現するサービス。
/// 既に起動しているサーバーインスタンスへコマンドライン引数を転送します。
/// </summary>
public static class SingleInstanceService
{
    private const string PipeName = "AvatarExplorerV2.Pipe";
    private const char ArgumentSeparator = '|';

    /// <summary>パイプメッセージ（コマンドライン引数の配列）を受信したときに発生するイベント。</summary>
    private static event Action<string[]>? OnPipeMessageReceived = null;

    /// <summary>購読者がいない間に受信したメッセージの保留キュー。</summary>
    private static readonly Queue<string[]> _pendingMessages = new();
    private static readonly Lock _syncLock = new();

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
    /// 戻り時点でパイプの作成は完了しているため、直後から接続を受け付けられます。
    /// 受信したメッセージは引数配列に分割され、購読者へ通知されます。
    /// </summary>
    public static void StartServer()
    {
        NamedPipeServerStream firstServer;

        try
        {
            firstServer = CreateServer();
        }
        catch
        {
            // パイプを作成できない場合は引数転送を行わない
            return;
        }

        Task.Run(() => AcceptLoopAsync(firstServer));
    }

    /// <summary>
    /// パイプメッセージの購読を開始します。
    /// 購読前に受信して保留されていたメッセージは、このメソッドの呼び出しスレッド上でまとめて通知されます。
    /// </summary>
    /// <param name="handler">受信したコマンドライン引数を受け取るハンドラ。</param>
    public static void Subscribe(Action<string[]> handler)
    {
        string[][] pendingMessages;

        lock (_syncLock)
        {
            OnPipeMessageReceived += handler;
            pendingMessages = [.. _pendingMessages];
            _pendingMessages.Clear();
        }

        foreach (var args in pendingMessages)
        {
            handler(args);
        }
    }

    private static NamedPipeServerStream CreateServer()
        => new(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    private static async Task AcceptLoopAsync(NamedPipeServerStream server)
    {
        while (true)
        {
            try
            {
                await server.WaitForConnectionAsync();

                using (var reader = new StreamReader(server, leaveOpen: true))
                {
                    var message = await reader.ReadLineAsync() ?? string.Empty;
                    Dispatch(message.Split(ArgumentSeparator));
                }

                server.Disconnect();
            }
            catch
            {
                // 1接続分の失敗は無視して次の接続を待つ
                try { server.Disconnect(); } catch { /* Ignored */ }
                await Task.Delay(100);
            }
        }
    }

    private static void Dispatch(string[] args)
    {
        lock (_syncLock)
        {
            if (OnPipeMessageReceived == null)
            {
                // 購読前（起動処理中など）に届いたメッセージは、購読時に通知する
                _pendingMessages.Enqueue(args);
                return;
            }

            OnPipeMessageReceived.Invoke(args);
        }
    }
}
