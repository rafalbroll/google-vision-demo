using System;
using System.Collections.Generic;

namespace app.Models
{
   public class Photo
    {
        public int PhotoId { get; set; }
        public string Filename { get; set; }
        public string Thumbnail { get;  set; }

        public List<Label> Labels {get; set;}
        
    }

}