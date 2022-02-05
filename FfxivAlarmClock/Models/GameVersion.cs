using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FfxivAlarmClock.Models
{
    internal enum GameVersion
    {
        None = 0,
        Endwalker = 6,
        Shadowbringers = 5,
        Stormblood = 4,
        Heavensward = 3,
        RealmReborn = 2
    }

    public class GameVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is GameVersion)
            {
                return value.ToString();
            }

            return "FAIL";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                if(Enum.TryParse(typeof(GameVersion) ,value.ToString(), out object result))
                    return result;
            }

            return GameVersion.None;
        }
    }
}
