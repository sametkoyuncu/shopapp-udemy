using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using shopapp.data.Concrete.EfCore;
using shopapp.data.Abstract;
using shopapp.business.Abstract;
using shopapp.business.Concrete;
using shopapp.webui.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using shopapp.webui.EmailServices;

namespace shopapp.webui
{
    public class Startup
    {
        private IConfiguration _configuration;
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationContext>(options => options.UseSqlite("Data Source=shopDb"));
            services.AddIdentity<User, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationContext>()
                    .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options => {
                // password
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = true;

                // locaout
                    //5 yanlış parola deneme hakkı
                options.Lockout.MaxFailedAccessAttempts = 5;
                    //5 dakika sonra tekrar deneyebilir
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    //locaout u aktif etmek
                options.Lockout.AllowedForNewUsers = true;

                // options.User.AllowedUserNameCharacters = "";
                    //her user ın farklı bir email adresi olmalı
                options.User.RequireUniqueEmail = true;
                    //hesabın aktif olması için emailin onaylanması gerek
                options.SignIn.RequireConfirmedEmail = true;
                    //hesabın aktif olması için telefonun onaylanması gerek
                options.SignIn.RequireConfirmedPhoneNumber = false;

            });
            
            services.ConfigureApplicationCookie(options => {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/accessdenied";
                //varsayılan cookie süresi 20 dk
                //eğer true ise 20 dk içinde her istek yaptığında geri sayım sıfırlanır,
                //tekrar 20 dk süre verir
                //eğer false olursa ne yaparsan yap girişten itibaren 20dk süre verir
                options.SlidingExpiration = true;
                //elle süre atama
                    //timespan dan sonra gün, dakika vs. gibi seçenekler var
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.Cookie = new CookieBuilder
                {
                    //sadece tarayıcıdan ulaşılsın
                    //bir script cookieyi alamasın içün
                    HttpOnly =true,
                    //görülen isim
                    Name = ".ShopApp.Security.Cookie",
                    SameSite = SameSiteMode.Strict
                };
            });

            services.AddScoped<ICategoryRepository, EfCoreCategoryRepository>();
            services.AddScoped<IProductRepository, EfCoreProductRepository>();
            services.AddScoped<ICartRepository, EfCoreCartRepository>();
            services.AddScoped<IOrderRepository, EfCoreOrderRepository>();

            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<IProductService, ProductManager>();
            services.AddScoped<ICartService, CartManager>();
            services.AddScoped<IOrderService, OrderManager>();

            services.AddScoped<IEmailSender, SmtpEmailSender>(i=>
                new SmtpEmailSender(
                    _configuration["EmailSender:Host"],
                    _configuration.GetValue<int>("EmailSender:Port"),
                    _configuration.GetValue<bool>("EmailSender:EnableSSL"),
                    _configuration["EmailSender:UserName"],
                    _configuration["EmailSender:Password"]
                    )
                );

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            if (env.IsDevelopment())
            {
                SeedDatabase.Seed();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            //node_modules dizini için dışarıdan erişim izni gibi gibi
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath, "node_modules")),
                RequestPath = "/modules"
            });

            app.UseAuthentication();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //cart isleri
                endpoints.MapControllerRoute(
                    name: "cart",
                    pattern: "cart",
                    defaults: new {controller="Cart", action="Index"}
                );
                endpoints.MapControllerRoute(
                    name: "checkout",
                    pattern: "checkout",
                    defaults: new {controller="Cart", action="Checkout"}
                );
                endpoints.MapControllerRoute(
                    name: "orders",
                    pattern: "orders",
                    defaults: new {controller="Cart", action="GetOrders"}
                );
                //admin/user isleri
                endpoints.MapControllerRoute(
                    name: "adminuserlist",
                    pattern: "admin/user/list",
                    defaults: new {controller="Admin", action="UserList"}
                );
                endpoints.MapControllerRoute(
                    name: "adminuserdetails",
                    pattern: "admin/user/{id}",
                    defaults: new {controller="Admin", action="UserDetails"}
                );
                endpoints.MapControllerRoute(
                    name: "adminuseredit",
                    pattern: "admin/user/edit/{id}",
                    defaults: new {controller="Admin", action="UserEdit"}
                );
                // admin/role isleri
                endpoints.MapControllerRoute(
                    name: "adminrolelist",
                    pattern: "admin/role/list",
                    defaults: new {controller="Admin", action="RoleList"}
                );
                endpoints.MapControllerRoute(
                    name: "adminrolecreate",
                    pattern: "admin/role/create",
                    defaults: new {controller="Admin", action="RoleCreate"}
                );
                endpoints.MapControllerRoute(
                    name: "adminroleedit",
                    pattern: "admin/role/{id?}",
                    defaults: new {controller="Admin", action="RoleEdit"}
                );

                // admin/productlist e admin/products ile gitmek
                endpoints.MapControllerRoute(
                    name: "adminproducts",
                    pattern: "admin/products",
                    defaults: new {controller="Admin", action="ProductList"}
                );

                //
                endpoints.MapControllerRoute(
                    name: "adminproductcreate",
                    pattern: "admin/products/create",
                    defaults: new {controller="Admin", action="ProductCreate"}
                );

                // ürün güncelleme
                endpoints.MapControllerRoute(
                    name: "adminproductedit",
                    pattern: "admin/products/{id?}",
                    defaults: new {controller="Admin", action="ProductEdit"}
                );

                //
                endpoints.MapControllerRoute(
                    name: "admincategories",
                    pattern: "admin/categories",
                    defaults: new {controller="Admin", action="CategoryList"}
                );

                //
                endpoints.MapControllerRoute(
                    name: "admincategorycreate",
                    pattern: "admin/categories/create",
                    defaults: new {controller="Admin", action="CategoryCreate"}
                );

                // ürün güncelleme
                endpoints.MapControllerRoute(
                    name: "admincategoryedit",
                    pattern: "admin/categories/{id?}",
                    defaults: new {controller="Admin", action="CategoryEdit"}
                );




                // localhost/search
                endpoints.MapControllerRoute(
                    name: "search",
                    pattern: "search",
                    defaults: new {controller="Shop", action="search"}
                );

                endpoints.MapControllerRoute(
                    name: "productsdetails",
                    pattern: "{url}",
                    defaults: new {controller="Shop", action="details"}
                );

                endpoints.MapControllerRoute(
                    name: "products",
                    pattern: "products/{category?}",
                    defaults: new {controller="Shop", action="list"}
                );

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            SeedIdentity.Seed(userManager, roleManager, configuration).Wait();
        }
    }
}
