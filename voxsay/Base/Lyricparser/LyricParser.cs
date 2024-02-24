using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace voxsay
{
    public class LyricParser
    {
        private const string lyricParentChars1 =
                                   @"[あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
                                   @"アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
                                   @"がぎぐげござじずぜぞだぢづでどばびぶべぼ" +
                                   @"ガギグゲゴザジズゼゾダヂヅデドバビブベボ" +
                                   @"ぱぴぷぺぽ" +
                                   @"パピプペポ" +
                                   @"ぁぃぅぇぉゃゅょァィゥェォャュョっッー" +
                                   @"+\-" +
                                   @"\(（\)）" +
                                   @" 　\t]";          // タブ + 空白(半角 + 全角)

        private const string lyricParentChars2 =
                                   @"[きぎ][ぇゃゅょ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[キギ][ェャュョ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[しじ][ぇゃゅょ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[シジ][ェャュョ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[すず][ぃ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[スズ][ィ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ちぢ][ぇゃゅょ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[チヂ][ェャュョ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[つ][ぁぃぇぉ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ツ][ァィェォ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[てで][ぃ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[テデ][ィ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[と][ぅ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ト][ゥ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[に][ぇゃゅょ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ニ][ェャュョ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ひびぴ][ぇゃゅょ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ヒビピ][ェャュョ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ふ][ぁぃぇぉ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[フ][ァィェォ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[み][ぇゃゅょ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[ミ][ェャュョ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[り][ぇ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @"[リ][ェ]ー{0,1}[+\-]{0,1}\d{0,1}|" +
                                   @".[+\-]{0,1}\d{0,1}";

        private const string lyricParentChars3 = @"[ー 　\t]"; // タブ + 空白(半角 + 全角)含む
        private const string lyricParentChars4 = @"(.)([+\-]{1,1}\d{0,})";
        private const string lyricParentChars5 = @"[っッ][+\-]{0,1}";

        private const string lyricGroupingOpen = @"（(";
        private const string lyricGroupingClose = @")）";

        private Regex lyricParentChars1ex;
        private Regex lyricParentChars2ex;
        private Regex lyricParentChars3ex;
        private Regex lyricParentChars4ex;
        private Regex lyricParentChars5ex;

        public LyricParser()
        {
            lyricParentChars1ex = new Regex(lyricParentChars1);
            lyricParentChars2ex = new Regex(lyricParentChars2);
            lyricParentChars3ex = new Regex(lyricParentChars3);
            lyricParentChars4ex = new Regex(lyricParentChars5);
            lyricParentChars5ex = new Regex(lyricParentChars4);
        }

        public List<List<MyLyricInfo>> ParseLyricString(string lyric)
        {
            int lyricposition = 0;
            var planelist = new List<MyLyricInfo>();

            // 歌詞単位で分解
            var lp = lyricParentChars2ex.Matches(lyric).GetEnumerator();
            while (lp.MoveNext())
            {
                // 歌詞に含まれる長音記号、空白、タブ、を消す
                string lyricChar = lyricParentChars3ex.Replace(lp.Current.ToString(), @"");

                if (lyricChar != "")
                {
                    // 歌詞として受け入れできない
                    if (!lyricParentChars1ex.IsMatch(lyricChar)) throw new Exception(string.Format(@"lyric Part column {0}, 歌詞として受け入れられない文字 '{1}' があります", lyricposition + 1, lyricChar));

                    MyLyricInfo mylyric = new MyLyricInfo
                    {
                        Column = lyricposition,
                        Lyric = lyricChar,
                        keyUpdown = 0
                    };

                    // キー移動指定の処理
                    if (lyricParentChars5ex.IsMatch(lyricChar))
                    {
                        mylyric.Lyric = lyricParentChars5ex.Replace(lyricChar, @"$1");

                        var keydispstr = lyricParentChars5ex.Replace(lyricChar, @"$2");
                        if (keydispstr.Length == 1) keydispstr += "1";

                        int.TryParse(keydispstr, out var keydisp);

                        mylyric.keyUpdown += keydisp; 
                    }

                    lyricposition += lp.Current.ToString().Length;

                    planelist.Add(mylyric);
                }
            }

            // 音符単位の歌詞集合に変換する
            var lyricList = AggregationLyricParts(planelist);

            return lyricList;
        }

        private List<List<MyLyricInfo>> AggregationLyricParts(List<MyLyricInfo> list)
        {
            List<List<MyLyricInfo>> parsed1stlist = new List<List<MyLyricInfo>>();

            int index = 0;

            // 分割した歌詞の集約
            for (index = 0; index < list.Count; index++)
            {
                parsed1stlist.Add(new List<MyLyricInfo>());

                if (!lyricGroupingOpen.Contains(list[index].Lyric) && !lyricGroupingClose.Contains(list[index].Lyric))
                {
                    // 開始カッコでも閉じカッコでもないので単独文字として扱う。

                    if (lyricParentChars4ex.IsMatch(list[index].Lyric))
                    {
                        // 先頭の促音文字なら適用を許す。
                        if (index == 0)
                        {
                            parsed1stlist[parsed1stlist.Count - 1].Add(list[index]);
                        }
                    }
                    else
                    {
                        parsed1stlist[parsed1stlist.Count - 1].Add(list[index]); //.LyriclyricParentChars4ex.Replace(, ""));
                    }

                }
                else if (lyricGroupingClose.Contains(list[index].Lyric))
                {
                    // 開始カッコが現れる前に閉じカッコが来た
                    throw new Exception(string.Format(@"lyric Part column {0},  閉じ括弧に対応する開き括弧がありません", list[index].Column + 1));
                }
                else if (lyricGroupingOpen.Contains(list[index].Lyric))
                {
                    // 開始カッコが来た
                    int spos = index;
                    var epos = index;

                    // 閉じカッコを検索
                    epos = list.FindIndex(spos, v => lyricGroupingClose.Contains(v.Lyric));

                    if (epos < 0)
                    {
                        // 閉じカッコが無いのでエラー
                        throw new Exception(string.Format(@"lyric Part column {0}, 開き括弧に対応する閉じ括弧がありません", list[index].Column + 1));
                    }
                    if ((spos + 1) == epos)
                    {
                        //括弧内に文字が無い
                        throw new Exception(string.Format(@"lyric Part column {0}, 開き括弧と閉じ括弧の間に歌詞がありません", list[index].Column + 1));
                    }

                    // 開き括弧と閉じ括弧の間の歌詞を処理
                    index++;
                    while (index < epos)
                    {
                        if (lyricGroupingOpen.Contains(list[index].Lyric))
                        {
                            // ネストはサポートしていない
                            throw new Exception(string.Format(@"lyric Part column {0} - {1}, {2}, 括弧のネストはサポートしていません", list[spos].Column + 1, list[epos].Column + 1, list[index].Column + 1));
                        }

                        if (lyricParentChars4ex.IsMatch(list[index].Lyric))
                        {
                            if ((spos + 1) == index)
                            {
                                // 文字列最初の促音文字は残す
                                parsed1stlist[parsed1stlist.Count - 1].Add(list[index]);
                            }
                        }
                        else
                        {
                            parsed1stlist[parsed1stlist.Count - 1].Add(list[index]);
                        }
                        index++;
                    }
                }
            }

            return parsed1stlist.Where(v=>v.Count != 0).ToList();
        }

    }
}
