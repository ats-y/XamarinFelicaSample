using System;
using System.Diagnostics;
using NfcSamples.NfcService;
using Prism.Mvvm;
using Xamarin.Forms;

namespace NfcSamples.ViewModels
{
    public class MainPageViewModel : BindableBase
    {
        public Command StartPollingCommand { get; private set; }
        public Command StopPollingCommand { get; private set; }

        private int _remaining;
        public int Remaining
        {
            get => _remaining;
            set => SetProperty(ref _remaining, value);
        }

        private INfcService _nfcService;

        public MainPageViewModel()
        {
            StartPollingCommand = new Command(x => OnStartPolling());
            StopPollingCommand = new Command(x => OnStopPolling());

            _nfcService = DependencyService.Get<INfcService>();
        }

        private void OnStartPolling()
        {
            Debug.WriteLine($"OnStartPolling()");
            _nfcService.StartPolling(remaining =>
           {
               Remaining = remaining;
           });
        }

        private void OnStopPolling()
        {
            Debug.WriteLine($"OnStopPolling()");
            _nfcService.StopPolling();
        }
    }
}
