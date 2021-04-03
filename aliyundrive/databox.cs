using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aliyundrive_Client_CSharp.aliyundrive
{
    class Databox_personal_rights_info
    {
        public string is_expires { get; set; }
        public string name { get; set; }
        public List<Databox_privileges> privileges { get; set; }
        public string spu_id { get; set; }

    }
    class Databox_privileges
    {
        public string feature_id { get; set; }
        public string feature_attr_id { get; set; }
        public string speed_limit { get; set; }
        public long quota { get; set; }
    }
    class Databox_personal_space_info
    {
        public long used_size { get; set; }
        public long total_size { get; set; }

    }
    class databox : Config<databox>, MagBase
    {
        public string access_token { get; set; }
        public Databox_personal_rights_info personal_rights_info { get; set; }
        public Databox_personal_space_info personal_space_info { get; set; }

        public string code { get; set; }
        public string message { get; set; }
        public async Task<HttpResult<databox>> get_personal_info(bool redirect = false)
        {
            var client = new Hclient<databox>();
            var r = await client.PostAsJsonAsync(DriveApi.databox_get_personal_info, new { });

            if (r.success)
            {
                databox.Instance = r.obj;
                //databox.Instance.Config_Save();不保存这个了
            }
            else
            {
                Console.WriteLine($"获取个人信息失败:{r.Error}");
            }
            return r;
        }

    }
}
