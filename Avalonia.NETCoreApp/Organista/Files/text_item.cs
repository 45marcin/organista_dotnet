using System;

namespace Organista
{
    public class text_item : IComparable<text_item>
    {
        public  string text { get; set; }
        public  double time { get; set; }
        
        
        public int CompareTo(text_item comparePart) =>
            comparePart == null ? 1 : time.CompareTo(comparePart.time);
    }
}