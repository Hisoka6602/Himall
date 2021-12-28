﻿namespace Himall.CommonModel.WeiXin
{
    /// <summary>
    /// 微信模板信息模型
    /// <para>keyword请按顺序对应，如果无内容保持空</para>
    /// </summary>
    public class WX_MsgTemplateRefundDataModel
    {
        public WX_MsgTemplateRefundDataModel()
        {
            this.first = new WX_MSGItemBaseModel();
            this.orderProductPrice = new WX_MSGItemBaseModel();
            this.orderProductName = new WX_MSGItemBaseModel();
            this.orderName = new WX_MSGItemBaseModel();
            this.remark = new WX_MSGItemBaseModel();
        }
        public WX_MSGItemBaseModel first { get; set; }
        public WX_MSGItemBaseModel orderProductPrice { get; set; }
        public WX_MSGItemBaseModel orderProductName { get; set; }
        public WX_MSGItemBaseModel orderName { get; set; }
        public WX_MSGItemBaseModel remark { get; set; }
    }
}
