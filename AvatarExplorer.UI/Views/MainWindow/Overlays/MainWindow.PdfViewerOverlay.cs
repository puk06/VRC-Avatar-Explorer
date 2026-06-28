using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using PDFtoImage;
using SkiaSharp;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    internal sealed class PdfPageViewModel(Bitmap image, string title) : IDisposable
    {
        public Bitmap Image { get; } = image;
        public string Title { get; } = title;

        public void Dispose() => Image.Dispose();
    }

    private List<PdfPageViewModel>? _pdfViewerOverlay_pageItems;

    private async Task PdfViewerOverlay_OpenAsync(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed], isError: true);
            return;
        }

        PdfViewerOverlay_Close();
        PdfViewerOverlay_FileName.Text = Path.GetFileName(pdfPath);
        PdfViewerOverlay_StatusText.Text = "Loading...";
        PdfViewerOverlay.IsVisible = true;

        try
        {
            var pageItems = await Task.Run(() => PdfViewerOverlay_LoadPages(pdfPath));
            _pdfViewerOverlay_pageItems = pageItems;
            PdfViewerOverlay_PageItemsControl.ItemsSource = pageItems;
            PdfViewerOverlay_StatusText.Text = $"{pageItems.Count} pages";
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to load PDF viewer overlay content. '{pdfPath}'", ex);
            PdfViewerOverlay_StatusText.Text = "Failed to load PDF.";
        }
    }

    private static List<PdfPageViewModel> PdfViewerOverlay_LoadPages(string pdfPath)
    {
        using var pdfStream = File.OpenRead(pdfPath);
#pragma warning disable CA1416
        var pageSizes = Conversion.GetPageSizes(pdfStream, leaveOpen: true);
        var pages = Enumerable.Range(0, pageSizes.Count);
        var pdfBitmaps = Conversion.ToImages(pdfStream, pages, leaveOpen: true);
#pragma warning restore CA1416

        var pageItems = new List<PdfPageViewModel>(pageSizes.Count);

        int index = 0;
        foreach (var pdfBitmap in pdfBitmaps)
        {
            pageItems.Add(new PdfPageViewModel(
                PdfViewerOverlay_CreateBitmap(pdfBitmap),
                $"Page {index + 1} / {pageSizes.Count}"
            ));

            pdfBitmap.Dispose();
            index++;
        }

        return pageItems;
    }

    private static Bitmap PdfViewerOverlay_CreateBitmap(SKBitmap pdfBitmap)
    {
        using var image = SKImage.FromBitmap(pdfBitmap);
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, 100);
        using var encodedStream = encodedImage.AsStream();
        return new(encodedStream);
    }

    private void PdfViewerOverlay_Close()
    {
        PdfViewerOverlay_PageItemsControl.ItemsSource = null;
        if (_pdfViewerOverlay_pageItems != null)
        {
            foreach (var pageItem in _pdfViewerOverlay_pageItems)
            {
                pageItem.Dispose();
            }

            _pdfViewerOverlay_pageItems = null;
        }

        PdfViewerOverlay.IsVisible = false;
        PdfViewerOverlay_FileName.Text = string.Empty;
        PdfViewerOverlay_StatusText.Text = string.Empty;
    }

    private void PdfViewerOverlay_Close_Click(object? sender, RoutedEventArgs e) => PdfViewerOverlay_Close();
}
