using System;
using System.Diagnostics;
using NfcSamples.NfcService;
using Prism.Mvvm;
using Xamarin.Forms;

namespace NfcSamples.ViewModels
{
    /// <summary>
    /// MainPageのViewModel。
    /// </summary>
    public class MainPageViewModel : BindableBase
    {
        /// <summary>
        /// NFCサービス
        /// </summary>
        private INfcService _nfcService;

        /// <summary>
        /// NFCポーリング開始コマンド
        /// </summary>
        public Command StartPollingCommand { get; private set; }

        /// <summary>
        /// 残高
        /// </summary>
        private int _remaining;
        public int Remaining
        {
            get => _remaining;
            set => SetProperty(ref _remaining, value);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainPageViewModel()
        {
            _nfcService = DependencyService.Get<INfcService>();
            StartPollingCommand = new Command(x => OnStartPolling());
        }

        /// <summary>
        /// NFCポーリング開始コマンドイベントハンドラ
        /// </summary>
        private void OnStartPolling()
        {
            // NFCポーリングを開始する。
            Debug.WriteLine($"OnStartPolling()");
            _nfcService.StartPollingSuica(remaining =>
            {
                Remaining = remaining;
            });
        }
    }
}
