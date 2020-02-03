using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Foods.Data;
using Foods.Models;
using Foods.Models.ViewModels;
using Foods.Utility;
using Stripe;

namespace Foods.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public OrderDetailsCart detailCart { get; set; }

        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            detailCart.OrderHeader.OrderTotal = 0;

            // current logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // get all carts of the current user that added
            var carts = await _db.ShoppingCarts.Where(c => c.ApplicationUserID == claim.Value).ToListAsync();

            if (carts != null)
            {
                detailCart.ListCart = carts;
            }

            foreach (var cart in detailCart.ListCart)
            {
                cart.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == cart.MenuItemId);
                detailCart.OrderHeader.OrderTotal = detailCart.OrderHeader.OrderTotal + (cart.MenuItem.Price * cart.Count);
                cart.MenuItem.Description = SD.ConvertToRawHtml(cart.MenuItem.Description);
                if (cart.MenuItem.Description.Length > 100)
                {
                    cart.MenuItem.Description = cart.MenuItem.Description.Substring(0,99)+ "....";
                }
            }

            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupons.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }


            return View(detailCart);
        }


        public async Task<IActionResult> Summary()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            detailCart.OrderHeader.OrderTotal = 0;

            // current logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ApplicationUser applicationUser = await _db.ApplicationUsers.Where(c => c.Id == claim.Value).FirstOrDefaultAsync();

            // get all carts of the current user that added
            var carts = await _db.ShoppingCarts.Where(c => c.ApplicationUserID == claim.Value).ToListAsync();

            if (carts != null)
            {
                detailCart.ListCart = carts;
            }

            foreach (var cart in detailCart.ListCart)
            {
                cart.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == cart.MenuItemId);
                detailCart.OrderHeader.OrderTotal = detailCart.OrderHeader.OrderTotal + (cart.MenuItem.Price * cart.Count);
            }

            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;
            detailCart.OrderHeader.PickupName = applicationUser.Name;
            detailCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            detailCart.OrderHeader.PickUpTime = DateTime.Now;


            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupons.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }


            return View(detailCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {

            // current logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // get all carts of the current user that added
            detailCart.ListCart = await _db.ShoppingCarts.Where(x => x.ApplicationUserID == claim.Value).ToListAsync();

            detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            detailCart.OrderHeader.Status = SD.PaymentStatusPending;
            detailCart.OrderHeader.OrderDate = DateTime.Now;
            detailCart.OrderHeader.UserId = claim.Value;
            detailCart.OrderHeader.PickUpTime = Convert.ToDateTime(detailCart.OrderHeader.PickUpDate.ToShortDateString()+ " "+detailCart.OrderHeader.PickUpTime.ToShortTimeString());

            //List<OrderDetails> orderDetailsList = new List<OrderDetails>();
            _db.OrderHeaders.Add(detailCart.OrderHeader);
            await _db.SaveChangesAsync();

            detailCart.OrderHeader.OrderTotalOriginal = 0;

            
            foreach (var cart in detailCart.ListCart)
            {
                cart.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == cart.MenuItemId);
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = cart.MenuItemId,
                    OrderId = detailCart.OrderHeader.Id,
                    Description = cart.MenuItem.Description,
                    Name = cart.MenuItem.Name,
                    Price = cart.MenuItem.Price,
                    Count = cart.Count
                };
                detailCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);
            }

            // if he use Coupon
            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupons.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                detailCart.OrderHeader.OrderTotal = detailCart.OrderHeader.OrderTotalOriginal;
            }
            detailCart.OrderHeader.CouponCodeDiscount = detailCart.OrderHeader.OrderTotalOriginal - detailCart.OrderHeader.OrderTotal;

            // remove the shopping carts from DB and sessions
            _db.ShoppingCarts.RemoveRange(detailCart.ListCart);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();



            // Use Stripe charge options to build a transaction including 
            // StripeToken that coming from Post Action after (this token come from stripe servers)
            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(detailCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + detailCart.OrderHeader.Id,
                Source = stripeToken
            };

            var service = new ChargeService();
            Charge charge = service.Create(options);

            if (charge.BalanceTransactionId == null)
            {
                // mean error while charging the credit card
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                detailCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                //email for successful order
                await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == claim.Value).Email,
                                                    "Foods - Order Created " + detailCart.OrderHeader.Id.ToString(), 
                                                    "Order has been submitted successfully , please follow the order status in Order History and we will emailed you as it has been completed.");

                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;    // payment status
                detailCart.OrderHeader.Status = SD.StatusSubmitted;                 // order status
            }
            else
            {
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }

            await _db.SaveChangesAsync();
            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm","Order",new {id = detailCart.OrderHeader.Id});
        }


        public IActionResult AddCoupon()
        {
            if (detailCart.OrderHeader.CouponCode == null)
            {
                detailCart.OrderHeader.CouponCode = "";
            }

            // Store the coupon value in session
            HttpContext.Session.SetString(SD.ssCouponCode, detailCart.OrderHeader.CouponCode);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            // Make the session empty
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);

            if (cart.Count == 1)
            {
                //if count = 1 and minus it , so delete it from DB
                _db.ShoppingCarts.Remove(cart);
                await _db.SaveChangesAsync();

                // and update the session value
                var cnt = _db.ShoppingCarts.Where(x => x.ApplicationUserID == cart.ApplicationUserID).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);

            //delete it from DB
            _db.ShoppingCarts.Remove(cart);
            await _db.SaveChangesAsync();

            // and update the session value
            var cnt = _db.ShoppingCarts.Where(x => x.ApplicationUserID == cart.ApplicationUserID).ToList().Count;
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}