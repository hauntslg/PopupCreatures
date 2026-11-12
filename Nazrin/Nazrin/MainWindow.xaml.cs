using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace Nazrin
{
    public partial class MainWindow : Window
    {
        private MediaPlayer? audioPlayer;
        private readonly string assetDirectory;

        public MainWindow()
        {
            InitializeComponent();

            assetDirectory = Path.Combine(
                Path.GetTempPath(),
                "Nazrin"
            );

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ExtractResources();

                await Browser.EnsureCoreWebView2Async();

                Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "nazrin.local",
                    assetDirectory,
                    CoreWebView2HostResourceAccessKind.DenyCors
                );

                Browser.CoreWebView2.WebMessageReceived += (_, e) =>
                {
                    var message = e.TryGetWebMessageAsString();

                    if (message == "video-ready")
                    {
                        audioPlayer?.Play();
                    }
                    else if (message == "finished")
                    {
                        Close();
                    }
                };

                Browser.NavigationCompleted += (_, _) =>
                {
                    audioPlayer = new MediaPlayer();

                    audioPlayer.MediaOpened += (_, _) =>
                    {
                        Browser.CoreWebView2.PostWebMessageAsString("start");
                    };

                    audioPlayer.Open(
                        new Uri(Path.Combine(assetDirectory, "irashaimase.mp3"))
                    );
                };

                Browser.Source = new Uri(
                    "https://nazrin.local/index.html"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Nazrin - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Close();
            }
        }

        private void ExtractResources()
        {
            Directory.CreateDirectory(assetDirectory);

            var assembly = Assembly.GetExecutingAssembly();

            string[] resources =
            [
                "index.html",
                "irashaimase-fade-alpha.webm",
                "irashaimase.mp3"
            ];

            foreach (var resource in resources)
            {
                var resourceName = $"Nazrin.{resource}";

                using var stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new FileNotFoundException(
                        $"Embedded resource not found: {resourceName}"
                    );

                using var file = File.Create(
                    Path.Combine(assetDirectory, resource)
                );

                stream.CopyTo(file);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            audioPlayer?.Stop();
            audioPlayer?.Close();

            base.OnClosed(e);
        }
    }
}
