namespace ChuckDeviceController.Data.Translators;

using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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

    public MySqlQueryTranslator(IEnumerable<string>? reservedKeywords = null)
    {
        _reservedKeywords = reservedKeywords;
    }

    public string Translate(Expression expression)
    {
        expression = Evaluator.PartialEval(expression);

        _sb = new StringBuilder();
        Visit(expression);
        _whereClause = _sb.ToString();
        return _whereClause;
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }
        return e;
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
        {
            Visit(m.Arguments[0]);
            var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
            Visit(lambda.Body);
            return m;
        }
        else if (m.Method.Name == "Take")
        {
            if (ParseTakeExpression(m))
            {
                var nextExpression = m.Arguments[0];
                return Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "Skip")
        {
            if (ParseSkipExpression(m))
            {
                var nextExpression = m.Arguments[0];
                return Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderBy")
        {
            if (ParseOrderByExpression(m, "ASC"))
            {
                var nextExpression = m.Arguments[0];
                return Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "OrderByDescending")
        {
            if (ParseOrderByExpression(m, "DESC"))
            {
                var nextExpression = m.Arguments[0];
                return Visit(nextExpression);
            }
        }
        else if (m.Method.Name == "Any")
        {
            if (ParseAnyExpression(m))
            {
                var nextExpression = m.Arguments[0];
                var exp = Visit(nextExpression);
                return exp;
            }
        }
        else if (m.Method.Name == "IsNullOrEmpty" || m.Method.Name == "IsNullOrWhitespace")
        {
            //if (ParseEmptyStringExpression(m))
            //{
            var nextExpression = m.Arguments[0];
            return Visit(nextExpression);
            //}
        }

        // TODO: IsNullOrEmpty / IsNullOrWhitespace

        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                _sb.Append(" NOT ");
                Visit(u.Operand);
                break;
            case ExpressionType.Convert:
                Visit(u.Operand);
                break;
            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }
        return u;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    protected override Expression VisitBinary(BinaryExpression b)
    {
        _sb.Append('(');
        Visit(b.Left);

        switch (b.NodeType)
        {
            case ExpressionType.And:
                _sb.Append(" AND ");
                break;

            case ExpressionType.AndAlso:
                _sb.Append(" AND ");
                break;

            case ExpressionType.Or:
                _sb.Append(" OR ");
                break;

            case ExpressionType.OrElse:
                _sb.Append(" OR ");
                break;

            case ExpressionType.Equal:
                if (IsNullConstant(b.Right))
                {
                    _sb.Append(" IS ");
                }
                else
                {
                    _sb.Append(" = ");
                }
                break;

            case ExpressionType.NotEqual:
                if (IsNullConstant(b.Right))
                {
                    _sb.Append(" IS NOT ");
                }
                else
                {
                    _sb.Append(" <> ");
                }
                break;

            case ExpressionType.LessThan:
                _sb.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                _sb.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                _sb.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                _sb.Append(" >= ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));

        }

        Visit(b.Right);
        _sb.Append(')');
        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        var q = c.Value as IQueryable;
        if (q == null && c.Value == null)
        {
            _sb.Append("NULL");
        }
        else if (q == null)
        {
            var typeCode = Type.GetTypeCode(c.Value?.GetType());
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    _sb.Append(((bool?)c.Value ?? false) ? 1 : 0);
                    break;

                case TypeCode.String:
                    _sb.Append('\'');
                    _sb.Append(c.Value);
                    _sb.Append('\'');
                    break;

                case TypeCode.DateTime:
                    _sb.Append('\'');
                    _sb.Append(c.Value);
                    _sb.Append('\'');
                    break;

                //case TypeCode.Int32:
                //    if (c.Type.IsEnum)
                //    {
                //        Console.WriteLine($"Enum: {c}");
                //        var enumerator = c.Value as Enum;
                //        Console.WriteLine($"Enumerator: {enumerator}");
                //        _sb.Append(c.Value);
                //    }
                //    else
                //    {
                //        var instanceType = (InstanceType)c.Value;
                //        _sb.Append('\'');
                //        _sb.Append(Instance.InstanceTypeToString(instanceType));
                //        _sb.Append('\'');
                //    }
                //    break;

                case TypeCode.Object:
                    throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                default:
                    _sb.Append(c.Value);
                    break;
            }
        }

        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null)
        {
            if (m.Expression.NodeType == ExpressionType.Parameter)
            {
                var attr = m.Member.GetCustomAttribute<ColumnAttribute>();
                if (attr?.Name != null)
                {
                    if (_reservedKeywords?.Contains(attr.Name) ?? false)
                    {
                        _sb.Append($"`{attr.Name}`");
                    }
                    else
                    {
                        _sb.Append(attr.Name);
                    }
                }
                //else
                //{
                //    _sb.Append(m.Member.Name);
                //}
                return m;
            }
            else if (m.Expression.NodeType == ExpressionType.MemberAccess)
            {
                var expression = (MemberExpression)m.Expression;
                var attr = expression.Member.GetCustomAttribute<ColumnAttribute>();
                if (attr?.Name != null)
                {
                    //_sb.Append($"JSON_LENGTH(JSON_EXTRACT({attr.Name}, \"$\"))");
                    _sb.Append($"JSON_LENGTH({attr.Name})");
                    return m;
                }
            }
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    protected static bool IsNullConstant(Expression exp)
    {
        return exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null;
    }

    private bool ParseAnyExpression(MethodCallExpression expression)
    {
        //var propertyExpression = (MemberExpression)expression.Arguments[0];
        //var lambdaExpression = (ParameterExpression)propertyExpression.Expression;
        //lambdaExpression = (ParameterExpression)Evaluator.PartialEval(lambdaExpression);

        ////if (lambdaExpression.Body is MemberExpression body)
        ////{
        ////    return true;
        ////}

        ////return false;
        //return true;

        //var type = expression.Arguments[0].GetType();
        //var p0 = Expression.Parameter(type);
        //var result = Expression.Lambda(Expression.Call(expression.Method, p0),
        //      new ParameterExpression[] { p0 });
        //return result;
        return true;
    }

    private bool ParseOrderByExpression(MethodCallExpression expression, string order)
    {
        var unary = (UnaryExpression)expression.Arguments[1];
        var lambdaExpression = (LambdaExpression)unary.Operand;
        lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

        if (lambdaExpression.Body is MemberExpression body)
        {
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

        return false;
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
}

// Credits: https://github.com/mattwar/iqtoolkit/blob/master/docs/blog/building-part-03.md
public static class Evaluator
{
    /// <summary>
    /// Performs evaluation & replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
    {
        return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
    }

    /// <summary>
    /// Performs evaluation & replacement of independent sub-trees
    /// </summary>
    /// <param name="expression">The root of the expression tree.</param>
    /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
    public static Expression PartialEval(Expression expression)
    {
        return PartialEval(expression, CanBeEvaluatedLocally);
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

        internal SubtreeEvaluator(HashSet<Expression> candidates)
        {
            _candidates = candidates;
        }

        internal Expression Eval(Expression exp)
        {
            return Visit(exp);
        }

        public override Expression Visit(Expression? exp)
        {
            if (exp == null)
            {
                return null!;
            }

            if (_candidates.Contains(exp))
            {
                return Evaluate(exp);
            }

            return base.Visit(exp);
        }

        private static Expression Evaluate(Expression e)
        {
            if (e.NodeType == ExpressionType.Constant)
            {
                return e;
            }

            var lambda = Expression.Lambda(e);
            var fn = lambda.Compile();
            return Expression.Constant(fn.DynamicInvoke(null), e.Type);
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

        public override Expression Visit(Expression? expression)
        {
            if (expression != null)
            {
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
            }
            return expression!;
        }
    }
}