using Himall.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.ProductInfo;
using Himall.Core;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 商品缓存对象
    /// </summary>
    public class ProductData
    {
        /// <summary>
        /// 阶梯价
        /// </summary>
        [JsonIgnore]
        public List<ProductLadderPriceData> LadderPrice { get; set; }
        /// <summary>
        /// 商品详情
        /// </summary>
        [JsonIgnore]
        public ProductDescriptionData Description { get; set; }

        [JsonIgnore]
        public ProductVirtualData VirtualData { get; set; }

        [JsonIgnore]
        public List<SkuData> Skus { get; set; }


        /// <summary>
        /// 商品Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 店铺ID
        /// </summary>
        public long ShopId { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        public long CategoryId { get; set; }

        /// <summary>
        /// 分类路径
        /// </summary>
        public string CategoryPath { get; set; }

        /// <summary>
        /// 商品类型(0=实物商品，1=虚拟商品)
        /// </summary>
        public sbyte ProductType { get; set; }

        /// <summary>
        /// 类型ID
        /// </summary>
        public long TypeId { get; set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        public long BrandId { get; set; }

        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 商品编号
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// 广告词
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// 销售状态
        /// </summary>
        public ProductSaleStatus SaleStatus { get; set; }

        /// <summary>
        /// 审核状态
        /// </summary>
        public ProductAuditStatus AuditStatus { get; set; }

        /// <summary>
        /// 存放图片的目录
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// 市场价
        /// </summary>
        public decimal MarketPrice { get; set; }

        /// <summary>
        /// 最小销售价
        /// </summary>
        public decimal MinSalePrice { get; set; }

        /// <summary>
        /// 是否有SKU
        /// </summary>
        public bool HasSKU { get; set; }

        /// <summary>
        /// 浏览次数
        /// </summary>
        public long VistiCounts { get; set; }

        public bool ValidityType { get; set; }

        /// <summary>
        /// 运费模板ID
        /// </summary>
        public long FreightTemplateId { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 计量单位
        /// </summary>
        public string MeasureUnit { get; set; }

        /// <summary>
        /// 是否已删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 最大购买数
        /// </summary>
        public int MaxBuyCount { get; set; }

        /// <summary>
        /// 是否开启阶梯价格
        /// </summary>
        public bool IsOpenLadder { get; set; }

        /// <summary>
        /// 颜色别名
        /// </summary>
        public string ColorAlias { get; set; }

        /// <summary>
        /// 尺码别名
        /// </summary>
        public string SizeAlias { get; set; }

        /// <summary>
        /// 版本别名
        /// </summary>
        public string VersionAlias { get; set; }

        /// <summary>
        /// 虚拟销量
        /// </summary>
        public long VirtualSaleCounts { get; set; }

        /// <summary>
        /// 商品主图视频
        /// </summary>
        public string VideoPath { get; set; }

        /// <summary>
        /// 最后商品修改时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        protected string ImageServerUrl = "";
        /// <summary>
        /// 图片路径
        /// </summary>
        public string ImagePathUrl
        {
            get { return Core.HimallIO.GetImagePath(ImagePath); }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(ImageServerUrl))
                    ImagePath = value.Replace(ImageServerUrl, "");
                else
                    ImagePath = value;
            }
        }

        /// <summary>
        /// ImagePath路径，把“//”保证只一个“/”
        /// </summary>
        public string RelativePath
        {
            get { return string.IsNullOrEmpty(ImagePath) ? "" : ImagePath.Replace("//", "/"); }
        }

        /// <summary>
        /// 商品显示状态文本
        /// </summary>
        public string ShowProductState
        {
            get
            {
                string result = "错误数据";
                if (this != null)
                {
                    if (this.AuditStatus == ProductInfo.ProductAuditStatus.WaitForAuditing)
                    {
                        result = (this.SaleStatus == ProductInfo.ProductSaleStatus.OnSale ? ProductInfo.ProductAuditStatus.WaitForAuditing.ToDescription() :
                ProductInfo.ProductSaleStatus.InStock.ToDescription());
                    }
                    else
                    {
                        result = (this.AuditStatus == ProductAuditStatus.Audited && this.SaleStatus != ProductSaleStatus.OnSale) ? this.SaleStatus.ToDescription() : this.AuditStatus.ToDescription();
                    }
                }
                return result;
            }
        }
    }
}
