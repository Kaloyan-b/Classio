using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Classio.Tests.Infrastructure;

/// <summary>
/// Wires up an empty <see cref="HttpContext"/> and a working <c>TempData</c>
/// dictionary on a controller so action methods that read <c>User</c> or
/// write <c>TempData</c> do not throw.
/// </summary>
public static class TestControllerContext
{
    public static T WithHttpContext<T>(this T controller) where T : Controller
    {
        var httpContext = new DefaultHttpContext();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }
}
