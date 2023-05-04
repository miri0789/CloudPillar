using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using static FirmwareUpdateAgent.Program;


public partial class ProgressWindow
{
    public class AvaliniaProgressApp : Application, IProgressObserver
    {
        private ProgressWindow _progressWindow;
        public bool IsWindowOpen => _progressWindow != null && !_isWindowClosed;
        private bool _isWindowClosed;

        public AvaliniaProgressApp()
        {
            // _progressWindow = new ProgressWindow();
        }

        public override void Initialize()
        {
            // Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();
            ShowProgressWindow();
        }

        private void ShowProgressWindow()
        {
            Dispatcher.UIThread.Post(async () => {
                if (_progressWindow == null)
                {
                    _progressWindow = new ProgressWindow();
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        desktop.MainWindow = _progressWindow;
                    }
                    _progressWindow.Closed += (sender, e) => _isWindowClosed = true;
                }

                if (!_progressWindow.IsVisible)
                {
                    _progressWindow.Show();
                    _isWindowClosed = false;
                }

                _progressWindow.Activate();
            });
        }

        public void ReportProgress(string fileName, int percentage, bool isUpload)
        {
            // Console.WriteLine($"Dispatcher.UIThread: {Dispatcher.UIThread}");

            if (!IsWindowOpen)
            {
                _progressWindow = null;
                ShowProgressWindow();
            }

            Dispatcher.UIThread.Post(async () =>
            {
                _progressWindow.UpdateProgress(fileName, percentage, isUpload);
            });
        }

        public void InitProgressObserver(CancellationToken cancellationToken = default, string[]? args = null)
        {
            var appBuilder = BuildAvaloniaApp();
            var lifetime = new ClassicDesktopStyleApplicationLifetime
            {
                Args = args ?? Array.Empty<string>(),
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };
        
            using (cancellationToken.Register(() => Dispatcher.UIThread.Post(() => lifetime.Shutdown())))
            {
                appBuilder.SetupWithLifetime(lifetime);
                lifetime.Start(args);
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<AvaliniaProgressApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI()
            .With(new AvaloniaNativePlatformOptions { UseGpu = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) })
            .With(new Win32PlatformOptions { AllowEglInitialization = true })
            .With(new X11PlatformOptions { UseGpu = true });
    }

}
