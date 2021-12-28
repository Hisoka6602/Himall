using Himall.Core;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Application
{
   public class AdvanceApplication
    {
        private static AdvanceService _AdvanceService = ObjectContainer.Current.Resolve<AdvanceService>();

        public static AdvanceInfo GetAdvanceInfo() {
            return _AdvanceService.GetAdvance();
        }


        public static void AddAdvance(AdvanceInfo advance) {

            _AdvanceService.AddAdvance(advance);
        }
    }
}
