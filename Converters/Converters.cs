using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace APO.Converters
{
    public class FileNameWithoutExtensionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
                return Path.GetFileNameWithoutExtension(path) ?? path;
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AutorunHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInAutorun = value is bool b && b;
            string key = isInAutorun ? "RemoveFromAutorun" : "AddToAutorun";
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
