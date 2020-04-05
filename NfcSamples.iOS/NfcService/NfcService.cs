using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        /// <summary>
        /// NFCタグ読取セッション
        /// </summary>
        private NFCTagReaderSession _session;

        /// <summary>
        /// SuiCa残高をスキャンした際に呼び出す。
        /// int引数は読み取った残高。
        /// </summary>
        private Action<int> _onScanAction;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NfcService()
        {
        }

        /// <summary>
        /// Suicaポーリングを開始する。
        /// </summary>
        /// <param name="onScanAction"></param>
        public void StartPollingSuica(Action<int> onScanAction)
        {
            // Suica検知デリゲートを保存。
            _onScanAction = onScanAction;

            // NFC読取セッションを開始する。
            _session = new NFCTagReaderSession(NFCPollingOption.Iso18092, this, DispatchQueue.CurrentQueue);
            _session.AlertMessage = "Suika/Kitacaをかざして";
            _session.BeginSession();
        }

        /// <summary>
        /// NFC読取セッションが無効になった際に呼び出される。
        /// </summary>
        /// <param name="session">無効になったセッション</param>
        /// <param name="error">無効になった理由</param>
        public override void DidInvalidate(NFCTagReaderSession session, NSError error)
        {
            Debug.WriteLine($"DidInvalidate. error=[{error}]");
            _session.Dispose();
            _session = null;
        }

        /// <summary>
        /// NFC読取セッションがタグを検知されたら呼び出される。
        /// </summary>
        /// <param name="session"></param>
        /// <param name="tags"></param>
        public override void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags)
        {
            Debug.WriteLine($"DidDetectTags");

            // タグに接続する。
            if (tags.Length <= 0) return;
            session.ConnectTo(tags[0], connectErr =>
            {
                // 接続エラー時はメッセージを表示してNFC読取セッションを終了する、
                if (connectErr != null)
                {
                    Debug.WriteLine($"Connect Error = [{connectErr}]");
                    session.InvalidateSession("タグに接続失敗。");
                    return;
                }

                // FeliCa準拠のタグプロトコルを取得する。
                INFCFeliCaTag felica = tags[0].GetNFCFeliCaTag();
                if (felica == null) return;

                // サービスコード0x090Fを指定し、カード種別およびカード残額情報サービスに接続する。
                // （サービスコードはリトルエンディアンで）
                byte[] bytesServiceCode = { 0x09, 0x0f };
                Array.Reverse(bytesServiceCode);
                NSData[] serviceCodes = { NSData.FromArray(bytesServiceCode) };
                felica.RequestService(serviceCodes, (datas, err) =>
                {
                    // 接続エラー時はメッセージを表示してNFC読取セッションを終了する、
                    if (err != null)
                    {
                        Debug.WriteLine($"RequestService Error = [{err}]");
                        session.InvalidateSession("カード種別およびカード残額情報サービスに接続失敗。");
                        return;
                    }

                    // 鍵バージョン（リトルエンディアン）
                    byte[] keyVersion = datas[0].ToArray();
                    if( keyVersion.SequenceEqual( new byte[] { 0xff, 0xff }))
                    {
                        // 0xFFFFはエラー。
                        Debug.WriteLine($"鍵バージョンが0xffff");
                        session.InvalidateSession("カード種別およびカード残額情報サービスが存在しない。");
                        return;
                    }

                    // ReadWithoutEncryptionコマンドで
                    // ブロック番号0〜11の12個分のブロックを読み取るブロックリストを作成する。
                    List<NSData> readBlocks = new List<NSData>();
                    for (int i = 0; i < 12; i++)
                    {
                        byte[] block = new byte[] { 0x80, (byte)i };
                        readBlocks.Add(NSData.FromArray(block));
                    }

                    // ブロックデータを読み出す。
                    felica.ReadWithoutEncryption(serviceCodes
                        , readBlocks.ToArray()
                        , (status1, status2, dataList, readErr) =>
                    {
                        // ReadWithoutEncryptionエラー。
                        if (readErr != null)
                        {
                            Debug.WriteLine($"ReadWithoutEncryption error = {readErr}");
                            session.InvalidateSession("ReadWithoutEncryptionエラー");
                            return;
                        }

                        // ステータスフラグ異常。
                        if (status1 != 0x00 || status2 != 0x00)
                        {
                            Debug.WriteLine($"status error. status1={status1:X2} status2={status2:X2}");
                            session.InvalidateSession("ステータスフラグが異常");
                            return;
                        }

                        // 読取セッションを終了する。画面には成功イメージが表示される。
                        session.InvalidateSession();

                        // 読み取ったブロックデータから残高を取り出す。
                        byte[] readBytes = dataList[0].ToArray();
                        int year = (readBytes[4] >> 1) + 2000;
                        int month = ((readBytes[4] & 1) == 1 ? 8 : 0) + (readBytes[5] >> 5);
                        int day = readBytes[5] & 0x1f;
                        int remaining = readBytes[10] + (readBytes[11] << 8);
                        Debug.WriteLine($"{year}年{month}月{day}日 {remaining}円");

                        // 残高を通知する。
                        _onScanAction(remaining);
                    });
                });
            });
        }
    }
}
