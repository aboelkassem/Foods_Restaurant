using System;
using System.Collections.Generic;
using System.IO;
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
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupons.ToListAsync());
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View();
        }

        //POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                //Get the file selected from form
                var files = HttpContext.Request.Form.Files;
                if (files.Count>0)
                {
                    byte[] p1 = null;
                    
                    // Convert the image into a stream of bytes and stored it in p1 to store in database
                    using(var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    coupon.Picture = p1;
                }

                _db.Coupons.Add(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }


        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _db.Coupons.FindAsync(id);

            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        //POST - CREATE
        [HttpPost,ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, Coupon coupon)
        {
            if (id == null)
            {
                return NotFound();
            }

            var couponFromDb = await _db.Coupons.FindAsync(id);

            if (couponFromDb == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                //Get the file selected from form
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    byte[] p1 = null;

                    // Convert the image into a stream of bytes and stored it in p1 to store in database
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    couponFromDb.Picture = p1;
                }

                couponFromDb.Name = coupon.Name;
                couponFromDb.CouponType = coupon.CouponType;
                couponFromDb.Discount = coupon.Discount;
                couponFromDb.MinimumAmount = coupon.MinimumAmount;
                couponFromDb.IsActive = coupon.IsActive;

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _db.Coupons.FindAsync(id);

            if (coupon==null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // With Ajax request in index page
        [ActionName("Delete")]
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _db.Coupons.FindAsync(id);

            if (coupon == null)
            {
                return Json(new { success = false, message = "Error While Deleting" });
            }

            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Delete Successful" });
        }
    }
}