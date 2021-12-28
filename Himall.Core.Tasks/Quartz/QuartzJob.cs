using Quartz;
using System;
namespace Himall.Core.Tasks.Quartz
{
    public class QuartzJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            var detail = context.MergedJobDataMap["TaskDetail"] as TaskDetail;
            try
            {
                detail.Handler.Invoke(null, null);
            }
            catch (HimallException ex)
            {
                //忽略业务异常
            }
            catch(Exception e)
            {
                Log.Error("自动化运行异常:" + detail.Name, e);
            }
        }

       
    }
}
