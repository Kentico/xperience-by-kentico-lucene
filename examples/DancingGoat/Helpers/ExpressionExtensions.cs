using System;
using System.Linq.Expressions;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DancingGoat.Helpers;

internal static class ExpressionExtensions
{
    private static readonly ModelExpressionProvider ModelExpressionProvider = new ModelExpressionProvider(new EmptyModelMetadataProvider());

    /// <summary>
    /// Returns the expression text for the specified expression.
    /// </summary>
    public static string GetExpressionText<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression)
    {
        return ModelExpressionProvider.GetExpressionText(expression);
    }
}
