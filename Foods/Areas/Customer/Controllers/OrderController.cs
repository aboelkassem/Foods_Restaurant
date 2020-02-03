using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Foods.Data;
using Foods.Models;
using Foods.Models.ViewModels;
using Foods.Utility;

namespace Foods.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private int PageSize = 2;
        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeaders.Include(x => x.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id && o.UserId == claim.Value),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            return View(orderDetailsViewModel);
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            List<OrderHeader> orderHeadersList = await _db.OrderHeaders.Include(x => x.ApplicationUser).Where(u => u.UserId == claim.Value).ToListAsync();

            foreach (var orderHeader in orderHeadersList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual);
            }

            var count = orderListVM.Orders.Count;

            // get and skip the orders depend on PageSelected and PageSize to display them
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                .Skip((productPage - 1) * PageSize) // to show next items
                                .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                UrlParam = "/Customer/Order/OrderHistory?productPage=:" // [ : ] will replaced in our Custom TagHelper (PageLinkTageHelper)
            };


            return View(orderListVM);
        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(x => x.Id == id),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            orderDetailsViewModel.OrderHeader.ApplicationUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == orderDetailsViewModel.OrderHeader.UserId);

            return PartialView("_IndividualOrderDetails", orderDetailsViewModel);
        }

        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(x => x.Id == id);

            return PartialView("_IndividualOrderStatus", orderHeader);
        }


        [Authorize(Roles = SD.KitckenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> MangeOrder()
        {

            List<OrderDetailsViewModel> orderDetailsVM = new List<OrderDetailsViewModel>();

            List<OrderHeader> orderHeadersList = await _db.OrderHeaders.Where(o => o.Status == SD.StatusSubmitted || o.Status == SD.StatusInProcess).OrderByDescending(o => o.PickUpTime).ToListAsync();

            foreach (var orderHeader in orderHeadersList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };
                orderDetailsVM.Add(individual);
            }

            return View(orderDetailsVM.OrderBy(o => o.OrderHeader.PickUpTime).ToList());
        }

        [Authorize(Roles = SD.KitckenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int? OrderId)
        {
            if (OrderId == null) { return NotFound(); }

            OrderHeader orderHeader = await _db.OrderHeaders.FindAsync(OrderId);

            if (orderHeader == null) { return NotFound(); }

            orderHeader.Status = SD.StatusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction("MangeOrder", "Order");
        }

        [Authorize(Roles = SD.KitckenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int? OrderId)
        {
            if (OrderId == null) { return NotFound(); }

            OrderHeader orderHeader = await _db.OrderHeaders.FindAsync(OrderId);

            if (orderHeader == null) { return NotFound(); }

            orderHeader.Status = SD.StatusReady;
            await _db.SaveChangesAsync();


            // Email Logic to notify User that Order is ready for pickup
            await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == orderHeader.UserId).Email,
                                                "Foods - Order Ready For Pickup " + orderHeader.Id.ToString(),
                                                "Order Ready For Pickup , our shipping agent will call you as soon as possible to deliver your order");

            return RedirectToAction("MangeOrder", "Order");
        }

        [Authorize(Roles = SD.KitckenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int? OrderId)
        {
            if (OrderId == null) { return NotFound(); }

            OrderHeader orderHeader = await _db.OrderHeaders.FindAsync(OrderId);

            if (orderHeader == null) { return NotFound(); }

            orderHeader.Status = SD.StatusCancelled;
            await _db.SaveChangesAsync();
            //email for canceled order
            await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == orderHeader.UserId).Email,
                                                "Foods - Order Canceled " + orderHeader.Id.ToString(),
                                                "Your order has been canceled , may be because that order are not available until now or the total of Menu Items in particular small time... so we are happy to serve you and don't forget to make another orders");

            return RedirectToAction("MangeOrder", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchEmail = null, string searchPhone = null, string searchName = null)
        {
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            StringBuilder param = new StringBuilder();
            // paginating
            param.Append("/Customer/Order/OrderPickup?productPage=:");

            // Search Name
            param.Append("&searchName=");
            if (searchName != null)
            {
                param.Append(searchName);
            }

            // Search Email
            param.Append("&searchEmail=");
            if (searchEmail != null)
            {
                param.Append(searchEmail);
            }

            // Search Phone
            param.Append("&searchPhone=");
            if (searchPhone != null)
            {
                param.Append(searchPhone);
            }

            List<OrderHeader> orderHeadersList = new List<OrderHeader>();
            // get the search values
            if (searchName != null || searchEmail != null || searchPhone != null)
            {
                var user = new ApplicationUser();

                if (searchName != null)
                {
                    orderHeadersList = await _db.OrderHeaders.Include(o => o.ApplicationUser)
                                                .Where(u => u.PickupName.ToLower().Contains(searchName.ToLower()))
                                                .OrderByDescending(o => o.OrderDate).ToListAsync();
                }
                else
                {
                    if (searchEmail != null)
                    {
                        user = await _db.ApplicationUsers.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower())).FirstOrDefaultAsync();
                        orderHeadersList = await _db.OrderHeaders.Include(o => o.ApplicationUser)
                                                    .Where(o => o.UserId == user.Id)
                                                    .OrderByDescending(o => o.OrderDate).ToListAsync();
                    }
                    else
                    {
                        if (searchPhone != null)
                        {
                            orderHeadersList = await _db.OrderHeaders.Include(o => o.ApplicationUser)
                                                        .Where(u => u.PhoneNumber.ToLower().Contains(searchPhone.ToLower()))
                                                        .OrderByDescending(o => o.OrderDate).ToListAsync();
                        }
                    }
                }

            }
            else
            {
                orderHeadersList = await _db.OrderHeaders.Include(x => x.ApplicationUser).Where(u => u.Status == SD.StatusReady).ToListAsync();
            }


            foreach (var orderHeader in orderHeadersList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };
                orderListVM.Orders.Add(individual);
            }


            var count = orderListVM.Orders.Count;

            // get and skip the orders depend on PageSelected and PageSize to display them
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                .Skip((productPage - 1) * PageSize) // to show next items
                                .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                UrlParam = param.ToString()
            };

            return View(orderListVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("OrderPickup")]
        [Authorize(Roles =SD.ManagerUser + "," + SD.FrontDeskUser)]
        public async Task<IActionResult> OrderPickupPost(int orderId)
        {

            OrderHeader orderHeader = await _db.OrderHeaders.FindAsync(orderId);

            if (orderHeader == null) { return NotFound(); }

            orderHeader.Status = SD.StatusCompleted;
            await _db.SaveChangesAsync();

            //Email For order completed
            await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == orderHeader.UserId).Email,
                                    "Foods - Order Completed " + orderHeader.Id.ToString(),
                                    "Order has been completed successfully , we are so happy to serve you .... Thanks for your truth, have a nice day!");

            return RedirectToAction("OrderPickup", "Order");
        }
    }
}