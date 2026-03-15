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

        // --- Role  Seeding ---
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            string[] roles = { "Admin", "Student", "Parent", "Teacher" };

            foreach (var role in roles)
            {
                if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                }
            }

            // default admin
            string adminEmail = "admin@classio.com";
            string adminPassword = "Admin123!";

            var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(adminUser, adminPassword).GetAwaiter().GetResult();

                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
                }
                else
                {
                    throw new Exception(
                        "Admin seeding failed: " +
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                }
            }
        }


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

        app.Run();
    }
}
