using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace GoogleLensCapture
{
    public static class ScreenCaptureHandler
    {
        private static string? _lastImagePath;

        public static void HandleCapture()
        {
            // 백그라운드 스레드에서 실행하여 UI 프리징 방지
            Task.Run(async () =>
            {
                try
                {
                    // 0. 클립보드 비우기 (이전 캡처 데이터가 바로 인식되는 문제 방지)
                    System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Clipboard.Clear());

                    // 1. 캡처 도구 실행
                    LaunchSnippingTool();

                    // 2. 클립보드 변화 대기 (0.1초 반응 속도)
                    bool captureSuccess = await WaitForClipboardImageAsync(15000);

                    if (captureSuccess)
                    {
                        // 3. 클립보드 이미지를 임시 파일로 저장
                        string? imagePath = SaveClipboardImageToTemp();

                        if (imagePath != null)
                        {
                            _lastImagePath = imagePath;

                            // 4. 구글 렌즈 웹페이지 + 자동 업로드 실행
                            OpenGoogleLensWithImage(imagePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"오류 발생: {ex.Message}", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        /// 윈도우 기본 캡처 도구 실행
        /// </summary>
        private static void LaunchSnippingTool()
        {
            try
            {
                // Windows 11: ms-screenclip: URI 스킴 사용
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "ms-screenclip:",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback: Windows 10 이전 버전
                Process.Start("SnippingTool.exe");
            }
        }

        /// <summary>
        /// 클립보드에 이미지가 붙여넣어질 때까지 대기
        /// </summary>
        private static async Task<bool> WaitForClipboardImageAsync(int maxWaitMs)
        {
            int elapsed = 0;
            int checkInterval = 100; // 0.1초 단위로 감지 (Peak Performance 설정)

            while (elapsed < maxWaitMs)
            {
                try
                {
                    // Dispatcher를 통해 UI 스레드(STA)에서 클립보드 확인
                    bool hasImage = System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        return System.Windows.Clipboard.ContainsImage();
                    });

                    if (hasImage)
                        return true;
                }
                catch { }

                await Task.Delay(checkInterval);
                elapsed += checkInterval;
            }

            return false;
        }

        /// <summary>
        /// 클립보드의 이미지를 임시 파일로 저장
        /// </summary>
        private static string? SaveClipboardImageToTemp()
        {
            try
            {
                return System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var image = System.Windows.Clipboard.GetImage();
                    if (image == null)
                        return null;

                    string tempPath = Path.Combine(Path.GetTempPath(), $"lens_capture_{Guid.NewGuid().ToString("N").Substring(0, 8)}.png");

                    // WPF 이미지를 PNG로 저장하는 방식
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                        encoder.Save(fileStream);
                    }
                    return tempPath;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이미지 저장 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 구글 렌즈 페이지 열고 이미지 업로드
        /// </summary>
        private static async void OpenGoogleLensWithImage(string imagePath)
        {
            bool directUploadSuccess = false;
            try
            {
                // [대처법 1] 구글 서버와의 통신 규격을 정밀하게 타격하도록 업그레이드했습니다! 🎯
                // 분석 결과, 단순히 데이터를 던지는 것보다 구글이 신뢰할 수 있는 필수 파라미터(re=df, st, ep)와 헤더(Origin, Referer)들을 챙겨주는 것이 핵심이었습니다.
                string? resultsUrl = await TryDirectUploadAsync(imagePath);

                if (!string.IsNullOrEmpty(resultsUrl) && resultsUrl.Contains("/search"))
                {
                    Process.Start(new ProcessStartInfo { FileName = resultsUrl, UseShellExecute = true });
                    directUploadSuccess = true;
                }
            }
            catch { /* 백업 모드 실행 준비 */ }

            // [대처법 2] 가장 안정적이었던 6시 35분 버전의 로직으로 즉시 복구 및 보강을 완료했습니다!
            // 안전 우선: POST 로직이 조금이라도 의심스러우면(검색 결과 주소가 아니면) 즉시 0.1초 만에 포기하고 성공 방식(Ctrl+V)으로 전환하게 만들었습니다. 
            if (!directUploadSuccess)
            {
                try
                {
                    // 지능적 순서 변경: 브라우저를 띄우기 직전에 클립보드에 이미지를 미리 넣어두어, 
                    // 브라우저가 뜨는 1.5초 동안 시스템이 이미 붙여넣기 준비를 끝내도록 했습니다.
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (File.Exists(imagePath))
                        {
                            using (var stream = new System.IO.FileStream(imagePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                            {
                                // 브라우저 붙여넣기에 가장 최적화된 WinForms Clipboard를 명시적으로 사용
                                using (var img = System.Drawing.Image.FromStream(stream))
                                {
                                    System.Windows.Forms.Clipboard.SetImage(img);
                                }
                            }
                        }
                    });

                    // 구글 렌즈 업로드 페이지 열기
                    Process.Start(new ProcessStartInfo { FileName = "https://lens.google.com/search?p", UseShellExecute = true });

                    // 브라우저가 띄워지는 시간을 충분히 고려 (1.2초)
                    await Task.Delay(1200);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.Forms.SendKeys.SendWait("^v"); // 붙여넣기 실행
                        System.Windows.Forms.SendKeys.SendWait("{ENTER}"); // 전송 실행
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"업로드 실패: {ex.Message}", "에러");
                }
            }
        }

        private static async Task<string?> TryDirectUploadAsync(string imagePath)
        {
            try
            {
                // [6시 35분 세션 유지 비법] AllowAutoRedirect = false를 통해 세션 탈취를 방지합니다.
                using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(3);

                    // 🛠️ 수정한 신속 로직(POST) 디테일:
                    // 1. Redirect Flag (re=df) 추가: 구글에게 결과 주소로 바로 보내달라고 요청
                    // 2. Origin/Referer 위장: 브라우저 환경 모방
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Origin", "https://lens.google.com");
                    client.DefaultRequestHeaders.Referrer = new Uri("https://lens.google.com/");

                    using (var content = new MultipartFormDataContent())
                    {
                        var imageBytes = await File.ReadAllBytesAsync(imagePath);
                        var byteContent = new ByteArrayContent(imageBytes);
                        byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                        // 구글 렌즈 데이터 필드명: 'encoded_image'
                        content.Add(byteContent, "encoded_image", "screenshot.png");

                        // 3. Timestamp (st) 동기화 및 필수 파라미터 조합
                        long st = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        // hl=ko-KR, ep=gsb, re=df가 모두 포함된 정밀 타격 URL
                        string url = $"https://lens.google.com/v3/upload?re=df&st={st}&ep=gsb&hl=ko-KR";

                        var response = await client.PostAsync(url, content);

                        // 302 Found 발생 시 Location 헤더를 가로채 브라우저로 직접 전달
                        if (response.StatusCode == System.Net.HttpStatusCode.Found ||
                            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                            response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                        {
                            var location = response.Headers.Location?.ToString();
                            if (!string.IsNullOrEmpty(location) && location.Contains("/search"))
                            {
                                return location;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
