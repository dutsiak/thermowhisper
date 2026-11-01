using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Localization;

/// <summary>XAML text from the resource file without code-behind: <c>Title="{loc:Localize Key={x:Static keys:TextKeys.AppTitle}}"</c>.</summary>
[ContentProperty(nameof(Key))]
public sealed class LocalizeExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = string.Empty;

    public string ProvideValue(IServiceProvider serviceProvider) =>
        IPlatformApplication.Current!.Services.GetRequiredService<ILocalizer>().Get(Key);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
