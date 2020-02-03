using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Foods.Data;
using Foods.Models;
using Foods.Models.ViewModels;
using Foods.Utility;

namespace Foods.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        
        [TempData]
        public string StatusMessage { get; set; }
        private int SubCategoriesPerPgae = 10;

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        //GET - INDEX
        public async Task<IActionResult> Index(int page = 1)
        {
            var subCategories = await _db.SubCategories.Include(s=>s.Category).ToListAsync();

            PagingInfo paging = new PagingInfo
            {
                CurrentPage = page,
                ItemsPerPage = SubCategoriesPerPgae,
                TotalItem = subCategories.Count,
                UrlParam = "/Admin/Subcategory?page=:"
            };

            SubCategoryVM subCategoriesVM = new SubCategoryVM()
            {
                SubCategories = subCategories,
                PagingInfo = paging
            };

            // get and skip the subCategories depend on PageSelected and PageSize to display them
            subCategoriesVM.SubCategories = subCategoriesVM.SubCategories.OrderBy(p => p.Id)
                                .Skip((page - 1) * SubCategoriesPerPgae) // to show next items
                                .Take(SubCategoriesPerPgae).ToList();

            return View(subCategoriesVM);
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subcategory = await _db.SubCategories.Include(s => s.Category).SingleOrDefaultAsync(m => m.Id == id);

            if (subcategory == null)
            {
                return NotFound();
            }

            return View(subcategory);
        }


        //GET - CREATE
        public async Task<IActionResult> Create()
        {
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Categories.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategories.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategoryExists = _db.SubCategories.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (doesSubCategoryExists.Count() > 0)
                {
                    // Error , That the SubCategory already exist
                    StatusMessage = "Error : Sub Category exists under " + doesSubCategoryExists.First().Category.Name + " category , Please Use another name.";
                }
                else
                {
                    _db.SubCategories.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            // return same page with same data
            SubCategoryAndCategoryViewModel modelVM = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Categories.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategories.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),
                StatusMessage = StatusMessage
            };

            return View(modelVM);
        }

        
        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            //var subCategories = await _db.SubCategories.Where(x => x.CategoryId == id).ToListAsync();

            List<SubCategory> subCategories = new List<SubCategory>();
            subCategories = await (from subCategory in _db.SubCategories
                                   where subCategory.CategoryId == id
                                   select subCategory).ToListAsync();


            return Json(new SelectList(subCategories,"Id","Name"));
        }

        //GET - CREATE
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subCategory = await _db.SubCategories.SingleOrDefaultAsync(x => x.Id == id);

            if (subCategory == null)
            {
                return NotFound();
            }

            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Categories.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategories.OrderBy(p => p.Name).Select(p => p.Name).Distinct().ToListAsync()
            };

            return View(model);
        }

        //POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doesSubCategoryExists = _db.SubCategories.Include(s => s.Category).Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (doesSubCategoryExists.Count() > 0)
                {
                    // Error , That the SubCategory already exist
                    StatusMessage = "Error : Sub Category exists under " + doesSubCategoryExists.First().Category.Name + " category , Please Use another name.";
                }
                else
                {
                    var subCatFromDb = await _db.SubCategories.FindAsync(model.SubCategory.Id);
                    subCatFromDb.Name = model.SubCategory.Name;

                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            // return same page with same data
            SubCategoryAndCategoryViewModel modelVM = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Categories.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await _db.SubCategories.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(),
                StatusMessage = StatusMessage
            };

            return View(modelVM);
        }

        [ActionName("Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subcategory = await _db.SubCategories.FindAsync(id);

            if (subcategory == null)
            {
                return Json(new { success = false, message = "Error While Deleting" });
            }

            _db.SubCategories.Remove(subcategory);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Delete Successful" });
        }
    }
}