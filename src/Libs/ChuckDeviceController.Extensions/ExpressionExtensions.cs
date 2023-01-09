namespace ChuckDeviceController.Extensions;

using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionExtensions
{
    public static string GetOperator(this string methodName)
    {
        return methodName switch
        {
            "Add" => "+",
            "Subtract" => "-",
            "Multiply" => "*",
            "Divide" => "/",
            "Negate" => "-",
            "Remainder" => "%",
            _ => "",
        };
    }

    public static string GetOperator(this UnaryExpression node)
    {
        return node.NodeType switch
        {
            ExpressionType.Negate or
            ExpressionType.NegateChecked => "-",
            ExpressionType.UnaryPlus => "+",
            ExpressionType.Not => node.Operand.Type.IsBoolean()
                ? "NOT"
                : "~",
            _ => "",
        };
    }

    public static string GetOperator(this BinaryExpression node)
    {
        return node.NodeType switch
        {
            ExpressionType.And or
            ExpressionType.AndAlso => node.Left.Type.IsBoolean()
                ? "AND"
                : "&",
            ExpressionType.Or or
            ExpressionType.OrElse => node.Left.Type.IsBoolean()
                ? "OR"
                : "|",
            ExpressionType.Equal => node.Right.IsNullConstant()
                ? "IS"
                : "=",
            ExpressionType.NotEqual => node.Right.IsNullConstant()
                ? "IS NOT"
                : "<>",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.Add or
            ExpressionType.AddChecked => "+",
            ExpressionType.Subtract or
            ExpressionType.SubtractChecked => "-",
            ExpressionType.Multiply or
            ExpressionType.MultiplyChecked => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            ExpressionType.ExclusiveOr => "^",
            ExpressionType.LeftShift => "<<",
            ExpressionType.RightShift => ">>",
            _ => "",
        };
    }

    public static bool IsPredicate(this Expression node)
    {
        return node.NodeType switch
        {
            ExpressionType.And or
            ExpressionType.AndAlso or
            ExpressionType.Or or
            ExpressionType.OrElse => IsBoolean(((BinaryExpression)node).Type),
            ExpressionType.Not => IsBoolean(((UnaryExpression)node).Type),
            ExpressionType.Equal or
            ExpressionType.NotEqual or
            ExpressionType.LessThan or
            ExpressionType.LessThanOrEqual or
            ExpressionType.GreaterThan or
            ExpressionType.GreaterThanOrEqual => true,
            ExpressionType.Call => IsBoolean(((MethodCallExpression)node).Type),
            _ => false,
        };
    }

    public static bool IsNullConstant(this Expression node)
    {
        return node.NodeType == ExpressionType.Constant && ((ConstantExpression)node).Value == null;
    }

    public static bool IsBoolean(this Type type)
    {
        return type == typeof(bool) || type == typeof(bool?);
    }

    public static bool IsInteger(this Type type)
    {
        var nnType = type.GetNonNullableType();

        return nnType.GetTypeCode() switch
        {
            TypeCode.SByte or
            TypeCode.Int16 or
            TypeCode.Int32 or
            TypeCode.Int64 or
            TypeCode.Byte or
            TypeCode.UInt16 or
            TypeCode.UInt32 or
            TypeCode.UInt64 => true,
            _ => false,
        };
    }

    public static TypeCode GetTypeCode(this Type type)
    {
        if (type == typeof(bool))
        {
            return TypeCode.Boolean;
        }
        else if (type == typeof(byte))
        {
            return TypeCode.Byte;
        }
        else if (type == typeof(sbyte))
        {
            return TypeCode.SByte;
        }
        else if (type == typeof(short))
        {
            return TypeCode.Int16;
        }
        else if (type == typeof(ushort))
        {
            return TypeCode.UInt16;
        }
        else if (type == typeof(int))
        {
            return TypeCode.Int32;
        }
        else if (type == typeof(uint))
        {
            return TypeCode.UInt32;
        }
        else if (type == typeof(long))
        {
            return TypeCode.Int64;
        }
        else if (type == typeof(ulong))
        {
            return TypeCode.UInt64;
        }
        else if (type == typeof(float))
        {
            return TypeCode.Single;
        }
        else if (type == typeof(double))
        {
            return TypeCode.Double;
        }
        else if (type == typeof(decimal))
        {
            return TypeCode.Decimal;
        }
        else if (type == typeof(string))
        {
            return TypeCode.String;
        }
        else if (type == typeof(char))
        {
            return TypeCode.Char;
        }
        else if (type == typeof(DateTime))
        {
            return TypeCode.DateTime;
        }
        else
        {
            return TypeCode.Object;
        }
    }

    /// <summary>
    /// Returns true if the type is a <see cref="Nullable{T}"/>.
    /// </summary>
    public static bool IsNullableType(this Type type)
    {
        return type != null && type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Gets the underlying type if the specified type is a <see cref="Nullable{T}"/>,
    /// otherwise just returns given type.
    /// </summary>
    public static Type GetNonNullableType(this Type type)
    {
        if (IsNullableType(type))
        {
            return type.GetTypeInfo().GenericTypeArguments[0];
        }

        return type;
    }
}