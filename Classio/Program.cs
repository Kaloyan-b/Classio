using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Classio.Data;
using Classio.Models;

namespace Classio;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Database setup ---
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ClassioDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // --- Identity setup ---
        builder.Services.AddIdentity<User, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ClassioDbContext>()
        .AddDefaultTokenProviders()
        .AddDefaultUI();

        builder.Services.AddScoped<Classio.Services.IScheduleService, Classio.Services.ScheduleService>();

        // --- MVC & Razor ---
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        builder.Services.AddSignalR();

        var app = builder.Build();

        app.MapHub<Classio.Hubs.ChatHub>("/chatHub");

        // --- Pipeline ---
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Add authentication*before authorization
        app.UseAuthentication();
        app.UseAuthorization();
        // Routing for Areas
        app.MapControllerRoute(
            name: "MyArea",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        //DbSeed
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Classio.Data.ClassioDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Classio.Data.DbInitializer.Initialize(context, userManager, roleManager);
        }

        app.Run();
    }
}
