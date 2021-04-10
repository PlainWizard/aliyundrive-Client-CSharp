using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp.aliyundrive
{
    class token : Config<token>, MagBase
    {
        public string code { get; set; }
        public string message { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public long expires_in { get; set; }
        public string user_id { get; set; }
        public string user_name { get; set; }
        public string avatar { get; set; }
        public string nick_name { get; set; }
        public string default_drive_id { get; set; }
        public string default_sbox_drive_id { get; set; }
        public string state { get; set; }
        public string role { get; set; }
        public bool pin_setup { get; set; }
        public bool is_first_login { get; set; }
        public bool need_rp_verify { get; set; }
        public string device_id { get; set; }
        public static void SetToken(string str)
        {
            //"refresh_token":"72c8fa104efe4536816165dcebf1619c"
            var m=Regex.Match(str, "\"refresh_token\":\"(.+?)\"");
            if (!m.Success)
            {
                m = Regex.Match(str, "refresh_token: \"(.+?)\"");
            }
            Instance.refresh_token = m.Success ? m.Groups[1].Value : "";
        }
        public async Task<HttpResult<token>> Refresh()
        {
            Console.WriteLine($"正在刷新access_token");
            var client = new Hclient<token>();
            var r = await client.PostAsJsonAsync(DriveApi.token_refresh, new 
            {
                refresh_token
            },null,true);
            if (r.success)
            {
                Console.WriteLine($"获取access_token成功");
                Instance = r.obj;
                Instance.Config_Save();
                Console.WriteLine($"获取access_token成结束:{Instance.access_token},旧:{access_token}");
            }
            else
            {
                Console.WriteLine($"获取access_token失败:{r.body}");
            }
            return r;
        }
    }
}
