using System.Collections.Generic;

namespace Organista
{
    public class status
    {
        public bool audioPlaying { get; set; } = false;
        public bool videoPlaying { get; set; } = false;
        public bool imagePlaying { get; set; } = false;
        public string nowPlaying  { get; set; }
        public bool refreshingFiles { get; set; } = false;
        public bool stopTime { get; set; } = false;
        public int AudioBalance  { get; set; } = 0;
        public int AudioVolume  { get; set; } = 0;
        public List<string> usb { get; set; } = new List<string>();
    }
}