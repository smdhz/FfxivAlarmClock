using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

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
        #endregion

        #region 显示信息用
        public bool Ready { get; private set; }
        public string EorzeaTimeExp => EorzeaTime.ToString("HH:mm:ss");
        public string LocalTime => DateTime.Now.ToString("T");
        public string LocalDate => DateTime.Now.ToString("d");
        public Models.ItemInfo[] Table { get; private set; }
        public System.Collections.ObjectModel.ObservableCollection<Models.ItemInfo> Favorite { get; private set; } = new System.Collections.ObjectModel.ObservableCollection<Models.ItemInfo>();
        #endregion

        private MainViewModel() 
        {
            Task.Run(timer);
            Init();
        }

        /// <summary>
        /// 事件处理用计时器
        /// </summary>
        private void timer() 
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
        /// 初始化
        /// </summary>
        private async void Init()
        {
            allItems = await Models.ItemInfo.Load();
            foreach (var i in allItems)
            {
                // 监视选项改变并更新列表
                i.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(i.Checked))
                    {
                        Models.ItemInfo fi = Favorite.FirstOrDefault(j => j.NameJp == i.NameJp);
                        if (i.Checked)
                            Favorite.Add(i);
                        else if (fi != null)
                            Favorite.Remove(fi);
                    }
                };
            }
            Table = allItems;
            //TODO: Favorite.Add
            Ready = true;
            Dispatcher.TryEnqueue(() =>
            {
                RaisePropertyChanged(nameof(Ready));
                RaisePropertyChanged(nameof(Table));
            });
        }

        /// <summary>
        /// 计算艾欧泽亚时间
        /// </summary>
        /// <returns></returns>
        private DateTimeOffset GetEorzeaTime() =>
            DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 3600 / 175);
    }
}
