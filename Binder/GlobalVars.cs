using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Binder
{
    class GlobalVars
    {
        public static BinderHelper BinderHelper = new BinderHelper();

        //当前软件版本号
        public static string Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        //保存当前运行目录，因为多处用到,后面没有“\”
        public static string CurrentPath = Environment.CurrentDirectory;

        public static string BanQuan;
        public static string ShangBiao;
        public static string ChanPin;
        public static string GongSi;
        public static string ShuoMing;
        public static string BiaoTi;

        public static bool a;
    }
}
