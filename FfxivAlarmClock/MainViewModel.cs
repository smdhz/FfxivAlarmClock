using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace FfxivAlarmClock
{
    internal class MainViewModel : Prism.Mvvm.BindableBase
    {
        private static MainViewModel instance;
        public static MainViewModel Instance => instance ??= new MainViewModel();

        private Models.ItemInfo[] allItems;

        #region 内部控制用
        internal Microsoft.UI.Dispatching.DispatcherQueue Dispatcher { private get; set; }
        public DateTimeOffset EorzeaTime { get; private set; }
        private Models.ItemInfo selected;
        public Models.ItemInfo Selected 
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }
        public string Query {private get; set; }
        public bool EnableAlarm { get; set; } = true;
        private bool small;
        public bool SmallIcon
        {
            get => small;
            set => SetProperty(ref small, value);
        }
        #endregion

        #region 显示信息用
        private bool ready;
        public bool Ready
        {
            get => ready;
            private set => SetProperty(ref ready, value);
        }
        public string EorzeaTimeExp => EorzeaTime.ToString("HH:mm:ss");
        public string LocalTime => DateTime.Now.ToString("T");
        public string LocalDate => DateTime.Now.ToString("d");
        public Models.ItemInfo[] Table { get; private set; }
        public System.Collections.ObjectModel.ObservableCollection<Models.ItemInfo> Favorite { get; private set; } = new System.Collections.ObjectModel.ObservableCollection<Models.ItemInfo>();
        #endregion

        private MainViewModel() 
        {
            Task.Run(FastTimer);
            Task.Run(SlowTimer);
            Init();
        }

        /// <summary>
        /// 事件处理用计时器
        /// </summary>
        private void FastTimer() 
        {
            while (true)
            {
                EorzeaTime = GetEorzeaTime();
                foreach(var i in Favorite)
                    Dispatcher.TryEnqueue(() => i.RefreshValue(EorzeaTime));

                if (Dispatcher != null)
                {
                    Dispatcher.TryEnqueue(() =>
                    {
                        RaisePropertyChanged(nameof(EorzeaTimeExp));
                        RaisePropertyChanged(nameof(LocalTime));
                    });
                }
                Task.Delay(1000).Wait();
            }
        }
        
        /// <summary>
        /// 非重要刷新用计时器
        /// </summary>
        private void SlowTimer() 
        {
            while (true)
            {
                foreach (var i in Favorite)
                    Dispatcher.TryEnqueue(() => i.SlowRefresh());

                Task.Delay(20000).Wait();
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private async void Init()
        {
            // 读取文件
            try
            {
                allItems = await Models.ItemInfo.Load();
            }
            catch (Exception ex)
            {
                new CommunityToolkit.WinUI.Notifications.ToastContentBuilder()
                    .AddText("连接失败")
                    .AddText(ex.Message)
                    .AddText("请检查网络连接")
                    .Show();
                allItems = Array.Empty<Models.ItemInfo>();
            }
            string[] checkList = await Load();

            foreach (var i in allItems)
            {
                // 监视选项改变并更新列表
                i.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(i.Checked))
                    {
                        Models.ItemInfo fi = Favorite.FirstOrDefault(j => j.NameJp == i.NameJp);
                        if (i.Checked)
                        {
                            Favorite.Add(i);
                            i.Alert += i.ShowAlert;
                        }
                        else if (fi != null)
                        {
                            Favorite.Remove(fi);
                            i.Alert -= i.ShowAlert;
                        }
                    }
                };
            }
            Favorite.CollectionChanged += (sender, e) => 
            {
                if (Ready) Task.Run(Save);
            };

            Table = allItems;
            foreach (string i in checkList)
            {
                Models.ItemInfo item = allItems.FirstOrDefault(j => j.NameJp == i);
                if (item != null)
                {
                    item.Checked = true;
                }
            }

            Dispatcher.TryEnqueue(() =>
            {
                RaisePropertyChanged(nameof(Ready));
                RaisePropertyChanged(nameof(Table));
            });
            Ready = true;
        }

        /// <summary>
        /// 计算艾欧泽亚时间
        /// </summary>
        /// <returns></returns>
        private DateTimeOffset GetEorzeaTime() =>
            DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 3600 / 175);

        /// <summary>
        /// 保存选项
        /// </summary>
        private async void Save() 
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            await storageFolder.CreateFileAsync("list.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(
                await storageFolder.GetFileAsync("list.txt"),
                string.Join(Environment.NewLine, Favorite.Select(i => i.NameJp)));
        }

        /// <summary>
        /// 载入选项设定列表
        /// </summary>
        /// <returns></returns>
        private async Task<string[]> Load() 
        {
            try
            {
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile file = await storageFolder.GetFileAsync("list.txt");
                string raw = await Windows.Storage.FileIO.ReadTextAsync(file);
                return raw.Split(Environment.NewLine).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        public void ExecuteQuery() 
        {
            if (string.IsNullOrEmpty(Query))
            {
                Table = allItems;
            }
            else
            {
                Table = allItems.Where(i => (i.NameCn + i.NameJp + i.NameEn).Contains(Query)).ToArray();
            }
            Dispatcher.TryEnqueue(() => RaisePropertyChanged(nameof(Table)));
        }
    }
}
