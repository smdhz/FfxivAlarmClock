using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FfxivAlarmClock.Models
{
    internal enum Job
    {
        None,
        Botanist,
        Miner
    }

    public class JobConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Job)
            {
                if ((Job)value == Job.Botanist)
                    return "园艺";
                else if ((Job)value == Job.Miner)
                    return "采掘";
            }

            return "FAIL";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                if (Enum.TryParse(typeof(Job), value.ToString(), out object result))
                    return result;

                if (value.ToString() == "园艺")
                    return Job.Botanist;
                else if (value.ToString() == "采掘")
                    return Job.Miner;
            }

            return Job.None;
        }
    }

    public class JobImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Job)
            {
                if ((Job)value == Job.Botanist)
                    return "Assets\\Botanist.png";
                else if ((Job)value == Job.Miner)
                    return "Assets\\Miner.png";
            }

            return "FAIL";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
