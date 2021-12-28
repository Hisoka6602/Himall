using Himall.Application;
using Himall.DTO;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 页面基类型
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class WebViewPage<TModel> : System.Web.Mvc.WebViewPage<TModel>
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
        public string Generator
        {
            get
            {
                return "3.4.6";
            }
        }
    }
    /// <summary>
    /// 页面基类型
    /// </summary>
    public abstract class WebViewPage : WebViewPage<dynamic>
    {

    }
}
