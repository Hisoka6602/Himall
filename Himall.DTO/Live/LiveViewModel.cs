using Himall.CommonModel;
using Himall.Core;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Live
{
    public class LiveViewModel : LiveRoomInfo
    {

        public string ShopName { get; set; }

    

        /// <summary>
        /// 商品数
        /// </summary>
        public int ProductCount { get; set; }


        /// <summary>
        /// 商品列表
        /// </summary>
        public List<LiveProduct> ProductList { get; set; }

        /// <summary>
        /// 回放集合
        /// </summary>
        public List<string> RecordingUrlList { get; set; }

       
        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusDesc => Status.ToDescription();

       

        public string StartTimeStr => StartTime.ToString("yyyy-MM-dd HH:mm");

        public string EndTimeStr => EndTime.HasValue ? EndTime.Value.ToString("yyyy-MM-dd HH:mm") : "";

        /// <summary>
        /// 开播时间的文字提示
        /// </summary>
        public string StartTimeDesc
        {
            get
            {
                if (StartTime.Date == DateTime.Now.Date)
                    return "今天 " + StartTime.ToString("HH:mm");
                else if (StartTime.Date == DateTime.Now.AddDays(1).Date)
                    return "明天 " + StartTime.ToString("HH:mm");
                else if (StartTime.Date == DateTime.Now.AddDays(2).Date)
                    return "后天 " + StartTime.ToString("HH:mm");
                else
                    return StartTime.ToString("MM-dd HH:mm");
            }
        }
    }

}
