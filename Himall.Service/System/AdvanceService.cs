using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Service
{
    public class AdvanceService : ServiceBase
    {

        public AdvanceInfo GetAdvance()
        {
            return DbFactory.Default.Get<AdvanceInfo>().FirstOrDefault();
        }

        public void AddAdvance(AdvanceInfo advanceInfo)
        {
            DbFactory.Default.Execute("delete from Himall_Advance");
            DbFactory.Default.Add(advanceInfo);
        }
    }
}
