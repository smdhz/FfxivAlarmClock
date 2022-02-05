using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace FfxivAlarmClock.Models
{
    internal class ItemInfo : BindableBase
    {
        private bool chk;
        public bool Checked 
        {
            get => chk;
            set => SetProperty(ref chk, value);
        }
        public GameVersion Version { get; private set; }
        public string NameCn { get; set; }
        public string NameEn { get; set; }
        public string NameJp { get; set; }
        public string Type { get; set; }
        public int Level { get; set; }
        public Job Job { get; set; }
        public MapInfo[] Maps { get; private set; }

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

        private DateTime lastSend;
        public event EventHandler Alert;

        public static async Task<ItemInfo[]> Load()
        {
            Windows.Storage.StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Windows.Storage.StorageFile list = await storageFolder.GetFileAsync("Data\\list.csv");
            List<ItemInfo> items = new List<ItemInfo>();

            using (StreamReader reader = new StreamReader(File.OpenRead(list.Path)))
            using (CsvHelper.CsvReader csv = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture))
            {
                GameVersionConverter vc = new GameVersionConverter();
                JobConverter jc = new JobConverter();

                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    items.Add(new ItemInfo
                    {
                        Version = (GameVersion)vc.ConvertBack(csv.GetField("版本归属"), typeof(GameVersion), null, null),
                        Type = csv.GetField("类型"),
                        NameCn = csv.GetField("材料名CN"),
                        NameJp = csv.GetField("材料名JP"),
                        NameEn = csv.GetField("材料名EN"),
                        Level = csv.GetField<int>("等级"),

                        Job = (Job)jc.ConvertBack(csv.GetField("职能"), typeof(Job), null, null),
                        Maps = new MapInfo[] 
                        {
                            new MapInfo
                            {
                                Starts = csv.GetField<int>("开始ET"),
                                Ends = csv.GetField<int>("结束ET"),
                                MapCn = csv.GetField("地区CN"),
                                MapJp = csv.GetField("地区JP"),
                                MapEn = csv.GetField("地区EN"),
                                Position = csv.GetField("具体坐标")
                            }
                        },
                    });
                }
            }

            foreach (ItemInfo i in items)
                i.Maps[0].TimeSpan = string.Format("{0:00}:00 - {1:00}:00", i.Maps[0].Starts, i.Maps[0].Ends);

            return items
                .GroupBy(i => i.NameJp)
                .Select(g =>
                {
                    var item = g.First();
                    item.Maps = g.SelectMany(i => i.Maps).ToArray();
                    return item;
                })
                .ToArray();
        }

        /// <summary>
        /// 计算进度
        /// </summary>
        /// <param name="eorzea">换算艾欧泽亚时间</param>
        public void RefreshValue(DateTimeOffset eorzea)
        {
            foreach (var i in Maps)
                i.SetValue(eorzea);
            var map = Maps.Any(i => i.Active) ? Maps.First(i => i.Active) : Maps.OrderBy(i => i.Value).First();

            Active = Maps.Any(i => i.Active);
            Value = map.Value;
            Maximum = map.Maximum;

            // 启动通知
            if (!Active && Value <= 300 && (DateTime.Now - lastSend).TotalSeconds > 600)
            {
                Alert?.Invoke(this, EventArgs.Empty);
                lastSend = DateTime.Now;
            }
        }
    }
}
