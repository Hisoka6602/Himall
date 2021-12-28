using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.Core;
using Himall.DTO;
using Himall.Service;

namespace Himall.Application
{

    public class SystemAgreementApplication
    {
        private static SystemAgreementService _iSystemAgreementService = ObjectContainer.Current.Resolve<SystemAgreementService>();

        /// <summary>
        /// 获取协议信息
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static Entities.AgreementInfo GetAgreement(Entities.AgreementInfo.AgreementTypes type)
        {
            return _iSystemAgreementService.GetAgreement(type);
        }

        /// <summary>
        /// 保存协议
        /// </summary>
        /// <param name="model"></param>
        public static bool SaveAgreement(Entities.AgreementInfo model)
        {
            bool result = true;
            if (model.Id <= 0)
                _iSystemAgreementService.AddAgreement(model); //添加协议
            else
                result = _iSystemAgreementService.UpdateAgreement(model); //修改协议

            return result;
        }
    }
}
