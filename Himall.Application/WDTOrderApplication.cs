using Hidistro.Core;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Himall.Entities.OrderInfo;

namespace Himall.Application
{
    public class WDTOrderApplication : BaseApplicaion<OrderService>
    {

        private static OrderService _OrderService = ObjectContainer.Current.Resolve<OrderService>();
        private static WDTOrderService _WDTOrderService = ObjectContainer.Current.Resolve<WDTOrderService>();
        static WDTOrderApplication()
        {
            PushWangDianTongOrder();
            // 每5分钟自动推送到订单

        }

        public static void SyncOrderSendGoodsStatus()
        {
            WDTConfigModel setting = GetConfigModel();
            if (WdtParamIsValid(setting))
            {
                _WDTOrderService.SyncOrderSendGoodsStatus(GetConfigModel());
            }
        }

        static void PushWangDianTongOrder()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                WDTConfigModel setting = GetConfigModel();
                if (WdtParamIsValid(setting))
                {
                    _WDTOrderService.PushWangDianTongOrder(setting, ShopApplication.GetSelfShop().Id);
                }
                else
                {
                    Log.Info($"旺店通参数配置验证错误，是否开启：{setting.OpenErp}，库存同步：{setting.OpenErpStock},ErpAppkey:{setting.ErpAppkey},ErpAppsecret:{setting.ErpAppsecret},ErpPlateId:{setting.ErpPlateId},ErpSid:{setting.ErpSid},ErpStoreNumber:{setting.ErpStoreNumber},ErpUrl:{setting.ErpUrl}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("推送订单异常：" + ex);
            }
        }

    }
}
