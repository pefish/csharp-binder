using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Binder
{
    class BinderHelper
    {

        /// <summary>
        /// 访问指定网页，返回网页内容
        /// </summary>
        /// <param name="URL"></param>
        /// <returns>访问网页失败时返回空字符串</returns>
        public string ExploreWeb(string URL)
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(URL);
                WebResponse webResponce = webRequest.GetResponse();
                StreamReader streamReader = new StreamReader(webResponce.GetResponseStream());
                string result = streamReader.ReadToEnd();
                return result;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 启动外部文件，异步（完全脱离主程序），不等待其退出
        /// </summary>
        /// <param name="fileName">文件全文件名</param>
        /// <param name="strs">参数</param>
        public void StartFile(string fileName, params string[] strs)
        {
            string args = string.Empty;
            foreach (string str in strs)
            {
                args += str + " ";
            }
            Process.Start(fileName, args);
        }

        /// <summary>
        /// 执行cmd命令,并等待执行完成返回结果（可以利用线程实现异步，但不会脱离主程序）
        /// </summary>
        /// <param name="cmd">cmd命令,如“ipconfig”</param>
        /// <returns>返回命令执行结果</returns>
        public string ExeCmd(string cmd)
        {
            Process p = new Process();
            //初始化start方法的属性
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C " + cmd;
            p.StartInfo.UseShellExecute = false;//重定向前必须设置
            p.StartInfo.RedirectStandardInput = false;//重定向
            p.StartInfo.RedirectStandardOutput = true;//重定向
            p.StartInfo.CreateNoWindow = true;//不显示窗口
            //启动进程
            p.Start();
            string strOutput = p.StandardOutput.ReadToEnd();//获取输出信息
            p.WaitForExit(500);//等待进程退出
            p.Close();//释放资源
            return strOutput;
        }

        /// <summary>
        /// 用给定的KDC密钥异或加密(解密同样可以用此方法)
        /// </summary>
        /// <param name="bytes">待加密的字节集</param>
        /// <param name="KDC">加密密钥</param>
        /// <returns>返回加密后的字节集</returns>
        public byte[] EncodeOrDecode(byte[] bytes, int KDC)
        {

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ KDC);
            }

            return bytes;
        }

        /// <summary>
        /// 加密字符串位异或后的十进制字符串
        /// </summary>
        /// <param name="strToEncode">待加密的字符串</param>
        /// <param name="KDC">加密密钥</param>
        /// <returns>返回加密后的字符串</returns>
        public string EncodeString(string strToEncode, int KDC)
        {
            byte[] bytesStrToEncode = Encoding.UTF8.GetBytes(strToEncode);
            byte[] encodedBytesStrToEncode = this.EncodeOrDecode(bytesStrToEncode, KDC);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in encodedBytesStrToEncode)
            {
                sb.AppendFormat("{0:D3}", b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解密字符串（对应上面的加密）
        /// </summary>
        /// <param name="strToDecode">待解密的字符串</param>
        /// <param name="KDC">解密密钥</param>
        /// <returns>返回解密后的字符串</returns>
        public string DecodeString(string strToDecode, int KDC)
        {
            MatchCollection matches = Regex.Matches(strToDecode, @"\d{3}");
            byte[] bytes = new byte[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                bytes[i] = (byte)(byte.Parse(matches[i].Value) ^ KDC);
            }
            return Encoding.UTF8.GetString(bytes);
        }


        [DllImport("wininet")]
        private extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);

        /// <summary>
        /// 检测本机是否联网
        /// </summary>
        /// <returns>已联网返回真</returns>
        public bool IsConnectedInternet()
        {
            int i = 0;
            if (InternetGetConnectedState(out i, 0))
            {
                //已联网
                return true;
            }
            else
            {
                //未联网
                return false;
            }

        }



        /// <summary>
        /// 打开已经加密的某个工具，同步
        /// </summary>
        /// <param name="toolProcessName">工具运行时的进程名称</param>
        /// <param name="toolName">工具的全文件名</param>
        /// <param name="tempStr">指定temp目录下的临时工具非全文件名</param>
        /// <returns>失败时返回假，成功返回真</returns>
        public bool StartTool(string toolProcessName, string toolName, string tempStr)
        {
            //检测工具是否存在
            if (!File.Exists(toolName))
            {
                return false;
            }
            string tempPath = System.IO.Path.GetTempPath();

            FileStream fileStream = new FileStream(toolName, FileMode.Open);
            byte[] bytesFile = new byte[fileStream.Length];
            fileStream.Read(bytesFile, 0, bytesFile.Length);
            fileStream.Close();

            //解密
            bytesFile = this.EncodeOrDecode(bytesFile, 0x07);

            //生成到临时目录
            string fileNameToSave = tempPath + tempStr;
            if (fileNameToSave.Length > 4 && fileNameToSave.Substring(fileNameToSave.Length - 4, 4) != ".exe")
            {
                fileNameToSave += ".exe";
            }
            FileStream fileStream1 = new FileStream(fileNameToSave, FileMode.Create);
            fileStream1.Write(bytesFile, 0, bytesFile.Length);
            fileStream1.Close();

            this.StartFile(fileNameToSave);

            return true;
        }

        /// <summary>
        /// 打开已经加密的某个命令行工具,等待退出结果
        /// </summary>
        /// <param name="toolProcessName">工具运行时的进程名称</param>
        /// <param name="toolName">工具的全文件名</param>
        /// <param name="tempStr">指定temp目录下的临时工具文件名</param>
        /// <param name="args">传给命令行工具的参数</param>
        /// <returns>运行失败返回空字符串，成功返回命令行工具的输出值</returns>
        public string StartCmdTool(string toolProcessName, string toolName, string tempStr, params string[] args)
        {
            //检测工具是否存在
            if (!File.Exists(toolName))
            {
                return string.Empty;
            }
            string tempPath = System.IO.Path.GetTempPath();

            FileStream fileStream = new FileStream(toolName, FileMode.Open);
            byte[] bytesFile = new byte[fileStream.Length];
            fileStream.Read(bytesFile, 0, bytesFile.Length);
            fileStream.Close();

            //解密
            bytesFile = this.EncodeOrDecode(bytesFile, 0x07);

            //生成到临时目录
            string fileNameToSave = tempPath + tempStr;
            if (fileNameToSave.Length > 4 && fileNameToSave.Substring(fileNameToSave.Length - 4, 4) != ".exe")
            {
                fileNameToSave += ".exe";
            }
            FileStream fileStream1 = new FileStream(fileNameToSave, FileMode.Create);
            fileStream1.Write(bytesFile, 0, bytesFile.Length);
            fileStream1.Close();

            //取得参数
            string strs = string.Empty;
            foreach (string str in args)
            {
                strs += str + " ";
            }
            strs = strs.Substring(0, strs.Length - 1);
            return this.ExeCmd(tempPath + tempStr + " " + strs);
        }


        /// <summary>
        /// 创建子键项的方法
        /// </summary>
        /// <param name="subkey">格式为“HKEY_CURRENT_CONFIG\Software\pefish”</param>
        /// <returns>成功返回RegistryKey实例，子键已经存在返回此实例，失败返回null</returns>
        public RegistryKey CreateSubKey(string subkey)
        {
            string[] nodes = subkey.Split(new char[] { '\\' });
            string a = nodes[0] + "\\";
            try
            {
                //循环检测子键是否存在，若不存在则创建
                for (int i = 1; i < nodes.Length; i++)
                {
                    if (GetInstance(a + nodes[i]) == null)
                    {
                        RegistryKey registryKey = GetInstance(a.Substring(0, a.Length - 1));
                        registryKey.CreateSubKey(nodes[i]);
                    }
                    a += nodes[i] + "\\";
                }
                return GetInstance(subkey);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 根据注册表某结点全名称返回对应RegistryKey子健项实例
        /// </summary>
        /// <param name="subkey"></param>
        /// <returns>子键项不存在时返回空</returns>
        public RegistryKey GetInstance(string subkey)
        {
            string[] node = subkey.Split(new char[] { '\\' });
            RegistryKey classroot = null;
            switch (node[0])
            {
                case "HKEY_CLASSES_ROOT":
                    classroot = Registry.ClassesRoot;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    classroot = Registry.CurrentConfig;
                    break;
                case "HKEY_CURRENT_USER":
                    classroot = Registry.CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                    classroot = Registry.LocalMachine;
                    break;
                case "HKEY_USERS":
                    classroot = Registry.Users;
                    break;
            }
            RegistryKey registryKey = classroot;
            for (int i = 1; i < node.Length; i++)
            {
                try
                {
                    registryKey = registryKey.OpenSubKey(node[i], true); //无访问权时会引发异常
                }
                catch (SecurityException) //捕捉特定异常
                {
                    //利用regini命令提权
                    StreamWriter sw = new StreamWriter(@"C:/1.ini");
                    sw.WriteLine(subkey + @" [1]");
                    sw.Close();

                    this.ExeCmd(@"regini C:/1.ini");
                    File.Delete(@"C:/1.ini");

                    registryKey = registryKey.OpenSubKey(node[i], true);
                }

            }
            return registryKey;
        }

        /// <summary>
        /// 创建键值对的方法
        /// </summary>
        /// <param name="keyvaluename">格式为“子键项：键名”，如“HKEY_CURRENT_CONFIG\Software&pefish：Isvoice”</param>
        /// <param name="value">键值</param>
        /// <param name="type">键值类型。0为Binary，1为DWord，2为ExpandString，3为MultiString，4为QWord，5为String</param>
        /// <returns>成功返回真，失败返回假</returns>
        public void CreateKeyValue(string keyvaluename, object value, int type)
        {
            string[] split = keyvaluename.Split(new char[] { ':' });
            try
            {
                RegistryKey registryKey = CreateSubKey(split[0]);
                switch (type)
                {
                    case 0:
                        //new byte[] {10, 43, 44, 45, 14, 255}
                        registryKey.SetValue(split[1], value, RegistryValueKind.Binary);
                        break;
                    case 1:
                        //42
                        registryKey.SetValue(split[1], value, RegistryValueKind.DWord);
                        break;
                    case 2:
                        //"The path is %PATH%"
                        registryKey.SetValue(split[1], value, RegistryValueKind.ExpandString);
                        break;
                    case 3:
                        //new string[] {"One", "Two", "Three"}
                        registryKey.SetValue(split[1], value, RegistryValueKind.MultiString);
                        break;
                    case 4:
                        //42
                        registryKey.SetValue(split[1], value, RegistryValueKind.QWord);
                        break;
                    case 5:
                        //"The path is %PATH%"
                        registryKey.SetValue(split[1], value, RegistryValueKind.String);
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 根据健名获取键值
        /// </summary>
        /// <param name="keyName">格式为“子键项：健名”,如“HKEY_CURRENT_CONFIG\Software\pefish:Isvoice”</param>
        /// <returns>子键项不存在或键值不存在都是返回null</returns>
        public object GetKeyValue(string keyName)
        {
            string[] substring = keyName.Split(new char[] { ':' });

            RegistryKey registryKey = GetInstance(substring[0]);
            if (registryKey != null)
            {
                return registryKey.GetValue(substring[1]);
            }
            else
            {
                return null;
            }
        }

    }
}
