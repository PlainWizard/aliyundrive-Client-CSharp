using aliyundrive_Client_CSharp.aliyundrive;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace aliyundrive_Client_CSharp
{    public class DateConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                switch (parameter)
                {
                    case "FormatDate": return FormatDate(value.ToString());
                    case "FileSize": return FileSize(value);
                }
            }
            catch { }
            return value;
        }

        object FormatDate(string value)
        {
            return DateTime.Parse(value).ToString("yyyy-MM-dd\r\nHH:mm:ss");
        }
        object FileSize(object value)
        {
            var v = value as info_file;
            if (v.type == "folder") return "目录";
            long size = v.size;
            String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
            long mod = 1024;
            int i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }
            return size + "\r\n"+ units[i];
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
