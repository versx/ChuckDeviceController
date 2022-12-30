namespace ChuckDeviceConfigurator.Extensions;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;

// Credits: https://stackoverflow.com/a/66258565
public static class HtmlHelperViewExtensions
{
    public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, object? parameters = null)
    {
        var controller = Convert.ToString(helper.ViewContext.RouteData.Values["controller"]);

        return RenderAction(helper, action, controller, parameters);
    }

    public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, string controller, object? parameters = null)
    {
        var area = Convert.ToString(helper.ViewContext.RouteData.Values["area"]);

        return RenderAction(helper, action, controller, area, parameters);
    }

    public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, string controller, string area, object? parameters = null)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (controller == null)
            throw new ArgumentNullException(nameof(controller));

        //if (area == null)
        //    throw new ArgumentNullException(nameof(area));

        var task = RenderActionAsync(helper, action, controller, area, parameters);

        return task.Result;
    }

    private static async Task<IHtmlContent> RenderActionAsync(this IHtmlHelper helper, string action, string controller, string area, object? parameters = null)
    {
        // fetching required services for invocation
        var currentHttpContext = helper.ViewContext?.HttpContext;
        var httpContextFactory = GetServiceOrFail<IHttpContextFactory>(currentHttpContext);
        var actionInvokerFactory = GetServiceOrFail<IActionInvokerFactory>(currentHttpContext);
        var actionSelector = GetServiceOrFail<IActionDescriptorCollectionProvider>(currentHttpContext);

        // creating new action invocation context
        var routeData = new RouteData();
        var routeParams = new RouteValueDictionary(parameters ?? new { });
        var routeValues = new RouteValueDictionary(new { area, controller, action });
        var newHttpContext = httpContextFactory.Create(currentHttpContext.Features);

        newHttpContext.Response.Body = new MemoryStream();

        foreach (var router in helper.ViewContext.RouteData.Routers)
            routeData.PushState(router, null, null);

        routeData.PushState(null, routeValues, null);
        routeData.PushState(null, routeParams, null);

        var actionDescriptor = actionSelector.ActionDescriptors.Items.Where(i => i.RouteValues["controller"] == controller && i.RouteValues["action"] == action).First();
        var actionContext = new ActionContext(newHttpContext, routeData, actionDescriptor);

        // invoke action and retreive the response body
        var invoker = actionInvokerFactory.CreateInvoker(actionContext);
        var content = string.Empty;

        await invoker.InvokeAsync().ContinueWith(task => {
            if (task.IsFaulted)
            {
                content = task.Exception?.Message;
            }
            else if (task.IsCompleted)
            {
                newHttpContext.Response.Body.Position = 0;
                using var reader = new StreamReader(newHttpContext.Response.Body);
                content = reader.ReadToEnd();
            }
        });

        return new HtmlString(content);
    }

    private static TService GetServiceOrFail<TService>(HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));

        var service = httpContext.RequestServices.GetService(typeof(TService))
            ?? throw new InvalidOperationException($"Could not locate service: {nameof(TService)}");
        return (TService)service;
    }
}