$(function () {
    var a;
    var b = ($(this), $("#vtclient").val());
    var c = ($(this), $("#vttype").val());
    $.ajax({
        url: HiShop.Config.AjaxUrl.getVshopPage,
        type: 'GET',
        dataType: 'text',
        data: {
            client: b,
            type: c,
            shopId: shopid
        },
        success: function (data) {
            a = data.length ? $.parseJSON(data) : Defaults[b];
            $(".j-pagetitle").text(a.page.title);
            $(".j-pagetitle-ipt").val(a.page.title);
            $(".j-pagetitle-tags").val(a.page.tags);
            $(".j-pagetitle-icon").val(a.page.icon);
            _.each(a.PModules,
             function (a, b) {
                 var c = 0 == b ? !0 : !1;
                 HiShop.DIY.add(a, c);
             });
            _.each(a.LModules,
            function (a) {
                HiShop.DIY.add(a);
            });
            //icon
            if ($(".j-pagetitle-icon").length > 0) {
                $("#IconUrlBox").himallUpload(
               {
                   title: '',
                   headerWidth: 0,
                   dataWidth: 10,
                   imageDescript: '建议700 * 360',
                   displayImgSrc: a.page.icon,
                   imgFieldName: "IconUrlPic",
                   callback: function (data) {
                       $(".j-pagetitle-icon").val(data);
                   }
               });
            }
        }
    }),

    $("#j-savePage").click(function () {
        return HiShop.DIY.Unit.getData() ? ($.ajax({
            url: HiShop.Config.AjaxUrl.savePage,
            type: "post",
            dataType: "json",
            data: {
                content: JSON.stringify(HiShop.DIY.Unit.getData()),
                client: $("#vtclient").val(),
                type: c,
                getGoodGroupUrl: HiShop.Config.CodeBehind.getGoodGroupUrl,
                getGoodUrl: HiShop.Config.CodeBehind.getGoodUrl,
                is_preview: 0,
                shopId: shopid
            },
            beforeSend: function () {
                $.jBox.showloading()
            },
            success: function (a) {
                if (1 == a.status) {
                    HiShop.hint("success", "恭喜您，保存成功！");
                    if (c == 11 || c == 12 || c == 15 || c == 16) {
                        window.opener && window.opener.location.reload();
                        $("#vtclient").val(a.tname);
                        //if (b.length == 0 || b == "" || b == "0") {
                        setTimeout(function () {
                            window.location.href = a.link;
                        }, 1000);
                        //}
                    }
                }
                else {
                    HiShop.hint("danger", "对不起，保存失败：" + a.msg)
                }
                $.jBox.hideloading();
                //处理专题新增的关闭
            }
        }), !1) : void 0
    })
});