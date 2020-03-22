using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoreFoundation;
using CoreNFC;
using Foundation;
using NfcSamples.NfcService;
using Xamarin.Forms;

[assembly: Dependency(typeof(NfcSamples.iOS.NfcService.NfcService))]
namespace NfcSamples.iOS.NfcService
{
    /// <summary>
    /// NFCサービス
    /// 参考サイト：
    /// https://tech-blog.rakus.co.jp/entry/20190930/ios
    /// https://qiita.com/m__ike_/items/7dc3e643396cf3381167
    /// </summary>
    public class NfcService : NFCTagReaderSessionDelegate, INfcService
    {
        private NFCTagReaderSession _nfcSession;

        public NfcService()
        {
        }

        public override void DidInvalidate(NFCTagReaderSession session, NSError error)
        {
            Debug.WriteLine($"DidInvalidate");
        }

        public override void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags)
        {
            Debug.WriteLine($"DidDetectTags");

            if (tags.Length < 1) return;

            session.ConnectTo(tags[0], x =>
            {
                if (x != null)
                {
                    Debug.WriteLine($"Error {x.ToString()}");
                    return;
                }

                INFCFeliCaTag felica = tags[0].GetNFCFeliCaTag();
                if (felica == null) return;

                byte[] bytesServiceCode = { 0x09, 0x0f };
                Array.Reverse(bytesServiceCode);

                NSData[] serviceCodes = { NSData.FromArray(bytesServiceCode) };
                felica.RequestService(serviceCodes, (datas, err) =>
                {
                    if (err != null)
                    {
                        Debug.WriteLine($"RequestService error = {err.ToString()}");
                        return;
                    }

                    // FFFF
                    List<NSData> readBlocks = new List<NSData>();
                    for (int i = 0; i < 12; i++)
                    {
                        byte[] block = new byte[] { 0x80, (byte)i };
                        readBlocks.Add(NSData.FromArray(block));
                    }

                    felica.ReadWithoutEncryption(serviceCodes, readBlocks.ToArray(), (status1, status2, dataList, readErr) =>
                    {
                        if (readErr != null)
                        {
                            Debug.WriteLine($"ReadWithoutEncryption error = {readErr.ToString()}");
                            return;
                        }

                        if (status1 != 0x00 || status2 != 0x00)
                        {
                            Debug.WriteLine($"status error. status1={status1:X2} status2={status2:X2}");
                            return;
                        }

                        session.InvalidateSession();

                        byte[] readBytes = dataList[0].ToArray();
                        int year = (readBytes[4] >> 1) + 2000;
                        int month = ((readBytes[4] & 1) == 1 ? 8 : 0) + (readBytes[5] >> 5);
                        int day = readBytes[5] & 0x1f;
                        int remaining = readBytes[10] + (readBytes[11] << 8);

                        Debug.WriteLine($"{year}年{month}月{day}日 {remaining}円");
                        _notifyRemaining(remaining);
                    });
                });
            });

        }

        private Action<int> _notifyRemaining;

        public void StartPolling( Action<int> getRemaining )
        {
            _notifyRemaining = getRemaining;
            _nfcSession = new NFCTagReaderSession(NFCPollingOption.Iso18092, this, DispatchQueue.CurrentQueue);
            _nfcSession.AlertMessage = "NFCをどうぞ";
            _nfcSession.BeginSession();
        }

        public void StopPolling()
        {
            _nfcSession?.InvalidateSession();
        }
    }
}
