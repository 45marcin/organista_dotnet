using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
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
using Avalonia.Threading;
using HttpServerModule;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace Avalonia.NETCoreApp
{
    public class MainWindow : Window
    {
        private status _status = new status();
        private readonly LibVLC _libVlc = new LibVLC();
        
        private VideoView VideoView;
        private Image image_view;

        private List<TagValue> files;

        public int stop_point = -1;
        public  List<text_item> nowShowinText;
        public List<int> stopPoints;

        private string status = "idle";  //playing_audio, playing video, refreshing files

        public MainWindow()
        {
            InitializeComponent();
            
            initAudio();
            
            VideoView = this.Get<VideoView>("VideoView");
            image_view = this.Get<Image>("image_view");
#if DEBUG
            this.AttachDevTools();
#endif

            
            
            files = new List<TagValue>();
            //ProcessDirectory("/home/marcin/music");
            MediaPlayer = new MediaPlayer(_libVlc);
            MediaPlayer.Volume = 100;
            MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
            MediaPlayer.EndReached  += MediaPlayerOnEndReached;
            VideoView.MediaPlayer = MediaPlayer;
            //Play();
            Thread thread1 = new Thread(http_task);
            thread1.Start();
            Console.Out.WriteLine("here");
            
            
            
            //////////////////
            // Get the output into a string
            string result = "uname -a".Bash();


            ////////////////////////////
            ///
            ///



            HttpServerModule.HttpServer  server = new HttpServer();
            server.newRequest += ServerOnnewRequest;

            UdpServer udpServer = new UdpServer();

        }

        void initAudio()
        {
            readVolume();
            readBalance();
            
            SetBalance(_status.AudioBalance);
            SetVolume(_status.AudioVolume);
        }
        private void MediaPlayerOnEndReached(object? sender, EventArgs e)
        {
            setImageBlank();
        }
        private async void ServerOnnewRequest(object? sender, EventArgs e)
        {
                HttpRequestsArgs args = (HttpRequestsArgs) e;
            try
            {
                string body = null;
                if (args.request.HasEntityBody)
                {
                    byte[] body_bytes = new byte[args.request.ContentLength64];
                    args.request.InputStream.Read(body_bytes, 0, body_bytes.Length);
                    body = System.Text.Encoding.UTF8.GetString(body_bytes);
                }

                switch (args.request.HttpMethod)
                {
                    case "GET":
                        if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0 && args.request.QueryString.GetValues("audio")[0].Equals("get_files"))
                        {
                            HttpServer.SendRespone(args.response, JsonSerializer.Serialize(files), 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("device") != null && args.request.QueryString.GetValues("device").Length > 0  && args.request.QueryString.GetValues("device")[0].Equals("status"))
                        {
                            HttpServer.SendRespone(args.response, JsonSerializer.Serialize(_status), 200);
                        }
                        else
                        {
                            HttpServer.SendRespone(args.response, "unrecognized query string", 404);
                        }
                        break;
                    case "POST":
                        if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("play"))
                        {
                            if (args.request.QueryString.GetValues("file") != null)
                            {
                                PlayAudio(args.request.QueryString.GetValues("file")[0]);
                            }
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("stop"))
                        {
                            Stop();
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("stop_time"))
                        {
                            if (stopPoints != null)
                            {
                                int current = Convert.ToInt32(MediaPlayer.Position*MediaPlayer.Media.Duration);
                                foreach(var time in stopPoints)
                                {
                                    if (time > current)
                                    {
                                        stop_point = time;
                                        HttpServer.SendRespone(args.response, "set time stop point successfull", 200);
                                        _status.stopTime = true;
                                        return;
                                    }
                                }
                            }
                            HttpServer.SendRespone(args.response, "set time stop point impossible", 501);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("stop_time_cancek"))
                        {
                            try
                            {
                                stop_point = -1;
                                _status.stopTime = false;
                            }
                            catch (Exception exception)
                            {
                                HttpServer.SendRespone(args.response, "clearing time stop unsuccessfull", 200);
                                return;
                            }

                            HttpServer.SendRespone(args.response, "clearing time stop point successfull", 200);
                            return;
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("set_volume"))
                        {
                            if (args.request.QueryString.GetValues("value") != null)
                            {
                                SetVolume(Convert.ToInt32(args.request.QueryString.GetValues("value")[0].ToString()));
                            }
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("balance"))
                        {
                            if (args.request.QueryString.GetValues("value") != null)
                            {
                                SetBalance(Convert.ToInt32(args.request.QueryString.GetValues("value")[0].ToString()));
                            }
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else
                        {
                            HttpServer.SendRespone(args.response, "unrecognized query string", 404);
                        }
                        
                        break;
                    case "PUT":
                        HttpServer.SendRespone(args.response, "PUT", 200);
                        break;
                }
            }
            catch
            {
                HttpServer.SendRespone(args.response, "INTERNAL ERROR", 500);
            }

        }

        
        




        private void MediaPlayerOnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
        {
            try
            {
                float currentTime = e.Position*MediaPlayer.Media.Duration;
                if (nowShowinText != null && e.Position*MediaPlayer.Media.Duration/1000 > Convert.ToSingle(nowShowinText[0].time))
                {
                    setImageText(nowShowinText[0].text);
                    nowShowinText.RemoveAt(0);
                    if (nowShowinText.Count == 0)
                    {
                        nowShowinText = null;
                    }
                }
                if (stop_point > -1 && currentTime > stop_point)
                {
                    Stop();
                }
            }
            catch
            {
                
            }
        }

        async void http_task()
        {
            
            
        }
        async void setImageText(string text)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            { 
                
                ("convert -background black -fill white -size 1920x1080 -gravity center  label:'" + text + "' /home/marcin/test.png").Bash();
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                image_view.Source = new Bitmap(@"/home/marcin/test.png");
                
                image_view.IsVisible = true;
                VideoView.IsVisible = false;
                MediaPlayer.Fullscreen = true; 
            });
        }

        async void setImageBlank()
        {
            _status.imagePlaying = false;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                image_view.Source = null;
                
                image_view.IsVisible = true;
                VideoView.IsVisible = false;
                MediaPlayer.Fullscreen = true; 
            });
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
        
        public void PlayAudio(string file_path)
        {
            
            setImageBlank();
            foreach(var file in  files)
            {
                if (file.path.Equals(file_path))
                {
                    nowShowinText = new List<text_item>();
                    if (file.TEXT != null && file.TEXT.Length > 0)
                    {
                        foreach(var y in file.TEXT)
                        {
                            nowShowinText.Add(y);
                        }
                        nowShowinText.Sort(delegate(text_item x, text_item y)
                        {
                            return Convert.ToSingle(x.time).CompareTo(Convert.ToSingle(y.time));
                        });
                    }

                   

                    if (file.STOPS != null && file.STOPS.Length > 0)
                    {
                        stopPoints = new List<int>();
                        foreach(var y in file.STOPS)
                        {
                            stopPoints.Add(Convert.ToInt32(y*1000));
                        }
                        stopPoints.Sort();
                    }
                }

            }
            
            if (MediaPlayer.IsPlaying)
            {
                MediaPlayer.Stop();
                nowShowinText = null;
            }
            
            using var media = new LibVLCSharp.Shared.Media(_libVlc, new Uri(file_path));
            MediaPlayer.Play(media);
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            { 
                image_view.IsVisible = true;
                VideoView.IsVisible = false;
                MediaPlayer.Fullscreen = true; 
            });

            _status.nowPlaying = file_path;
            _status.audioPlaying = true;
            _status.videoPlaying = false;
            _status.imagePlaying = false;
        }

        public async void Stop()
        {
            if (MediaPlayer.IsPlaying)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                { 
                    MediaPlayer.Stop();
                });
            }
            stopPoints = null;
            stop_point = -1;
            
            _status.stopTime = false;
            nowShowinText = null;
            setImageBlank();

            _status.nowPlaying = null;
            _status.audioPlaying = false;
            _status.videoPlaying = false;
            _status.imagePlaying = false;
        }
        
        
        public void PlayVideo(string path)
        {
            
            if (MediaPlayer.IsPlaying)
            {
               MediaPlayer.Stop();
            }
            
            var media = new LibVLCSharp.Shared.Media(_libVlc, new Uri(path));
            MediaPlayer.Play(media);

            
        
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            { 
                image_view.IsVisible = false;
                VideoView.IsVisible = true;
                MediaPlayer.Fullscreen = true; 
            });
            
            _status.nowPlaying = path;
            _status.audioPlaying = false;
            _status.videoPlaying = true;
            _status.imagePlaying = false;
        }

        public void SetBalance(int balance)
        {
            if (balance > 100)
            {
                balance = 100;
            }
            else if (balance < -100)
            {
                balance = -100;
            }
            if (balance > 0)
            {
                ("amixer -c 1 sset Headphone " + (100 - balance).ToString() + "%,100%").Bash();
                ("amixer -c 1 sset Front " + (100 - balance).ToString() + "%,100%").Bash();
                ("amixer -c 1 sset Speaker " + (100 - balance).ToString() + "%,100%").Bash();
                ("amixer -c 1 sset PCM " + (100 - balance).ToString() + "%,100%").Bash();
            }
            else{
                ("amixer -c 1 sset Headphone 100%," + (100+balance).ToString() + "%").Bash();
                ("amixer -c 1 sset Front 100%," + (100+balance).ToString() + "%").Bash();
                ("amixer -c 1 sset Speaker 100%," + (100+balance).ToString() + "%").Bash();
                ("amixer -c 1 sset PCM 100%," + (100+balance).ToString() + "%").Bash();
            }
            
            saveBalance(balance);
        }
        
        public void SetVolume(int volume)
        {
            if (volume > 100)
            {
                volume = 100;
            }
            else if (volume < 0)
            {
                volume = 0;
            }
            ("amixer -c 1 sset Master " + volume.ToString() + "%").Bash();
            saveVolume(volume);
        }
        
        
        public void saveVolume(int volume)
        {
            File.WriteAllText("volume", volume.ToString());
        }
        
        public void readVolume()
        {
            if (File.Exists("volume"))
            {
                _status.AudioVolume =  Convert.ToInt32(File.ReadAllText("volume"));
            }
            else
            {
                saveVolume(100);
                _status.AudioVolume = 100;
            }
        }
        
        
        
        public void saveBalance(int balance)
        {
            File.WriteAllText("balance", balance.ToString());
        }
        
        public void readBalance()
        {
            if (File.Exists("balance"))
            {
                _status.AudioBalance = Convert.ToInt32(File.ReadAllText("balance"));
            }
            else
            {
                saveBalance(0);
                _status.AudioBalance = 0;
            }
           
        }
        
    }
}