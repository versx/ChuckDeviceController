namespace ChuckDeviceController.Extensions;

using System.Reflection;

public static class ReflectionExtensions
{
    /// <summary>
    /// Gets the value of the field or property of the instance.
    /// </summary>
    public static object? GetValue(this MemberInfo member, object? instance)
    {
        var pi = member as PropertyInfo;
        if (pi != null)
        {
            return pi.GetValue(instance, null);
        }

        var fi = member as FieldInfo;
        if (fi != null)
        {
            return fi.GetValue(instance);
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Sets the value of the field or property of the instance.
    /// </summary>
    public static void SetValue(this MemberInfo member, object instance, object value)
    {
        var pi = member as PropertyInfo;
        if (pi != null)
        {
            pi.SetValue(instance, value, null);
            return;
        }

        var fi = member as FieldInfo;
        if (fi != null)
        {
            fi.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException();
    }
}