using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Custom authorization requirement that checks database-driven permissions.
/// Usage: [Authorize(Policy = "Permission:services.create")]
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The permission key to check, e.g., "services.create"
    /// </summary>
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Handles PermissionRequirement by checking the user's role permissions from the database.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider, ILogger<PermissionAuthorizationHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                     ?? context.User.FindFirst("role")?.Value;

        if (string.IsNullOrEmpty(roleClaim))
        {
            _logger.LogWarning("User has no role claim, denying permission '{Permission}'", requirement.Permission);
            return;
        }

        // SuperAdmin always passes
        if (string.Equals(roleClaim, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        // Use a scope to resolve the scoped PermissionService
        using var scope = _serviceProvider.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        var hasPermission = await permissionService.HasPermissionAsync(roleClaim, requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogInformation("User with role '{Role}' denied permission '{Permission}'", roleClaim, requirement.Permission);
        }
    }
}

/// <summary>
/// Handles PermissionRequirement for "any of" permission checks.
/// </summary>
public class AnyPermissionRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }

    public AnyPermissionRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }
}

public class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public AnyPermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyPermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return;

        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                     ?? context.User.FindFirst("role")?.Value;

        if (string.IsNullOrEmpty(roleClaim))
            return;

        if (string.Equals(roleClaim, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        if (await permissionService.HasAnyPermissionAsync(roleClaim, requirement.Permissions))
        {
            context.Succeed(requirement);
        }
    }
}
