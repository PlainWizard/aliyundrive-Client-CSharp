using aliyundrive_Client_CSharp.aliyundrive;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp
{
    class FapShare : MagBase
    {
        public string code { get; set; }
        public string message { get; set; }
        public string id { get; set; }
    }
    class Fap
    {
        public string ver { get; set; }
        public string des { get; set; }
        public string tab { get; set; }
        public string pwd { get; set; }
        public long expires { get; set; }
        public string id { get; set; }
        public string user { get; set; }
        public string flag { get; set; }
        public List<FapInfo> list { get; set; }
        public static Fap FAP_get(string hex)
        {
            try
            {
                return JsonConvert.DeserializeObject<Fap>(Encoding.UTF8.GetString(Util.Hex2Bin(hex)));
            }
            catch { }
            return null;
        }
        public static string FAP_set(Fap fap)
        {
            try
            {
                return Util.Bin2Hex(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(fap)));
            }
            catch { }
            return null;
        }
        public static void FAP_rapid_upload(Fap fap,string fid, Action<Action> invoke)
        {
            foreach (var item in fap.list)
            {
                //dir 所在相对当前路径的目录 格式为 一层目录(aaa/) 二层目录(aaa/bbb/)
                if (string.IsNullOrEmpty(item.dir))
                {
                    invoke(() => {
                        TaskMange.Add(new TaskInfo { Type = TaskType.上传, Name = item.name, sha1 = item.sha1, size = item.size, parent_file_id = fid });
                    });
                }
                else
                {
                    Task.Run(async () => {
                        try
                        {
                            var u = new upload();
                            var ms = Regex.Matches(item.dir, "([^/]+?)/");
                            if (ms.Count == 0) throw new Exception("目录错误");

                            string _fid = fid;
                            if (!AsyncTaskMange.Instance.ParentIDs.ContainsKey(item.dir))
                            {
                                foreach (Match m in ms)
                                {
                                    Console.WriteLine($"fap创建目录[{item.dir}]:{m.Groups[1].Value}");
                                    _fid = await u.getfolder(_fid, m.Groups[1].Value);
                                }
                                AsyncTaskMange.Instance.ParentIDs[item.dir] = _fid;
                            }
                            _fid = AsyncTaskMange.Instance.ParentIDs[item.dir];
                            invoke(() => {
                                TaskMange.Add(new TaskInfo { Type = TaskType.上传, Name = item.name, sha1 = item.sha1, size = item.size, parent_file_id = _fid });
                            });
                        }
                        catch (Exception ex)
                        {
                            invoke(() => {
                                TaskMange.Add(new TaskInfo { Type = TaskType.上传, Status = 3, Name = item.name + $"[{ex.Error()}]", sha1 = item.sha1, size = item.size, parent_file_id = fid });
                            });
                        }
                    });
                }
            }

        }
    }
    class FapInfo
    {
        public string name { get; set; }
        public string author { get; set; }
        public long size { get; set; }
        public string sha1 { get; set; }
        public string md5 { get; set; }
        public string c1 { get; set; }
        public string c2 { get; set; }
        public string c3 { get; set; }
        public string url { get; set; }
        public string tmp { get; set; }
        public string ext { get; set; }
        public string type { get; set; }
        public string mime { get; set; }
        public string dir { get; set; }
        public long created { get; set; }
        public long updated { get; set; }
    }
}
