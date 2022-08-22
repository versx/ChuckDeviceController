namespace RobotsPlugin.Data.Contracts
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.Infrastructure;

    /// <summary>
    /// This interface contract is implemented by the AspNetCore.PluginManager and will
    /// determine a valid route based on the class or public action method for a Type.
    /// </summary>
    public interface IRouteDataService
    {
        /// <summary>
        /// Provides the route associated with a class, this will be based on the controller name
        /// and if supplied the Route attributes placed on the class.
        /// </summary>
        /// <param name="type">Type to be checked for route data.</param>
        /// <param name="routeProvider">IActionDescriptorCollectionProvider instance obtained using DI.</param>
        /// <returns>string</returns>
        string GetRouteFromClass(Type type, IActionDescriptorCollectionProvider routeProvider);

        /// <summary>
        /// Provides the route associated with an action method, this will be based on the name of the action and 
        /// controller and if supplied the Route attributes placed on the class and method in question.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="routeProvider">IActionDescriptorCollectionProvider instance obtained using DI.</param>
        /// <returns></returns>
        string GetRouteFromMethod(MethodInfo method, IActionDescriptorCollectionProvider routeProvider);
    }
}