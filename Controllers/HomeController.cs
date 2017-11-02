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

                    using (var fileStream = new FileStream( pathToOrigin, FileMode.Create))
                    using (var dbContext = new VisionDbContext() ) 
                    {
                        await file.CopyToAsync(fileStream);

                        using (Image<Rgba32> thumbnail = Image.Load(pathToOrigin))
                        {
                            thumbnail.Mutate(x => x
                                .Resize(300,200)
                                .Sepia()); //because it's all the Instagram is about ;)
                            thumbnail.Save(pathToThumbnail); 
                        }

                        var photo = new Photo() {
                            Filename = fileName,
                            Thumbnail = thumbnailFileName
                        };
                        dbContext.Photos.Add(photo);
                        dbContext.SaveChanges();


                        GVisionImage image = GVisionImage.FromFile(pathToOrigin);

                        Google.Cloud.Vision.V1.ImageAnnotatorClient client = Google.Cloud.Vision.V1.ImageAnnotatorClient.Create();
                        IReadOnlyList<Google.Cloud.Vision.V1.EntityAnnotation> labels = client.DetectLabels(image);

                        photo.Labels = (from label in labels
                                        select new Label() {
                                            Description = label.Description,
                                            Score = label.Score
                                        }).ToList();

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
