/***********************************************
 * 名称:Record1_0 
 * 功能/用途：用于基本的记录语言
 * 目标：从原有EckyLanguage独立出来，专门针对最简单的记录语言应用
 * 版本号：V1.0;
 * 作者：Ecky Leung;
 * 立项时间：2011-8-1
 * 修改信息： 
 * 1.增加编辑函数 UpdateRecord(int row,int column,string text) 2017-11-1
 * 备注：
 **********************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EckyLanguage
{
    public class Record1_0
    {
        #region---------------------记录相关变量-----------------------------------------
        int RecordBodyBegin;//当前记录体开始位置  1
        int RecordBodyEnd;//当前记录体结束位置    2
        int RecordBodyPos;//记录体的当前位置      3
        int RecordLinePos;//记录的行位置          4
        //记录元素分隔符
        #endregion
        //内部变量编号，主要用于系统自动识别参数传递
        #region------------------ 文件相关变量-----------------------------

        #endregion

        #region  脚本缓冲区相关变量
        int ScriptOffset;     //                  5
        int LastSeparatorOffset;//上一个分隔符的位置。包括空格、换行、回车等
        int CurrentSeparatorOffset;//当前分隔符的位置。
        #endregion

        internal System.IO.FileStream fs;
        internal string ScriptBody;
        internal Encoding ScriptEncode;
        //List<KeyWordAttributes> KeyWordList = new List<KeyWordAttributes>();

        public delegate void RecordBeginSignOperatingHandler(int StringOffset);
        public void RecordBeginSignOperating(int StringOffset)
        {
            RecordBodyBegin = StringOffset;
        }

        public delegate void RecordEndSignOperatingHandler(int StringOffset);
        public void RecordEndSignOperating(int StringOffset)
        {
            RecordBodyEnd = StringOffset;
        }

        char _RecordSeparator = ',';
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
        bool _UsingStardardRecordLanguage = true;
        /// <summary>
        /// 使用标准记录语言，所谓标准记录语言，即为默认的记录语法，主要包括
        /// 记录体符号[]，记录分隔符 ","
        /// 子记录体符号(),分隔符号 “|”；
        /// </summary>
        public bool UsingStandardRecordLanguage
        {
            get
            {
                return _UsingStardardRecordLanguage;
            }
            set
            {
                _UsingStardardRecordLanguage = value;
            }
        }
        public Record1_0(string FileName)
        {
            fs = new System.IO.FileStream(FileName, System.IO.FileMode.OpenOrCreate);
            //br = new System.IO.BinaryReader(fs);
            byte[] BOM = new byte[3];
            fs.Read(BOM, 0, 3);
            if (BOM[0] == 255 && BOM[1] == 254)//BOM:FF FE
            {
                ScriptEncode = Encoding.Unicode;
            }
            else if (BOM[0] == 254 && BOM[1] == 255)//FE FF
            {
                ScriptEncode = Encoding.BigEndianUnicode;
            }
            else if (BOM[0] == 239 && BOM[1] == 187 && BOM[2] == 191)//EF BB BF
            {
                ScriptEncode = Encoding.UTF8;
            }
            else
            {
                ScriptEncode = Encoding.Default;//ANSI编码
            }

            fs.Seek(0, System.IO.SeekOrigin.Begin);//移到文件开始处
            int FileSize = (int)fs.Length;
            byte[] FileBuffer = new byte[FileSize];
            int Count = fs.Read(FileBuffer, 0, FileSize);

            ScriptBody = ScriptEncode.GetString(FileBuffer);
        }

        List<EnumeratorAttributes> EnumeratorList = new List<EnumeratorAttributes>();
        //int GlobalOffset = 0;
        object objLock = 1;
        class EnumeratorAttributes
        {
            public int EnumeratorId;
            public int EnumeratorOffset;
        }
        public string[] EnumRecord()
        {
            EnumeratorAttributes CurrentEnumerator = null;
            int EnumeratorId = System.AppDomain.GetCurrentThreadId();//以线程ID为枚举器ID。
            //int EnumeratorId = System.Threading.Thread.ManagedThreadId;//ManagedThreadId是实例成员
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
            string[] strResult;

            //lock (objLockAdd)
            //{
            strResult = ReadRecord(ref CurrentEnumerator.EnumeratorOffset);
            //}

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
            StringBuilder sbRecord = new StringBuilder("[");
            foreach (string s in RecordElements)
            {
                sbRecord.Append(s + _RecordSeparator);
            }

            sbRecord[sbRecord.Length - 1] = ']';//替换最后的逗号为记录终止符号
            sbRecord.Append("\r\n");
            string r = sbRecord.ToString();
            byte[] b = ScriptEncode.GetBytes(r);

            lock (objLockAdd)
            {
                fs.Seek(0, System.IO.SeekOrigin.End);
                fs.Write(b, 0, b.Length);
                fs.Flush();//更新内容到文件

                ScriptBody += r;
            }

            return true;
        }
        public string[] ReadRecord(ref int Offset)
        {
            lock (objLockAdd)
            {
                string[] RecordElements = null;
                int Begin = -1, End = -1;
                for (int i = Offset; i < ScriptBody.Length; i++)
                {
                    if (ScriptBody[i] == '[')
                    {
                        Begin = i;
                    }
                    if (ScriptBody[i] == ']')
                    {
                        End = i;
                        break;
                    }
                }
                //if (Begin == 0 && End == 0)
                //{
                //    return null;
                //}
                if (Begin == -1 || End == -1)//只要开始和结束出现异常，即返回null。
                {
                    return null;
                }

                Offset = End + 1;
                string RecordBody = ScriptBody.Substring(Begin + 1, End - Begin - 1);
                RecordElements = RecordBody.Split(_RecordSeparator);//按记录分隔符分离
                return RecordElements;
            }
        }

        public void Dispose()
        {
            fs.Flush();
            fs.Close();
            fs.Dispose();//关闭文件
            //this.Dispose();
        }
        public static string[] SplitSubrecord(string SubrecordBody, out string SubrecordName, char? SubrecordSeparator, char? SubrecordStartCharacter, char? SubrecordTerminator)
        {
            char _SubrecordSeparator, _SubrecordStartCharacter, _SubrecordTerminator;

            _SubrecordSeparator = SubrecordSeparator == null ? ',' : (char)SubrecordSeparator;
            _SubrecordStartCharacter = SubrecordStartCharacter == null ? '(' : (char)SubrecordStartCharacter;
            _SubrecordTerminator = SubrecordTerminator == null ? ')' : (char)SubrecordTerminator;

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
                //System.IO.FileStream fsNew = new System.IO.FileStream(FileName, System.IO.FileMode.CreateNew);
                if (System.IO.File.Exists(FileName))
                {
                    System.IO.File.Delete(FileName);
                }

                System.IO.FileStream fsNew = new System.IO.FileStream(FileName, System.IO.FileMode.Create);
                byte[] NewBytes = ScriptEncode.GetBytes(ScriptBody);
                fsNew.Write(NewBytes, 0, NewBytes.Length);
                fsNew.Flush();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void Clear()
        {
            lock(objLockAdd)
            {
                ScriptBody = "";
            }
            fs.Seek(0,System.IO.SeekOrigin.Begin);
            fs.SetLength(0);
        }

        public void UpdateRecord(int row, params string[] records) {

        }

        public void UpdateRecord(int row, int column, string record) {
            int iRow = 0;
            
            StringHelper.StringPos pos = new StringHelper.StringPos();
            pos.StartPosition = 0;
            pos.NextStartPosition = 0;

            int iBegin, iEnd;
            do
            {
                if (iRow == row)
                {

                    iBegin = StringHelper.SearchChar(ref ScriptBody, pos.NextStartPosition, '[');
                    iBegin = StringHelper.SearchChar(ref ScriptBody, iBegin, _RecordSeparator, ']', column);

                    iEnd = StringHelper.SearchChar(ref ScriptBody, iBegin + 1, _RecordSeparator, ']', 1);

                    if (iBegin != -1 && iEnd != -1)
                    {
                        iBegin += 1;
                        ScriptBody = ScriptBody.Remove(iBegin, iEnd - iBegin);
                        ScriptBody = ScriptBody.Insert(iBegin, record);

                        fs.Seek(0, System.IO.SeekOrigin.Begin);

                        byte[] data = ScriptEncode.GetBytes(ScriptBody);
                        fs.Write(data,0,data.Length);
                        fs.SetLength(data.Length);
                        fs.Flush();
                    }

                    break;
                }

                pos = StringHelper.SearchString(ref ScriptBody, pos.NextStartPosition, 0, "[");
                if (pos.StartPosition == -1)
                {
                    break;
                }

                pos = StringHelper.SearchString(ref ScriptBody, pos.NextStartPosition, 0, "]");
                if (pos.StartPosition == -1) {
                    break;
                }

                ++iRow;
            } while (true);
        }
    }
}
