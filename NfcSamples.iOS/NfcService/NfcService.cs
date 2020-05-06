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
        /// SuiCa残高をスキャンした際に呼び出す。
        /// int引数は読み取った残高。
        /// </summary>
        private Action<int, DateTime> _onScanAction;

        /// <summary>
        /// NFCタグ読取セッション
        /// </summary>
        private NFCTagReaderSession _session;

        /// <summary>
        /// FeliCaタグ
        /// </summary>
        private INFCFeliCaTag _felicaTag;

        /// <summary>
        /// Suica履歴のサービスコード
        /// （リトルエンディアン）
        /// </summary>
        private static readonly NSData[] ServiceCodes = { NSData.FromArray(new byte[] { 0x0F, 0x09 }) };

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public NfcService()
        {
        }

        /// <summary>
        /// Suica読取開始。
        /// </summary>
        /// <param name="onScanAction">Suica検知デリゲート。引数はSuicaから読み取った残高と日付。</param>
        public void StartScanningSuica(Action<int, DateTime> onScanAction)
        {
            // Suica検知デリゲートを保存。
            _onScanAction = onScanAction;

            // NFC読取セッションを開始する。
            _session = new NFCTagReaderSession(NFCPollingOption.Iso18092, this, DispatchQueue.CurrentQueue);
            _session.AlertMessage = "Suica/Kitacaをかざして";
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
        /// <param name="session">NFC読取セッション</param>
        /// <param name="tags">検知タグ</param>
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
                    session.InvalidateSession("タグ接続失敗。");
                    return;
                }

                // FeliCa準拠のタグプロトコルを取得する。
                _felicaTag = tags[0].GetNFCFeliCaTag();
                if (_felicaTag == null) return;

                // FeliCaのRequest Serviceコマンドを実行し、
                // サービスコード0x090Fを指定し、カード種別およびカード残額情報サービスに接続する。
                // （サービスコードはリトルエンディアンで）
                _felicaTag.RequestService(ServiceCodes, OnCompletedRequestService);
            });
        }

        /// <summary>
        /// FeliCaのRequest Serviceコマンドのレスポンス受信ハンドラ
        /// </summary>
        /// <param name="nodeVersions">ノード鍵バージョンリスト</param>
        /// <param name="err">エラー</param>
        private void OnCompletedRequestService(NSData[] nodeVersions, NSError err)
        {
            // 接続エラー時はメッセージを表示してNFC読取セッションを終了する、
            if (err != null)
            {
                Debug.WriteLine($"RequestService Error = [{err}]");
                _session.InvalidateSession("カード種別およびカード残額情報サービスに接続失敗。");
                return;
            }

            // 鍵バージョン（リトルエンディアン）
            byte[] keyVersion = nodeVersions[0].ToArray();
            if (keyVersion.SequenceEqual(new byte[] { 0xff, 0xff }))
            {
                // 0xFFFFは指定したノードが存在しない。
                Debug.WriteLine($"鍵バージョンが0xffff");
                _session.InvalidateSession("カード種別およびカード残額情報サービスが存在しない。");
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
            _felicaTag.ReadWithoutEncryption(ServiceCodes
                , readBlocks.ToArray()
                , OnCompletedReadWithoutEncryption);
        }

        /// <summary>
        /// FeliCaのRead Without Encryptionコマンドのレスポンス受信ハンドラ
        /// </summary>
        /// <param name="statusFlag1">ステータスフラグ1</param>
        /// <param name="statusFlag2">ステータスフラグ2</param>
        /// <param name="blockData">ブロックデータ</param>
        /// <param name="error">エラー</param>
        private void OnCompletedReadWithoutEncryption(nint statusFlag1, nint statusFlag2, NSData[] blockData, NSError error)
        {
            // ReadWithoutEncryptionエラー。
            if (error != null)
            {
                Debug.WriteLine($"ReadWithoutEncryption error = {error}");
                _session.InvalidateSession("ReadWithoutEncryptionエラー");
                return;
            }

            // ステータスフラグ異常。
            if (statusFlag1 != 0x00 || statusFlag2 != 0x00)
            {
                Debug.WriteLine($"status error. status1={statusFlag1:X2} status2={statusFlag2:X2}");
                _session.InvalidateSession("ステータスフラグが異常");
                return;
            }

            // 読取セッションを終了する。画面には成功イメージが表示される。
            _session.InvalidateSession();

            // 読み取ったブロックデータから残高を取り出す。
            byte[] readBytes = blockData[0].ToArray();
            int year = (readBytes[4] >> 1) + 2000;
            int month = ((readBytes[4] & 1) == 1 ? 8 : 0) + (readBytes[5] >> 5);
            int day = readBytes[5] & 0x1f;
            int remaining = readBytes[10] + (readBytes[11] << 8);
            DateTime useDate = new DateTime(year, month, day);
            Debug.WriteLine($"{year}年{month}月{day}日 {remaining}円");

            // 残高を通知する。
            _onScanAction(remaining, useDate);
        }
    }
}
