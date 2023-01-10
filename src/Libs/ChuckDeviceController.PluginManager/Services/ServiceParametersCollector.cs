namespace ChuckDeviceController.PluginManager.Services;

using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.PluginManager.Mvc.Extensions;

public class ServiceParametersCollector
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<Type> _ignoredServices;

    public ServiceParametersCollector(IServiceProvider serviceProvider, IEnumerable<Type> ignoredServices)
    {
        _serviceProvider = serviceProvider;
        _ignoredServices = ignoredServices;
    }

    public Dictionary<Type, object> GetParameterInstances(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (_serviceProvider != null)
        {
            return GetInstancesConstructorParameters(type);
        }

        var result = new Dictionary<Type, object>();
        var constructors = type.GetPluginConstructors();
        foreach (var constructor in constructors)
        {
            foreach (var param in constructor.GetParameters())
            {
                if (_ignoredServices.Contains(param.ParameterType))
                    continue;

                var paramClass = _serviceProvider?.GetService(param.ParameterType);
                if (paramClass == null)
                    continue;

                // If we did not find a specific param type for this constructor,
                // clear the args list and try the next constructor.
                if (paramClass == null)
                {
                    result.Clear();
                    break;
                }

                result.Add(param.ParameterType, paramClass);
            }

            if (result.Count > 0)
            {
                return result;
            }
        }

        return result;
    }

    private T? GetClassImplementation<T>(Type classType)
        where T : class
    {
        var service = _serviceProvider.GetService(classType);
        var sd = new ServiceDescriptor(classType, service!);
        if (sd == null)
        {
            return default;
        }

        T? result = default;

        if (sd.ImplementationInstance != null)
        {
            result = (T)sd.ImplementationInstance;
        }
        else if (sd.ImplementationType != null)
        {
            var args = GetInstancesConstructorParameters(sd.ImplementationType);
            result = Activator.CreateInstance(sd.ImplementationType, args) as T;
        }
        else if (sd.ImplementationFactory != null)
        {
            result = sd.ImplementationFactory.Invoke(null!) as T;
        }

        return result;
    }

    private Dictionary<Type, object> GetInstancesConstructorParameters(Type type)
    {
        var result = new Dictionary<Type, object>();
        var constructors = type.GetPluginConstructors();
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            foreach (var param in parameters)
            {
                if (_ignoredServices.Contains(param.ParameterType))
                    continue;

                if (param.ParameterType == typeof(IServiceProvider))
                    continue;

                var paramClass = GetClassImplementation<object>(param.ParameterType);
                if (paramClass == null)
                {
                    // If we did not find a specific param type for this constructor,
                    // clear the args list and try the next constructor.
                    result.Clear();
                    break;
                }

                result.Add(param.ParameterType, paramClass);
            }

            if (result.Count > 0)
            {
                return result;
            }
        }

        return result;
    }
}