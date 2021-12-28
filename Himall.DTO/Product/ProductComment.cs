using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
	public class ProductComment
	{
		public long Id { get; set; }
		public long ProductId { get; set; }
		public long ShopId { get; set; }
		public string ShopName { get; set; }
		public long UserId { get; set; }
		public string UserName { get; set; }
		public string Email { get; set; }
		public string ReviewContent { get; set; }
		public System.DateTime ReviewDate { get; set; }
		public string ReplyContent { get; set; }
		public Nullable<System.DateTime> ReplyDate { get; set; }
		public int ReviewMark { get; set; }
		public long SubOrderId { get; set; }
		public string AppendContent { get; set; }
		public Nullable<System.DateTime> AppendDate { get; set; }
		public string ReplyAppendContent { get; set; }
		public Nullable<System.DateTime> ReplyAppendDate { get; set; }
		public Nullable<bool> IsHidden { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// 规格编号
        /// </summary>
        public string SkuId { get; set; }
        /// <summary>
        /// 规格内容
        /// </summary>
        public string SKU { get; set; }
        public List<ProductCommentImage> Images { get; set; }
	}

	public class ProductCommentImage
	{
		public long Id { get; set; }
		public string CommentImage { get; set; }
		public long CommentId { get; set; }
		public int CommentType { get; set; }
	}

	public class ProductCommentModelDTO
	{
		/// <summary>
		/// 头像
		/// </summary>
		public string Picture { get; set; }
		public string UserName { get; set; }
		public string ReviewContent { get; set; }
		public string ReviewDate { get; set; }
		public string ReplyContent { get; set; }
		public string ReplyDate { get; set; }
		public int ReviewMark { get; set; }
		public string AppendContent { get; set; }
		public string AppendDate { get; set; }
		public string ReplyAppendContent { get; set; }
		public string ReplyAppendDate { get; set; }

		/// <summary>
		/// 购买时间
		/// </summary>
		public string BuyDate { get; set; }

		/// <summary>
		/// 订单完成时间
		/// </summary>
		public string FinishDate { get; set; }

		public string Color { get; set; }
		public string Version { get; set; }
		public string Size { get; set; }
		public string ColorAlias { get; set; }
		public string SizeAlias { get; set; }
		public string VersionAlias { get; set; }

		public string Sku { 
			get {
				List<string> arrstr = new List<string>();
				if (!string.IsNullOrEmpty(this.Color))
					arrstr.Add(" " + this.ColorAlias + "：" + this.Color);
				if (!string.IsNullOrEmpty(this.Size))
					arrstr.Add(" " + this.SizeAlias + "：" + this.Size);
				if (!string.IsNullOrEmpty(this.Version))
					arrstr.Add(" " + this.VersionAlias + "：" + this.Version);

				if (arrstr.Count > 0)
					return string.Join(";", arrstr.ToArray());
				else
					return string.Empty;
			}
		}

		public List<ProductCommentImageInfo> Images { get; set; }
		public List<ProductCommentImageInfo> AppendImages { get; set; }
	}
}
