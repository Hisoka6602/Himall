using Himall.Application;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.OpenApi.Model.Parameter;
using Himall.OpenApi.Model.Parameter.Message;
using Himall.Web.Framework;
using Hishop.Open.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Himall.OpenApi
{
    /// <summary>
    /// 消息接口
    /// </summary>
    [RoutePrefix("OpenApi")]
    public class MessageController : HiOpenAPIController
    {

        [HttpPost]
        public object SendMessageOnDistributorCommissionSettled(DistributorCommissionSettledArgs args)
        {
            MessageApplication.SendMessageOnDistributorCommissionSettled(args.UserId, args.Amount, args.SettlementDate);
            return new { success = true };
        }

        [HttpPost]
        public object SendMessageOnRefundDeliver(RefundDeliverArgs args)
        {
            MessageApplication.SendMessageOnRefundDeliver(args.UserId, args.Info, args.RefundId);
            return new { success = true };
        }

     
 
        public object SendMessageOrderWaitPay(OrderApply args)
        {
            MessageApplication.SendMessageOnOrderCreate(args.UserId, args.Info);
            return new { success = true };
        }

        [HttpPost]
        public object ConfirmRefund(ConfirmRefund args)
        {
            string notifyurl = CurrentUrlHelper.CurrentUrlNoPort() + "/Pay/RefundNotify/{0}";
            var result = RefundApplication.ConfirmRefund(args.RefundId, args.Remark, "", notifyurl);
            return new { success = true };
        }
    }
}
