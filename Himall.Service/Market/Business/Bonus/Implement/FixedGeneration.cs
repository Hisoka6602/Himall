using Himall.Entities;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Himall.Service.Market.Business
{
    class FixedGeneration : IGenerateDetail
    {
        private readonly decimal _fixedAmount = 0;

        public FixedGeneration(decimal fixedAmount)
        {
            this._fixedAmount = fixedAmount;
        }

        public void Generate(long bounsId, decimal totalPrice)
        {
            try
            {
                DbFactory.Default
                    .InTransaction(() =>
                    {
                        var flag = true;
                        //红包个数
                        int detailCount = (int)(totalPrice / this._fixedAmount);
                        //生成固定数量个红包
                        StringBuilder sql = new StringBuilder();
                        for (int i = 0; i < detailCount; i++)
                        {
                            BonusReceiveInfo detail = new BonusReceiveInfo
                            {
                                BonusId = bounsId,
                                Price = this._fixedAmount,
                                IsShare = false,
                                OpenId = null
                            };
                            sql.AppendFormat(" insert into himall_bonusreceive (BonusId,Price,IsShare,IsTransformedDeposit)values({0},{1},false,false);", bounsId, this._fixedAmount);
                        }
                        flag = DbFactory.Default.Execute(sql.ToString()) > 0;
                        return flag;
                    });
            }
            catch (Exception ex)
            {
                Core.Log.Error(ex.Message, ex);
            }
        }
    }
}
