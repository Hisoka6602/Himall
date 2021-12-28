using Himall.Application;
using Himall.DTO;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 页面基类型
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class MobileWebViewPage<TModel> : System.Web.Mvc.WebViewPage<TModel>
    {
		public SiteSettings SiteSetting
		{
			get
			{
				return SiteSettingApplication.SiteSettings;
			}
		}

        /// <summary>
        /// 当前用户信息
        /// </summary>
        public Entities.MemberInfo CurrentUser
        {
            get
			{
				if (this.ViewContext.Controller is BaseController)
					return ((BaseController)this.ViewContext.Controller).CurrentUser;
				return BaseController.GetUser(Request);
            }
        }

		public CommonModel.Model.WeiXinShareArgs WeiXinShareArgs
		{
			get
			{
				if (this.ViewContext.Controller is BaseMobileController)
					return ((BaseMobileController)this.ViewContext.Controller).WeiXinShareArgs;
				return null;
			}
		}

        public string CurrentAreaName
        {
            get
            {
                string result = "m";
                result = ViewBag.AreaName;
                return result;
            }
        }
    }
    /// <summary>
    /// 页面基类型
    /// </summary>
    public abstract class MobileWebViewPage : MobileWebViewPage<dynamic>
    {

    }
}
