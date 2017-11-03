using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using app.Models;
using GVisionImage = Google.Cloud.Vision.V1.Image;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Pens;
using RectangularePolygon =  SixLabors.Shapes.RectangularePolygon;
using SixLabors.Primitives;

namespace app.Logic {
    public class PhotoQueries
    {
        internal IEnumerable<Photo> getPhotos()
        {
             using (var dbContext = new VisionDbContext() ) {
                return dbContext.Photos
                                .Include(p => p.Labels)
                                .ToList(); 
                                              
           }
        }

        internal Photo getPhoto(int id)
        {
             using (var dbContext = new VisionDbContext() ) {
                return dbContext.Photos
                                .Include(p => p.Labels)
                                .First( p => p.PhotoId == id );               
           }
        }
    }
}