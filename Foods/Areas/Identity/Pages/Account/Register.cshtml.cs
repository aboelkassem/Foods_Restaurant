using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Foods.Models;
using Foods.Utility;

namespace Foods.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            // My Custom Addition to user model

            [Required]
            public string Name { get; set; }

            public string StreetAddress { get; set; }
            public string PhoneNumber { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }

        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            string role = Request.Form["rdUserRole"].ToString();

            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { 
                    UserName = Input.Email, 
                    Email = Input.Email,
                    Name = Input.Name,
                    City = Input.City,
                    State = Input.State,
                    StreetAddress = Input.StreetAddress,
                    PostalCode = Input.PostalCode,
                    PhoneNumber = Input.PhoneNumber
                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    //if (!await _roleManager.RoleExistsAsync(SD.ManagerUser))
                    //{
                    //    await _roleManager.CreateAsync(new IdentityRole(SD.ManagerUser));
                    //}
                    //if (!await _roleManager.RoleExistsAsync(SD.CustomerEndUser))
                    //{
                    //    await _roleManager.CreateAsync(new IdentityRole(SD.CustomerEndUser));
                    //}
                    //if (!await _roleManager.RoleExistsAsync(SD.KitckenUser))
                    //{
                    //    await _roleManager.CreateAsync(new IdentityRole(SD.KitckenUser));
                    //}
                    //if (!await _roleManager.RoleExistsAsync(SD.FrontDeskUser))
                    //{
                    //    await _roleManager.CreateAsync(new IdentityRole(SD.FrontDeskUser));
                    //}

                    if (role == SD.KitckenUser)
                    {
                        await _userManager.AddToRoleAsync(user, SD.KitckenUser);
                    }
                    else if (role == SD.FrontDeskUser)
                    {
                        await _userManager.AddToRoleAsync(user, SD.FrontDeskUser);
                    }
                    else if (role == SD.ManagerUser)
                    {
                        await _userManager.AddToRoleAsync(user, SD.ManagerUser);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.CustomerEndUser);
                        //await _signInManager.SignInAsync(user, isPersistent: true);
                        //return LocalRedirect(returnUrl);
                    }


                    _logger.LogInformation("User created a new account with password.");


                    if (User.IsInRole(SD.ManagerUser))
                    {
                        return RedirectToAction("Index", "User", new { area = "Admin" });
                    }
                    else
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = user.Id, code = code },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                        }
                        else
                        {
                            return RedirectToAction("Index", "User", new { area = "Admin" });
                        }
                    }

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
