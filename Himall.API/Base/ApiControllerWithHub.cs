using System;
using System.Web.Http;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;


namespace Himall.API.Base
{
    public abstract class ApiControllerWithHub<THub> : BaseShopBranchLoginedApiController
       where THub : IHub
    {
        protected IHubConnectionContext<dynamic> Clients { get; private set; }
        protected IGroupManager Groups { get; private set; }
        protected ApiControllerWithHub()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<THub>();
            Clients = context.Clients;
            Groups = context.Groups;
        }
    }
}
