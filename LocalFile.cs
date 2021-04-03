using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp
{
    class LocalFile
    {
        public static void Watch(string path, Action<string, string, WatcherChangeTypes> action)
        {
            FileListenerServer f1 = new FileListenerServer(path, action);
            f1.Start();
        }

    }
    public class FileListenerServer
    {
        private FileSystemWatcher _watcher;
        Action<string, string, WatcherChangeTypes> action;
        public FileListenerServer()
        {
        }
        public FileListenerServer(string path,Action<string, string, WatcherChangeTypes> action)
        {
            this.action = action;
            this._watcher = new FileSystemWatcher();
            _watcher.Path = path;
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName;
            _watcher.IncludeSubdirectories = true;
            _watcher.Created += FileWatcher_Created;
            _watcher.Changed += FileWatcher_Changed;
            _watcher.Deleted += FileWatcher_Deleted;
            _watcher.Renamed += FileWatcher_Renamed;
            _watcher.Error += _watcher_Error;
        }

        private void _watcher_Error(object sender, ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {

            this._watcher.EnableRaisingEvents = true;
            Console.WriteLine("文件监控已经启动[{0}]", _watcher.Path);
        }

        public void Stop()
        {

            this._watcher.EnableRaisingEvents = false;
            this._watcher.Dispose();
            this._watcher = null;
        }

        protected void FileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("新增:" + e.ChangeType + ";" + e.FullPath + ";" + e.Name);
            action(e.Name,e.FullPath,e.ChangeType);
        }
        protected void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("变更:" + e.ChangeType + ";" + e.FullPath + ";" + e.Name);
            action(e.Name, e.FullPath, e.ChangeType);
        }
        protected void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("删除:" + e.ChangeType + ";" + e.FullPath + ";" + e.Name);
            action(e.Name, e.FullPath, e.ChangeType);
        }
        protected void FileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine("重命名: OldPath:{0} NewPath:{1} OldFileName{2} NewFileName:{3}", e.OldFullPath, e.FullPath, e.OldName, e.Name);
            action(e.Name, e.FullPath, e.ChangeType);
        }

    }
}
