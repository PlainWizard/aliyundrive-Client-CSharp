using aliyundrive_Client_CSharp.aliyundrive;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace aliyundrive_Client_CSharp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// PlainWizard
    /// https://github.com/PlainWizard/aliyundrive-Client-CSharp
    /// Bug反馈群:476304388
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Task.Run(InitFile);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitClipboard();
        }
        public static string localRootDir = Environment.CurrentDirectory;
        void InitFile()
        {
            Dispatcher.Invoke(() =>
            {
                localRootDirectory.Text = "文件根目录:" + localRootDir;
                var task_MaxCountList = new List<int>();
                for (var i = 1; i <= 100; i++) task_MaxCountList.Add(i);
                task_MaxCount.ItemsSource = task_MaxCountList;
                task_MaxCount.Text = "10";
                taskList.ItemsSource = TaskMange.Tasks;
                AsyncTaskMange.Instance.invoke = cb => { Dispatcher.Invoke(cb); };
                SyncButton.Background = AsyncTaskMange.Instance.Status ? System.Windows.Media.Brushes.Orange : null;
                AddDirectory(null);
            });
            TaskMange.OnTaskChange = OnTaskChange;
            TaskMange.OnServerFileChange = file_id =>
            {
                if (!ShowUserinfoAsync().Result) return;
                if (_currentDirectory_server.Count > 0)
                {
                    if (_currentDirectory_server[_currentDirectory_server.Count - 1].file_id != file_id) return;
                }
                else if (file_id != "root") return;
                AddDirectory_server(null).Wait();
            };
            LocalFile.Watch(localRootDir, (name, path, type) =>
            {
                Console.WriteLine($"文件变动[{path}][{fullDirectory}]");
                if (fullDirectory == path.Substring(0, path.LastIndexOf('\\')))
                {
                    Console.WriteLine("文件变动,重新绑定:[{0}]", path);
                    Dispatcher.Invoke(() =>
                    {
                        UpdateShowFile();
                    });
                }
                AsyncTaskMange.Instance.Add(path);
            });
            RefreshData();
            Sync();
        }
        void Sync()
        {
            if (!AsyncTaskMange.Instance.Status) return;
            var dir = new DirectoryInfo(MainWindow.localRootDir);
            foreach (var item in dir.GetFiles("*", SearchOption.AllDirectories))
            {
                AsyncTaskMange.Instance.Add(item.FullName);
            }
        }
        async void RefreshData()
        {
            if (!await ShowUserinfoAsync()) return;
            await AddDirectory_server(null);
        }
        async Task<bool> ShowUserinfoAsync()
        {
            if (token.Instance.refresh_token == "")
            {
                LoginOK = false;
                ShowServerTip($"token错误(点击右上角重新登录)");
                return false;
            }
            LoadingShow();
            var r = await databox.Instance.get_personal_info();
            LoadingHide();
            LoginOK = r.success;
            if (!r.success)
            {
                ShowServerTip($"需要登录(点击右上角登录):{r.Error}");
                return false;
            }
            var used_size = Util.GetShowSize(databox.Instance.personal_space_info.used_size);
            var total_size = Util.GetShowSize(databox.Instance.personal_space_info.total_size);
            var step = 100 * databox.Instance.personal_space_info.used_size / databox.Instance.personal_space_info.total_size;
            string userinfo = $@"欢迎您!{databox.Instance.personal_rights_info.name} [{token.Instance.nick_name}][{token.Instance.user_name}][{databox.Instance.personal_rights_info.spu_id}]
总空间:{total_size}    已使用:{used_size}    使用:{step}%
";

            Dispatcher.Invoke(() =>
            {
                localRootSpace.Value = step;
                localRootUserinfo.Text = userinfo;
            });
            return true;
        }

        public string rootDirectory { get { return _currentDirectory[0]; } set { _currentDirectory[0] = value; } }
        List<string> _currentDirectory = new List<string> { localRootDir };
        List<info_file> _currentDirectory_server = new List<info_file> { };
        public string fullDirectory { get; set; }
        public string serverDirectory_file_id { get; set; } = "root";
        public string relativeDirectory { get { return fullDirectory.Substring(rootDirectory.Length); } }
        void OnTaskChange()
        {
            Dispatcher.Invoke(() => { taskHeader.Header = TaskMange.GetHeadStr(); });
        }
        #region 本地文件区
        private void Button_Click_OpenDirectory(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(fullDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开文件夹:{ex.Error()}");
            }
        }
        void UpdateShowFile()
        {
            List<info_file> list = new List<info_file>();
            var d = new System.IO.DirectoryInfo(fullDirectory);
            foreach (var item in d.GetDirectories())
            {
                list.Add(new info_file { type = "folder", name = item.Name, file_id = item.FullName, updated_at = item.LastWriteTime });
            }
            foreach (var item in d.GetFiles())
            {
                list.Add(new info_file { size = item.Length, name = item.Name, file_id = item.FullName, updated_at = item.LastWriteTime });
                //.ToString("yyyy-MM-dd HH:mm:sss")
            }
            Dispatcher.Invoke(() =>
            {
                localFile.ItemsSource = list;
            });
        }

        private void Button_Click_Upload(object sender, RoutedEventArgs e)
        {
            foreach (info_file item in localFile.SelectedItems)
            {
                //添加上传任务
                if (item.type == "folder") continue;
                TaskMange.Add(new TaskInfo { Type = TaskType.上传, FullName = fullDirectory + "\\" + item.name, Name = item.name, parent_file_id = serverDirectory_file_id });
            }
        }

        private void localFile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (localFile.SelectedItem == null) return;
            var f = (info_file)localFile.SelectedItem;
            Console.WriteLine($"localFile_MouseDoubleClick:{f.name}");
            if (f.type == "folder")
            {
                Console.WriteLine($"点击目录:{f.name}:{fullDirectory}");
                AddDirectory(f.name);
            }
        }

        public void SubDirectory(bool root = false)
        {
            if (_currentDirectory.Count != 1)
            {
                _currentDirectory.RemoveAt(_currentDirectory.Count - 1);
                while (root)
                {
                    if (_currentDirectory.Count == 1) break;
                    _currentDirectory.RemoveAt(_currentDirectory.Count - 1);
                }
            }
            fullDirectory = Path.Combine(_currentDirectory.ToArray());
            Dispatcher.Invoke(() =>
            {
                localFileDirectory.Text = relativeDirectory;
            });
            UpdateShowFile();
        }
        public void AddDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path)) _currentDirectory.Add(path);
            fullDirectory = Path.Combine(_currentDirectory.ToArray());
            Dispatcher.Invoke(() =>
            {
                localFileDirectory.Text = relativeDirectory;
            });
            UpdateShowFile();
        }

        private void Button_Click_RootDirectory(object sender, RoutedEventArgs e)
        {
            SubDirectory(true);
        }

        private void Button_Click_SubDirectory(object sender, RoutedEventArgs e)
        {
            SubDirectory();
        }

        #endregion
        #region 服务器区
        private void Button_Click_Server_OpenDownUrl(object sender, RoutedEventArgs e)
        {            
            foreach (info_file item in serverFile.SelectedItems)
            {
                Console.WriteLine($"OpenDownUrl:{item.name},{item.file_id},{item.download_url}");
                if (item.type == "folder") continue;
                new RefererDownload.MainWindow(item.download_url, "https://www.aliyundrive.com/").Show();
                break;
            }
        }
        private void Button_Click_Server_Show(object sender, RoutedEventArgs e)
        {
            try
            {
                Fap fap = new Fap() { tab= "aliyundrive", list=new List<FapInfo>()};
                foreach (info_file item in serverFile.SelectedItems)
                {
                    if (item.type == "folder") throw new Exception("暂时不支持分享目录");
                    fap.list.Add(new FapInfo {
                        name=item.name 
                        ,type=item.type
                        ,updated=item.updated_at.Ticks
                        ,ext=item.file_extension
                        ,size=item.size
                        ,sha1=item.content_hash
                        ,c1=item.content_hash_name
                        ,c2=item.crc64_hash
                    });
                }
                if (fap.list.Count==0) throw new Exception("没有文件");
                Process.Start("https://file.wyfxw.cn/?fap="+Fap.FAP_set(fap));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Button_Click_Server_CopyDownUrl(object sender, RoutedEventArgs e)
        {

            try
            {
                string urls = "";
                if (serverFile.SelectedItems.Count == 1)
                {
                    var item = ((info_file)serverFile.SelectedItems[0]);
                    if (item.type == "folder") throw new Exception("暂时不支持复制目录");
                    urls = item.download_url;
                    Console.WriteLine($"copy:{urls}");
                }
                else
                {
                    foreach (info_file item in serverFile.SelectedItems)
                    {
                        Console.WriteLine($"copy:{item.name},{item.file_id},{item.download_url}");
                        if (item.type == "folder") throw new Exception("暂时不支持复制目录");
                        urls += $"{item.name}:{item.download_url}\r\n";
                    }
                }

                if (urls == "") throw new Exception("没有url");
                Clipboard.SetText(urls);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void ShowServerTip(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(msg))
                {
                    serverErorTip.Visibility = Visibility.Hidden;
                }
                else
                {
                    serverErorTip.Text = msg;
                    serverErorTip.Visibility = Visibility.Visible;
                }
            });
        }
        async Task UpdateShowFile_server(string file_id)
        {
            var f = new file();
            if (string.IsNullOrEmpty(file_id)) f.parent_file_id = "root";
            else f.parent_file_id = file_id;
            serverDirectory_file_id = f.parent_file_id;
            f.drive_id = token.Instance.default_drive_id;
            try
            {
                var r = await f.list();
                Dispatcher.Invoke(() =>
                {
                    serverFile.ItemsSource = r.success ? r.obj.items : null;
                    ShowServerTip(r.Error);
                });

            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowServerTip(ex.Message);
                });
            }
        }

        async Task SubDirectory_server(bool root = false)
        {

            if (root || _currentDirectory_server.Count <= 1)
            {
                _currentDirectory_server.Clear();
                await UpdateShowFile_server(null);
            }
            else
            {
                _currentDirectory_server.RemoveAt(_currentDirectory_server.Count - 1);
                await UpdateShowFile_server(_currentDirectory_server[_currentDirectory_server.Count - 1].file_id);
            }
            Dispatcher.Invoke(() =>
            {
                serverFileDirectory.Text = Path.Combine(_currentDirectory_server.Select(o => o.name).ToArray());
            });
        }
        async Task AddDirectory_server(info_file fileinfo)
        {
            if (fileinfo == null)
            {
                if (_currentDirectory_server.Count == 0)
                {
                    await UpdateShowFile_server(null);
                }
                else
                {
                    await UpdateShowFile_server(_currentDirectory_server[_currentDirectory_server.Count - 1].file_id);
                }
            }
            else
            {
                _currentDirectory_server.Add(fileinfo);
                UpdateShowFile_server(fileinfo.file_id).Wait();
            }
            Dispatcher.Invoke(() =>
            {
                serverFileDirectory.Text = Path.Combine(_currentDirectory_server.Select(o => o.name).ToArray());
            });
        }
        void LoadingShow() { Dispatcher.Invoke(() => { Loading.Visibility = Visibility.Visible; }); }
        void LoadingHide() { Dispatcher.Invoke(() => { Loading.Visibility = Visibility.Hidden; }); }
        private void Button_Click_Refresh(object sender, RoutedEventArgs e)
        {
            LoadingShow();
            Task.Run(() =>
            {
                ShowUserinfoAsync().Wait();
                AddDirectory_server(null).Wait();
                LoadingHide();
            });
        }
        private void Button_Click_Server_RootDirectory(object sender, RoutedEventArgs e)
        {
            LoadingShow();
            Task.Run(() =>
            {
                SubDirectory_server(true).Wait();
                LoadingHide();
            });
        }
        private void Button_Click_Server_SubDirectory(object sender, RoutedEventArgs e)
        {
            LoadingShow();
            Task.Run(() =>
            {
                SubDirectory_server().Wait();
                LoadingHide();
            });

        }
        #region 命名编辑
        private TaskType edit_type;
        private string edit_file_id;
        private void Button_Click_Server_CreatDirctory(object sender, RoutedEventArgs e)
        {
            edit_type = TaskType.目录;
            serverEditV.Visibility = Visibility.Visible;
        }
        private void Button_Click_serverEdit_OK(object sender, RoutedEventArgs e)
        {
            string name = serverEdit.Text;
            if (name == "")
            {
                serverEdit_tip.Text = "请输入名称";
                return;
            }
            if (TaskOK != null)
            {
                TaskOK(name);
                return;
            }
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                serverEdit_tip.Text = "含有非法字符 \\ / : * ? \" < > | 等";
                return;
            }

            if (edit_type == TaskType.目录)
            {
                //添加目录任务
                TaskMange.Add(new TaskInfo { Type = TaskType.目录, Name = name, parent_file_id = serverDirectory_file_id });
            }
            else if (edit_type == TaskType.改名)
            {
                //添加改名任务
                TaskMange.Add(new TaskInfo { Type = TaskType.改名, Name = name, file_id = edit_file_id, parent_file_id = serverDirectory_file_id });
            }
            Button_Click_serverEdit_Canel(sender, e);
        }
        Action<string> TaskOK;
        private void Button_Click_Server_Search(object sender, RoutedEventArgs e)
        {
            if (serverFile.SelectedItem != null) serverEdit.Text = ((info_file)serverFile.SelectedItem).name;
            
            serverEditV.Visibility = Visibility.Visible;
            TaskOK = async (name) => {
                Dispatcher.Invoke(() =>
                {
                    LoadingShow();
                });
                var f = new file();
                f.parent_file_id = serverDirectory_file_id;
                f.drive_id = token.Instance.default_drive_id;
                f.query = $"parent_file_id='{serverDirectory_file_id}' and name match '{name}'";
                try
                {
                    var r = await f.search();
                    Dispatcher.Invoke(() =>
                    {
                        serverFile.ItemsSource = r.success ? r.obj.items : null;
                        ShowServerTip(r.Error);
                    });

                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ShowServerTip(ex.Message);
                    });
                }
                TaskOK = null;
                Dispatcher.Invoke(() =>
                {
                    Button_Click_serverEdit_Canel(sender, e);
                    LoadingHide();
                });
            };
        }
        private void Button_Click_Server_Rename(object sender, RoutedEventArgs e)
        {
            if (serverFile.SelectedItem == null) return;
            var f = (info_file)serverFile.SelectedItem;

            serverEdit.Text = f.name;
            edit_type = TaskType.改名;
            edit_file_id = f.file_id;
            serverEditV.Visibility = Visibility.Visible;
        }
        private void Button_Click_serverEdit_Canel(object sender, RoutedEventArgs e)
        {
            serverEditV.Visibility = Visibility.Hidden;
            serverEdit_tip.Text = "";
        }

        #endregion
        private void Button_Click_Server_Del(object sender, RoutedEventArgs e)
        {
            foreach (info_file item in serverFile.SelectedItems)
            {
                Console.WriteLine($"Button_Click_Server_Del:{item.name},{item.file_id}");
                TaskMange.Add(new TaskInfo { Type = TaskType.删除, parent_file_id = serverDirectory_file_id, file_id = item.file_id, Name = item.name });
            }
        }
        private void serverFile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (serverFile.SelectedItem == null) return;
            var f = (info_file)serverFile.SelectedItem;
            Console.WriteLine($"serverFile_MouseDoubleClick:{f.name}");
            if (f.type == "folder")
            {
                Console.WriteLine($"点击目录:{f.name}:{fullDirectory}");
                LoadingShow();
                Task.Run(() =>
                {
                    AddDirectory_server(f).Wait();
                    LoadingHide();
                });
            }
        }

        private void Button_Click_Server_QrShow(object sender, RoutedEventArgs e)
        {
            if (serverFile.SelectedItem == null) return;
            var f = (info_file)serverFile.SelectedItem;
            if (f.type != "folder")
            {
                serverQRV.Visibility = Visibility.Visible;
                serverQR.Source = new BitmapImage(new Uri(Util.GetQrUrl(f.download_url)));
            }
        }

        private void serverQR_Image_MouseMove(object sender, MouseEventArgs e)
        {
            serverQRV.Visibility = Visibility.Hidden;
        }

        #endregion
        #region 任务区
        private void Button_Click_TaskClear(object sender, RoutedEventArgs e)
        {
            TaskMange.Clear();
        }
        private void Button_Click_TaskSync(object sender, RoutedEventArgs e)
        {
            new SyncFile().ShowDialog();
            SyncButton.Background = AsyncTaskMange.Instance.Status ? System.Windows.Media.Brushes.Orange : null;
        }
        private void Button_down_Click(object sender, RoutedEventArgs e)
        {
            new RefererDownload.MainWindow().Show();
        }
        private void task_MaxCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TaskMange.MaxCount = (int)task_MaxCount.SelectedItem;
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

        #endregion
        #region 顶部提示
        Action topTip_okRun;
        private void Button_Click_topTip_OK(object sender, RoutedEventArgs e)
        {
            topTip_okRun?.Invoke();
            Button_Click_topTip_Canel(sender, e);
        }
        private void Button_Click_topTip_Canel(object sender, RoutedEventArgs e)
        {
            topTip.Visibility = Visibility.Hidden;
            topTip_okRun = null;
        }
        void ShowTopTip(string str, Action ok)
        {
            Dispatcher.Invoke(() =>
            {
                topTip.Visibility = Visibility.Visible;
                topTip_tip.Text = str;
                topTip_okRun = ok;
            });
        }
        #endregion
        #region 监视剪贴板
        private ClipboardWatcher ClipboardWatcher;
        void InitClipboard()
        {
            ClipboardWatcher = new ClipboardWatcher(this);
            ClipboardWatcher.DrawClipboard += ClipboardWatcher_DrawClipboard;
            ClipboardWatcher.Start();
        }
        private void ClipboardWatcher_DrawClipboard(object sender, EventArgs e)
        {
            try
            {
                ClipboardChange();
            }
            catch (Exception) { }
        }
        void ClipboardChange()
        {
            if (Clipboard.ContainsText())
            {
                var str = Clipboard.GetText();
                if (str.Contains("fap://"))
                {
                    var r = Regex.Match(str, "fap://([a-fA-F0-9]+)");
                    if (r.Success)
                    {
                        var fap = Fap.FAP_get(r.Groups[1].Value);
                        if (fap != null)
                        {
                            str = "";
                            foreach (var item in fap.list)
                            {
                                str += Environment.NewLine + item.name;
                            }
                            ShowTopTip("要尝试秒传文件吗?" + Environment.NewLine + str, () =>
                            {
                                Fap.FAP_rapid_upload(fap, serverDirectory_file_id, cb => { Dispatcher.Invoke(cb); });
                            });
                        }
                    }
                }
            }
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                var str = "";
                foreach (var item in files)
                {
                    str += Environment.NewLine + item;
                }

                ShowTopTip("要上传剪贴板中的文件吗?" + str, () =>
                {
                    foreach (string item in files)
                    {
                        //添加上传任务
                        var f = new FileInfo(item);
                        TaskMange.Add(new TaskInfo { Type = TaskType.上传, FullName = f.FullName, Name = f.Name, parent_file_id = serverDirectory_file_id });
                    }
                });

            }
        }
        #endregion
        #region 拖拽上传文件
        private void serverFile_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
                List<string> files = new List<string>();
                foreach (string item in (System.Array)e.Data.GetData(DataFormats.FileDrop))
                {
                    files.Add(new FileInfo(item).Name);
                }
                localFileTip.Text = "鼠标放下上传文件:\n" + string.Join(",", files);
                localFileTip.Visibility = Visibility.Visible;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        private void serverFile_DragLeave(object sender, DragEventArgs e)
        {
            localFileTip.Visibility = Visibility.Hidden;
        }
        private void serverFile_Drop(object sender, DragEventArgs e)
        {
            localFileTip.Visibility = Visibility.Hidden;
            foreach (string item in (System.Array)e.Data.GetData(DataFormats.FileDrop))
            {
                //添加上传任务
                var f = new FileInfo(item);
                TaskMange.Add(new TaskInfo { Type = TaskType.上传, FullName = f.FullName, Name = f.Name, parent_file_id = serverDirectory_file_id });
            }
        }

        #endregion 拖拽上传文件
        #region 点击链接
        bool _LoginOK = true;
        bool LoginOK
        {
            set
            {
                if (_LoginOK == value) return;
                _LoginOK = value;
                Dispatcher.Invoke(() => { ClickLogin.Text = _LoginOK ? "退出登录" : "点击登录"; });
            }
            get
            {
                return _LoginOK;
            }
        }
        private void Click_Link_LoginOut(object sender, RoutedEventArgs e)
        {
            if (!LoginOK)
            {
                new Login().ShowDialog();
                RefreshData();
                return;
            }
            try
            {
                token.Instance.refresh_token = "";
                token.Instance.access_token = "";
                token.Instance.Config_Save();
                Task.Run(ShowUserinfoAsync);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Click_Link_Code(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/PlainWizard/aliyundrive-Client-CSharp");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 支持开源
        /// 开发交流群:476304388
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Click_Link_Supporting(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("http://6op.cn/PlainWizard.aliyundrive");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

    }

}