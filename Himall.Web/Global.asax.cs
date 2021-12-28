using FluentValidation.Mvc;
using Himall.Core;
using Himall.Web.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Himall.Application;
using System.Collections.Specialized;
using Himall.Core.Tasks;
using Himall.Core.Tasks.Quartz;
using System.Configuration;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin.Entities;
using Senparc.Weixin;

namespace Himall.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static string WapIndexCache = "";
        public static ITaskCenter center;
        public override string GetVaryByCustomString(System.Web.HttpContext context, string custom)
        {
            switch (custom)
            {
                case "Home":

                    return WapIndexCache + "_" + context.Request.QueryString["ispv"] + "_" + context.Request.QueryString["tn"];
            }

            return "";
        }

        protected void Application_Start()
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("Areas/");

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            AreaRegistrationOrder.RegisterAllAreasOrder();
            BundleConfig.RegisterBundles(BundleTable.Bundles);



            RegistAtStart.RegistStrategies();
            ObjectContainer.ApplicationStart(new AutoFacContainer());
            RegistAtStart.RegistPlugins();
            MemberApplication.InitMessageQueue();

            //定时任务框架注册启动
            var taskEnable = ConfigurationManager.AppSettings["TaskEnable"];
            if (string.IsNullOrEmpty(taskEnable) || taskEnable.ToLower() == "true")
            {
                center = TaskCenterBuilder.Create()
                   .Register("Himall.Tasks")
                   .UseQuartz()
                   .Build();
                center.Start();
            }

            #region Senparc 微信功能注册
            /* CO2NET 全局注册开始
             * 建议按照以下顺序进行注册
             */

            //设置全局 Debug 状态
            var _senparc_global_debug = ConfigurationManager.AppSettings["senparc_global_debug"];
            var isGLobalDebug = (_senparc_global_debug != null && _senparc_global_debug != "false" && _senparc_global_debug != "0");
            var senparcSetting = SenparcSetting.BuildFromWebConfig(isGLobalDebug);

            //CO2NET 全局注册，必须！！
            IRegisterService register = RegisterService.Start(senparcSetting).UseSenparcGlobal(true, null);

            /* 微信配置开始
             * 建议按照以下顺序进行注册
             */

            //设置微信 Debug 状态
            var _senparc_weixin_debug = ConfigurationManager.AppSettings["senparc_weixin_debug"];
            var isWeixinDebug = (_senparc_weixin_debug != null && _senparc_weixin_debug != "false" && _senparc_weixin_debug != "0");
            var senparcWeixinSetting = SenparcWeixinSetting.BuildFromWebConfig(isWeixinDebug);

            //微信全局注册，必须！！
            register.UseSenparcWeixin(senparcWeixinSetting, senparcSetting);

            register.RegisterTraceLog(ConfigWeixinTraceLog);//配置TraceLog
            #endregion


            ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new CustomValidatorFactory()));
            DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false;
        }

        /// <summary>
        /// 配置微信跟踪日志
        /// </summary>
        private void ConfigWeixinTraceLog()
        {
            Senparc.CO2NET.Config.IsDebug = false;

            //这里设为Debug状态时，/App_Data/WeixinTraceLog/目录下会生成日志文件记录所有的API请求日志，正式发布版本建议关闭
            Senparc.Weixin.WeixinTrace.SendCustomLog("系统日志", "系统启动");//只在Senparc.Weixin.Config.IsDebug = true的情况下生效

            //自定义日志记录回调
            Senparc.Weixin.WeixinTrace.OnLogFunc = () =>
            {
                //加入每次触发Log后需要执行的代码
            };

            //当发生基于WeixinException的异常时触发
            Senparc.Weixin.WeixinTrace.OnWeixinExceptionFunc = ex =>
            {
                //加入每次触发WeixinExceptionLog后需要执行的代码
                Log.Error(ex);
            };
        }

        /// <summary>
        /// 自定义验证工厂
        /// </summary>
        public class CustomValidatorFactory : FluentValidation.ValidatorFactoryBase
        {
            public override FluentValidation.IValidator CreateInstance(Type validatorType)
            {
                var type = validatorType.GetGenericArguments()[0];
                var validatorAttribute = type.GetCustomAttribute<FluentValidation.Attributes.ValidatorAttribute>();
                if (validatorAttribute != null)
                {
                    //创建验证实体
                    var obj = System.Activator.CreateInstance(validatorAttribute.ValidatorType);
                    return obj as FluentValidation.IValidator;
                }

                return null;
            }
        }
        protected void Application_End()
        {
            #region 访问首页，重启数据池
            string hosturl = SiteSettingApplication.GetUrlHttpOrHttps(SiteSettingApplication.SiteSettings.SiteUrl);

#if DEBUG
            Himall.Core.Log.Info(System.DateTime.Now.ToString() + " -  " + hosturl);
#endif
            if (!string.IsNullOrWhiteSpace(hosturl))
            {
                System.Net.HttpWebRequest myHttpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(hosturl);
                System.Net.HttpWebResponse myHttpWebResponse = (System.Net.HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            #endregion
        }
        protected DateTime dt;
        protected void Application_BeginRequest(Object sender, EventArgs E)
        {
            dt = DateTime.Now;
            HttpContext.Current.Items["MysqlExecuted"] = 0;
            //Core.Log.Debug(dt.ToString("yyyy-MM-dd hh:mm:ss fff") + ":[当前请求URL：" + HttpContext.Current.Request.Url + "；请求的参数为：" + HttpContext.Current.Request.QueryString + "；页面开始时间：" + dt.ToString("yyyy-MM-dd hh:mm:ss fff")+"]");

        }
        protected void Application_EndRequest(Object sender, EventArgs E)
        {
            DateTime dt2 = DateTime.Now;
            TimeSpan ts = dt2 - dt;
            if (ts.TotalMilliseconds >= 5000)//5秒以上的慢页面进行记录
                Core.Log.Debug(dt2.ToString("yyyy-MM-dd hh:mm:ss fff") + ":[当前请求URL：" + HttpContext.Current.Request.Url + "；请求的参数为：" + HttpContext.Current.Request.QueryString + "；页面加载的时间：" + ts.TotalMilliseconds.ToString() + " 毫秒]");
            if (HttpContext.Current.Items.Contains("MysqlExecuted"))
            {
                var executed = (int)HttpContext.Current.Items["MysqlExecuted"];
                if (executed > 50)
                {
                    var msg = "MysqlExecuted:" + executed + ",耗时:" + ts.TotalMilliseconds.ToString() + "毫秒";
                    msg += "\r\n请求:" + HttpContext.Current.Request.Url;
                    Log.Info(msg);
                }
            }

        }
        //#endif
    }

    /// <summary>
    /// 性能查询
    /// </summary>
    public class ActionPerformance : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            GetTimer(filterContext, "action").Stop();

            base.OnActionExecuted(filterContext);
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            GetTimer(filterContext, "action").Start();

            base.OnActionExecuting(filterContext);
        }



        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {

            var renderTimer = GetTimer(filterContext, "render");
            renderTimer.Stop();

            var actionTimer = GetTimer(filterContext, "action");
            var response = filterContext.HttpContext.Response;

            if (response.ContentType == "text/html")
            {
                if (actionTimer.ElapsedMilliseconds >= 5000 || renderTimer.ElapsedMilliseconds >= 5000)
                {
                    string result = String.Format(
                         "{0}.{1}, Action执行时间: {2}毫秒, View执行时间: {3}毫秒.",
                         filterContext.RouteData.Values["controller"],
                         filterContext.RouteData.Values["action"],
                         actionTimer.ElapsedMilliseconds,
                         renderTimer.ElapsedMilliseconds
                     );
                    Himall.Core.Log.Debug(result);
                }
            }

            base.OnResultExecuted(filterContext);
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            GetTimer(filterContext, "render").Start();

            base.OnResultExecuting(filterContext);
        }

        private Stopwatch GetTimer(ControllerContext context, string name)
        {
            string key = "__timer__" + name;
            if (context.HttpContext.Items.Contains(key))
            {
                return (Stopwatch)context.HttpContext.Items[key];
            }

            var result = new Stopwatch();
            context.HttpContext.Items[key] = result;
            return result;
        }

    }
}
