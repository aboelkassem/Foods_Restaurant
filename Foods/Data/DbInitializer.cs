using Foods.Models;
using Foods.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foods.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async void Initialize()
        {
            try
            {
                // Apply all migrations in production in new blank database
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {

            }

            if (_db.Roles.Any(r => r.Name == SD.ManagerUser)) return;

            // Create all roles
            _roleManager.CreateAsync(new IdentityRole(SD.ManagerUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.FrontDeskUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.KitckenUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.CustomerEndUser)).GetAwaiter().GetResult();

            // Create default Manager User
            _userManager.CreateAsync(new ApplicationUser { 
                UserName="admin@gmail.com",
                Email = "admin@gmail.com",
                Name = "Aboelkassem",
                EmailConfirmed = true,
                PhoneNumber="01154321101"
            },"Admin123").GetAwaiter().GetResult();

            IdentityUser user = await _db.Users.FirstOrDefaultAsync(u=>u.Email == "admin@gmail.com");

            // Adding Manager role 
            await _userManager.AddToRoleAsync(user, SD.ManagerUser);
        }
    }
}
