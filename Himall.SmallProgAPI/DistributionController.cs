using AutoMapper;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.SmallProgAPI.Model;
using Himall.SmallProgAPI.Model.ParaModel;
using Himall.Web.Framework;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    /// <summary>
    /// 分销相关接口
    /// </summary>
    public class DistributionController : BaseApiController
    {
        private const string DISTRIBUTOR_LOGO_PATH = "/Storage/Distribution/";
        /// <summary>
        /// 获取分销员信息
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetDistributor()
        {
            CheckUserLogin();
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(CurrentUser.Id);
            var site = SiteSettingApplication.SiteSettings;
            string strdistributorRenameMyShop = site.DistributorRenameOpenMyShop;//默认开店名称
            if (site.DistributionIsEnable && currentDistributor != null && currentDistributor.DistributionStatus == (int)DistributorStatus.Audited)
            {
                strdistributorRenameMyShop = site.DistributorRenameMyShop;
            }
            return JsonResult<dynamic>(new
            {
                DistributionIsEnable = site.DistributionIsEnable,
                IsDistributor = currentDistributor != null,
                DistributionStatus = currentDistributor == null ? 0 : currentDistributor.DistributionStatus,
                IsOnlyMember = currentDistributor == null ? true : currentDistributor.IsOnlyMember,
                Remark = currentDistributor == null ? "" : currentDistributor.Remark,
                DistributorRenameMyShop = strdistributorRenameMyShop    //自定义名称
            });
        }

        /// <summary>
        /// 我要开店
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetOpenMyShopInfo()
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            return JsonResult<dynamic>(new
            {
                DistributorPageContent = string.IsNullOrEmpty(sitesettings.DistributorPageContent) ? string.Empty : sitesettings.DistributorPageContent.Replace("src=\"/temp/", "src=\"" + Core.HimallIO.GetRomoteImagePath("/temp") + "/"),
                DistributorRenameOpenMyShop = sitesettings.DistributorRenameOpenMyShop  //我要开店
            });
        }

        /// <summary>
        /// 获取分销设置
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetDistributorSiteSetting()
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            return JsonResult<dynamic>(new
            {
                DistributorPageContent = sitesettings.DistributorPageContent,  //我要开店页面内容
                DistributorRenameOpenMyShop = sitesettings.DistributorRenameOpenMyShop,  //我要开店
                DistributorRenameMyShop = sitesettings.DistributorRenameMyShop,  //我的小店
                DistributorRenameSpreadShop = sitesettings.DistributorRenameSpreadShop,  //推广小店
                DistributorRenameBrokerage = sitesettings.DistributorRenameBrokerage,  //佣金
                DistributorRenameMarket = sitesettings.DistributorRenameMarket,  //分销市场
                DistributorRenameShopOrder = sitesettings.DistributorRenameShopOrder,  //小店订单
                DistributorRenameMyBrokerage = sitesettings.DistributorRenameMyBrokerage,  //我的佣金
                DistributorRenameMySubordinate = sitesettings.DistributorRenameMySubordinate,  //我的下级
                DistributorRenameMemberLevel1 = sitesettings.DistributorRenameMemberLevel1,  //一级会员
                DistributorRenameMemberLevel2 = sitesettings.DistributorRenameMemberLevel2,  //二级会员
                DistributorRenameMemberLevel3 = sitesettings.DistributorRenameMemberLevel3, //三级会员
                DistributorRenameShopConfig = sitesettings.DistributorRenameShopConfig,  //小店设置
                DistributorWithdrawMinLimit = sitesettings.DistributorWithdrawMinLimit, //分销提现最小金额
                DistributorWithdrawMaxLimit = sitesettings.DistributorWithdrawMaxLimit,  //分销提现最大金额
                DistributionIsEnable = sitesettings.DistributionIsEnable,  //是否启用分销功能
            });
        }

        /// <summary>
        /// 是否可以申请销售员
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<bool>> GetCanApplyMyShop()
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(CurrentUser.Id);
            if (sitesettings.DistributorApplyNeedQuota > 0)
            {
                var _u = MemberApplication.GetMember(CurrentUser.Id);
                if (_u.NetAmount < sitesettings.DistributorApplyNeedQuota)
                {
                    return Json(ErrorResult<bool>("需要累计消费金额达到" + sitesettings.DistributorApplyNeedQuota + "元才可申请哦！"));
                }
            }
            if (currentDistributor != null)
            {
                if (currentDistributor.DistributionStatus == (int)DistributorStatus.NotAvailable)
                {
                    return Json(ErrorResult<bool>("您己经被清退，不可以申请！"));
                }
                if (currentDistributor.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    return Json(ErrorResult<bool>("您己经是销售员，不可以重复申请！"));
                }
                if (currentDistributor.DistributionStatus == (int)DistributorStatus.UnAudit)
                {
                    return Json(ErrorResult<bool>("您己经提交销售员申请，请耐心等待！"));
                }
            }
            return JsonResult(true);
        }

        /// <summary>
        /// 提交申请销售员
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> PostApplyDistributor(PostSaveShopConfigModel model)
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(CurrentUser.Id);
            if (sitesettings.DistributorApplyNeedQuota > 0)
            {
                var _u = MemberApplication.GetMember(CurrentUser.Id);
                if (_u.NetAmount < sitesettings.DistributorApplyNeedQuota)
                {
                    return Json(ErrorResult<dynamic>("需要累计消费金额达到" + sitesettings.DistributorApplyNeedQuota + "元才可申请哦！"));
                }
            }
            if (currentDistributor == null)
            {
                currentDistributor = new DistributorInfo
                {
                    MemberId = CurrentUser.Id
                };
            }
            if (string.IsNullOrWhiteSpace(currentDistributor.ShopLogo))
            {
                currentDistributor.ShopLogo = CurrentUser.Photo;
            }
            if (string.IsNullOrWhiteSpace(model.logoUrl))
            {
                if (currentDistributor == null || string.IsNullOrWhiteSpace(currentDistributor.ShopLogo))
                {
                    return Json(ErrorResult<dynamic>("请上传小店logo！"));
                }
            }
            if (string.IsNullOrWhiteSpace(model.shopName))
            {
                return Json(ErrorResult<dynamic>("请填写小店名称！"));
            }
            if (model.shopName.Length > 10)
            {
                return Json(ErrorResult<dynamic>("小店名称不能超过10个字符！"));
            }
            if (currentDistributor != null)
            {
                if (currentDistributor.DistributionStatus == (int)DistributorStatus.NotAvailable)
                {
                    return Json(ErrorResult<dynamic>("您己经被清退，不可以申请！"));
                }
                if (currentDistributor.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    return Json(ErrorResult<dynamic>("您己经是销售员，不可以重复申请！"));
                }
                if (currentDistributor.DistributionStatus == (int)DistributorStatus.UnAudit)
                {
                    return Json(ErrorResult<dynamic>("您己经提交销售员申请，请耐心等待！"));
                }
            }

            string shoplogo = currentDistributor != null ? currentDistributor.ShopLogo : "";
            if (!string.IsNullOrWhiteSpace(model.logoUrl))
            {
                //shoplogo = DownloadWxImage(model.logoWXmediaId);
                shoplogo = model.logoUrl;
            }
            if (string.IsNullOrWhiteSpace(shoplogo))
            {
                return Json(ErrorResult<dynamic>("请上传小店logo！"));
            }
            var oldname = Path.GetFileName(shoplogo);
            string ImageDir = string.Empty;

            if (!string.IsNullOrWhiteSpace(shoplogo))
            {
                //转移图片
                string relativeDir = DISTRIBUTOR_LOGO_PATH;
                Random ra = new Random();
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + ra.Next(4) + ".jpg";
                if (shoplogo.Replace("\\", "/").Contains("/temp/"))//只有在临时目录中的图片才需要复制
                {
                    var de = shoplogo.Substring(shoplogo.LastIndexOf("/temp/"));
                    Core.HimallIO.CopyFile(de, relativeDir + fileName, true);
                    shoplogo = relativeDir + fileName;
                }  //目标地址
                else if (shoplogo.Contains("/Storage"))
                {
                    shoplogo = shoplogo.Substring(shoplogo.LastIndexOf("/Storage"));
                }
            }
            var d = DistributionApplication.ApplyDistributor(CurrentUser.Id, shoplogo, model.shopName);
            if (d == null)
            {
                return Json(ErrorResult<dynamic>("申请失败，系统异常！"));
            }
            if (d.DistributionStatus == (int)DistributorStatus.Audited)
            {
                return JsonResult<dynamic>(new { success = true, NeedAudit = sitesettings.DistributorNeedAudit }, "申请销售员成功！");
            }
            return JsonResult<dynamic>(true, "提交销售员申请成功！");
        }

        /// <summary>
        /// 下载微信图片
        /// </summary>
        /// <param name="link">下载地址</param>
        /// <param name="filePath">保存相对路径</param>
        /// <param name="fileName">保存地址</param>
        /// <returns></returns>
        //public string DownloadWxImage(string mediaId)
        //{
        //    var sitesettings = SiteSettingApplication.SiteSettings;
        //    var token = AccessTokenContainer.TryGetAccessToken(sitesettings.WeixinAppId, sitesettings.WeixinAppSecret);
        //    var address = string.Format("https://file.api.weixin.qq.com/cgi-bin/media/get?access_token={0}&media_id={1}", token, mediaId);
        //    Random ra = new Random();
        //    var fileName = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + ra.Next(4) + ".jpg";
        //    var ImageDir = DISTRIBUTOR_LOGO_PATH;
        //    WebClient wc = new WebClient();
        //    try
        //    {
        //        string fullPath = Path.Combine(ImageDir, fileName);
        //        ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
        //        var data = wc.DownloadData(address);
        //        MemoryStream stream = new MemoryStream(data);
        //        Core.HimallIO.CreateFile(fullPath, stream, FileCreateType.Create);
        //        return DISTRIBUTOR_LOGO_PATH + fileName;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("下载图片发生异常" + ex.Message);
        //        return string.Empty;
        //    }
        //}
        //public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        //{  // 总是接受  
        //    return true;
        //}


        /// <summary>
        /// 我的小店
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetMyShop()
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(CurrentUser.Id);
            dynamic result = new System.Dynamic.ExpandoObject();
            result.MemberId = currentDistributor.MemberId;
            result.ShopLogo = currentDistributor.ShopLogo;
            result.ShopName = currentDistributor.ShopName;
            result.IsShowLogo = currentDistributor.IsShowShopLogo;
            result.GradeName = currentDistributor.GradeName;
            result.SettlementAmount = currentDistributor.SettlementAmount;
            result.Balance = currentDistributor.Balance;
            if (string.IsNullOrWhiteSpace(currentDistributor.ShopLogo))
            {
                result.ShopLogo = CurrentUser.Photo;
            }
            result.ShopLogo = Himall.Core.HimallIO.GetRomoteImagePath(result.ShopLogo);
            var grades = DistributionApplication.GetDistributorGrades();
            var nextgrade = grades.Where(d => d.Quota > result.SettlementAmount).OrderBy(d => d.Quota).FirstOrDefault();
            if (nextgrade != null)
            {
                result.NextGradeName = nextgrade.GradeName;
                result.UpgradeNeedAmount = nextgrade.Quota - result.SettlementAmount;
            }
            result.NoSettlementAmount = DistributionApplication.GetNoSettlementAmount(currentDistributor.MemberId);
            result.DistributorRenameSpreadShop = sitesettings.DistributorRenameSpreadShop;  //推广小店
            result.DistributorRenameBrokerage = sitesettings.DistributorRenameBrokerage;  //佣金
            result.DistributorRenameMarket = sitesettings.DistributorRenameMarket;  //分销市场
            result.DistributorRenameShopOrder = sitesettings.DistributorRenameShopOrder;  //小店订单
            result.DistributorRenameMyBrokerage = sitesettings.DistributorRenameMyBrokerage;  //我的佣金
            result.DistributorRenameMySubordinate = sitesettings.DistributorRenameMySubordinate;  //我的下级
            result.DistributorRenameShopConfig = sitesettings.DistributorRenameShopConfig;  //小店设置
            result.DistributorRenameMyBusinessCard = sitesettings.DistributorRenameMyBusinessCard;  //我的名片

            return JsonResult<dynamic>(result);
        }
        /// <summary>
        /// 我的佣金
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetMyBrokerage()
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            dynamic result = new System.Dynamic.ExpandoObject();
            result.DistributorRenameMyBrokerage = sitesettings.DistributorRenameMyBrokerage;
            var model = DistributionApplication.GetDistributorDTO(CurrentUser.Id);
            result.SettlementAmount = model.SettlementAmount;
            result.Balance = model.Balance;
            result.WithdrawalsAmount = model.WithdrawalsAmount;
            result.NoSettlementAmount = DistributionApplication.GetNoSettlementAmount(CurrentUser.Id);
            return JsonResult<dynamic>(result);
        }
        /// <summary>
        /// 获取流水记录
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetRecords(int pageSize, int pageNo)
        {
            CheckUserLogin();
            DistributorRecordQuery query = new DistributorRecordQuery();
            query.PageSize = pageSize;
            query.PageNo = pageNo;
            query.MemberId = CurrentUser.Id;
            var models = DistributionApplication.GetRecords(query);
            var list = models.Models.Select(item => new
            {
                Type = item.Type.ToDescription(),
                Time = item.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Amount = item.Amount.ToString("n2"),
                item.Remark,
            });
            return JsonResult<dynamic>(new { rows = list, total = models.Total });
        }
        /// <summary>
        /// 申请提现页面
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetApplyWithdraw()
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            dynamic result = new System.Dynamic.ExpandoObject();
            var distributor = DistributionApplication.GetDistributorDTO(CurrentUser.Id);
            result.Balance = distributor.Balance;//账户余额
            result.IsSetPassword = MemberApplication.HasPayPassword(CurrentUser.Id);//是否设置交易密码
            result.MinLimit = sitesettings.DistributorWithdrawMinLimit;
            result.MaxLimit = sitesettings.DistributorWithdrawMaxLimit < distributor.Balance ? sitesettings.DistributorWithdrawMaxLimit : distributor.Balance;
            result.Types = sitesettings.DistributorWithdrawTypes;
            result.EnableCapital = result.Types?.ToLower().Contains("capital") ?? false;
            result.EnableWeChat = result.Types?.ToLower().Contains("wechat") ?? false;
            result.EnableAlipay = result.Types?.ToLower().Contains("alipay") ?? false;
            return JsonResult<dynamic>(result);
        }
        /// <summary>
        /// 申请提现
        /// </summary>
        /// <param name="post"></param>
        /// <returns></returns>
        public JsonResult<Result<bool>> PostWithdraw(DistributionApplyWithdraw post)
        {
            CheckUserLogin();
            var sitesettings = SiteSettingApplication.SiteSettings;
            post.MemberId = CurrentUser.Id;
            if (post.Type == DistributionWithdrawType.WeChat) //获取用户微信账户
            {

                var mo = MemberApplication.GetMemberOpenIdInfoByuserId(CurrentUser.Id, Entities.MemberOpenIdInfo.AppIdTypeEnum.Payment, "Himall.Plugin.OAuth.WeiXin");
                if (mo != null && !string.IsNullOrEmpty(mo.OpenId))
                {
                    var openid = mo.OpenId;
                    post.WithdrawAccount = openid;
                    post.WithdrawName = string.IsNullOrWhiteSpace(CurrentUser.RealName) ? CurrentUser.RealName : CurrentUser.Nick;
                }
                else
                {
                    var moweb = MemberApplication.GetMemberOpenIdInfoByuserId(CurrentUser.Id, Entities.MemberOpenIdInfo.AppIdTypeEnum.Normal, "WeiXinSmallProg");
                    if (moweb == null)
                    {
                        return Json(ErrorResult<bool>("无法获取微信小程序账号，请先重新登录账号！"));
                    }

                    var openid = moweb.OpenId;
                    post.WithdrawAccount = openid;
                    if (string.IsNullOrWhiteSpace(openid))
                        return Json(ErrorResult<bool>("数据异常,OpenId或收款账号不可为空！"));
                    post.WithdrawName = string.IsNullOrWhiteSpace(CurrentUser.RealName) ? CurrentUser.RealName : CurrentUser.Nick;



                }

                if (string.IsNullOrEmpty(post.WithdrawName))
                    post.WithdrawName = CurrentUser.Nick;//它上面可能没获取到用户信息(比如小程序获取用户信息api暂是不可以的,还有网络请求)
            }
            DistributionApplication.ApplyWithdraw(post);
            return JsonResult(true);
        }
        /// <summary>
        /// 获取提现记录
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetWithdraws(int pageSize, int pageNo)
        {
            CheckUserLogin();
            DistributionWithdrawQuery query = new DistributionWithdrawQuery();
            query.PageSize = pageSize;
            query.PageNo = pageNo;
            query.MemberId = CurrentUser.Id;
            var data = DistributionApplication.GetWithdraws(query);
            var list = data.Models.Select(p => new
            {
                p.Amount,
                Id = p.Id.ToString(),
                Time = p.ApplyTime.ToString("yyyy-MM-dd HH:mm:ss"),
                WithdrawType = p.WithdrawType.ToDescription(),
                Status = p.WithdrawStatus,
                WithdrawStatus = p.WithdrawStatus.ToDescription()
            });
            return JsonResult<dynamic>(new { rows = list, total = data.Total });
        }
        /// <summary>
        /// 设置支付密码
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public JsonResult<Result<bool>> PostInitPayPassowrd(PostSetPasswordModel model)
        {
            CheckUserLogin();
            if (MemberApplication.HasPayPassword(CurrentUser.Id))
                return Json(ErrorResult<bool>("已设置过支付密码！"));
            MemberApplication.ChangePayPassword(CurrentUser.Id, model.password);
            return JsonResult(true);
        }

        /// <summary>
        /// 获取小店订单
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetShopOrder(int pageNo, int pageSize = 5)
        {
            CheckUserLogin();
            DistributionBrokerageQuery query = new DistributionBrokerageQuery
            {
                PageNo = pageNo,
                PageSize = pageSize,
                DistributorId = CurrentUser.Id
            };
            var data = DistributionApplication.GetDistributorBrokerageOrderList(query);
            var result = new { rows = data.Models, total = data.Total };
            return JsonResult<dynamic>(result);
        }
        /// <summary>
        /// 我的下级
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetMySubordinateLevel()
        {
            CheckUserLogin();
            var settings = SiteSettingApplication.SiteSettings;
            var subordinate = DistributionApplication.GetSubordinate(CurrentUser.Id);
            var model = new MySubordinateViewModel
            {
                MaxLevel = settings.DistributionMaxLevel,
                Levels = new Dictionary<int, MySubordinateLevelViewModel>()
            };
            if (model.MaxLevel >= 1)
            {
                model.Levels.Add(1, new MySubordinateLevelViewModel
                {
                    Name = settings.DistributorRenameMemberLevel1,
                    Count = subordinate[1].Count
                });
            }
            if (model.MaxLevel >= 2)
            {
                model.Levels.Add(2, new MySubordinateLevelViewModel
                {
                    Name = settings.DistributorRenameMemberLevel2,
                    Count = subordinate[2].Count
                });
            }
            if (model.MaxLevel >= 3)
            {
                model.Levels.Add(3, new MySubordinateLevelViewModel
                {
                    Name = settings.DistributorRenameMemberLevel3,
                    Count = subordinate[3].Count
                });
            }
            return JsonResult<dynamic>(model);
        }

        /// <summary>
        /// 查询我的下级明细
        /// </summary>
        /// <param name="level"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetMySubordinateRecords(int level, int pageSize, int pageNo)
        {
            CheckUserLogin();
            DistributorSubQuery query = new DistributorSubQuery();
            query.Level = level;
            query.PageSize = pageSize;
            query.PageNo = pageNo;
            query.SuperiorId = CurrentUser.Id;
            query.IsAll = true;
            var data = DistributionApplication.GetDistributors(query);
            var list = data.Models.Select(item => new
            {
                MemberName = item.Member.DistributionShowNick,
                TotalCount = item.Achievement?.TotalCount ?? 0,
                TotalAmount = item.Achievement?.TotalAmount ?? 0,
                RegTime = item.Member.CreateDate.ToString("yyyy-MM-dd"),
                Photo = HimallIO.GetRomoteImagePath(item.Member.Photo)
            }).ToList();

            return JsonResult<dynamic>(new { rows = list, total = data.Total });
        }

        /// <summary>
        /// 获取分销商品
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetProductList(string productName = null, long? categoryId = null, int pageSize = 10, int pageNo = 1)
        {
            CheckUserLogin();
            //查询条件
            DistributionProductQuery query = new DistributionProductQuery();
            query.ProductName = productName;
            query.CategoryId = categoryId;
            query.Status = DistributionProductStatus.Normal;
            query.PageSize = pageSize;
            query.PageNo = pageNo;
            query.Sort = "SaleCount";
            query.IsAsc = false;

            var data = DistributionApplication.GetProducts(query);

            var result = new { rows = data.Models, total = data.Total };
            return JsonResult<dynamic>(result);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<bool>> PostSaveShopConfig(PostSaveShopConfigModel model)
        {
            CheckUserLogin();
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(CurrentUser.Id);
            if (string.IsNullOrWhiteSpace(currentDistributor.ShopLogo) && string.IsNullOrWhiteSpace(model.logoUrl))
            {
                return Json(ErrorResult<bool>("请上传小店logo！"));
            }
            if (string.IsNullOrWhiteSpace(model.shopName))
            {
                return Json(ErrorResult<bool>("请填写小店名称！"));
            }
            if (model.shopName.Length > 10)
            {
                return Json(ErrorResult<bool>("小店名称不能超过10个字符！"));
            }
            string shoplogo = currentDistributor.ShopLogo;
            if (!string.IsNullOrWhiteSpace(model.logoUrl))
            {
                //shoplogo = DownloadWxImage(model.logoWXmediaId);
                shoplogo = model.logoUrl;
            }
            if (string.IsNullOrWhiteSpace(shoplogo))
            {
                return Json(ErrorResult<bool>("请上传小店logo！"));
            }
            var oldname = Path.GetFileName(shoplogo);
            string ImageDir = string.Empty;

            if (!string.IsNullOrWhiteSpace(shoplogo))
            {
                //转移图片
                string relativeDir = DISTRIBUTOR_LOGO_PATH;
                Random ra = new Random();
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + ra.Next(4) + ".jpg";
                if (shoplogo.Replace("\\", "/").Contains("/temp/"))//只有在临时目录中的图片才需要复制
                {
                    var de = shoplogo.Substring(shoplogo.LastIndexOf("/temp/"));
                    Core.HimallIO.CopyFile(de, relativeDir + fileName, true);
                    shoplogo = relativeDir + fileName;
                }  //目标地址
                else if (shoplogo.Contains("/Storage"))
                {
                    shoplogo = shoplogo.Substring(shoplogo.LastIndexOf("/Storage"));
                }
            }
            DistributionApplication.UpdateDistributorConfig(currentDistributor.MemberId, shoplogo, model.shopName, model.isShowLogo);
            return JsonResult(true);
        }

        /// <summary>
        /// 获取分销市场分类
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetMarketCategory()
        {
            var categorys = DistributionApplication.GetHaveDistributionProductTopCategory();
            return JsonResult<dynamic>(categorys);
        }


        public JsonResult<Result<dynamic>> GetShopHeader(long distributorId)
        {
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(distributorId);
            if (currentDistributor == null)
            {
                return Json(ErrorResult<dynamic>("分销员不存在！"));
            }
            dynamic result = new System.Dynamic.ExpandoObject();
            result.MemberId = currentDistributor.MemberId;
            result.ShopLogo = currentDistributor.ShopLogo;
            result.ShopName = currentDistributor.ShopName;
            result.ShopLogo = Himall.Core.HimallIO.GetRomoteImagePath(result.ShopLogo);
            result.IsShowLogo = currentDistributor.IsShowShopLogo;
            result.IsOpenDistributor = SiteSettingApplication.SiteSettings.DistributionIsEnable;
            return JsonResult<dynamic>(result);
        }

        /// <summary>
        /// 我的名片
        /// </summary>
        /// <param name="distributorId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetBusinessCard()
        {
            CheckUserLogin();

            decimal qrCodePositionX = 0, qrCodePositionY = 0, qrCodeWidth = 0;
            string referralPosterUrl = "";
            //try
            //{
            referralPosterUrl = DistributionApplication.GetAppletReferralPostUrl(CurrentUser, out qrCodePositionX, out qrCodePositionY, out qrCodeWidth).Replace("http://", "https://");
            //}
            //catch(Exception ex)
            //{
            //    Log.Error(ex.Message);
            //}

            dynamic result = new System.Dynamic.ExpandoObject();
            result.ReferralPosterUrl = referralPosterUrl;
            result.QrCodePositionX = qrCodePositionX;
            result.QrCodePositionY = qrCodePositionY;
            result.QrCodeWidth = qrCodeWidth;
            return JsonResult<dynamic>(result);
        }
    }
}
