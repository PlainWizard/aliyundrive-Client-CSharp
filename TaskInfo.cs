using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp
{

    public enum TaskType
    {
        删除, 上传, 秒传, 目录, 改名,同步, 分享
    }


    public class TaskInfo : INotifyPropertyChanged
    {
        public string parent_file_id = "root";
        public string file_id = "";
        public List<String> file_id_list;
        public string sha1 = "";
        public long size = 0;
        public string file_name = "";
        public string FullName = "";
        private TaskType _type;
        private string _name;
        private int _step;
        private int _status = 0;
        public Task task;
        private CancellationTokenSource cancelTokenSource=new CancellationTokenSource();
        public CancellationToken cancelToken { get { return cancelTokenSource.Token; } }
        public void Cancel() { cancelTokenSource.Cancel(); }
        public long Position;
        public long Length;
        public string Foreground
        {
            get
            {
                if (_status == 1) return "#f8b800";
                if (_status == 2) return "#39f";
                return "#f60";
            }
        }

        public int Status//0 初始,1正在执行,2成功,3失败
        {
            set
            {
                _status = value;
                if (_status > 1) Step = 100;
                OnPropertyChanged("Foreground");
                TaskMange.StatusChange(this);
            }
            get
            {
                return _status;
            }
        }
        public TaskType Type
        {
            set
            {
                UpdateProperty(ref _type, value);
            }
            get
            {
                return _type;
            }
        }
        public string Name
        {
            set
            {
                UpdateProperty(ref _name, value);
            }
            get
            {
                return _name;
            }
        }
        public virtual int Step
        {
            set
            {
                UpdateProperty(ref _step, value);
            }
            get
            {
                return _step;
            }
        }
        private void UpdateProperty<T>(ref T properValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (object.Equals(properValue, newValue))
            {
                return;
            }
            properValue = newValue;

            OnPropertyChanged(propertyName);
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }


    public class DataItem : INotifyPropertyChanged
    {
        private string name;
        private bool isEnabled;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
