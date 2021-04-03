using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace aliyundrive_Client_CSharp
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            WinMethod.DisableMinmizebox(this);
            InitializeComponent();
            BrowserControl.Navigating += BrowserControl_Navigating;
            BrowserControl.LoadCompleted += BrowserControl_LoadCompleted;
        }

        private void BrowserControl_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            SuppressScriptErrors((WebBrowser)sender, true);
        }

        bool clear = true;
        private void BrowserControl_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            BrowserControl.InvokeScript("eval", "window.onerror=function(){return true}");
            try
            {
                if (clear)
                {
                    clear = false;
                    BrowserControl.InvokeScript("eval", "localStorage.clear()");
                }
                var o = BrowserControl.InvokeScript("eval", "localStorage.getItem('token')");
                if (o != DBNull.Value)
                {
                    aliyundrive.token.SetToken(o.ToString());
                    Close();
                }
                Console.WriteLine("内容:{0}", o);
            }
            catch (Exception ex)
            {
                Console.WriteLine("错误:{0}", ex.Message);
            }
        }
        public void SuppressScriptErrors(WebBrowser wb, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        private void Btn_SetLogin_Click(object sender, RoutedEventArgs e)
        {
            aliyundrive.token.SetToken(Txt_token.Text);
            if (aliyundrive.token.Instance.refresh_token == "")
            {
                Txt_token.Text = "token格式错误,浏览器登录后,按F12打开控制台输入以下命令获取token:\r\nlocalStorage.getItem('token')";
            }
            else
            {
                Close();
            }
        }
        private void Btn_LoginOther_Click(object sender, RoutedEventArgs e)
        {
            Sp_Login.Visibility = Visibility.Hidden;
            BrowserControl.Visibility = Visibility.Visible;
            BrowserControl.Navigate("https://aliyundrive.com/sign/in");
        }
    }
}
