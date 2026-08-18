using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using AvaloniaContextMenu = Avalonia.Controls.ContextMenu;
using AvaloniaMenuItem = Avalonia.Controls.MenuItem;

namespace AvatarExplorer.UI.Models.ContextMenu;

/// <summary>
///   <see cref="AvaloniaContextMenu"/> とその子 <see cref="AvaloniaMenuItem"/> に登録した
///   Click イベントハンドラーを追跡し、<see cref="Dispose"/> で一括解除します。
///   これにより、イベントハンドラー経由の ItemViewModel への参照保持
///   (メモリリーク) を防ぎます。
/// </summary>
internal sealed class ContextMenuHolder(AvaloniaContextMenu? menu) : IDisposable
{
    public AvaloniaContextMenu? Menu { get; } = menu;

    private readonly List<(AvaloniaMenuItem item, EventHandler<RoutedEventArgs> handler)> _handlers = [];
    private bool _disposed;

    public void AddClickHandler(AvaloniaMenuItem item, EventHandler<RoutedEventArgs> handler)
    {
        item.Click += handler;
        _handlers.Add((item, handler));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var (item, handler) in _handlers)
            item.Click -= handler;

        _handlers.Clear();

        // Items をクリアして MenuItem → ハンドラー → ItemViewModel
        // の参照チェーンを確実に断ち切る
        Menu?.Items.Clear();
    }
}
