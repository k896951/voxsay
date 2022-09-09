# voxsay

voicevox | coeiroink | lmroid | sharevox のREST APIを呼び出して音声再生するCUIクライアント

## 使用方法

オプション無しで実行するとヘルプが出ます。

```
F:\Sandbox>voxsay

voxsay command 2022(c) by k896951

command line exsamples:
    voxsay <-prod TTS> -list
    voxsay <-prod TTS> <-index N> [option [option [... [option] ] ] ] -t TALKTEXT

Options:
    -prod TTS             : Select tts product. TTS := <voicevox | coeiroink | lmroid | sharevox>
    -list                 : List speakers for a given product.
    -index N              : specify the speaker index.
    -speed P              : specify the speedScale.
    -pitch P              : specify the pitchScale.
    -intonation P         : specify the intonationScale.
    -volume P             : specify the volumeScale.
    -prephonemelength P   : specify the prephonemelength.
    -postphonemelength P  : specify the postphonemelength.
    -t TALKTEXT           : specify the tts text.

        * Anything specified after -t is treated as tts text.
        * Please refer to the value of the editor for each product for the range of P.

F:\Sandbox>
```

製品の話者一覧でインデクスを確認します。

```
F:\Sandbox>voxsay -prod voicevox -list
index: 0,  speaker:四国めたん（あまあま）
index: 1,  speaker:ずんだもん（あまあま）
index: 2,  speaker:四国めたん（ノーマル）
index: 3,  speaker:ずんだもん（ノーマル）
index: 4,  speaker:四国めたん（セクシー）
index: 5,  speaker:ずんだもん（セクシー）
index: 6,  speaker:四国めたん（ツンツン）
index: 7,  speaker:ずんだもん（ツンツン）
index: 8,  speaker:春日部つむぎ（ノーマル）
index: 9,  speaker:波音リツ（ノーマル）
index: 10,  speaker:雨晴はう（ノーマル）
index: 11,  speaker:玄野武宏（ノーマル）
index: 12,  speaker:白上虎太郎（ノーマル）
index: 13,  speaker:青山龍星（ノーマル）
index: 14,  speaker:冥鳴ひまり（ノーマル）
index: 15,  speaker:九州そら（あまあま）
index: 16,  speaker:九州そら（ノーマル）
index: 17,  speaker:九州そら（セクシー）
index: 18,  speaker:九州そら（ツンツン）
index: 19,  speaker:九州そら（ささやき）
index: 20,  speaker:もち子さん（ノーマル）
index: 21,  speaker:剣崎雌雄（ノーマル）
index: 22,  speaker:ずんだもん（ささやき）

F:\Sandbox>
```

ずんだもん（セクシー）に呟いてもらいます。

```
F:\Sandbox>voxsay -prod voicevox -index 5 -t 早く寝るのだ！

F:\Sandbox>
```

あとは各自オプションを試してください。


## 使用しているサードパーティライブラリとライセンス

### Fody

以下は各ライセンスで提供されています。

　Costura.Fody	5.7.0	geertvanhorrik,simoncropp	MIT
　Fody	6.6.3	Fody	https://www.nuget.org/packages/Fody/6.6.3/license

### NAudio

以下は各ライセンスで提供されています。

　NAudio	2.1.0	Mark Heath & Contributors	https://www.nuget.org/packages/NAudio/2.1.0/license
　NAudio.Asio	2.1.0	Mark Heath	MIT
　NAudio.Core	2.1.0	Mark Heath	MIT
　NAudio.Midi	2.1.0	Mark Heath	MIT
　NAudio.Wasapi	2.1.0	Mark Heath	MIT
　NAudio.WinForms	2.1.0	Mark Heath	MIT
　NAudio.WinMM	2.1.0	WinMM	MIT

### Microsoft

以下は MITライセンスで提供されています。

　System.Security.AccessControl	6.0.0
　System.Runtime.CompilerServices.Unsafe	6.0.0
　System.Diagnostics.DiagnosticSource	6.0.0
　Microsoft.Win32.Registry	5.0.0
　Microsoft.NETCore.Platforms	6.0.5

以下は https://dotnet.microsoft.com/ja-jp/dotnet_library_license.htm で示すライセンスで提供されています。

　NETStandard.Library	2.0.3
　Microsoft.Win32.Primitives	4.3.0
　System.AppContext	4.3.0
　System.Buffers	4.5.1
　System.Collections	4.3.0
　System.Collections.Concurrent	4.3.0
　System.Console	4.3.1
　System.Diagnostics.Debug	4.3.0
　System.Diagnostics.Tools	4.3.0
　System.Diagnostics.Tracing	4.3.0
　System.Globalization	4.3.0
　System.Globalization.Calendars	4.3.0
　System.IO	4.3.0
　System.IO.Compression	4.3.0
　System.IO.Compression.ZipFile	4.3.0
　System.IO.FileSystem	4.3.0
　System.IO.FileSystem.Primitives	4.3.0
　System.Linq	4.3.0
　System.Linq.Expressions	4.3.0
　System.Memory	4.5.5
　System.Net.Http	4.3.4
　System.Net.Primitives	4.3.1
　System.Net.Sockets	4.3.0
　System.Numerics.Vectors	4.5.0
　System.ObjectModel	4.3.0
　System.Reflection	4.3.0
　System.Reflection.Extensions	4.3.0
　System.Reflection.Primitives	4.3.0
　System.Resources.ResourceManager	4.3.0
　System.Runtime	4.3.1
　System.Runtime.Extensions	4.3.1
　System.Runtime.Handles	4.3.0
　System.Runtime.InteropServices	4.3.0
　System.Runtime.InteropServices.RuntimeInformation	4.3.0
　System.Runtime.Numerics	4.3.0
　System.Security.Cryptography.Algorithms	4.3.1
　System.Security.Cryptography.Encoding	4.3.0
　System.Security.Cryptography.Primitives	4.3.0
　System.Security.Cryptography.X509Certificates	4.3.2
　System.Security.Principal.Windows	5.0.0
　System.Text.Encoding	4.3.0
　System.Text.Encoding.Extensions	4.3.0
　System.Text.RegularExpressions	4.3.1
　System.Threading	4.3.0
　System.Threading.Tasks	4.3.0
　System.Threading.Timer	4.3.0
　System.Xml.ReaderWriter	4.3.1
　System.Xml.XDocument	4.3.0
