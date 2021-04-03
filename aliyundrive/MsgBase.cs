using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp.aliyundrive
{
    interface MagBase
    {
        string code { get; set; }
        string message { get; set; }

    }
    class HttpResult<T>
    {
        public string code { get; set; }
        public string message { get; set; }
        public bool success { get; set; }
        public bool ok { get; set; }
        public string body { get; set; }
        public string err { get; set; }
        public T obj { get; set; }
        public string Error { get { return ok ? message : err; } }
        public bool Yes { get { return ok && success; } }

    }
}
