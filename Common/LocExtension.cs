using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace VocabTrainer.Common
{
    /// <summary>
    /// XAML markup extension: {loc:Loc Nav_Home}
    /// Binds to LocalizationService.Instance[key] with live-update on language change.
    /// </summary>
    [MarkupExtensionReturnType(typeof(BindingExpression))]
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public LocExtension() { }
        public LocExtension(string key) => Key = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationService.Instance,
                Mode = BindingMode.OneWay,
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
