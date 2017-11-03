using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app.Models;
using GVisionImage = Google.Cloud.Vision.V1.Image;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Pens;
using RectangularePolygon =  SixLabors.Shapes.RectangularePolygon;
using SixLabors.Primitives;

namespace app.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _environment;

        public HomeController(IHostingEnvironment environment)
        {
            _environment = environment;
        }
        
        public IActionResult Index()
        {

           IEnumerable<Photo> photos;
           using (var dbContext = new VisionDbContext() ) {
                photos = dbContext.Photos
                                .Include(p => p.Labels)
                                .ToList(); 
                                              
           }

            return View(photos);
        }

        public IActionResult GetPhoto(int id)
        {

           Photo photo;
           using (var dbContext = new VisionDbContext() ) {
                photo = dbContext.Photos
                                .Include(p => p.Labels)
                                .First( p => p.PhotoId == id );               
           }

            return Json(photo);
        }



        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> Upload( )
        {
            var files = HttpContext.Request.Form.Files;
            var uploads = Path.Combine(_environment.WebRootPath, "uploads");
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    
                    var fileName =  Guid.NewGuid().ToString() + '.' + file.FileName.Split('.').Last();
                    var thumbnailFileName =  Guid.NewGuid().ToString() + '.' + file.FileName.Split('.').Last();
                    var pathToOrigin = Path.Combine(uploads, fileName );
                    var pathToThumbnail = Path.Combine(uploads, thumbnailFileName );

                    using (var fileStream = new FileStream( pathToOrigin, FileMode.Create)) {
                        await file.CopyToAsync(fileStream);
                    }
                    using (var dbContext = new VisionDbContext() ) 
                    {
                        using (Image<Rgba32> thumbnail = Image.Load(pathToOrigin))
                        {
                            thumbnail.Mutate(x => x
                                .Resize(300,200)
                                .Sepia() //because it's all the Instagram is about ;)
                                );   
                            thumbnail.Save(pathToThumbnail); 
                            
                        }

                        var photo = new Photo() {
                            Filename = fileName,
                            Thumbnail = thumbnailFileName
                        };
                        dbContext.Photos.Add(photo);
                        dbContext.SaveChanges();

                        try
                        {
                            GVisionImage image = GVisionImage.FromFile(pathToOrigin);

                            Google.Cloud.Vision.V1.ImageAnnotatorClient client = Google.Cloud.Vision.V1.ImageAnnotatorClient.Create();
                            IReadOnlyList<Google.Cloud.Vision.V1.EntityAnnotation> labels = client.DetectLabels(image);

                            photo.Labels = (from label in labels
                                            orderby label.Score descending
                                            select new Label() {
                                                Description = label.Description,
                                                Score = label.Score
                                            }).ToList();



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

                            // photo.Labels.AddRange( (from landmark in landmarks
                            //                 orderby landmark.Score descending
                            //                 select new Label() {
                            //                     Description = "landmark: " +landmark.Description + landmark.Score
                            //                 }).ToList() ) ;
 
                        }
                        catch (Google.Cloud.Vision.V1.AnnotateImageException e)
                        {
                            Google.Cloud.Vision.V1.AnnotateImageResponse response = e.Response;
                            Console.WriteLine(response.Error);
                        }

                        dbContext.SaveChanges();
                    }
               }
            
                    
                
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int photoId) {
            using (var dbContext = new VisionDbContext() ) 
                    {
                   
                        var photo = dbContext.Photos.Include(p => p.Labels).First(p => p.PhotoId == photoId);
                        
                        var path = Path.Combine(_environment.WebRootPath, "uploads", photo.Filename);
                        
                        System.IO.File.Delete(path);
                    
                        dbContext.Photos.Remove(photo);
                        dbContext.SaveChanges();
                    }
            return RedirectToAction("Index");
        }


    }
}
