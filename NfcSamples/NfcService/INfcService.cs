using System;
namespace NfcSamples.NfcService
{
    public interface INfcService
    {
        void StartPolling(Action<int> getRemaining);
        void StopPolling();
    }
}
