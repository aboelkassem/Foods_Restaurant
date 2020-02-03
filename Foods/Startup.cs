using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Foods.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Foods.Service;
using Microsoft.AspNetCore.Identity.UI.Services;
using Foods.Utility;
using Stripe;

namespace Foods
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddScoped<IDbInitializer, DbInitializer>();

            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));

            services.AddSingleton<IEmailSender, EmailSender>();
            services.Configure<EmailOptions>(Configuration);

            services.ConfigureApplicationCookie(options => options.LoginPath = "/Identity/Account/Login");

            services.AddAuthentication().AddFacebook(facebookOptions => 
            {
                facebookOptions.AppId = "2472329526363094";
                facebookOptions.AppSecret = "29aa32b30c406fbb242d75e3bba41d2d";
            });

            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = "1088371421677-r1vdip2a4ck604cse5m2jdqimqg9u5ar.apps.googleusercontent.com";
                googleOptions.ClientSecret = "6m5p6hiR2HQNX0zkGG8NiYG2";
            });

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
            });

            services.AddControllersWithViews();

            services.AddRazorPages().AddRazorRuntimeCompilation();


            services.AddSession(options =>
            {
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env , IDbInitializer dbInitializer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();

            StripeConfiguration.ApiKey = Configuration.GetSection("Stripe")["SecretKey"];
            dbInitializer.Initialize();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
