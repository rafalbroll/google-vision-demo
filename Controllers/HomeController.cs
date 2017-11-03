using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using app.Logic;

namespace app.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _environment;
        private string _uploadsPath;

        public HomeController(IHostingEnvironment environment)
        {
            _environment = environment;
            _uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
        }
        
        public IActionResult Index()
        {

           IEnumerable<Photo> photos = new PhotoQueries().getPhotos();
          
            return View(photos);
        }

        public IActionResult GetPhoto(int id)
        {

           Photo photo = new PhotoQueries().getPhoto(id);
          
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
            var photoCommands = new PhotoCommands(_uploadsPath);
            foreach (var file in files)
            {
                await photoCommands.ProcessPhotoAndAddToDatabase(file);
            }
            return RedirectToAction("Index");
        }

        

        [HttpPost]
        public IActionResult Delete(int photoId) {
            new PhotoCommands(_uploadsPath).DeletePhoto(photoId);
            return RedirectToAction("Index");
        }


    }
}
