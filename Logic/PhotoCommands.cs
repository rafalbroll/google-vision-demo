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
using RectangularePolygon = SixLabors.Shapes.RectangularePolygon;
using SixLabors.Primitives;
using ImageAnnotatorClient = Google.Cloud.Vision.V1.ImageAnnotatorClient;

namespace app.Logic {
    public class PhotoCommands {

        private string _uploadsPath;
        public PhotoCommands(string uploadsPath) {
            _uploadsPath = uploadsPath;
        }
        public async Task<bool> ProcessPhotoAndAddToDatabase(IFormFile file) {
            if (file.Length == 0) return false;
                    
            var fileName =           Guid.NewGuid().ToString() + '.' + file.FileName.Split('.').Last();
            var thumbnailFileName =  Guid.NewGuid().ToString() + '.' + file.FileName.Split('.').Last();
            var pathToOrigin =       Path.Combine(_uploadsPath, fileName );
            var pathToThumbnail =    Path.Combine(_uploadsPath, thumbnailFileName );

            using (var fileStream = new FileStream( pathToOrigin, FileMode.Create)) {
                await file.CopyToAsync(fileStream);
            }
            using (var dbContext = new VisionDbContext() ) 
            {
                _createThumbnail(pathToOrigin, pathToThumbnail);
                
                var photoEntity =  _addPhotoWithThumbnail(dbContext, fileName,thumbnailFileName );
                
                try
                {
                    Google.Cloud.Vision.V1.ImageAnnotatorClient client = Google.Cloud.Vision.V1.ImageAnnotatorClient.Create();
                   
                    _processLabels(dbContext, client,  photoEntity, pathToOrigin);             
                    _markLandmarksOnOriginalPhoto(dbContext,client, photoEntity, pathToOrigin);
                    

                }
                catch (Google.Cloud.Vision.V1.AnnotateImageException e)
                {
                    Google.Cloud.Vision.V1.AnnotateImageResponse response = e.Response;
                    Console.WriteLine(response.Error);
                }

            }

            return true;
               
        }

        public void DeletePhoto(int photoId)
        {
           using (var dbContext = new VisionDbContext() ) 
                    {
                   
                        var photo = dbContext.Photos.Include(p => p.Labels).First(p => p.PhotoId == photoId);
                        
                        var path = Path.Combine(_uploadsPath, photo.Filename);
                        
                        System.IO.File.Delete(path);
                    
                        dbContext.Photos.Remove(photo);
                        dbContext.SaveChanges();
                    }
        }

        private void _markLandmarksOnOriginalPhoto(VisionDbContext dbContext, ImageAnnotatorClient client, Photo photo, string pathToOrigin)
        {
            GVisionImage image = GVisionImage.FromFile(pathToOrigin);
            IReadOnlyList<Google.Cloud.Vision.V1.EntityAnnotation> landmarks = client.DetectLandmarks(image);
            using (Image<Rgba32> originFile = Image.Load(pathToOrigin))
            {
                foreach (Google.Cloud.Vision.V1.EntityAnnotation landmark in landmarks){
                    var P1 = landmark.BoundingPoly.Vertices.First();
                    var P2 = landmark.BoundingPoly.Vertices.Skip(2).First();
                    
                    originFile.Mutate(x => x
                        .Draw( new Pen<Rgba32>(Rgba32.DarkBlue, 5 ) ,
                                new RectangularePolygon(new PointF(P1.X, P1.Y), new PointF(P2.X, P2.Y ) )  )
                        );   
                }
                originFile.Save(pathToOrigin); 
            }
            dbContext.SaveChanges();
        }

        private void _processLabels(VisionDbContext dbContext, ImageAnnotatorClient client, Photo photo, string pathToOrigin)
        {
                GVisionImage image = GVisionImage.FromFile(pathToOrigin);

                IReadOnlyList<Google.Cloud.Vision.V1.EntityAnnotation> labels = client.DetectLabels(image);

                photo.Labels = (from label in labels
                            orderby label.Score descending
                            select new Label() {
                                Description = label.Description,
                                Score = label.Score
                            }).ToList();
                dbContext.SaveChanges();
        }

      
        private Photo _addPhotoWithThumbnail(VisionDbContext dbContext, string fileName, string thumbnailFileName)
        {
           var photo = new Photo() {
                    Filename = fileName,
                    Thumbnail = thumbnailFileName
                };
                dbContext.Photos.Add(photo);
                dbContext.SaveChanges();
            return photo;
        }

    
        private void _createThumbnail(string pathToOrigin, string pathToThumbnail)
        {
           using (Image<Rgba32> thumbnail = Image.Load(pathToOrigin))
                {
                    thumbnail.Mutate(x => x
                        .Resize(300,200)
                        .Sepia() //because it's all the Instagram is about ;)
                        );   
                    thumbnail.Save(pathToThumbnail); 
                    
                }
        }

       
    }
}