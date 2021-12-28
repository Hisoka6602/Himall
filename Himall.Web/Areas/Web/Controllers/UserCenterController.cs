using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using ServiceStack.Messaging;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    //TODO:YZY好多Service
    public class UserCenterController : BaseMemberController
    {
        private MemberService _MemberService;
        private ProductService _ProductService;
        private CommentService _CommentService;
        private MemberCapitalService _MemberCapitalService;
        private OrderService _OrderService;
        //private MemberInviteService _iMemberInviteService;
        //private MemberIntegralService _iMemberIntegralService;
        private CartService _iCartService;
        public UserCenterController(
            MemberService MemberService,
            ProductService ProductService,
            CommentService CommentService,
            MemberCapitalService MemberCapitalService,
            OrderService OrderService,
            CartService CartService
            )
        {
            _MemberService = MemberService;
            _ProductService = ProductService;
            _CommentService = CommentService;
            _MemberCapitalService = MemberCapitalService;
            _OrderService = OrderService;
            _iCartService = CartService;

        }

        public ActionResult Index()
        {
            //TODO:个人中心改成单页后，将index框架页改成与home页一致
            return RedirectToAction("home");
        }

        public ActionResult Home()
        {
            UserCenterHomeModel viewModel = new UserCenterHomeModel();

            viewModel.userCenterModel = MemberApplication.GetUserCenterModel(CurrentUser.Id);
            viewModel.UserName = CurrentUser.Nick == "" ? CurrentUser.UserName : CurrentUser.Nick;
            viewModel.Logo = CurrentUser.Photo;
            var items = _iCartService.GetCart(CurrentUser.Id).Items.OrderByDescending(a => a.AddTime).Select(p => p.ProductId).Take(3).ToArray();
            viewModel.ShoppingCartItems = ProductManagerApplication.GetProductByIds(items).ToArray();
            var UnEvaluatProducts = _CommentService.GetUnEvaluatProducts(CurrentUser.Id).ToArray();
            viewModel.UnEvaluatProductsNum = UnEvaluatProducts.Count();
            viewModel.Top3UnEvaluatProducts = UnEvaluatProducts.Take(3).ToArray();
            viewModel.Top3RecommendProducts = _ProductService.GetPlatHotSaleProductByNearShop(8, CurrentUser.Id).ToArray();
            viewModel.BrowsingProducts = BrowseHistrory.GetBrowsingProducts(4, CurrentUser == null ? 0 : CurrentUser.Id);

            var messagePlugins = PluginsManagement.GetPlugins<IMessagePlugin>();
            var data = messagePlugins.Select(item => new PluginsInfo
            {
                ShortName = item.Biz.ShortName,
                PluginId = item.PluginInfo.PluginId,
                Enable = item.PluginInfo.Enable,
                IsSettingsValid = item.Biz.IsSettingsValid,
                IsBind = !string.IsNullOrEmpty(MessageApplication.GetDestination(CurrentUser.Id, item.PluginInfo.PluginId, Entities.MemberContactInfo.UserTypes.General))
            });
            viewModel.BindContactInfo = data;

            var statistic = StatisticApplication.GetMemberOrderStatistic(CurrentUser.Id);
            viewModel.OrderCount = statistic.OrderCount;
            viewModel.OrderWaitReceiving = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(CurrentUser.Id);
            viewModel.OrderWaitPay = statistic.WaitingForPay;
            viewModel.OrderEvaluationStatus = statistic.WaitingForComments;
            viewModel.Balance = MemberCapitalApplication.GetBalanceByUserId(CurrentUser.Id);
            //TODO:[YZG]增加账户安全等级
            MemberAccountSafety memberAccountSafety = new MemberAccountSafety
            {
                AccountSafetyLevel = 1
            };
            if (CurrentUser.PayPwd != null)
            {
                memberAccountSafety.PayPassword = true;
                memberAccountSafety.AccountSafetyLevel += 1;
            }
            foreach (var messagePlugin in data)
            {
                if (messagePlugin.PluginId.IndexOf("SMS") > 0)
                {
                    if (messagePlugin.IsBind)
                    {
                        memberAccountSafety.BindPhone = true;
                        memberAccountSafety.AccountSafetyLevel += 1;
                    }
                }
                else
                {
                    if (messagePlugin.IsBind)
                    {
                        memberAccountSafety.BindEmail = true;
                        memberAccountSafety.AccountSafetyLevel += 1;
                    }
                }
            }
            viewModel.memberAccountSafety = memberAccountSafety;
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(viewModel);
        }

        public ActionResult Bind(string pluginId)
        {
            var messagePlugin = PluginsManagement.GetPlugin<IMessagePlugin>(pluginId);
            ViewBag.ShortName = messagePlugin.Biz.ShortName;
            ViewBag.id = pluginId;
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }

        [HttpPost]
        public ActionResult SendCode(string pluginId, string destination, bool checkBind = false)
        {
            if (checkBind && MessageApplication.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
            {
                return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
            }
            _MemberService.CheckContactInfoHasBeenUsed(pluginId, destination);
            var user = new MessageUserInfo() { UserName = CurrentUser.UserName};
            MessageApplication.SendMessageCode(destination, pluginId, user);
            return Json(new Result() { success = true, msg = "发送成功" });
        }

        [HttpPost]
        public ActionResult CheckCode(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            var member = CurrentUser;
            var mark = "";
            if (cacheCode != null && cacheCode == code)
            {
                if (MessageApplication.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
                {
                    return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
                }
                if (pluginId.ToLower().Contains("email"))
                {
                    member.Email = destination;
                    mark = "邮箱";
                }
                else if (pluginId.ToLower().Contains("sms"))
                {
                    member.CellPhone = destination;
                    mark = "手机";
                }

                _MemberService.UpdateMember(member);
                MessageApplication.UpdateMemberContacts(new Entities.MemberContactInfo()
                {
                    Contact = destination,
                    ServiceProvider = pluginId,
                    UserId = CurrentUser.Id,
                    UserType = Entities.MemberContactInfo.UserTypes.General
                });
                MessageApplication.RemoveMessageCacheCode(destination,pluginId);

                Himall.Entities.MemberIntegralRecordInfo info = new Himall.Entities.MemberIntegralRecordInfo();
                info.UserName = member.UserName;
                info.MemberId = member.Id;
                info.RecordDate = DateTime.Now;
                info.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Reg;
                info.ReMark = "绑定" + mark;
                var memberIntegral = ObjectContainer.Current.Resolve<MemberIntegralConversionFactoryService>().Create(Himall.Entities.MemberIntegralInfo.IntegralType.Reg);
                ObjectContainer.Current.Resolve<MemberIntegralService>().AddMemberIntegral(info, memberIntegral);


                var inviteMember = _MemberService.GetMember(member.InviteUserId);
                if (inviteMember != null)
                    ObjectContainer.Current.Resolve<MemberInviteService>().AddInviteIntegel(member, inviteMember);

                return Json(new Result() { success = true, msg = "验证正确" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确或者已经超时" });
            }
        }

        public ActionResult Finished()
        {
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }

        public ActionResult AccountSafety()
        {
            MemberAccountSafety model = new MemberAccountSafety();
            model.AccountSafetyLevel = 1;
            if (CurrentUser.PayPwd != null)
            {
                model.PayPassword = true;
                model.AccountSafetyLevel += 1;
            }
            var messagePlugins = PluginsManagement.GetPlugins<IMessagePlugin>();
            var data = messagePlugins.Select(item => new PluginsInfo
            {
                ShortName = item.Biz.ShortName,
                PluginId = item.PluginInfo.PluginId,
                Enable = item.PluginInfo.Enable,
                IsSettingsValid = item.Biz.IsSettingsValid,
                IsBind = !string.IsNullOrEmpty(MessageApplication.GetDestination(CurrentUser.Id, item.PluginInfo.PluginId, Entities.MemberContactInfo.UserTypes.General))
            });
            foreach (var messagePlugin in data)
            {
                if (messagePlugin.PluginId.IndexOf("SMS") > 0)
                {
                    if (messagePlugin.IsBind)
                    {
                        model.BindPhone = true;
                        model.AccountSafetyLevel += 1;
                    }
                }
                else
                {
                    if (messagePlugin.IsBind)
                    {
                        model.BindEmail = true;
                        model.AccountSafetyLevel += 1;
                    }
                }
            }
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(model);
        }
    }
}
