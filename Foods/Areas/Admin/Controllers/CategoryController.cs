using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Foods.Data;
using Foods.Models;
using Foods.Utility;

namespace Foods.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET
        public async Task<IActionResult> Index()
        {
            return View(await _db.Categories.ToListAsync());
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }


        //GET - CREATE
        public IActionResult Create()
        {
            return View();
        }

        //POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                await _db.Categories.AddAsync(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
                //return RedirectToAction("Index");
            }

            return View(category);
        }

        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = await _db.Categories.FindAsync(id);
            
            if (category == null)
            {
                return NotFound();
            }
            return View(category);

        }

        //POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Update(category);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }


        // it's manual , but doesn't need now because it's wrong to delete this using GET request ... see every delete action method with ajax request [HttpDelete]

        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }
            return View(category);

        }

        //POST - DELETE
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return View();
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // With Ajax request in index page
        [ActionName("Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FindAsync(id);

            if (category == null)
            {
                return Json(new { success = false, message = "Error While Deleting" });
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Delete Successful" });
        }
    }
}