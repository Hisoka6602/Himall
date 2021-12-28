using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Live
{
    public class LiveRoom
    {
        public string Name { get; set; }
        public long RoomId { get; set; }

        public long ShopId { get; set; }

        public string CoverImg { get; set; }
        public LiveRoomStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string AnchorName { get; set; }
        public string AnchorImg { get; set; }
        public List<LiveProduct> Products { get; set; }
		/// <summary>
		/// 分享图片地址
		/// </summary>

		
		public string ShareImg
		{
			get; set;
		}


		/// <summary>
		/// 分享图片上传小程序端MediaId
		/// </summary>
		public string ShareImgMediaId
		{
			get;set;
		}
		
		/// <summary>
		/// 主播微信号
		/// </summary>

		public string AnchorWechat
		{
			get;set;
		}


		/// <summary>
		/// 直播间类型 【1: 推流，0：手机直播】
		/// </summary>

		
		public int Type
		{
			get; set;
		}

		/// <summary>
		/// 横屏、竖屏 【1：横屏，0：竖屏】（横屏：视频宽高比为16:9、4:3、1.85:1 ；竖屏：视频宽高比为9:16、2:3）
		/// </summary>


		public int ScreenType
		{
			get; set;
		}

		/// <summary>
		/// 创建时间
		/// </summary>


		public DateTime CreateTime
		{
			get; set;
		}


		/// <summary>
		/// 是否关闭评论（若关闭，直播开始后不允许开启）
		/// </summary>


		public int CloseComment
		{
			get; set;
		}


		/// <summary>
		/// 横屏、竖屏 【1：横屏，0：竖屏】（横屏：视频宽高比为16:9、4:3、1.85:1 ；竖屏：视频宽高比为9:16、2:3）
		/// </summary>


		public int CloseGoods
		{
			get; set;
		}


		/// <summary>
		/// 是否关闭点赞（若关闭，直播开始后不允许开启）
		/// </summary>


		public int CloseLike
		{
			get; set;
		}

		/// <summary>
		/// 封面图，填入mediaID（mediaID获取后，三天内有效）
		/// </summary>


		public string CoverImgMediaId
		{
			get; set;
		}
	}
}
