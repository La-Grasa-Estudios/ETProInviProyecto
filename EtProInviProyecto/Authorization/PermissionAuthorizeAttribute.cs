using EtPro.Authorization;
using Microsoft.AspNetCore.Authorization;

public class PermissionAuthorizeAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public string Permission { get; }

    public PermissionAuthorizeAttribute(string permission)
    {
        Permission = permission;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(Permission);
    }
}