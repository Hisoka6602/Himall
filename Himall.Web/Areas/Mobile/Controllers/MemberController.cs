using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.App_Code.Common;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Himall.Web.Areas.Web.Models;
using Himall.CommonModel;
using System.Drawing;
using NPOI.HSSF.Record.Chart;
using ServiceStack.Messaging;

namespace Himall.Web.Areas.Mobile.Controllers
{

    //TODO:Service 好多Service ？
    public class MemberController : BaseMobileMemberController
    {
        private OrderService _OrderService;
        private MemberService _MemberService;
        private MemberCapitalService _MemberCapitalService;
        private CouponService _CouponService;
        private ShopBonusService _ShopBonusService;
        private VShopService _VShopService;
        private ShopService _ShopService;
        private ProductService _ProductService;
        private ShippingAddressService _ShippingAddressService;
        private MemberSignInService _iMemberSignInService;
        private RefundService _RefundService;
        private CommentService _CommentService;
        private ShopBranchService _iShopBranchService;
        public MemberController(
            OrderService OrderService,
            MemberService MemberService,
             MemberCapitalService MemberCapitalService,
             CouponService CouponService,
             ShopBonusService ShopBonusService,
             VShopService VShopService,
             ProductService ProductService,
             ShippingAddressService ShippingAddressService,
            MemberSignInService MemberSignInService,
            RefundService RefundService,
            CommentService CommentService,
            ShopBranchService ShopBranchService,
            ShopService ShopService
            )
        {
            _OrderService = OrderService;
            _MemberService = MemberService;
            _MemberCapitalService = MemberCapitalService;
            _CouponService = CouponService;
            _ShopBonusService = ShopBonusService;
            _VShopService = VShopService;
            _ProductService = ProductService;
            _ShippingAddressService = ShippingAddressService;
            _iMemberSignInService = MemberSignInService;
            _RefundService = RefundService;
            _CommentService = CommentService;
            _iShopBranchService = ShopBranchService;
            _ShopService = ShopService;
        }
        public ActionResult Center()
        {
            var userId = CurrentUser.Id;
            MemberCenterModel model = new MemberCenterModel();

            var statistic = StatisticApplication.GetMemberOrderStatistic(userId, true);

            var member = _MemberService.GetMember(userId);
            model.Member = member;
            model.AllOrders = statistic.OrderCount;
            model.WaitingForRecieve = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(UserId);
            model.WaitingForPay = statistic.WaitingForPay;
            model.WaitingForDelivery = statistic.WaitingForDelivery;
            model.WaitingForComments = statistic.WaitingForComments;
            model.RefundOrders = statistic.RefundCount;
            model.FavoriteProductCount = FavoriteApplication.GetFavoriteCountByUser(userId);

            //拼团
            model.CanFightGroup = FightGroupApplication.IsOpenMarketService();
            model.BulidFightGroupNumber = FightGroupApplication.CountJoiningOrder(userId);

            model.Capital = MemberCapitalApplication.GetBalanceByUserId(userId);
            model.CouponsCount = MemberApplication.GetAvailableCouponCount(userId);
            var integral = MemberIntegralApplication.GetMemberIntegral(userId);
            model.GradeName = MemberGradeApplication.GetMemberGradeByUserIntegral(integral.HistoryIntegrals).GradeName;
            model.GradeName=model.GradeName.Equals("vip0")?"": model.GradeName;
            model.MemberAvailableIntegrals = MemberIntegralApplication.GetAvailableIntegral(userId);

            model.CollectionShop = ShopApplication.GetUserConcernShopsCount(userId);

            model.CanSignIn = _iMemberSignInService.CanSignInByToday(userId);
            model.SignInIsEnable = _iMemberSignInService.GetConfig().IsEnable;
            model.userMemberInfo = CurrentUser;
            model.IsOpenRechargePresent = SiteSettings.IsOpenRechargePresent;

            model.DistributionOpenMyShopShow = SiteSettings.DistributorRenameOpenMyShop;
            model.DistributionMyShopShow = SiteSettings.DistributorRenameMyShop;
            
            if (PlatformType == PlatformType.WeiXin)
            {
                //分销
                model.IsShowDistributionOpenMyShop = SiteSettings.DistributionIsEnable;
                var duser = DistributionApplication.GetDistributor(CurrentUser.Id);
                if (duser != null && duser.DistributionStatus != (int)DistributorStatus.UnApply)
                {
                    model.IsShowDistributionOpenMyShop = false;
                    //拒绝的分销员显示“我要开店”
                    if (duser.DistributionStatus == (int)DistributorStatus.Refused || duser.DistributionStatus == (int)DistributorStatus.UnAudit)
                        model.IsShowDistributionOpenMyShop = true && SiteSettings.DistributionIsEnable;

                    model.IsShowDistributionMyShop = true && SiteSettings.DistributionIsEnable;
                    if (duser.DistributionStatus == (int)DistributorStatus.NotAvailable || duser.DistributionStatus == (int)DistributorStatus.Refused || duser.DistributionStatus == (int)DistributorStatus.UnAudit)
                    {
                        model.IsShowDistributionMyShop = false;
                    }
                }
            }
            _MemberService.AddIntegel(member); //给用户加积分//执行登录后初始化相关操作
            return View(model);
        }

        public ActionResult ShippingAddress()
        {
            return View();
        }

        #region 订单相关处理
        public ActionResult Orders(int? orderStatus,string keysword)
        {
            //判断是否需要跳转到支付地址
            if (this.Request.Url.AbsolutePath.EndsWith("/member/orders", StringComparison.OrdinalIgnoreCase) && (orderStatus == null || orderStatus == 0 || orderStatus == 1))
            {
                var returnUrl = Request.QueryString["returnUrl"];
                return Redirect(Url.RouteUrl("PayRoute") + "?area=mobile&keysword="+keysword+"&platform=" + this.PlatformType.ToString() + "&controller=member&action=orders&orderStatus=" + orderStatus + (string.IsNullOrEmpty(returnUrl) ? "" : "&returnUrl=" + HttpUtility.UrlEncode(returnUrl)));
            }
            var statistic = StatisticApplication.GetMemberOrderStatistic(CurrentUser.Id);
            ViewBag.AllOrders = statistic.OrderCount;
            ViewBag.WaitingForComments = statistic.WaitingForComments;
            ViewBag.WaitingForRecieve = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(CurrentUser.Id);
            ViewBag.WaitingForPay = statistic.WaitingForPay;
            ViewBag.WaitingForDelivery = statistic.WaitingForDelivery;
            return View();
        }

        public ActionResult PaymentToOrders(string ids)
        {
            //红包数据
            var bonusGrantIds = new Dictionary<long, Entities.ShopBonusInfo>();
            string url = CurrentUrlHelper.CurrentUrlNoPort() + "/m-weixin/shopbonus/index/";
            if (!string.IsNullOrEmpty(ids))
            {
                string[] strIds = ids.Split(',');
                List<long> longIds = new List<long>();
                foreach (string id in strIds)
                {
                    longIds.Add(long.Parse(id));
                }
                var result = PaymentHelper.GenerateBonus(longIds, WebHelper.GetHost());
                foreach (var item in result)
                {
                    bonusGrantIds.Add(item.Key, item.Value);
                }
            }

            ViewBag.Path = url;
            ViewBag.BonusGrantIds = bonusGrantIds;
            ViewBag.Shops = ShopApplication.GetShops(bonusGrantIds.Select(p => p.Value.ShopId));
            ViewBag.BaseAddress = CurrentUrlHelper.CurrentUrlNoPort();

            var statistic = StatisticApplication.GetMemberOrderStatistic(CurrentUser.Id);
            ViewBag.WaitingForComments = statistic.WaitingForComments;
            ViewBag.AllOrders = statistic.OrderCount;
            ViewBag.WaitingForRecieve = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(CurrentUser.Id);
            ViewBag.WaitingForPay = statistic.WaitingForPay;
            ViewBag.WaitingForDelivery = statistic.WaitingForDelivery;

            var order = OrderApplication.GetUserOrders(CurrentUser.Id, 1).FirstOrDefault();
            if (order != null && order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                var gpord = FightGroupApplication.GetOrder(order.Id);
                if (gpord != null)
                {
                    return Redirect(string.Format("/m-{0}/FightGroup/GroupOrderOk?orderid={1}", PlatformType.ToString(), order.Id));
                }
            }
            return View("~/Areas/Mobile/Templates/Default/Views/Member/Orders.cshtml");
        }

        public JsonResult GetUserOrders(int? orderStatus,string keysword, int pageNo, int pageSize = 8)
        {
            if (orderStatus.HasValue && orderStatus == 0)
            {
                orderStatus = null;
            }
            var queryModel = new OrderQuery()
            {
                Status = (Entities.OrderInfo.OrderOperateStatus?)orderStatus,
                UserId = CurrentUser.Id,
                PageSize = pageSize,
                PageNo = pageNo,
                IsFront = true,
                SearchKeyWords= keysword
            };
            Log.Info("keyword:"+keysword);
            if (queryModel.Status.HasValue && queryModel.Status.Value == Entities.OrderInfo.OrderOperateStatus.WaitReceiving)
            {
                if (queryModel.MoreStatus == null)
                {
                    queryModel.MoreStatus = new List<Entities.OrderInfo.OrderOperateStatus>() { };
                }
                queryModel.MoreStatus.Add(Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp);
            }
            if (orderStatus.GetValueOrDefault() == (int)OrderInfo.OrderOperateStatus.Finish)
                queryModel.Commented = false;//只查询未评价的订单

            var orders = OrderApplication.GetOrders(queryModel);
            var orderItems = OrderApplication.GetOrderItemsByOrderId(orders.Models.Select(p => p.Id));
            var orderComments = OrderApplication.GetOrderCommentCount(orders.Models.Select(p => p.Id));
            var orderRefunds = OrderApplication.GetOrderRefunds(orderItems.Select(p => p.Id));
            var products = ProductManagerApplication.GetProductsByIds(orderItems.Select(p => p.ProductId));
            var vshops = VshopApplication.GetVShopsByShopIds(products.Select(p => p.ShopId));
            //查询结果的门店ID
            var branchIds = orders.Models.Where(e => e.ShopBranchId>0).Select(p => p.ShopBranchId).ToList();
            //根据门店ID获取门店信息
            var shopBranchs = ShopBranchApplication.GetStores(branchIds);
            var orderVerificationCodes = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(orders.Models.Select(p => p.Id).ToList());
            var result = orders.Models.Select(item =>
            {
                var codes = orderVerificationCodes.Where(a => a.OrderId == item.Id);
                var _ordrefobj = _RefundService.GetOrderRefundByOrderId(item.Id) ?? new Entities.OrderRefundInfo { Id = 0 };
                if (item.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitDelivery && item.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                {
                    _ordrefobj = new Entities.OrderRefundInfo { Id = 0 };
                }
                int? ordrefstate = (_ordrefobj == null ? null : (int?)_ordrefobj.SellerAuditStatus);
                ordrefstate = (ordrefstate > 4 ? (int?)_ordrefobj.ManagerConfirmStatus : ordrefstate);
                var branchObj = shopBranchs.FirstOrDefault(e => item.ShopBranchId > 0 && e.Id == item.ShopBranchId);
                string branchName = branchObj == null ? string.Empty : branchObj.ShopBranchName;
                return new
                {
                    id = item.Id,
                    status = item.OrderStatus.ToDescription(),
                    orderStatus = item.OrderStatus,
                    shopname = item.ShopName,
                    orderTotalAmount = item.OrderTotalAmount,
                    capitalAmount = item.CapitalAmount,
                    productCount = orderItems.Where(oi => oi.OrderId == item.Id).Sum(a=>a.Quantity),
                    commentCount = orderComments.ContainsKey(item.Id) ? orderComments[item.Id] : 0,
                    PaymentType = item.PaymentType,
                    RefundStats = ordrefstate,
                    OrderRefundId = _ordrefobj.Id,
                    OrderType = item.OrderType,
                    PickUp = item.PickupCode,
                    ShopBranchId = item.ShopBranchId,
                    ShopBranchName = branchName,
                    DeliveryType = item.DeliveryType,
                    ShipOrderNumber = item.ShipOrderNumber,
                    EnabledRefundAmount = item.OrderEnabledRefundAmount,
                    itemInfo = orderItems.Where(oi => oi.OrderId == item.Id).Select(a =>
                            {
                                var prodata = products.FirstOrDefault(p => p.Id == a.ProductId);
                                VShop vshop = null;
                                if (prodata != null)
                                    vshop = vshops.FirstOrDefault(vs => vs.ShopId == prodata.ShopId);
                                if (vshop == null)
                                    vshop = new VShop { Id = 0 };

                                var itemrefund = orderRefunds.Where(or => or.OrderItemId == a.Id).FirstOrDefault(d => d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                                int? itemrefstate = (itemrefund == null ? null : (int?)itemrefund.SellerAuditStatus);
                                itemrefstate = (itemrefstate > 4 ? (int?)itemrefund.ManagerConfirmStatus : itemrefstate);
                                string picimg = HimallIO.GetProductSizeImage(a.ThumbnailsUrl, 1, (int)ImageSize.Size_100);
                                if (a.ThumbnailsUrl.Contains("skus")) {
                                    picimg=HimallIO.GetRomoteImagePath(a.ThumbnailsUrl);
                                }
                                return new
                                {
                                    itemid = a.Id,
                                    productId = a.ProductId,
                                    productName = a.ProductName,
                                    image = picimg,
                                    count = a.Quantity,
                                    price = a.SalePrice,
                                    Unit = prodata == null ? "" : prodata.MeasureUnit,
                                    vshopid = vshop.Id,
                                    color = a.Color,
                                    size = a.Size,
                                    version = a.Version,
                                    RefundStats = itemrefstate,
                                    OrderRefundId = (itemrefund == null ? 0 : itemrefund.Id),
                                    EnabledRefundAmount = a.EnabledRefundAmount
                                };
                            }),
                    HasAppendComment = HasAppendComment(orderItems.Where(oi => oi.OrderId == item.Id).FirstOrDefault()),
                    CanRefund = OrderApplication.CanRefund(item, ordrefstate),
                    IsVirtual = item.OrderType == OrderInfo.OrderTypes.Virtual ? 1 : 0,
                    IsPay = item.PayDate.HasValue ? 1 : 0
                };
            });

            foreach (var item in result)
            {
                var refund = item.itemInfo.Any(p => p.OrderRefundId > 0);
                //if (!refund)
                //item.CanRefund = false;
            }
            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PickupGoods(long id)
        {
            var orderInfo = OrderApplication.GetOrder(id);
            if (orderInfo == null)
                throw new HimallException("订单不存在！");
            if (orderInfo.UserId != CurrentUser.Id)
                throw new HimallException("只能查看自己的提货码！");

            AutoMapper.Mapper.CreateMap<Order, Himall.DTO.OrderListModel>();
            AutoMapper.Mapper.CreateMap<DTO.OrderItem, OrderItemListModel>();
            var orderModel = AutoMapper.Mapper.Map<Order, Himall.DTO.OrderListModel>(orderInfo);
            var orderItems = OrderApplication.GetOrderItemsByOrderId(orderInfo.Id);
            orderModel.OrderItemList = AutoMapper.Mapper.Map<List<DTO.OrderItem>, List<OrderItemListModel>>(orderItems);
            if ( orderInfo.ShopBranchId >0)
            {//补充数据
                var branch = ShopBranchApplication.GetShopBranchById(orderInfo.ShopBranchId);
                orderModel.ShopBranchName = branch.ShopBranchName;
                orderModel.ShopBranchAddress = branch.AddressFullName;
                orderModel.ShopBranchContactPhone = branch.ContactPhone;
            }
            orderModel.PickupCode = GetQRCode(orderModel.PickupCode);
            return View(orderModel);
        }
        private string GetQRCode(string verificationCode)
        {
            string qrCodeImagePath = string.Empty;
            string fileName = "pickupgoods-" + verificationCode + ".jpg";
            qrCodeImagePath = CurrentUrlHelper.CurrentUrl() + "/temp/" + fileName;
            if (!System.IO.File.Exists(qrCodeImagePath))
            {
                Image qrcode = Core.Helper.QRCodeHelper.Create(verificationCode, 150, 150);
                qrcode.Save(Server.MapPath("~/temp/") + fileName);
            }
            return qrCodeImagePath;
        }
        #endregion 订单相关处理
        public ActionResult CollectionProduct()
        {
            return View();
        }
        private bool HasAppendComment(DTO.OrderItem orderItem)
        {
            var result = _CommentService.HasAppendComment(orderItem.Id);
            return result;
        }
        public ActionResult CollectionShop()
        {
            ViewBag.SiteName = SiteSettings.SiteName;
            return View();
        }

        public ActionResult ChangeLoginPwd()
        {
            return View(CurrentUser);
        }

        /// <summary>
        /// 修改支付密码
        /// </summary>
        /// <returns></returns>
        public ActionResult ChangePayPwd()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ChangePayPwd(ChangePayPwd model)
        {
            if (string.IsNullOrEmpty(model.NewPayPwd))
                return Json(new { success = false, msg = "请输入新支付密码" });

            if (!string.IsNullOrEmpty(model.OldPayPwd))
            {
                var success = MemberApplication.VerificationPayPwd(CurrentUser.Id, model.OldPayPwd);
                if (!success)
                    return Json(new { success = false, msg = "原支付密码输入不正确" });
                MemberApplication.ChangePayPassword(CurrentUser.Id, model.NewPayPwd);
            }
            else if (!string.IsNullOrEmpty(model.PhoneCode))
            {
                var codeCache= MessageApplication.GetMessageCacheCode(CurrentUser.CellPhone, model.SendCodePluginId);
               
                if (string.IsNullOrEmpty(codeCache))
                    return Json(new { success = false, msg = "验证码已过期" });


                MemberApplication.ChangePayPassword(CurrentUser.Id, model.NewPayPwd);
            }
            else
                return Json(new { success = false });
            return Json(new { success = true });
        }

        public JsonResult GetUserCollectionProduct(int pageNo, int pageSize = 16)
        {
            var data = _ProductService.GetUserConcernProducts(CurrentUser.Id, pageNo, pageSize).Models;
            var products = _ProductService.GetProducts(data.Select(p => p.ProductId).ToList());
            var result = data.Select(item =>
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                return new
                {
                    Id = product.Id,
                    Image = product.GetImage(ImageSize.Size_220),
                    ProductName = product.ProductName,
                    SalePrice = product.MinSalePrice.ToString("F2"),
                    Evaluation = CommentApplication.GetCommentCountByProduct(product.Id),
                    Status = GetProductShowStatus(product)
                };
            });
            return Json(new { success = true, data = result });
        }
        /// <summary>
        /// 删除关注商品
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult CancelConcernProduct(long productId)
        {
            if (productId < 1)
            {
                throw new HimallException("错误的参数");
            }
            _ProductService.DeleteFavorite(productId, CurrentUser.Id);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        private int GetProductShowStatus(ProductInfo pro)
        {
            int result = 0;  //0:正常；1：失效；2：库存不足 3：下架
            if (pro.AuditStatus != ProductInfo.ProductAuditStatus.Audited || pro.SaleStatus != ProductInfo.ProductSaleStatus.OnSale)
                result = 3;
            else
            {
                var skus = ProductManagerApplication.GetSKUs(pro.Id);
                if (skus.Sum(d => d.Stock) < 1)
                {
                    result = 2;
                }
            }
            if (pro.IsDeleted)
                result = 1;
            return result;
        }

        public JsonResult GetUserCollectionShop(int pageNo, int pageSize = 8)
        {

            var model = _ShopService.GetUserConcernShops(CurrentUser.Id, pageNo, pageSize);
            List<ShopConcernModel> list = new List<ShopConcernModel>();
            foreach (var m in model.Models)
            {
                var shop = ShopApplication.GetShop(m.ShopId);
                if (shop == null) continue;
                var vshopobj = _VShopService.GetVShopByShopId(m.ShopId);
                ShopConcernModel concern = new ShopConcernModel();
                concern.FavoriteShopInfo.Id = m.Id;
                concern.FavoriteShopInfo.Logo = vshopobj == null ? shop.Logo : vshopobj.Logo;
                concern.FavoriteShopInfo.ConcernTime = m.Date;
                concern.FavoriteShopInfo.ConcernTimeStr = m.Date.ToString("yyyy-MM-dd");
                concern.FavoriteShopInfo.ShopId = m.ShopId;
                concern.FavoriteShopInfo.ShopName = shop.ShopName;
                concern.FavoriteShopInfo.ConcernCount = FavoriteApplication.GetFavoriteShopCountByShop(m.ShopId);
                concern.FavoriteShopInfo.ShopStatus = shop.ShopStatus;
                list.Add(concern);
            }

            return SuccessResult<dynamic>(data: list);
        }

        public JsonResult CheckVshopIfExist(long shopid)
        {
            var vshop = _VShopService.GetVShopByShopId(shopid);
            if (vshop != null)
                return SuccessResult<dynamic>(data: new { vshopid = vshop.Id });
            else
                return ErrorResult();
        }

        [HttpGet]
        public JsonResult GetCancelConcernShop(long shopId)
        {
            _ShopService.CancelConcernShops(shopId, CurrentUser.Id);
            return Json(new Result() { success = true, msg = "取消成功！" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddShippingAddress(Entities.ShippingAddressInfo info)
        {
            info.UserId = CurrentUser.Id;

            if (info.shopBranchId>0)
            {
                var shopBranchInfo = _iShopBranchService.GetShopBranchById(info.shopBranchId);
                if (shopBranchInfo == null)
                {
                    return Json(new { success = false, msg = "门店信息获取失败" }, true);
                }
                if (shopBranchInfo.ServeRadius > 0)
                {
                    string form = string.Format("{0},{1}", info.Latitude, info.Longitude);//收货地址的经纬度
                    if (form.Length <= 1)
                    {
                        return Json(new { success = false, msg = "收货地址经纬度获取失败" }, true);
                    }
                    double Distances = _iShopBranchService.GetLatLngDistancesFromAPI(form, string.Format("{0},{1}", shopBranchInfo.Latitude, shopBranchInfo.Longitude));
                    if (Distances > shopBranchInfo.ServeRadius)
                    {
                        _ShippingAddressService.Create(info);
                        return Json(new { success = true, msg = "收货地址超出门店配送范围" }, true);
                    }
                }
                else
                {
                    return Json(new { success = false, msg = "门店不提供配送服务" }, true);
                }

            }

            _ShippingAddressService.Create(info);
            return Json(new
            {
                success = true,
                msg = "",
                data = new
                {
                    regionFullName = RegionApplication.GetFullName(info.RegionId),
                    id = info.Id
                }
            }, true);
        }

        [HttpPost]
        public JsonResult DeleteShippingAddress(long id)
        {
            var userId = CurrentUser.Id;
            _ShippingAddressService.Remove(id, userId);
            return Json(new Result() { success = true, msg = "删除成功" });
        }

        [HttpPost]
        public JsonResult EditShippingAddress(Entities.ShippingAddressInfo info)
        {
            info.UserId = CurrentUser.Id;
            if (info.shopBranchId>0)
            {
                var shopBranchInfo = _iShopBranchService.GetShopBranchById(info.shopBranchId);
                if (shopBranchInfo == null)
                {
                    return Json(new { success = false, msg = "门店信息获取失败" }, true);
                }
                if (shopBranchInfo.ServeRadius > 0)
                {
                    string form = string.Format("{0},{1}", info.Latitude, info.Longitude);//收货地址的经纬度
                    if (form.Length <= 1)
                    {
                        return Json(new { success = false, msg = "收货地址经纬度获取失败" }, true);
                    }
                    double Distances = _iShopBranchService.GetLatLngDistancesFromAPI(form, string.Format("{0},{1}", shopBranchInfo.Latitude, shopBranchInfo.Longitude));
                    if (Distances > shopBranchInfo.ServeRadius)
                    {
                        _ShippingAddressService.Save(info);
                        return Json(new { success = true, msg = "收货地址超出门店配送范围" }, true);
                    }
                }
                else
                {
                    return Json(new { success = false, msg = "门店不提供配送服务" }, true);
                }

            }

            _ShippingAddressService.Save(info);
            return Json(new
            {
                success = true,
                data = new { regionFullName = RegionApplication.GetFullName(info.RegionId) },
                msg = ""
            }, true);
        }

        [HttpPost]
        public JsonResult ChangePassword(string oldpassword, string password)
        {
            if (string.IsNullOrWhiteSpace(oldpassword) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new Result() { success = false, msg = "密码不能为空！" });
            }
            var model = CurrentUser;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(oldpassword) + model.PasswordSalt);
            bool CanChange = false;
            if (pwd == model.Password)
            {
                CanChange = true;
            }
            if (model.PasswordSalt.StartsWith("o"))
            {
                CanChange = true;
            }
            if (CanChange)
            {
                _MemberService.ChangePassword(model.Id, password);
                return Json(new Result() { success = true, msg = "修改成功" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "旧密码错误" });
            }
        }

        public ActionResult AccountManagement()
        {
            return View();
        }

        public ActionResult AccountSecure()
        {
            return View(CurrentUser);
        }

        public ActionResult BindPhone()
        {
            return View(CurrentUser);
        }

        public ActionResult BindEmail()
        {
            return View(CurrentUser);
        }

        [HttpPost]
        public JsonResult SendCode(string pluginId, string destination = null, bool checkBind = false)
        {
            if (string.IsNullOrEmpty(destination))
                destination = CurrentUser.CellPhone;

            if (string.IsNullOrEmpty(destination))
                return Json(new { success = false, msg = "请输入手机号码" });

            if (checkBind && MessageApplication.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
            {
                return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
            }
            _MemberService.CheckContactInfoHasBeenUsed(pluginId, destination);

            MessageApplication.SendMessageCodeDirect(destination,CurrentUser.UserName, pluginId);
          
            return Json(new Result() { success = true, msg = "发送成功" });
        }

        [HttpPost]
        public JsonResult SendFindCode(string pluginId, string destination = null)
        {
            if (string.IsNullOrEmpty(destination))
                destination = CurrentUser.CellPhone;

            if (string.IsNullOrEmpty(destination))
                return Json(new { success = false, msg = "请先绑定手机" });

            MessageApplication.SendMessageCodeDirect(destination, CurrentUser.UserName, pluginId);
            return Json(new Result() { success = true, msg = "发送成功" });
        }



        [HttpPost]
        public JsonResult CheckCode(string pluginId, string code, string destination)
        {

            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            var member = CurrentUser;
            var mark = "";
            if (cacheCode != null && cacheCode == code)
            {
              
                if (MessageApplication.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
                {
                    return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
                }
                if (pluginId.ToLower().Contains("email"))
                {
                    member.Email = destination;
                    mark = "邮箱";
                }
                else if (pluginId.ToLower().Contains("sms"))
                {
                    member.CellPhone = destination;
                    mark = "手机";
                }
                _MemberService.UpdateMember(member);
                MessageApplication.UpdateMemberContacts(new Entities.MemberContactInfo()
                {
                    Contact = destination,
                    ServiceProvider = pluginId,
                    UserId = CurrentUser.Id,
                    UserType = Entities.MemberContactInfo.UserTypes.General
                });
                MessageApplication.RemoveMessageCacheCode(destination,pluginId);

                Himall.Entities.MemberIntegralRecordInfo info = new Himall.Entities.MemberIntegralRecordInfo();
                info.UserName = member.UserName;
                info.MemberId = member.Id;
                info.RecordDate = DateTime.Now;
                info.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Reg;
                info.ReMark = "绑定" + mark;
                var memberIntegral = ObjectContainer.Current.Resolve<MemberIntegralConversionFactoryService>().Create(Himall.Entities.MemberIntegralInfo.IntegralType.Reg);
                ObjectContainer.Current.Resolve<MemberIntegralService>().AddMemberIntegral(info, memberIntegral);
               
             

                return Json(new Result() { success = true, msg = "验证正确" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确或者已经超时" });
            }
        }

        public ActionResult Integral()
        {
            return View();
        }

        public ActionResult AccountInfo()
        {
            ViewBag.WeiXin = PlatformType == PlatformType.WeiXin;
            return View(CurrentUser);
        }

        [HttpPost]
        public JsonResult SaveAccountInfo(MemberUpdate model)
        {
            if (string.IsNullOrWhiteSpace(model.RealName))
            {
                return ErrorResult("真实姓名必须填写");
            }
            if (!string.IsNullOrWhiteSpace(model.Photo))
            {
                model.Photo = UploadPhoto(model.Photo);
            }
            model.Id = CurrentUser.Id;
            MemberApplication.UpdateMemberInfo(model);
            return Json<dynamic>(success: true, msg: "修改成功");
        }

        private string UploadPhoto(string strPhoto)
        {
            string url = string.Empty;
            string fullPath = "/Storage/Member/" + CurrentUser.Id + "/headImage.jpg";
            try
            {
                byte[] bytes = Convert.FromBase64String(strPhoto.Replace("data:image/jpeg;base64,", ""));
                MemoryStream memStream = new MemoryStream(bytes);
                Core.HimallIO.CreateFile(fullPath, memStream, FileCreateType.Create);
                url = fullPath;
            }
            catch (Exception ex)
            {
                Core.Log.Error("头像上传异常：" + ex);
            }
            return url;
        }

        /// <summary>
        /// 获取用户积分明细
        /// </summary>
        /// <param name="id">用户编号</param>
        /// <param name="type"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public object GetIntegralRecord(int? type, int pageSize = 10, int pageNo = 1)
        {
            var id = CurrentUser.Id;
            //处理当前用户与id的判断
            var _iMemberIntegralService = ObjectContainer.Current.Resolve<MemberIntegralService>();
            Himall.Entities.MemberIntegralInfo.IntegralType? integralType = null;
            if (type.HasValue)
            {
                integralType = (Himall.Entities.MemberIntegralInfo.IntegralType)type.Value;
            }
            var query = new IntegralRecordQuery() { IntegralType = integralType, UserId = CurrentUser.Id, PageNo = pageNo, PageSize = pageSize };
            var result = _iMemberIntegralService.GetIntegralRecordListForWeb(query);
            var list = result.Models.Select(item =>
            {
                var actions = _iMemberIntegralService.GetIntegralRecordAction(item.Id);
                return new
                {
                    Id = item.Id,
                    RecordDate = ((DateTime)item.RecordDate).ToString("yyyy-MM-dd HH:mm:ss"),
                    Integral = item.Integral,
                    TypeId = item.TypeId,
                    ShowType = (item.TypeId == Himall.Entities.MemberIntegralInfo.IntegralType.WeiActivity) ? item.ReMark : item.TypeId.ToDescription(),
                    ReMark = GetRemarkFromIntegralType(item.TypeId, actions, item.ReMark)
                };
            }).ToList();
            var userInte = MemberIntegralApplication.GetMemberIntegral(UserId);
            return Json(new
            {
                success = true,
                total = result.Total,
                availableIntegrals = userInte.AvailableIntegrals,
                data = Json(list)
            });
        }

        private string GetRemarkFromIntegralType(Himall.Entities.MemberIntegralInfo.IntegralType type, ICollection<Himall.Entities.MemberIntegralRecordActionInfo> recordAction, string remark = "")
        {
            if (recordAction == null || recordAction.Count == 0)
                return remark;
            switch (type)
            {
                case Himall.Entities.MemberIntegralInfo.IntegralType.Consumption:
                    var orderIds = "";
                    foreach (var item in recordAction)
                    {
                        orderIds += item.VirtualItemId + ",";
                    }
                    remark = "订单号:" + orderIds.TrimEnd(',');
                    break;
                default:
                    return remark;
            }
            return remark;
        }

        /// <summary>
        /// 是否强制绑定手机号
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult IsConBindSms()
        {
            return Json<dynamic>(success: MessageApplication.IsOpenBindSms(CurrentUser.Id));
        }


        public ActionResult GotoGifts()
        {
            return RedirectToAction("Index", "Gifts");
        }
        public ActionResult GotoChooseAddress()
        {
            return RedirectToAction("StoreListAddress", "ShopBranch");
        }
    }
}