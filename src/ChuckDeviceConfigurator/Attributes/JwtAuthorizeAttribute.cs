namespace ChuckDeviceConfigurator.Attributes
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var identifier = context.HttpContext.Items[Strings.Identifier];
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
}