namespace Organista
{
    public class VideoFile: MediaFile
    {
        public string path { get; set; }
        public string title { get; set; }
        public string folder { get; set; }
        public int length { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        
        
        public VideoFile()
        {
            fileType = FileType.Video;
        }
    }
}