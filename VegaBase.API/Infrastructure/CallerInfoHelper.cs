// VegaBase.API/Infrastructure/CallerInfoHelper.cs
using System.Security.Claims;
using VegaBase.Service.Models;

namespace VegaBase.API.Infrastructure;

/// <summary>
/// Populates CallerInfo fields on a BaseParamModel from the current ClaimsPrincipal (JWT).
/// </summary>
public static class CallerInfoHelper
{
    public static void Fill(BaseParamModel param, ClaimsPrincipal user)
    {
        param.CallerUsername = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        var allRoleCodes = user.FindAll("roleCode").Select(c => c.Value).ToList();
        param.CallerRole = allRoleCodes.Contains("admin", StringComparer.OrdinalIgnoreCase)
            ? "admin"
            : allRoleCodes.FirstOrDefault() ?? string.Empty;

        param.CallerRoleIds = user.FindAll("roleId")
            .Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }
}
