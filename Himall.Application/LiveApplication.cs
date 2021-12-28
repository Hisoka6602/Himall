using Himall.CommonModel;
using Himall.CommonModel.Model;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.Live;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Himall.Entities.LiveProductInfo;

namespace Himall.Application
{
    public class LiveApplication : BaseApplicaion<LiveService>
    {
        private static WXApiService _iWXService = ObjectContainer.Current.Resolve<WXApiService>();
        private static LiveService _LiveService = ObjectContainer.Current.Resolve<LiveService>();
        /// <summary>
        /// 同步直播间信息
        /// </summary>
        public static void SyncLiveRoom()
        {
            var setting = SiteSettingApplication.SiteSettings;
            var lives = _iWXService.GetLive(setting.WeixinAppletId, setting.WeixinAppletSecret);
            foreach (var item in lives)
            {
                var room = item.Map<LiveRoomInfo>();
                var products = item.Products.Map<List<LiveProductInfo>>();
                Service.SaveRoom(room, products);
            }
        }

        public static void SyncLiveData()
        {
            try
            {
                var setting = SiteSettingApplication.SiteSettings;
                var lives = _iWXService.GetLive(setting.WeixinAppletId, setting.WeixinAppletSecret);
                foreach (var item in lives)
                {
                    var room = item.Map<LiveRoomInfo>();
                    var products = item.Products.Map<List<LiveProductInfo>>();
                    Service.SaveRoom(room, products);
                }

                var list = Service.GetNoReplay();
                if (list.Count == 0) return;

                foreach (var item in list)
                {
                    var replay = _iWXService.GetLiveReplay(setting.WeixinAppletId, setting.WeixinAppletSecret, item)
                        .Where(p => p.MediaUrl.Contains(".m3u8")).ToList();//仅保留.m3u8格式文件
                    if (replay.Count == 0) continue;
                    Service.CreateReplay(item, replay);
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("api unauthorized rid"))
                {
                    Log.Error(ex);
                }
            }
        }

        public static void SyncLiveRoom(object obj)
        {
            try
            {
                var setting = SiteSettingApplication.SiteSettings;
                var lives = _iWXService.GetLive(setting.WeixinAppletId, setting.WeixinAppletSecret);
                foreach (var item in lives)
                {
                    var room = item.Map<LiveRoomInfo>();
                    var products = item.Products.Map<List<LiveProductInfo>>();
                    Service.SaveRoom(room, products);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public static void SyncLiveReply()
        {
            var list = Service.GetNoReplay();
            if (list.Count == 0) return;

            var setting = SiteSettingApplication.SiteSettings;
            foreach (var item in list)
            {
                var replay = _iWXService.GetLiveReplay(setting.WeixinAppletId, setting.WeixinAppletSecret, item)
                    .Where(p => p.MediaUrl.Contains(".m3u8")).ToList();//仅保留.m3u8格式文件
                if (replay.Count == 0) continue;
                Service.CreateReplay(item, replay);
            }
        }

        public static void SetShop(long roomId, long franchiseeId)
        {
            Service.SetShop(roomId, franchiseeId);
        }

        public static void SetSequence(long roomId, long sequence)
        {
            Service.SetSequence(roomId, sequence);
        }

        public static long GetMaxSequence()
        {
            return Service.GetMaxSequence();
        }

        public static void DelLiveRoom(long roomId, long id)
        {
            Service.DelLiveRoom(roomId, id);
        }

        public static void DelLiveReplay(long roomId)
        {
            Service.DelLiveReplay(roomId);
        }
        public static QueryPageModel<LiveViewModel> GetLiveList(LiveQuery query)
        {
            var data = Service.GetLiveList(query);
            var shop_list = data.Models.Where(p => p.ShopId > 0).Select(p => p.ShopId).ToList();
            var shops = ShopApplication.GetShops(shop_list);
            foreach (var item in data.Models)
            {
                var model = shops.FirstOrDefault(p => p.Id == item.ShopId);
                if (model != null) item.ShopName = model.ShopName;
                else item.ShopName = "平台";

                item.CoverImg = HimallIO.GetRomoteImagePath(item.CoverImg);
            }
            return data;
        }

        public static QueryPageModel<LiveProduct> GetLiveProducts(LiveQuery query)
        {
            return Service.GetLiveProducts(query);
        }

        public static List<LiveProductInfo> GetLiveProducts(List<long> roomIds) {
            return Service.GetLiveProducts(roomIds);
        }

        public static List<LiveViewModel> GetLiveRoomByIds(List<long> roomIds) {
            return Service.GetLiveRoomByIds(roomIds);
        }

        

        public static LiveViewModel GetFirstLivingRoom()
        {
            var live = Service.GetFirstLivingRoom();
            if (live == null) return null;
            live.CoverImg = HimallIO.GetRomoteImagePath(live.CoverImg);
            return live;
        }

        public static LiveViewModel GetLiveDetail(long roomId)
        {
            var live = Service.GetLiveDetail(roomId);
            //仅显示.m3u8
            live.RecordingUrlList = live.RecordingUrlList.Where(p => p.Contains("playlist_eof.m3u8")).ToList();
            live.CoverImg = HimallIO.GetRomoteImagePath(live.CoverImg);
            return live;
        }

        public static void SetLiveBanner(long roomId, string banner)
        {
            Service.SetLiveBanner(roomId, banner);
        }

        /// <summary>
        /// 保存直播间信息
        /// </summary>
        /// <param name="liveRoom"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static long AuditLiveRoom(LiveRoomInfo liveRoom, out string msg)
        {
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.SaveLiveRoom(setting.WeixinAppletId, setting.WeixinAppletSecret, liveRoom, out msg);
        }



        /// <summary>
        /// 小程序上传文件（后端上传）
        /// </summary>
        /// <param name="fileUrl">文件路径（已在服务器中的图片）</param>
        /// <param name="mediaType">文件类型（默认为图片）</param>
        /// <param name="msg">输出错误信息</param>
        /// <param name="appId">appId</param>
        /// <param name="appSecrect">appSecrect</param>
        /// <returns></returns>
        public static AppletUploadResult AppletMeidaUpload(string fileUrl, MediaType mediaType, out string msg)
        {
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.AppletMeidaUpload(setting.WeixinAppletId, setting.WeixinAppletSecret, fileUrl, mediaType, out msg);
        }
        /// <summary>
        /// 获取主播列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<AnchorModel> GetAnchorList(AnchorQuery query)
        {
            QueryPageModel<AnchorModel> pageModel = _LiveService.GetAnchorList(query);
            if (pageModel.Models != null && pageModel.Models.Count > 0)
            {
                var models = pageModel.Models;
                var member_list = models.Where(a => a.UserId > 0).Select(a => a.UserId).ToList();
                var members = MemberApplication.GetMembersByIds(member_list);
                var shop_list = models.Where(a => a.ShopId > 0).Select(a => a.ShopId).ToList();
                var shops = ShopApplication.GetShopNames(shop_list);
                foreach (var item in pageModel.Models)
                {
                    var model = members.FirstOrDefault(m => m.Id == item.UserId);
                    item.ShopName = shops.ContainsKey(item.ShopId) ? shops[item.ShopId] : "";
                    if (model != null)
                    {
                        item.UserName = model.UserName;
                        item.RealName = model.RealName;
                        item.Nick = model.Nick;
                    }
                    else
                    {
                        item.UserName = "";
                        item.RealName = "";
                        item.Nick = "";
                    }
                }
            }
            return pageModel;
        }

        /// <summary>
        /// 获取主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static AnchorModel GetAnchorInfo(int anchorId)
        {
            var anchor= _LiveService.GetAnchorInfo(anchorId);
            if(anchor!=null && anchor.UserId > 0)
            {
                var mem = MemberApplication.GetMember(anchor.UserId);
                if (mem != null)
                {
                    anchor.UserName = mem.UserName;
                    anchor.Nick = mem.Nick;
                    anchor.RealName = mem.RealName;
                }
            }
            return anchor;
        }

        /// <summary>
        /// 判断商品是否正在参与直播
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static long IsLiveProduct(long productId)
        {
            var living = Service.GetLiveing();
            var room = living.FirstOrDefault(p => p.Products.Any(i => i.ProductId == productId));
            return room?.RoomId ?? 0;
        }

        /// <summary>
        /// 根据微信贴获取主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static AnchorModel GetAnchorInfo(string wechat)
        {
            return _LiveService.GetAnchorInfo(wechat);
        }

        /// <summary>
        /// 获取可关联主播的会员信息.
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="pageNo">页数</param>
        /// <param name="pageSize">一页多少条</param>
        /// <returns></returns>
        public static QueryPageModel<MemberInfo> GetAnchorMemberList(string keyWords, long userId, long shopId, int pageNo, int pageSize)
        {
            return _LiveService.GetAnchorMemberList(keyWords, userId, shopId, pageNo, pageSize);
        }

        /// <summary>
        /// 更新主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool UpdateAnchorInfo(AnchorInfo anchor)
        {
            return _LiveService.UpdateAnchorInfo(anchor);
        }
        /// <summary>
        /// 添加主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool AddAnchorInfo(AnchorInfo anchor,out string codeurl,out bool isRealNameVerify)
        {
            bool tag = false;
            var setting = SiteSettingApplication.SiteSettings;
            if (_iWXService.AddAnchorRole(setting.WeixinAppletId, setting.WeixinAppletSecret, anchor.WeChat, out codeurl,out isRealNameVerify))
            {
                tag = _LiveService.AddAnchorInfo(anchor);
            }
            return tag;
        }

       

       
        /// <summary>
        /// 删除主播信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool DelAnchorInfo(int anchorId)
        {
            return _LiveService.DelAnchorInfo(anchorId);
        }
        /// <summary>
        /// 导入商品到直播间
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool ImportProductToLiveRoom(List<LiveProductLibaryModel> productInfos, long roomId, out string msg)
        {
            msg = "";
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.ImportProductToLiveRoom(setting.WeixinAppletId, setting.WeixinAppletSecret, productInfos, roomId, out msg);

        }

        /// <summary>
        /// 商品添加并提审到直播商品库
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool AddProductToLiveProductLibary(List<ProductInfo> productInfos)
        {
            List<LiveProductLibraryInfo> liveProductLibraryInfos = new List<LiveProductLibraryInfo>();
            liveProductLibraryInfos = CopyLiveProductLibarayInfo(productInfos);
            return _LiveService.AddProductToLiveProductLibary(liveProductLibraryInfos);
        }

        /// <summary>
        /// 商品添加并提审到直播商品库
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool AddProductToLiveProductLibary(List<LiveProductLibaryModel> productInfos, out string msg)
        {
            msg = "";
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.AddProductToLiveProductLibary(setting.WeixinAppletId, setting.WeixinAppletSecret, productInfos, out msg);
        }
        /// <summary>
        /// 根据商品信息获取直播商品库信息
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>

        /// <summary>
        /// 根据商品信息获取直播商品库信息
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public static List<LiveProductLibraryInfo> CopyLiveProductLibarayInfo(List<ProductInfo> products)
        {
            List<LiveProductLibraryInfo> liveProductLibaries = new List<LiveProductLibraryInfo>();
            if (products == null || products.Count == 0)
            {
                return liveProductLibaries;
            }

            foreach (ProductInfo product in products)
            {
                LiveProductLibraryInfo info = new LiveProductLibraryInfo();
                List<long> productIds = new List<long>();
                productIds.Add(product.Id);
                List<SKUInfo> skus = ProductManagerApplication.GetSKUsByProduct(productIds);
                info.ApplyLiveTime = DateTime.Now;
                info.AuditId = 0;
                info.GoodsId = 0;
                info.LiveAuditMsg = "";
                info.LiveAuditStatus = LiveProductLibraryInfo.LiveProductAuditStatus.NoSubmit;
                info.ProductId = product.Id;
                info.ShopId = product.ShopId;
                liveProductLibaries.Add(info);
            }
            return liveProductLibaries;

        }

        public static List<LiveProductLibaryModel> CopyLiveProductLibarayModel(List<ProductInfo> products)
        {
            List<LiveProductLibaryModel> liveProductLibaries = new List<LiveProductLibaryModel>();
            if (products == null || products.Count == 0)
            {
                return liveProductLibaries;
            }
            List<long> productIds = products.Select(p => p.Id).ToList();
            List<SKUInfo> skus = ProductManagerApplication.GetSKUsByProduct(productIds);
            List<long> shopIds = products.Select(p => p.ShopId).ToList();
            var shops = ShopApplication.GetShopNames(shopIds);
            foreach (ProductInfo product in products)
            {
                LiveProductLibaryModel info = new LiveProductLibaryModel();

                info.ApplyLiveTime = DateTime.Now;
                info.AuditId = 0;
                info.GoodsId = 0;
                info.LiveAuditMsg = "";
                info.MarketPrice = product.MarketPrice;
                var productSkus = skus.Where(s => s.ProductId == product.Id).ToList();
                if (productSkus != null && productSkus.Count > 0)
                {
                    info.MinSalePrice = productSkus.Min(s => s.SalePrice);
                    info.MaxSalePrice = productSkus.Max(s => s.SalePrice);
                }
                else
                {
                    info.MinSalePrice = 0;
                    info.MaxSalePrice = 0;
                }
                if (shops.ContainsKey(product.ShopId))
                {
                    info.ShopName = shops[product.ShopId];
                }
                info.ShopId = product.ShopId;
                info.Name = product.ProductName;
                info.Image = product.GetImage(ImageSize.Size_220);
                info.LiveAuditStatus = LiveProductLibraryInfo.LiveProductAuditStatus.NoAudit;
                info.ProductId = product.Id;
                info.ShopId = product.ShopId;
                liveProductLibaries.Add(info);
            }
            return liveProductLibaries;

        }

        /// <summary>
        /// 撤回审核
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool ReCallAudit(List<LiveProductLibraryInfo> productInfos, out string msg)
        {
            msg = "";
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.ReCallAudit(setting.WeixinAppletId, setting.WeixinAppletSecret, productInfos, out msg);
        }
        /// <summary>
        /// 重新审核
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool ReApplyAudit(List<LiveProductLibraryInfo> productInfos, out string msg)
        {
            msg = "";
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.ReApplyAudit(setting.WeixinAppletId, setting.WeixinAppletSecret, productInfos, out msg);

        }
        /// <summary>
        /// 移除商品（从商品库中）
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool RemoveProduct(List<LiveProductLibraryInfo> productInfos)
        {
            var setting = SiteSettingApplication.SiteSettings;
            foreach (LiveProductLibraryInfo product in productInfos)
            {
                _LiveService.DeleteAppletLiveProduct(product.ProductId);
            }
            return true;
        }
        /// <summary>
        /// 删除商品
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool DeleteProduct(List<LiveProductLibraryInfo> productInfos, out string msg)
        {
            msg = "";
            var setting = SiteSettingApplication.SiteSettings;
            return _iWXService.DeleteProduct(setting.WeixinAppletId, setting.WeixinAppletSecret, productInfos, out msg);
        }


        /// <summary>
        /// 更新商品信息
        /// </summary>
        /// <param name="productInfos"></param>
        /// <param name="roomId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool UpdateAppletLiveProduct(LiveProductLibaryModel product, out string msg)
        {
            var setting = SiteSettingApplication.SiteSettings;

            return _iWXService.UpdateAppletLiveProduct(setting.WeixinAppletId, setting.WeixinAppletSecret, product, out msg);
        }
        /// <summary>
        /// 更新商品状态
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool UpdateLiveProductStatus(List<LiveProductLibraryInfo> list, out string msg)
        {
            var setting = SiteSettingApplication.SiteSettings;

            return _iWXService.UpdateLiveProductStatus(setting.WeixinAppletId, setting.WeixinAppletSecret, list, out msg);
        }
        /// <summary>
        /// 从小程序获取商品列表
        /// </summary>
        /// <returns></returns>
        public static AppletApiLiveProductList GetLivePrdouctsFromApplet(int start = 0, int limit = 100, int status = 0)
        {
            var setting = SiteSettingApplication.SiteSettings;

            return _iWXService.GetLivePrdouctsFromApplet(setting.WeixinAppletId, setting.WeixinAppletSecret, start, limit, status);
        }
        /// <summary>
        /// 保存直播间图片
        /// </summary>
        /// <param name="filePath"></param>
        public static string SaveLiveRoomImage(string fileURL, long roomId, string imageType = "ShareImg")
        {
            return _LiveService.SaveLiveImage(fileURL, roomId, imageType);
        }
        /// <summary>
        /// 根据直播间ID获取直播间数据
        /// </summary>
        /// <param name="filePath"></param>
        public static LiveRoomInfo GetLiveRoomInfo(long roomId)
        {
            return _LiveService.GetLiveRoomInfo(roomId);
        }
        /// <summary>
        /// 根据ID获取直播间数据
        /// </summary>
        /// <param name="filePath"></param>
        public static LiveRoomInfo GetLiveRoomInfoBaseId(long Id)
        {
            return _LiveService.GetLiveRoomInfoBaseId(Id);
        }
        /// <summary>
        /// 更新直播间
        /// </summary>
        /// <param name="filePath"></param>
        public static bool UpdateLiveRoom(LiveRoomInfo liveRoom)
        {
            return _LiveService.UpdateLiveRoom(liveRoom);
        }
        /// <summary>
        /// 更新直播间
        /// </summary>
        /// <param name="filePath"></param>
        public static bool AddLiveRoom(LiveRoomInfo liveRoom)
        {
            return _LiveService.AddLiveRoom(liveRoom);
        }
        /// <summary>
        /// 获取直播商品列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<LiveProductLibaryModel> GetLiveProductLibrarys(LiveProductLibaryQuery query)
        {
            if (query.CategoryId > 0 && query.ShopId == 0)
            {
                var categories = GetService<CategoryService>().GetAllCategoryByParent(query.CategoryId);
                query.Categories = categories.Select(p => p.Id).ToList();
                query.Categories.Add(query.CategoryId);
            }
            QueryPageModel<LiveProductLibaryModel> pageModel = _LiveService.GetLiveProductList(query);
            if (pageModel.Models != null && pageModel.Models.Count > 0)
            {
                var models = pageModel.Models;
                var product_list = models.Where(a => a.ProductId > 0).Select(a => a.ProductId).ToList();
                var products = ProductManagerApplication.GetProductByIds(product_list);

                var skus = ProductManagerApplication.GetSKUByProducts(product_list);
                var shop_list = models.Where(a => a.ShopId > 0).Select(a => a.ShopId).ToList();
                var shops = ShopApplication.GetShopNames(shop_list);
                foreach (var item in pageModel.Models)
                {
                    var model = products.FirstOrDefault(m => m.Id == item.ProductId);
                    if (model != null)
                    {
                        item.Name = model.ProductName;
                        item.MarketPrice = model.MarketPrice;
                        item.Image = model.GetImage(ImageSize.Size_220);
                    }
                    else
                    {
                        item.Name = "";
                        item.MarketPrice = 0;
                        item.Image = "";
                    }
                    if (shops.ContainsKey(item.ShopId))
                    {
                        item.ShopName = shops[item.ShopId];
                    }

                    if (model != null && model.IsOpenLadder)
                    {
                        item.MinSalePrice = item.MaxSalePrice = model.MinSalePrice;
                    }
                    else
                    {
                        var skulist = skus.Where(m => m.ProductId == item.ProductId).ToList();
                        if (skulist != null && skulist.Count > 0)
                        {
                            item.MaxSalePrice = skulist.Max(s => s.SalePrice);
                            item.MinSalePrice = skulist.Min(s => s.SalePrice);
                        }
                    }
                }

            }
            return pageModel;
        }
        public static List<LiveProductLibraryInfo> GetLiveProductLibaryList(LiveProductLibaryQuery query)
        {
            return _LiveService.GetLiveProductLibaryList(query);
        }

        public static List<LiveProductInfo> GetLiveingProductByIds(IEnumerable<long> pids) {
            return _LiveService.GetLiveingProduct(pids);
        }

    }
}
