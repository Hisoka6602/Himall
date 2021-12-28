using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Linq;

namespace Himall.Service
{
    public class SystemAgreementService : ServiceBase
    {
        /// <summary>
        /// 获取协议信息
        /// </summary>
        /// <param name="Id">协议类型</param>
        /// <returns></returns>
        public AgreementInfo GetAgreement(AgreementInfo.AgreementTypes type)
        {
            return DbFactory.Default.Get<AgreementInfo>().Where(b => b.AgreementType == type).FirstOrDefault();
        }
        /// <summary>
        /// 添加协议信息
        /// </summary>
        /// <param name="model">协议信息</param>
        public void AddAgreement(AgreementInfo model)
        {
            model.LastUpdateTime = DateTime.Now;
            DbFactory.Default.Add(model);
        }

        /// <summary>
        /// 修改协议信息
        /// </summary>
        /// <param name="model">协议信息</param>
        public bool UpdateAgreement(AgreementInfo model)
        {
            var agreement = GetAgreement(model.AgreementType);
            agreement.AgreementType = model.AgreementType;
            agreement.AgreementContent = model.AgreementContent;
            agreement.LastUpdateTime = DateTime.Now;

            return DbFactory.Default.Update(agreement) > 0;
        }
    }
}
