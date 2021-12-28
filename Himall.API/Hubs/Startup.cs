using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Himall.API.Hubs.Startup))]
namespace Himall.API.Hubs
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //app.MapSignalR();
            //服务器的hub注册

            //消息总线--集线器Hub配置
            app.Map("/print", map => {
                //SignalR允许跨域调用
                //map.UseCors(CorsOptions.AllowAll);
                HubConfiguration config = new HubConfiguration()
                {
                    //禁用JavaScript代理
                    EnableJavaScriptProxies = false,
                    //启用JSONP跨域
                    EnableJSONP = true,
                    //反馈结果给客户端
                    EnableDetailedErrors = true
                };
                map.RunSignalR(config);
            });
            //WebApi允许跨域调用
            //app.UseCors(CorsOptions.AllowAll);
        }
    }
}
