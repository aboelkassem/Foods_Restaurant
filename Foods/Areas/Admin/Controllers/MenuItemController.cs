using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Foods.Data;
using Foods.Models.ViewModels;
using Foods.Utility;

namespace Foods.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        // this binding property will the controller use it to fetch data , so don't need to pass it in post and get parameters
        // when post the form , menuItemViewModel Will be loaded by default
        [BindProperty]  
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemController(ApplicationDbContext db,IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Categories = _db.Categories,
                MenuItem = new Models.MenuItem()
            };
        }

        //GET - INDEX
        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItems.Include(x=>x.Category).Include(x=>x.SubCategory).ToListAsync();

            return View(menuItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems()
        {
            var menuItems = await _db.MenuItems.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync();

            return Json(new { data = menuItems });
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        //POST - CREATE
        [HttpPost,ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            // get the SubCategoryId from the View form [name = "SubCategoryId"] and bind it to menuItem that will Create ... because we
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }

            _db.MenuItems.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            // Work on the image saving section

            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _db.MenuItems.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // files has been uploaded
                var uploads = Path.Combine(webRootPath, "images/MenuItemsImages");  // The images Folder , Location
                var extension = Path.GetExtension(files[0].FileName);   // use the just first one upload and get it's extension

                using (var filesStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id + extension), FileMode.Create))
                {
                    // copy the first file to this location (FileStream) with new name (the id of item)
                    files[0].CopyTo(filesStream);
                }

                menuItemFromDb.Image = @"\images\MenuItemsImages\" + MenuItemVM.MenuItem.Id + extension;
            }
            else
            {
                // No file was uploaded , so use default
                menuItemFromDb.Image = @"\images\" + SD.DefaultFoodImage;

            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItems.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);
            MenuItemVM.SubCategories = await _db.SubCategories.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }

            return View(MenuItemVM);
        }

        //POST - EDIT
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // get the SubCategoryId from the View form [name = "SubCategoryId"] and bind it to menuItem that will Create ... because we
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemVM.SubCategories = await _db.SubCategories.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }
            
            // Work on the image saving section

            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _db.MenuItems.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // files has been uploaded
                var uploads = Path.Combine(webRootPath, "images/MenuItemsImages");  // The images Folder , Location
                var extension_new = Path.GetExtension(files[0].FileName);   // use the just first one upload and get it's extension

                // Delete the original file
                var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                //we will upload new file and save it to database
                using (var filesStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id + extension_new), FileMode.Create))
                {
                    // copy the first file to this location (FileStream) with new name (the id of item)
                    files[0].CopyTo(filesStream);
                }

                menuItemFromDb.Image = @"\images\MenuItemsImages\" + MenuItemVM.MenuItem.Id + extension_new;
            }
            //else
            //{
            //    // No file was uploaded , so don't change and use the original
            //    menuItemFromDb.Image = MenuItemVM.MenuItem.Image;

            //}

            menuItemFromDb.Name = MenuItemVM.MenuItem.Name;
            menuItemFromDb.Description = MenuItemVM.MenuItem.Description;
            menuItemFromDb.Price = MenuItemVM.MenuItem.Price;
            menuItemFromDb.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItemFromDb.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItemFromDb.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

        // DELETE - DELETE
        [ActionName("Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _db.MenuItems.FindAsync(id);

            if (menuItem == null)
            {
                return Json(new { success = false, message = "Error While Deleting , There is no item with Id " + id });
            }


            if (menuItem.Image != "\\images\\defaultFoodImage.png")
            {
                // Delete the original file
                string webRootPath = _hostingEnvironment.WebRootPath;
                var imagePath = Path.Combine(webRootPath, menuItem.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _db.MenuItems.Remove(menuItem);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Delete Successful" });
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItems.Include(x => x.Category).Include(x => x.SubCategory).SingleOrDefaultAsync(x => x.Id == id);
            MenuItemVM.SubCategories = await _db.SubCategories.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }

            return View(MenuItemVM);
        }
    }
}