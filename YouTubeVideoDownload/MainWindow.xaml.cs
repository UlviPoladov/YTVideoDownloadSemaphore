using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode;

namespace YouTubeVideoDownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Semaphore semaphore = new Semaphore(1, 1);

        public MainWindow()
        {
            InitializeComponent();
        }

        static async Task downloadmethod(string videourl, string outputDirectory, ProgressBar progressBar)
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(videourl);

            
            string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

            
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

            if (muxedStreams.Any())
            {
                semaphore.WaitOne();
                var streamInfo = muxedStreams.First();
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(streamInfo.Url);
                var datetime = DateTime.Now;

                string outputFilePath = Path.Combine(outputDirectory, $"{sanitizedTitle}.{streamInfo.Container}");
                using var outputStream = File.Create(outputFilePath);

                var totalBytes = streamInfo.Size;
                var buffer = new byte[4096];
                var bytesRead = 0L;

                while (true)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0) break;

                    await outputStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;

                    var progressPercentage = (int)(bytesRead * 100.0 / totalBytes.Bytes);

                   
                    
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() => progressBar.Value = progressPercentage);
                    }
                    finally
                    {
                        
                    }
                }
                semaphore.Release();
                MessageBox.Show("Download completed!");
                MessageBox.Show($"Video saved as: {outputFilePath} \n{datetime}");
                progressBar.Value = 0;
            }
            else
            {
                MessageBox.Show($"No suitable video stream found for {video.Title}.");
            }
        }

        private async void downloadbtn_Click(object sender, RoutedEventArgs e)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outputDirectory = Path.Combine(desktopPath, "YouTubeDownloads");

            
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            progressBar.Value = 0;
            await downloadmethod(youtubelinktxt.Text, outputDirectory, progressBar);
        }
    }
}
