namespace ChuckDeviceController.Data.Translators;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using ChuckDeviceController.Extensions;

// Credits: https://stackoverflow.com/a/7891426
public class MySqlQueryTranslator : ExpressionVisitor
{
    #region Variables

    private readonly IEnumerable<string>? _reservedKeywords;
    private StringBuilder _sb = null!;
    private string _orderBy = string.Empty;
    private int? _skip = null;
    private int? _take = null;
    private string _whereClause = string.Empty;

    #endregion

    #region Properties

    public int? Skip => _skip;

    public int? Take => _take;

    public string OrderBy => _orderBy;

    public string WhereClause => _whereClause;

    #endregion

    #region Constructor

    public MySqlQueryTranslator(IEnumerable<string>? reservedKeywords = null)
    {
        _reservedKeywords = reservedKeywords;
    }

    #endregion

    #region Public Methods

    public string Translate(Expression expression)
    {
        expression = Evaluator.PartialEval(expression);

        _sb = new StringBuilder();
        Visit(expression);
        _whereClause = _sb.ToString();
        return _whereClause;
    }

    #endregion

    #region Override Methods

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Math))
        {
            switch (node.Method.Name)
            {
                case "Abs":
                case "Acos":
                case "Asin":
                case "Atan":
                case "Atan2":
                case "Cos":
                case "Exp":
                case "Log10":
                case "Sin":
                case "Tan":
                case "Sqrt":
                case "Sign":
                case "Ceiling":
                case "Floor":
                    Write(node.Method.Name.ToUpper());
                    Write("(");
                    Visit(node.Arguments[0]);
                    Write(")");
                    return node;
                case "Pow":
                    Write("POWER(");
                    Visit(node.Arguments[0]);
                    Write(", ");
                    Visit(node.Arguments[1]);
                    Write(")");
                    return node;
                case "Round":
                    if (node.Arguments.Count == 1)
                    {
                        Write("ROUND(");
                        Visit(node.Arguments[0]);
                        Write(")");
                        return node;
                    }
                    else if (node.Arguments.Count == 2 && node.Arguments[1].Type == typeof(int))
                    {
                        Write("ROUND(");
                        Visit(node.Arguments[0]);
                        Write(", ");
                        Visit(node.Arguments[1]);
                        Write(")");
                        return node;
                    }
                    break;
                case "Truncate":
                    Write("TRUNCATE(");
                    Visit(node.Arguments[0]);
                    Write(",0)");
                    return node;
            }
        }
        else if (node.Method.DeclaringType == typeof(decimal))
        {
            switch (node.Method.Name)
            {
                case "Add":
                case "Subtract":
                case "Multiply":
                case "Divide":
                case "Remainder":
                    Write("(");
                    VisitValue(node.Arguments[0]);
                    Write(" ");
                    Write(node.Method.Name.GetOperator());
                    Write(" ");
                    VisitValue(node.Arguments[1]);
                    Write(")");
                    return node;

                case "Negate":
                    Write("-");
                    Visit(node.Arguments[0]);
                    Write("");
                    return node;

                case "Ceiling":
                case "Floor":
                    Write(node.Method.Name.ToUpper());
                    Write("(");
                    Visit(node.Arguments[0]);
                    Write(")");
                    return node;

                case "Round":
                    if (node.Arguments.Count == 1)
                    {
                        Write("ROUND(");
                        Visit(node.Arguments[0]);
                        Write(")");
                        return node;
                    }
                    else if (node.Arguments.Count == 2 && node.Arguments[1].Type == typeof(int))
                    {
                        Write("ROUND(");
                        Visit(node.Arguments[0]);
                        Write(", ");
                        Visit(node.Arguments[1]);
                        Write(")");
                        return node;
                    }
                    break;

                case "Truncate":
                    Write("TRUNCATE(");
                    Visit(node.Arguments[0]);
                    Write(",0)");
                    return node;
            }
        }
        else //if (node.Method.DeclaringType == typeof(string))
        {
            switch (node.Method.Name)
            {
                case "StartsWith":
                    Write("(");
                    Visit(node.Object);
                    Write(" LIKE CONCAT(");
                    Visit(node.Arguments[0]);
                    Write(",'%'))");
                    return node;

                case "EndsWith":
                    Write("(");
                    Visit(node.Object);
                    Write(" LIKE CONCAT('%',");
                    Visit(node.Arguments[0]);
                    Write("))");
                    return node;

                case "Contains":
                    if (node.Method.DeclaringType == typeof(string))
                    {
                        Write("(");
                        Visit(node.Object);
                        Write(" LIKE CONCAT('%',");
                        Visit(node.Arguments[0]);
                        Write(",'%'))");
                    }
                    else
                    {
                        Write('(');
                        Visit(node.Arguments[1]);
                        Write(" IN (");
                        Visit(node.Arguments[0]);
                        Write("))");
                    }
                    return node;

                case "Concat":
                    IList<Expression> args = node.Arguments;
                    if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit)
                    {
                        args = ((NewArrayExpression)args[0]).Expressions;
                    }
                    Write("CONCAT(");
                    for (int i = 0, n = args.Count; i < n; i++)
                    {
                        if (i > 0) Write(", ");
                        Visit(args[i]);
                    }
                    Write(")");
                    return node;

                case "IsNullOrEmpty":
                    Write("(");
                    Visit(node.Arguments[0]);
                    Write(" IS NULL OR ");
                    Visit(node.Arguments[0]);
                    Write(" = '')");
                    return node;

                case "IsNullOrWhitespace":
                    Write("(");
                    Visit(node.Arguments[0]);
                    Write(" IS NULL OR ");
                    Visit(node.Arguments[0]);
                    Write(" = ' ')");
                    return node;

                case "ToUpper":
                    Write("UPPER(");
                    Visit(node.Object);
                    Write(")");
                    return node;

                case "ToLower":
                    Write("LOWER(");
                    Visit(node.Object);
                    Write(")");
                    return node;

                case "Replace":
                    Write("REPLACE(");
                    Visit(node.Object);
                    Write(", ");
                    Visit(node.Arguments[0]);
                    Write(", ");
                    Visit(node.Arguments[1]);
                    Write(")");
                    return node;

                case "Substring":
                    Write("SUBSTRING(");
                    Visit(node.Object);
                    Write(", ");
                    Visit(node.Arguments[0]);
                    Write(" + 1");
                    if (node.Arguments.Count == 2)
                    {
                        Write(", ");
                        Visit(node.Arguments[1]);
                    }
                    Write(")");
                    return node;

                case "Remove":
                    if (node.Arguments.Count == 1)
                    {
                        Write("LEFT(");
                        Visit(node.Object);
                        Write(", ");
                        Visit(node.Arguments[0]);
                        Write(")");
                    }
                    else
                    {
                        Write("CONCAT(");
                        Write("LEFT(");
                        Visit(node.Object);
                        Write(", ");
                        Visit(node.Arguments[0]);
                        Write("), SUBSTRING(");
                        Visit(node.Object);
                        Write(", ");
                        Visit(node.Arguments[0]);
                        Write(" + ");
                        Visit(node.Arguments[1]);
                        Write("))");
                    }
                    return node;

                case "IndexOf":
                    Write("(LOCATE(");
                    Visit(node.Arguments[0]);
                    Write(", ");
                    Visit(node.Object);
                    if (node.Arguments.Count == 2 && node.Arguments[1].Type == typeof(int))
                    {
                        Write(", ");
                        Visit(node.Arguments[1]);
                        Write(" + 1");
                    }
                    Write(") - 1)");
                    return node;

                case "Trim":
                    Write("TRIM(");
                    Visit(node.Object);
                    Write(")");
                    return node;

                case "Where":
                    if (node.Method.DeclaringType == typeof(Queryable))
                    {
                        Visit(node.Arguments[0]);
                        var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
                        Visit(lambda.Body);
                        return node;
                    }
                    break;

                case "Take":
                    if (ParseTakeExpression(node))
                    {
                        return Visit(node.Arguments[0]);
                    }
                    break;

                case "Skip":
                    if (ParseSkipExpression(node))
                    {
                        return Visit(node.Arguments[0]);
                    }
                    break;

                case "OrderBy":
                    if (ParseOrderByExpression(node, "ASC"))
                    {
                        return Visit(node.Arguments[0]);
                    }
                    break;

                case "OrderByDescending":
                    if (ParseOrderByExpression(node, "DESC"))
                    {
                        return Visit(node.Arguments[0]);
                    }
                    break;

                case "Any":
                    //var inExpression = new InExpression(node, node.Arguments);
                    //VisitIn(inExpression);
                    //return node;
                    if (ParseAnyExpression(node, includeOperand: true))
                    {
                        return node;
                    }
                    break;

                case "Count":
                    if (ParseAnyExpression(node, includeOperand: false))
                    {
                        return node;
                    }
                    break;

                case "ToString":
                    if (node.Object?.Type != typeof(string))
                    {
                        Write("CAST(");
                        Visit(node.Object);
                        Write(" AS CHAR)");
                    }
                    else
                    {
                        Visit(node.Object);
                    }
                    return node;

                case "Equals":
                    if (node.Method.IsStatic && node.Method.DeclaringType == typeof(object))
                    {
                        Write("(");
                        Visit(node.Arguments[0]);
                        Write(" = ");
                        Visit(node.Arguments[1]);
                        Write(")");
                        return node;
                    }
                    else if (!node.Method.IsStatic && node.Arguments.Count == 1 && node.Arguments[0].Type == node.Object?.Type)
                    {
                        Write("(");
                        Visit(node.Object);
                        Write(" = ");
                        Visit(node.Arguments[0]);
                        Write(")");
                        return node;
                    }
                    break;

                case "CompareTo":
                    if (!node.Method.IsStatic && node.Method.ReturnType == typeof(int) && node.Arguments.Count == 1)
                    {
                        Write("(CASE WHEN ");
                        Visit(node.Object);
                        Write(" = ");
                        Visit(node.Arguments[0]);
                        Write(" THEN 0 WHEN ");
                        Visit(node.Object);
                        Write(" < ");
                        Visit(node.Arguments[0]);
                        Write(" THEN -1 ELSE 1 END)");
                        return node;
                    }
                    break;

                case "Compare":
                    if (node.Method.IsStatic && node.Method.ReturnType == typeof(int) && node.Arguments.Count == 2)
                    {
                        Write("(CASE WHEN ");
                        Visit(node.Arguments[0]);
                        Write(" = ");
                        Visit(node.Arguments[1]);
                        Write(" THEN 0 WHEN ");
                        Visit(node.Arguments[0]);
                        Write(" < ");
                        Visit(node.Arguments[1]);
                        Write(" THEN -1 ELSE 1 END)");
                        return node;
                    }
                    break;
            }
        }

        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported");
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                Write(" NOT ");
                Visit(node.Operand);
                break;

            case ExpressionType.Convert:
                Visit(node.Operand);
                break;

            default:
                throw new NotSupportedException($"The unary operator '{node.NodeType}' is not supported");
        }

        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var op = node.GetOperator();
        var left = node.Left;
        var right = node.Right;

        Write('(');

        switch (node.NodeType)
        {
            case ExpressionType.Power:
                Write(" POWER(");
                VisitValue(left);
                Write(", ");
                VisitValue(right);
                Write(")");
                break;

            case ExpressionType.Coalesce:
                Write(" COALESCE(");
                VisitValue(left);
                Write(", ");
                while (right.NodeType == ExpressionType.Coalesce)
                {
                    var rb = (BinaryExpression)right;
                    VisitValue(rb.Left);
                    Write(", ");
                    right = rb.Right;
                }
                VisitValue(right);
                Write(")");
                break;

            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.RightShift:
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.Modulo:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
                VisitValue(left);
                Write(" ");
                Write(op);
                Write(" ");
                VisitValue(right);
                break;

            case ExpressionType.Divide:
                if (node.Type.IsInteger())
                {
                    Write(" TRUNCATE(");
                    base.VisitBinary(node);
                    Write(",0)");
                }
                break;

            default:
                VisitValue(left);
                Write(" ");
                Write(op);
                Write(" ");
                VisitValue(right);
                //throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported");
                break;
        }

        Write(')');

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var value = node.Value as IQueryable;
        if (value == null && node.Value == null)
        {
            Write("NULL");
        }
        else if (node.Value?.GetType().GetTypeInfo().IsEnum ?? false)
        {
            Write(Convert.ChangeType(node.Value, Enum.GetUnderlyingType(node.Value.GetType())));
        }
        else if (value == null)
        {
            var typeCode = node.Value!.GetType().GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    Write(((bool?)node.Value ?? false) ? 1 : 0);
                    break;

                case TypeCode.String:
                    Write('\'');
                    Write(node.Value);
                    Write('\'');
                    break;

                case TypeCode.DateTime:
                    Write('\'');
                    Write(node.Value);
                    Write('\'');
                    break;

                case TypeCode.Single:
                case TypeCode.Double:
                    var result = ((IConvertible)node.Value!).ToString(NumberFormatInfo.InvariantInfo);
                    if (!result.Contains('.'))
                    {
                        result += ".0";
                    }
                    Write(result);
                    break;

                case TypeCode.Object:
                    if (node.Value is IEnumerable list)
                    {
                        var items = list.Cast<string>();
                        //Write('(');
                        var str = "'" + string.Join("', '", items) + "'";
                        Write(str);
                        //Write(')');
                    }
                    else
                    {
                        throw new NotSupportedException($"The constant for '{node.Value}' is not supported");
                    }
                    break;

                default:
                    var converted = (node.Value as IConvertible)?.ToString(CultureInfo.InvariantCulture) ?? node.Value;
                    Write(converted);
                    //Write(node.Value);
                    break;
            }
        }

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null)
        {
            switch (node.Expression.NodeType)
            {
                case ExpressionType.Constant:
                    // Wrap column name in backticks if it is a reserved MySQL keyword
                    var constValue = _reservedKeywords?.Contains(node.Member.Name) ?? false
                        ? $"`{node.Member.Name}`"
                        : node.Member.Name;
                    Write(constValue);
                    return node;

                case ExpressionType.Parameter:
                    var attr = node.Member.GetCustomAttribute<ColumnAttribute>();
                    if (attr?.Name != null)
                    {
                        // Wrap column name in backticks if it is a reserved MySQL keyword
                        var value = _reservedKeywords?.Contains(attr.Name) ?? false
                            ? $"`{attr.Name}`"
                            : attr.Name;
                        Write(value);
                    }
                    return node;

                case ExpressionType.MemberAccess:
                    switch (node.Member.Name)
                    {
                        case "Count":
                        case "Length":
                        default:
                            var genericTypes = new[]
                            {
                                typeof(IEnumerable<>),
                                typeof(ICollection),
                                typeof(IList),
                                typeof(IList<>),
                                typeof(List<>),
                                typeof(Array),
                                typeof(IDictionary),
                                typeof(Dictionary<,>),
                            };
                            // Check if array/list, if so use json_length
                            if (node.Expression.Type.IsGenericType &&
                                genericTypes.Contains(node.Expression.Type.UnderlyingSystemType.GetGenericTypeDefinition()))
                            {
                                // Inner expression (node.Member = Count, node.Expression.Memeber = pokemonIds)
                                var expression = (MemberExpression)node.Expression;
                                var colAttr = expression.Member.GetCustomAttribute<ColumnAttribute>();
                                if (colAttr?.Name != null)
                                {
                                    var colName = _reservedKeywords?.Contains(colAttr.Name) ?? false
                                        ? $"`{colAttr.Name}`"
                                        : colAttr.Name;
                                    //Write($"JSON_LENGTH(JSON_EXTRACT({attr.Name}, \"$\"))");
                                    Write($"JSON_LENGTH({colName})");
                                }
                            }
                            else
                            {
                                Write("CHAR_LENGTH(");
                                Visit(node.Expression);
                                Write(")");
                            }
                            break;
                    }
                    return node;
            }
        }

        throw new NotSupportedException($"The member '{node.Member.Name}' is not supported");
    }

    protected ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
    {
        for (int i = 0, count = original.Count; i < count; i++)
        {
            Visit(original[i]);
            if (i < count - 1)
            {
                Write(",");
                WriteLine();
            }
        }
        return original;
    }

    protected Expression VisitValue(Expression node, bool skipVisit = false)
    {
        if (node.IsPredicate())
        {
            Write("CASE WHEN (");
            Visit(node);
            Write(") THEN 1 ELSE 0 END");
            return node;
        }

        if (skipVisit)
        {
            return node;
        }
        return Visit(node);
    }

    //public override Expression Visit(Expression? node)
    //{
    //    if (node == null)
    //    {
    //        return null!;
    //    }

    //    // check for supported node types first 
    //    // non-supported ones should not be visited (as they would produce bad SQL)
    //    switch (node.NodeType)
    //    {
    //        case ExpressionType.Negate:
    //        case ExpressionType.NegateChecked:
    //        case ExpressionType.Not:
    //        case ExpressionType.Convert:
    //        case ExpressionType.ConvertChecked:
    //        case ExpressionType.UnaryPlus:
    //        case ExpressionType.Add:
    //        case ExpressionType.AddChecked:
    //        case ExpressionType.Subtract:
    //        case ExpressionType.SubtractChecked:
    //        case ExpressionType.Multiply:
    //        case ExpressionType.MultiplyChecked:
    //        case ExpressionType.Divide:
    //        case ExpressionType.Modulo:
    //        case ExpressionType.And:
    //        case ExpressionType.AndAlso:
    //        case ExpressionType.Or:
    //        case ExpressionType.OrElse:
    //        case ExpressionType.LessThan:
    //        case ExpressionType.LessThanOrEqual:
    //        case ExpressionType.GreaterThan:
    //        case ExpressionType.GreaterThanOrEqual:
    //        case ExpressionType.Equal:
    //        case ExpressionType.NotEqual:
    //        case ExpressionType.Coalesce:
    //        case ExpressionType.RightShift:
    //        case ExpressionType.LeftShift:
    //        case ExpressionType.ExclusiveOr:
    //        case ExpressionType.Power:
    //        case ExpressionType.Conditional:
    //        case ExpressionType.Constant:
    //        case ExpressionType.MemberAccess:
    //        case ExpressionType.Call:
    //        case ExpressionType.New:
    //        case (ExpressionType)DbExpressionType.Table:
    //        case (ExpressionType)DbExpressionType.Column:
    //        case (ExpressionType)DbExpressionType.Select:
    //        case (ExpressionType)DbExpressionType.Join:
    //        case (ExpressionType)DbExpressionType.Aggregate:
    //        case (ExpressionType)DbExpressionType.Scalar:
    //        case (ExpressionType)DbExpressionType.Exists:
    //        case (ExpressionType)DbExpressionType.In:
    //        case (ExpressionType)DbExpressionType.AggregateSubquery:
    //        case (ExpressionType)DbExpressionType.IsNull:
    //        case (ExpressionType)DbExpressionType.Between:
    //        case (ExpressionType)DbExpressionType.RowCount:
    //        case (ExpressionType)DbExpressionType.Projection:
    //        case (ExpressionType)DbExpressionType.NamedValue:
    //        case (ExpressionType)DbExpressionType.Insert:
    //        case (ExpressionType)DbExpressionType.Update:
    //        case (ExpressionType)DbExpressionType.Delete:
    //        case (ExpressionType)DbExpressionType.Block:
    //        case (ExpressionType)DbExpressionType.If:
    //        case (ExpressionType)DbExpressionType.Declaration:
    //        case (ExpressionType)DbExpressionType.Variable:
    //        case (ExpressionType)DbExpressionType.Function:
    //            return base.Visit(node);

    //        case ExpressionType.ArrayLength:
    //        case ExpressionType.Quote:
    //        case ExpressionType.TypeAs:
    //        case ExpressionType.ArrayIndex:
    //        case ExpressionType.TypeIs:
    //        case ExpressionType.Parameter:
    //        case ExpressionType.Lambda:
    //        case ExpressionType.NewArrayInit:
    //        case ExpressionType.NewArrayBounds:
    //        case ExpressionType.Invoke:
    //        case ExpressionType.MemberInit:
    //        case ExpressionType.ListInit:
    //        default:
    //            //if (!forDebug)
    //            //{
    //            //    throw new NotSupportedException($"The expression node of type '{node.NodeType}' is not supported");
    //            //}
    //            //else
    //            //{
    //            Write($"?{node.NodeType}?");
    //            base.Visit(node);
    //            Write(")");
    //            return node;
    //            //}
    //    }
    //}

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        if (node.Test.IsPredicate())
        {
            Write("(CASE WHEN ");
            VisitPredicate(node.Test);
            Write(" THEN ");
            VisitValue(node.IfTrue);
            var ifFalse = node.IfFalse;
            while (ifFalse != null && ifFalse.NodeType == ExpressionType.Conditional)
            {
                var fc = (ConditionalExpression)ifFalse;
                Write(" WHEN ");
                VisitPredicate(fc.Test);
                Write(" THEN ");
                VisitValue(fc.IfTrue);
                ifFalse = fc.IfFalse;
            }
            if (ifFalse != null)
            {
                Write(" ELSE ");
                VisitValue(ifFalse);
            }
            Write(" END)");
        }
        else
        {
            Write("(CASE ");
            VisitValue(node.Test);
            Write(" WHEN 0 THEN ");
            VisitValue(node.IfFalse);
            Write(" ELSE ");
            VisitValue(node.IfTrue);
            Write(" END)");
        }
        return node;
    }

    protected virtual Expression VisitPredicate(Expression node)
    {
        Visit(node);
        if (!node.IsPredicate())
        {
            Write(" <> 0");
        }
        return node;
    }

    #endregion

    #region Expression Parsers

    private bool ParseAnyExpression(MethodCallExpression expression, bool includeOperand = true)
    {
        var memberExpression = (MemberExpression)expression.Arguments[0];
        var colAttr = memberExpression.Member.GetCustomAttribute<ColumnAttribute>();
        if (colAttr?.Name == null)
        {
            return false;
        }

        Write($"JSON_LENGTH({colAttr.Name})");
        if (includeOperand)
        {
            Write(" > 0");
        }
        return true;
    }

    private bool ParseOrderByExpression(MethodCallExpression expression, string order)
    {
        var unary = (UnaryExpression)expression.Arguments[1];
        var lambdaExpression = (LambdaExpression)unary.Operand;
        lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

        if (lambdaExpression.Body is not MemberExpression body)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_orderBy))
        {
            _orderBy = string.Format("{0} {1}", body.Member.Name, order);
        }
        else
        {
            _orderBy = string.Format("{0}, {1} {2}", _orderBy, body.Member.Name, order);
        }

        return true;
    }

    private bool ParseTakeExpression(MethodCallExpression expression)
    {
        var sizeExpression = (ConstantExpression)expression.Arguments[1];

        if (!int.TryParse(sizeExpression.Value?.ToString(), out var size))
        {
            return false;
        }

        _take = size;
        return true;
    }

    private bool ParseSkipExpression(MethodCallExpression expression)
    {
        var sizeExpression = (ConstantExpression)expression.Arguments[1];

        if (!int.TryParse(sizeExpression.Value?.ToString(), out var size))
        {
            return false;
        }

        _skip = size;
        return true;
    }

    #endregion

    #region Private Methods

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }
        return expression;
    }

    private void Write(object? value)
    {
        _sb.Append(value);
    }

    private void WriteLine(int indent = 2)
    {
        _sb.AppendLine();
        for (var i = 0; i < indent; i++)
        {
            Write(" ");
        }
    }

    #endregion
}

// Credits: https://github.com/mattwar/iqtoolkit/blob/master/docs/blog/building-part-03.md
public static class Evaluator
{
    /// <summary>
    /// Performs evaluation and replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression) =>
        PartialEval(expression, CanBeEvaluatedLocally, null!);

    /// <summary>
    /// Performs evaluation and replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated) =>
        PartialEval(expression, fnCanBeEvaluated, null!);

    /// <summary>
    /// Performs evaluation & replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
    /// <param name="fnPostEval">A function to apply to each newly formed <see cref="ConstantExpression"/>.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression, Func<Expression, bool>? fnCanBeEvaluated, Func<ConstantExpression, Expression> fnPostEval)
    {
        fnCanBeEvaluated ??= CanBeEvaluatedLocally;
        //return SubtreeEvaluator.Eval(new Nominator(fnCanBeEvaluated).Nominate(expression), fnPostEval, expression);
        return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression), fnPostEval).Eval(expression);
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter;
    }

    /// <summary>
    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
    /// </summary>
    private class SubtreeEvaluator : ExpressionVisitor
    {
        private readonly HashSet<Expression> _candidates;
        private readonly Func<ConstantExpression, Expression> _onEval;

        internal SubtreeEvaluator(HashSet<Expression> candidates, Func<ConstantExpression, Expression> onEval)
        {
            _candidates = candidates;
            _onEval = onEval;
        }

        internal Expression Eval(Expression node)
        {
            return Visit(node);
        }

        //internal static Expression Eval(HashSet<Expression> candidates, Func<ConstantExpression, Expression> onEval, Expression exp)
        //{
        //    return new SubtreeEvaluator(candidates, onEval).Visit(exp);
        //}

        public override Expression Visit(Expression? node)
        {
            if (node == null)
            {
                return null!;
            }

            if (_candidates.Contains(node))
            {
                return Evaluate(node);
            }

            return base.Visit(node);
        }

        //private static Expression Evaluate(Expression node)
        //{
        //    if (node.NodeType == ExpressionType.Constant)
        //    {
        //        return node;
        //    }

        //    var lambda = Expression.Lambda(node);
        //    var fn = lambda.Compile();
        //    return Expression.Constant(fn.DynamicInvoke(null), node.Type);
        //}

        private Expression Evaluate(Expression node)
        {
            var type = node.Type;

            switch (node.NodeType)
            {
                case ExpressionType.Convert:
                    // Check for unnecessary convert & strip them
                    var u = (UnaryExpression)node;
                    if (u.Operand.Type.GetNonNullableType() == type.GetNonNullableType())
                    {
                        node = ((UnaryExpression)node).Operand;
                    }
                    break;

                case ExpressionType.Constant:
                    // In case we actually threw out a nullable conversion above, simulate it here
                    // don't post-eval nodes that were already constants
                    if (node.Type == type)
                    {
                        return node;
                    }
                    else if (node.Type.GetNonNullableType() == type.GetNonNullableType())
                    {
                        return Expression.Constant(((ConstantExpression)node).Value, type);
                    }
                    break;
            }

            if (node is MemberExpression me)
            {
                // Member accesses off of constant's are common, and yet since these partial evals
                // are never re-used, using reflection to access the member is faster than compiling  
                // and invoking a lambda
                if (me.Expression is ConstantExpression ce)
                {
                    return PostEval(Expression.Constant(me.Member.GetValue(ce.Value), type));
                }
            }

            if (type.GetTypeInfo().IsValueType)
            {
                node = Expression.Convert(node, typeof(object));
            }

            var lambda = Expression.Lambda<Func<object>>(node);
            var fn = lambda.Compile();
            return PostEval(Expression.Constant(fn(), type));
        }

        private Expression PostEval(ConstantExpression node)
        {
            if (_onEval != null)
            {
                return _onEval(node);
            }
            return node;
        }
    }

    /// <summary>
    /// Performs bottom-up analysis to determine which nodes can possibly
    /// be part of an evaluated sub-tree.
    /// </summary>
    private class Nominator : ExpressionVisitor
    {
        private readonly Func<Expression, bool> _fnCanBeEvaluated;
        private HashSet<Expression> _candidates = null!;
        private bool _cannotBeEvaluated;

        internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
        {
            _fnCanBeEvaluated = fnCanBeEvaluated;
        }

        internal HashSet<Expression> Nominate(Expression expression)
        {
            _candidates = new HashSet<Expression>();

            Visit(expression);

            return _candidates;
        }

        internal static HashSet<Expression> Nominate(Func<Expression, bool> fnCanBeEvaluated, Expression expression)
        {
            var nominator = new Nominator(fnCanBeEvaluated);
            nominator.Visit(expression);
            return nominator._candidates;
        }

        //protected override Expression VisitConstant(ConstantExpression node)
        //{
        //    return base.VisitConstant(node);
        //}

        public override Expression Visit(Expression? expression)
        {
            if (expression == null)
            {
                return expression!;
            }

            var saveCannotBeEvaluated = _cannotBeEvaluated;
            _cannotBeEvaluated = false;

            base.Visit(expression);

            if (!_cannotBeEvaluated)
            {
                if (_fnCanBeEvaluated(expression))
                {
                    _candidates.Add(expression);
                }
                else
                {
                    _cannotBeEvaluated = true;
                }
            }
            _cannotBeEvaluated |= saveCannotBeEvaluated;

            return expression!;
        }
    }
}