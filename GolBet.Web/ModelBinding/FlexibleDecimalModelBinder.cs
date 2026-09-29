using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GolBet.Web.ModelBinding;

// Accept a single decimal separator, never interpret a dot as a thousands separator.
public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var text = value.FirstValue?.Trim() ?? "";
        if (Regex.IsMatch(text, @"^[0-9]+([.,][0-9]{1,2})?$") &&
            decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
            context.Result = ModelBindingResult.Success(result);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Ingrese un número sin miles y con máximo dos decimales (ej.: 2,50 o 2.50).");
        return Task.CompletedTask;
    }
}

public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.Metadata.ModelType == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
}
