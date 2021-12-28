using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Extends;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.Live;
using Himall.DTO.QueryModel;
using Himall.Entities;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using static Himall.Entities.ProductInfo;

namespace Himall.Service
{
    public class LiveService : ServiceBase
    {
        public void SaveRoom(LiveRoomInfo room, List<LiveProductInfo> products)
        {
            var model = DbFactory.Default.Get<LiveRoomInfo>(p => p.RoomId == room.RoomId).FirstOrDefault();
          
            if (model == null)
            {
           
                DbFactory.Default.Add(room);
                CacheManager.ClearRoomLiveing();
            }
            else
            {
                model.Name = room.Name;
                model.AnchorImg = room.AnchorImg;
                model.AnchorName = room.AnchorName;
                //model.CoverImg = room.CoverImg; 忽略封面更新
                model.EndTime = room.EndTime;
                model.Status = room.Status;
                if (model.Status != room.Status)
                    CacheManager.ClearRoomLiveing();
                model.StartTime = room.StartTime;
                DbFactory.Default.Update(model);
            }
            SaveProduct(room.RoomId, products);
            
        }

        public void SaveProduct(long roomId, List<LiveProductInfo> products)
        {
            var models = DbFactory.Default.Get<LiveProductInfo>(p => p.RoomId == roomId).ToList();
            foreach (var product in products)
            {
                var model = models.FirstOrDefault(p => p.ProductId == product.ProductId);
                if (model == null)
                {
                    product.RoomId = roomId;
                    DbFactory.Default.Add(product);
                }
                else
                {
                    model.Name = product.Name;
                    model.Url = product.Url;
                    model.Image = product.Image;
                    model.Price = product.Price;
                    DbFactory.Default.Update(model);
                }
            }
            CacheManager.ClearRoomLiveing();
        }
        /// <summary>
        /// 获取最大的排序值
        /// </summary>
        /// <returns></returns>
        public long GetMaxSequence()
        {
            return DbFactory.Default.Get<LiveRoomInfo>().Max<long>(r => r.Sequence);

        }
        public void SetSequence(long roomId, long sequence)
        {
            DbFactory.Default.Set<LiveRoomInfo>()
              .Where(p => p.RoomId == roomId)
              .Set(p => p.Sequence, sequence)
              .Execute();
        }
        /// <summary>
        /// 删除直播间数据，同时删除商品以及回放信息
        /// </summary>
        /// <param name="roomId"></param>
        public void DelLiveRoom(long roomId, long id)
        {
            if (roomId <= 0 && id <= 0)
            {
                return;
            }
            if (roomId > 0)
            {
                DbFactory.Default.Del<LiveProductInfo>().Where(p => p.RoomId == roomId).Execute();
                DbFactory.Default.Del<LiveReplyInfo>().Where(p => p.RoomId == roomId).Execute();
                DbFactory.Default.Del<LiveRoomInfo>().Where(p => p.RoomId == roomId).Execute();
            }
            else
            {
                DbFactory.Default.Del<LiveRoomInfo>().Where(p => p.Id == id).Execute();
            }
            CacheManager.ClearRoomLiveing();
        }
        /// <summary>
        /// 删除直播间回放数据
        /// </summary>
        /// <param name="roomId"></param>
        public void DelLiveReplay(long roomId)
        {
            DbFactory.Default.Del<LiveReplyInfo>().Where(p => p.RoomId == roomId).Execute();
        }

        public void SetShop(long roomId, long shopId)
        {
            DbFactory.Default.Set<LiveRoomInfo>()
                .Where(p => p.RoomId == roomId)
                .Set(p => p.ShopId, shopId)
                .Execute();
        }

        /// <summary>
        /// 查询直播列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<LiveViewModel> GetLiveList(LiveQuery query)
        {
            var db = DbFactory.Default.Get<LiveRoomInfo>();
            if (query.ShopId.HasValue)
            {
                db.Where(e => e.ShopId == query.ShopId.Value);
            }
            if (!string.IsNullOrEmpty(query.ShopName))
            {
                db.LeftJoin<ShopInfo>((r, s) => r.ShopId == s.Id).Where<ShopInfo>(e => e.ShopName.Contains(query.ShopName));
            }
            if (!string.IsNullOrEmpty(query.Name))
            {
                db.Where(e => e.Name.Contains(query.Name));
            }
            if (!string.IsNullOrEmpty(query.AnchorName))
            {
                db.Where(e => e.AnchorName.Contains(query.AnchorName));
            }
            if (query.Status.HasValue)
            {
                db.Where(e => e.Status == query.Status.Value);
            }
            if (query.StartTime.HasValue)
            {
                db.Where(e => e.StartTime >= query.StartTime.Value);
            }
            if (query.EndTime.HasValue)
            {
                var endTime = query.EndTime.Value.AddDays(1);
                db.Where(e => e.StartTime < endTime);
            }
            if (query.ids != null && query.ids.Count > 0)
            {
                db.Where(e => e.Id.ExIn(query.ids));
            }
            if (query.StatusList != null && query.StatusList.Count > 0)
            {
                db.Where(e => e.Status.ExIn(query.StatusList));
            }
            switch (query.Sort.ToLower())
            {
                case "sequence":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.Sequence);
                        else db.OrderByDescending(p => p.Sequence);
                        break;
                    }

                case "cartcount":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.CartCount);
                        else db.OrderByDescending(p => p.CartCount);
                        break;
                    }

                case "cartmember":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.CartMember);
                        else db.OrderByDescending(p => p.CartMember);
                        break;
                    }
                case "paymentmember":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.PaymentMember);
                        else db.OrderByDescending(p => p.PaymentMember);
                        break;
                    }
                case "paymentorder":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.PaymentOrder);
                        else db.OrderByDescending(p => p.PaymentOrder);
                        break;
                    }
                case "paymentamount":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.PaymentAmount);
                        else db.OrderByDescending(p => p.PaymentAmount);
                        break;
                    }
                case "starttimestr":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.StartTime);
                        else db.OrderByDescending(p => p.StartTime);
                        break;
                    }
                default:
                    db.OrderByDescending(p => p.Sequence).OrderByDescending(p => p.RoomId);
                    break;
            }


            if (query.HasPage)
            {
                var model = db.ToPagedList<LiveViewModel>(query.PageNo, query.PageSize);
                return new QueryPageModel<LiveViewModel> { Models = model, Total = model.TotalRecordCount };
            }
            else
            {
                var model = db.ToList<LiveViewModel>();
                return new QueryPageModel<LiveViewModel> { Models = model, Total = model.Count };
            }
        }

        /// <summary>
        /// 获取无回放直播
        /// </summary>
        /// <returns></returns>
        public List<long> GetNoReplay()
        {
            return DbFactory.Default
                .Get<LiveRoomInfo>(p => p.Status == LiveRoomStatus.End && p.HasReplay == false)
                .Select(p => p.RoomId)
                .ToList<long>();
        }

        public void CreateReplay(long roomId, List<LiveReply> replays)
        {
            var data = replays.Select(p => new LiveReplyInfo
            {
                RoomId = (int)roomId,
                CreateTime = p.CreateTime,
                ExpireTime = p.ExpireTime,
                MediaUrl = p.MediaUrl,
            }).ToList();
            DbFactory.Default.Set<LiveRoomInfo>()
                .Where(p => p.RoomId == roomId)
                .Set(p => p.HasReplay, true)
                .Execute();
            DbFactory.Default.Add(data);
        }

        public QueryPageModel<LiveProduct> GetLiveProducts(LiveQuery query)
        {
            var liveQuery = DbFactory.Default
               .Get<LiveProductInfo>();
            if (query.RoomId > 0)
            {
                liveQuery.Where(p => p.RoomId == query.RoomId);
            }
            if (!string.IsNullOrEmpty(query.ProductName))
            {
                liveQuery.Where(p => p.Name.Contains(query.ProductName));
            }
            if (query.HasPage)
            {
                var model = liveQuery.ToPagedList<LiveProduct>(query.PageNo, query.PageSize);

                return new QueryPageModel<LiveProduct> { Models = model, Total = model.TotalRecordCount };
            }
            else
            {
                var model = liveQuery.ToList<LiveProduct>();
                return new QueryPageModel<LiveProduct> { Models = model, Total = model.Count };
            }

        }


        public List<LiveProductInfo> GetLiveProducts(List<long> roomIds)
        {
            var livep = DbFactory.Default
               .Get<LiveProductInfo>().Where(l=>l.RoomId.ExIn(roomIds)).ToList<LiveProductInfo>();
            return livep;
        }
        /// <summary>
        /// 新增订单支付
        /// </summary>
        public void IncreasecPayment(long roomId, decimal amount, bool newPayment)
        {
            if (roomId <= 0) { return; }
            var db = DbFactory.Default.Set<LiveRoomInfo>()
                .Where(p => p.RoomId == roomId)
                .Set(p => p.PaymentAmount, p => p.PaymentAmount + amount)
                .Set(p => p.PaymentOrder, p => p.PaymentOrder + 1);
            if (newPayment)
                db.Set(p => p.PaymentMember, p => p.PaymentMember + 1);
            db.Execute();
        }

        /// <summary>
        /// 新增购物车
        /// </summary>
        public void IncreasecCart(long roomId, bool newMember)
        {
            if (roomId <= 0) { return; }
            var db = DbFactory.Default.Set<LiveRoomInfo>()
                    .Where(p => p.RoomId == roomId)
                    .Set(p => p.CartCount, p => p.CartCount + 1);
            if (newMember)
                db.Set(p => p.CartMember, p => p.CartMember + 1);
            db.Execute();
        }

        /// <summary>
        /// 新增商品业绩
        /// </summary>
        /// <param name="key"></param>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <param name="realTotalPrice"></param>
        public void IncreasecProduct(long roomId, long productId, long quantity, decimal amount)
        {
            if (roomId <= 0) { return; }
            DbFactory.Default.Set<LiveProductInfo>()
               .Where(p => p.RoomId == roomId && p.ProductId == productId)
               .Set(p => p.SaleCount, p => p.SaleCount + quantity)
               .Set(p => p.SaleAmount, p => p.SaleAmount + amount)
               .Execute();
        }


        public LiveViewModel GetFirstLivingRoom()
        {
            return DbFactory.Default
             .Get<LiveRoomInfo>().Where(p => p.Status == LiveRoomStatus.Living).OrderByDescending(p => p.Sequence).FirstOrDefault<LiveViewModel>();
        }

        /// <summary>
        /// 获取场次详情
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public LiveViewModel GetLiveDetail(long roomId)
        {
            var detailModel = DbFactory.Default.Get<LiveRoomInfo>().Where(p => p.RoomId == roomId).FirstOrDefault<LiveViewModel>();
            var recordingData = DbFactory.Default.Get<LiveReplyInfo>()
                .Where(p => p.RoomId == roomId)
                .Select(p => p.MediaUrl).ToList<string>();

            detailModel.RecordingUrlList = recordingData;
            return detailModel;
        }

        /// <summary>
        /// 设置封面图
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="banner"></param>
        public void SetLiveBanner(long roomId, string banner)
        {
            DbFactory.Default.Set<LiveRoomInfo>().Set(p => p.CoverImg, banner).Where(p => p.RoomId == roomId).Succeed();
        }


        /// <summary>
        /// 查询主播列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<AnchorModel> GetAnchorList(AnchorQuery query)
        {
            var db = DbFactory.Default.Get<AnchorInfo>();
            if (query.ShopId > 0)
            {
                db.Where(e => e.ShopId == query.ShopId);
            }
            if (!string.IsNullOrEmpty(query.AnchorName))
            {
                db.Where(e => e.AnchorName.Contains(query.AnchorName));
            }
            if (!string.IsNullOrEmpty(query.Cellphone))
            {
                db.Where(e => e.CellPhone == query.Cellphone);
            }
            switch (query.Sort.ToLower())
            {
                case "id":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.Id);
                        else db.OrderByDescending(p => p.Id);
                        break;
                    }


                default:
                    db.OrderByDescending(p => p.Id);
                    break;
            }


            if (query.HasPage)
            {
                var model = db.ToPagedList<AnchorModel>(query.PageNo, query.PageSize);

                return new QueryPageModel<AnchorModel> { Models = model, Total = model.TotalRecordCount };
            }
            else
            {
                var model = db.ToList<AnchorModel>();
                return new QueryPageModel<AnchorModel> { Models = model, Total = model.Count };
            }

        }


        /// <summary>
        /// 获取主播详情信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AnchorModel GetAnchorInfo(long id)
        {
            var detailModel = DbFactory.Default.Get<AnchorInfo>().Where(p => p.Id == id).FirstOrDefault<AnchorModel>();
            return detailModel;
        }
        /// <summary>
        /// 获取主播详情信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AnchorModel GetAnchorInfo(string wechat)
        {
            var detailModel = DbFactory.Default.Get<AnchorInfo>().Where(p => p.WeChat == wechat).FirstOrDefault<AnchorModel>();
            return detailModel;
        }

        /// <summary>
        /// 获取直播商品列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<LiveProductLibaryModel> GetLiveProductList(LiveProductLibaryQuery query)
        {
            var db = DbFactory.Default.Get<LiveProductLibraryInfo>();
           
            if (query.ShopId > 0)
            {
                var productIds = DbFactory.Default
                .Get<ProductInfo>()
                .Where(item =>item.IsDeleted == false&&item.AuditStatus == ProductAuditStatus.Audited&&item.ShopId==query.ShopId).Select(item => item.Id);
                db.Where(e =>e.ProductId.ExIn(productIds));
            }
            if (!query.FilterProductIds.IsEmptyString())
            {
                List<long> productIds = query.FilterProductIds.ToLongList();
                db.Where(e => e.ProductId.ExNotIn(productIds));
            }
            if (query.FilterRoomId > 0)
            {
                var productIds = DbFactory.Default
                  .Get<LiveProductInfo>()
                  .Where(p => p.RoomId == query.FilterRoomId).Select(p => p.ProductId);
                db.Where(item => item.ProductId.ExNotIn(productIds));
            }
            if (query.IsCanMoveProduct)
            {
                db.Where(item => (item.LiveAuditStatus == LiveProductLibraryInfo.LiveProductAuditStatus.NoAudit && item.LiveAuditMsg == "撤回审核"));
            }
            if (query.IsReCallProduct)
            {
                db.Where(item => (item.LiveAuditStatus == LiveProductLibraryInfo.LiveProductAuditStatus.NoAudit && item.LiveAuditMsg == "撤回审核") || item.AuditId == 0 || item.GoodsId == 0);
            }
            if (query.LiveAuditStatus != null && query.LiveAuditStatus.Count > 0)
            {
                db.Where(item => item.LiveAuditStatus.ExIn(query.LiveAuditStatus));
            }
            if (query.AuditStatus.HasValue)
            {
                db.Where(item => item.LiveAuditStatus == query.AuditStatus.Value);
            }
            if (!query.ProductIds.IsEmptyString())
            {
                List<long> productIds = query.ProductIds.ToLongList();
                db.Where(e => e.ProductId.ExIn(productIds));
            }
            if (!query.ProductName.IsEmptyString())
            {
                var productIds = DbFactory.Default
                    .Get<ProductInfo>()
                    .Where(p => p.ProductName.Contains(query.ProductName)).Select(p => p.Id);
                db.Where(item => item.ProductId.ExIn(productIds));
            }
            if (query.RoomId > 0)
            {
                var productIds = DbFactory.Default
                  .Get<LiveProductInfo>()
                  .Where(p => p.RoomId == query.RoomId).Select(p => p.ProductId);
                db.Where(item => item.ProductId.ExIn(productIds));
            }
            if (query.CategoryId > 0 && query.ShopId > 0)
            {
                var shopCategoryIds = DbFactory.Default
                    .Get<ShopCategoryInfo>()
                    .Where(p => p.ShopId == query.ShopId && (p.Id == query.CategoryId || p.ParentCategoryId == query.CategoryId))
                    .Select(p => p.Id);

                var productIds = DbFactory.Default
                    .Get<ProductShopCategoryInfo>()
                    .Where(p => p.ShopCategoryId.ExIn(shopCategoryIds))
                    .Select(p => p.ProductId);

                db.Where(item => item.ProductId.ExIn(productIds));
            }
            if (query.Categories != null)
            {
                var productIds = DbFactory.Default
                       .Get<ProductInfo>()
                       .Where(p => p.CategoryId.ExIn(query.Categories)).Select(p => p.Id);
                db.Where(item => item.ProductId.ExIn(productIds));
            }
            if (!query.Keywords.IsEmptyString())
            {
                var productIds = DbFactory.Default
                      .Get<ProductInfo>()
                      .Where(p => p.ProductName.Contains(query.Keywords)).Select(p => p.Id);
                db.Where(item => item.ProductId.ExIn(productIds));
            }
            switch (query.Sort.ToLower())
            {
                case "id":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.ProductId);
                        else db.OrderByDescending(p => p.ProductId);
                        break;
                    }


                default:
                    db.OrderBy(p => p.LiveAuditStatus);
                    break;
            }


            if (query.HasPage)
            {
                var model = db.ToPagedList<LiveProductLibaryModel>(query.PageNo, query.PageSize);
                return new QueryPageModel<LiveProductLibaryModel> { Models = model, Total = model.TotalRecordCount };
            }
            else
            {
                var model = db.ToList<LiveProductLibaryModel>();
                return new QueryPageModel<LiveProductLibaryModel> { Models = model, Total = model.Count };
            }
        }
   
        public List<LiveRoomData> GetLiveing() =>
            CacheManager.GetRoomLiveing(() =>
            {
                var data = DbFactory.Default.Get<LiveRoomInfo>(p => p.Status == LiveRoomStatus.Living).ToList<LiveRoomData>();
                var list = data.Select(p => p.RoomId).ToList();
                var products = DbFactory.Default.Get<LiveProductInfo>(p => p.RoomId.ExIn(list)).ToList();
                foreach (var item in data)
                {
                    item.Products = products.Where(p => p.RoomId == item.RoomId)
                    .Select(p => new LiveProductData
                    {
                        Name = p.Name,
                        Price = p.Price,
                        ProductId = p.ProductId
                    }).ToList();
                }
                return data;
            });

        /// <summary>
        /// 获取正在直播中的商品
        /// </summary>
        /// <returns></returns>
        public List<LiveProductInfo> GetLiveingProduct(IEnumerable<long> pids) {
            var liveprodcut=DbFactory.Default.Get<LiveProductInfo>().Where(p=>p.ProductId.ExIn(pids)).LeftJoin<LiveRoomInfo>((p, r) => p.RoomId == r.RoomId).Where<LiveRoomInfo>(r=>r.Status== LiveRoomStatus.Living).ToList();
            return liveprodcut;
        }

        /// <summary>
        /// 获取直播商品列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<LiveProductLibraryInfo> GetLiveProductLibaryList(LiveProductLibaryQuery query)
        {
            var db = DbFactory.Default.Get<LiveProductLibraryInfo>().LeftJoin<ProductInfo>((pl, p) => pl.ProductId == p.Id);

            if (query.ShopId > 0)
            {
                db.Where(e => e.ShopId == query.ShopId);
            }
            if (!query.FilterProductIds.IsEmptyString())
            {
                List<long> productIds = query.FilterProductIds.ToLongList();
                db.Where(e => e.ProductId.ExNotIn(productIds));
            }
            if (query.FilterRoomId > 0)
            {
                var productIds = DbFactory.Default
                  .Get<LiveProductInfo>()
                  .Where(p => p.RoomId == query.FilterRoomId);
                db.Where(item => item.ProductId.ExNotIn(productIds));
            }
            if (query.IsCanMoveProduct)
            {
                db.Where(item => (item.LiveAuditStatus == LiveProductLibraryInfo.LiveProductAuditStatus.NoSubmit || (item.LiveAuditStatus == LiveProductLibraryInfo.LiveProductAuditStatus.NoAudit && item.LiveAuditMsg == "撤回审核")));
            }
            if (query.IsReCallProduct)
            {
                db.Where(item => (item.LiveAuditStatus == LiveProductLibraryInfo.LiveProductAuditStatus.NoAudit && item.LiveAuditMsg == "撤回审核") || item.AuditId == 0 || item.GoodsId == 0);
            }
            if (query.LiveAuditStatus != null && query.LiveAuditStatus.Count > 0)
            {
                db.Where(item => item.LiveAuditStatus.ExIn(query.LiveAuditStatus));
            }
            if (!query.ProductIds.IsEmptyString())
            {
                List<long> productIds = query.ProductIds.ToLongList();
                db.Where(e => e.ProductId.ExIn(productIds));
            }
            if (!query.ProductName.IsEmptyString())
            {
                var productIds = DbFactory.Default
                    .Get<ProductInfo>()
                    .Where(p => p.ProductName.Contains(query.ProductName));
                db.Where(item => item.ProductId.ExIn(productIds));
            }
            if (query.RoomId > 0)
            {
                var productIds = DbFactory.Default
                  .Get<LiveProductInfo>()
                  .Where(p => p.RoomId == query.RoomId);
                db.Where(item => item.ProductId.ExIn(productIds));
            }
            switch (query.Sort.ToLower())
            {
                case "id":
                    {
                        if (query.IsAsc) db.OrderBy(p => p.ProductId);
                        else db.OrderByDescending(p => p.ProductId);
                        break;
                    }


                default:
                    db.OrderByDescending(p => p.ProductId);
                    break;
            }



            return db.ToList<LiveProductLibraryInfo>();

        }

     
        /// <summary>
        /// 从直播库中删除商品
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public bool DeleteAppletLiveProduct(long productId)
        {
            LiveProductLibraryInfo liveProductLibraryInfo = DbFactory.Default.Get<LiveProductLibraryInfo>(p => p.ProductId == productId).FirstOrDefault();
            if (liveProductLibraryInfo != null)
            {
                Log.Info(liveProductLibraryInfo);
                if (DbFactory.Default.Delete<LiveProductLibraryInfo>(liveProductLibraryInfo) > 0)
                {

                    return true;

                }
                else { Log.Info("删除失败：" + productId); return false; }
            }
            else
            {
                Log.Info("商品为空：" + productId);
                return false;
            }

        }

      
        /// <summary>
        /// 获取可关联主播的会员信息.
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="pageNo">页数</param>
        /// <param name="pageSize">一页多少条</param>
        /// <returns></returns>
        public QueryPageModel<MemberInfo> GetAnchorMemberList(string keyWords,long userId,long shopId,int pageNo,int pageSize)
        {
            var existAnchorUserIds = DbFactory.Default.Get<AnchorInfo>().Where(o => o.ShopId == shopId).Select(o => o.UserId);

            var db = DbFactory.Default.Get<MemberInfo>().LeftJoin<MemberOpenIdInfo>((m, o) => m.Id == o.UserId).Where<MemberOpenIdInfo>(t => t.ServiceProvider == "WeiXinSmallProg");
            db.Where(t => t.Id != userId && t.Disabled == false && t.Id.ExNotIn(existAnchorUserIds));
            if (!string.IsNullOrEmpty(keyWords))
            {
                db.Where(t => t.UserName.Contains(keyWords) || t.Nick.Contains(keyWords) || t.CellPhone.Contains(keyWords));
            }

            var model = db.ToPagedList<MemberInfo>(pageNo, pageSize);
            return new QueryPageModel<MemberInfo> { Models = model, Total = model.TotalRecordCount };
        }

        /// <summary>
        /// 更新主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool UpdateAnchorInfo(AnchorInfo anchor)
        {
            return DbFactory.Default.Update(anchor) > 0;
        }
        /// <summary>
        /// 添加主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool AddAnchorInfo(AnchorInfo anchor)
        {
            return DbFactory.Default.Add(anchor);
        }
        /// <summary>
        /// 删除主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool DelAnchorInfo(int anchorId)
        {
            AnchorInfo info = DbFactory.Default.Get<AnchorInfo>(a => a.Id == anchorId).FirstOrDefault();
            if (info == null)
            {
                return false;
            }

            return DbFactory.Default.Del<AnchorInfo>(info);
        }
        /// <summary>
        /// 保存直播间图片
        /// </summary>
        /// <param name="filePath"></param>
        public string SaveLiveImage(string fileURL, long roomId, string imageType = "ShareImg")
        {
            if (fileURL.ToNullString().ToLower().IndexOf("/temp/") < 0 || fileURL.ToNullString().ToLower().StartsWith("http://") || fileURL.ToNullString().ToLower().StartsWith("https://"))
            {
                return fileURL;
            }
            string returnPath = string.Empty;
            string targetFolder = "\\Storage\\Live\\" + roomId + "\\";
            string storeagePath = string.Format("{0}", HttpContext.Current.Server.MapPath(targetFolder));
            IOHelper.CreatePath(targetFolder);
            int fileNameStartIndex = fileURL.LastIndexOf("/");
            if (fileNameStartIndex == -1 && fileNameStartIndex >= fileURL.Length)
            {
                return fileURL;
            }

            var newFilename = imageType + "_" + fileURL.Substring(fileURL.LastIndexOf("/") + 1);
            var destFileName = storeagePath + newFilename;
            var sourceFilePath = HttpContext.Current.Server.MapPath(fileURL);
            if (File.Exists(sourceFilePath))
            {
                // 复制原图到商品目录
                File.Copy(sourceFilePath, destFileName, true);
            }
            returnPath = targetFolder + newFilename;

            var deleteFilePath = HttpContext.Current.Server.MapPath(fileURL);
            //删除临时文件
            if (File.Exists(deleteFilePath))
            {
                File.Delete(deleteFilePath);
            }

            returnPath = returnPath.Replace("\\", "/").Replace("//", "/");
            return returnPath;
        }
        /// <summary>
        /// 根据直播间ID获取直接间数据
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public LiveRoomInfo GetLiveRoomInfo(long roomId)
        {
            return DbFactory.Default.Get<LiveRoomInfo>(l => l.RoomId == roomId).FirstOrDefault();
        }

        /// <summary>
        /// 根据ID获取直接间数据
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public LiveRoomInfo GetLiveRoomInfoBaseId(long Id)
        {
            return DbFactory.Default.Get<LiveRoomInfo>(l => l.Id == Id).FirstOrDefault();
        }
        /// <summary>
        /// 更新直播间数据
        /// </summary>
        /// <param name="path"></param>
        public bool UpdateLiveRoom(LiveRoomInfo liveRoom)
        {
            var result = DbFactory.Default.Update(liveRoom, liveRoom.Id) > 0;
            CacheManager.ClearRoomLiveing();
            return result;
        }

        /// <summary>
        /// 新增直播间数据(商家添加）
        /// </summary>
        /// <param name="liveRoom"></param>
        public bool AddLiveRoom(LiveRoomInfo liveRoom)
        {
            var result = DbFactory.Default.Add(liveRoom);
            CacheManager.ClearRoomLiveing();
            return result;
        }
        /// <summary>
        /// 商家将商品添加至商品库
        /// </summary>
        /// <param name="productInfos"></param>
        /// <returns></returns>
        public bool AddProductToLiveProductLibary(List<LiveProductLibraryInfo> productInfos)
        {
            var result = DbFactory.Default.AddRange<LiveProductLibraryInfo>(productInfos);
            CacheManager.ClearRoomLiveing();
            return result;
        }

        public List<LiveViewModel> GetLiveRoomByIds(List<long> roomIds) {
            var livemodel= DbFactory.Default.Get<LiveRoomInfo>().Where(r=>r.RoomId.ExIn(roomIds)).ToList<LiveViewModel>();
            return livemodel;
        }


    }
}
