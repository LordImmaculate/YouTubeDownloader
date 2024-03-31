using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using YoutubeExplode;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Input;
using YoutubeExplode.Videos.Streams;
using System.Linq;
using MediaToolkit;
using MediaToolkit.Model;

namespace YouTube_Downloader_GUI;

public partial class MainWindow : Window
{
    string _outputDirectory;
    string? _videoUrl;
    bool _audioOnly;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Main();
    }
    
    private void Url_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Main();
        }
    }

    private void Main()
    {
        _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        _videoUrl = Url.Text;
        _audioOnly = RadioButtonAudio.IsChecked ?? false;
        Download(_outputDirectory, _videoUrl, _audioOnly);
    }

    async Task Download(string outputDirectory, string? videoUrl, bool audioOnly)
{
    if (videoUrl != null)
    {
        Status.Text = "Downloading...";
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(videoUrl);
        var videoTitle = SanitizeFileName(video.Title);
        if (videoTitle == "" && !audioOnly)
            {
                videoTitle = "Video";
            }

        else if (videoTitle == "")
            {
                videoTitle = "Audio";
            }

            if (!audioOnly)
        {
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            string savedPath = Path.Combine(outputDirectory, videoTitle + "." + streamInfo.Container);
            await youtube.Videos.Streams.DownloadAsync(streamInfo, savedPath);
            Status.Text = $"Downloaded as {savedPath}";
        }

        else
        {
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            string savedPath = Path.Combine(outputDirectory, videoTitle + "." + streamInfo.Container);
            string savedPathMp3 = Path.ChangeExtension(savedPath, ".mp3");
            await youtube.Videos.Streams.DownloadAsync(streamInfo, savedPath);
            ConvertWebmToMp3(savedPath, savedPathMp3);
            File.Delete(savedPath);
            Status.Text = $"Downloaded as {savedPathMp3}";
        }
    }

    else
    {
        Status.Text = "No URL provided";
    }
}
    
    private static string SanitizeFileName(string fileName)
{
    var invalidChars = Path.GetInvalidFileNameChars();
    return new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
}
    
    public void ConvertWebmToMp3(string inputFile, string outputFile)
    {
        var inputFileMedia = new MediaFile { Filename = inputFile };
        var outputFileMedia = new MediaFile { Filename = outputFile };

        using (var engine = new Engine(@"path\to\ffmpeg.exe"))
        {
            engine.Convert(inputFileMedia, outputFileMedia);
        }
    }
}