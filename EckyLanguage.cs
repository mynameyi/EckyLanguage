/***********************************************
 * 功能：通用交互语言（脚本语言、记录语言、配置语言）
 * 目标：构建通用的交互语言
 * 版本号：V2.0;
 * 作者：Ecky Leung;
 * 立项时间：2011-8-1
 * 完成时间：
 * 最后修改时间：2011-11-24
 * 修改信息： 
 * 1.将原有UsingStandardRecordLanguage属性改为UsingDefaultRecordLanguage
 * 2.
 * 备注：
 * 1.完善错误处理机制
 * 2.加入转义字符的支持(2011-12-14)
 * 3.增加动态设置关键字功能
 * 4.增加标准注释：记录体元素注释，记录体注释
 * 5.增加动态引用程序集的支持
 * 6.增加公共参数的定义
 * 7.增加记录语言的选择功能
 * 8.增加记录插入函数Insert()
 * 9.修复Config类ReadString末尾搜索无换行符的错误判断缺陷  2016-8-31
 * 10.增加自动创建功能文件功能，完善文件保存功能 2017-4-1
 * 11.将Record1_0分离出去   2017-11-1
 **********************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EckyLanguage
{
    public class Record
    {
        private System.IO.FileStream fs;
        #region ----------------------记录关键字-----------------------
        char _EscapeCharacter = '\\';
        char _RecordStartSymbol = '[';
        char _RecordTerminator = ']';
        char _RecordSeparator = ',';
        /// <summary>
        /// 记录起始符
        /// </summary>
        public char RecordStartSymbol
        {
            get 
            {
                return _RecordStartSymbol;
            }
            set 
            {
                _RecordStartSymbol = value;
            }
        }
        /// <summary>
        /// 记录结束符
        /// </summary>
        public char RecordTerminator
        {
            get
            { 
                return _RecordTerminator;
            }
            set 
            { 
                _RecordTerminator = value;
            }
        }
        /// <summary>
        /// 记录分隔符
        /// </summary>
        public char RecordSeparator
        {
            set 
            {
                _RecordSeparator = value;
            }
            get 
            { 
                return _RecordSeparator; 
            }
        }
        /// <summary>
        /// 转义字符
        /// </summary>
        public char EscapeCharacter
        {
            get 
            {
                return _EscapeCharacter; 
            }
            set 
            { 
                _EscapeCharacter = value;
            }
        }
        #endregion
        bool _UsingDefaultRecordLanguage = true;
        /// <summary>
        /// 使用标准记录语言，所谓标准记录语言，即为默认的记录语法，主要包括
        /// 记录体符号[]，记录分隔符 ";"
        /// 子记录体符号(),分隔符号 “,”； 
        /// 转义字符为反斜杠  “\”；
        /// </summary>
        public bool UsingDefaultRecordLanguage
        {
            get 
            {
                return _UsingDefaultRecordLanguage;
            }
            set
            {
                _UsingDefaultRecordLanguage = value;
            }
        }
        public RecordLanguageType RecordLanguage
        {
            set 
            {
                switch (value)
                {
                    case RecordLanguageType.StandardRecordLanguage0:
                        _RecordStartSymbol = '[';
                        _RecordTerminator = ']';
                        _RecordSeparator = ';';
                        _EscapeCharacter = '\\';
                        break;
                    case RecordLanguageType.StandardRecordLanguage1:
                        _RecordStartSymbol = '$';
                        _RecordTerminator = '$';
                        _RecordSeparator = '|';
                        _EscapeCharacter = '!';
                        break;
                    case RecordLanguageType.StandardRecordLanguage2:
                        break;
                    default:
                        break;
                }
            }
        }
        public enum RecordLanguageType
        {
            StandardRecordLanguage0 = 0,
            StandardRecordLanguage1 = 1,
            StandardRecordLanguage2 = 2,
        }
        private Encoding TextEncode;
        private string TextBody;
        public Record(string FileName)
        {
            //--------------------------------------------------------------------------------
            try
            {
                fs = new System.IO.FileStream(FileName, System.IO.FileMode.OpenOrCreate);
            }
            catch
            {
                throw new Exception("打开记录文件失败");
            }
            //br = new System.IO.BinaryReader(fs);
            //BOM头，Byte Order Mark,识别文件的编码格式，其选择合适编码打开文件
            byte[] BOM= new byte[3];
            fs.Read(BOM, 0, 3);
            if (BOM[0] == 255 && BOM[1] == 254)//BOM:FF FE
            {
                TextEncode = Encoding.Unicode;
            }
            else if (BOM[0] == 254 && BOM[1] == 255)//FE FF
            {
                TextEncode = Encoding.BigEndianUnicode;
            }
            else if (BOM[0] == 239 && BOM[1] == 187 && BOM[2] == 191)//EF BB BF
            {
                TextEncode = Encoding.UTF8;
            }
            else
            {
                TextEncode = Encoding.Default;//ANSI编码
            }
            fs.Seek(0, System.IO.SeekOrigin.Begin);//移到文件开始处

            int FileSize = (int)fs.Length;
            byte[] FileBuffer = new byte[FileSize];
            int Count = fs.Read(FileBuffer, 0, FileSize);

            TextBody = TextEncode.GetString(FileBuffer);
        }
      
        List<EnumeratorAttributes> EnumeratorList = new List<EnumeratorAttributes>();
        object objLock = 1;
        class EnumeratorAttributes
        {
            public int EnumeratorId;
            public int EnumeratorOffset;
        }
        public string[] EnumRecord()
        {
            EnumeratorAttributes CurrentEnumerator = null;
            int EnumeratorId = System.AppDomain.GetCurrentThreadId();//以线程ID作为枚举器ID。
            //int EnumeratorId = System.Threading.Thread.ManagedThreadId;//ManagedThreadId是实例成员
            //int EnumeratorId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            foreach (EnumeratorAttributes ea in EnumeratorList)
            {
                if (ea.EnumeratorId == EnumeratorId)
                {
                    CurrentEnumerator = ea;
                    break;
                }
            }
            if (CurrentEnumerator == null)
            {
                CurrentEnumerator = new EnumeratorAttributes();
                CurrentEnumerator.EnumeratorId = EnumeratorId;
                CurrentEnumerator.EnumeratorOffset = 0;

                lock (objLock)
                {
                    EnumeratorList.Add(CurrentEnumerator);
                }
            }
            string[] strResult = ReadRecord(ref CurrentEnumerator.EnumeratorOffset);
            if (strResult == null)
            {
                lock (objLock)
                {
                    foreach (EnumeratorAttributes ea in EnumeratorList)
                    {
                        if (ea.EnumeratorId == EnumeratorId)
                        {
                            EnumeratorList.Remove(ea);
                            break;
                        }
                    }
                }
            }
            return strResult;
        }
        /// <summary>
        /// 手动终止枚举器，当枚举没有完成之前需要终止枚举，调用该方法
        /// </summary>
        public void EndEnum()
        {
            int EnumeratorId = System.AppDomain.GetCurrentThreadId();//以线程ID为枚举器ID。
            lock (objLock)
            {
                foreach (EnumeratorAttributes ea in EnumeratorList)
                {
                    if (ea.EnumeratorId == EnumeratorId)
                    {
                        EnumeratorList.Remove(ea);
                        break;
                    }
                }
            }
        }
        object objLockAdd = 1;
        public bool Add(params string[] RecordElements)
        {
            StringBuilder sbRecord = new StringBuilder(_RecordStartSymbol.ToString());
            foreach (string s in RecordElements)
            {
                sbRecord.Append(s + ',');
            }

            sbRecord[sbRecord.Length - 1] = _RecordTerminator;//替换最后的逗号为记录终止符号
            sbRecord.Append("\r\n");
            string r =  sbRecord.ToString();
            byte[] b = TextEncode.GetBytes(r);

            lock (objLockAdd)
            {
                fs.Seek(0, System.IO.SeekOrigin.End);
                fs.Write(b, 0, b.Length);
                fs.Flush();//更新内容到文件

                TextBody += r;
            }
            
            return true;
        }
        /// <summary>
        /// 从指定偏移量位置开始读取一笔记录
        /// </summary>
        /// <param name="Offset">偏移量</param>
        /// <returns>记录体</returns>
        public string[] ReadRecord(ref int Offset)
        {
            string[] RecordElements = null;
            int Begin = -1, End = -1;
            for (int i = Offset; i < TextBody.Length; i++)
            {
                //转义处理
                if (TextBody[i] == _EscapeCharacter)
                {
                    i += 1;//跳过转义字符
                    continue;
                }
                if (TextBody[i] == _RecordStartSymbol && Begin==-1)
                {
                    Begin = i;
                    continue;
                }
                if (TextBody[i] == _RecordTerminator)
                {
                    End = i;
                    break;
                }
            }

            if (Begin == -1 || End == -1)//只要开始和结束其中之一出现异常，即返回null。
            {
                return null;
            }

            Offset = End + 1;
            string RecordBody = TextBody.Substring(Begin + 1, End - Begin -1);

            //处理转义字符
            StringBuilder sb = new StringBuilder(RecordBody);
            for (int i = 0; i < RecordBody.Length; i++)
            {
                if (RecordBody[i] == _EscapeCharacter)
                {
                    string strUnicodeNo;
                    string strEscape;
                    i++;//移动到下一位
                    switch(RecordBody[i])
                    {
                        case 'r':
                            strUnicodeNo = string.Format(new UnicodeCharStringFormat(), "\x1B{0:D}\x1B", '\r');
                            strEscape = "\\r";
                            break;
                        case 'n':
                            strUnicodeNo = string.Format(new UnicodeCharStringFormat(), "\x1B{0:D}\x1B", '\n');
                            strEscape = "\\n";
                            break;
                        case 't':
                            strUnicodeNo = string.Format(new UnicodeCharStringFormat(), "\x1B{0:D}\x1B", '\t');
                            strEscape = "\\t";
                            break;
                        default:
                            strUnicodeNo= string.Format(new UnicodeCharStringFormat(), "\x1B{0:D}\x1B", RecordBody[i]);
                            strEscape = "\\" + RecordBody[i];
                            break;
                    }
                    sb.Replace(strEscape, strUnicodeNo, 0, sb.Length);//替换所有相同的转义字串
                }
            }

            RecordBody = sb.ToString();
            RecordElements = RecordBody.Split(_RecordSeparator);//按记录分隔符分离

            int iElementCount = RecordElements.Length;
            for (int i = 0; i < iElementCount; i++)
            {
                sb = new StringBuilder(RecordElements[i]);

                int iBeginPos = 0,iEndPos = 0;
                while ((iBeginPos = RecordElements[i].IndexOf('\x1B',iBeginPos))!=-1)
                {
                    iEndPos = RecordElements[i].IndexOf('\x1B', iBeginPos + 1);
                    string strUnicodeChar = RecordElements[i].Substring(iBeginPos + 1, iEndPos - iBeginPos - 1);

                    string strTemp = Convert.ToChar(int.Parse(strUnicodeChar)).ToString();

                    sb.Replace("\x1B" + strUnicodeChar + "\x1B", strTemp);

                    iBeginPos = iEndPos + 1;
                    if (iBeginPos == RecordElements[i].Length)
                    {
                        break;
                    }          
                }
                RecordElements[i] = sb.ToString();
            }

            return RecordElements;
        }

        public void Dispose()
        {
            fs.Close();
            fs.Dispose();//关闭文件
            //this.Dispose();
        }
        public static string[] SplitSubrecord(string SubrecordBody, out string SubrecordName, char? SubrecordSeparator, char? SubrecordStartCharacter, char? SubrecordTerminator)
        {
            char _SubrecordSeparator,_SubrecordStartCharacter,_SubrecordTerminator;

            _SubrecordSeparator = SubrecordSeparator == null?',' : (char) SubrecordSeparator;
            _SubrecordStartCharacter = SubrecordStartCharacter == null? '(':(char) SubrecordStartCharacter;
            _SubrecordTerminator = SubrecordTerminator == null? ')':(char)SubrecordTerminator;

            SubrecordName = "";
            int iPos = 0;
            iPos = SubrecordBody.IndexOf(_SubrecordStartCharacter);
            if (iPos > 0)
            {
                SubrecordName = SubrecordBody.Substring(0, iPos);

                SubrecordBody = SubrecordBody.Substring(iPos + 1);
                SubrecordBody = SubrecordBody.Remove(SubrecordBody.Length - 1);
            }

            return SubrecordBody.Split(_SubrecordSeparator);
        }

        public bool SaveAs(string FileName)
        {
            try
            {
                System.IO.FileStream fsNew = new System.IO.FileStream(FileName, System.IO.FileMode.CreateNew);
                byte[] NewBytes = TextEncode.GetBytes(TextBody);
                fsNew.Write(NewBytes, 0, NewBytes.Length);
                fsNew.Flush();
            }
            catch
            {
                return false;
            }
            return true ;
        }

        public void Clear()
        {
            ;
        }
    }
    public class Config
    {
        System.IO.FileStream fs = null;
        System.IO.BinaryReader br;
        System.Text.Encoding FileEncode;
        string FileBody;
        string CurrentFileName;
        //List<KeywordAttributes> KeywordList = new List<KeywordAttributes>();

        //int ForeBorderCharacterPos;//前边界符位置
        //int BackBorderCharacterPos;//后边界符位置

        public Config(string FileName)
        {
            fs = new System.IO.FileStream(FileName, System.IO.FileMode.OpenOrCreate);
            //br = new System.IO.BinaryReader(fs);
            byte[] BOM = new byte[3];
            fs.Read(BOM, 0, 3);
            if (BOM[0] == 255 && BOM[1] == 254)//BOM:FF FE
            {
                FileEncode = Encoding.Unicode;
            }
            else if (BOM[0] == 254 && BOM[1] == 255)//FE FF
            {
                FileEncode = Encoding.BigEndianUnicode;
            }
            else if (BOM[0] == 239 && BOM[1] == 187 && BOM[2] == 191)//EF BB BF
            {
                FileEncode = Encoding.UTF8;
            }
            else
            {
                FileEncode = Encoding.Default;//ANSI编码
            }
            fs.Seek(0, System.IO.SeekOrigin.Begin);//移到文件开始处

            int FileSize = (int)fs.Length;
            byte[] FileBuffer = new byte[FileSize];
            int Count = fs.Read(FileBuffer, 0, FileSize);
            FileBody = FileEncode.GetString(FileBuffer);
            //fs.Seek(0, System.IO.SeekOrigin.Begin);//移到文件开始处
            //br = new System.IO.BinaryReader(fs, FileEncode);
            //while (true)
            //{
            //    char c = br.ReadChar();
            //    fs.Position = 3;
            //    char[] ca = br.ReadChars(5);
                
            //    //string s = br.ReadString();
            //}
            CurrentFileName = FileName;
        }
        public void Dispose()
        {
            fs.Close();
            fs.Dispose();
            fs = null;
        }
        public string ReadVariant(string SectionName, string VariantName,string DefaultValue)
        {
            return DefaultValue;
        }
        public string ReadString(string SectionName, string VariantName, string DefaultValue)
        {

            if (fs == null)
            {
                fs = new System.IO.FileStream(CurrentFileName, System.IO.FileMode.Open);
            }

            int fileLen = FileBody.Length;

            int SectionBeginPos = -1;
            int SectionEndPos = -1;
            int ForeBorderCharacterPos = -1;//前边界符位置
            int BackBorderCharacterPos = -1;//后边界符位置

            string _SectionName;
            string _VariantName;
            string _Value;

            if (SectionName == null)
            {
                ;
            }
            for (int i = 0; i < fileLen; i++)
            {
                if (FileBody[i] == '[')
                {
                    SectionBeginPos = i;
                }
                if (FileBody[i] == ']')
                {
                    SectionEndPos = i;
                    if (SectionBeginPos != -1 && SectionEndPos != -1)
                    {
                        _SectionName = FileBody.Substring(SectionBeginPos+1,SectionEndPos - SectionBeginPos -1);
                        if (_SectionName.Trim().Equals(SectionName))//当找到节点之后
                        {
                            for (int j = i; j < fileLen; j++)
                            {
                                switch (FileBody[j])
                                {
                                    case '\n':
                                        if (ForeBorderCharacterPos == -1)
                                        {
                                            ForeBorderCharacterPos = j;
                                        }
                                        else
                                        {
                                            BackBorderCharacterPos = j;
                                        }
                                        break;
                                    case ' ':
                                        if (ForeBorderCharacterPos == -1)
                                        {
                                            ForeBorderCharacterPos = j;
                                        }
                                        else
                                        {
                                            BackBorderCharacterPos = j;
                                        }
                                        break;
                                    case '=':
                                        if (ForeBorderCharacterPos == -1)
                                        {
                                            ForeBorderCharacterPos = j;
                                        }
                                        else
                                        {
                                            BackBorderCharacterPos = j;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                if (ForeBorderCharacterPos != -1 && BackBorderCharacterPos != -1)
                                {
                                    _VariantName = FileBody.Substring(ForeBorderCharacterPos + 1, BackBorderCharacterPos - ForeBorderCharacterPos - 1);

                                    int ValueTagPos = -1;
                                    if (_VariantName.Trim().Equals(VariantName))//找到属性名之后
                                    {
                                        for (int k = j; k < fileLen; k++)
                                        {
                                            if (FileBody[k] == '=')
                                            {
                                                ValueTagPos = k;
                                            }
                                            else if (FileBody[k] == '\n')
                                            {
                                                _Value = FileBody.Substring(ValueTagPos + 1, k - ValueTagPos - 1).Trim();
                                                return _Value;
                                            }
                                            else if (k == (fileLen - 1)) {
                                                _Value = FileBody.Substring(ValueTagPos + 1, k - ValueTagPos).Trim();
                                                return _Value;
                                            }
                                        }
                                    }
                                    ForeBorderCharacterPos = BackBorderCharacterPos;
                                    BackBorderCharacterPos = -1;
                                }
                            }
                        }
                    }
                }
            }
            Exit:
            return DefaultValue;
        }
        public bool WriteString(string SectionName, string KeyName,string KeyValue)
        {
            //SectionName = SectionName.TrimStart('[');
            //SectionName = SectionName.TrimEnd(']');

            StringPos SectionPos;
            StringPos NextSectionPos;
            StringPos KeyPos;
            StringPos EqualCharacterPos;

            if (SectionName == null)
            {
                ;
            }

            SectionName = '[' + SectionName + ']';
            SectionPos = FindString(ref FileBody, 0, 0, SectionName);

            if (SectionPos.StartPosition == -1)//如果没有找到节名
            {
                FileBody += SectionName + "\r\n";
                FileBody += KeyName + " = " + KeyValue + "\r\n";
                goto Exit;
            }

            NextSectionPos = FindString(ref FileBody, SectionPos.NextStartPosition, null, "[");
            if (NextSectionPos.StartPosition == -1)//如果节位于最后，则
            {
                KeyPos = FindString(ref FileBody, SectionPos.NextStartPosition, null, KeyName);//寻找关键字
                if (KeyPos.StartPosition == -1)
                {
                    FileBody += KeyName + " = " + KeyValue + "\r\n";
                    goto Exit;
                }
                else
                {
                    EqualCharacterPos = FindString(ref FileBody, KeyPos.NextStartPosition, null, "=");
                }
            }
            else
            {
                KeyPos = FindString(ref FileBody,SectionPos.NextStartPosition,NextSectionPos.StartPosition, KeyName);
                if (KeyPos.StartPosition == -1)
                {
                    FileBody = FileBody.Insert(NextSectionPos.StartPosition,KeyName +" = " + KeyValue + "\r\n");//插入数据
                    goto Exit;
                }
                EqualCharacterPos = FindString(ref FileBody, KeyPos.NextStartPosition, NextSectionPos.StartPosition, "=");
            }
    
            string OriginalKeyName;
            string strKeyValuePair = KeyName + " = " + KeyValue + "\r\n";

            int iValueEndPos;
            StringPos EndPos; 
            if (EqualCharacterPos.StartPosition == -1)
            {
                FileBody = FileBody.Insert(NextSectionPos.StartPosition, strKeyValuePair);
            }
            else
            {
                OriginalKeyName = FileBody.Substring(KeyPos.StartPosition, EqualCharacterPos.StartPosition - KeyPos.StartPosition);
                OriginalKeyName = OriginalKeyName.Trim();
                if (KeyName == OriginalKeyName)
                {
                    EndPos = FindString(ref FileBody, EqualCharacterPos.NextStartPosition, null, "\r\n");
                    iValueEndPos = EndPos.StartPosition - EqualCharacterPos.NextStartPosition;
                    if (EndPos.StartPosition == -1)
                    {
                        FileBody = FileBody.Remove(EqualCharacterPos.NextStartPosition);
                    }
                    else
                    {
                        FileBody = FileBody.Remove(EqualCharacterPos.NextStartPosition, iValueEndPos);
                    }

                    FileBody = FileBody.Insert(EqualCharacterPos.NextStartPosition, ' ' + KeyValue);//插入数据
                }
                else
                {
                    FileBody = FileBody.Insert(NextSectionPos.StartPosition,strKeyValuePair);
                }
            }
            Exit:

            if (fs == null)
            {
                fs = new System.IO.FileStream(CurrentFileName, System.IO.FileMode.Open);
            }

            fs.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] bytes = FileEncode.GetBytes(FileBody);
            fs.SetLength(bytes.Length);
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush();
            return true;
        }
        public List<string> GetSections()
        {
            return new List<string>();
        }
        public List<KeyValuePair<string,string>> GetKeyValuePair(string SectionName)
        {
            return new List<KeyValuePair<string, string>>();
        }


        struct StringPos
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
        private StringPos FindString(ref string Text,int StartOffset,int? EndOffset,string Key)
        {
            int iTxtLen = Text.Length;
            int iKeyLen = Key.Length;
            int iTheSameCharCount = 0; 

            //KeyValuePair<int, int> KeyPos = new KeyValuePair<int, int>(-1, -1);
            StringPos KeyPos = new StringPos();
            KeyPos.StartPosition = -1;
            KeyPos.NextStartPosition = -1;

            //if (StartOffset > EndOffset)
            //{
            //    goto Exit;
            //}
            if (EndOffset == null || EndOffset == 0)
            {
                EndOffset = iTxtLen;
            }

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
                            KeyPos.StartPosition = i;
                            KeyPos.NextStartPosition = i + iKeyLen;
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
            return KeyPos;
        }
    }
    public class Action
    {
        #region----原来的关键字定义方式
        //KeywordAttributes[] Keywords = new KeywordAttributes[]
        //{
        //    //指令体开始符号
        //    new KeywordAttributes()
        //    {
        //        KeywordName = "OrderBodyBegin",
        //        Keyword = '{'
        //    },
        //    //指令体结束符号
        //    new KeywordAttributes
        //    {
        //        KeywordName = "OrderBodyEnd",
        //        Keyword = '}'
        //    },
        //    //赋值符号
        //    new KeywordAttributes
        //    {
        //        KeywordName = "AssignmentSymbol",//赋值符号
        //        Keyword = '='
        //    },
        //    //参数分隔符
        //    new KeywordAttributes
        //    {
        //        KeywordName = "",
        //        Keyword = ','
        //    }
        //};
        //KeywordAttributes[] Keys = 
        //{
        //    new KeywordAttributes
        //    {
        //        KeywordName = "OrderBegin",
        //        Keyword = '{'
        //    }
        //};
        #endregion

        bool _IsIgnoreCase = true;//函数是否忽略大小写，默认为不区分大小写
        bool OnErrorResumeNextSwither = false;//遇到错误是否继续执行，默认为不执行，即抛出异常
        List<OrderInfo> OrderList = new List<OrderInfo>();
        KeywordSetClass KeySet = new KeywordSetClass();
        System.Reflection.Assembly Asm;
        public Action()
        {
        }
        public bool LoadOrderLibrary(string AssemblyName)
        {
            Asm = System.Reflection.Assembly.Load(AssemblyName);
            Type[] DataTypes = Asm.GetTypes();
            foreach (var item in DataTypes)
            {
                if (item.IsClass && item.IsPublic)
                {
                    AddToOrderList(item, null);
                }
            }
            return true;
        }
        public bool LoadOrderLibrary(string AssemblyName, string ClassName)
        {
            Asm = System.Reflection.Assembly.Load(AssemblyName);
            Type t = Asm.GetType(ClassName);
            AddToOrderList(t, null);
            return true;
        }
        public bool LoadOrderLibrary(object ClassInstance)
        {
            AddToOrderList(ClassInstance.GetType(), ClassInstance);
            return true;
        }
        public bool LoadOrderLibrary(Type t)
        {
            AddToOrderList(t, null);
            return true;
        }
        public void Execute(string FileNameOrScriptString)
        {
            List<VariantInfo> VariantList = new List<VariantInfo>();
            System.IO.FileStream fs;
            string FileBody;

            //根据编码加载脚本文件
            if (System.IO.File.Exists(FileNameOrScriptString))//如果存在相应文件名，则打开相关文件，否则直接当作脚本代码来执行
            {
                try
                {
                    fs = new System.IO.FileStream(FileNameOrScriptString, System.IO.FileMode.Open);
                }
                catch
                {
                    return;
                }
                Encoding FileCoding = CommonFunction.GetFileCoding(ref fs);

                int FileSize = (int)fs.Length;
                byte[] FileBuffer = new byte[FileSize];
                int Count = fs.Read(FileBuffer, 0, FileSize);
                FileBody = FileCoding.GetString(FileBuffer);
            }
            else
            {
                FileBody = FileNameOrScriptString;
            }

            //int ScriptOffset = -1;
            int LastSeparatorOffset = -1;
            //int CurrentSeparatorOffset = -1;

            bool IsExistAssignment = false;//指示当前是否存在赋值符号，用于指令体执行完毕之后，指示返回值是否需要保存到变量上
            bool IsExistDeclaration = false;//指示是否存在变量声明符号，如果是，用于创建相应变量，否则，用于设置脚本的关键字符号
            //bool IsInOrderDomain = false;//指示是否进入指令体域名
            string HeapString = "";//字符堆变量，用于累积临界符（包括换行、关键字符号等）与临界符号之间的非关键字（或称保留字）
            System.Collections.Queue HeapStringQueue = new System.Collections.Queue(10, 2);
            System.Collections.Stack OrderDomainStack = new System.Collections.Stack(20);//指令体域栈

            string ExpectedChars = "*";//期待字符

            //解析文本
            int iScriptLength = FileBody.Length;
            for (int i = 0; i < iScriptLength; i++)
            {
                if (FileBody[i] == ' ' || FileBody[i] == '\n' || FileBody[i] == '\r')
                {
                    //LastSeparatorOffset = i;
                    if (OrderDomainStack.Count > 0)//在指令体内，空格不看做是临界符号
                    {
                        if (FileBody[i] == ' ')
                        {
                            HeapString += FileBody[i];
                        }
                    }
                    else
                    {
                        HeapString = "";//语句临界符号将 HeapString 的清空
                    }
                    continue;
                }
                else if (FileBody[i] == KeySet["OrderStartSymbol"])
                {
                    OrderDomainStack.Push(i);//将指令de开始符号压栈
                }
                else if (FileBody[i] == KeySet.OrderNameSeparator)
                {
                    HeapStringQueue.Enqueue(HeapString);
                    HeapString = "";
                }
                else if (FileBody[i] == KeySet["OrderTerminator"])
                {
                    int? iStartPos = (int)OrderDomainStack.Pop();//跳出上一层领域

                    if (iStartPos == null)
                    {
                        throw new Exception("期待指令开始符号'" + KeySet.OrderStartSymbol + "'");
                    }
                    if (OrderDomainStack.Count == 0)//当返回最外一层领域
                    {
                        HeapStringQueue.Enqueue(HeapString);
                        HeapString = "";

                        if (HeapStringQueue.Count > 0)
                        {
                            string strOrderName = (string)HeapStringQueue.Dequeue();//指令名称走出队列

                            int iArgusCount = HeapStringQueue.Count;
                            object[] objArguments = new object[iArgusCount];//定义参数数组存放所有的参数

                            for (int p = 0; p < iArgusCount; p++)
                            {
                                objArguments[p] = HeapStringQueue.Dequeue();//将指令名称和参数出队列
                            }
                        }
                    }
                    else
                    {
                        HeapString += FileBody[i];//保存到堆上暂存
                    }
                }
                else if (FileBody[i] == KeySet["AssignmentSymbol"])
                {
                    //CurrentSeparatorOffset = i;
                    IsExistAssignment = true;
                    if (IsExistDeclaration)
                    {
                        ;
                    }
                    IsExistDeclaration = false;
                }
                else if (FileBody[i] == KeySet["ArgumentSeparator"])
                {
                    HeapStringQueue.Enqueue(HeapString);//将参数移入dao列
                    HeapString = "";//清空HeapString
                }
                else if (FileBody[i] == KeySet.VariantDeclarationSymbol)
                {
                    IsExistDeclaration = true;
                }
                else
                {
                    HeapString += FileBody[i];
                }
            }
        }
        public bool RegisterOrder()
        {
            return true;
        }
        private void AddToOrderList(Type t,object obj)
        {
            if (obj == null)
            {
                //obj = Activator.GetObject(t, "http://www.baidu.com");
                //获取无参数构造函数，如果没有无参数构造函数，则无法加载；
                Type[] tArgs = new Type[0]; 
                if (t.GetConstructor(tArgs) != null)
                {
                    obj = Activator.CreateInstance(t);
                }
                else
                {
                    //throw new Exception("类必须拥有无参数的构造函数");
                    System.Diagnostics.Debug.WriteLine("注意:无法实例化 " + t.Name + " 类该类必须拥有无参数的构造函数");
                    //return;
                }
                //obj = Activator.CreateInstance(
            }
            System.Reflection.MethodInfo[] mis = t.GetMethods(); 
            foreach (var mi in mis)
            {
                //bool b = mi.IsDefined(t, false);
                //bool dd = mi.IsHideBySig;
                //bool add = mi.IsSpecialName;
                //bool sdfd = mi.IsVirtual;
                //if (obj != null && !mi.IsDefined(typeof(Object),false) && mi.IsPublic && !mi.IsVirtual)
                if (obj != null && mi.IsPublic && !mi.IsVirtual)//当类可被实例化，并且是公开函数，不是虚函数，则认为是有效函数。
                {
                    OrderList.Add(new OrderInfo() { ClassInstance = obj, Mothod = mi });
                }
                else if(mi.IsStatic)//加载静态函数
                {
                    OrderList.Add(new OrderInfo() { ClassInstance = null, Mothod = mi });//静态函数不需要创建类实例即可调用
                }
                //object obj2;
                //obj2 = OrderList[0].Mothod.Invoke(OrderList[0].ClassInstance, null);
            }
        }
        //添加钩子函数，勾取指定名称的函数，进行特殊处理
        public void AddHook()
        {
            ;
        }
        
        private void InvokeHook()
        {
            string HookName;
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(new System.Diagnostics.StackFrame());
            System.Diagnostics.StackFrame sf = st.GetFrame(0);
            HookName = sf.GetMethod().Name + "Hook";
        }

        
    }
    struct VariantInfo
    {
        public string VariantName;
        public object VariantValue;
    }

    struct OrderInfo
    {
        public object ClassInstance;
        public System.Reflection.MethodInfo Mothod;
    }
    //支持 多线程操作RecordEx
    //单线程操作的Record

    struct KeywordAttributes
    {
        public string KeywordName;
        public char Keyword;
        //public Delegate Operating;//遇到关键字的处理
        //public byte[] ArgumentsList;
        //public bool EnableOpterating;
    }

    class KeywordSetClass
    {
        char[] Keywords = {
                              '{',
                              '}',
                              '=',
                              ',',
                              '$',
                              '\\',
                              ';',
                              ':'//指令分隔符号；
                          };
        public char OrderStartSymbol
        {
            get
            {
                return Keywords[0];
            }
            set 
            {
                Keywords[0] = value;
            }
        }
        public char OrderTerminator
        {
            get { return Keywords[1]; }
            set { Keywords[1] = value; }
        }
        public char AssignmentSymbol
        {
            get { return Keywords[2]; }
            set { Keywords[2] = value; }
        }
        public char ArgumentSeparator
        {
            get { return Keywords[3]; }
            set { Keywords[3] = value; }
        }
        public char VariantDeclarationSymbol
        {
            get { return Keywords[4]; }
            set { Keywords[4] = value; }
        }
        public char EscapeSysbol
        {
            get { return Keywords[5]; }
            set { Keywords[5] = value; }
        }
        public char EndingSymbol
        {
            get { return Keywords[6]; }
            set { Keywords[6] = value; }
        }
        public char OrderNameSeparator
        {
            get { return Keywords[7]; }
            set { Keywords[7] = value; }
        }
        public char this[string KeywordName]
        {
            get
            {
                switch(KeywordName)
                {
                    case "OrderStartSymbol"://命令起始符
                        return Keywords[0];
                    case "OrderTerminator"://命令终止符
                        return Keywords[1];
                    case "AssignmentSymbol"://赋值符号
                        return Keywords[2];
                    case "ArgumentSeparator"://参数分隔符
                        return Keywords[3];
                    case "VariantDeclarationSymbol"://变量声明字符
                        return Keywords[4];
                    case "EscapeSymbol"://转义符号
                        return Keywords[5];
                    case "EndingSymbol":
                        return Keywords[6];
                    case "OrderNameSeparator"://指令分隔符，用于将指令名与参数分隔
                        return Keywords[7];
                    default:
                        throw new Exception("没有关键字：" + KeywordName);
                }
            }
            set
            {
                switch (KeywordName)
                {
                    case "OrderStartSymbol"://命令起始符
                        Keywords[0] = value;
                        break;
                    case "OrderTerminator"://命令终止符
                        Keywords[1] = value;
                        break;
                    case "AssignmentSymbol"://赋值符号
                        Keywords[2] = value;
                        break;
                    case "ArgumentSeparator"://参数分隔符
                        Keywords[3] = value;
                        break ;
                    case "VariantDeclarationSymbol"://变量声明字符
                        Keywords[4] = value;
                        break;
                    case "EscapeSymbol"://转义符号
                        Keywords[5] = value;
                        break;
                    case "EndingSymbol":
                        Keywords[6] = value;
                        break;
                    default:
                        break;
                }
            }
        }
        public char this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Keywords[0];
                    case 1:
                        return Keywords[1];
                    case 2:
                        return Keywords[2];
                    case 3:
                        return Keywords[3];
                    case 4:
                        return Keywords[4];
                    default:
                        throw new Exception("索引超出界限");
                }
            }
            set
            {
                ;
            }
        }
    }
    public class UnicodeCharStringFormat : ICustomFormatter,IFormatProvider,IFormattable
    {
        string _Separator = " ";
        public string Separator
        {
            get { return _Separator;  }
            set { _Separator = value; }
        }
        #region IFormatProvider 成员

        public object GetFormat(Type formatType)
        {
 	        //throw new NotImplementedException();
            //Type类型包含类型的所有信息，而string仅仅是类型的名称。
            //Type t = typeof(int);
            if (formatType == typeof(ICustomFormatter))
            {
                return this;
            }
            return null;
        }

        #endregion

        #region ICustomFormatter 成员

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            UnicodeCharStringFormat UCSF = formatProvider as UnicodeCharStringFormat;
            string typeName =  arg.GetType().Name;
            string strRet  = "";
            switch (typeName)
            {
                case "String":
                    break;
                case "Char":
                    char c =(char)arg;
                    Int16 iUnicodeChar = Convert.ToInt16(c);//将字符串转为十进制数
                    switch(format)
                    {
                        case "D":  //十进制字符串形式           
                            strRet = iUnicodeChar.ToString();
                            //byte b = (byte)c;
                            //strRet = b.ToString();
                            break;
                        case "X":   //十六进制字符串形式
                            strRet = iUnicodeChar.ToString("X");
                            break;
                        case "B":   //二进制字符串形式
                            //strRet = iUnicodeChar.ToString("B");
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            return strRet;
            //throw new NotImplementedException();
        }

        #endregion

        #region IFormattable 成员

        public string ToString(string format, IFormatProvider formatProvider)
        {
            format = format.ToUpper();
            switch (format)
            {
                case "X":
                    break;
                case "B":
                    break;
                case "O":
                    break;
                case "D":
                    break;
                default:
                    break;
            }
            return "a";
            //throw new NotImplementedException();
        }
        #endregion
    }
    static class CommonFunction
    {
        public static System.Text.Encoding GetFileCoding(ref System.IO.FileStream fs)
        {
            Encoding FileCoding;
            //br = new System.IO.BinaryReader(fs);
            byte[] BOM = new byte[3];
            fs.Read(BOM, 0, 3);
            if (BOM[0] == 255 && BOM[1] == 254)//BOM:FF FE
            {
                FileCoding = Encoding.Unicode;
            }
            else if (BOM[0] == 254 && BOM[1] == 255)//FE FF
            {
                FileCoding = Encoding.BigEndianUnicode;
            }
            else if (BOM[0] == 239 && BOM[1] == 187 && BOM[2] == 191)//EF BB BF
            {
                FileCoding = Encoding.UTF8;
            }
            else
            {
                FileCoding = Encoding.Default;//ANSI编码
            }
            fs.Seek(0, System.IO.SeekOrigin.Begin);//移到文件开始处,恢复偏移
            return FileCoding;
        }
    }
}
