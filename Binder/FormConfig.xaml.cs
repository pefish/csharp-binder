using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Binder
{
    /// <summary>
    /// FormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class FormConfig : Window
    {
        public FormConfig()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //检查长度
            string banquan = this.banquan.Text;
            string shangbiao = this.shangbiao.Text;
            string chanpin = this.chanpin.Text;
            string gongsi = this.gongsi.Text;
            string shuoming = this.shuoming.Text;
            string biaoti = this.biaoti.Text;
            if (banquan.Length > 50 || shangbiao.Length > 50 || chanpin.Length > 50 || gongsi.Length > 50 || shuoming.Length > 50 || biaoti.Length > 50)
            {
                MessageBox.Show("Length can not exceed 50!!","Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }

            GlobalVars.BanQuan = banquan;
            GlobalVars.ShangBiao = shangbiao;
            GlobalVars.ChanPin = chanpin;
            GlobalVars.GongSi = gongsi;
            GlobalVars.ShuoMing = shuoming;
            GlobalVars.BiaoTi = biaoti;

            GlobalVars.a = true;
            this.Close();
        }
    }
}
