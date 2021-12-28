﻿using Himall.Application;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class GiftOrderController : BaseMemberController
    {
        private GiftService _iGiftService;
        private GiftsOrderService _iGiftsOrderService;
        private MemberService _MemberService;
        private ShippingAddressService _ShippingAddressService;
        private MemberGradeService _iMemberGradeService;
        public GiftOrderController(
            GiftService GiftService,
            GiftsOrderService GiftsOrderService,
            MemberService MemberService,
            ShippingAddressService ShippingAddressService,
            MemberGradeService MemberGradeService
            )
        {
            _iGiftService = GiftService;
            _iGiftsOrderService = GiftsOrderService;
            _MemberService = MemberService;
            _ShippingAddressService = ShippingAddressService;
            _iMemberGradeService = MemberGradeService;

        }
        /// <summary>
        /// 确认订单信息并提交
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public ActionResult SubmitOrder(long id, long? regionId, int count = 1)
        {
            #region 礼品信息判断
            //礼品信息
            var giftdata = GiftApplication.GetGift(id);
            if (giftdata == null)
            {
                throw new HimallException("错误的礼品编号！");
            }
            #endregion
            var data = new GiftOrderConfirmPageModel();
            var gorditemlist = new List<GiftOrderItemInfo>();
            var gorditem = new GiftOrderItemInfo(); //补充订单项
            gorditem.GiftId = giftdata.Id;
            gorditem.GiftName = giftdata.GiftName;
            gorditem.GiftValue = giftdata.GiftValue;
            gorditem.ImagePath = giftdata.ImagePath;
            gorditem.OrderId = 0;
            gorditem.Quantity = count;
            gorditem.SaleIntegral = giftdata.NeedIntegral;
            gorditemlist.Add(gorditem);

            data.GiftList = gorditemlist;

            data.GiftValueTotal = (decimal)data.GiftList.Sum(d => d.Quantity * d.GiftValue);
            data.TotalAmount = (int)data.GiftList.Sum(d => d.SaleIntegral * d.Quantity);

            //用户地址
            data.ShipAddress = GetShippingAddress(regionId);

            //顶部信息 Logo
            ViewBag.Logo = SiteSettingApplication.SiteSettings.Logo;//获取Logo
            ViewBag.Step = 2;

            ViewBag.Keyword = SiteSettings.Keyword;
            return View(data);
        }
        /// <summary>
        /// 提交并处理订单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="regionId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SubmitOrder(long id, long regionId, int count)
        {
            Result result = new Result() { success = false, msg = "未知错误", status = 0 };
            bool isdataok = true;

            if (count < 1)
            {
                isdataok = false;
                result.success = false;
                result.msg = "错误的兑换数量！";
                result.status = -8;

                return Json(result);
            }
            //Checkout
            List<GiftOrderItemModel> gorditemlist = new List<GiftOrderItemModel>();

            #region 礼品信息判断
            //礼品信息
            GiftInfo giftdata = _iGiftService.GetById(id);
            if (giftdata == null)
            {
                isdataok = false;
                result.success = false;
                result.msg = "礼品不存在！";
                result.status = -2;

                return Json(result);
            }

            if (giftdata.GetSalesStatus != GiftInfo.GiftSalesStatus.Normal)
            {
                isdataok = false;
                result.success = false;
                result.msg = "礼品已失效！";
                result.status = -2;

                return Json(result);
            }

            //库存判断
            if (count > giftdata.StockQuantity)
            {
                isdataok = false;
                result.success = false;
                result.msg = "礼品库存不足,仅剩 " + giftdata.StockQuantity.ToString() + " 件！";
                result.status = -3;

                return Json(result);
            }

            //积分数
            if (giftdata.NeedIntegral < 1)
            {
                isdataok = false;
                result.success = false;
                result.msg = "礼品关联等级信息有误或礼品积分数据有误！";
                result.status = -5;

                return Json(result);
            }
            #endregion

            #region 用户信息判断
            //限购数量
            if (giftdata.LimtQuantity > 0)
            {
                int ownbuynumber = _iGiftsOrderService.GetOwnBuyQuantity(CurrentUser.Id, id);
                if (ownbuynumber + count > giftdata.LimtQuantity)
                {
                    isdataok = false;
                    result.success = false;
                    result.msg = "超过礼品限兑数量！";
                    result.status = -4;

                    return Json(result);
                }
            }
            var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
            if (giftdata.NeedIntegral * count > userInte.AvailableIntegrals)
            {
                isdataok = false;
                result.success = false;
                result.msg = "积分不足！";
                result.status = -6;

                return Json(result);
            }

            //等级判定
            if (!MemberGradeApplication.IsAllowGrade(CurrentUser.Id, giftdata.NeedGrade))
            {
                isdataok = false;
                result.success = false;
                result.msg = "用户等级不足！";
                result.status = -6;
                return Json(result);
            }
            
            #endregion

            Entities.ShippingAddressInfo shipdata = GetShippingAddress(regionId);
            if (shipdata == null)
            {
                isdataok = false;
                result.success = false;
                result.msg = "错误的收货人地址信息！";
                result.status = -6;

                return Json(result);
            }

            if (isdataok)
            {
                gorditemlist.Add(new GiftOrderItemModel { GiftId = giftdata.Id, Counts = count });
                GiftOrderModel createorderinfo = new GiftOrderModel();
                createorderinfo.Gifts = gorditemlist;
                createorderinfo.CurrentUser = CurrentUser;
                createorderinfo.ReceiveAddress = shipdata;
                Himall.Entities.GiftOrderInfo orderdata = _iGiftsOrderService.CreateOrder(createorderinfo);
                result.success = true;
                result.msg = orderdata.Id.ToString();
                result.status = 1;
            }

            return Json(result);
        }
        /// <summary>
        /// 下单成功
        /// </summary>
        /// <returns></returns>
        public ActionResult OrderSuccess(long id)
        {
            var data = _iGiftsOrderService.GetOrder(id, CurrentUser.Id);
            if (data == null)
            {
                throw new HimallException("错误的订单编号！");
            }
            //Logo
            ViewBag.Logo = SiteSettingApplication.SiteSettings.Logo;//获取Logo
            ViewBag.Step = 3;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(data);
        }
        /// <summary>
        /// 获取收货地址
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        private Entities.ShippingAddressInfo GetShippingAddress(long? regionId)
        {
            Entities.ShippingAddressInfo result = null;
            if (regionId != null)
            {
                result = _ShippingAddressService.Get((long)regionId);
            }
            else
            {
                result = _ShippingAddressService.GetDefault(CurrentUser.Id);
            }

            return result;
        }
    }
}