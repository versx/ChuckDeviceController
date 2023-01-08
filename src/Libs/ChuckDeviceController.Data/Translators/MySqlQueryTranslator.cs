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

    #region Overrides

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        //if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
        //{
        //    Visit(node.Arguments[0]);
        //    var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);
        //    Visit(lambda.Body);
        //    return node;
        //}

        switch (node.Method.Name)
        {
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
                //if (ParseAnyExpression(node))
                //{
                //    return Visit(node.Arguments[0]);
                //}
                break;

            case "IsNullOrEmpty" or "IsNullOrWhitespace":
                //if (ParseEmptyStringExpression(node))
                //{
                //    return Visit(node.Arguments[0]);
                //}
                break;
        }

        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported");
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                _sb.Append(" NOT ");
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
        _sb.Append('(');
        Visit(node.Left);

        switch (node.NodeType)
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
                if (IsNullConstant(node.Right))
                {
                    _sb.Append(" IS ");
                }
                else
                {
                    _sb.Append(" = ");
                }
                break;

            case ExpressionType.NotEqual:
                if (IsNullConstant(node.Right))
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
                throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported");

        }

        Visit(node.Right);
        _sb.Append(')');
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var q = node.Value as IQueryable;
        if (q == null && node.Value == null)
        {
            _sb.Append("NULL");
        }
        else if (q == null)
        {
            var typeCode = Type.GetTypeCode(node.Value?.GetType());
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    _sb.Append(((bool?)node.Value ?? false) ? 1 : 0);
                    break;

                case TypeCode.String:
                    _sb.Append('\'');
                    _sb.Append(node.Value);
                    _sb.Append('\'');
                    break;

                case TypeCode.DateTime:
                    _sb.Append('\'');
                    _sb.Append(node.Value);
                    _sb.Append('\'');
                    break;

                case TypeCode.Object:
                    throw new NotSupportedException($"The constant for '{node.Value}' is not supported");

                default:
                    _sb.Append(node.Value);
                    break;
            }
        }

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null)
        {
            var attr = node.Member.GetCustomAttribute<ColumnAttribute>();
            if (attr?.Name != null)
            {
                switch (node.Expression.NodeType)
                {
                    case ExpressionType.Parameter:
                        // Wrap column name in backticks if it is a reserved MySQL keyword
                        if (_reservedKeywords?.Contains(attr.Name) ?? false)
                        {
                            _sb.Append($"`{attr.Name}`");
                        }
                        else
                        {
                            _sb.Append(attr.Name);
                        }
                        return node;

                    case ExpressionType.MemberAccess:
                        //var expression = (MemberExpression)node.Expression;
                        //_sb.Append($"JSON_LENGTH(JSON_EXTRACT({attr.Name}, \"$\"))");
                        _sb.Append($"JSON_LENGTH({attr.Name})");
                        return node;
                }
            }
        }

        throw new NotSupportedException($"The member '{node.Member.Name}' is not supported");
    }

    #endregion

    #region Private Methods

    //private bool ParseAnyExpression(MethodCallExpression expression)
    //{
    //    var propertyExpression = (MemberExpression)expression.Arguments[0];
    //    var lambdaExpression = (ParameterExpression)propertyExpression.Expression;
    //    lambdaExpression = (ParameterExpression)Evaluator.PartialEval(lambdaExpression);

    //    //if (lambdaExpression.Body is MemberExpression body)
    //    //{
    //    //    return true;
    //    //}

    //    //return false;
    //    return true;

    //    var type = expression.Arguments[0].GetType();
    //    var p0 = Expression.Parameter(type);
    //    var result = Expression.Lambda(Expression.Call(expression.Method, p0),
    //          new ParameterExpression[] { p0 });
    //    return result;
    //}

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

    protected static bool IsNullConstant(Expression expression)
    {
        return expression.NodeType == ExpressionType.Constant && ((ConstantExpression)expression).Value == null;
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }
        return expression;
    }

    #endregion
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