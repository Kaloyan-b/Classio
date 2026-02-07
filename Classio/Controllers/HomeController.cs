using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Classio.Models;
using Microsoft.AspNetCore.Identity;

namespace Classio.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<User> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<User> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null)
            return View();

        var roles = await _userManager.GetRolesAsync(currentUser);

        if (roles.Contains("Student"))
            return RedirectToAction("Dashboard", "Student");

        if (roles.Contains("Parent"))
            return RedirectToAction("Dashboard", "Parent");

        if (roles.Contains("Teacher"))
            return RedirectToAction("Dashboard", "Teacher");

        if (roles.Contains("Admin"))
            return RedirectToAction("Index", "Admin", new { area = "Admin" });

        return View();
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
