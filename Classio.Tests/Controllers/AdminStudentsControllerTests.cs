using Classio.Areas.Admin.Controllers;
using Classio.Models;
using Classio.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Classio.Tests.Controllers;

public class AdminStudentsControllerTests
{
    private static StudentsController BuildController(Classio.Data.ClassioDbContext db)
    {
        var userManager = TestUserManager.Create("admin-user");
        return new StudentsController(db, userManager.Object).WithHttpContext();
    }

    [Fact]
    public async Task Index_ReturnsAllStudents()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var ctrl = BuildController(db);
        var result = await ctrl.Index();

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Student>>(view.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_ForUnknownId()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var ctrl = BuildController(db);
        var result = await ctrl.Edit(99999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsPartialViewWithModel()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db);
        var result = await ctrl.Edit(seed.Alice.Id);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_EditStudent", partial.ViewName);
    }
}
