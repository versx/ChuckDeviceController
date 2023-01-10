namespace ChuckDeviceController.Authorization.Jwt.Attributes;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Class and method attribute decoration restricting access
/// to JWT authorized gRPC requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string Identifier { get; }

    public JwtAuthorizeAttribute(string identifier)
    {
        Identifier = identifier;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var identifier = context.HttpContext.Items[Identifier];
        if (identifier == null)
        {
            context.Result = new JsonResult(new
            {
                message = "Unauthorized",
                status = StatusCodes.Status401Unauthorized,
            });
        }
    }
}