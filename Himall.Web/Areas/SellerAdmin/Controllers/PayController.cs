using Himall.Core;
using Himall.Core.Plugins.Payment;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Himall.DTO.QueryModel;

using Himall.Service;
using Himall.Application;
using Himall.CommonModel;
using Himall.Entities;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class PayController : BaseController
    {

        /// <summary>
        /// 平台充值同步回调
        /// </summary>
        /// <param name="id">充值订单号</param>
        /// <param name="balance">充值金额</param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult CapitalChargeReturn(string id)
        {
            id = DecodePaymentId(id);
            Log.Info("商家充值同步回调key：" + id);
            string error = string.Empty;

            try
            {
                var payment = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(id);
                var payInfo = payment.Biz.ProcessReturn(HttpContext.Request);
                CashDepositDetailInfo model = new CashDepositDetailInfo();
                string payStateKey = CacheKeyCollection.PaymentState(string.Join(",", payInfo.OrderIds));//获取支付状态缓存键
                bool result = Cache.Get<bool>(payStateKey);//记录缓存，不重复处理
                if (!result)
                {
                    long orderIds = payInfo.OrderIds.FirstOrDefault();
                    Log.Info("商家充值同步回调订单号：" + orderIds);
                    BillingApplication.ShopRecharge(orderIds, payInfo.TradNo, id);

                    //写入支付状态缓存
                    Cache.Insert(payStateKey, true);//标记为已支付
                }
            }
            catch (Exception ex)
            {
                Log.Error("商家充值同步回调错误：" + ex.Message);
                error = ex.Message;
            }
            ViewBag.Error = error;
            return View();
        }


        /// <summary>
        /// 平台充值同步异步
        /// </summary>
        /// <param name="id">充值订单号</param>
        /// <param name="balance">充值金额</param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ContentResult CapitalChargeNotify(string id)
        {
            id = DecodePaymentId(id);
            Log.Info("商家充值异步回调key：" + id);
            string str = string.Empty;

            try
            {
                var payment = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(id);
                Log.Debug("payment="+payment.PluginInfo.Description);
                var payInfo = payment.Biz.ProcessNotify(HttpContext.Request);
                CashDepositDetailInfo model = new CashDepositDetailInfo();
                string payStateKey = CacheKeyCollection.PaymentState(string.Join(",", payInfo.OrderIds));//获取支付状态缓存键
                bool isPayed = false;
                bool isExist = Cache.Exists(payStateKey);
                if (isExist)
                {
                    isPayed = Cache.Get<bool>(payStateKey);//记录缓存，不重复处理
                }
                if (!isPayed)
                {
                    long orderIds = payInfo.OrderIds.FirstOrDefault();
                    Log.Info("商家充值异步回调订单号：" + orderIds);
                    BillingApplication.ShopRecharge(orderIds, payInfo.TradNo, id);

                    str = payment.Biz.ConfirmPayResult();
                    //写入支付状态缓存
                    Cache.Insert(payStateKey, true);//标记为已支付
                }
            }
            catch (Exception ex)
            {
                Log.Error("商家充值异步回调错误：" + ex.Message);
            }
            return Content(str);
        }

        /// <summary>
        /// 订单解密处理方法
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        string DecodePaymentId(string paymentId)
        {
            return paymentId.Replace("-", ".");
        }
    }
}