﻿using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Models;
using System.Collections.Generic;
using System.Linq;
using Himall.Core;
using System.Web.Mvc;
using Himall.Web.Framework;
using System.ComponentModel;
using System;
using Himall.Application;
using System.Threading.Tasks;
using Himall.Entities;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class ShopController : BaseAdminController
    {
        ShopService _ShopService;
        ProductService _ProductService;
        OperationLogService _iOperationLogService;
        RegionService _RegionService;
        CategoryService _iCategoryService;
        SearchProductService _iSearchProductService;
        public ShopController(ShopService ShopService,
            ProductService ProductService,
            OperationLogService OperationLogService,
            RegionService RegionService,
            CategoryService CategoryService,
            SearchProductService SearchProductService)
        {
            _ShopService = ShopService;
            _ProductService = ProductService;
            _iOperationLogService = OperationLogService;
            _RegionService = RegionService;
            _iCategoryService = CategoryService;
            _iSearchProductService = SearchProductService;
            //获取当前店铺发布商品的数量
        }
        // GET: Admin/Shop
        public ActionResult Management(string type = "", int s = 0)
        {
            Himall.Web.Models.ShopModel result = new ShopModel();
            var state = (Himall.Entities.ShopInfo.ShopAuditStatus)s;
            List<string> drpstate = new List<string>();
            drpstate.Add("2");
            //drpstate.Add("3");
            //drpstate.Add("5");

            var status = Himall.Entities.ShopInfo.ShopAuditStatus.Open.ToSelectList();
            if (state > 0)
            {
                status = state.ToSelectList();
            }
            var StatusList = type == "Auditing" ? status.Where(c => drpstate.Contains(c.Value)) : status;
            var ssellist = StatusList.Where(t => t.Value != "5"&&t.Value!="1"&&t.Value!="3").Select(d => new { Id = d.Value, Name = d.Text }).ToList(); //待确认状态没用到下拉不用显示
            //if (type == "Auditing")
            //{
            ssellist.Insert(0, new { Id = "", Name = "请选择..." });
            //}
            SelectList StateSelectList = new SelectList((ssellist), "Id", "Name", s);

            ViewBag.StatusList = StateSelectList;
            var grade = _ShopService.GetShopGrades();
            List<SelectListItem> gradeList = new List<SelectListItem>{new SelectListItem
            {
                Selected = true,
                Value = 0.ToString(),
                Text = "请选择..."
            }};
            foreach (var item in grade)
            {
                gradeList.Add(new SelectListItem
                {
                    Selected = false,
                    Value = item.Id.ToString(),
                    Text = item.Name
                });
            }
            ViewBag.Type = type;
            ViewBag.Grade = gradeList;
            return View(result);
        }

        public ActionResult ShopOverview()
        {
            var result = new ShopModel();
            return View(result);
        }


        public JsonResult GetApplyList(int page, int rows, string shopName, Entities.BusinessCategoryApplyInfo.BusinessCateApplyStatus? status)
        {
            BussinessCateApplyQuery query = new BussinessCateApplyQuery();
            query.PageNo = page;
            query.PageSize = rows;
            query.ShopName = shopName;
            query.Status = status;
            var model = _ShopService.GetBusinessCateApplyList(query);
            var cate = model.Models.ToList().Select(a => new { Id = a.Id, ShopName = a.ShopName, ApplyDate = a.ApplyDate.ToString("yyyy-MM-dd HH:mm"), AuditedStatus = a.AuditedStatus.ToDescription() });
            var p = new { rows = cate.ToList(), total = model.Total };
            return Json(p);
        }


        public ActionResult ApplyDetail(long id)
        {
            var model = _ShopService.GetBusinessCategoriesApplyInfo(id);
            ViewBag.Details = ShopApplication.GetBusinessCategoriesApplyDetails(id);
            return View(model);
        }

        public JsonResult AgreeOrNot(Himall.Entities.BusinessCategoryApplyInfo.BusinessCateApplyStatus type, long id)
        {
            _ShopService.AuditShopBusinessCate(id, type);
            return Json(new Result() { success = true, msg = "审核成功" });
        }

        /// <summary>
        /// 冻结
        /// </summary>
        /// <param name="id">店铺标识</param>
        /// <param name="state">是否冻结</param>
        [HttpPost]
        public JsonResult FreezeShop(long id, bool state)
        {
            _ShopService.FreezeShop(id, state);
            Task.Factory.StartNew(() =>
            {
                _iSearchProductService.UpdateSearchStatusByShop(id);
            });
            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
            {
                Date = DateTime.Now,
                Description = (state ? "冻结" : "解冻") + "店铺信息，Id=" + id,
                IPAddress = Request.UserHostAddress,
                PageUrl = "/Shop/FreezeShop/",
                UserName = CurrentManager.UserName,
                ShopId = 0

            });
            return Json(new Result() { success = true, msg = "操作成功" });
        }


        public ActionResult ApplyList()
        {
            return View();
        }




        [HttpPost]
        [UnAuthorize]
        public JsonResult List(ShopQuery query, string type = "")
        {
            if (type == "Auditing")
            {
                query.Status = ShopInfo.ShopAuditStatus.WaitAudit;
                query.MoreStatus.Add(ShopInfo.ShopAuditStatus.WaitConfirm);
            }
            var shops = ShopApplication.GetShops(query);
            var shopGrades = ShopApplication.GetShopGrades();
            var shopAccounts = ShopApplication.GetShopAccounts(shops.Models.Select(p => p.Id).ToList());
            var models = shops.Models.Select(item =>
            {
                var shopGrade = shopGrades.FirstOrDefault(p => p.Id == item.GradeId);
                var shopAccountModel = shopAccounts.FirstOrDefault(p => p.ShopId == item.Id);
                var shopbranchs = ShopBranchApplication.GetShopBranchByShopId(item.Id);
                return new ShopModel()
                {
                    Id = item.Id,
                    Account = item.ShopAccount,
                    EndDate = type == "Auditing" ? "--" : item.EndDate.HasValue ? item.EndDate.Value.ToString("yyyy-MM-dd") : "",
                    Name = item.ShopName,
                    ShopGrade = shopGrade != null ? shopGrade.Name : "",
                    Status = (item.EndDate < DateTime.Now && item.ShowShopAuditStatus == ShopInfo.ShopAuditStatus.Open) ? "已过期" : item.ShowShopAuditStatus.ToDescription(),
                    IsSelf = item.IsSelf,
                    BusinessType = item.BusinessType == null ? CommonModel.ShopBusinessType.Enterprise : item.BusinessType.Value,
                    Balance = shopAccountModel != null ? shopAccountModel.Balance : 0,
                    ShopBranchCount = shopbranchs.Count()
                };
            });

            var dataGrid = new DataGridModel<ShopModel>() { rows = models, total = shops.Total };
            return Json(dataGrid);
        }
        /// <summary>
        /// 选择商家控件数据源
        /// </summary>
        /// <param name="query"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult ShopList(string shopName, int page, int rows, string ids = "")
        {
            ShopQuery query = new ShopQuery
            {
                Status = ShopInfo.ShopAuditStatus.Open,
                ShopName = shopName,
                PageNo = page,
                PageSize = rows > 0 ? rows : 7,
                IsAsc = false,
                Sort = "Id",
                ShopIds = ids,
            };
            var shops = ShopApplication.GetShops(query);
            var shopGrades = ShopApplication.GetShopGrades();
            var models = shops.Models.Select(item =>
            {
                var shopGrade = shopGrades.FirstOrDefault(p => p.Id == item.GradeId);
                return new ShopModel()
                {
                    Id = item.Id,
                    Account = item.ShopAccount,
                    EndDate = item.EndDate.HasValue ? item.EndDate.Value.ToString("yyyy-MM-dd") : "",
                    Name = item.ShopName,
                    ShopGrade = shopGrade != null ? shopGrade.Name : "",
                    Status = (item.EndDate < DateTime.Now && item.ShowShopAuditStatus == ShopInfo.ShopAuditStatus.Open) ? "已过期" : item.ShowShopAuditStatus.ToDescription(),
                    IsSelf = item.IsSelf,
                    BusinessType = item.BusinessType == null ? CommonModel.ShopBusinessType.Enterprise : item.BusinessType.Value,
                };
            });

            var dataGrid = new DataGridModel<ShopModel>() { rows = models, total = shops.Total };
            return Json(dataGrid);
        }

        /// <summary>
        /// 修改商家入驻信息
        /// </summary>
        /// <param name="shop"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult Edit(ShopModel shop)
        {
            var service = _ShopService;
            var nowShop = service.GetShop(shop.Id);

            //验证修改的店铺等级是否通过
            if (!CheckShopGrade(shop.Id, Convert.ToInt64(shop.ShopGrade)))
            {
                throw new HimallException("该店铺已使用空间数或已添加商品数大于该套餐");
            }

            if (service.ExistShop(shop.Name, shop.Id))
            {
                throw new HimallException("该店铺已存在");
            }

            if (ShopApplication.ExistBusinessLicenceNumber(shop.BusinessLicenceNumber, shop.Id))
            {
                throw new HimallException("营业执照号已存在");
            }
            bool isExpired = false;
            if (ModelState.IsValid)
            {
                isExpired = Convert.ToDateTime(shop.EndDate) < DateTime.Now.AddDays(1).Date;

                nowShop.CompanyName = shop.CompanyName;
                nowShop.CompanyAddress = shop.CompanyAddress;
                nowShop.CompanyEmployeeCount = shop.CompanyEmployeeCount;
                nowShop.ShopName = shop.Name;
                nowShop.GradeId = Convert.ToInt64(shop.ShopGrade);
                nowShop.EndDate = Convert.ToDateTime(shop.EndDate);
                nowShop.CompanyRegionId = shop.CompanyRegionId;
                nowShop.ShopStatus = (Entities.ShopInfo.ShopAuditStatus)(Convert.ToInt32(shop.Status));
                nowShop.ContactsName = shop.ContactsName;
                nowShop.ContactsPhone = shop.ContactsPhone;
                nowShop.ContactsEmail = shop.ContactsEmail;
                nowShop.BusinessLicenseCert = Request.Form["BusinessLicenseCert"];
                nowShop.ProductCert = Request.Form["ProductCert"];
                nowShop.OtherCert = Request.Form["OtherCert"];
                nowShop.BusinessLicenceNumber = shop.BusinessLicenceNumber;
                nowShop.BusinessSphere = shop.BusinessSphere;
                nowShop.BusinessLicenceNumberPhoto = shop.BusinessLicenceNumberPhoto == null ? "" : shop.BusinessLicenceNumberPhoto;
                nowShop.OrganizationCode = shop.OrganizationCode;
                nowShop.OrganizationCodePhoto = shop.OrganizationCodePhoto;
                nowShop.GeneralTaxpayerPhot = shop.GeneralTaxpayerPhot;
                nowShop.OrderPayIsSendSMS = shop.OrderPayIsSendSMS;
                if (nowShop.EndDate > DateTime.Now)
                {
                    nowShop.ShopStatus = Entities.ShopInfo.ShopAuditStatus.Open;
                }
                if (nowShop.EndDate < DateTime.Now)
                {
                    nowShop.ShopStatus = ShopInfo.ShopAuditStatus.HasExpired;
                }

                _ShopService.UpdateShop(nowShop);

                Task.Factory.StartNew(() =>
                {
                    _iSearchProductService.UpdateShop(shop.Id, shop.Name);
                    if (isExpired)
                        _iSearchProductService.UpdateSearchStatusByShop(shop.Id);
                });

                _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                {
                    Date = DateTime.Now,
                    Description = "修改店铺信息，Id=" + shop.Id,
                    IPAddress = Request.UserHostAddress,
                    PageUrl = "/Shop/Edit/" + shop.Id,
                    UserName = CurrentManager.UserName,
                    ShopId = 0

                });
                return Json(new Result { success = true, msg = "保存成功！" });
            }
            else
            {
                List<string> sb = new List<string>();
                //获取所有错误的Key
                List<string> Keys = ModelState.Keys.ToList();
                //获取每一个key对应的ModelStateDictionary
                foreach (var key in Keys)
                {
                    var errors = ModelState[key].Errors.ToList();
                    //将错误描述添加到sb中
                    foreach (var error in errors)
                    {
                        sb.Add(error.ErrorMessage);
                    }
                }
                return Json(new Result { success = false, msg = sb[0].ToString() });
            }

        }

        /// <summary>
        /// 修改个人入驻信息
        /// </summary>
        /// <param name="shop"></param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult EditPersonal(ShopPersonal shop)
        {
            var service = _ShopService;
            var nowShop = service.GetShop(shop.Id);

            //验证修改的店铺等级是否通过
            if (!CheckShopGrade(shop.Id, Convert.ToInt64(shop.ShopGrade)))
            {
                throw new HimallException("该店铺已使用空间数或已添加商品数大于该套餐");
            }

            if (service.ExistShop(shop.Name, shop.Id))
            {
                throw new HimallException("该店铺已存在");
            }
            //日龙修改
            //去除检查公司名称重复
            //if (service.ExistShop(nowShop.CompanyName, shop.Id))
            //{
            //    throw new HimallException("该公司名称已存在");
            //}
            bool isExpired = false;
            if (ModelState.IsValid)
            {
                isExpired = Convert.ToDateTime(shop.EndDate).Date < DateTime.Now.AddDays(1).Date;
                nowShop.CompanyName = shop.CompanyName;
                nowShop.CompanyAddress = shop.CompanyAddress;
                nowShop.ShopName = shop.Name;
                nowShop.GradeId = Convert.ToInt64(shop.ShopGrade);
                nowShop.EndDate = Convert.ToDateTime(shop.EndDate);
                nowShop.IDCard = shop.IDCard;
                nowShop.IDCardUrl = shop.IDCardUrl;
                nowShop.IDCardUrl2 = shop.IDCardUrl2;
                nowShop.CompanyRegionId = shop.NewCompanyRegionId;
                nowShop.ContactsName = shop.ContactsName;
                nowShop.ContactsEmail = shop.ContactsEmail;
                nowShop.ContactsPhone = shop.ContactsPhone;
                nowShop.OrderPayIsSendSMS = shop.OrderPayIsSendSMS;
                if (nowShop.EndDate > DateTime.Now)
                {
                    nowShop.ShopStatus = Entities.ShopInfo.ShopAuditStatus.Open;
                }
                if (nowShop.EndDate < DateTime.Now)
                {
                    nowShop.ShopStatus = ShopInfo.ShopAuditStatus.HasExpired;
                }

                _ShopService.UpdateShop(nowShop);

                Task.Factory.StartNew(() =>
                {
                    _iSearchProductService.UpdateShop(shop.Id, shop.Name);
                    if (isExpired)
                        _iSearchProductService.UpdateSearchStatusByShop(shop.Id);
                });

                _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                {
                    Date = DateTime.Now,
                    Description = "修改店铺信息，Id=" + shop.Id,
                    IPAddress = Request.UserHostAddress,
                    PageUrl = "/Shop/EditPersonal/" + shop.Id,
                    UserName = CurrentManager.UserName,
                    ShopId = 0

                });
                return Json(new Result { success = true, msg = "保存成功！" });
            }
            else
            {
                List<string> sb = new List<string>();
                //获取所有错误的Key
                List<string> Keys = ModelState.Keys.ToList();
                //获取每一个key对应的ModelStateDictionary
                foreach (var key in Keys)
                {
                    var errors = ModelState[key].Errors.ToList();
                    //将错误描述添加到sb中
                    foreach (var error in errors)
                    {
                        sb.Add(error.ErrorMessage);
                    }
                }
                return Json(new Result { success = false, msg = sb[0].ToString() });
            }

        }

        /// <summary>
        /// 企业入驻信息编辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [UnAuthorize]
        public ActionResult Edit(long id = 0)
        {
            if (id == 0)
                return View(new ShopModel());
            var shop = _ShopService.GetShop(id);
            var shopGrade = _ShopService.GetShopGrades();
            List<SelectListItem> ShopG = new List<SelectListItem>();
            foreach (var item in shopGrade)
            {
                ShopG.Add(new SelectListItem
                {
                    Selected = item.Id == shop.GradeId,
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }
            ViewBag.ShopGrade = ShopG;
            ViewBag.Status = (int)shop.ShopStatus;
            ViewBag.BankRegionIds = _RegionService.GetRegionPath(shop.BankRegionId);
            ViewBag.CompanyRegionIds = _RegionService.GetRegionPath(shop.CompanyRegionId);
            ViewBag.BusinessLicenceArea = _RegionService.GetRegionPath(shop.BusinessLicenceRegionId);
            return View(new ShopModel(shop));
        }

        /// <summary>
        /// 个人入驻信息编辑
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [UnAuthorize]
        public ActionResult EditPersonal(long id = 0)
        {
            if (id == 0)
                return View(new ShopModel());
            var shop = _ShopService.GetShop(id);

            ShopPersonal shopPersoal = new ShopPersonal()
            {
                Id = shop.Id,
                CompanyAddress = shop.CompanyAddress,
                CompanyName = shop.CompanyName,
                CompanyRegion = ObjectContainer.Current.Resolve<RegionService>().GetFullName(shop.CompanyRegionId),
                EndDate = shop.EndDate.ToString("yyyy-MM-dd"),
                IDCard = shop.IDCard,
                IDCardUrl = shop.IDCardUrl,
                IDCardUrl2 = shop.IDCardUrl2,
                NewCompanyRegionId = shop.CompanyRegionId,
                Name = shop.ShopName,
                ContactsName = shop.ContactsName,
                ContactsEmail = shop.ContactsEmail,
                ContactsPhone = shop.ContactsPhone,
                OrderPayIsSendSMS = shop.OrderPayIsSendSMS,
            };
            var obj = ObjectContainer.Current.Resolve<ShopService>().GetShopGrade(shop.GradeId);
            shopPersoal.ShopGrade = obj == null ? "" : obj.Name;


            var shopGrade = _ShopService.GetShopGrades();
            List<SelectListItem> ShopG = new List<SelectListItem>();
            foreach (var item in shopGrade)
            {
                ShopG.Add(new SelectListItem
                {
                    Selected = item.Id == shop.GradeId,
                    Text = item.Name,
                    Value = item.Id.ToString()
                });
            }
            ViewBag.ShopGrade = ShopG;
            ViewBag.Status = (int)shop.ShopStatus;
            ViewBag.BankRegionIds = _RegionService.GetRegionPath(shop.BankRegionId);
            ViewBag.CompanyRegionIds = _RegionService.GetRegionPath(shop.CompanyRegionId);
            ViewBag.BusinessLicenceArea = _RegionService.GetRegionPath(shop.BusinessLicenceRegionId);
            return View(shopPersoal);
        }

        [HttpPost]
        public JsonResult DeleteShop(long Id)
        {
            _ShopService.DeleteShop(Id);
            _iOperationLogService.AddPlatformOperationLog(new LogInfo
            {
                Date = DateTime.Now,
                Description = "删除店铺，Id=" + Id,
                IPAddress = Request.UserHostAddress,
                PageUrl = "/Shop/Edit/" + Id,
                UserName = CurrentManager.UserName,
                ShopId = 0

            });
            return Json(new { success = true });
        }

        public ActionResult Details(long id)
        {
            var shop = _ShopService.GetShop(id, true);
            var model = new ShopModel(shop);
            model.BusinessCategory = new List<CategoryKeyVal>();
            foreach (var key in shop.BusinessCategory.Keys)
            {
                var category = _iCategoryService.GetCategory(key);
                if (category != null)
                {
                    model.BusinessCategory.Add(new CategoryKeyVal
                    {
                        CommisRate = shop.BusinessCategory[key],
                        Name = category.Name
                    });
                }
            }

            var shopModel = _ShopService.GetShop(id);
            ViewBag.PassStr = shopModel.ShopStatus.ToDescription();
            return View(model);
        }

        [HttpGet]
        [UnAuthorize]
        public JsonResult GetCategoryCommisRate(long id)
        {
            var comm = _iCategoryService.GetCategory(id).CommisRate;
            return Json(new { success = true, CommisRate = comm }, JsonRequestBehavior.AllowGet);
        }

        [Description("开店审核页面")]
        [HttpGet]
        [UnAuthorize]
        public ActionResult Auditing(long id)
        {
            var shop = _ShopService.GetShop(id, true);
            ViewBag.ShopId = id;
            ViewBag.Status = (int)shop.ShopStatus;
            var model = new ShopModel(shop);
            model.BusinessCategory = new List<CategoryKeyVal>();
            foreach (var key in shop.BusinessCategory.Keys)
            {
                model.BusinessCategory.Add(new CategoryKeyVal
                {
                    CommisRate = shop.BusinessCategory[key],
                    Name = _iCategoryService.GetCategory(key).Name
                });
            }
            return View(model);
        }

        [Description("开店审核页面(POST)")]
        [HttpPost]
        [UnAuthorize]
        public JsonResult Auditing(long shopId, int status, string comment = "")
        {
            Himall.DTO.Settled mSettled = ShopApplication.GetSettled();

            _ShopService.UpdateShopStatus(shopId, (Entities.ShopInfo.ShopAuditStatus)status, comment, mSettled.TrialDays);
            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
            {
                Date = DateTime.Now,
                Description = string.Format("开店审核页面，店铺Id={0},状态为：{1}, 说明是：{2}", shopId,
                       ((ShopInfo.ShopAuditStatus)status).ToString(), comment),
                IPAddress = Request.UserHostAddress,
                PageUrl = "/Shop/Auditing/" + shopId,
                UserName = CurrentManager.UserName,
                ShopId = 0

            });
            return Json(new { success = true });
        }

        [Description("类目经营页面")]
        public ActionResult BusinessCategory(long id)
        {
            var shop = _ShopService.GetShop(id);
            ViewBag.ShopId = id;
            ViewBag.ShopName = shop != null ? shop.ShopName : "";
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult BusinessCategoryList(BusinessCategoryQuery query)
        {
            var shops = ShopApplication.GetBusinessCategoryList(query);

            var dataGrid = new DataGridModel<BusinessCategoryInfo>() { rows = shops.Models, total = shops.Total };
            return Json(dataGrid);
        }

        public JsonResult CanDeleteBusinessCategory(long id, long cid)
        {
            var result = new Result { status = -1, msg = "未知异常", success = false };
            if (_ShopService.CanDeleteBusinessCategory(id, cid))
            {
                _ShopService.DeleteBusinessCategory(id, cid);
                result = new Result { status = 1, msg = "删除成功", success = true };
            }
            else
            {
                result = new Result { status = -1, msg = "不可删除，有商品被购买", success = false };
            }
            return Json(result);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SaveBusinessCategory(long shopId = 0, string bcategory = "")
        {
            Dictionary<long, decimal> businessCategory = new Dictionary<long, decimal>();
            foreach (var item in bcategory.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    if (!businessCategory.ContainsKey(int.Parse(item.Split('|')[0])))
                        businessCategory.Add(int.Parse(item.Split('|')[0]), decimal.Parse(item.Split('|')[1]));
                }
            }
            _ShopService.SaveBusinessCategory(shopId, businessCategory);
            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
            {
                Date = DateTime.Now,
                Description = "修改店铺经营类目，店铺Id=" + shopId,
                IPAddress = Request.UserHostAddress,
                PageUrl = "/Shop/SaveBusinessCategory?shopId=" + shopId + "&bcategory=" + bcategory,
                UserName = CurrentManager.UserName,
                ShopId = 0

            });
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult AddShopCommisRate(long shopId, long businessCategoryId, decimal commisRate)
        {
            BusinessCategoryInfo business = new BusinessCategoryInfo();
            business.ShopId = shopId;
            business.CategoryId = businessCategoryId;
            business.CommisRate = commisRate;
            _ShopService.AddBusinessCategory(business);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateShopCommisRate(long businessCategoryId, decimal commisRate)
        {
            _ShopService.SaveBusinessCategory(businessCategoryId, commisRate);
            return Json(new { success = true });
        }
        /// <summary>
        /// 验证修改的店铺等级是否符合要求
        /// </summary>
        /// <param name="shopId">店铺编号</param>
        /// <param name="newId">需修改的店铺等级编号</param>
        /// <returns></returns>
        [HttpPost]
        public bool CheckShopGrade(long shopId, long newId)
        {
            var shopservice = _ShopService;
            var productservice = _ProductService;
            //获取当前店铺发布商品的数量
            var productCount = productservice.GetShopAllProducts(shopId);
            //获取当前店铺使用的空间
            var usage = shopservice.GetShopUsageSpace(shopId);

            if (newId > 0)
            {
                var newshopGrade = _ShopService.GetShopGrade(newId);
                if (newshopGrade != null)
                {
                    var newproductLimit = newshopGrade.ProductLimit;
                    var newimageLimit = newshopGrade.ImageLimit;
                    //如果修改的店铺等级商品发布数、使用空间 任何一个小于正在使用的商品发布数、使用空间 则不通过
                    if (newproductLimit < productCount || newimageLimit < usage)
                        return false;
                    else
                        return true;
                }
                else
                    return true;
            }
            else
                return true;
        }
    }
}