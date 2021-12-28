using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.Distribution;
using Himall.DTO.QueryModel;
using Himall.Service;
using System.Collections.Generic;
using System.Linq;
using System;
using Himall.Entities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Himall.Core.Plugins.Payment;
using Himall.Core.Plugins;
using Himall.Core.Plugins.Message;
using System.Web;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Drawing;
using System.Net;
using System.Net.Security;
using Newtonsoft.Json.Linq;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using ThoughtWorks.QRCode.Codec;

namespace Himall.Application
{
    /// <summary>
    /// 商品类别
    /// </summary>
    public class DistributionApplication : BaseApplicaion<DistributionService>
    {
        private static DistributionService _DistributionService = ObjectContainer.Current.Resolve<DistributionService>();
        private static OrderService _OrderService = ObjectContainer.Current.Resolve<OrderService>();
        private static ShopService _ShopService = ObjectContainer.Current.Resolve<ShopService>();
        private static RefundService _RefundService = ObjectContainer.Current.Resolve<RefundService>();
        /// <summary>
        /// 获取分销商品集
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<DistributionProduct> GetProducts(DistributionProductQuery query)
        {
            if (query.CategoryId.HasValue)
            {
                var categories = GetService<CategoryService>().GetAllCategoryByParent(query.CategoryId.Value);
                query.Categories = categories.Select(p => p.Id).ToList();
                query.Categories.Add(query.CategoryId.Value);
            }
            var result = _DistributionService.GetProducts(query);
            foreach (var item in result.Models)
            {
                item.DefaultImage = HimallIO.GetRomoteProductSizeImage(item.ImagePath, 1, (int)ImageSize.Size_500);
                item.ImagePath = item.DefaultImage;
                item.NoSettlementAmount = Math.Round(item.NoSettlementAmount, 2, MidpointRounding.AwayFromZero);
            }

            FillMarketing(result.Models);
            return result;
        }


        private static void FillMarketing(List<DistributionProduct> data)
        {
            var products = data.Select(p => p.ProductId).ToList();
            var groups = ServiceProvider.Instance<FightGroupService>.Create.GetGoingByProduct(products).ToList();
            var flashs = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetGoingByProduct(products).ToList();
            foreach (var item in data)
            {
                item.ActiveType = 0;
                item.ActivityId = 0;

                var group = groups.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (group != null)
                {
                    item.ActiveType = 2;
                    item.ActivityId = group.Id;
                    item.MinSalePrice = group.Items.Min(p => p.ActivePrice);
                }
                var flash = flashs.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (flash != null)
                {
                    item.ActiveType = 1;
                    item.ActivityId = flash.Id;
                    item.MinSalePrice = flash.Items.Min(p => p.Price);
                }
            }
        }
        /// <summary>
        /// 分销商品(忽略分页)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<DistributionProduct> GetProductsAll(DistributionProductQuery query)
        {
            if (query.CategoryId.HasValue)
            {
                var categories = GetService<CategoryService>().GetAllCategoryByParent(query.CategoryId.Value);
                query.Categories = categories.Select(p => p.Id).ToList();
                query.Categories.Add(query.CategoryId.Value);
            }
            var result = _DistributionService.GetProductsAll(query);
            return result;
        }

        /// <summary>
        /// 获取有分销商品的一级分类
        /// </summary>
        /// <returns></returns>
        public static List<CategoryInfo> GetHaveDistributionProductTopCategory()
        {
            return _DistributionService.GetHaveDistributionProductTopCategory();
        }
        /// <summary>
        /// 获取分销商品
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static DistributionProduct GetProduct(long productId)
        {
            DistributionProduct result = _DistributionService.GetProduct(productId);
            return result;
        }
        /// <summary>
        /// 获取分销中的商品
        /// </summary>
        /// <param name="curshopid"></param>
        /// <returns></returns>
        public static List<long> GetAllDistributionProductIds(long shopId)
        {
            return _DistributionService.GetAllDistributionProductIds(shopId);
        }
        /// <summary>
        /// 推广分销商品
        /// </summary>
        /// <param name="productIds"></param>
        public static void AddSpreadProducts(IEnumerable<long> productIds, long shopId)
        {
            _DistributionService.AddSpreadProducts(productIds, shopId);
        }
        /// <summary>
        /// 取消推广分销商品
        /// </summary>
        /// <param name="productIds"></param>
        public static void RemoveSpreadProducts(IEnumerable<long> productIds, long? shopId = null)
        {
            _DistributionService.RemoveSpreadProducts(productIds, shopId);
        }
        /// <summary>
        /// 修改商品分佣比例
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="rate"></param>
        /// <param name="level"></param>
        public static void SetProductBrokerageRate(long productId, decimal rate, decimal? rate2 = null, decimal? rate3 = null)
        {
            var config = SiteSettingApplication.SiteSettings;
            var pro = _DistributionService.GetProduct(productId);
            if (pro == null)
                throw new HimallException("分销商品不存在！");
            pro.BrokerageRate1 = rate;
            pro.BrokerageRate2 = rate2 ?? pro.BrokerageRate2;
            pro.BrokerageRate3 = rate3 ?? pro.BrokerageRate3;
            var sumrate = rate;
            if (rate < 0.1m || rate > 100)
            {
                throw new HimallException("错误的分佣比例！");
            }
            if (config.DistributionMaxLevel > 1)
            {
                if (!rate2.HasValue)
                {
                    throw new HimallException("错误的分佣比例！");
                }
                if (rate2 < 0.1m || rate2 > 100)
                {
                    throw new HimallException("错误的分佣比例！");
                }
                sumrate += rate2.Value;
            }
            if (config.DistributionMaxLevel > 2)
            {
                if (!rate3.HasValue)
                {
                    throw new HimallException("错误的分佣比例！");
                }
                if (rate3 < 0.1m || rate3 > 100)
                {
                    throw new HimallException("错误的分佣比例！");
                }
                sumrate += rate3.Value;
            }
            if (sumrate > config.DistributionMaxBrokerageRate)
            {
                throw new HimallException("多级比例总和需在 0.1% ~ " + config.DistributionMaxBrokerageRate + "% 之间");
            }
            _DistributionService.SetProductBrokerageRate(pro.ProductId, pro.BrokerageRate1, pro.BrokerageRate2, pro.BrokerageRate3);
        }
        /// <summary>
        /// 获取默认分佣比
        /// </summary>
        /// <param name="shopId"></param>
        public static DistributionShopRateConfigInfo GetDefaultBrokerageRate(long shopId)
        {
            var result = _DistributionService.GetDefaultBrokerageRate(shopId);
            if (result == null)
            {
                result = new DistributionShopRateConfigInfo()
                {
                    ShopId = shopId,
                    BrokerageRate1 = 0,
                    BrokerageRate2 = 0,
                    BrokerageRate3 = 0,
                };
                _DistributionService.UpdateDefaultBrokerageRate(shopId, result);
                result = _DistributionService.GetDefaultBrokerageRate(shopId);
            }
            return result;
        }


        public static QueryPageModel<BrokerageOrder> GetBrokerageOrders(BrokerageOrderQuery query)
        {
            var data = Service.GetBrokerageOrders(query);

            //填充 门店名 与 会员名
            var shops = GetService<ShopService>().GetShops(data.Models.Select(p => p.ShopId));
            var memberIds = new List<long>();
            memberIds.AddRange(data.Models.Select(p => p.SuperiorId1));
            memberIds.AddRange(data.Models.Select(p => p.SuperiorId2));
            memberIds.AddRange(data.Models.Select(p => p.SuperiorId3));
            var members = GetService<MemberService>().GetMembers(memberIds);
            data.Models.ForEach(item =>
            {
                item.ShopName = shops.FirstOrDefault(p => p.Id == item.ShopId)?.ShopName ?? string.Empty;
                item.SuperiorName1 = members.FirstOrDefault(p => p.Id == item.SuperiorId1)?.UserName ?? string.Empty;
                item.SuperiorName2 = members.FirstOrDefault(p => p.Id == item.SuperiorId2)?.UserName ?? string.Empty;
                item.SuperiorName3 = members.FirstOrDefault(p => p.Id == item.SuperiorId3)?.UserName ?? string.Empty;
            });
            return data;
        }

        /// <summary>
        /// 分销订单列表(忽略分页)
        /// </summary>
        /// <param name="query"></param>
        /// <param name="isShopName">是否填充商铺名称</param>
        /// <param name="isShopName">是否填分销员(会员)</param>
        /// <returns></returns>
        public static List<BrokerageOrder> GetBrokerageOrdersAll(BrokerageOrderQuery query, bool isShopName = false, bool isSupperior = false)
        {
            var data = Service.GetBrokerageOrdersAll(query);

            if (isShopName)
            {
                //填充 门店名
                var shops = GetService<ShopService>().GetShops(data.Select(p => p.ShopId));
                if (isSupperior)//填充分销员
                {
                    var memberIds = new List<long>();
                    memberIds.AddRange(data.Select(p => p.SuperiorId1));
                    memberIds.AddRange(data.Select(p => p.SuperiorId2));
                    memberIds.AddRange(data.Select(p => p.SuperiorId3));
                    var members = GetService<MemberService>().GetMembers(memberIds);
                    data.ForEach(item =>
                    {
                        item.ShopName = shops.FirstOrDefault(p => p.Id == item.ShopId)?.ShopName ?? string.Empty;
                        item.SuperiorName1 = members.FirstOrDefault(p => p.Id == item.SuperiorId1)?.UserName ?? string.Empty;
                        item.SuperiorName2 = members.FirstOrDefault(p => p.Id == item.SuperiorId2)?.UserName ?? string.Empty;
                        item.SuperiorName3 = members.FirstOrDefault(p => p.Id == item.SuperiorId3)?.UserName ?? string.Empty;
                    });
                }
                else
                {
                    data.ForEach(item =>
                    {
                        item.ShopName = shops.FirstOrDefault(p => p.Id == item.ShopId)?.ShopName ?? string.Empty;
                    });
                }
            }
            return data;
        }

        public static QueryPageModel<BrokerageProduct> GetBrokerageProduct(BrokerageProductQuery query)
        {
            return Service.GetBrokerageProduct(query);
        }

        /// <summary>
        /// 获取分销明细列表(忽略分页)
        /// </summary>
        /// <param name="query"></param>
        /// <param name="isShopName">是否填充商铺名称</param>
        /// <returns></returns>
        public static List<BrokerageProduct> GetBrokerageProductAll(BrokerageProductQuery query, bool isShopName = false)
        {
            var data = Service.GetBrokerageProductAll(query);

            if (isShopName)
            {
                //填充 门店名
                var shops = GetService<ShopService>().GetShops(data.Select(p => p.ShopId));
                data.ForEach(item =>
                {
                    item.ShopName = shops.FirstOrDefault(p => p.Id == item.ShopId)?.ShopName ?? string.Empty;
                });
            }
            return Service.GetBrokerageProductAll(query);
        }

        /// <summary>
        /// 修改默认分佣比
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="data"></param>
        public static void UpdateDefaultBrokerageRate(long shopId, DistributionShopRateConfigInfo data)
        {
            _DistributionService.UpdateDefaultBrokerageRate(shopId, data);
        }
        /// <summary>
        /// 重置非开放等级分佣比
        /// </summary>
        /// <param name="maxLevel"></param>
        public static void ResetDefaultBrokerageRate(int maxLevel)
        {
            _DistributionService.ResetDefaultBrokerageRate(maxLevel);
        }

        public static QueryPageModel<DistributorRecordInfo> GetRecords(DistributorRecordQuery query)
        {
            return Service.GetRecords(query);
        }
        /// <summary>
        /// 获取销售员
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public static DistributorInfo GetDistributor(long memberId)
        {
            return _DistributionService.GetDistributor(memberId);
        }

        public static Distributor GetDistributorDTO(long member)
        {
            var model = Service.GetDistributorBase(member);
            var distributor = model.Map<Distributor>();
            distributor.Member = GetService<MemberService>().GetMember(member);
        
            distributor.Grade = GetDistributorGrade(distributor.GradeId) ?? new DistributorGradeInfo();
            return distributor;
        }

        /// <summary>
        /// 获取未结算佣金
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static decimal GetNoSettlementAmount(long member)
        {
            return Service.GetNoSettlementAmount(member);
        }
        /// <summary>
        /// 获得分销业绩
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static DistributionAchievement GetAchievement(long member)
        {
            var settings = SiteSettingApplication.SiteSettings;
            var result = Service.GetSubAchievement(member);
            if (result == null)
            {
                result = new DistributionAchievement
                {
                    MemberId = member
                };
            }
            return result;
        }

        /// <summary>
        /// 获取销售员列表
        /// </summary>
        public static QueryPageModel<DistributorListDTO> GetDistributors(DistributorQuery query)
        {
            var result = new QueryPageModel<DistributorListDTO>();
            var data = _DistributionService.GetDistributors(query);
            result.Models = data.Models.Map<List<DistributorListDTO>>();
            result.Total = data.Total;
            return result;
        }

        /// <summary>
        /// 获取销售员列表(忽略分页)
        /// </summary>
        public static List<DistributorListDTO> GetDistributorsAll(DistributorQuery query)
        {
            var result = new List<DistributorListDTO>();
            var data = _DistributionService.GetDistributorsAll(query);
            result = data.Map<List<DistributorListDTO>>();
            return result;
        }

        public static Dictionary<int, List<long>> GetSubordinate(long superior)
        {
            return Service.GetSubordinate(superior, SiteSettingApplication.SiteSettings.DistributionMaxLevel);
        }

        /// <summary>
        /// 获取佣金管理列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<Distributor> GetDistributors(DistributorSubQuery query)
        {
            //获得所有下级
            if (query.SuperiorId > 0)
            {
                var subordinate = Service.GetSubordinate(query.SuperiorId, query.Level);
                query.Members = subordinate[query.Level];
            }
            var data = Service.GetNewDistributors(query);
            var result = data.Models.Map<List<Distributor>>();
            var memberids = data.Models.Select(p => p.MemberId).ToList();
            var members = GetService<MemberService>().GetMembers(memberids);
            var achievements = Service.GetAchievement(memberids);
            result.ForEach(item =>
            {
                item.Member = members.FirstOrDefault(p => p.Id == item.MemberId);
                item.Achievement = achievements.FirstOrDefault(p => p.MemberId == item.MemberId);
            });
            return new QueryPageModel<Distributor>
            {
                Models = result,
                Total = data.Total
            };
        }

        /// <summary>
        /// 获取佣金管理列表(忽略分页)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<Distributor> GetDistributorsAll(DistributorSubQuery query)
        {
            //获得所有下级
            if (query.SuperiorId > 0)
            {
                var subordinate = Service.GetSubordinate(query.SuperiorId, query.Level);
                query.Members = subordinate[query.Level];
            }
            var data = Service.GetNewDistributorsAll(query);
            var result = data.Map<List<Distributor>>();
            var memberids = data.Select(p => p.MemberId).ToList();
            var members = GetService<MemberService>().GetMembers(memberids);
            var achievements = Service.GetAchievement(memberids);
            result.ForEach(item =>
            {
                item.Member = members.FirstOrDefault(p => p.Id == item.MemberId);
                item.Achievement = achievements.FirstOrDefault(p => p.MemberId == item.MemberId);
            });

            return result;
        }


        /// <summary>
        /// 申请成为销售员
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="shopLogo"></param>
        /// <param name="shopName"></param>
        /// <returns></returns>
        public static DistributorInfo ApplyDistributor(long memberId, string shopLogo, string shopName)
        {
            DistributorInfo result = _DistributionService.GetDistributor(memberId);
            bool isadd = false;
            if (result == null)
            {
                result = new DistributorInfo();
                isadd = true;
                result.ProductCount = 0;
                result.OrderCount = 0;
                result.SettlementAmount = 0;
                result.IsShowShopLogo = false;
            }
            result.MemberId = memberId;
            result.ShopLogo = shopLogo;
            result.ShopName = shopName;
            result.ApplyTime = DateTime.Now;
            result.DistributionStatus = (int)DistributorStatus.UnAudit;
            if (!SiteSettingApplication.SiteSettings.DistributorNeedAudit)
            {
                result.DistributionStatus = (int)DistributorStatus.Audited;
            }
            var gradeId = GetDistributorGrades().OrderByDescending(d => d.Quota).FirstOrDefault(d => d.Quota <= result.SettlementAmount)?.Id;
            result.GradeId = gradeId ?? 0;

            if (isadd)
            {
                _DistributionService.AddDistributor(result);
            }
            else
            {
                _DistributionService.UpdateDistributor(result);
            }
            var uobj = MemberApplication.GetMember(result.MemberId);
            //发送短信通知
            Task.Factory.StartNew(() =>
            {
                MessageApplication.SendMessageOnDistributorApply(result.MemberId, uobj.UserName);
                if (result.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    MessageApplication.SendMessageOnDistributorAuditSuccess(result.MemberId, uobj.UserName);
                }
            });
            return result;
        }
        /// <summary>
        /// 小店设置
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="shopLogo"></param>
        /// <param name="shopName"></param>
        /// <returns></returns>
        public static void UpdateDistributorConfig(long memberId, string shopLogo, string shopName, bool isShowLogo)
        {
            DistributorInfo result = _DistributionService.GetDistributor(memberId);
            if (result == null)
            {
                throw new HimallException("错误的编号");
            }
            result.ShopLogo = shopLogo;
            result.ShopName = shopName;
            result.IsShowShopLogo = isShowLogo;
            _DistributionService.UpdateDistributor(result);
        }
        /// <summary>
        /// 添加等级
        /// </summary>
        /// <param name="data"></param>
        public static void AddDistributorGrade(DistributorGradeInfo data)
        {
            _DistributionService.AddDistributorGrade(data);
        }
        /// <summary>
        /// 修改等级
        /// </summary>
        /// <param name="data"></param>
        public static void UpdateDistributorGrade(DistributorGradeInfo data)
        {
            _DistributionService.UpdateDistributorGrade(data);
        }
        /// <summary>
        /// 获取销售员等级
        /// </summary>
        /// <returns></returns>
        public static DistributorGradeInfo GetDistributorGrade(long id)
        {
            return _DistributionService.GetDistributorGrade(id);
        }
        /// <summary>
        /// 获取销售员等级列表
        /// </summary>
        /// <returns></returns>
        public static List<DistributorGradeInfo> GetDistributorGrades(bool IsAvailable = false)
        {
            return _DistributionService.GetDistributorGrades(IsAvailable);
        }
        /// <summary>
        /// 是否己存在同名等级
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id">0表示新增检测</param>
        /// <returns></returns>
        public static bool ExistDistributorGradesName(string name, long id)
        {
            return _DistributionService.ExistDistributorGradesName(name, id);
        }
        /// <summary>
        /// 是否己存在同条件等级
        /// </summary>
        /// <param name="quota"></param>
        /// <param name="id">0表示新增检测</param>
        /// <returns></returns>
        public static bool ExistDistributorGradesQuota(decimal quota, long id)
        {
            return _DistributionService.ExistDistributorGradesQuota(quota, id);
        }
        /// <summary>
        /// 清退销售员
        /// </summary>
        /// <param name="memberIds"></param>
        public static void RemoveDistributor(IEnumerable<long> memberIds)
        {
            _DistributionService.RemoveDistributor(memberIds);
        }
        /// <summary>
        /// 恢复销售员
        /// </summary>
        /// <param name="memberIds"></param>
        public static void RecoverDistributor(IEnumerable<long> memberIds)
        {
            _DistributionService.RecoverDistributor(memberIds);
        }
        /// <summary>
        /// 拒绝销售员申请
        /// </summary>
        /// <param name="memberIds"></param>
        /// <param name="remark"></param>
        public static void RefuseDistributor(IEnumerable<long> memberIds, string remark)
        {
            _DistributionService.RefuseDistributor(memberIds, remark);
            //发送短信通知
            Task.Factory.StartNew(() =>
            {
                foreach (var item in memberIds)
                {
                    var uobj = MemberApplication.GetMember(item);
                    var duobj = _DistributionService.GetDistributor(item);
                    MessageApplication.SendMessageOnDistributorAuditFail(item, uobj.UserName, remark, duobj.ApplyTime);
                }
            });
        }
        /// <summary>
        /// 同意销售员申请
        /// </summary>
        /// <param name="memberIds"></param>
        /// <param name="remark"></param>
        public static void AgreeDistributor(IEnumerable<long> memberIds, string remark)
        {
            _DistributionService.AgreeDistributor(memberIds, remark);
            //发送短信通知
            Task.Factory.StartNew(() =>
            {
                foreach (var item in memberIds)
                {
                    var uobj = MemberApplication.GetMember(item);
                    MessageApplication.SendMessageOnDistributorAuditSuccess(item, uobj.UserName);
                }
            });
        }
        /// <summary>
        /// 调整上级
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="superMemberId"></param>
        public static void UpdateDistributorSuperId(long MemberId, long superMemberId)
        {
            long oldsuperid = 0;
            var cur = _DistributionService.GetDistributor(MemberId);
            if (cur == null)
            {
                throw new HimallException("错误的参数");
            }
            oldsuperid = cur.SuperiorId;
            cur.SuperiorId = superMemberId;
            _DistributionService.UpdateDistributor(cur);
            //维护下级数量
            _DistributionService.SyncDistributorSubNumber(MemberId);
            _DistributionService.SyncDistributorSubNumber(superMemberId);
            if (oldsuperid != superMemberId)
            {
                _DistributionService.SyncDistributorSubNumber(oldsuperid);
            }
        }
        /// <summary>
        /// 删除销售员等级
        /// </summary>
        /// <param name="id"></param>
        public static void DeleteDistributorGrade(long id)
        {
            Service.DeleteDistributorGrade(id);
        }
        /// <summary>
        /// 获取销售员小店订单
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<DistributorBrokerageOrder> GetDistributorBrokerageOrderList(DistributionBrokerageQuery query)
        {
            var orders = _DistributionService.GetDistributorBrokerageOrderList(query);
            var orderids = orders.Models.Select(d => d.Id).ToList();
            var itemBrokerages = _DistributionService.GetDistributionBrokerageByOrderIds(orderids);
            var shopids = itemBrokerages.Select(d => d.ShopId).Distinct().ToList();
            var orderItems = _OrderService.GetOrderItemsByOrderId(orderids);
            var shops = _ShopService.GetShops(shopids);
            var refunds = _RefundService.GetAllOrderRefunds(new RefundQuery
            {
                MoreOrderId = orderids
            });
            QueryPageModel<DistributorBrokerageOrder> result = new QueryPageModel<DistributorBrokerageOrder>
            {
                Models = new List<DistributorBrokerageOrder>(),
                Total = orders.Total
            };
            foreach (var item in orders.Models)
            {
                var bitems = itemBrokerages.Where(d => d.OrderId == item.Id).ToList();
                var oitems = orderItems.Where(d => d.OrderId == item.Id).ToList();
                if (bitems == null || bitems.Count == 0)
                {
                    continue;
                }
                var first = bitems.FirstOrDefault();
                var shdatas = refunds.Where(d => d.OrderId == item.Id);
                DistributorBrokerageOrder odata = new DistributorBrokerageOrder
                {
                    OrderId = first.OrderId,
                    OrderStatus = item.OrderStatus,
                    Status = first.BrokerageStatus,
                    SettlementTime = first.SettlementTime,
                    Items = new List<DistributorBrokerageOrderItem>(),
                    OrderAmount = item.OrderAmount,
                    OrderDate = item.OrderDate,
                    IsRefundCloseOrder = (item.OrderStatus == OrderInfo.OrderOperateStatus.Close && shdatas.Any(d => d.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed))
                };
                int SuperiorLevel = 1;
                decimal BrokerageRate = 0, BrokerageAmount = 0;
                foreach (var oitem in oitems)
                {
                    string[] _skus = (new string[] { oitem.Color, oitem.Size, oitem.Version }).Where(d => !string.IsNullOrWhiteSpace(d)).ToArray();
                    var _shop = shops.FirstOrDefault(d => d.Id == oitem.ShopId);
                    DistributorBrokerageOrderItem idata = new DistributorBrokerageOrderItem
                    {
                        ProductId = oitem.ProductId,
                        ProductName = oitem.ProductName,
                        ProductDefaultImage = oitem.ThumbnailsUrl.Contains("skus") ? HimallIO.GetRomoteImagePath(oitem.ThumbnailsUrl) : HimallIO.GetRomoteProductSizeImage(oitem.ThumbnailsUrl, 1, (int)ImageSize.Size_100),
                        Sku = string.Join("、", _skus),
                        ShopId = oitem.ShopId,
                        OrderItemId = oitem.Id,
                        Quantity = oitem.Quantity,
                        ShopName = _shop.ShopName,
                        IsHasRefund = item.OrderStatus != OrderInfo.OrderOperateStatus.Close && refunds.Any(d => d.OrderItemId == oitem.Id
                         && d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund
                         && d.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed)
                    };
                    var bitem = itemBrokerages.FirstOrDefault(d => d.OrderItemId == oitem.Id);
                    if (bitem != null)
                    {
                        idata.SuperiorId1 = bitem.SuperiorId1;
                        idata.BrokerageRate1 = bitem.BrokerageRate1;
                        idata.SuperiorId2 = bitem.SuperiorId2;
                        idata.BrokerageRate2 = bitem.BrokerageRate2;
                        idata.SuperiorId3 = bitem.SuperiorId3;
                        idata.BrokerageRate3 = bitem.BrokerageRate3;
                        idata.RealPayAmount = bitem.RealPayAmount;
                        idata.SettlementTime = bitem.SettlementTime;
                        idata.Status = bitem.BrokerageStatus;
                        if (idata.SuperiorId1 == query.DistributorId)
                        {
                            SuperiorLevel = 1;
                            BrokerageRate = idata.BrokerageRate1;
                        }
                        if (idata.SuperiorId2 == query.DistributorId)
                        {
                            SuperiorLevel = 2;
                            BrokerageRate = idata.BrokerageRate2;
                        }
                        if (idata.SuperiorId3 == query.DistributorId)
                        {
                            SuperiorLevel = 3;
                            BrokerageRate = idata.BrokerageRate3;
                        }
                        BrokerageAmount += GetDivide100Number(idata.RealPayAmount * BrokerageRate);
                    }
                    odata.Items.Add(idata);
                }
                odata.SuperiorLevel = SuperiorLevel;
                odata.BrokerageAmount = BrokerageAmount;
                odata.QuantitySum = odata.Items.Sum(d => d.Quantity);
                result.Models.Add(odata);
            }
            result.Models = result.Models.OrderByDescending(d => d.OrderDate).ThenByDescending(d => d.OrderId).ToList();
            return result;
        }

        #region 销量排行


        public static QueryPageModel<BrokerageRanking> GetRankings(DistributorRankingQuery query)
        {
            var data = Service.GetRankings(query);

            var memberids = data.Models.Select(p => p.MemberId).ToList();
            var members = GetService<MemberService>().GetMembers(memberids);
            var distributors = Service.GetDistributors(memberids);
            var rankingIndex = (query.PageNo - 1) * query.PageSize;

            var rankings = data.Models.Select(item =>
            {
                var distributor = distributors.FirstOrDefault(p => p.MemberId == item.MemberId);
                return new BrokerageRanking
                {
                    Rank = ++rankingIndex,
                    Amount = item.Amount,
                    NoSettlement = item.NoSettlement,
                    Quantity = item.Quantity,
                    Settlement = item.Settlement,
                    Member = members.FirstOrDefault(p => p.Id == item.MemberId),
                    Distributor = distributor,
                    Grade = GetDistributorGrade(distributor.GradeId),
                };
            }).ToList();

            return new QueryPageModel<BrokerageRanking>
            {
                Models = rankings,
                Total = data.Total
            };
        }

        /// <summary>
        /// 获取排行数据（忽略分页）
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<BrokerageRanking> GetRankingsAll(DistributorRankingQuery query)
        {
            var data = Service.GetRankingsAll(query);

            var memberids = data.Select(p => p.MemberId).ToList();
            var members = GetService<MemberService>().GetMembers(memberids);
            var distributors = Service.GetDistributors(memberids);
            var rankingIndex = (query.PageNo - 1) * query.PageSize;

            var rankings = data.Select(item =>
            {
                var distributor = distributors.FirstOrDefault(p => p.MemberId == item.MemberId);
                return new BrokerageRanking
                {
                    Rank = ++rankingIndex,
                    Amount = item.Amount,
                    NoSettlement = item.NoSettlement,
                    Quantity = item.Quantity,
                    Settlement = item.Settlement,
                    Member = members.FirstOrDefault(p => p.Id == item.MemberId),
                    Distributor = distributor,
                    Grade = GetDistributorGrade(distributor.GradeId),
                };
            }).ToList();

            return rankings;
        }

        /// <summary>
        /// 生成报表
        /// </summary>
        public static void GenerateRankingAsync(DateTime begin, DateTime end)
        {
            if (CheckGenerating())
                throw new HimallException("上次报表生成进行中...");
            Task.Factory.StartNew(() =>
            {
                #region 仅保留一次排名报表,移除之前所有报表数据
                var batchs = Service.GetRankingBatchs();
                if (batchs.Count > 0)
                    Service.RemoveRankingBatch(batchs.Select(p => p.Id).ToList());
                #endregion

                //生成报表
                var batch = new DistributionRankingBatchInfo
                {
                    BeginTime = begin,
                    EndTime = end,
                    CreateTime = DateTime.Now,
                };
                Cache.Insert(CacheKeyCollection.GenerateDistributionRankingAsync, batch);
                try
                {
                    Service.GenerateRanking(begin, end);
                }
                catch (Exception ex)
                {
                    Log.Error("分销业绩排行报表生成出错:", ex);
                }
                finally
                {
                    Cache.Remove(CacheKeyCollection.GenerateDistributionRankingAsync);
                }
            });
        }
        /// <summary>
        /// 检查是否生成中
        /// </summary>
        /// <returns></returns>
        public static bool CheckGenerating()
        {
            return Cache.Exists(CacheKeyCollection.GenerateDistributionRankingAsync);
        }
        public static DistributionRankingBatchInfo GetLastRankingBatch()
        {
            return Service.GetLastRankingBatch();
        }
        #endregion

        /// <summary>
        /// 获取提现记录
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<DistributionWithdraw> GetWithdraws(DistributionWithdrawQuery query)
        {
            var data = Service.GetWithdraws(query);
            var list = data.Models.Map<List<DistributionWithdraw>>();
            var members = GetService<MemberService>().GetMembers(list.Select(p => p.MemberId).ToList());
            list.ForEach(item =>
            {
                item.Member = members.FirstOrDefault(p => p.Id == item.MemberId);
            });

            return new QueryPageModel<DistributionWithdraw>
            {
                Models = list,
                Total = data.Total
            };
        }

        /// <summary>
        /// 获取提现记录(忽略分页)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<DistributionWithdraw> GetWithdrawsAll(DistributionWithdrawQuery query)
        {
            var data = Service.GetWithdrawsAll(query);
            var list = data.Map<List<DistributionWithdraw>>();
            var members = GetService<MemberService>().GetMembers(list.Select(p => p.MemberId).ToList());
            list.ForEach(item =>
            {
                item.Member = members.FirstOrDefault(p => p.Id == item.MemberId);
            });

            return list;
        }

        /// <summary>
        /// 提现申请
        /// </summary>
        /// <param name="apply"></param>
        public static void ApplyWithdraw(DistributionApplyWithdraw apply)
        {
            if (!MemberApplication.VerificationPayPwd(apply.MemberId, apply.Password))
                throw new HimallException("交易密码错误");

            if (apply.Amount > SiteSettingApplication.SiteSettings.DistributorWithdrawMaxLimit)
                throw new HimallException("超过最大提现额限");

            if (apply.Amount < SiteSettingApplication.SiteSettings.DistributorWithdrawMinLimit)
                throw new HimallException("小于最低提现额限");

            var distributor = Service.GetDistributor(apply.MemberId);
            if (apply.Amount > distributor.Balance)
                throw new HimallException("超过最多提现金额");

            var settings = SiteSettingApplication.SiteSettings;

            if (apply.Type == DistributionWithdrawType.Alipay)
            {
                if (!settings.DistributorWithdrawTypes.ToLower().Contains("alipay"))
                    throw new HimallException("暂不支持支付宝提现");
                if (string.IsNullOrEmpty(apply.WithdrawAccount))
                    throw new HimallException("支付宝账户不可为空");
                if (string.IsNullOrEmpty(apply.WithdrawName))
                    throw new HimallException("真实姓名不可为空");
            }
            else if (apply.Type == DistributionWithdrawType.WeChat)
            {
                if (!settings.DistributorWithdrawTypes.ToLower().Contains("wechat"))
                    throw new HimallException("暂不支持微信提现");
                if (string.IsNullOrEmpty(apply.WithdrawAccount))
                    throw new HimallException("尚未绑定微信,请先绑定微信账户");
            }

            var info = new DistributionWithdrawInfo
            {
                Amount = apply.Amount,
                WithdrawType = apply.Type,
                MemberId = apply.MemberId,
                WithdrawAccount = apply.WithdrawAccount,
                WithdrawName = apply.WithdrawName
            };
            Service.ApplyWithdraw(info);

            //发送消息
            var member = MemberApplication.GetMember(apply.MemberId);
            var message = new MessageWithDrawInfo();
            message.UserName = member != null ? member.UserName : "";
            message.Amount = info.Amount;
            message.ApplyType = info.WithdrawType.GetHashCode();
            message.ApplyTime = info.ApplyTime;
            message.Remark = info.Remark;
            message.SiteName = SiteSettingApplication.SiteSettings.SiteName;
            Task.Factory.StartNew(() => MessageApplication.SendMessageOnDistributionMemberWithDrawApply(apply.MemberId, message));

            //预付款提现,自动审核
            if (info.WithdrawType == DistributionWithdrawType.Capital)
                AuditingWithdraw(info.Id, "System", "预存款提现,自动审核");

        }

        /// <summary>
        /// 提现审核
        /// </summary>
        public static void AuditingWithdraw(long id, string operatorName, string remark)
        {
            //审核通过
            Service.AuditingWithdraw(id, operatorName, remark);
            //审核通过,自动启动支付流程
            PaymentWithdraw(id);
        }

        /// <summary>
        /// 提现拒绝
        /// </summary>
        /// <param name="id"></param>
        /// <param name="operatorName"></param>
        /// <param name="remark"></param>
        public static void RefusedWithdraw(long withdrawId, string operatorName, string remark)
        {
            Service.RefusedWithdraw(withdrawId, operatorName, remark);
        }
        /// <summary>
        /// 支付提现
        /// </summary>
        public static void PaymentWithdraw(long withdrawId)
        {
            var model = Service.GetWithdraw(withdrawId);
            try
            {
                switch (model.WithdrawType)
                {
                    case DistributionWithdrawType.Alipay:
                        var result = Payment(DistributionWithdrawType.Alipay, model.WithdrawAccount, model.Amount, $"(单号 {withdrawId})", model.Id.ToString(), model.WithdrawName);
                        Service.SuccessWithdraw(withdrawId, result.TradNo.ToString());
                        break;
                    case DistributionWithdrawType.Capital:
                        var no = MemberCapitalApplication.BrokerageTransfer(model.MemberId, model.Amount, $"(单号 {withdrawId})", model.Id.ToString());
                        Service.SuccessWithdraw(withdrawId, no.ToString());
                        break;
                    case DistributionWithdrawType.WeChat:
                        var resultWechat = Payment(DistributionWithdrawType.WeChat, model.WithdrawAccount, model.Amount, $"(单号 {withdrawId})", model.Id.ToString());
                        Service.SuccessWithdraw(withdrawId, resultWechat.TradNo.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                //支付失败(回滚提现状态)
                Service.FailWithdraw(withdrawId, ex.Message);
                throw ex;
            }
            //发送消息
            var member = MemberApplication.GetMember(model.MemberId);
            var message = new MessageWithDrawInfo();
            message.UserName = member != null ? member.UserName : "";
            message.Amount = model.Amount;
            message.ApplyType = model.WithdrawType.GetHashCode();
            message.ApplyTime = model.ApplyTime;
            message.Remark = model.Remark;
            message.SiteName = SiteSettingApplication.SiteSettings.SiteName;
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnDistributionMemberWithDrawSuccess(model.MemberId, message));
        }
        /// <summary>
        /// 调用第三方支付
        /// </summary>
        /// <param name="type"></param>
        /// <param name="account"></param>
        /// <param name="amount"></param>
        /// <param name="desc"></param>
        /// <param name="no"></param>
        /// <returns></returns>
        private static PaymentInfo Payment(DistributionWithdrawType type, string account, decimal amount, string desc, string no, string withdrawName = "")
        {
            Plugin<IPaymentPlugin> plugin = null;
            /// 支付宝真实姓名验证逻辑
            bool isCheckName = false;
            switch (type)
            {
                case DistributionWithdrawType.Alipay:
                    plugin = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.PluginInfo.PluginId.StartsWith("Himall.Plugin.Payment.Alipay"));
                    isCheckName = true;
                    break;
                case DistributionWithdrawType.WeChat:
                    var openidProvider = MemberApplication.GetMemberOpenIdInfoByOpenIdOrUnionId(account);
                    if (openidProvider != null)
                    {
                        switch (openidProvider.ServiceProvider.ToLower())
                        {
                            case "h5":
                                plugin = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.Biz.SupportPlatforms.Contains(PlatformType.Wap));
                                break;
                            case "weixinsmallprog":
                                plugin = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.Biz.SupportPlatforms.Contains(PlatformType.WeiXinSmallProg));
                                break;
                            case "himall.plugin.oauth.weixin":
                                plugin = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.Biz.SupportPlatforms.Contains(PlatformType.WeiXin));
                                break;
                        }
                        Log.Debug("ApplyWithDraw Confirm ServiceProvider=" + openidProvider.ServiceProvider);
                    }
                    else
                    {
                        plugin = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.PluginInfo.PluginId.ToLower().Contains("weixin"));
                    }
                    break;
                default:
                    throw new HimallException("不支持的支付类型");
            }
            if (plugin == null)
                throw new HimallException("未找到支付插件");

            var pay = new EnterprisePayPara()
            {
                amount = amount,
                openid = account,
                out_trade_no = no,
                check_name = isCheckName,
                re_user_name = withdrawName,
                desc = desc
            };
            try
            {
                return plugin.Biz.EnterprisePay(pay);
            }
            catch (PluginException pex)
            {
                //插件异常，直接返回错误信息
                Log.Error("调用付款接口异常：" + pex.Message);
                throw new HimallException("调用企业付款接口异常:" + pex.Message);
            }
            catch (Exception ex)
            {
                Log.Error("付款异常：" + ex.Message);
                throw new HimallException("企业付款异常:" + ex.Message);
            }
        }


        /// <summary>
        /// 获取除以100保留两位小数的结果
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static decimal GetDivide100Number(decimal data)
        {
            return decimal.Parse((data / (decimal)100).ToString("f2"));
        }

        /// <summary>
        /// 根据会员IDs获取分销员列表
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        public static List<DistributorInfo> GetDistributors(List<long> members)
        {
            return _DistributionService.GetDistributors(members);
        }

        #region 获取分销名片合成海报
        /// <summary>
        /// 获取微信我的名片合成海报
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string GetWXReferralPostUrl(MemberInfo member)
        {
            string referralPostUrl = "/Storage/master/ReferralPoster/PosterPic/Poster_Wap_" + member.Id + ".jpg";//海报图片路径
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(member.Id);

            string posterurl = "/Storage/master/ReferralPoster/ReferralPoster.js";
            if (!Himall.Core.HimallIO.ExistFile(posterurl))
                return string.Empty;//配置文件不存在返回空
            DownloadLocal(posterurl);//海报配置文件本地最新

            string SetJson = System.IO.File.ReadAllText(HttpRuntime.AppDomainAppPath.ToString() + posterurl); //获取配置文件
            if (string.IsNullOrEmpty(SetJson))
                return string.Empty;

            DistributionBusionessCardConfigModel posterjs = JsonConvert.DeserializeObject<DistributionBusionessCardConfigModel>(SetJson);
            if (posterjs == null)
                return string.Empty;

            if (Himall.Core.HimallIO.ExistFile(referralPostUrl))
            {
                var fiposter = HimallIO.GetFileMetaInfo(referralPostUrl);//海报文件基本信息
                var fi = HimallIO.GetFileMetaInfo(posterurl);//海报配置文件

                if (fi.LastModifiedTime < fiposter.LastModifiedTime)
                    return HimallIO.GetFilePath(referralPostUrl).Replace("//Storage", "/Storage").Replace("//Storage", "/Storage") + "?rnd=" + new Random().Next();
            }

            SiteSettings site = SiteSettingApplication.SiteSettings;
            string StoreName = site.SiteName;
            string UserHead = member.Photo;
            string UserName = string.IsNullOrEmpty(member.Nick) ? member.RealName : member.Nick;
            string CodeUrl = string.Empty;

            int defaultQRCode = int.Parse(posterjs.DefaultQRCode);
            if (defaultQRCode == 1)//公众号二维码
            {
                if (!string.IsNullOrEmpty(site.WeixinAppId) && !string.IsNullOrEmpty(site.WeixinAppSecret))
                {
                    string ticket = GetQRLIMITSTRSCENETicket("referraluserid:" + member.Id);
                    CodeUrl = string.Format("https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket={0}", ticket);
                }
            }

            //生成二维码图片
            Bitmap Qrimage = null;
            if (CodeUrl.Contains("weixin.qq.com"))
            {
                Log.Error("CodeUrl:" + CodeUrl);
                Qrimage = GetNetImg(CodeUrl);
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(CodeUrl))
                    {
                        Qrimage = (Bitmap)System.Drawing.Image.FromFile(HttpContext.Current.Server.MapPath(CodeUrl));
                    }
                    else
                    {
                        string codePath = (SiteSettingApplication.GetUrlHttpOrHttps(site.SiteUrl) + "/?SpreadId=" + member.Id).Replace("//?", "/?");
                        //Qrimage = Core.Helper.QRCodeHelper.Create(codePath);//二维码

                        //创建二维码生成类  
                        QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
                        //设置编码模式  
                        qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
                        //设置编码测量度  
                        qrCodeEncoder.QRCodeScale = 10;
                        //设置编码版本  
                        qrCodeEncoder.QRCodeVersion = 0;
                        //设置编码错误纠正  
                        qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;

                        Qrimage = qrCodeEncoder.Encode(codePath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("CodeUrl:" + CodeUrl + "--GetQrImageEx:" + ex.Message);
                }
            }
            int DefaultHead = int.Parse(posterjs.DefaultHead);
            if (DefaultHead == 1)//店铺logo
            {
                UserHead = currentDistributor.ShopLogo;
            }
            if (string.IsNullOrEmpty(UserHead) || (!UserHead.ToLower().StartsWith("http") && !UserHead.ToLower().StartsWith("https") && !File.Exists(HttpContext.Current.Request.MapPath(UserHead))))
                UserHead = "/images/imgnopic.jpg";

            if (DefaultHead == 2)
            {
                UserHead = "";
            }

            System.Drawing.Image logoimg;
            if (UserHead.ToLower().StartsWith("http") || UserHead.ToLower().StartsWith("https"))
            {
                logoimg = GetNetImg(UserHead); //获取网络图片 ;// new Bitmap(UserHead);
            }
            else
            {
                if (!string.IsNullOrEmpty(UserHead) && File.Exists(HttpContext.Current.Request.MapPath(UserHead)))
                {
                    logoimg = System.Drawing.Image.FromFile(HttpContext.Current.Request.MapPath(UserHead));
                }
                else
                {
                    logoimg = new Bitmap(100, 100);
                };
            }


            //转换成圆形图片
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(new Rectangle(0, 0, logoimg.Width, logoimg.Width));
            Bitmap Tlogoimg = new Bitmap(logoimg.Width, logoimg.Width);
            using (Graphics gl = Graphics.FromImage(Tlogoimg))
            { //假设bm就是你要绘制的正方形位图，已创建好
                gl.SetClip(gp);
                gl.DrawImage(logoimg, 0, 0, logoimg.Width, logoimg.Width);
            }
            logoimg.Dispose();


            ////合成二维码图像
            //if (defaultQRCode == 0)//只有当店铺二维码时才需要将头像合成到二维码中，公众号二维码不需要
            Qrimage = CombinImage(Qrimage, Tlogoimg, 80); //二维码图片

            Bitmap Cardbmp = new Bitmap(480, 735);

            Graphics g = Graphics.FromImage(Cardbmp);
            g.SmoothingMode = SmoothingMode.HighQuality; ; //抗锯齿
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.High;
            g.Clear(System.Drawing.Color.White); //白色填充

            Bitmap Bgimg = new Bitmap(100, 100);

            if (!string.IsNullOrEmpty(posterjs.BgImg) && File.Exists(HttpContext.Current.Request.MapPath(posterjs.BgImg)))
            {
                //如果背景图片存在，
                Bgimg = (Bitmap)System.Drawing.Image.FromFile(HttpContext.Current.Request.MapPath(posterjs.BgImg)); //如果存在，读取背景图片
                Bgimg = GetThumbnail(Bgimg, 735, 480); //处理成对应尺寸图片
            }

            //绘制背景图
            g.DrawImage(Bgimg, 0, 0, 480, 735);


            Font usernameFont = new Font("微软雅黑", (int)(posterjs.MyUserNameSize * 6 / 5));

            Font shopnameFont = new Font("微软雅黑", (int)(posterjs.ShopNameSize * 6 / 5));
            int shopNameSize = posterjs.ShopNameSize;

            //加入用户头像
            g.DrawImage(Tlogoimg, (int)(posterjs.PosList[0].Left * 480),
                (int)posterjs.PosList[0].Top * 735 / 490,
                (int)(((decimal)posterjs.PosList[0].Width) * 480),
                (int)(((decimal)posterjs.PosList[0].Width) * 480)
                );

            StringFormat StringFormat = new StringFormat(StringFormatFlags.DisplayFormatControl);
            string myusername = string.IsNullOrEmpty(posterjs.MyUserName) ? string.Empty : posterjs.MyUserName.Replace(@"{{昵称}}", "$");
            string shopname = string.IsNullOrEmpty(posterjs.ShopName) ? string.Empty : posterjs.ShopName.Replace(@"{{商城名称}}", "$");
            string[] myusernameArray = myusername.Split('$');
            string[] shopnameArray = shopname.Split('$');

            //写昵称
            g.DrawString(myusernameArray[0], usernameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.MyUserNameColor)),
                 (int)(posterjs.PosList[1].Left * 480),
                 (int)(posterjs.PosList[1].Top * 735 / 490),
                StringFormat);

            if (myusernameArray.Length > 1)
            {
                var spcSize1 = g.MeasureString(" ", usernameFont);
                var myusernameSize = g.MeasureString(myusernameArray[0], usernameFont);
                g.DrawString(UserName, usernameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.NickNameColor)),
               (int)(posterjs.PosList[1].Left * 480) + myusernameSize.Width - spcSize1.Width,
                (int)(posterjs.PosList[1].Top * 735 / 490),
               StringFormat);

                var usernameSize = g.MeasureString(UserName, usernameFont);
                g.DrawString(myusernameArray[1], usernameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.MyUserNameColor)),
               (int)(posterjs.PosList[1].Left * 480) + myusernameSize.Width - spcSize1.Width * 2 + usernameSize.Width,
                (int)(posterjs.PosList[1].Top * 735 / 490),
               StringFormat);

            }

            //写店铺名
            var lineWidth = 520 - (int)(posterjs.PosList[2].Left * 480);
            int lineIndex = 0;//当前行数，每20个字为一行

            int LINECOUNT = 21;//每行摆多少文字
            if (shopNameSize != 14)//如果不是默认14号字体则计算一行显示的字数,根据字体大小计算出一行最多输出的字符个数（中文）
            {
                string teststr = "中";
                for (int i = 0; i < 50; i++)
                {
                    string tempstr = SelfLoop(teststr, i);
                    var spctempSize = g.MeasureString(tempstr, shopnameFont);
                    if (spctempSize.Width >= lineWidth)
                    {
                        LINECOUNT = i - 1;
                        break;
                    }
                }
            }

            List<string> linelist = new List<string>();
            int lastLineCount = 0;
            string offset = " ";
            linelist = GetLineData(shopnameArray[0], LINECOUNT);
            //Log.Error("GetLineListData:" + Newtonsoft.Json.JsonConvert.SerializeObject(linelist));
            string str = string.Empty, lastLineStr = "";
            for (int i = 0; i < linelist.Count; i++)
            {
                var spcSize2 = g.MeasureString(linelist[i], shopnameFont);
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Near;
                g.DrawString(offset + linelist[i], shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.ShopNameColor)),
                   (int)(posterjs.PosList[2].Left * 480),
                   (int)(posterjs.PosList[2].Top * 735 / 490) + (lineIndex + i + 1) * (int)spcSize2.Height, format);

            }
            lastLineStr = linelist[linelist.Count - 1];
            lastLineCount = GetStringChineseLen(lastLineStr);
            lineIndex = linelist.Count;
            if (shopnameArray.Length > 1)
            {
                var StorenameSize = g.MeasureString(StoreName, shopnameFont);
                var lastLineSize = g.MeasureString(lastLineStr, shopnameFont);
                decimal width = (decimal)lastLineSize.Width;
                bool isNewLine = false;
                int shopnameLen = GetStringChineseLen(StoreName);
                if (LINECOUNT - lastLineCount < shopnameLen)//如果{{商城名称}}之前的文本中最后一行，字数在[10,LINECOUNT]之间，就要另起一行
                {
                    lineIndex++;
                    width = 0;
                    isNewLine = true;
                }
                //替换{{商城名称}}
                g.DrawString(offset + StoreName, shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.StoreNameColor)),
               (int)(posterjs.PosList[2].Left * 480 + width),
                (int)(posterjs.PosList[2].Top * 735 / 490) + lineIndex * (int)StorenameSize.Height,
               StringFormat);

                if (isNewLine)//如果{{商城名称}}新行，则另起一行
                {
                    lineIndex++;//另起一行画之后的部分
                }

                var shopStr = StoreName;
                int index = 0;
                if (width == 0)
                {
                    index = shopnameLen;
                }
                else
                {
                    index = shopnameLen + GetStringChineseLen(lastLineStr);
                    shopStr += lastLineStr;
                }

                var shopStrSize = g.MeasureString(shopStr, shopnameFont);
                //补齐
                if (LINECOUNT - index > 0 && !string.IsNullOrWhiteSpace(shopnameArray[1]))
                {
                    var newStr = shopnameArray[1];
                    if (!newStr.StartsWith("\n"))
                    {
                        if (GetStringChineseLen(newStr) >= LINECOUNT - index)
                        {
                            int newstrlen = LINECOUNT - index;//新字符度的中文长度
                            newStr = SubChineseLenStr(newStr, LINECOUNT - index);

                        }
                        index = newStr.Length;
                        g.DrawString(offset + newStr, shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.ShopNameColor)),
                               (int)(posterjs.PosList[2].Left * 480 + (decimal)shopStrSize.Width),
                               (int)(posterjs.PosList[2].Top * 735 / 490) + lineIndex * (int)shopStrSize.Height, StringFormat);
                    }
                    else { index = 0; shopnameArray[1] = shopnameArray[1].Replace("\n", ""); }
                }

                if (!string.IsNullOrWhiteSpace(shopnameArray[1]))
                {
                    var newshopnameArray = shopnameArray[1];
                    if (GetStringChineseLen(newshopnameArray) >= index)
                    {
                        newshopnameArray = newshopnameArray.Substring(index);
                    }
                    linelist = GetLineData(newshopnameArray, LINECOUNT);

                    for (int i = 0; i < linelist.Count; i++)
                    {
                        var spcSize2 = g.MeasureString(linelist[i], shopnameFont);
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Near;
                        g.DrawString(offset + linelist[i], shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.ShopNameColor)),
                             (int)(posterjs.PosList[2].Left * 480),
                             (int)(posterjs.PosList[2].Top * 735 / 490) + (lineIndex + i + 1) * (int)spcSize2.Height, format);
                    }
                }
            }

            //加入二维码
            g.DrawImage(Qrimage,
                (int)(posterjs.PosList[3].Left * 480),
                (int)(posterjs.PosList[3].Top * 735 / 490),
                (int)(posterjs.PosList[3].Width * 480),
                (int)(posterjs.PosList[3].Width * 480)
                );
            Qrimage.Dispose();

            if (!Directory.Exists(HttpContext.Current.Server.MapPath(@"/Storage/master/ReferralPoster/PosterPic")))
                Directory.CreateDirectory(HttpContext.Current.Server.MapPath(@"/Storage/master/ReferralPoster/PosterPic"));

            var strurl = HttpContext.Current.Server.MapPath(referralPostUrl);
            Cardbmp.Save(strurl, ImageFormat.Jpeg);
            Cardbmp.Dispose();

            //如部署了oss，把本地生成图更新到oss上
            if (Core.HimallIO.GetHimallIO().GetType().FullName.Equals("Himall.Strategy.OSS"))
            {
                Stream stream = File.OpenRead(strurl);
                HimallIO.CreateFile(referralPostUrl, stream, FileCreateType.CreateNew);//更新到oss上
            }

            return HimallIO.GetFilePath(referralPostUrl).Replace("//Storage", "/Storage").Replace("//Storage", "/Storage") + "?rnd=" + (new Random()).Next();
        }

        /// <summary>
        /// 获取小程序我的名片合成海报
        /// </summary>
        /// <returns></returns>
        public static string GetAppletReferralPostUrl(MemberInfo member, out decimal top, out decimal left, out decimal width)
        {
            top = 0;
            left = 0;
            width = 0;
            string referralPostUrl = "/Storage/master/ReferralPoster/PosterPic/Poster_AppletNew_" + member.Id + ".jpg";//海报图片路径
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(member.Id);
            if (currentDistributor == null || !currentDistributor.IsNormalDistributor)
                return string.Empty;

            string posterurl = "/Storage/master/ReferralPoster/ReferralPoster.js";
            if (!Himall.Core.HimallIO.ExistFile(posterurl))
                return string.Empty;//配置文件不存在返回空
            DownloadLocal(posterurl);//海报配置文件本地最新

            string SetJson = System.IO.File.ReadAllText(HttpRuntime.AppDomainAppPath.ToString() + posterurl); //获取配置文件
            if (string.IsNullOrEmpty(SetJson))
                return string.Empty;

            DistributionBusionessCardConfigModel posterjs = JsonConvert.DeserializeObject<DistributionBusionessCardConfigModel>(SetJson);
            if (posterjs == null)
                return string.Empty;
            left = posterjs.PosList[3].Left;
            top = posterjs.PosList[3].Top;
            width = posterjs.PosList[3].Width;

            if (Himall.Core.HimallIO.ExistFile(referralPostUrl))
            {
                var fiposter = HimallIO.GetFileMetaInfo(referralPostUrl);//海报文件基本信息
                var fi = HimallIO.GetFileMetaInfo(posterurl);//海报配置文件

                if (fi.LastModifiedTime < fiposter.LastModifiedTime)
                    return HimallIO.GetFilePath(referralPostUrl).Replace("//Storage", "/Storage").Replace("//Storage", "/Storage") + "?rnd=" + new Random().Next();
            }

            SiteSettings site = SiteSettingApplication.SiteSettings;
            string StoreName = site.SiteName;
            string UserHead = member.Photo;
            string UserName = string.IsNullOrEmpty(member.Nick) ? member.RealName : member.Nick;

            //生成二维码图片
            Bitmap Qrimage = null;
            //默认生成店铺分销二维码，否则启用公众号二维码
            int defaultQRCode = int.Parse(posterjs.DefaultQRCode);
            if (defaultQRCode == 1 && site.IsOpenH5)//公众号二维码
            {
                if (!string.IsNullOrEmpty(site.WeixinAppId) && !string.IsNullOrEmpty(site.WeixinAppSecret))
                {
                    string ticket = GetQRLIMITSTRSCENETicket("referraluserid:" + member.Id);
                    string codeUrl = string.Format("https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket={0}", ticket);
                    if (codeUrl.Contains("weixin.qq.com"))
                    {
                        Qrimage = GetNetImg(codeUrl);
                    }
                    else
                    {
                        Qrimage = (Bitmap)Image.FromFile(HttpContext.Current.Server.MapPath(codeUrl));
                    }
                }
            }
            else if (site.IsOpenMallSmallProg)
            {
                //小程序二维码图片
                string qrCodeImagePath = GetReferalAppletCode(member.Id);
                Qrimage = (Bitmap)Image.FromFile(HttpContext.Current.Server.MapPath(qrCodeImagePath));
            }

            int DefaultHead = int.Parse(posterjs.DefaultHead);
            if (DefaultHead == 1)//店铺logo
            {
                if (currentDistributor != null && !string.IsNullOrEmpty(currentDistributor.ShopLogo))
                {
                    UserHead = currentDistributor.ShopLogo.Contains("http://") || currentDistributor.ShopLogo.Contains("https://") ? currentDistributor.ShopLogo : new Uri(site.SiteUrl) + currentDistributor.ShopLogo;
                }
            }
            if (string.IsNullOrEmpty(UserHead) || (!UserHead.ToLower().StartsWith("http") && !UserHead.ToLower().StartsWith("https") && !File.Exists(HttpContext.Current.Server.MapPath(UserHead))))
                UserHead = "/Utility/pics/imgnopic.jpg";


            if (DefaultHead == 2)
            {
                UserHead = "";
            }


            System.Drawing.Image logoimg;
            if (UserHead.ToLower().StartsWith("http") || UserHead.ToLower().StartsWith("https"))
            {
                logoimg = GetNetImg(UserHead); //获取网络图片 ;// new Bitmap(UserHead);
            }
            else
            {
                if (!string.IsNullOrEmpty(UserHead) && File.Exists(HttpContext.Current.Server.MapPath(UserHead)))
                {
                    logoimg = System.Drawing.Image.FromFile(HttpContext.Current.Server.MapPath(UserHead));
                }
                else
                {
                    logoimg = new Bitmap(100, 100);
                };
            }


            //转换成圆形图片
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(new Rectangle(0, 0, logoimg.Width, logoimg.Width));
            Bitmap Tlogoimg = new Bitmap(logoimg.Width, logoimg.Width);
            using (Graphics gl = Graphics.FromImage(Tlogoimg))
            { //假设bm就是你要绘制的正方形位图，已创建好
                gl.SetClip(gp);
                gl.DrawImage(logoimg, 0, 0, logoimg.Width, logoimg.Width);
            }
            logoimg.Dispose();

            ////合成二维码图像
            //if (defaultQRCode == 0)//只有当店铺二维码时才需要将头像合成到二维码中，公众号二维码不需要
            Qrimage = CombinImage(Qrimage, Tlogoimg, 130); //二维码图片

            Bitmap Cardbmp = new Bitmap(480, 735);

            Graphics g = Graphics.FromImage(Cardbmp);
            g.SmoothingMode = SmoothingMode.HighQuality; ; //抗锯齿
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.High;
            g.Clear(System.Drawing.Color.White); //白色填充

            Bitmap Bgimg = new Bitmap(100, 100);

            if (!string.IsNullOrEmpty(posterjs.BgImg) && HimallIO.ExistFile(posterjs.BgImg))
            {
                //Log.Error("BgImg:" + posterjs.BgImg);
                //如果背景图片存在，
                Bgimg = (Bitmap)System.Drawing.Image.FromFile(HttpContext.Current.Request.MapPath(posterjs.BgImg)); //如果存在，读取背景图片
                Bgimg = GetThumbnail(Bgimg, 735, 480); //处理成对应尺寸图片
            }

            //绘制背景图
            g.DrawImage(Bgimg, 0, 0, 480, 735);

            Font usernameFont = new Font("微软雅黑", (int)(posterjs.MyUserNameSize * 6 / 5));

            Font shopnameFont = new Font("微软雅黑", (int)(posterjs.ShopNameSize * 6 / 5));

            //加入用户头像
            g.DrawImage(Tlogoimg, (int)(posterjs.PosList[0].Left * 480),
                (int)(posterjs.PosList[0].Top * 735 / 490),
                (int)(posterjs.PosList[0].Width * 480),
                (int)(posterjs.PosList[0].Width * 480)
                );

            StringFormat StringFormat = new StringFormat(StringFormatFlags.DisplayFormatControl);

            string myusername = string.IsNullOrEmpty(posterjs.MyUserName) ? string.Empty : posterjs.MyUserName.Replace(@"{{昵称}}", "$");
            string shopname = string.IsNullOrEmpty(posterjs.ShopName) ? string.Empty : posterjs.ShopName.Replace(@"{{商城名称}}", "$");

            string[] myusernameArray = myusername.Split('$');
            string[] shopnameArray = shopname.Split('$');

            //写昵称
            g.DrawString(myusernameArray[0], usernameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.MyUserNameColor)),
                 (int)(posterjs.PosList[1].Left * 480),
                 (int)(posterjs.PosList[1].Top * 735 / 490),
                StringFormat);

            if (myusernameArray.Length > 1)
            {
                var spcSize1 = g.MeasureString(" ", usernameFont);
                var myusernameSize = g.MeasureString(myusernameArray[0], usernameFont);
                g.DrawString(UserName, usernameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.NickNameColor)),
               (int)(posterjs.PosList[1].Left * 480) + myusernameSize.Width - spcSize1.Width,
                (int)(posterjs.PosList[1].Top * 735 / 490),
               StringFormat);

                var usernameSize = g.MeasureString(UserName, usernameFont);
                g.DrawString(myusernameArray[1], usernameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.MyUserNameColor)),
               (int)(posterjs.PosList[1].Left * 480) + myusernameSize.Width - spcSize1.Width * 2 + usernameSize.Width,
                (int)(posterjs.PosList[1].Top * 735 / 490),
               StringFormat);

            }
            //写店铺名
            var lineWidth = 520 - (int)(posterjs.PosList[2].Left * 480);
            int lineIndex = 0;//当前行数，每20个字为一行

            int LINECOUNT = 21;//每行摆多少文字
            int shopNameSize = posterjs.ShopNameSize;
            if (shopNameSize != 14)//如果不是默认14号字体则计算一行显示的字数,根据字体大小计算出一行最多输出的字符个数（中文）
            {
                string teststr = "中";
                for (int i = 0; i < 50; i++)
                {
                    string tempstr = SelfLoop(teststr, i);
                    var spctempSize = g.MeasureString(tempstr, shopnameFont);
                    if (spctempSize.Width >= lineWidth)
                    {
                        LINECOUNT = i - 1;
                        break;
                    }
                }
            }

            List<string> linelist = new List<string>();
            int lastLineCount = 0;
            string offset = " ";
            linelist = GetLineData(shopnameArray[0], LINECOUNT);
            //Log.Error("GetLineListData:" + Newtonsoft.Json.JsonConvert.SerializeObject(linelist));
            string str = string.Empty, lastLineStr = "";
            for (int i = 0; i < linelist.Count; i++)
            {
                var spcSize2 = g.MeasureString(linelist[i], shopnameFont);
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Near;
                g.DrawString(offset + linelist[i], shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.ShopNameColor)),
                   (int)(posterjs.PosList[2].Left * 480),
                   (int)(posterjs.PosList[2].Top * 735 / 490) + (lineIndex + i + 1) * (int)spcSize2.Height, format);

            }
            lastLineStr = linelist[linelist.Count - 1];
            lastLineCount = GetStringChineseLen(lastLineStr);
            lineIndex = linelist.Count;
            if (shopnameArray.Length > 1)
            {
                var StorenameSize = g.MeasureString(StoreName, shopnameFont);
                var lastLineSize = g.MeasureString(lastLineStr, shopnameFont);
                decimal widthshop = (decimal)lastLineSize.Width;
                bool isNewLine = false;
                int shopnameLen = GetStringChineseLen(StoreName);
                if (LINECOUNT - lastLineCount < shopnameLen)//如果{{商城名称}}之前的文本中最后一行，字数在[10,LINECOUNT]之间，就要另起一行
                {
                    lineIndex++;
                    widthshop = 0;
                    isNewLine = true;
                }
                //替换{{商城名称}}
                g.DrawString(offset + StoreName, shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.StoreNameColor)),
               (int)(posterjs.PosList[2].Left * 480 + widthshop),
                (int)posterjs.PosList[2].Top * 735 / 490 + lineIndex * (int)StorenameSize.Height,
               StringFormat);

                if (isNewLine)//如果{{商城名称}}新行，则另起一行
                {
                    lineIndex++;//另起一行画之后的部分
                }

                var shopStr = StoreName;
                int index = 0;
                if (widthshop == 0)
                {
                    index = shopnameLen;
                }
                else
                {
                    index = shopnameLen + GetStringChineseLen(lastLineStr);
                    shopStr += lastLineStr;
                }

                var shopStrSize = g.MeasureString(shopStr, shopnameFont);
                //补齐
                if (LINECOUNT - index > 0 && !string.IsNullOrWhiteSpace(shopnameArray[1]))
                {
                    var newStr = shopnameArray[1];
                    if (!newStr.StartsWith("\n"))
                    {
                        if (GetStringChineseLen(newStr) >= LINECOUNT - index)
                        {
                            int newstrlen = LINECOUNT - index;//新字符度的中文长度
                            newStr = SubChineseLenStr(newStr, LINECOUNT - index);

                        }
                        index = newStr.Length;
                        g.DrawString(offset + newStr, shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.ShopNameColor)),
                               (int)(posterjs.PosList[2].Left * 480 + (decimal)shopStrSize.Width),
                               (int)(posterjs.PosList[2].Top * 735 / 490) + lineIndex * (int)shopStrSize.Height, StringFormat);
                    }
                    else { index = 0; shopnameArray[1] = shopnameArray[1].Replace("\n", ""); }
                }

                if (!string.IsNullOrWhiteSpace(shopnameArray[1]))
                {
                    var newshopnameArray = shopnameArray[1];
                    if (GetStringChineseLen(newshopnameArray) >= index)
                    {
                        newshopnameArray = newshopnameArray.Substring(index);
                    }
                    linelist = GetLineData(newshopnameArray, LINECOUNT);

                    for (int i = 0; i < linelist.Count; i++)
                    {
                        var spcSize2 = g.MeasureString(linelist[i], shopnameFont);
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Near;
                        g.DrawString(offset + linelist[i], shopnameFont, new SolidBrush(System.Drawing.ColorTranslator.FromHtml(posterjs.ShopNameColor)),
                             (int)(posterjs.PosList[2].Left * 480),
                             (int)(posterjs.PosList[2].Top * 735 / 490) + (lineIndex + i + 1) * (int)spcSize2.Height, format);
                    }
                }
            }
            left = (decimal)posterjs.PosList[3].Left;
            top = (decimal)posterjs.PosList[3].Top;
            width = (decimal)posterjs.PosList[3].Width;
            //加入二维码
            g.DrawImage(Qrimage,
                (int)(((decimal)posterjs.PosList[3].Left) * 480),
                ((int)posterjs.PosList[3].Top * 735 / 490),
                (int)(((decimal)posterjs.PosList[3].Width) * 480),
                (int)(((decimal)posterjs.PosList[3].Width) * 480)
                );
            Qrimage.Dispose();

            if (!Directory.Exists(HttpContext.Current.Server.MapPath(@"/Storage/master/ReferralPoster/PosterPic")))
                Directory.CreateDirectory(HttpContext.Current.Server.MapPath(@"/Storage/master/ReferralPoster/PosterPic"));

            var strurl = HttpContext.Current.Server.MapPath(referralPostUrl);
            Cardbmp.Save(strurl, ImageFormat.Jpeg);

            Cardbmp.Dispose();

            //如部署了oss，把本地生成图更新到oss上
            if (Core.HimallIO.GetHimallIO().GetType().FullName.Equals("Himall.Strategy.OSS"))
            {
                Stream stream = File.OpenRead(strurl);
                HimallIO.CreateFile(referralPostUrl, stream, FileCreateType.CreateNew);//更新到oss上
            }

            return HimallIO.GetFilePath(referralPostUrl).Replace("//Storage", "/Storage").Replace("//Storage", "/Storage") + "?rnd=" + (new Random()).Next();
        }


        /// <summary>
        /// 判断文件本地是否最新，下载到本地来
        /// </summary>
        /// <param name="datapath">路径</param>
        public static void DownloadLocal(string datapath)
        {
            MetaInfo metaRemoteInfo = null;
            if (Core.HimallIO.IsNeedRefreshFile(datapath, out metaRemoteInfo))
            {
                var metaLocalFile = GetFileMetaInfo(datapath);
                if (CheckMetaInfo(metaRemoteInfo, metaLocalFile))
                {
                    var dataFileBytes = Core.HimallIO.DownloadTemplateFile(datapath);
                    if (null != dataFileBytes)
                    {
                        var strDataContent = Encoding.UTF8.GetString(dataFileBytes);
                        string abDataPath = HttpContext.Current.Server.MapPath(datapath);
                        using (StreamWriter sw = new StreamWriter(abDataPath, false, Encoding.UTF8))
                        {
                            foreach (var s in strDataContent)
                            {
                                sw.Write(s);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查文件信息remote是否最新
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        private static bool CheckMetaInfo(MetaInfo remote, MetaInfo local)
        {
            if (null == remote) return false;
            return null != local ? remote.LastModifiedTime > local.LastModifiedTime : true;
        }

        /// <summary>
        /// 获取文件基本信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static MetaInfo GetFileMetaInfo(string fileName)
        {
            MetaInfo minfo = new MetaInfo();
            var file = HttpContext.Current.Server.MapPath(fileName);
            FileInfo finfo = new FileInfo(file);
            if (finfo.Exists)
            {
                minfo.ContentLength = finfo.Length;
                var contentType = MimeMapping.GetMimeMapping(file);
                minfo.ContentType = contentType;
                minfo.LastModifiedTime = finfo.LastWriteTime;
                // minfo.ObjectType
                return minfo;
            }
            return null;
        }

        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) { return true; }

        public static string GetResponseResult(string url, string param)
        {
            string result = string.Empty;

            string strURL = url;
            try
            {
                System.Net.HttpWebRequest request;
                request = (HttpWebRequest)WebRequest.Create(strURL);
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                string paraUrlCoded = param;
                byte[] payload;
                payload = Encoding.UTF8.GetBytes(paraUrlCoded);
                request.ContentLength = payload.Length;
                Stream writer = request.GetRequestStream();
                writer.Write(payload, 0, payload.Length);
                writer.Close();
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream receiveStream = response.GetResponseStream())
                    {
                        using (StreamReader readerOfStream = new StreamReader(receiveStream, System.Text.Encoding.UTF8))
                        {
                            result = readerOfStream.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Himall.Core.Log.Error("ResponseUrl:" + url + "--ResponseParam" + param);
            }
            return result;
        }


        /// <summary>
        /// 分销时关注微信公众号二维码
        /// </summary>
        /// <param name="scene_str"></param>
        /// <param name="first"></param>
        /// <returns></returns>
        public static string GetQRLIMITSTRSCENETicket(string scene_str, bool first = true)
        {
            SiteSettings setting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrEmpty(setting.WeixinAppId) || string.IsNullOrEmpty(setting.WeixinAppSecret))
            {
                return "";
            }

            string accesstoken = string.Empty;
            try
            {
                accesstoken = WXApiApplication.TryGetToken(setting.WeixinAppId, setting.WeixinAppSecret);
                if (string.IsNullOrEmpty(accesstoken))
                {
                    accesstoken = WXApiApplication.TryGetToken(setting.WeixinAppId, setting.WeixinAppSecret, true);//如果access_token无效则重新获取                                                                                                                   //SetAccessToken_Cache(accesstoken);
                }
            }
            catch (Exception ex)
            {
                accesstoken = WXApiApplication.TryGetToken(setting.WeixinAppId, setting.WeixinAppSecret, true);//如果access_token无效则重新获取
            }

            string param = "{\"action_name\": \"QR_LIMIT_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"" + scene_str + "\"}}}";
            string ticketResponse = GetResponseResult(string.Format("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token={0}", accesstoken), param);
            string ticket = string.Empty;

            if (ticketResponse.IndexOf("ticket") != -1)
            {
                var resultObj = JsonConvert.DeserializeObject(ticketResponse) as JObject;
                ticket = resultObj["ticket"].ToString();
            }
            else
            {
                //如果access_token无效则重新获取
                //HttpContext.Current.Request.AppendLog(ticketResponse, accesstoken, "", "GetQRLIMITSTRSCENETicket");
                if (ticketResponse.Contains("access_token is invalid or not latest") && first)
                {
                    accesstoken = WXApiApplication.TryGetToken(setting.WeixinAppId, setting.WeixinAppSecret, true);
                    //SetAccessToken_Cache(accesstoken);
                    return GetQRLIMITSTRSCENETicket(scene_str, false);
                }
            }
            return ticket;
        }

        #region 生成下载的小程序二维码
        /// <summary>
        /// 生成小程序分销二维码扫描地址
        /// </summary>
        /// <param name="referralUserId"></param>
        /// <returns></returns>
        public static string GetReferalAppletCode(long referralUserId)
        {
            var fileName = string.Format(@"/Storage/master/ReferralPoster/QRCode/AppletReferral_{0}.png", referralUserId);
            if (HimallIO.ExistFile(fileName)) return fileName;
            FileInfo fiposter = new FileInfo(HttpRuntime.AppDomainAppPath.ToString() + fileName);//获取二维码文件
            if (fiposter.Exists) return fileName;

            SiteSettings site = SiteSettingApplication.SiteSettings;
            string accessToken = WXApiApplication.TryGetToken(site.WeixinAppletId, site.WeixinAppletSecret, true);//如果access_token无效则重新获取

            string reurl = "https://api.weixin.qq.com/wxa/getwxacode?access_token=" + accessToken;
            var data = "{\"path\":\"pages/home/home?distributorId=" + referralUserId + "\",\"width\":300}";

            HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(reurl);  //创建url
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] load = Encoding.UTF8.GetBytes(data);
            request.ContentLength = load.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(load, 0, load.Length);
            HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            Stream s = response.GetResponseStream();
            byte[] mg = StreamToBytes(s);
            MemoryStream ms = new MemoryStream(mg);
            HimallIO.DeleteFile(fileName);//新删除已存在的小程序二维码图，后面再生成最新的小程序二维码图
            HimallIO.CreateFile(fileName, ms);//生成小程序二维码图

            ms.Dispose();
            ms.Close();
            return fileName;
        }
        #endregion

        /// <summary>
        /// 流转换为字节
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] StreamToBytes(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            int temp = stream.ReadByte();
            while (temp != -1)
            {
                bytes.Add((byte)temp);
                temp = stream.ReadByte();
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// 获取网络图片
        /// </summary>
        /// <param name="imgUrl"></param>
        /// <returns></returns>
        public static Bitmap GetNetImg(string imgUrl)
        {
            try
            {
                Random seed = new Random();

                if (imgUrl.Contains("?"))
                {
                    imgUrl = imgUrl + "&aid=" + seed.NextDouble();
                }
                else
                {
                    imgUrl = imgUrl + "?aid=" + seed.NextDouble();
                }
                System.Net.WebRequest webreq = System.Net.WebRequest.Create(imgUrl);
                System.Net.WebResponse webres = webreq.GetResponse();
                System.IO.Stream stream = webres.GetResponseStream();
                System.Drawing.Image image = System.Drawing.Image.FromStream(stream);
                stream.Close();
                stream.Dispose();
                webreq = null;
                webres = null;
                // image.Save(@"D:\tt.jpg");
                return (Bitmap)image;
            }
            catch (Exception ex)
            {
                IDictionary<string, string> param = new Dictionary<string, string>();
                param.Add("NetImgUrl", imgUrl);
                //HttpContext.Current.Request.WriteExceptionLog(ex, param, "SplittinRuleGetNetImg");
                Log.Error("NetImgUrl:" + imgUrl + "--error:" + ex.Message);
                return new Bitmap(100, 100); //返回空图象
            }
        }

        /// <summary>
        /// 合成二维码图片
        /// </summary>
        /// <param name="imgBack"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap CombinImage(Bitmap QRimg, System.Drawing.Image Logoimg, int logoW)
        {
            Bitmap tbmp = new Bitmap(QRimg.Width + 20, QRimg.Height + 20);
            Graphics g = Graphics.FromImage(tbmp);
            g.Clear(System.Drawing.Color.White);
            g.DrawImage(QRimg, 10, 10, QRimg.Width, QRimg.Height);
            g.DrawImage(Logoimg, (tbmp.Width - logoW) / 2, (tbmp.Height - logoW) / 2, logoW, logoW);
            return tbmp;
        }

        /// <summary>
        /// 根据图片流生成指定大小的图片
        /// </summary>
        /// <param name="b"></param>
        /// <param name="destHeight"></param>
        /// <param name="destWidth"></param>
        /// <returns></returns>
        public static Bitmap GetThumbnail(Bitmap b, int destHeight, int destWidth)
        {
            System.Drawing.Image imgSource = b;
            System.Drawing.Imaging.ImageFormat thisFormat = imgSource.RawFormat;
            int sW = 0, sH = 0;

            // 按比例缩放           
            int sWidth = imgSource.Width;
            int sHeight = imgSource.Height;

            if (sHeight > destHeight || sWidth > destWidth)
            {

                if ((sWidth * destHeight) < (sHeight * destWidth))
                {
                    sW = destWidth;
                    sH = (destWidth * sHeight) / sWidth;
                }
                else
                {
                    sH = destHeight;
                    sW = (sWidth * destHeight) / sHeight;
                }

            }
            else
            {
                sW = destWidth; //sWidth;
                sH = destHeight;// sHeight;   
            }

            Bitmap outBmp = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage(outBmp);

            g.Clear(System.Drawing.Color.Transparent);

            // 设置画布的描绘质量         

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imgSource, new Rectangle((destWidth - sW) / 2, (destHeight - sH) / 2, sW, sH), 0, 0, imgSource.Width, imgSource.Height, GraphicsUnit.Pixel);
            g.Dispose();

            // 以下代码为保存图片时，设置压缩质量     

            EncoderParameters encoderParams = new EncoderParameters();
            long[] quality = new long[1];
            quality[0] = 100;
            EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            encoderParams.Param[0] = encoderParam;
            imgSource.Dispose();
            return outBmp;
        }

        /// <summary>
        /// 循环本身叠加，可以指定次数
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string SelfLoop(object obj, int len)
        {
            if (obj == null || len <= 0)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(obj.ToString());
            if (len > 0)
            {
                for (int i = 0; i < len; i++)
                {
                    sb.Append(obj.ToString());
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 判断是否是中文
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsZHCN(string data)
        {
            Regex RegChinese = new Regex(@"^[\u4e00-\u9fa5]+$", RegexOptions.IgnoreCase);
            return RegChinese.IsMatch(data);
        }

        public static List<string> GetLineData(string data, int chineseLineLen)
        {
            List<string> linelist = new List<string>();
            decimal strcount = 0;
            int startIndex = 0;
            for (int i = 0; i < data.Length; i++)
            {
                var tempstr = data.Substring(i, 1);
                if (IsZHCN(tempstr) || Encoding.Default.GetByteCount(tempstr) == 2)
                {
                    strcount += 1;
                }
                else
                {
                    strcount += 0.5M;
                }
                if (tempstr == "\n")
                {
                    //如果有换行符
                    if (i == 0)
                    {
                        linelist.Add("");
                    }
                    else
                    {
                        linelist.Add(data.Substring(startIndex, (i - startIndex)));
                    }
                    strcount = 0;
                    startIndex = i + 1;
                    continue;
                }
                if (strcount >= chineseLineLen)
                {
                    strcount = 0;
                    linelist.Add(data.Substring(startIndex, (i - startIndex)));
                    startIndex = i;
                }

            }

            if (linelist.Count == 0)
            {
                linelist.Add(data);
            }
            else if (startIndex < data.Length)
            {
                linelist.Add(data.Substring(startIndex));
            }
            if (data.Length > 0 && data.Substring(data.Length - 1) == "\n")
            {
                linelist.Add("");
            }
            return linelist;
        }

        /// <summary>
        /// 获取字符串中文长度，如果包含英文或者符号则以0.5个中文长度计算
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetStringChineseLen(string str)
        {
            decimal strcount = 0;
            for (int i = 0; i < str.Length; i++)
            {
                var tempstr = str.Substring(i, 1);
                if (IsZHCN(tempstr) || Encoding.Default.GetByteCount(tempstr) == 2)
                {
                    strcount += 1;
                }
                else
                {
                    strcount += 0.5M;
                }
            }
            return (int)strcount < strcount ? (int)strcount + 1 : (int)strcount;
        }

        /// <summary>
        /// 获取指定字符串，中文长度的实际字符长度
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetChineseLenStr(string str, int chineseLen, int startIndex = 0)
        {
            decimal strcount = 0;
            for (int i = startIndex; i < str.Length; i++)
            {
                var tempstr = str.Substring(i, 1);
                if (IsZHCN(tempstr) || Encoding.Default.GetByteCount(tempstr) == 2)
                {
                    strcount += 1;
                }
                else
                {
                    strcount += 0.5M;
                }
                if (strcount >= chineseLen)
                {
                    return i + 1;//实际长度
                }
            }
            return str.Length;
        }

        /// <summary>
        /// 截取字符串指定中文长度的字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="len"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static string SubChineseLenStr(string str, int len, int startIndex = 0)
        {

            decimal strcount = 0;
            for (int i = startIndex; i < str.Length; i++)
            {
                var tempstr = str.Substring(i, 1);
                if (IsZHCN(tempstr) || Encoding.Default.GetByteCount(tempstr) == 2)
                {
                    strcount += 1;
                }
                else
                {
                    strcount += 0.5M;
                }
                if (strcount >= len)
                {
                    return str.Substring(startIndex, i);
                }
            }
            return str;
        }
        #endregion

        /// <summary>
        /// 获取合成Logo的生成二维码Base64位图片(返回data:image/png;base64)
        /// </summary>
        /// <param name="url">图片内容</param>
        /// <returns></returns>
        public static string GetBusinessQRCodeBase64Url(string url)
        {
            //创建二维码生成类  
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            //设置编码模式  
            qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
            //设置编码测量度  
            qrCodeEncoder.QRCodeScale = 10;
            //设置编码版本  
            qrCodeEncoder.QRCodeVersion = 0;
            //设置编码错误纠正  
            qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;
            System.Drawing.Bitmap bitmap = qrCodeEncoder.Encode(SiteSettingApplication.GetUrlHttpOrHttps(url));

            #region 把logo合成中间到图
            System.Drawing.Image logoimg;
            string strlogo = SiteSettingApplication.SiteSettings.WXLogo;
            if (!string.IsNullOrEmpty(strlogo) && Himall.Core.HimallIO.ExistFile(strlogo))
            {
                strlogo = Himall.Core.HimallIO.GetImagePath(strlogo);
                if (strlogo.ToLower().StartsWith("http") || strlogo.ToLower().StartsWith("https"))
                {
                    logoimg = GetNetImg(strlogo); //获取网络图片 ;// new Bitmap(UserHead);
                }
                else
                {
                    logoimg = System.Drawing.Image.FromFile(HttpContext.Current.Request.MapPath(strlogo));
                }
            }
            else
            {
                string midpic = "/images/logo.png";//默认logo
                logoimg = System.Drawing.Image.FromFile(HttpContext.Current.Request.MapPath(midpic));
            }
            //转换成圆形图片
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(new Rectangle(0, 0, logoimg.Width, logoimg.Width));
            Bitmap Tlogoimg = new Bitmap(logoimg.Width, logoimg.Width);
            using (Graphics gl = Graphics.FromImage(Tlogoimg))
            { //假设bm就是你要绘制的正方形位图，已创建好
                gl.SetClip(gp);
                gl.DrawImage(logoimg, 0, 0, logoimg.Width, logoimg.Width);
            }
            logoimg.Dispose();
            bitmap = CombinImage(bitmap, Tlogoimg, 80); //二维码图片
            #endregion

            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            string qrCodeImagePath = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray()); // 将图片内存流转成base64,图片以DataURI形式显示  
            bitmap.Dispose();
            return qrCodeImagePath;
        }

        public static void Settlement()
        {
            var timeout = SiteSettingApplication.SiteSettings.SalesReturnTimeout;
            var expireTime = DateTime.Now.Date.AddDays(-timeout);
            var result = Service.Settlement(expireTime);
            foreach (var item in result)
            {
                MessageApplication.SendMessageOnDistributorCommissionSettled(item.MemberId, item.Brokerage, item.SettledDate);
            }
        }
    }
}
