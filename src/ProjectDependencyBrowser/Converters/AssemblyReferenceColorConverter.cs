extern alias build;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using build::MyToolkit.Build;

namespace ProjectDependencyBrowser.Converters
{
    public class AssemblyReferenceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((AssemblyReference) value).IsNuGetReference ? new SolidColorBrush(Colors.DarkBlue) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
