using System.Collections.Generic;

namespace Organista
{
    public class MediaFilesCollection
    {
        public string path { get; set; }
        public string name { get; set; }
        public List<AudioFile> audioFiles { get; set; }
        public List<ImageFile> imageFiles { get; set; }
        public List<VideoFile> videoFiles { get; set; }

        public MediaFilesCollection()
        {
            audioFiles = new List<AudioFile>();
            videoFiles = new List<VideoFile>();
            imageFiles = new List<ImageFile>();
        }
    }
}