using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Foods.Data;
using Foods.Models;
using Foods.Models.ViewModels;
using Foods.Utility;

namespace Foods.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel IndexVM = new IndexViewModel()
            {
                MenuItems = await _db.MenuItems.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync(),
                Categories = await _db.Categories.ToListAsync(),
                Coupons = await _db.Coupons.Where(x => x.IsActive == true).ToListAsync()
            };

            return View(IndexVM);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var menuItemFromDb = await _db.MenuItems.Include(x => x.Category).Include(s => s.SubCategory).Where(s => s.Id == id).FirstOrDefaultAsync();

            ShoppingCart cartObj = new ShoppingCart()
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                // mean the user is logged in ,so get all his shopping carts
                var cnt = _db.ShoppingCarts.Where(x => x.ApplicationUserID == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            }

            return View(cartObj);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart cartObject)
        {
            cartObject.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cartObject.ApplicationUserID = claim.Value;     // assign the value of current logged in user to car model

                ShoppingCart cartFromDb = await _db.ShoppingCarts.Where(x => x.ApplicationUserID == cartObject.ApplicationUserID && x.MenuItemId == cartObject.MenuItemId).FirstOrDefaultAsync();

                if (cartFromDb == null)
                {
                    // mean this user has not added this menuItem in his shopping cart before
                    await _db.ShoppingCarts.AddAsync(cartObject);

                }
                else
                {
                    // else just update the count of this item
                    cartFromDb.Count = cartFromDb.Count + cartObject.Count;
                }

                await _db.SaveChangesAsync();

                var count = _db.ShoppingCarts.Where(c => c.ApplicationUserID == cartObject.ApplicationUserID).ToList().Count();

                // Create Session
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
                return RedirectToAction("Index","Cart");
            }
            else
            {
                var menuItemFromDb = await _db.MenuItems.Include(x => x.Category).Include(s => s.SubCategory).Where(s => s.Id == cartObject.MenuItemId).FirstOrDefaultAsync();

                ShoppingCart cartObj = new ShoppingCart()
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };
                return View(cartObj);
            }
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
