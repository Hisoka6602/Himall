using Himall.CommonModel;
using Himall.DTO.QueryModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class LiveQuery : QueryBase
    {
        public long RoomId { get; set; }
        public string Name { get; set; }
        public long? ShopId { get; set; }

        public string ShopName { get; set; }
        public string AnchorName { get; set; }

        public string ProductName { get; set; }

        public LiveRoomStatus? Status { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }
        /// <summary>
        /// 直播间Id(本地Id)
        /// </summary>
        public List<long> ids { get; set; }

        private List<int> _statusList = new List<int>();
        public List<int> StatusList { get { return _statusList; } set { _statusList = value; } }
    }
}
