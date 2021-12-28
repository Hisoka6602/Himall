/*!
 * HiShop 选择链接插件 
 * version: 1.0
 * build: 2015/9/25
 * author: CJZhao
 */

/*!
 *创建链接插件
 *ele:获取返回值的dom，请传入jquery obj类型
 *obj:创建在那个dom下（请传入jquery obj类型）
 *parameter:参数
 *parameter.createType 构造类型  1：在 obj 下追加 2：插入obj之前 3：插入到obj 之后
 *parameter.showTitle 是否显示“链接”两字
 *parameter.txtContinuity 返回值写入到ele中 false：清空再写 true：追加内容
 *parameter.reWriteSpan 是否将选中的内容  展示出来
 *parameter.iscallback 是否自定义回调
 *parameter.callback  回调函数
 *parameter.style DIV样式
 */
function CreateDropdown(ele, obj, parameter, clientName, isopenstore) {
    var tempHtml = "    <div class=\"form-group\" id =\"dropdow-menu-link\" style=\"" + parameter.style + "\" >\
                                <div class=\"control-action clearfix\">\
                                 <div class=\"pull-left js-link-to link-to\">";
    if (parameter.showTitle) {
        tempHtml += "<label ><em>&nbsp;</em>链接：</label>";
    }
    if (clientName == "vshop") {
        tempHtml += "<div class=\"dropdown\">\
                                        <a class=\"dropdown-toggle\" data-toggle=\"dropdown\" href=\"javascript:void(0);\">\
                                        <span id=\"spLinkTitle\">选择链接</span> <i class=\"caret\"></i></a>\
                                        <ul class=\"dropdown-menu\">\
                                            <li data-val=\"/m-wap\"><a href=\"javascript:;\">首页</a></li>\
                                            <li data-val=\"/m-wap/Vshop\"><a href=\"javascript:;\">微店列表</a></li>\
                                            <li data-val=\"/m-wap/Category\"><a href=\"javascript:;\">分类</a></li>\
                                            <li data-val=\"/m-wap/FightGroup\"><a href=\"javascript:;\">拼团列表</a></li>\
                                            <li data-val=\"/m-wap/LimitTimeBuy/Home\"><a href=\"javascript:;\">限时购列表</a></li>\
                                            <li data-val=\"/m-wap/Cart/Cart\"><a href=\"javascript:;\">购物车</a></li>\
                                            <li data-val=\"/m-wap/Member/Center\"><a href=\"javascript:;\">个人中心</a></li>\
                                            <li data-val=\"/m-wap/SignIn\"><a href=\"javascript:;\">签到</a></li>";
        if (isopenstore == 1) {
            tempHtml += "<li data-val=\"/m-wap/shopBranch/storelist\"><a href=\"javascript:;\">周边门店</a></li>";
        }
      
        tempHtml += "<li data-val=\"\"><a href=\"javascript:;\">自定义链接</a></li>\
                                        </ul>\
                                    </div>\
                                    </div>\
                                    </div>\
                            </div> ";
    } else if (clientName == "smallprog") {
        tempHtml += "<div class=\"dropdown\">\
                                        <a class=\"dropdown-toggle\" data-toggle=\"dropdown\" href=\"javascript:void(0);\">\
                                        <span id=\"spLinkTitle\">选择链接</span> <i class=\"caret\"></i></a>\
                                        <ul class=\"dropdown-menu\">\
                                            <li data-val=\"/pages/vShopHome/vShopHome\"><a href=\"javascript:;\">微店首页</a></li>\
                                            <li data-val=\"/pages/vShopCategory/vShopCategory\"><a href=\"javascript:;\">商品分类</a></li>\
                                            <li data-val=\"/pages/vShopProductList/vShopProductList\"><a href=\"javascript:;\">全部商品</a></li>\
                                            <li data-val=\"/pages/vShopIntroduce/vShopIntroduce\"><a href=\"javascript:;\">店铺简介</a></li>\
                                        </ul>\
                                    </div>\
                                    </div>\
                                    </div>\
                            </div> ";
    }
    if (parameter.createType == 1) {
        obj.append(_.template(tempHtml));
    } else if (parameter.createType == 2) {
        obj.before(_.template(tempHtml));
    } else if (parameter.createType == 3) {
        obj.after(_.template(tempHtml));
    }
    if (parameter.callback == undefined) {
        parameter.callback = function () {
            // var index = $(this).parents("li.ctrl-item-list-li").index();
            //alert($(this).data("val"));
            HiShop.popbox.dplPickerColletion({
                linkType: $(this).data("val"),
                callback: function (item, type) {
                    //ele.show();
                    var link = item.link;
                    if (link.indexOf('http') > -1) {
                        link = item.link;
                    } else {
                        link = "http://" + window.location.host + item.link;
                    }
                    if (parameter.txtContinuity) {
                        ele.val(ele.val() + "  " + link);
                    } else {
                        ele.val(link);
                    }
                    if (parameter.reWriteSpan) {
                        $("#spLinkTitle").html(item.title);
                    }
                    if (type == 16) {
                        ele.hide();
                        ele.val("")
                    }
                }
            });
        }
    }
    $("#dropdow-menu-link li").unbind("click");
    $("#dropdow-menu-link li").click(parameter.callback);

}

