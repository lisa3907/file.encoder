using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;

namespace uBizSoft.PRD.FileEncoder
{
    /// <summary>
    /// Class1에 대한 요약 설명입니다.
    /// </summary>
    public class Encoder
    {
        private bool DoTraverse = false;
        private bool DoBackup = false;
        private bool DoCopy = false;
        private bool DoConvert = false;

        private bool DefinedRoot = false;
        private bool DefinedSource = false;
        private bool DefinedTarget = false;

        private string m_baseDirectory = null;
        private string BaseDirectory
        {
            get
            {
                if (m_baseDirectory == null)
                    m_baseDirectory = System.Environment.CurrentDirectory;
                
                return m_baseDirectory;
            }
            set
            {
                m_baseDirectory = value;
            }
        }

        private string FilePattern
        {
            get;
            set;
        }

        private Encoding SourceEncoding = Encoding.Default;
        private Encoding TargetEncoding = Encoding.UTF8;
 
        //-------------------------------------------------------------------------------------------------------------------------//
        //
        //-------------------------------------------------------------------------------------------------------------------------//
        public int DoEncoding()
        {
            return DoEncoding(BaseDirectory);
        }

        private int DoEncoding(string p_directory)
        {
            int _result = 0;

            if (DoTraverse == true)
            {
                if (p_directory.Length < 248)
                {
                    string[] _directories = Directory.GetDirectories(p_directory);
                    if (_directories.Length > 0)
                    {
                        foreach (string _currDirectory in _directories)
                        {
                            _result += DoEncoding(_currDirectory);
                            _result += Converting(_currDirectory);
                        }
                    }
                }
            }

            _result += Converting(p_directory);

            return _result;
        }

        //-------------------------------------------------------------------------------------------------------------------------//
        //
        //-------------------------------------------------------------------------------------------------------------------------//
        private int Converting(string p_directory)
        {
            int _result = 0;

            if (p_directory.Length < 248)
            {
                string[] _files = Directory.GetFiles(p_directory, FilePattern);
                foreach (string _source in _files)
                {
                    if (_source.Length >= 260)
                        continue;

                    FileStream _fs = new FileStream(_source, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (DoConvert == true)
                    {
                        string _temp = _source + ".temp";

                        if (ConvertFile(_fs, _source, _temp) == true)
                        {
                            _result++;
                            _fs.Close();

                            if (File.Exists(_temp) == true)
                            {
                                if (DoBackup == true)
                                    File.Move(_source, _source + ".bak");
                                else
                                    File.Delete(_source);

                                File.Move(_temp, _source);
                            }
                            else
                            {
                                Console.WriteLine("could not found file: " + _temp);
                            }
                        }
                    }
                    else
                    {
                        if (DisplayFile(_fs, _source) == true)
                            _result++;
                    }

                    _fs.Close();
                }
            }

            return _result;
        }

        private bool ConvertFile(FileStream p_fs, string p_source, string p_temp)
        {
            bool _result = false;

            try
            {
                Encoding _sourceEncoding = GetEncoding(p_fs);
                if (_sourceEncoding != TargetEncoding || DoCopy == true)
                {
                    using (StreamReader _sr = new StreamReader(p_fs, _sourceEncoding))
                    {
                        FileStream _fs = new FileStream(p_temp, FileMode.Create, FileAccess.Write);
                        {
                            StreamWriter _sw = new StreamWriter(_fs, TargetEncoding);

                            while (_sr.Peek() >= 0)
                                _sw.WriteLine(_sr.ReadLine());

                            _sw.Close();
                        }
                    }

                    Console.WriteLine(String.Format("{0}: {1} => {2}", p_source, _sourceEncoding.BodyName, TargetEncoding.BodyName));

                    _result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return _result;
        }

        private bool DisplayFile(FileStream p_fs, string p_source)
        {
            bool _result = false;

            Encoding _sourceEncoding = GetEncoding(p_fs);
            if (_sourceEncoding != TargetEncoding)
            {
                Console.WriteLine(String.Format("{0}: {1}", p_source, _sourceEncoding.BodyName));
                _result = true;
            }

            return _result;
        }

        /// <summary>
        /// 텍스트 파일 - 특별한 표식이 없습니다.
        /// 유니코드 (little endian) - 파일 처음에 0xFF 0xFE 의 두바이트로 시작합니다.
        /// 유니코드 (big endian) - 파일 처음에 0xFE 0xFF 의 두바이트로 시작합니다.
        /// 유니코드 (UTF-8) - 파일 처음에 0xEF 0xBB 0xBF 의 세바이트로 시작합니다.
        /// </summary>
        /// <param name="p_fs"></param>
        /// <returns></returns>
        private Encoding GetEncoding(FileStream p_fs)
        {
            Encoding _result = null;

            if (p_fs.CanSeek == true)
            {
                byte[] _header = new byte[4];
                p_fs.Read(_header, 0, 4);

                if ((_header[0] == 0xff && _header[1] == 0xfe))										        // ucs-2le, ucs-4le, and ucs-16le
                {
                    _result = Encoding.Unicode;
                }
                else if ((_header[0] == 0xfe && _header[1] == 0xff))								        // utf-16 and ucs-2
                {
                    _result = Encoding.BigEndianUnicode;
                }
                else if ((_header[0] == 0xef && _header[1] == 0xbb && _header[2] == 0xbf))				    // utf-8
                {
                    _result = Encoding.UTF8;
                }
                else if ((_header[0] == 0 && _header[1] == 0 && _header[2] == 0xfe && _header[3] == 0xff))	// ucs-4
                {
                    _result = Encoding.Unicode;
                }
                else
                {
                    if (DefinedSource == true)
                    {
                        _result = SourceEncoding;
                    }
                    else
                    {
                        _result = Encoding.Default;
                    }
                }

                p_fs.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                if (DefinedSource == true)
                {
                    _result = SourceEncoding;
                }
                else
                {
                    _result = Encoding.Default;
                }
            }

            return _result;
        }

        //-------------------------------------------------------------------------------------------------------------------------//
        // region prefix
        //-------------------------------------------------------------------------------------------------------------------------//
        public void WriteTitle()
        {
            Console.WriteLine();
            Console.WriteLine(String.Format("uBizSoft(R) Text-File-Encoding-Translator (Version {0})", Assembly.GetExecutingAssembly().GetName().Version));
            Console.WriteLine("Copyright (C) uBizSoft Corporation 2001-2007. All rights reserved.");
            Console.WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteUsage()
        {
            Console.WriteLine();
            Console.WriteLine(String.Format("Usage: {0} <path> [<option>]", System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name));
            Console.WriteLine();
            Console.WriteLine(" <path> path of will converted text file's path (default: cwd)");
            Console.WriteLine(" /c do convert");
            Console.WriteLine(" /s include subdirector");
            Console.WriteLine(" /b do backup (.bak)");
            Console.WriteLine(" /f do copy");
            Console.WriteLine(" /se:<encoding> source file's encoding (default: ANSI)");
            Console.WriteLine(" /de:<encoding> object file's encoding (default: UTF-8)");
            Console.WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_arg"></param>
        public void ParsingParameter(string p_arg)
        {
            p_arg = p_arg.ToLower();
            if (p_arg.Length > 4 && p_arg.Substring(0, 4).ToLower() == "/se:")
            {
                if (DefinedSource == true)
                {
                    throw new Exception();
                }
                else
                {
                    DefinedSource = true;
                    SourceEncoding = Encoding.GetEncoding(p_arg.Substring(4, p_arg.Length - 4));
                }
            }
            else if (p_arg.Length > 4 && p_arg.Substring(0, 4).ToLower() == "/de:")
            {
                if (DefinedTarget == true)
                {
                    throw new Exception();
                }
                else
                {
                    DefinedTarget = true;
                    TargetEncoding = Encoding.GetEncoding(p_arg.Substring(4, p_arg.Length - 4));
                }
            }
            else if (p_arg.Length == 2 && p_arg.Substring(0, 2).ToLower() == "/s")
            {
                if (DoTraverse == true)
                {
                    throw new Exception("duplicated define /s option.");
                }
                else
                {
                    DoTraverse = true;
                }
            }
            else if (p_arg.Length == 2 && p_arg.Substring(0, 2).ToLower() == "/b")
            {
                if (DoBackup == true)
                {
                    throw new Exception("duplicated define /b option.");
                }
                else
                {
                    DoBackup = true;
                }
            }
            else if (p_arg.Length == 2 && p_arg.Substring(0, 2).ToLower() == "/f")
            {
                if (DoCopy == true)
                {
                    throw new Exception("duplicated define /c option.");
                }
                else
                {
                    DoCopy = true;
                }
            }
            else if (p_arg.Length == 2 && p_arg.Substring(0, 2).ToLower() == "/c")
            {
                if (DoConvert == true)
                {
                    throw new Exception("duplicated define /c option.");
                }
                else
                {
                    DoConvert = true;
                }
            }
            else
            {
                if (DefinedRoot == true)
                {
                    throw new Exception("duplicated define file-path");
                }
                else
                {
                    DefinedRoot = true;

                    p_arg = p_arg.Replace("\"", "");

                    int _index = p_arg.LastIndexOf(@"\");
                    if (_index != -1)
                    {
                        BaseDirectory = p_arg.Substring(0, _index);
                        FilePattern = p_arg.Substring(_index + 1, p_arg.Length - _index - 1);
                    }
                    else
                    {
                        FilePattern = p_arg;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------//
        // end prefix
        //-------------------------------------------------------------------------------------------------------------------------//
    }
}