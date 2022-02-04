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

        #region 内部控制用
        internal Microsoft.UI.Dispatching.DispatcherQueue Dispatcher { private get; set; }
        public DateTimeOffset EorzeaTime { get; private set; }
        #endregion

        #region 显示信息用
        public string EorzeaTimeExp => EorzeaTime.ToString("HH:mm:ss");
        public string LocalTime => DateTime.Now.ToString("t");
        public string LocalDate => DateTime.Now.ToString("d");
        #endregion

        private MainViewModel() 
        {
            Task.Run(timer);
        }

        /// <summary>
        /// 事件处理用计时器
        /// </summary>
        private void timer() 
        {
            while (true)
            {
                EorzeaTime = GetEorzeaTime();

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
        /// 计算艾欧泽亚时间
        /// </summary>
        /// <returns></returns>
        private DateTimeOffset GetEorzeaTime() =>
            DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 3600 / 175);
    }
}
