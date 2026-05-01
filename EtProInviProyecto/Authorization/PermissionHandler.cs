using EtPro.Authorization;
using ETPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {

        if (string.IsNullOrEmpty(requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.Identity.IsAuthenticated)
            return;

        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return;

        var hasPermissions = await _context.UserPermission
            .AnyAsync(up => up.UserID == userIdClaim.Value && up.Permission.Name == requirement.Permission);

        if (hasPermissions)
            context.Succeed(requirement);
    }
}
