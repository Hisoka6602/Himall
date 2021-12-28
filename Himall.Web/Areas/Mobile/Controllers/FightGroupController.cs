using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static Himall.Entities.ProductInfo;

namespace Himall.Web.Areas.Mobile.Controllers
{
    /// <summary>
    /// 拼团
    /// </summary>
    public class FightGroupController : BaseMobileTemplatesController
    {
        private ProductService _ProductService;
        private TypeService _iTypeService;
        public FightGroupController(ProductService ProductService, TypeService TypeService
            )
        {
            _ProductService = ProductService;
            _iTypeService = TypeService;
        }


        /// <summary>
        /// 过滤过程
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //路由处理
            var route = filterContext.RouteData;
            //string controller = route.Values["controller"].ToString().ToLower();
            string action = route.Values["action"].ToString().ToLower();
            if (action.ToLower() != "close")
            {
                if (!FightGroupApplication.IsOpenMarketService())
                {
                    filterContext.Result = RedirectToAction("Close");
                    return;
                }
            }
            base.OnActionExecuting(filterContext);

        }
        /// <summary>
        /// 功能未开启
        /// </summary>
        /// <returns></returns>
        public ActionResult Close()
        {
            return View();
        }

        /// <summary>
        /// 拼团列表
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public JsonResult PostActiveList(FightGroupActiveQuery query)
        {
            query.SaleStatus = ProductSaleStatus.OnSale;//已销售状态商品
            query.PageSize = 5;
            query.ActiveStatusList = new List<FightGroupActiveStatus> {
                FightGroupActiveStatus.Ongoing,
                FightGroupActiveStatus.WillStart
            };
            var data = FightGroupApplication.GetActives(query);
            return Json(new { success = true, data = data.Models, total = data.Total });
        }

        /// <summary>
        /// 拼团活动详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
		public ActionResult Detail(long id)
        {
            FightGroupActiveModel data = FightGroupApplication.GetActive(id, false);
            if (data == null)
            {
                throw new HimallException("错误的活动信息");
            }
            data.InitProductImages();
            AutoMapper.Mapper.CreateMap<FightGroupActiveModel, FightGroupActiveDetailModel>();

            FightGroupActiveDetailModel model = AutoMapper.Mapper.Map<FightGroupActiveDetailModel>(data);
            decimal discount = 1M;
            if (CurrentUser != null)
            {
                discount = CurrentUser.MemberDiscount;
            }
            ViewBag.Discount = discount;
            var shopInfo = ShopApplication.GetShop(model.ShopId);
            ViewBag.IsSelf = shopInfo.IsSelf;

            //AutoMapper.Mapper.CreateMap<FightGroupActiveModel, FightGroupActiveResult>();
            //var fightGroupData = AutoMapper.Mapper.Map<FightGroupActiveResult>(model);

            model.ShareUrl = string.Format("{0}/m-{1}/FightGroup/Detail/{2}", CurrentUrlHelper.CurrentUrlNoPort(), "WeiXin", data.Id);

            #region 判断当前用户是否已参加拼团，且当前组团状态为进行中
            bool IsUserEnter = false;
            if (CurrentUser != null)
            {
                var list = FightGroupApplication.GetFightGroupOrderList(id, CurrentUser.Id);
                if (list.Count > 0)
                {
                    IsUserEnter = true;
                    model.ShareUrl = string.Format("{0}/m-{1}/FightGroup/GroupDetail/{2}?aid={3}", CurrentUrlHelper.CurrentUrlNoPort(), "WeiXin", list[0].GroupId, list[0].ActiveId);
                }
            }
            ViewBag.IsUserEnter = IsUserEnter;
            #endregion

            #region 分销员数据绑定
            var user = CurrentUser;
            if (user != null && user.Id > 0 && SiteSettingApplication.SiteSettings.DistributionIsEnable)
            {
                var prom = DistributionApplication.GetDistributor(user.Id);
                if (prom != null && prom.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    if (IsUserEnter)
                        model.ShareUrl += "&" + DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=" + user.Id.ToString();
                    else
                    {
                        model.ShareUrl += "?" + DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=" + user.Id.ToString();
                    }
                }
            }
            #endregion

            model.ShareTitle = data.ActiveStatus == FightGroupActiveStatus.WillStart ? "限时限量火拼 即将开始" : "限时限量火拼 正在进行";
            model.ShareImage = data.ProductDefaultImage;
            if (!string.IsNullOrWhiteSpace(model.ShareImage))
            {
                if (model.ShareImage.Substring(0, 4) != "http")
                {
                    model.ShareImage = HimallIO.GetRomoteImagePath(model.ShareImage);
                }
            }

            model.ShareDesc = data.ProductName;
            if (!string.IsNullOrWhiteSpace(data.ProductShortDescription))
            {
                model.ShareDesc += "，(" + data.ProductShortDescription + ")";
            }
            if (model.ProductId > 0)
            {
                //统计商品浏览量、店铺浏览人数
                StatisticApplication.StatisticVisitCount(model.ProductId, model.ShopId);
            }

            var product = new ProductInfo() { Id = model.ProductId, ProductName = model.ProductName, MinSalePrice = model.MiniGroupPrice, ImagePath = model.ProductImgPath };
            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(model.ShopId, true, CurrentUser, product);
            ViewBag.CustomerServices = customerServices;

            var bonus = ObjectContainer.Current.Resolve<ShopBonusService>().GetByShopId(model.ShopId);
            if (bonus != null)
            {
                model.BonusCount = bonus.Count;
                model.BonusGrantPrice = bonus.GrantPrice;
                model.BonusRandomAmountStart = bonus.RandomAmountStart;
                model.BonusRandomAmountEnd = bonus.RandomAmountEnd;
            }
            var fullDiscount = FullDiscountApplication.GetOngoingActiveByProductId(id, model.ShopId);
            model.FullDiscount = fullDiscount;

            model.IsSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;//是否显示销量
            model.SaleVolume = data.ActiveItems.Sum(d => d.BuyCount);//销量
            model.FreightTemplate = FreightTemplateApplication.GetFreightTemplate(model.FreightTemplateId);//运费模板
            model.FreightStr = FreightTemplateApplication.GetFreightStr(model.ProductId, model.FreightTemplate, CurrentUser?.Id ?? 0, product.ProductType);//运费或免运费

            ViewBag.QQMapKey = SiteSettingApplication.SiteSettings.QQMapAPIKey;

            return View(model);
        }

        /// <summary>
        /// 获取sku信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetSkus(long id)
        {
            FightGroupActiveModel model = FightGroupApplication.GetActive(id, false);
            if (model == null)
            {
                throw new HimallException("错误的活动信息");
            }
            List<SKUDataModel> skudata = model.ActiveItems.Where(d => d.ActiveStock > 0).Select(d => new SKUDataModel
            {
                SkuId = d.SkuId,
                Color = d.Color,
                Size = d.Size,
                Version = d.Version,
                Stock = (int)d.ActiveStock,
                CostPrice = d.ProductCostPrice,
                SalePrice = d.ProductPrice,
                Price = d.ActivePrice,
            }).ToList();

            return Json(skudata);
        }

        /// <summary>
        /// 检测购买数量
        /// </summary>
        /// <param name="skuIds"></param>
        /// <param name="counts"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CheckBuyNumber(long id, string skuId, int count)
        {
            long exist = 0;
            if (CurrentUser != null)
            {
                exist = FightGroupApplication.GetMarketSaleCountForUserId(id, CurrentUser.Id);
            }
            else
            {
                exist = 0;  //未登录用户默认为未买
            }
            FightGroupActiveInfo info = FightGroupApplication.GetFightGroupActiveInfo(id);
            long productId = 0;
            if (info != null)
            {
                productId = info.ProductId;
            }
            return Json(new { success = true, data = new { hasbuy = exist, ProductId = productId } });

        }

        public JsonResult CanJoin(long aid, long gpid)
        {
            Result result = new Result { success = false, msg = "不可重复参团" };
            if (FightGroupApplication.CanJoinGroup(aid, gpid, CurrentUser.Id))
            {
                result = new Result { success = true, msg = "yes" };
            }
            return Json(result);
        }
        #region 页面调用块
        /// <summary>
        /// 显示最新的可以参团列表
        /// </summary>
        /// <param name="id">活动列表</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult ShowNewCanJoinGroup(long id)
        {
            List<FightGroupBuildStatus> stlist = new List<FightGroupBuildStatus>();
            stlist.Add(FightGroupBuildStatus.Ongoing);
            var data = FightGroupApplication.GetGroups(id, stlist, null, null, 1, 10);
            var datatlist = data.Models.ToList();
            return View(datatlist);
        }
        /// <summary>
        /// 拼团活动详情
        /// </summary>
        /// <param name="data">活动数据</param>
        /// <param name="hst">活动时限显示形式</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult ShowDetail(FightGroupActiveModel data, int hst = 1, DateTime? etime = null)
        {
            FightGroupShowDetailModel model = new FightGroupShowDetailModel();
            if (data == null)
            {
                throw new HimallException("错误的活动信息");
            }
            //data.InitProductImages();
            model.ActiveData = data;
            model.LimitedHourShowType = hst;
            if (etime.HasValue)
            {
                model.EndBuildGroupTime = etime.Value;
            }
            model.ProductMiniPriceByUser = model.ActiveData.MiniSalePrice;
            var shopInfo = ShopApplication.GetShop(data.ShopId);
            if (shopInfo.IsSelf)
            {
                decimal discount = 1M;
                if (CurrentUser != null)
                {
                    discount = CurrentUser.MemberDiscount;
                }
                model.ProductMiniPriceByUser = model.ProductMiniPriceByUser * discount;
            }
            return View(model);
        }
        /// <summary>
        /// 拼团活动详情顶部商品图片
        /// </summary>
        /// <param name="data">活动数据</param>
        /// <param name="hst">活动时限显示形式</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult ShowActionHead(FightGroupActiveModel data)
        {
            FightGroupShowDetailModel model = new FightGroupShowDetailModel();
            if (data == null)
            {
                throw new HimallException("错误的活动信息");
            }
            data.InitProductImages();
            model.ActiveData = data;
            return View(model);
        }
        /// <summary>
        /// 显示活动的规格信息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult ShowSkuInfo(FightGroupActiveModel data)
        {
            if (data == null)
            {
                throw new HimallException("错误的活动信息");
            }
            ProductShowSkuInfoModel model = new ProductShowSkuInfoModel();
            model.MinSalePrice = data.MiniGroupPrice;
            model.ProductImagePath = data.ProductImgPath;
            model.MaxBuyCount = data.LimitQuantity.HasValue ? data.LimitQuantity.Value : 0;

            #region 商品规格
            Entities.TypeInfo typeInfo = _iTypeService.GetTypeByProductId((long)data.ProductId);
            string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
            string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
            string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
            var product = ProductManagerApplication.GetProduct((long)data.ProductId);
            if (product != null)
            {
                colorAlias = !string.IsNullOrWhiteSpace(product.ColorAlias) ? product.ColorAlias : colorAlias;
                sizeAlias = !string.IsNullOrWhiteSpace(product.SizeAlias) ? product.SizeAlias : sizeAlias;
                versionAlias = !string.IsNullOrWhiteSpace(product.VersionAlias) ? product.VersionAlias : versionAlias;
            }
            model.ColorAlias = colorAlias;
            model.SizeAlias = sizeAlias;
            model.VersionAlias = versionAlias;

            if (data.ActiveItems != null && data.ActiveItems.Count() > 0)
            {
                model.StockAll = (int)data.ActiveItems.Where(p => p.SkuId.Contains(product.Id + "_")).Sum(p => p.ActiveStock);//总库存(它where一次是因为有效规格是“产品ID_”,过滤无效“{0}_”)
                long colorId = 0, sizeId = 0, versionId = 0;
                foreach (var sku in data.ActiveItems)
                {
                    var specs = sku.SkuId.Split('_');
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }
                        if (colorId != 0)
                        {
                            if (!model.Color.Any(v => v.Value.Equals(sku.Color)))
                            {
                                var c = data.ActiveItems.Where(s => s.Color.Equals(sku.Color)).Sum(s => s.ActiveStock);
                                model.Color.Add(new Himall.Web.Areas.Web.Models.ProductSKU
                                {
                                    //Name = "选择颜色",
                                    Name = "选择" + colorAlias,
                                    EnabledClass = c != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Color.Any(c1 => c1.SelectedClass.Equals("selected")) && c != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = colorId,
                                    Value = sku.Color,
                                    Img = sku.ShowPic
                                });
                            }
                        }
                    }
                    if (specs.Count() > 1)
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0 && !string.IsNullOrEmpty(sku.Size))
                        {
                            if (!model.Size.Any(v => v.Value.Equals(sku.Size)))
                            {
                                var ss = data.ActiveItems.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.ActiveStock);
                                model.Size.Add(new Himall.Web.Areas.Web.Models.ProductSKU
                                {
                                    //Name = "选择尺码",
                                    Name = "选择" + sizeAlias,
                                    EnabledClass = ss != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Size.Any(s1 => s1.SelectedClass.Equals("selected")) && ss != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = sizeId,
                                    Value = sku.Size

                                });
                            }
                        }
                    }

                    if (specs.Count() > 2)
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0 && !string.IsNullOrEmpty(sku.Version))
                        {
                            if (!model.Version.Any(v => v.Value.Equals(sku.Version)))
                            {
                                var v = data.ActiveItems.Where(s => s.Version.Equals(sku.Version)).Sum(s => s.ActiveStock);
                                model.Version.Add(new Himall.Web.Areas.Web.Models.ProductSKU
                                {
                                    //Name = "选择版本",
                                    Name = "选择" + versionAlias,
                                    EnabledClass = v != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Version.Any(v1 => v1.SelectedClass.Equals("selected")) && v != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = versionId,
                                    Value = sku.Version

                                });
                            }
                        }
                    }

                }
            }
            #endregion

            return View(model);
        }
        #endregion

        /// <summary>
        /// 参与拼团
        /// <para>拼团查看</para>
        /// </summary>
        /// <param name="id">团编号</param>
        /// <param name="aid">活动编号</param>
        /// <returns></returns>
        public ActionResult GroupDetail(long id, long aid)
        {
            FightGroupActiveModel activedata = FightGroupApplication.GetActive(aid, false);
            if (activedata == null)
            {
                throw new HimallException("错误的活动信息");
            }
            activedata.InitProductImages();
            FightGroupsModel groupsdata = FightGroupApplication.GetGroup(aid, id);
            if (groupsdata == null)
            {
                throw new HimallException("错误的拼团信息");
            }
            if (groupsdata.BuildStatus == FightGroupBuildStatus.Opening)
            {
                return Redirect(string.Format("/m-{0}/Member/Center/", PlatformType.ToString()));
            }
            FightGroupGroupDetailModel model = new FightGroupGroupDetailModel();
            model.ActiveData = activedata;
            model.GroupsData = groupsdata;
            model.HasJoin = false;
            if (CurrentUser != null)
            {
                model.HasJoin = !FightGroupApplication.CanJoinGroup(aid, id, CurrentUser.Id);
            }

            model.ShareUrl = string.Format("{0}/m-{1}/FightGroup/GroupDetail/{2}?aid={3}", CurrentUrlHelper.CurrentUrlNoPort(), "WeiXin", id, groupsdata.ActiveId);
            model.ShareTitle = "我参加了(" + activedata.ProductName + ")的拼团";
            model.ShareImage = activedata.ProductDefaultImage;
            if (!string.IsNullOrWhiteSpace(model.ShareImage))
            {
                if (model.ShareImage.Substring(0, 4) != "http")
                {
                    model.ShareImage = HimallIO.GetRomoteImagePath(model.ShareImage);
                }
            }

            #region 分销员数据绑定
            var user = CurrentUser;
            if (user != null && user.Id > 0 && SiteSettingApplication.SiteSettings.DistributionIsEnable)
            {
                var prom = DistributionApplication.GetDistributor(user.Id);
                if (prom != null && prom.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    model.ShareUrl += "&" + DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=" + user.Id.ToString();
                }
            }
            #endregion


            int neednum = groupsdata.LimitedNumber - groupsdata.JoinedNumber;
            neednum = neednum < 0 ? 0 : neednum;
            if (neednum > 0)
            {
                model.ShareDesc = "还差" + neednum + "人即可成团";
            }
            if (!string.IsNullOrWhiteSpace(activedata.ProductShortDescription))
            {
                if (!string.IsNullOrWhiteSpace(model.ShareDesc))
                {
                    model.ShareDesc += "，(" + activedata.ProductShortDescription + ")";
                }
                else
                {
                    model.ShareDesc += activedata.ProductShortDescription;
                }
            }

            model.ActiveData.IsSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;//是否显示销量
            model.ActiveData.SaleVolume = activedata.ActiveItems.Sum(d => d.BuyCount);//销量
            model.ActiveData.FreightTemplate = FreightTemplateApplication.GetFreightTemplate(activedata.FreightTemplateId);//运费模板
            model.ActiveData.FreightStr = FreightTemplateApplication.GetFreightStr(activedata.ProductId, activedata.FreightTemplate, CurrentUser == null ? 0 : CurrentUser.Id);//运费或免运费

            return View(model);
        }

        /// <summary>
        /// 开团成功
        /// </summary>
        /// <param name="orderid">订单编号</param>
        /// <returns></returns>
		public ActionResult GroupOrderOk(long orderid)
        {
            FightGroupOrderOkModel model = new FightGroupOrderOkModel();
            var gpord = FightGroupApplication.GetOrder(orderid);
            if (gpord == null)
            {
                throw new HimallException("错误的拼团订单信息");
            }
            var gpobj = FightGroupApplication.GetGroup(gpord.ActiveId, gpord.GroupId);
            if (gpobj == null)
            {
                throw new HimallException("错误的拼团信息");
            }
            if (gpobj.BuildStatus == FightGroupBuildStatus.Opening)
            {
                //throw new HimallException("开团未成功，等待团长付款中");
                return Redirect(string.Format("/m-{0}/Member/Center/", PlatformType.ToString()));
            }
            if (gpobj.BuildStatus == FightGroupBuildStatus.Failed)
            {
                //throw new HimallException("成团已失败");
                //return Redirect(string.Format("/m-{0}/Member/Center/", PlatformType.ToString()));
            }
            var gpact = FightGroupApplication.GetActive(gpobj.ActiveId);
            model.isFirst = gpord.IsFirstOrder;
            model.LimitedNumber = gpobj.LimitedNumber;
            model.JoinNumber = gpobj.JoinedNumber;
            model.FightGroupStatus = gpobj.BuildStatus;
            if (gpobj.BuildStatus != FightGroupBuildStatus.Success)
            {
                model.ShareUrl = string.Format("{0}/m-{1}/FightGroup/GroupDetail/{2}?aid={3}", CurrentUrlHelper.CurrentUrlNoPort(), "WeiXin", gpord.GroupId, gpobj.ActiveId);
                model.ShareTitle = "我参加了(" + gpobj.ProductName + ")的拼团";
                model.ShareImage = gpact.ProductDefaultImage;
                if (!string.IsNullOrWhiteSpace(model.ShareImage))
                {
                    if (model.ShareImage.Substring(0, 4) != "http")
                    {
                        model.ShareImage = HimallIO.GetRomoteImagePath(model.ShareImage);
                    }
                }

                int neednum = gpobj.LimitedNumber - gpobj.JoinedNumber;
                neednum = neednum < 0 ? 0 : neednum;
                if (neednum > 0)
                {
                    model.ShareDesc = "还差" + neednum + "人即可成团";
                }
                if (!string.IsNullOrWhiteSpace(gpact.ProductShortDescription))
                {
                    if (!string.IsNullOrWhiteSpace(model.ShareDesc))
                    {
                        model.ShareDesc += "，(" + gpact.ProductShortDescription + ")";
                    }
                    else
                    {
                        model.ShareDesc += gpact.ProductShortDescription;
                    }
                }
            }
            return View(model);
        }
    }
}