using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp.aliyundrive
{
    class info_file : MagBase
    {
        public string code { get; set; }
        public string message { get; set; }

        public string drive_id { get; set; }
        public string domain_id { get; set; }
        public string file_id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string file_extension { get; set; }
        public string status { get; set; }
        public long size { get; set; }
        public string upload_id { get; set; }
        public string parent_file_id { get; set; }
        public string crc64_hash { get; set; }
        public string content_hash { get; set; }
        public string content_hash_name { get; set; }
        public string download_url { get; set; }
        public string fields { get; set; }

        public async Task<HttpResult<info_file>> get()
        {
            var client = new Hclient<info_file>();
            var r = await client.PostAsJsonAsync(DriveApi.file_create, new
            {
                drive_id=token.Instance.default_drive_id,
                parent_file_id,
                file_id,
                fields,
                type,
            });
            return r;
        }

    }
    class file : MagBase
    {
        public string code { get; set; }
        public string message { get; set; }
        public int limit { get; set; } = 20;
        public string marker { get; set; }
        public string drive_id { get; set; }
        public string query { get; set; }
        public string parent_file_id { get; set; }
        public string image_thumbnail_process { get; set; }
        public string image_url_process { get; set; }
        public string video_thumbnail_process { get; set; }
        public string fields { get; set; }
        public string order_by { get; set; }
        public string order_direction { get; set; } = "DESC";

        public List<info_file> items { get; set; }
        public string next_marker { get; set; }
        public async Task<string> getfolderxxxxx(string parent_file_id, string name)
        {
            var f = new file();
            f.query = "parent_file_id=\"" + parent_file_id + "\" and type=\"folder\" and name=\"" + name + "\" ";
            var r = await f.search();
            if (!r.Yes) return "";
            if (r.obj.items.Count > 0) return r.obj.items[0].file_id;
            var r2 = await new upload().folder(parent_file_id, name, null);
            if (!r2.Yes) return "";
            return "";
        }
        /* 获取目录id原理:直接创建目录,已存在则忽略创建直接返回id
{
    "parent_file_id": "root",
    "type": "folder",
    "file_id": "60476b1be0be64145d3a42ecb01a4c5a05dceea6",
    "domain_id": "bj29",
    "drive_id": "993274",
    "file_name": "sdddddd",
    "encrypt_mode": "none"
}
             */
        public async Task<HttpResult<file>> search()
        {
            //token.Instance.refresh_token = "xxxx";
            //var r = await token.Instance.refresh();

            var client = new Hclient<file>();
            if (drive_id == null) drive_id = token.Instance.default_drive_id;
            if (parent_file_id == null) parent_file_id = "root";
            var r = await client.PostAsJsonAsync(DriveApi.file_search, new
            {
                limit,
                marker,
                drive_id,
                image_thumbnail_process,
                image_url_process,
                video_thumbnail_process,
                order_by,
                query
            });
            return r;
        }
        public async Task<HttpResult<file>> search(string pfid, string name)
        {
            var client = new Hclient<file>();
            if (drive_id == null) drive_id = token.Instance.default_drive_id;
            if (parent_file_id == null) parent_file_id = "root";
            query = $"parent_file_id='{pfid}' and name = '{name}'";
            var r = await client.PostAsJsonAsync(DriveApi.file_search, new
            {
                limit,
                marker,
                drive_id,
                query,
                order_direction
            });
            return r;
        }

        public async Task<HttpResult<file>> list()
        {
            //token.Instance.refresh_token = "xxxx";
            //var r = await token.Instance.refresh();
            var client = new Hclient<file>();
            if (drive_id == null) drive_id = token.Instance.default_drive_id;
            if (parent_file_id == null) parent_file_id = "root";
            var r = await client.PostAsJsonAsync(DriveApi.file_list, new
            {
                limit,
                marker,
                drive_id,
                parent_file_id,
                image_thumbnail_process,
                image_url_process,
                video_thumbnail_process,
                fields,
                order_by,
                order_direction
            });
            return r;
        }
    }


    class File_part_info_list: I_File_part_info_list
    {
        public int part_number { get; set; }
        public long part_size { get; set; }
        public string etag { get; set; }
        public string upload_url { get; set; }
    }
    interface I_File_part_info_list
    {
        int part_number { get; set; }
        string etag { get; set; }
    }
    class upload : MagBase
    {
        public string code { get; set; }
        public string message { get; set; }
        public string parent_file_id { get; set; }
        public List<File_part_info_list> part_info_list { get; set; }
        public string upload_id { get; set; }
        public string upload_url { get; set; }
        public bool exist { get; set; }
        public bool rapid_upload { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public string file_id { get; set; }
        public string domain_id { get; set; }
        public string drive_id { get; set; }
        public string file_name { get; set; }
        public string encrypt_mode { get; set; }
        public string location { get; set; }


        public string name { get; set; }
        public string content_type { get; set; }
        public long size { get; set; }
        public string content_hash_name { get; set; }
        public string content_hash { get; set; }
        public bool ignoreError { get; set; }
        public string check_name_mode { get; set; }
        public string etag { get; set; }
        public async Task<string> getfolder(string parent_file_id, string name)
        {
            var r = await new upload().folder(parent_file_id, name, null);
            if (r.Yes) return r.obj.file_id;
            throw new Exception($"创建目录[{name}]失败:{r.Error}");
        }
        public async Task<HttpResult<upload>> create()
        {
            var client = new Hclient<upload>();
            var r = await client.PostAsJsonAsync(DriveApi.file_create, new
            {
                name,
                type,
                content_type,
                size,
                drive_id,
                parent_file_id,
                part_info_list,
                content_hash_name,
                content_hash,
                ignoreError,
                check_name_mode
            });
            return r;
        }
        public async Task<HttpResult<upload>> put(string url, Stream stream, TaskInfo task)
        {
            var client = new Hclient<upload>();
            var r = await client.PutAsJsonAsync(url, stream,task);
            if (r.success) etag = r.body;
            return r;
        }
        public async Task<HttpResult<upload>> folder(string file_id, string name, TaskInfo task)
        {
            var f = this;
            f.check_name_mode = "refuse";// ignore, auto_rename, refuse.
            f.drive_id = token.Instance.default_drive_id;
            f.name = name;
            f.parent_file_id = file_id;
            f.type = "folder";
            var client = new Hclient<upload>();
            var r = await client.PostAsJsonAsync(DriveApi.file_create, new
            {
                name,
                type,
                drive_id,
                parent_file_id,
                check_name_mode
            });
            return r;
        }
        public async Task<HttpResult<upload>> rename(string file_id, string name, TaskInfo task)
        {
            var f = this;
            f.check_name_mode = "refuse";// ignore, auto_rename, refuse.
            f.drive_id = token.Instance.default_drive_id;
            f.file_id = file_id;
            f.name = name;
            var client = new Hclient<upload>();
            var r = await client.PostAsJsonAsync(DriveApi.file_update, new
            {
                name,
                drive_id,
                file_id,
                check_name_mode
            });
            return r;
        }
        public async Task<HttpResult<upload>> share(List<String> file_id_list, string name, TaskInfo task)
        {
            var f = this;
            f.drive_id = token.Instance.default_drive_id;
            f.file_id = file_id;
            f.name = name;
            var client = new Hclient<upload>();
            
            var r = await client.PostAsJsonAsync(DriveApi.file_share, new
            {
                drive_id = drive_id,
                file_id_list = file_id_list,
                share_pwd = Guid.NewGuid().ToString().Substring(0, 4),
                expiration = "2030-04-15T17:13:58.720+08:00"
            });
            return r;
        }
        public async Task<HttpResult<upload>> Upload(string file_id, string name,TaskInfo task)
        {
            var f = this;
            f.check_name_mode = "ignore";// ignore, auto_rename, refuse.
            if (string.IsNullOrEmpty(task.sha1)|| task.size==0)
            {
                using (var stream = Util.GetFileStream(task.FullName))
                {
                    f.content_hash = Util.sha1(stream);
                    f.size = stream.Length;
                }
            }
            else
            {
                f.content_hash = task.sha1;
                f.size = task.size;
            }
            f.content_hash_name = "sha1";
            f.content_type = "application/octet-stream";
            f.drive_id = token.Instance.default_drive_id;
            f.ignoreError = false;
            f.name = name;
            f.parent_file_id = file_id;
            f.part_info_list = new List<File_part_info_list>();

            int part_number = 0;
            long chunk = 0;
            long chunkSize = 5242880;//分包上传,每块大小,5M=5242880
            /*
http://qr.js.cn/json.html
{
    "code": "InvalidResource.PartList",
    "message": "The resource part_list is not valid. part entity too small"
}
             */
            while (f.size > chunk)
            {
                chunk += chunkSize;
                if (chunk > f.size) chunkSize = f.size - chunk + chunkSize;
                part_number++;
                f.part_info_list.Add(new File_part_info_list { part_number= part_number, part_size = chunkSize });
            }
            f.type = "file";
            var r = await f.create();
            if (!r.success) return r;
            if (r.obj.rapid_upload|| r.obj.exist)
            {
                Console.WriteLine(r.body);
                //文件已存在,秒传思密达
                return r;
            }
            if (task.FullName == "") throw new Exception("无法秒传,需要文件上传");
            if (r.obj.part_info_list == null) throw new Exception("无需上传");
            List<Task> tasks = new List<Task>();
            long position = 0;
            task.Length = r.obj.part_info_list.Count;
            foreach (var item in r.obj.part_info_list)
            {
                TaskInfoUpload t = new TaskInfoUpload(task);
                t.Length = item.part_size;
                t.Position = position;
                position += t.Length;
                Console.WriteLine($"块上传,总:[{f.size}][{task.Length}],当前位置:[{t.Position}][{t.Length}],链接:{item.upload_url}");

                //tasks.Add(Task.Run(async() => {}));
                //不能用任务,会报错说上一部分还没传完,得按顺序传,估计服务器只能一个接一个组吧
                /*
  <Code>PartNotSequential</Code>
  <Message>For sequential multipart upload, you must upload or complete parts with sequential part number.</Message>
                 */

                var r2 = await PutTask(item.upload_url, task.FullName, r, t);
                if (!r2.success)
                {
                    if (r2.ok) return r2;
                    //if (!r.success) break;
                    //再给一次机会
                    task.Step -= t.Step;
                    r2 = await PutTask(item.upload_url, task.FullName, r, t);
                    if (!r2.success)
                    {
                        return r2;
                        //r = r2;
                        //task.Cancel();
                        //break;
                    }
                }
                item.etag = r2.body;
            }
            //Task.WaitAll(tasks.ToArray());
            //if (!r.success) return r;
            var rc = await r.obj.complete();
            Console.WriteLine(rc.body);
            if (rc.success || rc.ok) return rc;
            //不成功再组一次
            return await r.obj.complete();
        }
        async Task<HttpResult<upload>> PutTask(string url,string FullName, HttpResult<upload> r, TaskInfo t)
        {
            using (var stream = Util.GetFileStream(FullName))
            {
                return await r.obj.put(url, stream, t);
            }
        }
        public async Task<HttpResult<upload>> complete()
        {
            var client = new Hclient<upload>();
            var r = await client.PostAsJsonAsync(DriveApi.file_complete, new
            {
                drive_id,
                file_id,
                ignoreError,
                part_info_list=part_info_list.Select(o=> new { o.part_number, o.etag }),
                upload_id,
            });
            return r;
        }
        public string fields { get; set; }
    }
    class TaskInfoUpload: TaskInfo
    {
        private readonly TaskInfo _task;
        private readonly int _stepNum;
        private int step; 
        public TaskInfoUpload(TaskInfo task)
        {
            _task = task;
            _stepNum = (int)_task.Length;
        }
        public override int Step
        {
            set
            {
                var size = value / _stepNum;
                _task.Step += value - step;
                step = size;
            }
            get
            {
                return step;
            }
        }
    }
    class Type_batch_requests_body
    {
        public string drive_id { get; set; }
        public string file_id { get; set; }
    }
    class Type_batch_requests_headers
    {
        [JsonProperty("Content-Type")]
        public string ContentType { get; set; }
    }
    class Type_batch_requests
    {
        public Type_batch_requests_body body { get; set; }
        public Type_batch_requests_headers headers { get; set; }
        public string id { get; set; }
        public string method { get; set; }
        public string url { get; set; }
    }
    class batch : MagBase
    {
        public string code { get; set; }
        public string message { get; set; }
        public string resource { get; set; }
        public List<Type_batch_requests> requests { get; set; }
        public async Task<HttpResult<batch>> del(string id)
        {
            //token.Instance.refresh_token = "xxxx";
            //var r = await token.Instance.refresh();
            requests = new List<Type_batch_requests>();
            requests.Add(new Type_batch_requests
            {
                body = new Type_batch_requests_body
                {
                    drive_id = token.Instance.default_drive_id,
                    file_id = id,
                },
                headers = new Type_batch_requests_headers
                {
                    ContentType = "application/json"
                },
                id = id,
                method = "DELETE",
                url = "/file/delete"
            });
            var client = new Hclient<batch>();
            var r = await client.PostAsJsonAsync(DriveApi.batch, new
            {
                requests,
                resource = "file"
            });
            return r;
        }
    }
}
