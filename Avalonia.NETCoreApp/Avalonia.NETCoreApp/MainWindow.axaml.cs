using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LibVLCSharp.Shared;

namespace Avalonia.NETCoreApp
{
    public class MainWindow : Window
    {
        
        private readonly LibVLC _libVlc = new LibVLC();
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
            MediaPlayer = new MediaPlayer(_libVlc);
            Play();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public MediaPlayer MediaPlayer { get; }
        
        public void Play()
        {
            using var media = new LibVLCSharp.Shared.Media(_libVlc, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
            MediaPlayer.Play(media);
            MediaPlayer.Fullscreen = true;
        }
    }
}