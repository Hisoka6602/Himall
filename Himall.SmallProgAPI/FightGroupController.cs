using Himall.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Himall.DTO.QueryModel;
using static Himall.Entities.ProductInfo;
using Himall.CommonModel;
using Himall.Entities;
using AutoMapper;
using Himall.DTO;
using Himall.Core;
using Himall.SmallProgAPI.Model;
using Himall.Service;
using System.Web;
using System.Text.RegularExpressions;

namespace Himall.SmallProgAPI
{
    public class FightGroupController : SmallProgAPIController
    {
        /// <summary>
        /// 获取拼团活动列表
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetActiveList(long? shopId = 0, int pageSize = 5, int pageNo = 1)
        {
            FightGroupActiveQuery query = new FightGroupActiveQuery();
            query.SaleStatus = ProductSaleStatus.OnSale;//已销售状态商品
            query.PageSize = pageSize;
            query.PageNo = pageNo;
            if (shopId.HasValue && shopId > 0)
            {
                query.ShopId = shopId;
            }

            query.ActiveStatusList = new List<FightGroupActiveStatus> {
                FightGroupActiveStatus.Ongoing,
                FightGroupActiveStatus.WillStart
            };
            var data = FightGroupApplication.GetActives(query);
            var datalist = data.Models.ToList();
            foreach (DTO.FightGroupActiveListModel item in datalist)
            {
                if (!string.IsNullOrWhiteSpace(item.IconUrl))
                    item.IconUrl = Core.HimallIO.GetRomoteImagePath(item.IconUrl);
            }
            return JsonResult<dynamic>(new { rows = datalist, total = data.Total });
        }

        /// <summary>
        /// 拼团活动商品详情
        /// </summary>
        /// <param name="id">拼团活动ID</param>
        /// /// <param name="grouId">团活动ID</param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetActiveDetail(long id, long grouId = 0)
        {
            var userList = new List<FightGroupOrderInfo>();
            var fightGroup = FightGroupApplication.GetActive(id);
            var product = ProductManagerApplication.GetProductData(fightGroup.ProductId);
            var fightGroupData = new FightGroupActiveResult
            {
                ProductId = fightGroup.ProductId,
                ProductName = product.ProductName,
                ProductShortDescription = product.ShortDescription,
                ProductDefaultImage = product.ImagePath,
                MiniGroupPrice = fightGroup.ActiveItems.Min(p => p.ActivePrice),
                MiniSalePrice = product.MinSalePrice,

                LimitQuantity = fightGroup.LimitQuantity,
                LimitedHour = fightGroup.LimitedHour,
                LimitedNumber = fightGroup.LimitedNumber,
                StartTime = fightGroup.StartTime,
                EndTime = fightGroup.EndTime,
                Id = fightGroup.Id,
                ActiveStatus = fightGroup.ActiveStatus,
                MeasureUnit = product.MeasureUnit,
                CanBuy = product.AuditStatus == ProductAuditStatus.Audited && product.SaleStatus == ProductSaleStatus.OnSale,
            };

            var newGroupId = grouId;
            if (fightGroupData != null)
            {
                fightGroupData.IsEnd = true;
                if (fightGroup.EndTime.Date >= DateTime.Now.Date)
                {
                    fightGroupData.IsEnd = false;
                }
                //商品图片地址修正
                fightGroupData.ProductDefaultImage = HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350);
                fightGroupData.ProductImgPath = HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1);
            }
            if (fightGroupData.ProductImages != null)
            {
                //将主图相对路径处理为绝对路径
                fightGroupData.ProductImages = fightGroupData.ProductImages.Select(e => HimallIO.GetRomoteImagePath(e)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(fightGroupData.IconUrl))
            {
                fightGroupData.IconUrl = HimallIO.GetRomoteImagePath(fightGroupData.IconUrl);
            }
            bool IsUserEnter = false;
            bool isFavoriteShop = false;
            var memberId = 0L;
            decimal discount = 1M;
            if (CurrentUser != null)
            {
                memberId = CurrentUser.Id;
                discount = CurrentUser.MemberDiscount;
                isFavoriteShop = FavoriteApplication.HasFavoriteShop(product.ShopId, CurrentUser.Id);
            }

            if (newGroupId == 0)//获取已参团的用户
            {
                var list = FightGroupApplication.GetFightGroupOrderList(id, memberId);
                if (list.Count > 0)
                {
                    IsUserEnter = true;
                    newGroupId = list[0].GroupId;
                }
            }

            if (newGroupId > 0)
            {
                userList = FightGroupApplication.GetActiveUsers(id, newGroupId);
                foreach (var item in userList)
                {
                    item.Photo = !string.IsNullOrWhiteSpace(item.Photo) ? Core.HimallIO.GetRomoteImagePath(item.Photo) : "";
                    item.HeadUserIcon = !string.IsNullOrWhiteSpace(item.HeadUserIcon) ? Core.HimallIO.GetRomoteImagePath(item.HeadUserIcon) : "";
                    if (memberId.Equals(item.OrderUserId))
                    {
                        var fgroup = FightGroupApplication.GetGroup(id, newGroupId);
                        if (fgroup.BuildStatus == FightGroupBuildStatus.Ongoing) { IsUserEnter = true; }
                    }
                }
            }

            #region 商品规格
            ProductShowSkuInfoModel model = new ProductShowSkuInfoModel();
            model.MinSalePrice = fightGroup.ActiveItems.Min(p => p.ActivePrice);
            model.ProductImagePath = string.IsNullOrWhiteSpace(product.ImagePath) ? "" : HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)Himall.CommonModel.ImageSize.Size_350);
            //var activeItems = FightGroupApplication.GetActiveItemsSimp(id);

            List<SKUDataModel> skudata = new List<SKUDataModel>();
            foreach (var item in fightGroup.ActiveItems)
            {
                //var activeItem = activeItems.FirstOrDefault(p => p.SkuId == item.SkuId);

                if (item == null || item.ActiveStock <= 0)
                    continue;
                var sku = product.Skus.FirstOrDefault(p => p.Id == item.SkuId);
                skudata.Add(new SKUDataModel
                {
                    SkuId = item.SkuId,
                    Color = sku.Color,
                    Size = sku.Size,
                    Version = sku.Version,
                    Stock = (int)item.ActiveStock,
                    CostPrice = sku.CostPrice,
                    SalePrice = sku.SalePrice,
                    Price = item.ActivePrice,
                });
            }

            model.ColorAlias = product.ColorAlias;
            model.SizeAlias = product.SizeAlias;
            model.VersionAlias = product.VersionAlias;

            if (fightGroup.ActiveItems != null && fightGroup.ActiveItems.Count > 0)
            {
                long colorId, sizeId, versionId;
                foreach (var item in fightGroup.ActiveItems)
                {
                    var sku = product.Skus.FirstOrDefault(p => p.Id == item.SkuId);
                    var specs = sku.Id.Split('_').Select(p => long.Parse(p)).ToList();
                    colorId = specs[1]; sizeId = specs[2]; versionId = specs[3];
                    if (colorId > 0)
                    {
                        if (!model.Color.Any(v => v.Value.Equals(sku.Color)))
                        {
                            var stock = fightGroup.ActiveItems.Where(p => p.Color.Equals(sku.Color)).Sum(p => p.ActiveStock);
                            model.Color.Add(new ProductSKU
                            {
                                Name = "选择" + product.ColorAlias,
                                EnabledClass = stock > 0 ? "enabled" : "disabled",
                                SelectedClass = "",
                                SkuId = colorId,
                                Value = sku.Color,
                                Img = string.IsNullOrWhiteSpace(sku.ShowPic) ? "" : Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                            });
                        }
                    }

                    if (sizeId > 0)
                    {
                        if (!model.Size.Any(v => v.Value.Equals(sku.Size)))
                        {
                            var stock = fightGroup.ActiveItems.Where(s => s.Size.Equals(sku.Size)).Sum(p => p.ActiveStock);
                            model.Size.Add(new ProductSKU
                            {
                                //Name = "选择尺码",
                                Name = "选择" + product.SizeAlias,
                                EnabledClass = stock > 0 ? "enabled" : "disabled",
                                SelectedClass = "",
                                SkuId = sizeId,
                                Value = sku.Size
                            });
                        }
                    }


                    if (versionId > 0)
                    {
                        if (!model.Version.Any(v => v.Value.Equals(sku.Version)))
                        {
                            var stock = fightGroup.ActiveItems.Where(p => p.Version.Equals(sku.Version)).Sum(p => p.ActiveStock);
                            model.Version.Add(new ProductSKU
                            {
                                //Name = "选择规格",
                                Name = "选择" + product.VersionAlias,
                                EnabledClass = stock > 0 ? "enabled" : "disabled",
                                SelectedClass = "",
                                SkuId = versionId,
                                Value = sku.Version
                            });
                        }
                    }

                }
            }
            #endregion

            //提供服务（消费者保障、七天无理由、及时发货）
            var cashDepositModel = ProductManagerApplication.GetProductEnsure(product.Id);

            var GroupsData = FightGroupApplication.GetGroups(id, 10);
            foreach (var item in GroupsData)
            {
                TimeSpan mid = item.AddGroupTime.AddHours((double)item.LimitedHour) - DateTime.Now;
                item.Seconds = (int)mid.TotalSeconds;
                item.EndHourOrMinute = item.ShowHourOrMinute(item.GetEndHour);
                item.HeadUserIcon = !string.IsNullOrWhiteSpace(item.HeadUserIcon) ? Core.HimallIO.GetRomoteImagePath(item.HeadUserIcon) : "";
            }
            if (memberId > 0 && GroupsData.Any(p => p.HeadUserId == memberId))
                IsUserEnter = true;


            var commentSummary = CommentApplication.GetSummary(product.Id);
            #region 店铺信息
            VShopShowShopScoreModel modelShopScore = new VShopShowShopScoreModel();
            modelShopScore.ShopId = product.ShopId;
            var shop = ShopApplication.GetShop(product.ShopId);
            if (shop == null)
            {
                throw new HimallException("错误的店铺信息");
            }

            modelShopScore.ShopName = shop.ShopName;

            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(fightGroupData.ShopId);

            //宝贝与描述
            modelShopScore.ProductAndDescription = statistic.ProductAndDescription;
            modelShopScore.ProductAndDescriptionPeer = statistic.ProductAndDescriptionPeer;
            modelShopScore.ProductAndDescriptionMin = statistic.ProductAndDescriptionMin;
            modelShopScore.ProductAndDescriptionMax = statistic.ProductAndDescriptionMax;
            //卖家服务态度
            modelShopScore.SellerServiceAttitude = statistic.SellerServiceAttitude;
            modelShopScore.SellerServiceAttitudePeer = statistic.SellerServiceAttitudePeer;
            modelShopScore.SellerServiceAttitudeMax = statistic.SellerServiceAttitudeMax;
            modelShopScore.SellerServiceAttitudeMin = statistic.SellerServiceAttitudeMin;
            //卖家发货速度
            modelShopScore.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            modelShopScore.SellerDeliverySpeedPeer = statistic.SellerDeliverySpeedPeer;
            modelShopScore.SellerDeliverySpeedMax = statistic.SellerDeliverySpeedMax;
            modelShopScore.sellerDeliverySpeedMin = modelShopScore.sellerDeliverySpeedMin;
            #endregion

            modelShopScore.ProductNum = ServiceProvider.Instance<ProductService>.Create.GetOnSaleCountData(product.ShopId);
            modelShopScore.IsFavoriteShop = false;
            if (CurrentUser != null)
            {
                modelShopScore.IsFavoriteShop = FavoriteApplication.HasFavoriteShop(fightGroupData.ShopId, CurrentUser.Id);
            }

            long vShopId;
            var vshopinfo = ServiceProvider.Instance<VShopService>.Create.GetVshopDataByShopId(shop.Id);
            if (vshopinfo == null)
            {
                vShopId = -1;
            }
            else
            {
                vShopId = vshopinfo.Id;
            }
            modelShopScore.VShopId = vShopId;
            modelShopScore.VShopLog = vshopinfo.Logo;

            if (!string.IsNullOrWhiteSpace(modelShopScore.VShopLog))
            {
                modelShopScore.VShopLog = Himall.Core.HimallIO.GetRomoteImagePath(modelShopScore.VShopLog);
            }
            #endregion
            // 根据运费模板获取发货地址
            var template = FreightTemplateApplication.GetFreightTemplate(product.FreightTemplateId);
            string productAddress = string.Empty;
            if (template != null)
            {
                var fullName = RegionApplication.GetFullName(template.SourceAddress);
                if (fullName != null)
                {
                    var ass = fullName.Split(' ');
                    if (ass.Length >= 2)
                    {
                        productAddress = ass[0] + " " + ass[1];
                    }
                    else
                    {
                        productAddress = ass[0];
                    }
                }
            }
            var ProductAddress = productAddress;
            var FreightTemplate = template;

            VShopShowPromotionModel modelVshop = new VShopShowPromotionModel();
            modelVshop.ShopId = product.ShopId;
            var shopInfo = ShopApplication.GetShop(product.ShopId);
            if (shopInfo == null)
            {
                throw new HimallException("错误的店铺编号");
            }

            modelVshop.FreeFreight = shop.FreeFreight;



            //统计商品浏览量、店铺浏览人数
            StatisticApplication.StatisticVisitCount(product.Id, product.ShopId);
            //商品描述
            var productDescription = ProductManagerApplication.GetDescriptionContent(product.Id);


            var shopItem = ShopApplication.GetShop(product.ShopId);
            var minsaleprice = shopItem.IsSelf ? (fightGroupData.MiniSalePrice * discount) : fightGroupData.MiniSalePrice;
            fightGroupData.MiniSalePrice = decimal.Parse(minsaleprice.ToString("F2"));
            string loadShowPrice = string.Empty;//app拼团详细页加载时显示的区间价
            loadShowPrice = fightGroupData.MiniSalePrice.ToString("f2");

            if (fightGroup != null && fightGroup.ActiveItems.Count() > 0)
            {
                decimal min = fightGroup.ActiveItems.Min(s => s.ActivePrice);
                decimal max = fightGroup.ActiveItems.Max(s => s.ActivePrice);
                loadShowPrice = (min < max) ? (min.ToString("f2") + " - " + max.ToString("f2")) : min.ToString("f2");
            }

            long roomId = LiveApplication.IsLiveProduct(product.Id);
            var _result = new
            {
                success = true,
                IsLive = roomId > 0 ? true : false,
                roomId = roomId,
                FightGroupData = fightGroupData,
                ShowSkuInfo = new
                {
                    ColorAlias = model.ColorAlias,
                    SizeAlias = model.SizeAlias,
                    VersionAlias = model.VersionAlias,
                    MinSalePrice = model.MinSalePrice,
                    ProductImagePath = model.ProductImagePath,
                    Color = model.Color.OrderByDescending(p => p.SkuId),
                    Size = model.Size.OrderByDescending(p => p.SkuId),
                    Version = model.Version.OrderByDescending(p => p.SkuId)
                },
                IsFavoriteShop = isFavoriteShop,
                ShowPromotion = modelVshop,
                ShowNewCanJoinGroup = GroupsData,
                ReviewCount = commentSummary.Total,
                ProductDescription = productDescription.Replace("src=\"/Storage/", "src=\"" + Core.HimallIO.GetRomoteImagePath("/Storage") + "/"),
                ShopScore = modelShopScore,
                CashDepositsServer = cashDepositModel,
                ProductAddress = ProductAddress,
                userList = userList,
                IsUserEnter = IsUserEnter,
                SkuData = skudata,
                IsOpenLadder = product.IsOpenLadder,
                VideoPath = string.IsNullOrWhiteSpace(product.VideoPath) ? string.Empty : Himall.Core.HimallIO.GetRomoteImagePath(product.VideoPath),
                LoadShowPrice = loadShowPrice,   //商品时区间价
                ProductSaleCountOnOff = (SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1),//是否显示销量
                SaleCounts = fightGroup.ActiveItems.Sum(d => d.BuyCount),    //销量
                FreightStr = FreightTemplateApplication.GetFreightStr(product.Id, FreightTemplate, memberId, product.ProductType),//运费多少或免运费
                SendTime = (FreightTemplate != null && !string.IsNullOrEmpty(FreightTemplate.SendTime) ? (FreightTemplate.SendTime + "h内发货") : ""), //运费模板发货时间
            };
            return JsonResult<dynamic>(_result);
        }

        /// <summary>
        /// 获取能参加的拼团活动
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetActiveDetailSec(string ids = "")
        {
            ////获取能参加的拼团活动
            List<FightGroupBuildStatus> status = new List<FightGroupBuildStatus>();
            status.Add(FightGroupBuildStatus.Ongoing);
            var GroupsData = new List<FightGroupsListModel>();

            string[] activeIds = ids.Split(',');
            long[] unActiveId = new long[activeIds.Length];
            for (int i = 0; i < activeIds.Length; i++)
            {
                unActiveId[i] = Int64.Parse(activeIds[i]);
            }
            GroupsData = FightGroupApplication.GetCanJoinGroupsSecond(unActiveId, status);

            foreach (var item in GroupsData)
            {
                TimeSpan mid = item.AddGroupTime.AddHours((double)item.LimitedHour) - DateTime.Now;
                item.Seconds = (int)mid.TotalSeconds;
            }
            var _result = new
            {
                ShowNewCanJoinGroup = GroupsData
            };
            return JsonResult<dynamic>(_result);
        }

        /// <summary>
        /// 获取拼团团组详情
        /// </summary>
        /// <param name="activeId">拼团活动ID</param>
        /// <param name="groupId">拼团团组ID</param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetGroupDetail(long activeId, long groupId)
        {
            var data = FightGroupApplication.GetGroup(activeId, groupId);
            if (data == null)
            {
                throw new HimallException("错误的拼团信息");
            }
            return JsonResult<dynamic>(data);
        }

        /// <summary>
        /// 根据用户ID获取拼团订单列表
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetFightGroupOrderByUser(int pageSize = 5, int pageNo = 1)
        {
            CheckUserLogin();

            var data = FightGroupApplication.GetFightGroupOrderByUser(CurrentUserId, pageSize, pageNo);
            return JsonResult<dynamic>(new { rows = data.Models, total = data.Total });
        }

        /// <summary>
        /// 获取拼团订单详情
        /// </summary>
        /// <param name="Id">订单Id</param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetFightGroupOrderDetail(long id)
        {
            var userList = new List<FightGroupOrderInfo>();

            var orderDetail = FightGroupApplication.GetFightGroupOrderStatusByOrderId(id);
            //团组活动信息
            orderDetail.UserInfo = new List<UserInfo>();
            var data = FightGroupApplication.GetActive((long)orderDetail.ActiveId, false, true);
            Mapper.CreateMap<FightGroupActiveInfo, FightGroupActiveModel>();
            //规格映射
            Mapper.CreateMap<FightGroupActiveItemInfo, FightGroupActiveItemModel>();

            FightGroupsModel groupsdata = FightGroupApplication.GetGroup(orderDetail.ActiveId, orderDetail.GroupId);
            if (groupsdata == null)
            {
                throw new HimallException("错误的拼团信息");
            }
            if (data != null)
            {
                //商品图片地址修正
                data.ProductDefaultImage = HimallIO.GetRomoteProductSizeImage(data.ProductImgPath, 1, (int)ImageSize.Size_350);
            }
            orderDetail.AddGroupTime = groupsdata.AddGroupTime;
            orderDetail.GroupStatus = groupsdata.BuildStatus;
            orderDetail.ProductId = data.ProductId;
            orderDetail.ProductName = data.ProductName;

            orderDetail.IconUrl = HimallIO.GetRomoteProductSizeImage(data.ProductImgPath, 1, (int)ImageSize.Size_350);//result.IconUrl;
            var currentsku = ProductManagerApplication.GetSKUInfo(orderDetail.SkuId);
            if (currentsku != null)
            {
                if (!string.IsNullOrEmpty(currentsku.ShowPic))
                {
                    orderDetail.IconUrl = HimallIO.GetRomoteImagePath(currentsku.ShowPic);
                }
            }
            orderDetail.thumbs = HimallIO.GetRomoteProductSizeImage(data.ProductImgPath, 1, (int)ImageSize.Size_100);
            //在使用之后，再修正为绝对路径
            data.ProductImgPath = HimallIO.GetRomoteImagePath(data.ProductImgPath);

            orderDetail.MiniGroupPrice = data.MiniGroupPrice;
            TimeSpan mids = DateTime.Now - (DateTime)orderDetail.AddGroupTime;
            orderDetail.Seconds = (int)(data.LimitedHour * 3600) - (int)mids.TotalSeconds;
            orderDetail.LimitedHour = data.LimitedHour.GetValueOrDefault();
            orderDetail.LimitedNumber = data.LimitedNumber.GetValueOrDefault();
            orderDetail.JoinedNumber = groupsdata.JoinedNumber;
            orderDetail.OverTime = groupsdata.OverTime.HasValue ? groupsdata.OverTime : orderDetail.AddGroupTime.AddHours((double)orderDetail.LimitedHour);
            if (orderDetail.OverTime.HasValue)
            {
                orderDetail.OverTime = DateTime.Parse(orderDetail.OverTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            //拼装已参团成功的用户
            userList = FightGroupApplication.GetActiveUsers((long)orderDetail.ActiveId, (long)orderDetail.GroupId);
            foreach (var userItem in userList)
            {
                var userInfo = new UserInfo();
                userInfo.Photo = !string.IsNullOrWhiteSpace(userItem.Photo) ? Core.HimallIO.GetRomoteImagePath(userItem.Photo) : "";
                userInfo.UserName = userItem.UserName;
                userInfo.JoinTime = userItem.JoinTime;
                orderDetail.UserInfo.Add(userInfo);
            }
            //获取团长信息
            var GroupsData = ServiceProvider.Instance<FightGroupService>.Create.GetGroup(orderDetail.ActiveId, orderDetail.GroupId);
            if (GroupsData != null)
            {
                orderDetail.HeadUserName = GroupsData.HeadUserName;
                orderDetail.HeadUserIcon = !string.IsNullOrWhiteSpace(GroupsData.HeadUserIcon) ? Core.HimallIO.GetRomoteImagePath(GroupsData.HeadUserIcon) : "";
                orderDetail.ShowHeadUserIcon = !string.IsNullOrWhiteSpace(GroupsData.ShowHeadUserIcon) ? Core.HimallIO.GetRomoteImagePath(GroupsData.ShowHeadUserIcon) : "";
            }
            //商品评论数
            var product = ServiceProvider.Instance<ProductService>.Create.GetProduct(orderDetail.ProductId);
            var comCount = CommentApplication.GetCommentCountByProduct(product.Id);
            //商品描述

            var ProductDescription = ObjectContainer.Current.Resolve<ProductService>().GetProductDescription(orderDetail.ProductId);
            if (ProductDescription == null)
            {
                throw new Himall.Core.HimallException("错误的商品编号");
            }

            string description = ProductDescription.ShowMobileDescription.Replace("src=\"/Storage/", "src=\"" + Core.HimallIO.GetRomoteImagePath("/Storage/") + "/");//商品描述

            return JsonResult<dynamic>(new
            {
                OrderDetail = orderDetail,
                ComCount = comCount,
                ProductDescription = description
            });
        }


    }
}
