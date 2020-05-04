﻿using System;
namespace NfcSamples.NfcService
{
    /// <summary>
    /// NFCサービス
    /// </summary>
    public interface INfcService
    {
        /// <summary>
        /// Suicaポーリング開始。
        /// </summary>
        /// <param name="onScanAction">Suica検知デリゲート。引数はSuicaから読み取った残高。</param>
        void StartScanningSuica(Action<int, DateTime> onScanAction);
    }
}
