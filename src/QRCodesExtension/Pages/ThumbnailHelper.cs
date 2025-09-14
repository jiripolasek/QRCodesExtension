// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using JPSoftworks.CommandPalette.Extensions.Toolkit.Logging;

namespace JPSoftworks.QrCodesExtension.Pages;

internal static class ThumbnailHelper
{
    public static async Task<IRandomAccessStream?> GetImageThumbnailAsync(string filePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);

        IRandomAccessStream? thumbnail;
        try
        {
            thumbnail = await file.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, 64, ThumbnailOptions.ResizeThumbnail);
        }
        catch (ArgumentOutOfRangeException)
        {
             thumbnail = await file.OpenReadAsync();
        }

        try
        {
            if (thumbnail != null)
            {
                var decoder = await BitmapDecoder.CreateAsync(thumbnail);
                var transform = new BitmapTransform
                {
                    ScaledWidth = 64,
                    ScaledHeight = 64,
                    InterpolationMode = BitmapInterpolationMode.Fant
                };
                var pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.DoNotColorManage);
                var pixels = pixelData.DetachPixelData();
                var encoderStream = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, encoderStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform.ScaledWidth,
                    transform.ScaledHeight, decoder.DpiX, decoder.DpiY, pixels);
                await encoder.FlushAsync();
                encoderStream.Seek(0);
                return encoderStream;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }

        return thumbnail;
    }
}