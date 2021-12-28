using Hidistro.Core;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Himall.Application
{
    public class WDTProductApplication : BaseApplicaion<ProductService>
    {


        private static ProductService _ProductService = ObjectContainer.Current.Resolve<ProductService>();
        private static StockService _StockService = ObjectContainer.Current.Resolve<StockService>();

        private static Timer timer;
        static WDTProductApplication()
        {

            SyncWdtGoodsData();
        }

        private static void SyncWdtGoodsData()
        {
            WDTConfigModel setting = GetConfigModel();
            if (WdtParamIsValid(setting))
            {
                //推送货品档案
                PushWangDianTongArchives(setting);

                //推送平台货品
                PushWangDianTongProduct(setting);

            }
        }

        /// <summary>
        /// 推送货品档案
        /// </summary>
        /// <param name="setting"></param>
        private static void PushWangDianTongArchives(WDTConfigModel setting)
        {
            string apiUrl = setting.ErpUrl;
            string sid = setting.ErpSid;
            string appkey = setting.ErpAppkey;
            string appsecret = setting.ErpAppsecret;

            if (!string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(sid)
                && !string.IsNullOrEmpty(appkey) && !string.IsNullOrEmpty(appsecret))
            {
                //获取需推送的商品
                List<WdtGoodsArchives> archives = GetPushWdtGoodsArchives(50);
                if (archives == null || archives.Count <= 0)
                {
                    return;
                }

                string message = "";
                var pushResult = BatchPushGoodsArchives(archives, out message);
                if (pushResult == pushState.Success)
                {
                    //更新状态为已推送成功
                    BatchUpdateArchivesPushState(archives.Select(a => a.goods_id), true);
                }
                else
                {
                    //更新状态为推送失败
                    BatchUpdateArchivesPushState(archives.Select(a => a.goods_id), false);
                }

            }
        }

        /// <summary>
        /// 推送平台货品
        /// </summary>
        /// <param name="setting"></param>
        private static void PushWangDianTongProduct(WDTConfigModel setting)
        {
            string apiUrl = setting.ErpUrl;
            string sid = setting.ErpSid;
            string appkey = setting.ErpAppkey;
            string appsecret = setting.ErpAppsecret;
            if (!string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(sid)
                && !string.IsNullOrEmpty(appkey) && !string.IsNullOrEmpty(appsecret))
            {
                //获取需推送的商品
                List<WdtGoods> products = GetPlateGoods(100);
                if (products == null || products.Count <= 0)
                {
                    return;
                }

                string message = "";
                var pushResult = BatchPushGoods(products, out message);
                if (pushResult == pushState.Success)
                {
                    //更新状态为已推送成功
                    BatchUpdatePushState(products, true);
                }
                else
                {
                    //更新状态为推送失败
                    BatchUpdatePushState(products, false);
                }

            }
        }
        #region 货品档案

        /// <summary>
        /// 获取需推送到旺店通货品档案的商品列表
        /// </summary>
        /// <param name="count">推送条数</param>
        /// <returns></returns>
        public static List<WdtGoodsArchives> GetPushWdtGoodsArchives(int count)
        {
            var selfId = ShopApplication.GetSelfShop().Id;
            var prolist = _ProductService.GetPushProductByCount(count, selfId);//获取指定数量
            var skulist = _ProductService.GetSKUs(prolist.Select(p => p.Id));

            List<WdtGoodsArchives> list = new List<WdtGoodsArchives>();
            prolist.ForEach(pro =>
            {
                WdtGoodsArchives good = new WdtGoodsArchives();
                good.goods_id = pro.Id;
                good.goods_name = pro.ProductName;
                good.goods_type = 1;
                good.market_price = pro.MarketPrice;
                var currentskus = skulist.Where(sku => sku.ProductId == pro.Id).ToList();
                good.spec_list = GetWdtGoodsBySku(currentskus, pro);
                list.Add(good);
            });
            return list;
        }

        private static List<WdtGoods> GetWdtGoodsBySku(List<SKUInfo> skulist, ProductInfo currentpro)
        {
            List<WdtGoods> wdtlist = new List<WdtGoods>();
            if (skulist != null && skulist.Count() > 0)
            {
                skulist.ForEach(sku =>
                {
                    WdtGoods wdtgood = new WdtGoods();
                    wdtgood.goods_id = sku.ProductId.ToString();
                    wdtgood.goods_name = currentpro.ProductName;
                    wdtgood.goods_no = currentpro.ProductCode;
                    wdtgood.spec_id = sku.Id;
                    wdtgood.spec_no = string.IsNullOrEmpty(sku.Sku) ? currentpro.ProductCode : sku.Sku;
                    wdtgood.status = (int)currentpro.SaleStatus;//0代表删除，1代表上架，2代表下架
                    wdtgood.spec_name = GetSpecname(sku);
                    string img = currentpro.ImagePath;
                    if (!string.IsNullOrEmpty(sku.ShowPic))
                        img = sku.ShowPic;
                    wdtgood.pic_url = GetPicImg(img);
                    wdtgood.price = sku.SalePrice;
                    wdtgood.stock_num = sku.Stock;
                    wdtgood.cid = currentpro.CategoryId.ToString();
                    wdtlist.Add(wdtgood);
                });
            }
            return wdtlist;
        }

        /// <summary>
        /// 批量更新货品档案的推送状态
        /// </summary>
        /// <param name="goods">货品</param>
        /// <param name="state">推送状态 0未推送 1推送成功 2推送失败</param>
        /// <returns></returns>
        public static void BatchUpdateArchivesPushState(IEnumerable<long> proIds, bool state)
        {
            _ProductService.BatchUpdatePushArchivesState(proIds, state);
        }

        /// <summary>
        /// 批量推送商品至旺店通货品档案
        /// </summary>
        /// <param name="goods">货品</param>
        /// <returns></returns>
        public static pushState BatchPushGoodsArchives(List<WdtGoodsArchives> archivess, out string message)
        {
            try
            {
                message = "";
                WDTConfigModel setting = GetConfigModel();
                string apiUrl = setting.ErpUrl;
                string sid = setting.ErpSid;
                string appkey = setting.ErpAppkey;
                string appsecret = setting.ErpAppsecret;
                if (!WdtParamIsValid(setting))
                {
                    return pushState.Fail;
                }

                WdtClient client = new WdtClient();
                client.sid = sid;
                client.appkey = appkey;
                client.appsecret = appsecret;
                client.gatewayUrl = apiUrl + "/openapi2/goods_push.php";

                var goods_list = archivess.Select(e => new
                {
                    goods_no = e.goods_no,
                    goods_type = 1,
                    goods_name = e.goods_name,
                    spec_list = e.spec_list.Select(s => new
                    {
                        spec_no = s.spec_no,
                        spec_code = s.spec_code,
                        spec_name = s.spec_name,
                        retail_price = s.price,
                        market_price = e.market_price

                    })
                });
                string json = JsonConvert.SerializeObject(goods_list);

                client.putParams("goods_list", json);
                string result = client.wdtOpenapi();
                Log.Info(DateTime.Now.ToString() + "-推送货品档案result：" + result);
                //获取推送结果(没有部分失败情况)
                baseResponse resultModel = JsonConvert.DeserializeObject<baseResponse>(result);
                message = resultModel.message;
                if (resultModel.code == 0)
                {
                    return pushState.Success;
                }
                else
                {
                    return pushState.Fail;
                }

            }
            catch (Exception ex)
            {
                Log.Error(DateTime.Now.ToString() + "-推送货品档案异常：" + ex.ToString());
                message = ex.ToString();
                return pushState.Error;
            }
        }


        /// <summary>
        /// 推送商品档案至旺店通
        /// </summary>
        /// <param name="productId">商品Id</param>
        /// <returns></returns>
        public static bool PushArchivesById(long productId)
        {
            var proenty = ProductManagerApplication.GetProduct(productId);
            if (proenty == null)
            {
                return false;
            }

            WdtGoodsArchives archives = new WdtGoodsArchives()
            {
                goods_id = proenty.Id,
                goods_no = proenty.ProductCode,
                goods_name = proenty.ProductName,
                market_price = proenty.MarketPrice
            };
            string message = "";
            List<WdtGoodsArchives> Infos = new List<WdtGoodsArchives>() { archives };
            pushState state = BatchPushGoodsArchives(Infos, out message);

            if (state == pushState.Success)
            {
                BatchUpdateArchivesPushState(Infos.Select(i => i.goods_id), true);
                return true;
            }
            else
            {
                BatchUpdateArchivesPushState(Infos.Select(i => i.goods_id), false);
                return false;
            }
        }

        #endregion


        #region 平台货品

        /// <summary>
        /// 获取需推送到旺店通的平台货品列表
        /// </summary>
        /// <param name="count">推送条数</param>
        /// <returns></returns>
        public static List<WdtGoods> GetPlateGoods(int count)
        {
            var selfId = ShopApplication.GetSelfShop().Id;
            var prolist = _ProductService.GetPlatePushProductByCount(count, selfId);//获取指定数量
            var skulist = _ProductService.GetSKUs(prolist.Select(p => p.Id));//规格列表
            List<WdtGoods> list = new List<WdtGoods>();
            skulist.ForEach(sku =>
            {
                var currentpro = prolist.Where(p => p.Id == sku.ProductId).FirstOrDefault();

                WdtGoods wdtgood = new WdtGoods();
                wdtgood.goods_id = sku.ProductId.ToString();
                wdtgood.goods_name = currentpro.ProductName;
                wdtgood.goods_no = currentpro.ProductCode;
                wdtgood.spec_id = sku.Id;
                wdtgood.spec_no = string.IsNullOrEmpty(sku.Sku) ? currentpro.ProductCode : sku.Sku;
                wdtgood.status = (int)currentpro.SaleStatus;//0代表删除，1代表上架，2代表下架
                wdtgood.spec_name = GetSpecname(sku);
                string img = currentpro.ImagePath;
                if (!string.IsNullOrEmpty(sku.ShowPic))
                    img = sku.ShowPic;
                wdtgood.pic_url = GetPicImg(img);
                wdtgood.price = sku.SalePrice;
                wdtgood.stock_num = sku.Stock;
                wdtgood.cid = currentpro.CategoryId.ToString();
                list.Add(wdtgood);

            });
            return list;
        }

        private static string GetSpecname(SKUInfo sku)
        {
            string result = "";
            if (!string.IsNullOrEmpty(sku.Size))
            {
                result += sku.SizeAlias + ":" + sku.Size + ";";
            }
            if (!string.IsNullOrEmpty(sku.Color))
            {
                result += sku.ColorAlias + ":" + sku.Color + ";";
            }
            if (!string.IsNullOrEmpty(sku.Version))
            {
                result += sku.VersionAlias + ":" + sku.Version + ";";
            }

            return result;
        }

        private static string GetPicImg(string imgurl)
        {
            SiteSettings settings = SiteSettingApplication.SiteSettings;
            string result = "";
            if (imgurl == null || string.IsNullOrEmpty(imgurl))
            {
                return result;
            }
            if (imgurl.StartsWith("https://") || imgurl.StartsWith("http://"))
            {
                return imgurl;
            }
            string siteUrl = settings.SiteUrl;
            siteUrl = siteUrl.EndsWith("/") ? siteUrl : (siteUrl + "/");
            if (!siteUrl.StartsWith("https://") && !siteUrl.StartsWith("http://"))
            {
                siteUrl = "http://" + siteUrl;
            }
            if (!imgurl.Contains("skus"))
            {
                result = settings.SiteUrl + imgurl.TrimStart('/');
            }
            else
            {
                result = settings.SiteUrl + HimallIO.GetProductSizeImage(imgurl, 1, (int)Himall.CommonModel.ImageSize.Size_500).TrimStart('/');
            }
            return result;
        }

        /// <summary>
        /// 批量推送商品至旺店通
        /// </summary>
        /// <param name="goods">货品</param>
        /// <returns></returns>
        public static pushState BatchPushGoods(List<WdtGoods> goods, out string message)
        {
            try
            {

                message = "";
                WDTConfigModel setting = GetConfigModel();
                string apiUrl = setting.ErpUrl;
                string sid = setting.ErpSid;
                string appkey = setting.ErpAppkey;
                string appsecret = setting.ErpAppsecret;
                if (!WdtParamIsValid(setting))
                {
                    return pushState.Fail;
                }
                WdtClient client = new WdtClient();
                client.sid = sid;
                client.appkey = appkey;
                client.appsecret = appsecret;
                client.gatewayUrl = apiUrl + "/openapi2/api_goodsspec_push.php";

                var api_goods_info = new
                {
                    platform_id = setting.ErpPlateId,
                    shop_no = setting.ErpStoreNumber,
                    goods_list = goods.Select(t => new
                    {
                        goods_id = t.goods_id,
                        spec_id = t.spec_id,
                        goods_no = t.goods_no,
                        spec_no = t.spec_no,
                        status = t.status,
                        goods_name = t.goods_name,
                        spec_name = t.spec_name,
                        price = t.price,
                        stock_num = t.stock_num,
                        cid = t.cid
                    })
                };

                string json = JsonConvert.SerializeObject(api_goods_info);

                client.putParams("api_goods_info", json);
                string result = client.wdtOpenapi();
                //获取推送结果(没有部分失败情况)
                baseResponse resultModel = JsonConvert.DeserializeObject<baseResponse>(result);
                message = resultModel.message;
                if (resultModel.code == 0)
                {
                    return pushState.Success;
                }
                else
                {
                    Log.Error("推送货品失败：" + result);
                    return pushState.Fail;
                }

            }
            catch (Exception ex)
            {
                Log.Error("推送货品异常：" + ex.ToString());
                message = ex.ToString();
                return pushState.Error;
            }
        }

        /// <summary>
        /// 批量更新平台货品的推送状态
        /// </summary>
        /// <param name="goods">货品</param>
        /// <param name="state">推送状态 0未推送 1推送成功 2推送失败</param>
        /// <returns></returns>
        public static void BatchUpdatePushState(List<WdtGoods> goods, bool state)
        {
            var tempgoods_id = goods.Select(g => long.Parse(g.goods_id));

            _ProductService.BatchUpdatePushState(tempgoods_id, state);
        }

        /// <summary>
        /// 批量更新货品档案的推送状态
        /// </summary>
        /// <param name="goods">货品</param>
        /// <param name="state">推送状态 0未推送 1推送成功 2推送失败</param>
        /// <returns></returns>
        public static void BatchUpdatePushArchivesState(List<WdtGoods> goods, bool state)
        {
            var tempgoods_id = goods.Select(g => long.Parse(g.goods_id));

            _ProductService.BatchUpdatePushArchivesState(tempgoods_id, state);
        }

        /// <summary>
        /// 推送商品至平台货品旺店通
        /// </summary>
        /// <param name="productId">商品Id</param>
        /// <param name="skuId">规格Id</param>
        /// <returns></returns>
        public static bool PushById(long productId, string skuId = "")
        {
            List<WdtGoods> list = GetGoodsByProductId(productId, skuId);
            if (list == null || list.Count <= 0)
            {
                return false;
            }
            string message = "";
            pushState state = BatchPushGoods(list, out message);
            if (state == pushState.Success)
            {
                BatchUpdatePushState(list, true);
                return true;
            }
            else
            {
                BatchUpdatePushState(list, false);
                return false;
            }
        }

        #region 编辑商品删除规格时推送

        /// <summary>
        /// 获取需要推送到旺店通的货品信息
        /// </summary>
        /// <param name="productId">商品Id</param>
        /// <returns></returns>
        public static List<WdtGoods> GetGoodsByProductId(long productId, string skuId = "")
        {
            var currentpro = _ProductService.GetProduct(productId);
            var prosku = new List<SKUInfo>();
            if (skuId != "")
            {
                prosku.Add(_ProductService.GetSku(skuId));
            }
            else
            {
                prosku = _ProductService.GetSKUs(productId);
            }
            List<WdtGoods> list = new List<WdtGoods>();
            prosku.ForEach(sku =>
            {
                WdtGoods wdtgood = new WdtGoods();
                wdtgood.goods_id = sku.ProductId.ToString();
                wdtgood.goods_name = currentpro.ProductName;
                wdtgood.goods_no = currentpro.ProductCode;
                wdtgood.spec_id = sku.Id;
                wdtgood.spec_no = string.IsNullOrEmpty(sku.Sku) ? currentpro.ProductCode : sku.Sku;
                wdtgood.status = (int)currentpro.SaleStatus;//0代表删除，1代表上架，2代表下架
                wdtgood.spec_name = GetSpecname(sku);
                string img = currentpro.ImagePath;
                if (!string.IsNullOrEmpty(sku.ShowPic))
                    img = sku.ShowPic;
                wdtgood.pic_url = GetPicImg(img);
                wdtgood.price = sku.SalePrice;
                wdtgood.stock_num = sku.Stock;
                wdtgood.cid = currentpro.CategoryId.ToString();
                list.Add(wdtgood);

            });
            return list;
        }

        /// <summary>
        /// 修改旺店通货品的状态
        /// </summary>
        /// <param name="goods">货品</param>
        /// <param name="status">状态0删除 1在架 2下架</param>
        /// <param name="isAck">是否回写更新规格是否已推送状态值</param>
        /// <returns></returns>
        //public static bool PushByStatus(List<WdtGoods> goods, int status, bool isAck = false)
        //{
        //    if (goods == null || goods.Count <= 0)
        //    {
        //        return false;
        //    }
        //    foreach (WdtGoods item in goods)
        //    {
        //        item.status = status;  //0删除 1在架 2下架
        //    }
        //    string message = "";
        //    pushState state = BatchPushGoods(goods, out message);
        //    if (state == pushState.Success)
        //    {
        //        if (isAck)
        //        {
        //            BatchUpdateSkuPushState(goods,true);
        //        }
        //        return true;
        //    }
        //    else
        //    {
        //        if (isAck)
        //        {
        //            BatchUpdateSkuPushState(goods, false);
        //        }
        //        return false;
        //    }
        //}

        #endregion
        public static void PushGoodsByProductIds(IEnumerable<long> productIds)
        {
            var selfshopId = ShopApplication.GetSelfShop().Id;
            var products = _ProductService.GetProducts(productIds.ToList()).Where(s => s.ShopId == selfshopId).ToList();//只更新自营店的商品
            if (products.Count == 0)
                return;
            var skulist = _ProductService.GetSKUs(products.Select(p => p.Id));
            PushGoodByProdcuts(products, skulist);//平台货品推送
            PushArcheGoodsByProdcuts(products.ToList(), skulist);//货品档案推送
        }

        public static void PushArcheGoodsByProdcuts(List<ProductInfo> prolist, List<SKUInfo> skulist)
        {
            List<WdtGoodsArchives> list = new List<WdtGoodsArchives>();
            prolist.ForEach(pro =>
            {
                WdtGoodsArchives good = new WdtGoodsArchives();
                good.goods_id = pro.Id;
                good.goods_name = pro.ProductName;
                good.goods_type = 1;
                good.market_price = pro.MarketPrice;
                var currentskus = skulist.Where(sku => sku.ProductId == pro.Id).ToList();
                good.spec_list = GetWdtGoodsBySku(currentskus, pro);
                list.Add(good);
            });
        }

        public static void PushGoodByProdcuts(IEnumerable<ProductInfo> prolist, List<SKUInfo> skulist)
        {
            List<WdtGoods> goods = new List<WdtGoods>();
            skulist.ForEach(sku =>
            {
                var currentpro = prolist.Where(p => p.Id == sku.ProductId).FirstOrDefault();
                WdtGoods wdtgood = new WdtGoods();
                wdtgood.goods_id = sku.ProductId.ToString();
                wdtgood.goods_name = currentpro.ProductName;
                wdtgood.goods_no = currentpro.ProductCode;
                wdtgood.spec_id = sku.Id;
                wdtgood.spec_no = string.IsNullOrEmpty(sku.Sku) ? currentpro.ProductCode : sku.Sku;
                wdtgood.status = (int)currentpro.SaleStatus;//0代表删除，1代表上架，2代表下架
                wdtgood.spec_name = GetSpecname(sku);
                string img = currentpro.ImagePath;
                if (!string.IsNullOrEmpty(sku.ShowPic))
                    img = sku.ShowPic;
                wdtgood.pic_url = GetPicImg(img);
                wdtgood.price = sku.SalePrice;
                wdtgood.stock_num = sku.Stock;
                wdtgood.cid = currentpro.CategoryId.ToString();
                goods.Add(wdtgood);

            });
            if (goods == null || goods.Count <= 0)
            {
                return;
            }
            string message = "";
            pushState state = BatchPushGoods(goods, out message);
            if (state == pushState.Success)
            {
                BatchUpdatePushState(goods, true);
            }
            else
            {
                BatchUpdatePushState(goods, false);
                Log.Error("批量推送平台货品异常:" + message);
            }
        }


        /// <summary>
        /// 从旺店通同步商品库存，自动任务
        /// </summary>
        public static void SyncStockFromWdt()
        {
            WDTConfigModel setting = GetConfigModel();
            if (WdtParamIsValid(setting) && setting.OpenErpStock)
            {   //同步完成后，回写旺店通
                List<StockSyncAckInfo> stockSyncAcks = new List<StockSyncAckInfo>();
                List<SKUInfo> sotckSkuInfos = new List<SKUInfo>();
                List<int> sync_stock = new List<int>();
                List<SKUInfo> sKUInfos = _ProductService.SyncStockFromWdt(setting, out sync_stock);
                //获取更新库存后的实体（并且将不存的规格清除，然后更新）
                if (sKUInfos != null && sKUInfos.Count > 0)
                {
                    sotckSkuInfos = _StockService.GetSKUInfos(sKUInfos);
                    if (sotckSkuInfos.Count > 0)
                    {
                        _StockService.SetWDTSkuStock(sotckSkuInfos);
                    }

                    int index = 0;
                    foreach (SKUInfo sKUInfo in sKUInfos)
                    {
                        bool success = sotckSkuInfos.Exists(s => s.Id == sKUInfo.Id);
                        stockSyncAcks.Add(new StockSyncAckInfo()
                        {
                            code = success ? 0 : -1,
                            message = success ? "" : "规格信息不存在",
                            sync_stock = sync_stock[index]
                        });
                        index += 1;
                    }

                }
                else
                {
                    stockSyncAcks.Add(new StockSyncAckInfo()
                    {
                        code = 0,
                        message = "",
                        sync_stock = 0,
                    });
                }
                _ProductService.SyncStockBackWriteToWdt(setting, stockSyncAcks);
            }
        }

        #endregion
    }
}
