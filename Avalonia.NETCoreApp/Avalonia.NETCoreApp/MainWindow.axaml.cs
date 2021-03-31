using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace Avalonia.NETCoreApp
{
    public class MainWindow : Window
    {
        
        private readonly LibVLC _libVlc = new LibVLC();
        
        private VideoView VideoView;
        private Image image_view;

        private List<TagValue> files;
        public MainWindow()
        {
            InitializeComponent();
            
            VideoView = this.Get<VideoView>("VideoView");
            image_view = this.Get<Image>("image_view");
#if DEBUG
            this.AttachDevTools();
#endif

            "convert -background black -fill white -size 1920x1080 -gravity center  label:'Anthony\nmultiline text test' /home/marcin/test.png".Bash();
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            image_view.Source = new Bitmap(@"/home/marcin/test.png");
            
            files = new List<TagValue>();
            ProcessDirectory("/home/marcin/music");
            MediaPlayer = new MediaPlayer(_libVlc);
            MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
            VideoView.MediaPlayer = MediaPlayer;
            Play();
            Thread thread1 = new Thread(http_task);
            thread1.Start();
            Console.Out.WriteLine("here");
            
            
            
            //////////////////
            // Get the output into a string
            string result = "uname -a".Bash();


            ////////////////////////////

        }
        
        
        
        
        
        private void MediaPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
        {
           Console.Out.WriteLine(e.Position*MediaPlayer.Media.Duration);
        }

        async void http_task()
        {
            
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine ("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            
            
            
            
            
            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            listener.Prefixes.Add("http://localhost:8000/start/");
            
            listener.Start();
            while (true)
            {
                Console.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.
                string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer,0,buffer.Length);
                // You must close the output stream.
                output.Close();
                
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { Play();});
            }
            listener.Stop();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public  void ProcessDirectory(string targetDirectory)
        {
            Console.Out.WriteLine(targetDirectory);
            // Process the list of files found in the directory.
            string [] fileEntries = Directory.GetFiles(targetDirectory);
            foreach(string fileName in fileEntries){
                ProcessFile(fileName);}

            // Recurse into subdirectories of this directory.
            string [] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach(string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public  void ProcessFile(string path)
        {
            Console.WriteLine("Processed file '{0}'.", path);
            if (path.Contains(".wav"))
            {
                var tfile = TagLib.File.Create(@path);
                Console.Out.WriteLine(tfile.Tag.Title);
                Console.Out.WriteLine(tfile.Tag.Album);
                Console.Out.WriteLine(tfile.Tag.Comment);

                TagValue file_info = null;
                try
                {
                    file_info = JsonSerializer.Deserialize<TagValue>(tfile.Tag.Comment);
                }
                catch
                {
                    file_info = new TagValue();
                }

                file_info.title = tfile.Tag.Title;
                 file_info.album = tfile.Tag.Album;
                 file_info.path = path;
                 files.Add(file_info);
            }
        }
        
        public MediaPlayer MediaPlayer { get; }

        public void Play()
        {
            if (!MediaPlayer.IsPlaying)
            {
                if (MediaPlayer.Media == null)
                {
                    using var media = new LibVLCSharp.Shared.Media(_libVlc,
                        new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
                    MediaPlayer.Play(media);

                }
                else
                {
                    MediaPlayer.Play();
                }
                
                image_view.IsVisible = false;
                VideoView.IsVisible = true;

                MediaPlayer.Fullscreen = true;
            }
            else
            {
                MediaPlayer.Pause();
                
                image_view.IsVisible = true;
                VideoView.IsVisible = false;
            }

        }
    }
}