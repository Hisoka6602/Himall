using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class SyncOrderRecord 
    {
        public ObjectId Id { set; get; }
        /// <summary>
        /// 同步时间
        /// </summary>
        public DateTime SyncTime { set; get; }

        /// <summary>
        /// 最后同步的订单ID
        /// </summary>
        public long LastOrderId { set; get; }

        /// <summary>
        /// 更新表名
        /// </summary>
        public string TableName { set; get; }
    }
}
