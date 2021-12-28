using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.LiveProductLibraryInfo;

namespace Himall.DTO.Live
{
    public class LiveProductLibary
    {   
        /// <summary>
        /// 商品ID
        /// </summary>
        public long ProductId
        {
            get; set;
        }



        /// <summary>
        /// 店铺ID
        /// </summary>

        public long ShopId
        {
            get; set;
        }



        /// <summary>
        /// 审核状态
        /// </summary>
        public LiveProductAuditStatus LiveAuditStatus
        {

            get; set;
        }


        /// <summary>
        /// 小程序上传图片MeidaId
        /// </summary>

        public string ImageMediaId
        {
            get; set;
        }



        /// <summary>
        /// 小程序直播商品库商品ID
        /// </summary>
        public long GoodsId
        {
            get; set;
        }



        /// <summary>
        /// 小程序直播商品库审核单ID
        /// </summary>
        public long AuditId
        {
            get; set;
        }

        /// <summary>
        /// 提交申请直播商品库时间 用于判断商品图片的MediaId是否还有效
        /// </summary>
        public DateTime? ApplyLiveTime
        {
            get; set;
        }

        /// <summary>
        /// 小程序直播商品审核消息
        /// </summary>
        public string LiveAuditMsg
        {

            get; set;
        }
    }
}
