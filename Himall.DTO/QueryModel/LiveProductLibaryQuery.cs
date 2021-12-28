using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.LiveProductLibraryInfo;

namespace Himall.DTO.QueryModel
{
    public class LiveProductLibaryQuery : QueryBase
    {
        /// <summary>
        /// 商品索引ID
        /// </summary>
        public string ProIndexIds { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// 房间ID
        /// </summary>
        public long RoomId { get; set; }
        /// <summary>
        /// 排除指定房间已有商品
        /// </summary>
        public long FilterRoomId { get; set; }
        /// <summary>
        /// 查询的商品ID
        /// </summary>
        public string ProductIds { get; set; }
        /// <summary>
        /// 过滤指定商品ID
        /// </summary>
        public string FilterProductIds { get; set; }

        private List<int> _LiveAuditStatus = new List<int>();
        /// <summary>
        /// 审核状态
        /// </summary>
        public List<int> LiveAuditStatus
        {
            get { if (_LiveAuditStatus == null) { return new List<int>(); } else { return _LiveAuditStatus; } }
        }

        /// <summary>
        /// 审核状态
        /// </summary>
        public LiveProductAuditStatus? AuditStatus
        {
            get; set;
        }
        /// <summary>
        /// 是否已撤回商品
        /// </summary>
        public bool IsReCallProduct { get; set; }

        /// <summary>
        /// 是否可移除商品
        /// </summary>
        public bool IsCanMoveProduct { get; set; }
        /// <summary>
        /// 店铺ID
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 分类ID
        /// </summary>
        public long CategoryId { get; set; }

        public List<long> Categories { get; set; }
        /// <summary>
        /// 关键字
        /// </summary>
        public string Keywords { get; set; }

    }
}
