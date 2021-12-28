using Himall.CommonModel;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Application
{
    public class GiftsOrderApplication : BaseApplicaion<GiftsOrderService>
    {
        public static int GetOrderCount(GiftsOrderQuery query)
        {
            return Service.GetOrderCount(query);
        }

        public static int GetOwnBuyQuantity(long userid, long giftid)
        {
            return Service.GetOwnBuyQuantity(userid, giftid);
        }
        public static GiftOrderInfo CreateOrder(GiftOrderModel model)
        {
            return Service.CreateOrder(model);
        }
        public static QueryPageModel<GiftOrderInfo> GetOrders(GiftsOrderQuery query)
        {
            return Service.GetOrders(query);
        }
        public static List<GiftOrderItemInfo> GetOrderItemByOrder(long id)
        {
            return Service.GetOrderItemByOrder(id);
        }

        public static int GetOrderCount(GiftOrderInfo.GiftOrderStatus? status, long userId = 0)
        {
            return Service.GetOrderCount(status, userId);
        }
        public static GiftOrderInfo GetOrder(long orderId, long userId)
        {
            return Service.GetOrder(orderId, userId);
        }
        public static void ConfirmOrder(long id, long userId)
        {
            Service.ConfirmOrder(id, userId);
        }
    }
}
