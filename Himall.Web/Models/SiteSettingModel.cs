using Himall.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace Himall.Web.Models
{
    public class SiteSettingModel
    {

        /// <summary>
        /// 站点名称
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// ICP编号
        /// </summary>
        public string ICPNubmer { get; set; }

        /// <summary>
        /// 客服联系电话
        /// </summary>
        public string CustomerTel { get; set; }

        /// <summary>
        /// 站点
        /// </summary>
        public bool SiteIsOpen { get; set; }

        /// <summary>
        /// 注册方式
        /// </summary>
        public RegisterTypes RegisterType { get; set; }
        /// <summary>
        /// 手机是否需验证
        /// </summary>
        public bool MobileVerifOpen { set; get; }
        /// <summary>
        /// 邮箱是否必填
        /// </summary>
        public bool RegisterEmailRequired { get; set; }
        /// <summary>
        /// 邮箱是否需要验证
        /// </summary>
        public bool EmailVerifOpen { set; get; }


        /// <summary>
        /// 微信AppId
        /// </summary>
        public string WeixinAppId { get; set; }

        /// <summary>
        /// 微信AppSecret
        /// </summary>
        public string WeixinAppSecret { get; set; }

        /// <summary>
        /// 微信token
        /// </summary>
        public string WeixinToKen { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string WeixinPartnerID { get; set; }

        public string WeixinPartnerKey { get; set; }

        public string WeixinLoginUrl { get; set; }

        public bool IsValidationService { get; set; }

        /// <summary>
        /// 商家入住协议
        /// </summary>
        public string SellerAdminAgreement { get; set; }

        /// <summary>
        /// 预付款百分比
        /// </summary>
        public decimal AdvancePaymentPercent { get; set; }

        /// <summary>
        /// 预存款上限
        /// </summary>
        public decimal AdvancePaymentLimit { get; set; }


        public string Logo
        {
            get;
            set;
        }

        public string MemberLogo
        {
            get;
            set;
        }
        /// <summary>
        /// 微信Logo
        /// <para>用于微信卡券，100*100，小于1M</para>
        /// </summary>
        public string WXLogo
        {
            get;
            set;
        }
        /// <summary>
        /// PC登录页左侧图片
        /// </summary>
        public string PCLoginPic
        {
            get;
            set;
        }

        public string QRCode
        {
            get;
            set;
        }

        public string FlowScript
        {
            get;
            set;
        }

        public string Site_SEOTitle
        {
            get;
            set;
        }

        public string Site_SEOKeywords
        {
            get;
            set;
        }

        public string Site_SEODescription
        {
            get;
            set;
        }

        /// <summary>
        /// 未付款超时(小时)
        /// </summary>        
        public int UnpaidTimeout { get; set; }

        /// <summary>
        /// 确认收货超时(天数)
        /// </summary>        
        public int NoReceivingTimeout { get; set; }
        /// <summary>
        /// 关闭评价通道时限(天数)
        /// </summary>
        public int OrderCommentTimeout { get; set; }

        /// <summary>
        /// 确认收货后可退货周期(天数)
        /// </summary>
        public int SalesReturnTimeout { get; set; }
        /// <summary>
        /// 售后-商家自动确认售后时限(天数)
        /// <para>到期未审核，自动认为同意售后</para>
        /// </summary>
        public int AS_ShopConfirmTimeout { get; set; }
        /// <summary>
        /// 售后-用户发货限时(天数)
        /// <para>到期未发货自动关闭售后</para>
        /// </summary>
        public int AS_SendGoodsCloseTimeout { get; set; }
        /// <summary>
        /// 售后-商家确认到货时限(天数)
        /// <para>到期未收货，自动收货</para>
        /// </summary>
        public int AS_ShopNoReceivingTimeout { get; set; }


        /// <summary>
        /// 收到红包微信提醒模板编号
        /// </summary>
        public string WX_MSGGetCouponTemplateId { get; set; }

        #region 商城App
        /// <summary>
        /// 商城app版本号
        /// </summary>
        public string AppVersion { get; set; }
        /// <summary>
        /// 商城安卓下载地址
        /// </summary>
        public string AndriodDownLoad { set; get; }
        /// <summary>
        /// 商城APP更新说明
        /// </summary>
        public string AppUpdateDescription { set; get; }
        /// <summary>
        /// 商城IOS下载地址
        /// </summary>
        public string IOSDownLoad { set; get; }

        /// <summary>
        /// 是否提供下载
        /// </summary>
        public bool CanDownload { set; get; }

        #endregion

        #region 商家APP
        /// <summary>
        /// 商家app版本号
        /// </summary>
        public string ShopAppVersion { get; set; }
        /// <summary>
        /// 商家安卓下载地址
        /// </summary>
        public string ShopAndriodDownLoad { set; get; }
        /// <summary>
        /// 商家APP更新说明
        /// </summary>
        public string ShopAppUpdateDescription { set; get; }
        /// <summary>
        /// 商家IOS下载地址
        /// </summary>
        public string ShopIOSDownLoad { set; get; }
        #endregion

        /// <summary>
        /// 快递100Key
        /// </summary>
        public string Kuaidi100Key { set; get; }

        /// <summary>
        /// 客服电话
        /// </summary>
        public string SitePhone { get; set; }

        /// <summary>
        /// 京东地址库APPKEY
        /// </summary>
        public string JDRegionAppKey { get; set; }
        /// <summary>
        /// 首页页脚
        /// </summary>
        public string PageFoot { get; set; }
        /// <summary>
        /// 底部服务图片
        /// </summary>
        public string PCBottomPic { get; set; }

        /// <summary>
        /// 是否强制绑定手机号
        /// </summary>

        public bool IsConBindCellPhone { get; set; }
        /// <summary>
        /// 是否可以清理演示数据
        /// </summary>
        public bool IsCanClearDemoData { get; set; }

        /// <summary>
        /// 腾讯地图APIKEY
        /// </summary>
        public string QQMapAPIKey { get; set; }

        /// <summary>
        /// 站点主域名
        /// </summary>
        public string SiteUrl { get; set; }

        /// <summary>
        /// 启用入驻微店
        /// </summary>
        public bool StartVShop { get; set; }

        #region 主题颜色
        /// <summary>
        /// 主要颜色
        /// </summary>
        public string PrimaryColor { get; set; }
        /// <summary>
        /// 主要字体颜色
        /// </summary>
        public string PrimaryTxtColor { get; set; }
        /// <summary>
        /// 次要颜色
        /// </summary>
        public string SecondaryColor { get; set; }
        /// <summary>
        /// 次要字体颜色
        /// </summary>
        public string SecondaryTxtColor { get; set; }
        #endregion

        #region  旺店通设置
        public bool OpenErp { get; set; }

        public bool OpenErpStock { get; set; }

        public string ErpUrl { get; set; }

        public string ErpSid { get; set; }

        public string ErpAppkey { get; set; }

        public string ErpAppsecret { get; set; }

        public string ErpStoreNumber { get; set; }

        public string ErpPlateId { get; set; }
        #endregion

    }
}