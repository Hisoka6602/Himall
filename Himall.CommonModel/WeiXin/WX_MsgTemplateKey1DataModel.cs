﻿namespace Himall.CommonModel.WeiXin
{
    /// <summary>
    /// 微信模板信息模型
    /// <para>keyword请按顺序对应，如果无内容保持空</para>
    /// </summary>
    public class WX_MsgTemplateKey1DataModel
    {
        public WX_MsgTemplateKey1DataModel()
        {
            this.first = new WX_MSGItemBaseModel();
            this.keyword1 = new WX_MSGItemBaseModel();
            this.remark = new WX_MSGItemBaseModel();
        }
        public WX_MSGItemBaseModel first { get; set; }
        public WX_MSGItemBaseModel keyword1 { get; set; }
        public WX_MSGItemBaseModel remark { get; set; }
    }
}
