using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FfxivAlarmClock.Models
{
    internal class MapInfo : BindableBase, IComparable<MapInfo>
    {
        private static readonly TimeSpan WHOLE_DAY = new TimeSpan(24, 0, 0);

        public int Starts { get; set; }
        public int Ends { get; set; }
        public string TimeSpan { get; set; }
        public string MapCn { get; set; }
        public string MapEn { get; set; }
        public string MapJp { get; set; }
        public string Position { get; set; }

        private bool active;
        public bool Active
        {
            get => active;
            set => SetProperty(ref active, value);
        }
        private double val;
        public double Value
        {
            get => val;
            private set => SetProperty(ref val, value);
        }
        private double max;
        public double Maximum
        {
            get => max;
            private set => SetProperty(ref max, value);
        }

        /// <summary>
        /// 计算有效标的
        /// </summary>
        /// <param name="eorzea">换算艾欧泽亚时间</param>
        public void SetValue(DateTimeOffset eorzea)
        {
            TimeSpan start = new TimeSpan(Starts, 0, 0);
            TimeSpan end = new TimeSpan(Ends, 0, 0);

            if (start <= eorzea.TimeOfDay && eorzea.TimeOfDay <= end)
            {
                Active = true;
                Maximum = (Ends - Starts) * 3600;
                Value = (eorzea.TimeOfDay - new TimeSpan(Starts, 0, 0)).TotalSeconds;
            }
            else
            {
                double offset = eorzea.TimeOfDay < start ?
                    (start - eorzea.TimeOfDay).TotalSeconds :
                    (start + WHOLE_DAY - eorzea.TimeOfDay).TotalSeconds;
                Active = false;
                Maximum = 24 * 3600;
                Value = 24 * 3600 - offset;
            }
        }

        /// <summary>
        /// 定义比较方法
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(MapInfo other)
        {
            if (Active && !other.Active)
                return int.MaxValue;
            else if (!Active && other.Active)
                return int.MinValue;
            else
                return Value.CompareTo(other.Value);
        }
    }

    /// <summary>
    /// 强制排序支持
    /// </summary>
    internal static class ObservableCollectionSorter 
    {
        public static void SortDescending<T>(this ObservableCollection<T> collection) where T : IComparable<T>
        {
            List<T> sorted = collection.OrderByDescending(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }
    }
}
