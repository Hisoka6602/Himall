using Himall.Application;
using Himall.Core;
using Himall.Core.Plugins;
using Himall.Core.Plugins.Payment;
using Himall.Service;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Web.App_Code.Common
{
    public class PaymentHelper
    {
        /// <summary>
        /// 支付完生成红包
        /// </summary>
        public static Dictionary<long, Entities.ShopBonusInfo> GenerateBonus(IEnumerable<long> orderIds, string urlHost)
        {
            var bonusGrantIds = new Dictionary<long, Entities.ShopBonusInfo>();
            string url = CurrentUrlHelper.GetScheme() + "://" + urlHost + "/m-weixin/shopbonus/index/";
            var bonusService = ObjectContainer.Current.Resolve<ShopBonusService>();
            var buyOrders = ObjectContainer.Current.Resolve<OrderService>().GetOrders(orderIds);
            foreach (var o in buyOrders)
            {
                var shopBonus = bonusService.GetByShopId(o.ShopId);
                if (shopBonus == null)
                {
                    continue;
                }
                if (shopBonus.GrantPrice <= o.TotalAmount)
                {
                    long grantid = bonusService.GenerateBonusDetail(shopBonus, o.Id, url, o.TotalAmount);
                    bonusGrantIds.Add(grantid, shopBonus);
                }
            }

            return bonusGrantIds;
        }

        /// <summary>
        /// 更改限时购销售量
        /// </summary>
        public static void IncreaseSaleCount(List<long> orderid)
        {
            if (orderid.Count == 1)
            {
                var service = ObjectContainer.Current.Resolve<LimitTimeBuyService>();
                service.IncreaseSaleCount(orderid);
            }
        }

        /// <summary>
        /// 通过provider获取可用的支付方式
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IEnumerable<Plugin<IPaymentPlugin>> GetPluginIdByProvider(string provider)
        {
            provider = provider.ToLower();
            var plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true);
            switch (provider)
            {
                case "h5":
                    plugins = plugins.Where(e => e.Biz.SupportPlatforms.Contains(PlatformType.Wap));
                    break;
                case "weixinsmallprog":
                    plugins = plugins.Where(e => e.Biz.SupportPlatforms.Contains(PlatformType.WeiXinSmallProg));
                    break;
                case "himall.plugin.oauth.weixin":
                    plugins = plugins.Where(e => e.Biz.SupportPlatforms.Contains(PlatformType.WeiXin));
                    break;
            }
            return plugins;
        }
    }
}