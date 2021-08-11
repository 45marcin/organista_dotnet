namespace Organista
{
    public class ImageFile : MediaFile
    {
        public string path { get; set; }
        public string title { get; set; }
        public string folder { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        
        
        public ImageFile()
        {
            fileType = FileType.Image;
        }
        
    }
}