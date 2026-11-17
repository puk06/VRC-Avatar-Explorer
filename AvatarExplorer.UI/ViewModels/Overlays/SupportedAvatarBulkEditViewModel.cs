using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

/// <summary>
/// 対応アバターが未設定のアイテムを、全アバター対応または未対応として一括設定するダイアログの ViewModel です。
/// </summary>
public partial class SupportedAvatarBulkEditViewModel : ViewModelBase
{
    [Reactive] public partial bool IsVisible { get; set; }
    [Reactive] public partial string AffectedItemsText { get; set; } = string.Empty;

    private TaskCompletionSource<bool?> _tcs = new();

    public IReactiveCommand UseAllCommand { get; }
    public IReactiveCommand UseNoneCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public SupportedAvatarBulkEditViewModel()
    {
        UseAllCommand = ReactiveCommand.Create(() => Complete(true));
        UseNoneCommand = ReactiveCommand.Create(() => Complete(false));
        CancelCommand = ReactiveCommand.Create(() => Complete(null));
    }

    /// <summary>
    /// ダイアログを表示し、選択結果を返します。
    /// </summary>
    /// <param name="affectedItemCount">対応アバターが未設定のアイテム数。</param>
    /// <returns>全アバター対応として扱う場合は true、どのアバターにも対応しないものとして扱う場合は false、キャンセル時は null。</returns>
    public Task<bool?> ShowAsync(int affectedItemCount)
    {
        AffectedItemsText = Localizer.Instance.Get(Loc.SupportedAvatarBulkEdit.AffectedItems, affectedItemCount.ToString());
        _tcs = new();
        IsVisible = true;

        return _tcs.Task;
    }

    private void Complete(bool? supportsAllAvatars)
    {
        IsVisible = false;
        _tcs.TrySetResult(supportsAllAvatars);
    }
}
