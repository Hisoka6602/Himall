using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;

namespace Himall.Application
{
    public class BaseApplicaion
    {
        protected static T GetService<T>() where T : ServiceBase
        {
            return ObjectContainer.Current.Resolve<T>();
        }
        /// <summary>
        /// 验证旺店通参数是否可用
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public static bool WdtParamIsValid(WDTConfigModel setting)
        {
            if (!setting.OpenErp || string.IsNullOrEmpty(setting.ErpAppkey) || string.IsNullOrEmpty(setting.ErpAppsecret) || string.IsNullOrEmpty(setting.ErpSid) || string.IsNullOrEmpty(setting.ErpUrl))
            {
                return false;
            }
            return true;
        }

        public static WDTConfigModel GetConfigModel()
        {
            SiteSettings setting = SiteSettingApplication.SiteSettings;
            return new WDTConfigModel()
            {
                ErpAppkey = setting.ErpAppkey,
                ErpAppsecret = setting.ErpAppsecret,
                ErpSid = setting.ErpSid,
                ErpStoreNumber = setting.ErpStoreNumber,
                ErpUrl = setting.ErpUrl,
                OpenErp = setting.OpenErp,
                OpenErpStock = setting.OpenErpStock,
                ErpPlateId = setting.ErpPlateId

            };
        }
    }

    public class BaseApplicaion<T> : BaseApplicaion where T : ServiceBase
    {
        protected static T Service { get { return GetService<T>(); } }
    }
}
