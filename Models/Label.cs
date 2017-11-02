using System;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
   public class Label
    {
        public int LabelId { get; set; }
        public string Description { get; set; }
        public float Score {get; set; }

        [Required]
        public Photo Photo {get; set;} 
    }

}