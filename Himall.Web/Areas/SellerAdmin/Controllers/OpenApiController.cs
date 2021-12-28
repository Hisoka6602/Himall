using Himall.Core;
using Himall.Service;
using Himall.Web.Framework;
using System.Web.Mvc;


namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    /// <summary>
    /// OpenApi控制器
    /// </summary>
    public class OpenApiController : BaseSellerController
    {
        private ShopOpenApiService _iShopOpenApiService;
        private string erpuri = "";
        private string currootdomain = "";
        private long CurShopId;

        public OpenApiController(ShopOpenApiService ShopOpenApiService)
        {
            _iShopOpenApiService = ShopOpenApiService;
            if (CurrentSellerManager != null)
            {//退出登录后，直接进入controller异常处理
                    CurShopId = CurrentSellerManager.ShopId;
            }
            erpuri = System.Configuration.ConfigurationManager.AppSettings["HishopErpUri"];
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            currootdomain = Himall.Application.SiteSettingApplication.GetCurDomainUrl() + "/OpenApi/";
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            var data = _iShopOpenApiService.Get(CurShopId);
            if (data == null)
            {
                data = _iShopOpenApiService.MakeOpenApi(CurShopId);
                _iShopOpenApiService.Add(data);
            }
            return View(data);
        }
        /// <summary>
        /// 开启关闭
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SetEnableState(bool state)
        {
            Result result = new Result() { success = false };
            _iShopOpenApiService.SetEnableState(CurShopId, state);
            result.success = true;
            return Json(result);
        }
        /// <summary>
        /// 注册结果更新
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SetRegisterState()
        {
            Result result = new Result() { success = false };
            var data = _iShopOpenApiService.Get(CurShopId);
            if (data == null)
            {
                throw new HimallException("未初始开放平台");
            }
            string postdata = string.Format("appKey={0}&appSecret={1}&routeAddress={2}", data.AppKey, data.AppSecreat, currootdomain);
            string rdata = Hishop.Open.Api.OpenApiSign.PostData(erpuri, postdata);
            bool isreged = false;
            string strmes = string.Empty;
            if (!string.IsNullOrWhiteSpace(rdata))
            {
                if (rdata.Replace(" ", "").ToLower().IndexOf("\"seccuss\":true") > -1)
                {
                    isreged = true;
                }
                else
                {
                    Log.Debug("[OpenApi]" + rdata + "__" + postdata);

                    var erprdata = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(rdata);
                    strmes = erprdata == null ? strmes : (erprdata["msg"] + "");
                }

            }
            if (isreged)
            {
                _iShopOpenApiService.SetRegisterState(CurShopId, true);
            }
            else
            {
                _iShopOpenApiService.SetRegisterState(CurShopId, false);
                result.msg = "远程注册异常!" + strmes;
            }
            result.success = isreged;
            return Json(result);
        }
    }
}