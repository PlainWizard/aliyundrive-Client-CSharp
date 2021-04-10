using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp.aliyundrive
{
    class DriveApi
    {
        public static string api = "https://api.aliyundrive.com/v2";
        public static string apiToken = "https://websv.aliyundrive.com";

        public static string token_refresh = apiToken + "/token/refresh";
        public static string file_list = api + "/file/list";
        public static string file_complete = api + "/file/complete";
        public static string file_search = api + "/file/search";
        public static string file_create = api + "/file/create";
        public static string file_update = api + "/file/update";
        public static string file_get = api + "/file/get";
        public static string file_share = api + "/share_link/create";
        public static string batch = api + "/batch";
        public static string databox_get_personal_info = api + "/databox/get_personal_info";
    }
    class Util
    {
        private static string _qrapi;
        public static string QRapi { get {
                if (_qrapi != null) return _qrapi;
                try
                {
                    _qrapi = "https://gqrcode.alicdn.com/img?type=hv&text=";
                    if (!new HttpClient().GetAsync(_qrapi + "qr").Result.IsSuccessStatusCode)
                        throw new Exception();
                }
                catch (Exception)
                {
                    _qrapi= "http://qr.js.cn/api/qr?qr=";
                }
                Console.WriteLine("_qrapi:{0}", _qrapi);
                return _qrapi;
            }
        }
        public static string GetQrUrl(string url)
        {
            return QRapi+ Encode(url);
        }
        public static string Encode(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9]", delegate (Match match) { return "%" + BitConverter.ToString(Encoding.UTF8.GetBytes(match.Value)).Replace("-", "%"); });
        }
        public string ConfigName = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        public static string sha1(string data)
        {
            return BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "");
        }
        public static string sha1(byte[] data)
        {
            return BitConverter.ToString(SHA1.Create().ComputeHash(data)).Replace("-", "");
        }
        public static string sha1(Stream stream)
        {
            return BitConverter.ToString(SHA1.Create().ComputeHash(stream)).Replace("-", "");
        }
        public static string sha1File(string FullName)
        {
            using (var stream = GetFileStream(FullName))
                return BitConverter.ToString(SHA1.Create().ComputeHash(stream)).Replace("-", "");
        }

        public static string Bin2Hex(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }
        public static byte[] Hex2Bin(string hexString)
        {
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }


        public static Stream GetFileStream(string path)
        {
            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        public static string GetShowSize(long size)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            long mod = 1024;
            int i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }
            return size + units[i];
        }
    }

    public static class MyExceptionEx
    {
        public static string Error(this Exception ex)
        {
            return GetFirstException(ex).Message;
        }
        public static Exception GetFirstException(Exception ex)
        {
            if (ex.InnerException == null) return ex;
            return GetFirstException(ex.InnerException);
        }
    }
    public class Config<T> where T : new()
    {
        protected static object lock_obj { set; get; } = new object();
        protected static string AppDataPath { set; get; }
        static Config()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyProductAttribute product = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            AppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlainWizard",
                product.Product
                 );
            if (Directory.Exists(AppDataPath) == false)
            {
                Directory.CreateDirectory(AppDataPath);
            }
        }
        public Config()
        {
        }
        public void Config_Save()
        {
            try
            {
                lock (lock_obj) File.WriteAllText($"{AppDataPath}\\.Config.{typeof(T).FullName}.ini", JsonConvert.SerializeObject(this));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
        public void Config_Save(string name)
        {
            lock (lock_obj) File.WriteAllText($"{AppDataPath}\\.Config.custom.{name}.ini", JsonConvert.SerializeObject(this));
        }
        public static T Config_Get(string name)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText($"{AppDataPath}\\.Config.custom.{name}.ini"));
            }
            catch
            {
                return new T();
            }
        }
        protected static T _Instance;
        public static T Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (lock_obj)
                    {
                        if (_Instance == null)
                        {
                            try
                            {
                                _Instance = JsonConvert.DeserializeObject<T>(File.ReadAllText($"{AppDataPath}\\.Config.{typeof(T).FullName}.ini"));
                            }
                            catch (Exception ex) { Console.WriteLine($"获取配置错误:{ex.Message}"); }
                        }
                        if (_Instance == null) _Instance = new T();
                    }
                }
                return _Instance;
            }
            set
            {
                _Instance = value;
            }
        }
    }
}