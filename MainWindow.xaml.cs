using Microsoft.Win32;
using System.Drawing.Imaging.Effects;
using System.Security.RightsManagement;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Image_modify_wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private BitmapImage _originalBitmap;

    public void InputButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            _originalBitmap = new BitmapImage(new Uri(openFileDialog.FileName));
            InputImage.Source = _originalBitmap;
        }
    }

    public void OutputButton_Click(object sender, RoutedEventArgs e)
    {
        if(_originalBitmap == null)  return; // Safety check
        InputImage.Source = OutputImage.Source;
       _originalBitmap = ConvertWriteableBitmapToBitmapImage((WriteableBitmap)OutputImage.Source);
    //    _originalBitmap =  (BitmapImage) OutputImage.Source;

        OutputImage.Source = null;
    }

    private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
    {
        BitmapImage bmImage = new BitmapImage();
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(wbm));
            encoder.Save(stream);
            bmImage.BeginInit();
            bmImage.CacheOption = BitmapCacheOption.OnLoad;
            bmImage.StreamSource = stream;
            bmImage.EndInit();
            bmImage.Freeze(); // Crucial for performance and thread safety
        }
        return bmImage;
    }

    public void InvertEffect()
    {
        if (_originalBitmap == null) return;

        WriteableBitmap bitmap = new WriteableBitmap(_originalBitmap);
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;

        byte[] sourceData = new byte[height * stride];
        byte[] resultData = new byte[height * stride]; // We need a second array!

        bitmap.CopyPixels(sourceData, stride, 0);

        for (int i = 0; i < sourceData.Length; i += bytesPerPixel)
        {
            // Calculate the destination index: 
            // We want the last pixel's start position, then move backwards
            int destIndex = sourceData.Length - i - bytesPerPixel;
            resultData[destIndex] = sourceData[i];     // Blue
            resultData[destIndex + 1] = sourceData[i + 1]; // Green
            resultData[destIndex + 2] = sourceData[i + 2]; // Red
            resultData[destIndex + 3] = sourceData[i + 3]; // Alpha
        }

        Int32Rect rect = new Int32Rect(0, 0, width, height);
        bitmap.WritePixels(rect, resultData, stride, 0);
        OutputImage.Source = bitmap;
    }
    public void BinaryEffect()
    {
        if(_originalBitmap == null) return;
        WriteableBitmap bitmap = new WriteableBitmap(_originalBitmap);
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;
        byte[] pixelData = new byte[height * stride];
        bitmap.CopyPixels(pixelData, stride, 0);
        for(int i=0; i < pixelData.Length; i += bytesPerPixel)
        {
            byte blue = pixelData[i];
            byte green = pixelData[i + 1];
            byte red = pixelData[i + 2];
            byte gray = (byte)((red + green + blue) / 3);
            byte binaryValue = (gray >= 128) ? (byte)255 : (byte)0;
            pixelData[i] = binaryValue;     // Blue
            pixelData[i + 1] = binaryValue; // Green
            pixelData[i + 2] = binaryValue; // Red
        }
        Int32Rect rect = new Int32Rect(0, 0, width, height);
        bitmap.WritePixels(rect, pixelData, stride, 0);
        OutputImage.Source = bitmap;
    }
    public void BlurEffect()
    {
        if(_originalBitmap == null) return;
        WriteableBitmap bitmap = new WriteableBitmap(_originalBitmap);  
        int width = bitmap.PixelWidth;  
        int height = bitmap.PixelHeight;
        int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;
        byte[] sourceData = new byte[height * stride];
        byte[] resultData = new byte[height * stride];
        bitmap.CopyPixels(sourceData, stride, 0);
        int[,] kernel = new int[,]
        {
            { 0, 1, 0 },
            { 1, 5, 1 },
            { 0, 1, 0 }
        };
        int kernelSum = 9; // Sum of the kernel values
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int blueSum = 0, greenSum = 0, redSum = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int pixelIndex = ((y + ky) * stride) + ((x + kx) * bytesPerPixel);
                        blueSum += sourceData[pixelIndex] * kernel[ky + 1, kx + 1];
                        greenSum += sourceData[pixelIndex + 1] * kernel[ky + 1, kx + 1];
                        redSum += sourceData[pixelIndex + 2] * kernel[ky + 1, kx + 1];
                    }
                }
                int destIndex = (y * stride) + (x * bytesPerPixel);
                resultData[destIndex] = (byte)(blueSum / kernelSum);
                resultData[destIndex + 1] = (byte)(greenSum / kernelSum);
                resultData[destIndex + 2] = (byte)(redSum / kernelSum);
                resultData[destIndex + 3] = sourceData[destIndex + 3];
            }
        }
        Int32Rect rect = new Int32Rect(0, 0, width, height);
        bitmap.WritePixels(rect, resultData, stride, 0);
        OutputImage.Source = bitmap;
    }
    public void SharpenEffect()
    {
        if(_originalBitmap == null) return;
        WriteableBitmap bitmap = new WriteableBitmap(_originalBitmap);
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;
        byte[] sourceData = new byte[height * stride];
        byte[] resultData = new byte[height * stride];
        bitmap.CopyPixels(sourceData, stride, 0);
        int[,] kernel = new int[,]
        {
            { 0, -1, 0 },
            { -1, 5, -1 },
            { 0, -1, 0 }
        };
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int blueSum = 0, greenSum = 0, redSum = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int pixelIndex = ((y + ky) * stride) + ((x + kx) * bytesPerPixel);
                        blueSum += sourceData[pixelIndex] * kernel[ky + 1, kx + 1];
                        greenSum += sourceData[pixelIndex + 1] * kernel[ky + 1, kx + 1];
                        redSum += sourceData[pixelIndex + 2] * kernel[ky + 1, kx + 1];
                    }
                }
                int destIndex = (y * stride) + (x * bytesPerPixel);
                resultData[destIndex] = (byte)Math.Min(Math.Max(blueSum, 0), 255);
                resultData[destIndex + 1] = (byte)Math.Min(Math.Max(greenSum, 0), 255);
                resultData[destIndex + 2] = (byte)Math.Min(Math.Max(redSum, 0), 255);
                resultData[destIndex + 3] = sourceData[destIndex + 3];
            }
        }
        Int32Rect rect = new Int32Rect(0, 0, width, height);
        bitmap.WritePixels(rect, resultData, stride, 0);
        OutputImage.Source = bitmap;
    }   
    public void EdgeDetectionEffect()
    {
        if(_originalBitmap == null) return;
        WriteableBitmap bitmap = new WriteableBitmap(_originalBitmap);
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;
        byte[] sourceData = new byte[height * stride];
        byte[] resultData = new byte[height * stride];
        bitmap.CopyPixels(sourceData, stride, 0);
        int[,] kernel = new int[,]
        {
            { -1, -1, -1 },
            { -1, 8, -1 },
            { -1, -1, -1 }
        };
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int blueSum = 0, greenSum = 0, redSum = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        int pixelIndex = ((y + ky) * stride) + ((x + kx) * bytesPerPixel);
                        blueSum += sourceData[pixelIndex] * kernel[ky + 1, kx + 1];
                        greenSum += sourceData[pixelIndex + 1] * kernel[ky + 1, kx + 1];
                        redSum += sourceData[pixelIndex + 2] * kernel[ky + 1, kx + 1];
                    }
                }
                int destIndex = (y * stride) + (x * bytesPerPixel);
                resultData[destIndex] = (byte)Math.Min(Math.Max(blueSum, 0), 255);
                resultData[destIndex + 1] = (byte)Math.Min(Math.Max(greenSum, 0), 255);
                resultData[destIndex + 2] = (byte)Math.Min(Math.Max(redSum, 0), 255);
                resultData[destIndex + 3] = sourceData[destIndex + 3];
            }
        }
        Int32Rect rect = new Int32Rect(0, 0, width, height);
        bitmap.WritePixels(rect, resultData, stride, 0);
        OutputImage.Source = bitmap;
    }   
    public void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (_originalBitmap == null) return;
        if (TextInput.Text == "Gray")
        {
            GrayScaleEffect();
        }
        else if(TextInput.Text == "Invert")
        {
            InvertEffect();
        }
        else if(TextInput.Text == "Binary")
        {
            BinaryEffect();
        }
        else if(TextInput.Text == "Blur")
        {
            BlurEffect();
        }
        else if(TextInput.Text == "Sharpen")
        {
            SharpenEffect();
        }
        else if(TextInput.Text == "Edge")
        {
            EdgeDetectionEffect();
        }
        else
        {
            MessageBox.Show("Effect not recognized. Do you want to apply the GrayScale effect instead?", "Effect Not Found", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }
    }
    public void GrayScaleEffect()
    {
        if (_originalBitmap == null) return; // Safety check

        WriteableBitmap grayBitmap = new WriteableBitmap(_originalBitmap);
        int width = grayBitmap.PixelWidth;
        int height = grayBitmap.PixelHeight;
        int bytesPerPixel = (grayBitmap.Format.BitsPerPixel + 7) / 8;
        int stride = width * bytesPerPixel;
        byte[] pixelData = new byte[height * stride];

        grayBitmap.CopyPixels(pixelData, stride, 0);

        for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
        {
            byte blue = pixelData[i];
            byte green = pixelData[i + 1];
            byte red = pixelData[i + 2];

            byte gray = (byte)(red * 0.299 + green * 0.587 + blue * 0.114);

            pixelData[i] = gray;     // Blue
            pixelData[i + 1] = gray; // Green
            pixelData[i + 2] = gray; // Red
        }

        Int32Rect rect = new Int32Rect(0, 0, width, height);
        grayBitmap.WritePixels(rect, pixelData, stride, 0);
        OutputImage.Source = grayBitmap;
    }
    public void TextInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        
    }
}