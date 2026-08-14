using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace SwiftDrop.App.Services;

[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class LocalizeExtension : IMarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("A localization resource key is required.");
        return AppText.Get(Key);
    }
}
