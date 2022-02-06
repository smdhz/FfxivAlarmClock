using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace FfxivAlarmClock.Models
{
    /// <summary>
    /// 道具信息
    /// </summary>
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
        public ObservableCollection<MapInfo> Maps { get; private set; }

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

        public MapInfo mapInline;
        private DateTime lastSend;
        public event EventHandler Alert;

        /// <summary>
        /// 载入数据文件
        /// </summary>
        /// <returns></returns>
        public static async Task<ItemInfo[]> Load()
        {
            HttpClient http = new HttpClient();
            string raw = await http.GetStringAsync(new Uri("https://www.kokuryuu.club/static/ffxiv.csv"));
            List<ItemInfo> items = new List<ItemInfo>();

            using (StringReader reader = new StringReader(raw))
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
                        mapInline = new MapInfo
                        {
                            Starts = csv.GetField<int>("开始ET"),
                            Ends = csv.GetField<int>("结束ET"),
                            MapCn = csv.GetField("地区CN"),
                            MapJp = csv.GetField("地区JP"),
                            MapEn = csv.GetField("地区EN"),
                            Position = csv.GetField("具体坐标")
                        }
                    });
                }
            }

            foreach (ItemInfo i in items)
                i.mapInline.TimeSpan = string.Format("{0:00}:00 - {1:00}:00", i.mapInline.Starts, i.mapInline.Ends);

            return items
                .GroupBy(i => i.NameJp)
                .Select(g =>
                {
                    var item = g.First();
                    item.Maps = new ObservableCollection<MapInfo>(g.Select(i => i.mapInline));
                    return item;
                })
                .ToArray();
        }

        /// <summary>
        /// 计算进度（实时）
        /// </summary>
        /// <param name="eorzea">换算艾欧泽亚时间</param>
        public void RefreshValue(DateTimeOffset eorzea)
        {
            foreach (var i in Maps)
                i.SetValue(eorzea);
            var map = Maps.Any(i => i.Active) ? Maps.First(i => i.Active) : Maps.OrderByDescending(i => i.Value).First();

            Active = Maps.Any(i => i.Active);
            Value = map.Value;
            Maximum = map.Maximum;
        }

        /// <summary>
        /// 更新数据（低频率）
        /// </summary>
        public void SlowRefresh() 
        {
            Maps.SortDescending();

            // 启动通知
            if (MainViewModel.Instance.EnableAlarm && !Active && Maximum - Value <= 3600 && (DateTime.Now - lastSend).TotalSeconds > 600)
            {
                Alert?.Invoke(this, EventArgs.Empty);
                lastSend = DateTime.Now;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:验证平台兼容性", Justification = "<挂起>")]
        public void ShowAlert(object sender, EventArgs e)
        {
            MapInfo map = Maps.FirstOrDefault(i => i.Active);
            if (map != null) 
            {
                new CommunityToolkit.WinUI.Notifications.ToastContentBuilder()
                    .AddText($"{NameJp} / {map.TimeSpan}")
                    .AddText($"{map.MapJp} ({map.MapCn}/{map.MapEn})")
                    .AddText(map.Position)
                    .Show();
            }
        }
    }
}
