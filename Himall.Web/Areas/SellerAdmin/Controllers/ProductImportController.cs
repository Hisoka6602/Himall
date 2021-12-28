using Himall.Core;
using Himall.Core.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Text;
using Himall.Service;
using Himall.DTO.QueryModel;

using Himall.Web.Framework;
using LumenWorks.Framework.IO.Csv;
using Himall.CommonModel;
using Himall.Application;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.TaoBaoSDK;
using Himall.Entities;
using Himall.DTO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data;
using System.Web.Hosting;
using System.Web.Configuration;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class ProductImportController : BaseSellerController
    {
        private CategoryService _iCategoryService;
        private ShopService _ShopService;
        private BrandService _iBrandService;
        private ProductService _ProductService;
        public ProductImportController(
            CategoryService CategoryService,
            ShopService ShopService,
            BrandService BrandService, ProductService ProductService
            )
            : this()
        {
            _iBrandService = BrandService;
            _ShopService = ShopService;
            _iCategoryService = CategoryService;
            _ProductService = ProductService;
        }
        private long _shopid = 0;
        private long _userid = 0;
        public ProductImportController()
            : base()
        {
            //退出登录后，直接访问页面异常处理
            if (CurrentSellerManager != null)
            {
                _shopid = CurrentSellerManager.ShopId;
                _userid = CurrentSellerManager.Id;
            }
        }

        public ActionResult ImportManage()
        {
            string message = "";
            ViewBag.CanCreate = 1;
            ViewBag.CanNotCreateMessage = "";
            if (!CanCreate(out message))
            {
                ViewBag.CanCreate = 0;
                ViewBag.CanNotCreateMessage = message;
            }
            int lngCount = 0, lngTotal = 0;
            int intSuccess = 0;
            //从缓存取用户导入商品数量
            GetImportCountFromCache(out lngCount, out lngTotal);

            if (lngTotal == lngCount && lngTotal > 0)
            {
                intSuccess = 1;
            }
            var freightTemplates = ObjectContainer.Current.Resolve<FreightTemplateService>().GetShopFreightTemplate(CurrentSellerManager.ShopId);
            List<SelectListItem> freightList = new List<SelectListItem> { new SelectListItem
                {
                    Selected = false,
                    Text ="请选择运费模板...",
                    Value = "0"
                }};
            foreach (var item in freightTemplates)
            {
                freightList.Add(new SelectListItem
                {
                    Text = item.Name + "【" + item.ValuationMethod.ToDescription() + "】",
                    Value = item.Id.ToString()
                });
            }
            ViewBag.FreightTemplates = freightList;
            ViewBag.Count = lngCount;
            ViewBag.Total = lngTotal;
            ViewBag.Success = intSuccess;
            ViewBag.shopid = _shopid;
            ViewBag.userid = _userid;
            return View();
        }


        /// <summary>
        /// 抓取淘宝/天猫数据导入
        /// </summary>
        /// <returns></returns>
        public ActionResult SpiderManage()
        {
            int lngCount = 0, lngTotal = 0;
            int intSuccess = 0;
            //从缓存取用户导入商品数量
            GetImportCountFromCache(out lngCount, out lngTotal);

            if (lngTotal == lngCount && lngTotal > 0)
            {
                intSuccess = 1;
            }
            var freightTemplates = ObjectContainer.Current.Resolve<FreightTemplateService>().GetShopFreightTemplate(CurrentSellerManager.ShopId);
            List<SelectListItem> freightList = new List<SelectListItem> { new SelectListItem
                {
                    Selected = false,
                    Text ="请选择运费模板...",
                    Value = "0"
                }};
            foreach (var item in freightTemplates)
            {
                freightList.Add(new SelectListItem
                {
                    Text = item.Name + "【" + item.ValuationMethod.ToDescription() + "】",
                    Value = item.Id.ToString()
                });
            }
            ViewBag.FreightTemplates = freightList;
            ViewBag.Count = lngCount;
            ViewBag.Total = lngTotal;
            ViewBag.Success = intSuccess;
            ViewBag.shopid = _shopid;
            ViewBag.userid = _userid;
            return View();
        }

        public ActionResult SpiderSuccess(string guid)
        {
            SpiderProductResult resultInfo = new Models.SpiderProductResult();
            var result = Cache.Get<object>(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(CurrentShop.Id + guid));
            if (result != null)
            {
                Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(CurrentShop.Id + guid));
                resultInfo = result as SpiderProductResult;
            }
            return View(resultInfo);
        }
        private bool CanCreate(out string message, int productCount = 0)
        {
            if (ObjectContainer.Current.Resolve<ShopService>().GetShopSurplusSpace(CurrentSellerManager.ShopId) <= 0)
            {
                message = "存储图片空间不足,不能发布商品!";
                return false;
            }

            var grade = ObjectContainer.Current.Resolve<ShopService>().GetShopGrade(CurrentShop.GradeId);
            if (grade != null)
            {
                int count = _ProductService.GetShopAllProducts(CurrentSellerManager.ShopId);
                if (productCount > 0)
                {
                    count += productCount;
                }
                if (count >= grade.ProductLimit)
                {
                    message = "此店铺等级最多只能发布" + grade.ProductLimit + "件商品";
                    return false;
                }
            }
            message = "";
            return true;
        }

        [HttpPost]
        public JsonResult SaveProducts(SpiderProductModel model)
        {
            string message = "";
            if (!CanCreate(out message))
                return Json(new { success = false, msg = message });

            DateTime currentTime = DateTime.Now;
            var guid = Guid.NewGuid().ToString().Replace("-", "");
            if (!string.IsNullOrWhiteSpace(model.Guid))
                guid = model.Guid;

            string msg = CheckException(guid, model);//检查必填项以及正确性 
            if (!string.IsNullOrWhiteSpace(msg))
                return Json(new { success = false, msg = msg, guid = guid });

            if (!CanCreate(out message, model.GrabUrl == null ? 0 : model.GrabUrl.Count()))
                return Json(new { success = false, msg = message });

            string ip = Himall.Core.Helper.WebHelper.GetIP();
            List<long> ids = new List<long>();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //初始进度
                    if (ComfirmCancle(currentTime, guid)) return;
                    Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid), "@");

                    //定义商品创建实体
                    SpiderProductResult result = new Models.SpiderProductResult()
                    {
                        FailDataModel = new List<FailDataModel>()
                    };
                    ProductCreateModel createModel = null;

                    //获取商品详情内容
                    var list = new TaoBaoSpider().GetProductDetailsByUrl(model.GrabUrl);

                    //图片宕下后存储到临时目录
                    string imageRealtivePath = "/temp/" + Guid.NewGuid().ToString(), grabUrl = string.Empty;
                    int spiderNum = model.GrabUrl.Count, successNum = 0, failNum = 0, index = 0;//总共要处理的链接数量
                    decimal percenet = 0, singlePercenet = 0, infoPercenet = 0;//定义进度百分比
                    if (spiderNum > 0)
                        singlePercenet = decimal.Round(100 / spiderNum, 2);//单个URL百分均比

                    infoPercenet = decimal.Round(singlePercenet / 4, 2);//单个内信息百分比:分为基本信息+详情+sku处理+入库
                    foreach (var item in list)
                    {
                        //当前处理项
                        index = list.IndexOf(item) + 1;
                        grabUrl = item.GrabUrl;

                        //忽略错误的Url
                        if (string.IsNullOrWhiteSpace(item.ProductName))
                        {
                            failNum++;
                            percenet += singlePercenet;
                            result.FailDataModel.Add(new Models.FailDataModel()
                            {
                                GrabUrl = item.GrabUrl,
                                Remark = "链接无法解析"
                            });

                            if (ComfirmCancle(currentTime, guid)) return;
                            Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid), percenet + "@" + index);
                            continue;
                        }

                        //2、创建商品实体并赋值分类、商品编码、品牌等信息
                        createModel = SetCreateModel(item, model);

                        var categoryInfo = CategoryApplication.GetCategory(model.SmallCategoryId);
                        if (categoryInfo != null)
                        {
                            createModel.TypeId = categoryInfo.TypeId;//根据三级分类反查所属类型
                        }

                        //3、设置基本信息[无规格库存、商品名称、价格、主图、是否有规格]
                        SetProductBasicInfo(createModel, imageRealtivePath, item);

                        if (!string.IsNullOrWhiteSpace(createModel.ProductName) || createModel.Pics.Count() > 0)
                        {
                            percenet = percenet + infoPercenet;
                            if (ComfirmCancle(currentTime, guid)) return;
                            Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid), percenet + "@" + index);
                        }

                        //4、设置商品详情内容
                        if (!string.IsNullOrWhiteSpace(item.Description))
                            createModel.Description = SetProductDesc(item.Description, imageRealtivePath);

                        if (createModel.Description != null && !string.IsNullOrWhiteSpace(createModel.Description.Description))
                        {
                            percenet = percenet + infoPercenet;
                            if (ComfirmCancle(currentTime, guid)) return;
                            Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid), percenet + "@" + index);
                        }

                        TypeInfo typeInfo = TypeApplication.GetType(createModel.TypeId);
                        #region 读取规格，规格值，规格值ID
                        List<ProductImportSKU> listSku = new List<ProductImportSKU>();
                        if (typeInfo != null && item.SkuList != null && item.SkuList.Count > 0)
                        {
                            List<SpecificationValueInfo> specifications = ObjectContainer.Current.Resolve<SpecificationService>().GetSpecification(model.SmallCategoryId, this.CurrentShop.Id);
                            if (typeInfo.IsSupportColor)
                            {
                                if (!string.IsNullOrWhiteSpace(item.ColorAlias))
                                {
                                    createModel.ColorAlias = item.ColorAlias;
                                    var spitems = specifications.Where(a => a.Specification == SpecificationType.Color).ToList();
                                    var colors = item.SkuList.Where(a => !string.IsNullOrWhiteSpace(a.Color)).Select(a => a.Color).Distinct().Take(spitems.Count).ToList();
                                    for (int j = 0; j < colors.Count; j++)
                                    {
                                        listSku.Add(new ProductImportSKU()
                                        {
                                            Type = 1,
                                            PropertyName = item.ColorAlias,
                                            PropertyValueName = colors[j],
                                            ValueId = spitems[j].Id
                                        });
                                    }
                                }
                            }
                            if (typeInfo.IsSupportSize)
                            {
                                if (!string.IsNullOrWhiteSpace(item.SizeAlias))
                                {
                                    createModel.SizeAlias = item.SizeAlias;
                                    var spitems = specifications.Where(a => a.Specification == SpecificationType.Size).ToList();
                                    var sizes = item.SkuList.Where(a => !string.IsNullOrWhiteSpace(a.Size)).Select(a => a.Size).Distinct().Take(spitems.Count).ToList();
                                    for (int j = 0; j < sizes.Count; j++)
                                    {
                                        listSku.Add(new ProductImportSKU()
                                        {
                                            Type = 2,
                                            PropertyName = item.SizeAlias,
                                            PropertyValueName = sizes[j],
                                            ValueId = spitems[j].Id
                                        });
                                    }
                                }
                            }
                            if (typeInfo.IsSupportVersion)
                            {
                                if (!string.IsNullOrWhiteSpace(item.VersionAlias))
                                {
                                    createModel.VersionAlias = item.VersionAlias;
                                    var spitems = specifications.Where(a => a.Specification == SpecificationType.Version).ToList();
                                    var versions = item.SkuList.Where(a => !string.IsNullOrWhiteSpace(a.Version)).Select(a => a.Version).Distinct().Take(spitems.Count).ToList();
                                    for (int j = 0; j < versions.Count; j++)
                                    {
                                        listSku.Add(new ProductImportSKU()
                                        {
                                            Type = 3,
                                            PropertyName = item.VersionAlias,
                                            PropertyValueName = versions[j],
                                            ValueId = spitems[j].Id
                                        });
                                    }
                                }
                            }

                            //商家修改的规格值
                            List<SpecificationValue> updateSpecs = listSku.Select(ls => new SpecificationValue
                            {
                                Id = ls.ValueId,
                                Specification = ls.Type == 1 ? SpecificationType.Color : (ls.Type == 2 ? SpecificationType.Size : SpecificationType.Version),
                                TypeId = createModel.TypeId,
                                Value = ls.PropertyValueName
                            }).ToList();

                            createModel.UpdateSpecs = updateSpecs.ToArray();
                            createModel.HasSKU = item.HasSKU && listSku.Count > 0;

                            //根据listSku(根据系统是否支持相应规格生成的各规格值)，反向生成规格列表。不能直接用接口返回的SkuList
                            List<SKUEx> skuExs = new List<SKUEx>();//组装SKUEx
                            if (createModel.HasSKU)
                            {
                                var colorList = listSku.Where(a => a.Type == 1).ToList();
                                var sizeList = listSku.Where(a => a.Type == 2).ToList();
                                var versionList = listSku.Where(a => a.Type == 3).ToList();
                                var hasColor = colorList.Count > 0;
                                var hasSize = sizeList.Count > 0;
                                var hasVersion = versionList.Count > 0;

                                if (hasColor && hasSize && hasVersion)
                                {
                                    foreach (var color in colorList)
                                    {
                                        foreach (var size in sizeList)
                                        {
                                            foreach (var version in versionList)
                                            {
                                                skuExs.Add(GetSkuInfo(item.SkuList, color.PropertyValueName, color.ValueId, size.PropertyValueName, size.ValueId, version.PropertyValueName, version.ValueId));
                                            }
                                        }
                                    }
                                }
                                else if (hasColor && hasSize)
                                {
                                    foreach (var color in colorList)
                                    {
                                        foreach (var size in sizeList)
                                        {
                                            skuExs.Add(GetSkuInfo(item.SkuList, color.PropertyValueName, color.ValueId, size.PropertyValueName, size.ValueId));
                                        }
                                    }
                                }
                                else if (hasColor && hasVersion)
                                {
                                    foreach (var color in colorList)
                                    {
                                        foreach (var version in versionList)
                                        {
                                            skuExs.Add(GetSkuInfo(item.SkuList, color.PropertyValueName, color.ValueId, version: version.PropertyValueName, versionId: version.ValueId));
                                        }
                                    }
                                }
                                else if (hasSize && hasVersion)
                                {
                                    foreach (var size in sizeList)
                                    {
                                        foreach (var version in versionList)
                                        {
                                            skuExs.Add(GetSkuInfo(item.SkuList, size: size.PropertyValueName, sizeId: size.ValueId, version: version.PropertyValueName, versionId: version.ValueId));
                                        }
                                    }
                                }
                                else if (hasColor)
                                {
                                    foreach (var color in colorList)
                                    {
                                        skuExs.Add(GetSkuInfo(item.SkuList, color.PropertyValueName, color.ValueId));
                                    }
                                }
                                else if (hasSize)
                                {
                                    foreach (var size in sizeList)
                                    {
                                        skuExs.Add(GetSkuInfo(item.SkuList, size: size.PropertyValueName, sizeId: size.ValueId));
                                    }
                                }
                                else if (hasVersion)
                                {
                                    foreach (var version in versionList)
                                    {
                                        skuExs.Add(GetSkuInfo(item.SkuList, version: version.PropertyValueName, versionId: version.ValueId));
                                    }
                                }
                            }
                            //处理规格图片
                            if (skuExs != null && skuExs.Count > 0)
                            {
                                foreach (var sku in skuExs)
                                {
                                    if (string.IsNullOrWhiteSpace(sku.ShowPic))
                                        continue;

                                    sku.ShowPic = HtmlContentHelper.TransferNetworkImageToLocal(sku.ShowPic, "/", imageRealtivePath, Core.HimallIO.GetImagePath(imageRealtivePath) + "/");
                                }
                            }
                            createModel.SKUExs = skuExs.ToArray();
                            percenet = percenet + infoPercenet;
                            if (ComfirmCancle(currentTime, guid)) return;
                            Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid), percenet + "@" + index);
                        }
                        #endregion
                        var product = AutoMapper.Mapper.DynamicMap<DTO.Product.Product>(createModel);
                        ProductLadderPrice[] prices = new ProductLadderPrice[] { };
                        ProductAttribute[] attributes = new ProductAttribute[] { };
                        var sellerSpecs = createModel.GetSellerSpecification(this.CurrentSellerManager.ShopId, product.TypeId);
                        if (createModel.SKUExs != null && createModel.SKUExs.Count() == 0 || (createModel.SKUExs == null))
                        {
                            List<SKUEx> exList = new List<SKUEx>();
                            SKUEx ex = new SKUEx()
                            {
                                Sku = createModel.ProductCode,
                                Stock = createModel.Quantity.HasValue ? createModel.Quantity.Value : 0,
                                SalePrice = createModel.MinSalePrice
                            };
                            ex.Id = ex.CreateId(null);
                            exList.Add(ex);
                            createModel.SKUExs = exList.ToArray();
                        }
                        var skus = new SKUEx[] { };
                        if (createModel.SKUExs != null)
                        {
                            skus = createModel.SKUExs.ToArray();
                        }

                        if (ComfirmCancle(currentTime, guid)) return;

                        bool success = false;
                        var productDto = Himall.Application.ProductManagerApplication.AddProduct(this.CurrentShop.Id, product,
                           createModel.Pics, skus, createModel.Description, attributes, createModel.GoodsCategory, sellerSpecs, prices);
                        success = true;
                        ids.Add(productDto.Id);
                        Cache.Insert(CacheKeyCollection.CACHE_CACHEIMPORT(CurrentShop.Id + guid), ids);
                        //添加商家操作日志
                        LogInfo logInfo = new Entities.LogInfo
                        {
                            Date = DateTime.Now,
                            Description = string.Format("商家导入淘宝/天猫商品数据, [{0}]", success ? "成功" : "失败"),
                            PageUrl = createModel.GrabUrl,
                            UserName = CurrentSellerManager.UserName,
                            ShopId = CurrentSellerManager.ShopId,
                            IPAddress = ip
                        };
                        OperationLogApplication.AddSellerOperationLog(logInfo);
                        successNum++;
                        if (list.IndexOf(item) + 1 == list.Count)//如果导入完最后一个，则100%
                        {
                            percenet = 100;
                        }
                        Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid), percenet + "@" + index);
                    }

                    result.SuccessNum = successNum;
                    result.FailNum = failNum;
                    Cache.Insert(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(CurrentShop.Id + guid), result);//处理后将结果写入缓存
                }
                catch (Exception e)
                {
                    Core.Log.Error("导入淘宝/天猫商品异常", e);
                }
            });
            return Json(new { success = true, total = model.GrabUrl.Count, guid = guid });
        }

        private SKUEx GetSkuInfo(List<SkuModel> skuList, string color = "", long colorId = 0, string size = "", long sizeId = 0, string version = "", long versionId = 0)
        {
            var skuInfo = new SKUEx();
            if (colorId > 0)
            {
                skuInfo.Color = color;
                skuInfo.ColorId = colorId;
            }
            if (sizeId > 0)
            {
                skuInfo.Size = size;
                skuInfo.SizeId = sizeId;
            }
            if (versionId > 0)
            {
                skuInfo.Version = version;
                skuInfo.VersionId = versionId;
            }
            Himall.TaoBaoSDK.SkuModel current = new Himall.TaoBaoSDK.SkuModel();
            if (colorId > 0 && sizeId > 0 && versionId > 0)
                current = skuList.Where(a => a.Color == color && a.Size == size && a.Version == version).FirstOrDefault();
            else if (colorId > 0 && versionId > 0)
                current = skuList.Where(a => a.Color == color && a.Version == version).FirstOrDefault();
            else if (colorId > 0 && sizeId > 0)
                current = skuList.Where(a => a.Color == color && a.Size == size).FirstOrDefault();
            else if (versionId > 0 && sizeId > 0)
                current = skuList.Where(a => a.Size == size && a.Version == version).FirstOrDefault();
            else if (colorId > 0)
                current = skuList.Where(a => a.Color == color).FirstOrDefault();
            else if (sizeId > 0)
                current = skuList.Where(a => a.Size == size).FirstOrDefault();
            else if (versionId > 0)
                current = skuList.Where(a => a.Version == version).FirstOrDefault();

            if (current != null)
            {
                skuInfo.CostPrice = current.CostPrice;
                skuInfo.SalePrice = current.SalePrice;
                skuInfo.Sku = current.Sku;
                if (string.IsNullOrWhiteSpace(skuInfo.Sku))
                    skuInfo.Sku = GenerateRandomCode(8);
                skuInfo.Stock = current.Stock;
                if (string.IsNullOrWhiteSpace(skuInfo.ShowPic))
                    skuInfo.ShowPic = current.ShowPic;
            }
            skuInfo.Id = skuInfo.CreateId(null);
            return skuInfo;
        }

        private ProductDescription SetProductDesc(string productDesc, string imageRealtivePath)
        {
            var description = new DTO.ProductDescription();

            if (string.IsNullOrWhiteSpace(productDesc))
                return description;

            productDesc = productDesc.Replace("\\", "");
            Regex regex = new Regex(@"<table [\s]*>[\s]*(<br[\s]?/>)?[\s]*</table>", RegexOptions.IgnoreCase);
            productDesc = regex.Replace(productDesc, "");

            regex = new Regex("width\\s*=\\s*\\S+ height\\s*=\\s*\\S+");
            productDesc = regex.Replace(productDesc, "");

            productDesc = Regex.Replace(productDesc, "<a[^>]+>", "");
            productDesc = Regex.Replace(productDesc, "<area[^>]+>", "");

            var images = GetHtmlImageUrlList(productDesc).Distinct().ToList();
            foreach (var img in images)
            {
                if (!img.StartsWith("http"))
                    productDesc = productDesc.Replace(img, "http:" + img);
            }
            productDesc = Core.Helper.HtmlContentHelper.TransferToLocalImage(productDesc, "/", imageRealtivePath, Core.HimallIO.GetImagePath(imageRealtivePath) + "/", true);//处理详情图片

            description.Description = productDesc;
            description.MobileDescription = productDesc;
            return description;
        }

        IEnumerable<string> GetHtmlImageUrlList(string htmlText)
        {
            // 定义正则表达式用来匹配 img 标签   
            Regex regImg = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);

            // 搜索匹配的字符串   
            MatchCollection matches = regImg.Matches(htmlText);
            int i = 0;
            string[] sUrlList = new string[matches.Count];

            // 取得匹配项列表   
            foreach (Match match in matches)
                sUrlList[i++] = match.Groups["imgUrl"].Value;

            return sUrlList;
        }

        private void SetProductBasicInfo(ProductCreateModel createModel, string imageRealtivePath, ProductResultModel item)
        {
            //处理库存总数、商品名称、最小销售价、商品主图、是否有规格
            createModel.Quantity = item.Quantity;
            createModel.ProductName = item.ProductName;
            createModel.MinSalePrice = item.MinSalePrice;
            if (item.Images != null && item.Images.Count > 0)
            {
                for (int i = 0; i < item.Images.Count; i++)
                {
                    item.Images[i] = item.Images[i].Replace("\"", "").Trim();
                    item.Images[i] = "http://" + item.Images[i].Replace("//", "").Replace("http://", "").Replace("https://", "").Replace("httpsimg.alicdn.com", "img.alicdn.com");//去掉自带的前缀和域名
                    item.Images[i] = HtmlContentHelper.TransferNetworkImageToLocal(item.Images[i], "/", imageRealtivePath, Core.HimallIO.GetImagePath(imageRealtivePath) + "/");
                }
                createModel.Pics = item.Images.ToArray();
            }
            createModel.HasSKU = item.HasSKU;
        }

        private ProductCreateModel SetCreateModel(ProductResultModel item, SpiderProductModel model)
        {
            ProductCreateModel createModel = new ProductCreateModel();
            createModel.GrabUrl = item.GrabUrl;
            createModel.SaleStatus = Entities.ProductInfo.ProductSaleStatus.InDraft;//采集过来的商品状态为草稿箱
            createModel.CategoryId = model.SmallCategoryId;
            createModel.CategoryPath = string.Format("{0}|{1}|{2}", model.BigCategoryId, model.MidCategoryId, model.SmallCategoryId);
            createModel.FreightTemplateId = model.FreightTemplateId;
            createModel.GoodsCategory = new long[] { model.SellerMidcategoryId > 0 ? model.SellerMidcategoryId : model.SellerBigCategoryId };//这里可传大类也可传小类，优先小类
            createModel.AuditStatus = ProductInfo.ProductAuditStatus.UnAudit;
            createModel.BrandId = model.BrandId;
            createModel.MeasureUnit = "件";
            createModel.ProductCode = GenerateRandomCode(8);
            createModel.ShortDescription = item.ShortDescription;//广告词
            return createModel;
        }

        private string GenerateRandomCode(int length)
        {
            var result = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                var r = new Random(Guid.NewGuid().GetHashCode());
                result.Append(r.Next(0, 10));
            }
            return result.ToString();
        }

        private string CheckException(string guid, SpiderProductModel model)
        {
            var percent = Cache.Get<object>(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(this.CurrentShop.Id + guid));
            if (percent != null && Core.Helper.TypeHelper.StringToInt(percent.ToString().Split('@')[0]) != 100)
                return "上一批还没导入完成";

            if (model.SmallCategoryId <= 0)
                return "请选择完整平台分类";

            if (model.SellerBigCategoryId <= 0)
                return "请选择商家分类";

            if (model.FreightTemplateId <= 0)
                return "请选择运费模板";

            if (model.GrabUrl != null && model.GrabUrl.Exists(a => string.IsNullOrWhiteSpace(a)))
                return "抓取地址中含有空链接";

            return "";
        }

        private bool ComfirmCancle(DateTime current, string guid)
        {
            //如果确认取消
            var cancleImport = Cache.Get<object>(CacheKeyCollection.CACHE_IMPORTCANCLE(CurrentShop.Id + guid));
            if (cancleImport != null)
            {
                var flag = int.Parse(cancleImport.ToString().Split('=')[0]);
                var time = DateTime.Parse(cancleImport.ToString().Split('=')[1]);
                if (flag == 1 && time >= current)
                {
                    Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(this.CurrentShop.Id + guid));//当确认离开后，清除导入数据和百分比
                    Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(this.CurrentShop.Id + guid));
                    Cache.Remove(CacheKeyCollection.CACHE_CACHEIMPORT(this.CurrentShop.Id + guid));
                    Cache.Remove(CacheKeyCollection.CACHE_IMPORTCANCLE(this.CurrentShop.Id + guid));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 取消导入商品
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CancleImport(string guid)
        {
            Cache.Insert(CacheKeyCollection.CACHE_IMPORTCANCLE(CurrentShop.Id + guid), 1 + "=" + DateTime.Now);
            Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(this.CurrentShop.Id + guid));//当确认离开后，清除导入数据和百分比
            Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(this.CurrentShop.Id + guid));
            Cache.Remove(CacheKeyCollection.CACHE_CACHEIMPORT(this.CurrentShop.Id + guid));
            //Cache.Remove(CacheKeyCollection.CACHE_IMPORTCANCLE(this.CurrentShop.Id));
            //cancelTokenSource.Cancel();
            return Json(new
            {
                success = true
            });
        }

        [HttpPost]
        public JsonResult RefreshImportProduct(string guid)
        {
            var importCache = Cache.Get<object>(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid));

            decimal value = 0; int index = 0;
            if (importCache != null)
            {
                var percent = importCache.ToString().Split('@');
                if (percent.Length == 2)
                {
                    value = Himall.Core.Helper.TypeHelper.StringToDecimal(percent[0]);
                    index = Himall.Core.Helper.TypeHelper.StringToInt(percent[1]);
                }
            }

            if (value == 100)
            {
                //var result = Cache.Get<object>(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(CurrentShop.Id));
                Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTPERCENT(CurrentShop.Id + guid));
                Cache.Remove(CacheKeyCollection.CACHE_CACHEIMPORT(CurrentShop.Id + guid));
                //Cache.Remove(CacheKeyCollection.CACHE_IMPORTPRODUCTRESULT(CurrentShop.Id));
                return Json(new
                {
                    success = true,
                    value = value,
                    index = index
                    //result = result as SpiderProductResult
                });
            }
            return Json(new
            {
                success = false,
                value = value,
                index = index
            });
        }

        /// <summary>
        /// 规格名称检查
        /// </summary>
        /// <param name="str"></param>
        /// <param name="excludeWordList"></param>
        /// <returns></returns>
        static bool CheckSkuName(string str, ICollection<string> excludeWordList = null)
        {
            if (str.Trim().Length <= 0 || excludeWordList == null || excludeWordList.Count <= 0)
            {
                return false;
            }
            return excludeWordList.Any(s => str.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 取平台类目
        /// </summary>
        /// lly 2015-02-06
        /// <param name="key"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        [UnAuthorize]
        [HttpPost]
        public JsonResult GetPlatFormCategory(long? key = null, int? level = -1)
        {
            if (level == -1)
                key = 0;

            if (key.HasValue)
            {
                var shopcategories = ObjectContainer.Current.Resolve<ShopCategoryService>().GetBusinessCategory(CurrentSellerManager.ShopId).Select(e => e.Id);
                var categories = _iCategoryService.GetCategoryByParentId(key.Value, false);
                categories = categories.Where(a => shopcategories.Contains(a.Id));

                var cateoriesPair = categories.Select(item => new KeyValuePair<long, string>(item.Id, item.Name));
                return Json(cateoriesPair);
            }
            else
                return Json(new object[] { });
        }
        /// <summary>
        /// 取店铺品牌
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        [UnAuthorize]
        [HttpPost]
        public JsonResult GetShopBrand(long categoryId)
        {
            var brands = _iBrandService.GetBrandsByCategoryIds(CurrentSellerManager.ShopId, categoryId);
            var brandsPair = brands.Select(item => new KeyValuePair<long, string>(item.Id, item.Name));
            return Json(brandsPair);
        }

        /// <summary>
        /// 取导入记录条数
        /// </summary>
        /// <returns></returns>
        public JsonResult GetImportCount()
        {
            int lngCount = 0, lngTotal = 0;
            int intSuccess = 0;
            //从缓存取用户导入商品数量
            GetImportCountFromCache(out lngCount, out lngTotal);

            if (lngTotal == lngCount && lngTotal > 0)
            {
                intSuccess = 1;
            }
            return Json(new
            {
                Count = lngCount,
                Total = lngTotal,
                Success = intSuccess
            }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 取正在进行导入的人数
        /// </summary>
        /// <returns></returns>
        public JsonResult GetImportOpCount()
        {
            //long lngCount = 0;
            //
            var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
            //if (opcount != 0)
            //{
            //    lngCount = string.IsNullOrEmpty(opcount.ToString()) ? 0 : long.Parse(opcount.ToString());
            //}

            return Json(new
            {
                Count = opcount
            }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 从缓存读取导入记录数
        /// </summary>
        /// <param name="count"></param>
        /// <param name="total"></param>
        private void GetImportCountFromCache(out int count, out int total)
        {
            count = Core.Cache.Get<int>(CacheKeyCollection.UserImportProductCount(_userid));
            total = Core.Cache.Get<int>(CacheKeyCollection.UserImportProductTotal(_userid));
            //count = objCount == null ? 0 : long.Parse(objCount.ToString());
            //total = objTotal == null ? 0 : long.Parse(objTotal.ToString());
            if (count == total && total > 0)
            {
                Core.Cache.Remove(CacheKeyCollection.UserImportProductCount(_userid));
                Core.Cache.Remove(CacheKeyCollection.UserImportProductTotal(_userid));
            }
        }

        #region 本地宝导入操作



        public ActionResult HimallImportProduct()
        {
            string message = "";
            ViewBag.CanCreate = 1;
            ViewBag.CanNotCreateMessage = "";
            if (!CanCreate(out message))
            {
                ViewBag.CanCreate = 0;
                ViewBag.CanNotCreateMessage = message;
            }
            int lngCount = 0, lngTotal = 0;
            int intSuccess = 0;
            //从缓存取用户导入商品数量
            GetImportCountFromCache(out lngCount, out lngTotal);

            if (lngTotal == lngCount && lngTotal > 0)
            {
                intSuccess = 1;
            }
            ViewBag.Count = lngCount;
            ViewBag.Total = lngTotal;
            ViewBag.Success = intSuccess;
            ViewBag.shopid = _shopid;
            ViewBag.userid = _userid;
            return View();
        }
        #endregion
    }

    public class AsyncProductImportController : BaseAsyncController
    {
        private CategoryService _iCategoryService;
        private ProductService _ProductService;
        private SearchProductService _iSearchProductService;
        public AsyncProductImportController(CategoryService CategoryService, ProductService ProductService,
            SearchProductService SearchProductService)
        {
            _iCategoryService = CategoryService;
            _ProductService = ProductService;
            _iSearchProductService = SearchProductService;
        }

        [UnAuthorize]
        [HttpGet]
        public JsonResult ImportProductJson(long paraCategory, long paraShopCategory, long? paraBrand, int paraSaleStatus, long _shopid, long _userid, long freightId, string file)
        {
            /*
             产品ID/主图
             产品ID/Details/明细图片
            */
            string filePath = Server.MapPath("/temp/" + file);
            string imgpath1 = string.Format(@"/Storage/Shop/{0}/Products", _shopid);
            string imgpath2 = Server.MapPath(imgpath1);
            long brand = 0;
            if (paraBrand.HasValue)
                brand = paraBrand.Value;
            JsonResult result = new JsonResult();
            if (System.IO.File.Exists(filePath))
            {
                ZipHelper.ZipInfo zipinfo = ZipHelper.UnZipFile(filePath);
                if (zipinfo.Success)
                {
                    try
                    {
                        int intCnt = ProcessProduct(paraCategory, paraShopCategory, brand, paraSaleStatus, _shopid, _userid, freightId, zipinfo.UnZipPath, imgpath1, imgpath2);
                        if (intCnt > 0)
                        {
                            result = Json(new { success = true, message = "成功导入【" + intCnt.ToString() + "】件商品" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            result = Json(new { success = false, message = "导入【0】件商品，请检查数据包，是否是重复导入" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("导入商品异常：" + ex.Message);
                        Core.Cache.Remove(CacheKeyCollection.UserImportProductCount(_userid));
                        Core.Cache.Remove(CacheKeyCollection.UserImportProductTotal(_userid));
                        result = Json(new { success = false, message = "导入商品异常:" + ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    Core.Log.Error("解压文件异常：" + zipinfo.InfoMessage);
                    result = Json(new { success = false, message = "解压出现异常,请检查压缩文件格式" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                result = Json(new { success = false, message = "上传文件不存在" }, JsonRequestBehavior.AllowGet);
            }
            var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
            if (opcount != 0)
            {
                Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount - 1);
            }
            return result;
        }
        /// <summary>
        /// 异步导入商品
        /// </summary>
        /// <param name="paraCategory"></param>
        /// <param name="paraShopCategory"></param>
        /// <param name="paraBrand"></param>
        /// <param name="paraSaleStatus"></param>
        /// <param name="_shopid"></param>
        /// <param name="_userid"></param>
        /// <param name="file">压缩文件名</param>
        /// <returns></returns>
        public void ImportProductAsync(long paraCategory, long paraShopCategory, long? paraBrand, int paraSaleStatus, long _shopid, long _userid, long freightId, string file)
        {
            /*
             产品ID/主图
             产品ID/Details/明细图片
            */
            AsyncManager.OutstandingOperations.Increment();
            Task.Factory.StartNew(() =>
            {
                string filePath = Server.MapPath("/temp/" + file);
                string imgpath1 = string.Format(@"/Storage/Shop/{0}/Products", _shopid);
                string imgpath2 = Server.MapPath(imgpath1);
                long brand = 0;
                if (paraBrand.HasValue)
                    brand = paraBrand.Value;
                JsonResult result = new JsonResult();
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        ZipHelper.ZipInfo zipinfo = ZipHelper.UnZipFile(filePath);
                        if (zipinfo.Success)
                        {

                            int intCnt = ProcessProduct(paraCategory, paraShopCategory, brand, paraSaleStatus, _shopid, _userid, freightId, zipinfo.UnZipPath, imgpath1, imgpath2);
                            if (intCnt > 0)
                            {
                                AsyncManager.Parameters["success"] = true;
                                AsyncManager.Parameters["message"] = "成功导入【" + intCnt.ToString() + "】件商品";
                            }
                            else
                            {
                                Core.Cache.Remove(CacheKeyCollection.UserImportProductCount(_userid));
                                Core.Cache.Remove(CacheKeyCollection.UserImportProductTotal(_userid));
                                AsyncManager.Parameters["success"] = false;
                                AsyncManager.Parameters["message"] = "导入【0】件商品，请检查数据包格式，或是否重复导入";
                            }

                        }
                        else
                        {
                            Core.Log.Error("解压文件异常：" + zipinfo.InfoMessage);
                            AsyncManager.Parameters["success"] = false;
                            AsyncManager.Parameters["message"] = "解压出现异常,请检查压缩文件格式";
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("导入商品异常：" + ex.Message);
                        Core.Cache.Remove(CacheKeyCollection.UserImportProductCount(_userid));
                        Core.Cache.Remove(CacheKeyCollection.UserImportProductTotal(_userid));
                        AsyncManager.Parameters["success"] = false;
                        AsyncManager.Parameters["message"] = "导入商品异常:" + ex.Message;
                    }
                }
                else
                {
                    AsyncManager.Parameters["success"] = false;
                    AsyncManager.Parameters["message"] = "上传文件不存在";
                }
                AsyncManager.OutstandingOperations.Decrement();
                var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
                if (opcount != 0)
                {
                    Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount - 1);
                }
            });
        }
        public JsonResult ImportProductCompleted(bool success, string message)
        {
            return Json(new { success = success, message = message }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 商品明细处理
        /// </summary>
        /// <param name="paraCategory"></param>
        /// <param name="paraShopCategory"></param>
        /// <param name="paraBrand"></param>
        /// <param name="paraSaleStatus"></param>
        /// <param name="_shopid"></param>
        /// <param name="_userid"></param>
        /// <param name="mainpath">压缩文件的路径</param>
        /// <param name="imgpath1">虚拟相对路径</param>
        /// <param name="imgpath2">绝对路径(mappath)包含</param>
        /// <returns></returns>
        private int ProcessProduct(long paraCategory, long paraShopCategory, long paraBrand, int paraSaleStatus, long _shopid, long _userid, long freightId, string mainpath, string imgpath1, string imgpath2)
        {
            int result = 0;
            string strPath = mainpath;
            var category = _iCategoryService.GetCategory(paraCategory);

            if (Directory.Exists(strPath))
            {
                Dictionary<long, string> proimgs = new Dictionary<long, string>();//待异步处理主图的图片
                string[] csvfiles = Directory.GetFiles(strPath, "*.csv", SearchOption.AllDirectories);
                string line = string.Empty;
                List<string> cells = new List<string>();
                for (int i = 0; i < csvfiles.Length; i++)
                {
                    StreamReader reader = new StreamReader(csvfiles[i], Encoding.Unicode);
                    string str2 = reader.ReadToEnd();
                    reader.Close();
                    str2 = str2.Substring(str2.IndexOf('\n') + 1);
                    str2 = str2.Substring(str2.IndexOf('\n') + 1);
                    StreamWriter writer = new StreamWriter(csvfiles[i], false, Encoding.Unicode);
                    writer.Write(str2);
                    writer.Close();
                    using (CsvReader reader2 = new CsvReader(new StreamReader(csvfiles[i], Encoding.UTF8), true, '\t'))
                    {
                        int num = 0;
                        while (reader2.ReadNextRecord())
                        {
                            num++;
                            int columnCount = reader2.FieldCount;
                            //string[] heads = reader2.GetFieldHeaders();
                            string strProductName = reader2["宝贝名称"].Replace("\"", "");
                            ProductQuery productQuery = new ProductQuery();
                            productQuery.CategoryId = category.Id;
                            productQuery.ShopId = _shopid;
                            productQuery.KeyWords = strProductName;
                            var iProcudt = _ProductService;

                            var proCount = ProductManagerApplication.GetProductCount(productQuery);
                            if (proCount > 0)
                            {//当前店铺、分类已经存在相同编码的商品
                                result++;
                                Core.Log.Debug(strProductName + " : 商品不能重复导入");
                                Core.Cache.Insert<int>(CacheKeyCollection.UserImportProductCount(_userid), result);
                                continue;
                            }
                            long pid = iProcudt.GetNextProductId();
                            long lngStock = 0;
                            decimal price = decimal.Parse(reader2["宝贝价格"] == string.Empty ? "0" : reader2["宝贝价格"]);
                            var product = new ProductInfo()
                            {
                                Id = pid,
                                TypeId = category.TypeId,
                                AddedDate = DateTime.Now,
                                BrandId = paraBrand,
                                CategoryId = category.Id,
                                CategoryPath = category.Path,
                                MarketPrice = price,
                                ShortDescription = string.Empty,
                                ProductCode = reader2["商家编码"].Replace("\"", ""),
                                ImagePath = "",
                                DisplaySequence = 0,//默认的序号都为0
                                ProductName = strProductName,
                                MinSalePrice = price,
                                ShopId = _shopid,
                                HasSKU = false,//判断是否有多规格才能赋值
                                SaleStatus = paraSaleStatus == 1 ? Entities.ProductInfo.ProductSaleStatus.OnSale : Entities.ProductInfo.ProductSaleStatus.InStock,
                                AuditStatus = Entities.ProductInfo.ProductAuditStatus.WaitForAuditing,
                                FreightTemplateId = freightId,
                                MeasureUnit = "件"
                            };
                            var skus = new List<SKUInfo>() { new SKUInfo()
                                        { Id=string.Format("{0}_{1}_{2}_{3}" , pid , "0" , "0" , "0"),
                                          Stock=long.TryParse(reader2["宝贝数量"],out lngStock)?lngStock:0,
                                          SalePrice=price,
                                          CostPrice=price
                                        }};
                            var description = new ProductDescriptionInfo
                            {
                                AuditReason = "",
                                Description = reader2["宝贝描述"],//.Replace("\"", ""),//不能纯去除
                                DescriptiondSuffixId = 0,
                                DescriptionPrefixId = 0,
                                Meta_Description = string.Empty,
                                Meta_Keywords = string.Empty,
                                Meta_Title = string.Empty,
                                ProductId = pid
                            };
                            //图片处理
                            product.ImagePath = imgpath1 + "/" + product.Id.ToString();
                            if (!string.IsNullOrEmpty(reader2["新图片"]) && !proimgs.Keys.Contains(product.Id))
                            {
                                proimgs.Add(product.Id, csvfiles[i] + "####" + reader2["新图片"]);//保存图片路径，让下面异步处理
                            }
                            string strcsvfile = csvfiles[i].Replace("/products.csv", "").Replace("\\products.csv", "");
                            iProcudt.AddProduct(_shopid, product, null, skus.ToArray(), description, null, new long[] { paraShopCategory }, null, null, strcsvfile);
                            //iProcudt.AddProduct(product, description, skus);
                            //_iSearchProductService.AddSearchProduct(product.Id);
                            result++;
                            Core.Log.Debug(strProductName);
                            Core.Cache.Insert<int>(CacheKeyCollection.UserImportProductCount(_userid), result);
                        }
                    }
                }

                #region //商品主图图片异步处理，尤其OSS导入图片时占用资源，图片上传用异步
                if (proimgs != null && proimgs.Count > 0)
                {
                    System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        foreach (var pimg in proimgs)
                        {
                            string[] saves = pimg.Value.Split(new string[] { "####" }, System.StringSplitOptions.RemoveEmptyEntries);
                            ImportProductImg(pimg.Key, _shopid, saves[0], saves[1]);
                        }
                    });
                }
                #endregion
            }
            return result;
        }

        /// <summary>
        /// 导入主图
        /// </summary>
        /// <param name="pid">产品ID</param>
        /// <param name="path">主图目录</param>
        /// <param name="filenames">主图文件信息</param>
        private void ImportProductImg(long pid, long _shopid, string path, string filenames)
        {
            //ff50d4ebbbe59def9faa2672d7538f44:1:0:|;1cd65d2a2d6b8c5bf1818151e5c982e6:1:1:|;54845d82ec3db3731fd63ebf5568b82d:2:0:1627207:107121|;
            path = path.Replace(Path.GetExtension(path), string.Empty);
            filenames = filenames.Replace("\"", string.Empty);
            string despath = string.Format(@"/Storage/Shop/{0}/Products/{1}", _shopid, pid);
            string[] arrFiles = new string[] { };
            string strDesfilename = string.Empty;
            int intImgCnt = 0;
            filenames.Split(';').ToList().ForEach(item =>
            {
                if (item != string.Empty)
                {
                    string[] strArray = item.Split(':');
                    if (strArray.Length > 0)
                    {
                        arrFiles = Directory.GetFiles(path, strArray[0] + ".*", SearchOption.AllDirectories);

                        intImgCnt += 1;

                        try
                        {
                            string dest = string.Format("{0}/{1}.png", despath, intImgCnt);

                            //读取文件流
                            FileStream fileStream = new FileStream(arrFiles[0], FileMode.Open, FileAccess.Read);

                            int byteLength = (int)fileStream.Length;
                            byte[] fileBytes = new byte[byteLength];
                            fileStream.Read(fileBytes, 0, byteLength);

                            //文件流关閉,文件解除锁定
                            fileStream.Close();

                            MemoryStream stream = new MemoryStream(fileBytes);

                            //using (Image image = Image.FromFile(arrFiles[0]))
                            //{
                            //    MemoryStream stream = new MemoryStream();

                            //    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                            Core.HimallIO.CreateFile(dest, stream, FileCreateType.Create);
                            //}

                            var imageSizes = EnumHelper.ToDictionary<ImageSize>().Select(t => t.Key);
                            foreach (var imageSize in imageSizes)
                            {
                                string size = string.Format("{0}/{1}_{2}.png", despath, intImgCnt, imageSize);
                                Core.HimallIO.CreateThumbnail(dest, size, imageSize, imageSize);
                            }
                        }
                        catch (FileNotFoundException fex)
                        {
                            Core.Log.Error("导入商品处理图片时，没有找到文件", fex);
                        }
                        catch (System.Runtime.InteropServices.ExternalException eex)
                        {
                            Core.Log.Error("导入商品处理图片时，ExternalException异常", eex);
                        }
                        catch (Exception ex)
                        {
                            Core.Log.Error("导入商品处理图片时，Exception异常", ex);
                        }
                        //IOHelper.CopyFile(source, Server.MapPath(dest), true);

                    }
                }
            });

        }


        #region  本地导入数据包模块
        [UnAuthorize]
        [HttpGet]
        public JsonResult ImportHimallProductJson(int paraSaleStatus, long _shopid, long _userid,string file)
        {
            /*
             产品ID/主图
             产品ID/Details/明细图片
            */
            
            string filePath = Server.MapPath("/temp/" + file);
            string imgpath1 = string.Format(@"/Storage/Shop/{0}/Products", _shopid);
            string imgpath2 = Server.MapPath(imgpath1);
    
            JsonResult result = new JsonResult();
            if (System.IO.File.Exists(filePath))
            {
                ZipHelper.ZipInfo zipinfo = ZipHelper.UnZipFile(filePath);
                if (zipinfo.Success)
                {
                    try
                    {
                       
                        int errorpro = ProcessProduct(paraSaleStatus, _shopid, _userid, zipinfo.UnZipPath, imgpath1);
                        int intCnt = Cache.Get<int>(CacheKeyCollection.UserImportProductCount(_userid));//获取成功数
                        if (errorpro > 0)
                        {
                            string filepath = zipinfo.UnZipPath + "\\error.xlsx";
                            var mappath = SiteSettingApplication.GetCurDomainUrl()+urlconvertor(filepath);
                            result = Json(new { success = false, fielname = mappath, message = "成功导入【" + intCnt.ToString() + "】件商品,失败商品【"+errorpro.ToString()+"】件" }, JsonRequestBehavior.AllowGet);
                            
                        }
                        else {
                           
                            result = Json(new { success = true, message = "成功导入【" + intCnt.ToString() + "】件商品" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("导入商品异常：" + ex.Message);
                        Core.Cache.Remove(CacheKeyCollection.UserImportProductCount(_userid));
                        Core.Cache.Remove(CacheKeyCollection.UserImportProductTotal(_userid));
                        result = Json(new { success = false, message = "导入商品异常:" + ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    Core.Log.Error("解压文件异常：" + zipinfo.InfoMessage);
                    result = Json(new { success = false, message = "解压出现异常,请检查压缩文件格式" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                result = Json(new { success = false, message = "上传文件不存在" }, JsonRequestBehavior.AllowGet);
            }
            var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
            if (opcount != 0)
            {
                Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount - 1);
            }
            return result;
        }

        /// <summary>
        /// 物理路径转网络路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string urlconvertor(string path)
        {
            string tmpRootDir = Server.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath.ToString());//获取程序根目录
            string temp = "\\" + path.Replace(tmpRootDir, ""); //转换成相对路径
            temp = temp.Replace(@"\", @"/");
            return temp;
        }

        private int ProcessProduct(int paraSaleStatus, long _shopid, long _userid, string mainpath, string imgpath1)
        {
            int result = 0;
            DataTable errortable = new DataTable();
            string strPath = mainpath;
            if (Directory.Exists(strPath))
            {
                ExcelHelper excelhelper = new ExcelHelper();
                DataTable dt=excelhelper.ExcelToDataSet(strPath+ "\\products.xlsx").Tables[0];

                Dictionary<long, string> proimgs = new Dictionary<long, string>();//待异步处理主图的图片
                string line = string.Empty;
                List<string> cells = new List<string>();

                var prostatus = paraSaleStatus == 1 ? ProductInfo.ProductSaleStatus.OnSale : ProductInfo.ProductSaleStatus.InStock;
                proimgs = ProductManagerApplication.ValidProduct(dt,_userid,_shopid, prostatus, strPath, imgpath1,out result);//导入商品
                if (result > 0) { //存在错误的商品信息
                    DataTable errordt=(DataTable)dt.Select("失败原因<>''").CopyToDataTable();
                    excelhelper.DataTableToExcel(dt, strPath+"\\error.xlsx",false);
                } 

                #region //商品主图图片异步处理，尤其OSS导入图片时占用资源，图片上传用异步
                if (proimgs != null && proimgs.Count > 0)
                {
                    Task.Factory.StartNew(() =>
                    {
                        foreach (var pimg in proimgs)
                        {
                            ImportHimallProdutImg(pimg.Key, _shopid,pimg.Value, strPath+"\\products");
                        }
                    });
                }
                #endregion
            }
            return result;
        }



		private void ImportHimallProdutImg(long pid, long _shopid, string filename,string path)
		{

			//ff50d4ebbbe59def9faa2672d7538f44:1:0:|;1cd65d2a2d6b8c5bf1818151e5c982e6:1:1:|;54845d82ec3db3731fd63ebf5568b82d:2:0:1627207:107121|;
		
			string despath = string.Format(@"/Storage/Shop/{0}/Products/{1}", _shopid, pid);
			string[] arrFiles = new string[] { };
			string strDesfilename = string.Empty;
			int intImgCnt = 0;

		
				arrFiles = Directory.GetFiles(path, filename, SearchOption.AllDirectories);

				intImgCnt += 1;

				try
				{
					string dest = string.Format("{0}/{1}.png", despath, intImgCnt);

					//读取文件流
					FileStream fileStream = new FileStream(arrFiles[0], FileMode.Open, FileAccess.Read);

					int byteLength = (int)fileStream.Length;
					byte[] fileBytes = new byte[byteLength];
					fileStream.Read(fileBytes, 0, byteLength);

					//文件流关閉,文件解除锁定
					fileStream.Close();

					MemoryStream stream = new MemoryStream(fileBytes);

					Core.HimallIO.CreateFile(dest, stream, FileCreateType.Create);


					var imageSizes = EnumHelper.ToDictionary<ImageSize>().Select(t => t.Key);
					foreach (var imageSize in imageSizes)
					{
						string size = string.Format("{0}/{1}_{2}.png", despath, intImgCnt, imageSize);
						Core.HimallIO.CreateThumbnail(dest, size, imageSize, imageSize);
					}
				}
				catch (FileNotFoundException fex)
				{
					Core.Log.Error("导入商品处理图片时，没有找到文件", fex);
				}
				catch (System.Runtime.InteropServices.ExternalException eex)
				{
					Core.Log.Error("导入商品处理图片时，ExternalException异常", eex);
				}
				catch (Exception ex)
				{
					Core.Log.Error("导入商品处理图片时，Exception异常", ex);
				}

		


		}


        #endregion


    }
}