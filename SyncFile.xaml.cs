using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
    /// SyncFile.xaml 的交互逻辑
    /// </summary>
    public partial class SyncFile : Window
    {
        public SyncFile()
        {
            InitializeComponent();
            WinMethod.DisableMinmizebox(this);
            taskList.ItemsSource = TaskMange.Tasks;
            exclude.Text = string.Join(Environment.NewLine, AsyncTaskMange.Instance.ignore);
            GetList();
            ChangeStatus();
        }
        void GetList()
        {
            var dir=new DirectoryInfo(MainWindow.localRootDir);
            foreach (var item in dir.GetFiles("*",SearchOption.AllDirectories).Select(x=>x.Extension).Distinct().ToList())
            {
                if(!AsyncTaskMange.Instance.isExtExists(item))
                    AsyncTaskMange.Instance.ExtList.Add(new DataItem { Name = item });
            }
            AsyncTaskMange.Instance.Config_Save();
            checkListBox.ItemsSource = AsyncTaskMange.Instance.ExtList;
        }
        void Sync() {
            if (!AsyncTaskMange.Instance.Status) return;
            var dir = new DirectoryInfo(MainWindow.localRootDir);
            foreach (var item in dir.GetFiles("*", SearchOption.AllDirectories))
            {
                AsyncTaskMange.Instance.Add(item.FullName);
            }
        }
        void ChangeStatus()
        {
            if (AsyncTaskMange.Instance.Status)
            {
                Start.IsEnabled = false;
                Stop.IsEnabled = true;
                exclude.IsEnabled = false;
                checkListBox.IsEnabled = false;
                AsyncTaskMange.Instance.ignore= exclude.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(o=>o).ToList();
            }
            else
            {
                Start.IsEnabled = true;
                Stop.IsEnabled = false;
                exclude.IsEnabled = true;
                checkListBox.IsEnabled = true;
            }
            AsyncTaskMange.Instance.Config_Save();
        }
        private void Button_Click_Status(object sender, RoutedEventArgs e)
        {
            try
            {
                AsyncTaskMange.Instance.Status = !AsyncTaskMange.Instance.Status;
                ChangeStatus();
                if (AsyncTaskMange.Instance.Status) Sync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_TaskDel(object sender, RoutedEventArgs e)
        {
            List<TaskInfo> del = new List<TaskInfo>();
            foreach (TaskInfo item in taskList.SelectedItems)
            {
                //删除任务
                del.Add(item);
            }
            foreach (var item in del)
            {
                TaskMange.Remove(item);
            }
        }
    }


}
