using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

//packages needed: vlc, libvlc, imagemagick

namespace Organista
{
    public class MainWindow : Window
    {
        private status _status = new status();
        private readonly LibVLC _libVlc = new LibVLC();
        
        private VideoView VideoView;
        private Image image_view;

        private List<TagValue> files;
        private List<TagValue> video_files;
        private List<TagValue> image_files;

        public int stop_point = -1;
        public  List<text_item> nowShowinText;
        public List<int> stopPoints;

        public List<MediaFilesCollection> usbStorageCollections;
        private MediaFilesCollection _mediaFilesCollection;
        private bool scanningUSB = false;
            

        private string status = "idle";  //playing_audio, playing video, refreshing files

        public MainWindow()
        {
            InitializeComponent();
            _mediaFilesCollection = new MediaFilesCollection();
            usbStorageCollections = new List<MediaFilesCollection>();
            DirectoryWatcher deviceWatcher = new DirectoryWatcher("/media/" + "whoami".Bash().Replace("\n", ""));
            deviceWatcher.Directorydisappeared += DeviceWatcherOnDirectorydisappeared;
            deviceWatcher.DirectoryAppeared += DeviceWatcherOnDirectoryAppeared;
            deviceWatcher.start();
            initAudio();
            
            VideoView = this.Get<VideoView>("VideoView");
            image_view = this.Get<Image>("image_view");
#if DEBUG
            this.AttachDevTools();
#endif

            
            
            files = new List<TagValue>();
            ProcessDirectory("/home/marcin/Documents/GitHub/Organista_tagEditor_python", _mediaFilesCollection);
            /*
             *
            ProcessDirectory("/home/marcin/organista_audio/", _mediaFilesCollection);
             * 
             */
            
            //Play();
            
            MediaPlayer = new MediaPlayer(_libVlc);
            MediaPlayer.Volume = 100;
            MediaPlayer.PositionChanged += MediaPlayerOnPositionChanged;
            MediaPlayer.EndReached  += MediaPlayerOnEndReached;
            VideoView.MediaPlayer = MediaPlayer;
            
            
            //////////////////
            // Get the output into a string
            string result = "uname -a".Bash();




            HttpServer  server = new HttpServer();
            server.newRequest += ServerOnnewRequest;

            UdpServer udpServer = new UdpServer();

        }

        private void DeviceWatcherOnDirectoryAppeared(object? sender, EventArgs e)
        {
            
            MediaFilesCollection mediaFilesCollection = new MediaFilesCollection();
            usbStorageCollections.Add(mediaFilesCollection);
            _status.usb.Add(((DirecoryEventArgs) e).path);
            mediaFilesCollection.path = ((DirecoryEventArgs) e).path;
            mediaFilesCollection.name = ((DirecoryEventArgs) e).path.Split("/").Last();
            ProcessDirectory(((DirecoryEventArgs) e).path, mediaFilesCollection);
        }

        private void DeviceWatcherOnDirectorydisappeared(object? sender, EventArgs e)
        {
            try
            {
                string path = ((DirecoryEventArgs) e).path;
                _status.usb.Remove(((DirecoryEventArgs) e).path);
                foreach (var x in usbStorageCollections)
                {
                    if (x.path.Equals(path))
                    {
                        usbStorageCollections.Remove(x);
                    }
                }
            }
            catch
            {
                
            }
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

        async void DeviceWatcher()
        {
            try
            {
                scanningUSB = true;
                

                string[] devices = EnumerateExernalStorage("/media/" + "whoami".Bash().Replace("\n", ""));

                List<MediaFilesCollection> tmpUSB = new List<MediaFilesCollection>();
                
                foreach (var usb in  devices)
                {
                    var coll = isUSBContained(usb, usbStorageCollections);
                    if (coll != null)
                    {
                        tmpUSB.Add(coll);
                    }
                    else
                    {
                        MediaFilesCollection mediaFilesCollection = new MediaFilesCollection();
                        mediaFilesCollection.path = usb;
                        mediaFilesCollection.name = usb.Split("/").Last();
                        ProcessDirectory(usb, mediaFilesCollection);
                        tmpUSB.Add(mediaFilesCollection);
                        
                        
                        //parse directory
                    }
                    
                }

                scanningUSB = false;
            }
            catch
            {
                scanningUSB = false;
            }
        }

        MediaFilesCollection  isUSBContained(string usb, List<MediaFilesCollection> collections)
        {
            foreach (var y in collections)
            {
                if (usb.Contains(y.path))
                {
                    return y;
                }
            }

            return null;
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
                            HttpServer.SendRespone(args.response, JsonSerializer.Serialize(_mediaFilesCollection.audioFiles), 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("video") != null && args.request.QueryString.GetValues("video").Length > 0 && args.request.QueryString.GetValues("video")[0].Equals("get_files"))
                        {
                            HttpServer.SendRespone(args.response, JsonSerializer.Serialize(_mediaFilesCollection.videoFiles), 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("image") != null && args.request.QueryString.GetValues("image").Length > 0 && args.request.QueryString.GetValues("image")[0].Equals("get_files"))
                        {
                            HttpServer.SendRespone(args.response, JsonSerializer.Serialize(_mediaFilesCollection.imageFiles), 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("device") != null && args.request.QueryString.GetValues("device").Length > 0  && args.request.QueryString.GetValues("device")[0].Equals("status"))
                        {
                            HttpServer.SendRespone(args.response, JsonSerializer.Serialize(_status), 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("usb") != null && args.request.QueryString.GetValues("usb").Length > 0  && args.request.QueryString.GetValues("usb")[0].Equals("get_files"))
                        {
                            if (args.request.QueryString.GetValues("path") != null)
                            {
                                foreach (var VARIABLE in  usbStorageCollections)
                                {
                                    if (VARIABLE.path.Equals(args.request.QueryString.GetValues("path")[0]))
                                    {
                                        HttpServer.SendRespone(args.response, JsonSerializer.Serialize(VARIABLE), 200);
                                        return;
                                    }
                                }
                            }
                            HttpServer.SendRespone(args.response, "", 200);
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
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("video") != null && args.request.QueryString.GetValues("video").Length > 0   && args.request.QueryString.GetValues("video")[0].Equals("play"))
                        {
                            if (args.request.QueryString.GetValues("file") != null)
                            {
                                PlayVideo(args.request.QueryString.GetValues("file")[0]);
                            }
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("image") != null && args.request.QueryString.GetValues("image").Length > 0   && args.request.QueryString.GetValues("image")[0].Equals("show"))
                        {
                            if (args.request.QueryString.GetValues("file") != null)
                            {
                                showImage(args.request.QueryString.GetValues("file")[0]);
                            }
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("video") != null && args.request.QueryString.GetValues("video").Length > 0   && args.request.QueryString.GetValues("video")[0].Equals("stop"))
                        {
                            Stop();
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("video") != null && args.request.QueryString.GetValues("video").Length > 0   && args.request.QueryString.GetValues("video")[0].Equals("play_pause"))
                        {
                            PlayPuauseVideo();
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
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("audio") != null && args.request.QueryString.GetValues("audio").Length > 0   && args.request.QueryString.GetValues("audio")[0].Equals("stop_time_cancel"))
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
                        if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("show_text") != null)
                        {
                            setImageText(body);
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else if (args.request.QueryString.HasKeys() && args.request.QueryString.GetValues("hide_text") != null)
                        {
                            setImageBlank();
                            HttpServer.SendRespone(args.response, "", 200);
                        }
                        else
                        {
                            HttpServer.SendRespone(args.response, "unrecognized query string", 404);
                        }

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


        async void setImageText(string text)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            { 
                
                ("convert -background black -fill white -size 1920x1080 -gravity center  label:'" + text + "' /home/marcin/test.png").Bash();
                image_view.Source = new Bitmap(@"/home/marcin/test.png");
                
                image_view.IsVisible = true;
                //VideoView.IsVisible = false;
                //MediaPlayer.Fullscreen = true; 
            });
        }

        async void setImageBlank()
        {
            _status.imagePlaying = false;
            _status.nowPlaying = null;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                image_view.Source = null;
                
                image_view.IsVisible = true;
                VideoView.IsVisible = false;
                MediaPlayer.Fullscreen = true; 
            });
        }
        async void showImage(string path)
        {
            _status.imagePlaying = true;
            _status.nowPlaying = path;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                image_view.Source = new Bitmap(@path);
                
                image_view.IsVisible = true;
                VideoView.IsVisible = false;
                MediaPlayer.Fullscreen = true; 
            });
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public string[] EnumerateExernalStorage(string path)
        {
            return Directory.GetDirectories(path);
        }
        
       
        
        public  void ProcessDirectory(string targetDirectory,  MediaFilesCollection collection)
        {
            Console.Out.WriteLine(targetDirectory);
            // Process the list of files found in the directory.
            string [] fileEntries = Directory.GetFiles(targetDirectory);
            foreach(string fileName in fileEntries){
                ProcessFile(fileName, collection);
                
            }

            // Recurse into subdirectories of this directory.
            string [] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach(string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory, collection);
        }

        // Insert logic for processing found files here.
        public  void ProcessFile(string path,  MediaFilesCollection collection)
        {
            Console.WriteLine("Processed file '{0}'.", path);
            if (path.Contains(".wav") || path.Contains(".WAV") || path.Contains(".mp3") || path.Contains(".MP3") || path.Contains(".flac") || path.Contains(".FLAC"))
            {
                var tfile = TagLib.File.Create(@path);
                Console.Out.WriteLine(tfile.Tag.Title);
                Console.Out.WriteLine(tfile.Tag.Album);
                
                
                TagValue file_info = new TagValue();
                try
                {
                    Console.Out.WriteLine(Encoding.UTF8.GetString(Encoding.Default.GetBytes(tfile.Tag.Comment)));
                    string new_string = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(tfile.Tag.Comment));
                    new_string = new_string.Replace("\\u0105", "ą").Replace("\\u0107", "ć").Replace("\\u0119", "ę")
                        .Replace("\\u0142", "ł").Replace("\\u0144", "ń").Replace("\\u00f3", "ó")
                        .Replace("\\u015b", "ś").Replace("u017a", "ź").Replace("u017c", "ż")
                        .Replace("\\u0104", "Ą")
                        .Replace("\\u0106", "Ć").Replace("\\u0118", "Ę").Replace("\\u0141", "Ł")
                        .Replace("\\u0143", "Ń").Replace("\\u00d3", "Ó").Replace("\\u015a", "Ś")
                        .Replace("\\u0179", "Ż").Replace("\\u017b", "Ż");
                    setImageText(new_string);
                    file_info = JsonSerializer.Deserialize<TagValue>(new_string);
                    file_info.STOPS.Sort();
                    file_info.TEXT.Sort();
                }
                catch (Exception e)
                {
                    file_info = new TagValue();
                }


                 AudioFile audioFile = new AudioFile();
                 
                 audioFile.title = tfile.Tag.Title;
                 audioFile.album = tfile.Tag.Album;
                 audioFile.path = path;
                 audioFile.length = tfile.Length;
                 audioFile.tagValue = file_info;
                 collection.audioFiles.Add(audioFile);
                 
            }
            else if (path.ToUpper().Contains(".MP4") || path.ToUpper().Contains(".WMV") || path.ToUpper().Contains(".AVI") || path.ToUpper().Contains(".MKV") || path.ToUpper().Contains(".MOV"))
            {

                VideoFile videoFile = new VideoFile();
                videoFile.path = path;
                videoFile.length = 0;
                videoFile.title = path.Split("/").Last();
                TagValue file_info = new TagValue();

                collection.videoFiles.Add(videoFile);
            }
            else if (path.ToUpper().Contains(".JPG") || path.ToUpper().Contains(".JPEG") || path.ToUpper().Contains(".PNG"))
            {

                ImageFile imageFile = new ImageFile();
                imageFile.path = path;
                imageFile.title = path.Split("/").Last();
                
                
                collection.imageFiles.Add(imageFile);
            }
        }
        
        public MediaPlayer MediaPlayer { get; }
        
        public void PlayAudio(string file_path)
        {
            /*
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
            */
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

            foreach (var x in _mediaFilesCollection.audioFiles)
            {
                if (x.path.Equals(file_path))
                {
                    nowShowinText = x.tagValue.TEXT.ToList();
                }
            }
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
        public void PlayPuauseVideo()
        {
            if (_status.videoPlaying)
            {
                if (MediaPlayer.IsPlaying)
                {
                    MediaPlayer.Pause();
                }
                else
                {
                    MediaPlayer.Play();
                }
            }
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