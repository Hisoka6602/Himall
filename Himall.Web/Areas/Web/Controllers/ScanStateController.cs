using Himall.CommonModel;
using Himall.Entities;
using Himall.Web.Framework;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{

    public class ScanStateController : BaseAsyncController
    {
        public void GetStateAsync(string sceneid)
        {
            AsyncManager.OutstandingOperations.Increment();
            int interval = 200;//定义刷新间隔为200ms
            int maxWaitingTime = 10 * 1000;//定义最大等待时间为10s
            Task.Factory.StartNew(() =>
            {
                int time = 0;
                while (true)
                {
                    var key = CacheKeyCollection.SceneReturn(sceneid);
                    var obj = Core.Cache.Get<ApplyWithDrawInfo>(key);
                    if (obj != null)
                    {
                        AsyncManager.Parameters["state"] = true;
                        AsyncManager.Parameters["model"] = obj;
                        break;
                    }
                    else
                    {
                        if (time >= maxWaitingTime)
                        {
                            AsyncManager.Parameters["state"] = false;
                            AsyncManager.Parameters["model"] = obj;
                            break;
                        }
                        else
                        {
                            time += interval;
                            System.Threading.Thread.Sleep(interval);
                        }
                    }
                }
                AsyncManager.OutstandingOperations.Decrement();
            });
        }
        public JsonResult GetStateCompleted(bool state, ApplyWithDrawInfo model)
        {
            return Json(new { success = state, data = model }, JsonRequestBehavior.AllowGet);
        }

        public void GetStateToShopAsync(string sceneid)
        {
            AsyncManager.OutstandingOperations.Increment();
            int interval = 200;//定义刷新间隔为200ms
            int maxWaitingTime = 10 * 1000;//定义最大等待时间为10s
            Task.Factory.StartNew(() =>
            {
                int time = 0;
                while (true)
                {
                    var key = CacheKeyCollection.SceneReturn(sceneid);
                    var obj = Core.Cache.Get<Himall.DTO.WeiXinInfo>(key);
                    if (obj != null)
                    {
                        AsyncManager.Parameters["state"] = true;
                        AsyncManager.Parameters["model"] = obj;
                        break;
                    }
                    else
                    {
                        if (time >= maxWaitingTime)
                        {
                            AsyncManager.Parameters["state"] = false;
                            AsyncManager.Parameters["model"] = obj;
                            break;
                        }
                        else
                        {
                            time += interval;
                            System.Threading.Thread.Sleep(interval);
                        }
                    }
                }
                AsyncManager.OutstandingOperations.Decrement();
            });
        }
        public JsonResult GetStateToShopCompleted(bool state, Himall.DTO.WeiXinInfo model)
        {
            return Json(new { success = state, data = model }, JsonRequestBehavior.AllowGet);
        }
    }
}