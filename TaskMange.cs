using aliyundrive_Client_CSharp.aliyundrive;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp
{
    public class TaskMange
    {
        private static ObservableCollection<TaskInfo> tasks = new ObservableCollection<TaskInfo>();
        public static ObservableCollection<TaskInfo> Tasks { get { return tasks; } }
        protected static object lock_obj = new object();
        public static int MaxCount { get; set; }
        static int CurrentCount { get; set; }
        public static Action OnTaskChange;
        public static Action<string> OnServerFileChange;
        public static void StatusChange(TaskInfo task)
        {
            if (task.Status == 1) CurrentCount++;
            else if (task.Status > 1) CurrentCount--;
            if (CurrentCount < MaxCount)
            {
                lock (lock_obj)
                {
                    foreach (var item in tasks)
                    {
                        if (CurrentCount == MaxCount) break;
                        if (item.Status == 0) item.task = RunTask(item);
                    }
                }
            }
            OnTaskChange();
        }
        public static string GetHeadStr()
        {
            var list = tasks.ToList();
            //0 初始,1正在执行,2成功,3失败
            int allcount = list.Count
                , allcount0 = list.Where(t => { return t.Status == 0; }).Count()
                , allcount1 = list.Where(t => { return t.Status == 1; }).Count()
                , allcount2 = list.Where(t => { return t.Status == 2; }).Count()
                , allcount3 = list.Where(t => { return t.Status == 3; }).Count();
            string headstr = $"任务{allcount}({allcount0}/{allcount1}/{allcount2}/{allcount3})";
            return headstr;
        }
        static Task RunTask(TaskInfo task)
        {
            task.Status = 1;
            return Task.Run(async () =>
            {
                switch (task.Type)
                {
                    case TaskType.上传: await Task_ServerUploadAsync(task); break;
                    case TaskType.同步: await Task_ServerUploadAsync(task); break;
                    case TaskType.删除: await Task_ServerDelFileAsync(task); break;
                    case TaskType.目录: await Task_Server_folder(task); break;
                    case TaskType.改名: await Task_Server_rename(task); break;
                }
            });
        }
        public static void Add(TaskInfo task)
        {
            lock (lock_obj)
            {
                tasks.Insert(0, task);
                if (task.Status != 0) return;
                if (CurrentCount < MaxCount) task.task = RunTask(task);
            }
            OnTaskChange();
        }
        public static void Clear()
        {
            lock (lock_obj)
            {
                foreach (var item in tasks)
                {
                    item.Cancel();
                }
                tasks.Clear();
            }
            OnTaskChange();
        }
        public static void Remove(TaskInfo task)
        {
            lock (lock_obj)
            {
                task.Cancel();
                tasks.Remove(task);
            }
            OnTaskChange();
        }
        static async Task Task_Server_folder(TaskInfo task)
        {
            try
            {
                var r = await new upload().folder(task.parent_file_id, task.Name, task);
                if (r.success)
                {
                    if (r.obj.exist) throw new Exception("目录已存在");
                    OnServerFileChange(task.parent_file_id);
                    task.Status = 2;
                }
                else
                {
                    task.Status = 3;
                    task.Name = task.Name + $"[{r.Error}]";
                }
            }
            catch (Exception ex)
            {
                task.Status = 3;
                task.Name = task.Name + $"[{ex.Error()}]";
            }
        }
        static async Task Task_Server_rename(TaskInfo task)
        {
            try
            {
                var r = await new upload().rename(task.file_id, task.Name, task);
                if (r.success)
                {
                    OnServerFileChange(task.parent_file_id);
                    task.Status = 2;
                }
                else
                {
                    task.Status = 3;
                    task.Name = task.Name + $"[{r.Error}]";
                }
            }
            catch (Exception ex)
            {
                task.Status = 3;
                task.Name = task.Name + $"[{ex.Error()}]";
            }
        }
        static async Task Task_ServerDelFileAsync(TaskInfo task)
        {
            try
            {
                var r = await new batch().del(task.file_id);
                if (r.success)
                {
                    OnServerFileChange(task.parent_file_id);
                    task.Status = 2;
                }
                else
                {
                    task.Status = 3;
                    task.Name = task.Name + $"[{r.Error}]";
                }
            }
            catch (Exception ex)
            {
                task.Status = 3;
                task.Name = task.Name + $"[{ex.Error()}]";
            }
        }

        static async Task Task_ServerUploadAsync(TaskInfo task)
        {
            try
            {
                var r = await new upload().Upload(task.parent_file_id, task.Name, task);
                if (r.success)
                {
                    if (task.Type != TaskType.同步)
                    {
                        if (r.obj.rapid_upload) task.Type = TaskType.秒传;
                        OnServerFileChange(task.parent_file_id);
                    }
                    task.Status = 2;
                }
                else
                {
                    task.Status = 3;
                    task.Name = task.Name + $"[{r.Error}]";
                }
            }
            catch (Exception ex)
            {
                task.Status = 3;
                task.Name = task.Name + $"[{ex.Error()}]";
            }

        }
    }

    public class AsyncTaskMange : Config<AsyncTaskMange>
    {
        private ObservableCollection<DataItem> extList = new ObservableCollection<DataItem>();
        public ObservableCollection<DataItem> ExtList { get { return extList; } }
        public Dictionary<string, string> ParentIDs = new Dictionary<string, string>();
        public bool isExtExists(string name)
        {
            return ExtList.Where(o => o.Name == name).Count() > 0;
        }
        public bool Status = false;
        public List<string> ignore = new List<string>() { "\\node_modules", "\\logs", "\\.vs", "\\.git" };
        bool adding = false;
        public async void Add(string file, Action<Action> invoke)
        {
            if (!Status) return;
            await Task.Delay(3000);//优化同步速度O(∩_∩)O
            while (adding) await Task.Delay(1000);
            var f = new System.IO.FileInfo(file);
            if (!f.Exists) return;
            try
            {
                adding = true;
                foreach (var item in ignore)
                {
                    if (file.Contains(item)) return;
                }
                var ext = ExtList.Where(o => o.IsEnabled && o.Name == f.Extension).ToList();
                if (ext.Count == 0) return;
                if (!file.StartsWith(MainWindow.localRootDir)) throw new Exception("根目录异常");
                var relativeFile = file.Substring(MainWindow.localRootDir.Length + 1).Replace("\\","/");
                //dir 所在相对当前路径的目录 格式为 一层目录(aaa/) 二层目录(aaa/bbb/)
                string fid = "root";
                var ms = Regex.Matches(relativeFile, "([^/]+?)/");
                if (ms.Count > 0)
                {
                    if (!ParentIDs.ContainsKey(relativeFile))
                    {
                        var u = new upload();
                        foreach (Match m in ms)
                        {
                            Console.WriteLine($"同步创建目录[{relativeFile}]:{m.Groups[1].Value}");
                            fid = await u.getfolder(fid, m.Groups[1].Value);
                        }
                        ParentIDs[relativeFile] = fid;
                    }
                    fid = ParentIDs[relativeFile];
                }
                var r = await new file().search(fid, f.Name);
                if (!r.Yes) throw new Exception(r.Error);
                if (r.obj.items.Count > 0)
                {
                    using (var stream = Util.GetFileStream(f.FullName))
                    {
                        var sha1 = Util.sha1(stream);
                        var size = stream.Length;
                        foreach (var item in r.obj.items)
                        {

                            if (item.content_hash == sha1 && item.size == size) return;
                        }
                    }
                }
                invoke(() =>
                {
                    TaskMange.Add(new TaskInfo { Type = TaskType.同步, FullName = f.FullName, Name = f.Name, parent_file_id = fid });
                });

            }
            catch (Exception ex)
            {
                invoke(() =>
                {
                    TaskMange.Add(new TaskInfo { Type = TaskType.同步, FullName = f.FullName, Status = 3, Name = f.Name + $"[{ex.Error()}]" });
                });
            }
            finally
            {
                adding = false;
            }
        }
    }

}
