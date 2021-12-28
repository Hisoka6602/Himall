using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel
{
    public class WDTConfigModel
    {
        public string ErpUrl { get; set; }
        public string ErpSid { get; set; }
        public string ErpAppkey { get; set; }
        public string ErpAppsecret { get; set; }
        public string ErpStoreNumber { get; set; }
        /// <summary>
        /// 是否开启订单推送
        /// </summary>
        public bool OpenErp { get; set; }
        /// <summary>
        /// 是否库存同步
        /// </summary>
        public bool OpenErpStock { get; set; }
        public string ErpPlateId { get; set; }
    }
}

