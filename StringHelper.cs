/***********************************************
 * 功能：字符串处理函数
 * 目标：扩展更多的字符串处理函数
 * 版本号：V1.0;
 * 作者：Ecky Leung;
 * 立项时间：2012-1-18
 * 完成时间：
 * 最后修改时间：
 * 修改信息： 
 * 备注：
 **********************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class StringHelper
{
    public struct StringPos
    {
        public int StartPosition;
        public int NextStartPosition;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Text"></param>
    /// <param name="StartOffset"></param>
    /// <param name="EndOffset">如果为NULL或者为零，则一直搜索到最后</param>
    /// <param name="Key"></param>
    /// <returns>返回字符串的起始位置和字符结束的下一位置</returns>
    public static StringPos SearchString(ref string Text, int StartOffset, int? EndOffset, string Key)
    {
        //KeyValuePair<int, int> KeyPos = new KeyValuePair<int, int>(-1, -1);
        StringPos KeyPos = new StringPos();
        KeyPos.StartPosition = -1;
        KeyPos.NextStartPosition = -1;

        int iBeginPos = SearchString(ref Text, ref StartOffset, EndOffset, Key);
        KeyPos.StartPosition = iBeginPos;
        KeyPos.NextStartPosition = iBeginPos + Key.Length;
        return KeyPos;

    }

    public static int SearchString(ref string Text, ref int StartOffset, int? EndOffset, string Key)
    {
        int iTxtLen = Text.Length;
        int iKeyLen = Key.Length;
        int iTheSameCharCount = 0;


        if (EndOffset == null || EndOffset == 0)
        {
            EndOffset = iTxtLen;
        }

        if (EndOffset < StartOffset)
        {
            return -1;
        }

        //KeyValuePair<int, int> KeyPos = new KeyValuePair<int, int>(-1, -1);
        //StringPos KeyPos = new StringPos();
        int iBeginPos = -1;

        for (int i = StartOffset; i < EndOffset; i++)
        {
            for (int j = 0; j < iKeyLen; j++)
            {
                if (Key[j] == Text[i + j])
                {
                    iTheSameCharCount++;
                    if (iTheSameCharCount == iKeyLen)
                    {
                        //KeyPos = new KeyValuePair<int, int>(i, i + iKeyLen);
                        //KeyPos.StartPosition = i;
                        iBeginPos = i;
                        //KeyPos.NextStartPosition = i + iKeyLen;
                        StartOffset = i + iKeyLen;
                        goto Exit;
                    }
                    continue;
                }
                else
                {
                    iTheSameCharCount = 0;
                    break;
                }
            }
        }

    Exit:
        //return KeyPos;
        return iBeginPos;
    }
    public static int SearchChar(ref string text, int offset, char c)
    {
        int ret = -1;

        for (int i = offset; i < text.Length; i++)
        {
            if (text[i] == c)
            {
                return i;
            }
        }
        return ret;
    }

    public static int SearchChar(ref string text, int offset, char c, char endChar, int count)
    {
        int ret = -1;
        int iCount = 0;

        if (count == 0)
            return offset;//返回起始位置

        for (int i = offset; i < text.Length; i++)
        {
            if (iCount == count)
            {
                ret = i - 1;
                break;
            }

            if (text[i] == c)
            {
                ++iCount;
            }
            else if (text[i] == endChar)
            {
                return i;
            }
        }
        return ret;
    }

    public static string GetBoundaryRegion(ref string Text,ref int CurrentOffset,params char[] BoundaryChars)
    {
        int iCharCount = BoundaryChars.Length;
        int iTxtLen = Text.Length;

        string strRet = null;
        for (int i = CurrentOffset; i < iTxtLen; i++)
        {
            for (int j = 0; j < iCharCount; j++)
            {
                if (BoundaryChars[j] == Text[i])
                {
                    strRet = Text.Substring(CurrentOffset, i - CurrentOffset);
                    CurrentOffset = i;
                    break;
                }
            }
        }
        return strRet;
    }
    /// <summary>
    /// 标记直入字符串搜索
    /// </summary>
    /// <param name="Text"></param>
    /// <param name="StartOffset"></param>
    /// <param name="EndOffset"></param>
    /// <param name="ExpectedChars"></param>
    /// <param name="Compatible"></param>
    /// <returns></returns>
    public static bool TestExpectedChar(ref string Text, ref int StartOffset, int? EndOffset, char CompatibleChar, params char[] ExpectedChars)
    {
        int iTxtLen = Text.Length;
        int iExpCharsCount = ExpectedChars.Length;
        bool bRet = false;


        if (EndOffset < StartOffset)
        {
            return false;
        }

        if (EndOffset == null || EndOffset == 0)
        {
            EndOffset = iTxtLen - 1;
        }

        for (int i = StartOffset; i <= EndOffset; i++)
        {
            if (Text[i] == CompatibleChar)//如果等于兼容字符
            {
                continue;
            }
            else
            {
                for (int j = iExpCharsCount - 1; j >= 0; j--)
                {
                    if (Text[i] == ExpectedChars[j])//如果找到期待字符，返回true,否则判断是否符合下一个字串期待字符，直到最后一个字
                    {
                        StartOffset = i + j - 1;//最后停留字符所在的位置的前一个位置
                        return true;
                    }
                }
                return bRet;
            }
        }
        return false;
    }
    public static bool TestUnexpectedChar()
    {
        return false;
    }
    public static bool IsNumber(char TestChar)
    {
        bool bRet = false;
        int iCharCount = NumberTable.Length;
        for (int i = 0; i < iCharCount; i++)
        {
            if (NumberTable[i] == TestChar)
            {
                bRet = true;
                break;
            }
        }
        return bRet;
    }
    public static bool IsAlpha(char TestChar)
    {
        int iAlphaCount = AlphaTable.Length;

        bool bRet = false;
        for (int i = 0; i < iAlphaCount; i++)
        {
            if (AlphaTable[i] == TestChar)
            {
                bRet = true;
                break;
            }
        }
        return bRet;
    }
    public static bool IsLegalVariantName(string VariantName)
    {
        int iNameLen = VariantName.Length;
        bool bRet = true;

        //判断第一个字符是否为字母或下划线，如果不是，则为不合法变量名
        if (!(IsAlpha(VariantName[0]) || VariantName[0] == '_'))
        {
            return false;
        }


        for (int i = 1; i < iNameLen; i++)
        {
            if(IsNumber(VariantName[i]) || IsAlpha(VariantName[i]) || VariantName[i]=='_')
            {
                continue;
            }
            else
            {
                bRet = false;
                break;
            }
        }
        return bRet;
    }

    public static string GetStringBetween(ref string OrignalString,string BeginTag,string EndTag)
    {
        int iBeginTagLen = BeginTag.Length,iBeginTagPos,iEndTagPos;
        string strRet = null;

        iBeginTagPos = OrignalString.IndexOf(BeginTag);
        if (iBeginTagPos == -1) return strRet;

        iEndTagPos = OrignalString.IndexOf(EndTag);
        if (iEndTagPos == -1) return strRet;

        if (iEndTagPos < iBeginTagPos) return strRet;
        strRet = OrignalString.Substring(iBeginTagPos + iBeginTagLen, iEndTagPos - iBeginTagPos - iBeginTagLen);
        return strRet;
    }

    static char[] AlphaTable = new char[]
        {
            'A',
            'B',
            'C',
            'D',
            'E',
            'F',
            'G',
            'H',
            'I',
            'J',
            'K',
            'L',
            'M',
            'N',
            'O',
            'P',
            'Q',
            'R',
            'S',
            'T',
            'U',
            'V',
            'W',
            'X',
            'Y',
            'Z',
            'a',
            'b',
            'c',
            'd',
            'e',
            'f',
            'g',
            'h',
            'i',
            'j',
            'k',
            'l',
            'm',
            'n',
            'o',
            'p',
            'q',
            'r',
            's',
            't',
            'u',
            'v',
            'w',
            'x',
            'y',
            'z'
        };

    static char[] NumberTable = new char[] 
        {
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9'
        };
}