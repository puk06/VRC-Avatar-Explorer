using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.ViewModels.Component;
using PDFtoImage;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class PdfViewerViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    public ObservableCollection<PdfPageViewModel> Pages { get; set; } = [];
    [Reactive] public string FileName { get; set; } = string.Empty;
    [Reactive] public string Status { get; set; } = string.Empty;

    public IReactiveCommand CloseCommand { get; }

    private CancellationTokenSource? _cts;

    public PdfViewerViewModel()
    {
        CloseCommand = ReactiveCommand.Create(Close);
    }

    public async void Open(string filePath)
    {
        Reset();
        _cts = new();

        IsVisible = true;
        LoadPages(filePath, _cts.Token);
    }

    private void LoadPages(string filePath, CancellationToken ct)
    {
        Task.Run(async () =>
        {
            using var pdfStream = File.OpenRead(filePath);

            #pragma warning disable CA1416
            var pageSizes = Conversion.GetPageSizes(pdfStream, leaveOpen: true);
            var pages = Enumerable.Range(0, pageSizes.Count);
            var pdfBitmaps = Conversion.ToImagesAsync(pdfStream, pages, leaveOpen: true);
            #pragma warning restore CA1416

            int index = 0;
            await foreach (var pdfBitmap in pdfBitmaps.ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();

                var createdBitmap = CreateBitmap(pdfBitmap);
                pdfBitmap.Dispose();

                ct.ThrowIfCancellationRequested();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Pages.Add(new(
                        createdBitmap,
                        $"Page {index + 1} / {pageSizes.Count}"
                    ));
                }, DispatcherPriority.Normal, ct);

                index++;
            }
        }, ct);
    }
    private static Bitmap CreateBitmap(SKBitmap pdfBitmap)
    {
        using var image = SKImage.FromBitmap(pdfBitmap);
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, 100);
        using var encodedStream = encodedImage.AsStream();

        return new(encodedStream);
    }

    private void Close()
    {
        Reset();
        IsVisible = false;
    }

    private void Reset()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        Pages.ForEach(i => i.Dispose());
        Pages.Clear();
    }
}
