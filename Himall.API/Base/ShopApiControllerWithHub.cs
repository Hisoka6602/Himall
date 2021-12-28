using System;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Himall.API.Base
{
    public abstract class ShopApiControllerWithHub<THub> : BaseShopLoginedApiController
     where THub : IHub
    {
        protected IHubConnectionContext<dynamic> Clients { get; private set; }
        protected IGroupManager Groups { get; private set; }
        protected ShopApiControllerWithHub()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<THub>();
            Clients = context.Clients;
            Groups = context.Groups;
        }
    }
}
