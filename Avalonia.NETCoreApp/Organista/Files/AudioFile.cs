using LibVLCSharp.Shared;

namespace Organista
{
    public class AudioFile: MediaFile
    {
        public string path { get; set; }
        public string title { get; set; }
        public string album { get; set; }
        public int length { get; set; }
        public TagValue tagValue { get; set; }

        public AudioFile()
        {
            fileType = FileType.Audio;
        }
    }
}