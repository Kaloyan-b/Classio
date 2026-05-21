using Classio.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Security.Claims;

namespace Classio.Tests.Infrastructure;

/// <summary>
/// Helpers for constructing a minimally-stubbed <see cref="UserManager{User}"/>
/// suitable for controller tests. Real Identity dependencies (password hashers,
/// validators, options) are left null because the controllers only call
/// <c>GetUserId</c>, which we override.
/// </summary>
public static class TestUserManager
{
    public static Mock<UserManager<User>> Create(string returnedUserId)
    {
        var store = new Mock<IUserStore<User>>();
        var mgr = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
           .Returns(returnedUserId);

        return mgr;
    }
}
