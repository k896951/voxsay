using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace voxsay
{
    public class LyricParser
    {
        private const string lyricParentChars1 =
                                   @"[あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
                                   @"アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
                                   @"がぎぐげござじずせぞだぢづでどばびぶべぼ" +
                                   @"ガギグゲゴザジズセゾダヂヅデドバビブベボ" +
                                   @"ぱぴぷぺぽ" +
                                   @"パピプペポ" +
                                   @"ぁぃぅぇぉゃゅょァィゥェォャュョーっッ" +
                                   @"\(（\)）" +
                                   @" 　\t]";          // タブ + 空白(半角 + 全角)

        private const string lyricParentChars2 =
                                   @"[きぎ][ぇゃゅょ]ー{0,1}|" +
                                   @"[キギ][ェャュョ]ー{0,1}|" +
                                   @"[しじ][ぇゃゅょ]ー{0,1}|" +
                                   @"[シジ][ェャュョ]ー{0,1}|" +
                                   @"[すず][ぃ]ー{0,1}|" +
                                   @"[スズ][ィ]ー{0,1}|" +
                                   @"[ちぢ][ぇゃゅょ]ー{0,1}|" +
                                   @"[チヂ][ェャュョ]ー{0,1}|" +
                                   @"[つ][ぁぃぇぉ]ー{0,1}|" +
                                   @"[ツ][ァィェォ]ー{0,1}|" +
                                   @"[てで][ぃ]ー{0,1}|" +
                                   @"[テデ][ィ]ー{0,1}|" +
                                   @"[と][ぅ]ー{0,1}|" +
                                   @"[ト][ゥ]ー{0,1}|" +
                                   @"[に][ぇゃゅょ]ー{0,1}|" +
                                   @"[ニ][ェャュョ]ー{0,1}|" +
                                   @"[ひびぴ][ぇゃゅょ]ー{0,1}|" +
                                   @"[ヒビピ][ェャュョ]ー{0,1}|" +
                                   @"[ふ][ぁぃぇぉ]ー{0,1}|" +
                                   @"[フ][ァィェォ]ー{0,1}|" +
                                   @"[み][ぇゃゅょ]ー{0,1}|" +
                                   @"[ミ][ェャュョ]ー{0,1}|" +
                                   @"[り][ぇ]ー{0,1}|" +
                                   @"[リ][ェ]ー{0,1}|" +
                                   @".";

        private const string lyricParentChars3 = @"[ーっッ 　\t]"; // タブ + 空白(半角 + 全角)含む

        private const string lyricGroupingOpen = @"（(";
        private const string lyricGroupingClose = @")）";

        private Regex lyricParentChars1ex;
        private Regex lyricParentChars2ex;
        private Regex lyricParentChars3ex;

        public LyricParser()
        {
            lyricParentChars1ex = new Regex(lyricParentChars1);
            lyricParentChars2ex = new Regex(lyricParentChars2);
            lyricParentChars3ex = new Regex(lyricParentChars3);
        }

        public List<List<string>> ParseLyricString(string lyric)
        {
            int lyricposition = 0;
            var planelist = new List<MyLyricInfo>();

            // 文字単位で分解
            var lp = lyricParentChars2ex.Matches(lyric).GetEnumerator();
            while (lp.MoveNext())
            {
                string lyricChar = lyricParentChars3ex.Replace(lp.Current.ToString(), @"");

                if (lyricChar != "")
                {
                    if(!lyricParentChars1ex.IsMatch(lyricChar))
                    {
                        // 歌詞として受け入れできない
                        throw new Exception(string.Format(@"lyric Part column {0}, 歌詞として受け入れられない文字　'{1}' があります", lyricposition + 1, lyricChar));
                    }
                    MyLyricInfo mylyric = new MyLyricInfo
                    {
                        Column = lyricposition,
                        Lyric = lyricChar
                    };
                    lyricposition += lp.Current.ToString().Length;

                    planelist.Add(mylyric);
                }
            }

            // 音符単位の歌詞集合に変換する
            var lyricList = AggregationLyricParts(planelist);

            return lyricList;
        }

        private List<List<string>> AggregationLyricParts(List<MyLyricInfo> list)
        {
            List<List<string>> parsedlist = new List<List<string>>();

            int index = 0;

            // 分割した歌詞の集約
            for (index = 0; index < list.Count; index++)
            {
                parsedlist.Add(new List<string>());

                if (!lyricGroupingOpen.Contains(list[index].Lyric) && !lyricGroupingClose.Contains(list[index].Lyric))
                {
                    // 開始カッコでも閉じカッコでもないので単独文字として扱う
                    parsedlist[parsedlist.Count - 1].Add(list[index].Lyric);
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
                    if (epos == (spos + 1))
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

                        parsedlist[parsedlist.Count - 1].Add(list[index].Lyric);
                        index++;
                    }
                }
            }

            return parsedlist;
        }

    }
}
