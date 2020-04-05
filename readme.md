# Xamarin.FormsでFeliCaにアクセスするサンプル

## できること

Suica/Kitacaの残高を表示できます。

## 画面イメージ

![起動直後](Images/001.png)

![スキャン待ち](Images/002.png)

![NFC検知](Images/003.png)

![残高表示](Images/004.png)

![失敗時](Images/005.png)

## クラス構成

![クラス図](Images/ClassDiagram.png)

<details>
<summary>PlantUMLコード</summary>

```plantuml

namespace NfcSamples  {

    namespace Views{
        class MainPageView
    }

    namespace ViewModels{
        class MainPageViewModel
    }

    NfcSamples.Views.MainPageView --> "BindingContext" NfcSamples.ViewModels.MainPageViewModel

    namespace NfcService{
        interface INfcService{
            StartPollingSuica(Action<int> getRemaining)
        }
    }

    NfcSamples.ViewModels.MainPageViewModel --> "_nfcService" NfcSamples.NfcService.INfcService
}

namespace NfcSample.iOS {

    namespace NfcService{
        class NfcService 

        NfcService --|> CoreNFC.NFCTagReaderSessionDelegate
    }
}

NfcSample.iOS.NfcService.NfcService ..|> NfcSamples.NfcService.INfcService

```

</details>