# voxsay

音声合成製品 VOICEVOX、VOICEVOX Nemo、COEIROINK/v2、LMROID、SHAREVOX、ITVOICE、 のREST APIを呼び出して音声再生するWindows用のCUIクライアント

## 使用方法

[こちら](https://github.com/k896951/voxsay/wiki)を参照してください。

使用前に、各音声合成製品を起動します。
その後にvoxsay.exeをオプション無しで実行するとヘルプが出ます。

ローカルで稼働している製品一覧を確認します。
```
f:\sandbox>voxsay -prodlist
product: sapi
product: sharevox
product: voicevox
product: coeiroinkv2

f:\sandbox>
```

SHAREVOXの話者一覧でインデクスを確認します。
```
f:\sandbox>voxsay -prod sharevox -list
index: 0,  speaker:小春音アミ（ノーマル）
index: 1,  speaker:小春音アミ（喜び）
index: 2,  speaker:小春音アミ（怒り）
index: 3,  speaker:小春音アミ（悲しみ）
index: 4,  speaker:つくよみちゃん（おしとやか）
index: 5,  speaker:白痴ー/黒聡鵜月（虚偽）
index: 6,  speaker:Yくん/開発者（ノーマル）
index: 7,  speaker:小春音アミ（ノーマルv2）
index: 8,  speaker:小春音アミ（喜びv2）
index: 9,  speaker:小春音アミ（怒りv2）
index: 10,  speaker:小春音アミ（悲しみv2）
index: 11,  speaker:つくよみちゃん（おしとやかv2）
index: 12,  speaker:Yくん/開発者（ノーマルv2）
index: 13,  speaker:白痴ー/黒聡鵜月（虚偽v2）
index: 14,  speaker:小春音アミ（ノーマルv3）
index: 15,  speaker:小春音アミ（喜びv3）
index: 16,  speaker:小春音アミ（怒りv3）
index: 17,  speaker:小春音アミ（悲しみv3）
index: 18,  speaker:つくよみちゃん（おしとやかv3）
index: 19,  speaker:Yくん/開発者（ノーマルv3）
index: 20,  speaker:白痴ー/黒聡鵜月（虚偽v3）
index: 21,  speaker:らごぱすブラック（ノーマル）
index: 22,  speaker:らごぱすホワイト（ノーマル）
index: 23,  speaker:風花ゆき（ノーマル）
index: 24,  speaker:安倍広葉（ノーマル）
index: 25,  speaker:鈴乃（ノーマル）

f:\sandbox>
```

鈴乃（ノーマル）に呟いてもらいます。

```
f:\sandbox>voxsay -prod sharevox -index 25 -t 早く寝てください！

f:\sandbox>
```

音量が大きい気がしたので下げます。

```
f:\sandbox>voxsay -prod sharevox -index 25 -volume 0.5 -t 早く寝てください！

f:\sandbox>
```

オプション -prod voicevox, -renderingmode sing, を指定すると、VOICEVOX 0.16.1で利用可能になった歌唱APIを使って歌わせることができます。
話者一覧でインデクスを確認します。-renderingmode talk の時と番号が異なるので注意してください。
```
f:\sandbox>voxsay -prod voicevox -renderingmode sing -renderingmode sing -list
index: 3000,  speaker:四国めたん（あまあま）
index: 3001,  speaker:ずんだもん（あまあま）
index: 3002,  speaker:四国めたん（ノーマル）
index: 3003,  speaker:ずんだもん（ノーマル）
index: 3004,  speaker:四国めたん（セクシー）
index: 3005,  speaker:ずんだもん（セクシー）
index: 3006,  speaker:四国めたん（ツンツン）
index: 3007,  speaker:ずんだもん（ツンツン）
index: 3008,  speaker:春日部つむぎ（ノーマル）
index: 3009,  speaker:波音リツ（ノーマル）
index: 3010,  speaker:雨晴はう（ノーマル）
index: 6000,  speaker:波音リツ（ノーマル）

f:\sandbox>
```
春日部つむぎにドレミファソラシドを歌ってもらいましょう。
```
f:\sandbox>voxsay -prod voicevox -renderingmode sing -index 3008 -t O4CDEFGABO5C

f:\sandbox>
```
-t オプションで MMLを指定する事で歌唱が可能になります。ただし正確な実装ではありません。それらしいように仕上げただけです。


### 定義ファイル

オプションのいくつかは、voxsay.exeと同じフォルダに作成したJSONファイル voxsayconf.json で省略できます。
```
{
	"prod":"voicevoxnemo",
	"index":10003,
	"speed":1.5,
	"outputdevice":"EX-LDGC242HT (NVIDIA High Definition Audio)"
}
```
この例だと、オプションを指定して上書きしない限り、voxsay は
```
-prod voicevoxnemo -speed 1.5 -outputdevice "EX-LDGC242HT (NVIDIA High Definition Audio)" -index 10003
```
が指定されたものとして動作します。


## 使用しているサードパーティライブラリとライセンス

### Fody

以下は各ライセンスで提供されています。

- Costura.Fody	5.7.0	geertvanhorrik,simoncropp	MIT
- Fody	6.8.0	Fody	https://www.nuget.org/packages/Fody/6.8.0/license

### NAudio

以下は各ライセンスで提供されています。

- NAudio	2.2.0	Mark Heath & Contributors	https://www.nuget.org/packages/NAudio/2.2.0/license
- NAudio.Asio	2.2.0	Mark Heath	MIT
- NAudio.Core	2.2.0	Mark Heath	MIT
- NAudio.Midi	2.2.0	Mark Heath	MIT
- NAudio.Wasapi	22.0.0	Mark Heath	MIT
- NAudio.WinForms	2.2.0	Mark Heath	MIT
- NAudio.WinMM	2.2.0	WinMM	MIT

### Microsoft

以下は MITライセンスもしくは https://dotnet.microsoft.com/ja-jp/dotnet_library_license.htm で示すライセンスで提供されています。

- Microsoft.Win32.Registry	5.0.0
- Microsoft.NETCore.Platforms	7.0.4
- Microsoft.Win32.Primitives	4.3.0
- NETStandard.Library	2.0.3
- System.AppContext	4.3.0
- System.Buffers	4.5.1
- System.Collections	4.3.0
- System.Collections.Concurrent	4.3.0
- System.Console	4.3.1
- System.Diagnostics.Debug	4.3.0
- System.Diagnostics.DiagnosticSource	7.0.2
- System.Diagnostics.Tools	4.3.0
- System.Diagnostics.Tracing	4.3.0
- System.Globalization	4.3.0
- System.Globalization.Calendars	4.3.0
- System.IO	4.3.0
- System.IO.Compression	4.3.0
- System.IO.Compression.ZipFile	4.3.0
- System.IO.FileSystem	4.3.0
- System.IO.FileSystem.Primitives	4.3.0
- System.Linq	4.3.0
- System.Linq.Expressions	4.3.0
- System.Memory	4.5.5
- System.Net.Http	4.3.4
- System.Net.Primitives	4.3.1
- System.Net.Sockets	4.3.0
- System.Numerics.Vectors	4.5.0
- System.ObjectModel	4.3.0
- System.Reflection	4.3.0
- System.Reflection.Extensions	4.3.0
- System.Reflection.Primitives	4.3.0
- System.Resources.ResourceManager	4.3.0
- System.Runtime	4.3.1
- System.Runtime.CompilerServices.Unsafe	6.0.0
- System.Runtime.Extensions	4.3.1
- System.Runtime.Handles	4.3.0
- System.Runtime.InteropServices	4.3.0
- System.Runtime.InteropServices.RuntimeInformation	4.3.0
- System.Runtime.Numerics	4.3.0
- System.Security.AccessControl	6.0.0
- System.Security.Cryptography.Algorithms	4.3.1
- System.Security.Cryptography.Encoding	4.3.0
- System.Security.Cryptography.Primitives	4.3.0
- System.Security.Cryptography.X509Certificates	4.3.2
- System.Security.Principal.Windows	5.0.0
- System.Text.Encoding	4.3.0
- System.Text.Encoding.Extensions	4.3.0
- System.Text.RegularExpressions	4.3.1
- System.Threading	4.3.0
- System.Threading.Tasks	4.3.0
- System.Threading.Timer	4.3.0
- System.Xml.ReaderWriter	4.3.1
- System.Xml.XDocument	4.3.0
