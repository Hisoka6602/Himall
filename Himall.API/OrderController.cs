using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace Himall.API
{
    public class OrderController : BaseApiController
    {
        public dynamic Prepro(OrderPreproCommand command)
        {
            try
            {
                command.MemberId = CurrentUserId;
                command.PlatformType = PlatformType.Android;
                var result = OrderProcessApplication.Prepro(command);
                return new { 
                    success=true,
                    data = result
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    msg = ex.Message
                };
            }
        }

        public dynamic Submit(OrderCreateCommand command)
        {
            try
            {
                command.MemberId = CurrentUserId;
                command.PlatformType = PlatformType.Android;
                var result = OrderProcessApplication.Submit(command);
                return new
                {
                    success = true,
                    data = result
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    msg = ex.Message
                };
            }
        }

        public dynamic GetInvoiceTitle()
        {
            var titles = ServiceProvider.Instance<OrderService>.Create.GetInvoiceTitles(CurrentUserId);
            foreach (var item in titles)
            {
                if (item.RegionID > 0)
                    item.RegionFullName = ServiceProvider.Instance<RegionService>.Create.GetFullName(item.RegionID);
            }
            return new { success = true, data = titles };
        }

        public object GetOrderShareProduct(string orderids)
        {
            CheckUserLogin();
            if (string.IsNullOrWhiteSpace(orderids))
            {
                throw new HimallException("订单号不能为空！");
            }
            long orderId = 0;
            var ids = orderids.Split(',').Select(e =>
            {
                if (long.TryParse(e, out orderId))
                {
                    return orderId;
                }
                else
                {
                    return 0;
                }
            }
            );
            var orders = OrderApplication.GetOrderDetailViews(ids);
            return new { success = true, OrderDetail = orders };
        }
        public object PostOrderShareAddIntegral(OrderShareAddIntegralModel OrderIds)
        {
            CheckUserLogin();
            var orderids = OrderIds.orderids;
            if (string.IsNullOrWhiteSpace(orderids))
            {
                throw new HimallException("订单号不能为空！");
            }
            long orderId = 0;
            var ids = orderids.Split(',').Select(e =>
            {
                if (long.TryParse(e, out orderId))
                    return orderId;
                else
                    throw new HimallException("订单分享增加积分时，订单号异常！");
            }
            );
            if (MemberIntegralApplication.OrderIsShared(ids))
            {
                throw new HimallException("订单已经分享过！");
            }
            Himall.Entities.MemberIntegralRecordInfo record = new Himall.Entities.MemberIntegralRecordInfo();
            record.MemberId = CurrentUser.Id;
            record.UserName = CurrentUser.UserName;
            record.RecordDate = DateTime.Now;
            record.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Share;
            record.ReMark = string.Format("订单号:{0}", orderids);
            List<Himall.Entities.MemberIntegralRecordActionInfo> recordAction = new List<Himall.Entities.MemberIntegralRecordActionInfo>();

            foreach (var id in ids)
            {
                recordAction.Add(new Himall.Entities.MemberIntegralRecordActionInfo
                {
                    VirtualItemId = id,
                    VirtualItemTypeId = Himall.Entities.MemberIntegralInfo.VirtualItemType.ShareOrder
                });
            }
            record.MemberIntegralRecordActionInfo = recordAction;
            MemberIntegralApplication.AddMemberIntegralByEnum(record, Himall.Entities.MemberIntegralInfo.IntegralType.Share);
            return new { success = true, msg = "晒单添加积分成功！" };
        }

        /// <summary>
        /// 获取自提门店点
        /// </summary>
        /// <returns></returns>
        public object GetShopBranchs(long shopId, bool getParent, string skuIds, string counts, int page, int rows, long shippingAddressId, long regionId)
        {
            string[] _skuIds = skuIds.Split(',');
            int[] _counts = counts.Split(',').Select(p => Himall.Core.Helper.TypeHelper.ObjectToInt(p)).ToArray();

            var shippingAddressInfo = ShippingAddressApplication.GetUserShippingAddress(shippingAddressId);
            int streetId = 0, districtId = 0;//收货地址的街道、区域

            var query = new ShopBranchQuery()
            {
                ShopId = shopId,
                PageNo = page,
                PageSize = rows,
                Status = ShopBranchStatus.Normal,
                ShopBranchProductStatus = ShopBranchSkuStatus.Normal,
                IsAboveSelf = true    //自提点，只取自提门店
            };
            if (shippingAddressInfo != null)
            {
                query.FromLatLng = string.Format("{0},{1}", shippingAddressInfo.Latitude, shippingAddressInfo.Longitude);//需要收货地址的经纬度
                streetId = shippingAddressInfo.RegionId;
                var parentAreaInfo = RegionApplication.GetRegion(shippingAddressInfo.RegionId, Region.RegionLevel.Town);//判断当前区域是否为第四级
                if (parentAreaInfo != null && parentAreaInfo.ParentId > 0) districtId = parentAreaInfo.ParentId;
                else { districtId = streetId; streetId = 0; }
            }
            bool hasLatLng = false;
            if (!string.IsNullOrWhiteSpace(query.FromLatLng)) hasLatLng = query.FromLatLng.Split(',').Length == 2;

            var region = RegionApplication.GetRegion(regionId, getParent ? CommonModel.Region.RegionLevel.City : CommonModel.Region.RegionLevel.County);//同城内门店
            if (region != null) query.AddressPath = region.GetIdPath();

            #region 3.0版本排序规则
            var skuInfos = ProductManagerApplication.GetSKUs(_skuIds);
            query.ProductIds = skuInfos.Select(p => p.ProductId).ToArray();
            var data = ShopBranchApplication.GetShopBranchsAll(query);
            var shopBranchSkus = ShopBranchApplication.GetSkus(shopId, data.Models.Select(p => p.Id).ToList());//获取该商家下具有订单内所有商品的门店状态正常数据,不考虑库存
            data.Models.ForEach(p =>
            {
                p.Enabled = skuInfos.All(skuInfo => shopBranchSkus.Any(sbSku => sbSku.ShopBranchId == p.Id && sbSku.Stock >= _counts[skuInfos.IndexOf(skuInfo)] && sbSku.SkuId == skuInfo.Id));
            });

            List<Himall.DTO.ShopBranch> newList = new List<Himall.DTO.ShopBranch>();
            List<long> fillterIds = new List<long>();
            var currentList = data.Models.Where(p => hasLatLng && p.Enabled && (p.Latitude > 0 && p.Longitude > 0)).OrderBy(p => p.Distance).ToList();
            if (currentList != null && currentList.Count() > 0)
            {
                fillterIds.AddRange(currentList.Select(p => p.Id));
                newList.AddRange(currentList);
            }
            var currentList2 = data.Models.Where(p => !fillterIds.Contains(p.Id) && p.Enabled && p.AddressPath.Contains(CommonConst.ADDRESS_PATH_SPLIT + streetId + CommonConst.ADDRESS_PATH_SPLIT)).ToList();
            if (currentList2 != null && currentList2.Count() > 0)
            {
                fillterIds.AddRange(currentList2.Select(p => p.Id));
                newList.AddRange(currentList2);
            }
            var currentList3 = data.Models.Where(p => !fillterIds.Contains(p.Id) && p.Enabled && p.AddressPath.Contains(CommonConst.ADDRESS_PATH_SPLIT + districtId + CommonConst.ADDRESS_PATH_SPLIT)).ToList();
            if (currentList3 != null && currentList3.Count() > 0)
            {
                fillterIds.AddRange(currentList3.Select(p => p.Id));
                newList.AddRange(currentList3);
            }
            var currentList4 = data.Models.Where(p => !fillterIds.Contains(p.Id) && p.Enabled).ToList();//非同街、非同区，但一定会同市
            if (currentList4 != null && currentList4.Count() > 0)
            {
                fillterIds.AddRange(currentList4.Select(p => p.Id));
                newList.AddRange(currentList4);
            }
            var currentList5 = data.Models.Where(p => !fillterIds.Contains(p.Id)).ToList();//库存不足的排最后
            if (currentList5 != null && currentList5.Count() > 0)
            {
                newList.AddRange(currentList5);
            }
            if (newList.Count() != data.Models.Count())//如果新组合的数据与原数据数量不一致
            {
                return new
                {
                    success = false
                };
            }
            var needDistance = false;
            if (shippingAddressInfo != null && shippingAddressInfo.Latitude != 0 && shippingAddressInfo.Longitude != 0)
            {
                needDistance = true;
            }
            var storeList = newList.Select(sb =>
            {
                return new
                {
                    ContactUser = sb.ContactUser,
                    ContactPhone = sb.ContactPhone,
                    AddressDetail = sb.AddressDetail,
                    ShopBranchName = sb.ShopBranchName,
                    Id = sb.Id,
                    Enabled = sb.Enabled,
                    Distance = needDistance ? RegionApplication.GetDistance(sb.Latitude, sb.Longitude, shippingAddressInfo.Latitude, shippingAddressInfo.Longitude) : 0
                };
            });

            #endregion

            var result = new
            {
                success = true,
                StoreList = storeList
            };
            return result;
        }
     
        /// <summary>
        /// 删除发票抬头
        /// </summary>
        /// <param name="id">抬头ID</param>
        /// <returns>是否完成</returns>
        public object PostDeleteInvoiceTitle(PostDeleteInvoiceTitlePModel para)
        {
            CheckUserLogin();
            OrderApplication.DeleteInvoiceTitle(para.id, CurrentUserId);
            return new { success = true };
        }
        /// <summary>
        /// 设置发票抬头
        /// </summary>
        /// <param name="name">抬头名称</param>
        /// <returns>返回抬头ID</returns>
        [HttpPost]
        public object PostSaveInvoiceTitle(PostSaveInvoiceTitlePModel para)
        {
            CheckUserLogin();
            return OrderApplication.SaveInvoiceTitle(CurrentUserId, para.name, para.code);
        }

        /// <summary>
        /// 保存发票信息(新)
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public object PostSaveInvoiceTitleNew(InvoiceTitleInfo para)
        {
            CheckUserLogin();
            para.UserId = CurrentUserId;
            OrderApplication.SaveInvoiceTitleNew(para);
            return new { success = true, msg = "保存成功" };
        }
    }
}