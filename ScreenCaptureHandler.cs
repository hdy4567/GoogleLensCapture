using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;

namespace GoogleLensCapture
{
    public static class ScreenCaptureHandler
    {
        private static Bitmap? _atomicBitmap;

        public static void HandleCapture()
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("[1] Hotkey Received.");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Clipboard.Clear());

                    LaunchSnippingTool();
                    bool captureSuccess = await WaitForClipboardImageAsync(15000);

                    if (captureSuccess)
                    {
                        Console.WriteLine("[2] Capture SUCCESS.");
                        string? imagePath = SaveClipboardImageToTemp();
                        if (imagePath != null)
                        {
                            await FastClipboardUpload(imagePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                }
            });
        }

        private static void LaunchSnippingTool()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "ms-screenclip:", UseShellExecute = true });
            }
            catch { Process.Start("SnippingTool.exe"); }
        }

        private static async Task<bool> WaitForClipboardImageAsync(int maxWaitMs)
        {
            int elapsed = 0;
            while (elapsed < maxWaitMs)
            {
                bool hasImage = false;
                try { hasImage = System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Clipboard.ContainsImage()); } catch { }
                if (hasImage) return true;
                await Task.Delay(100);
                elapsed += 100;
            }
            return false;
        }

        private static string? SaveClipboardImageToTemp()
        {
            try
            {
                return System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var image = System.Windows.Clipboard.GetImage();
                    if (image == null) return null;

                    Console.WriteLine("[3] Saving image...");
                    string tempPath = Path.Combine(Path.GetTempPath(), "lens_capture.png");
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                        encoder.Save(fs);
                    }
                    return tempPath;
                });
            }
            catch { return null; }
        }

        private static async Task FastClipboardUpload(string imagePath)
        {
            try
            {
                Console.WriteLine("[4] Preparing fast clipboard upload...");

                // 원자적 비트맵 복제
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (File.Exists(imagePath))
                    {
                        byte[] bytes = File.ReadAllBytes(imagePath);
                        using (var ms = new MemoryStream(bytes))
                        {
                            using (var original = Image.FromStream(ms))
                            {
                                _atomicBitmap?.Dispose();
                                _atomicBitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppPArgb);
                                using (var g = Graphics.FromImage(_atomicBitmap))
                                {
                                    g.DrawImage(original, new Rectangle(0, 0, _atomicBitmap.Width, _atomicBitmap.Height));
                                }

                                System.Windows.Forms.Clipboard.SetImage(_atomicBitmap);
                                Console.WriteLine("[5] Clipboard READY.");
                            }
                        }
                    }
                });

                // 브라우저 실행
                Process.Start(new ProcessStartInfo { FileName = "https://lens.google.com/search?p", UseShellExecute = true });

                // 최적화된 1000ms 대기
                Console.WriteLine("[6] Waiting 1000ms...");
                await Task.Delay(1000);

                // 붙여넣기
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Console.WriteLine("[7] Pasting...");
                    System.Windows.Forms.SendKeys.SendWait("^v");
                });

                // 이미지 로드 대기
                Console.WriteLine("[8] Waiting for image load...");
                await Task.Delay(500);

                // 검색 트리거
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Console.WriteLine("[9] Triggering search...");
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                    Console.WriteLine("[10] COMPLETE!");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }
    }
}
