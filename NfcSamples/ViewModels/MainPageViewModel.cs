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
        /// NFC読取開始コマンド
        /// </summary>
        public Command StartScanningCommand { get; private set; }

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
        /// 使用日時
        /// </summary>
        private DateTime _useDate = DateTime.MinValue;
        public DateTime UseDate
        {
            get => _useDate;
            set => SetProperty(ref _useDate, value);
        }

        /// <summary>
        /// 残高エリアを表示するか
        /// </summary>
        private bool _isVisibleRemaining;
        public bool IsVisibleRemaining
        {
            get => _isVisibleRemaining;
            set => SetProperty(ref _isVisibleRemaining, value);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainPageViewModel()
        {
            IsVisibleRemaining = false;
            StartScanningCommand = new Command(x => OnStartScanningCommand());
        }

        /// <summary>
        /// NFCポーリング開始コマンドイベントハンドラ
        /// </summary>
        private void OnStartScanningCommand()
        {
            // 画面上の残高情報を隠す。
            IsVisibleRemaining = false;
            Remaining = 0;
            UseDate = DateTime.MinValue;

            // NFCポーリングを開始する。
            INfcService nfcService = DependencyService.Get<INfcService>();
            nfcService.StartScanningSuica((remaining, useDate) =>
            {
                // 読取に成功したら読み取った残高を表示する。
                Remaining = remaining;
                UseDate = useDate;
                IsVisibleRemaining = true;
            });
        }
    }
}
