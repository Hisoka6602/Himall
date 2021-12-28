﻿// JavaScript source code
$(function () {
    var ExpressCompanyName = $("#MECo").val();
    var ShipOrderNumber = $("#MSOn").val();
    var oid = $("#hidOrderId").val();
    if (ExpressCompanyName != "" & ShipOrderNumber != "") {
        // 物流信息
        $.post('/Common/ExpressData/Search', { expressCompanyName: ExpressCompanyName, shipOrderNumber: ShipOrderNumber, orderId: oid }, function (result) {
            var html = '';
            var obj = result;
            if (obj.success) {
                var data = obj.data;
                for (var i = data.length - 1; i >= 0; i--) {
                    html += '<div class="form-group clearfix"><label class="col-sm-4">' + data[i].time + '</label>\
                             <div class="col-sm-7 form-control-static">' + data[i].content + '</div>';
                    html += '</div>';
                }
            }
            else {
                if (ExpressCompanyName.indexOf('顺丰') == 0) {
                    html += '<div class="form-group clearfix" style=\"text-align:center;\">暂无物流信息，请稍后再试；如有信息推送不及时，请前往物流官网查询！<a class="express-link" href="http://www.sf-express.com/cn/sc/dynamic_function/waybill/#search/bill-number/' + ShipOrderNumber + '" target="_blank">顺丰官网查询</a></div>';
                } else {
                    html += '<div class="form-group clearfix" style=\"text-align:center;\">暂无物流信息，请稍后再试；如有信息推送不及时，请前往物流官网查询！<a href="https://www.kuaidi100.com/chaxun?nu=' + ShipOrderNumber + '" target="_blank">快递100查询</a></div>';
                }
            }

            //html += '<div class="form-group deli-link clearfix" style="top: -20px;"><a href="www.kuaidi100.com" target="_blank" id="power" runat="server" style="color:#F97616;display:none;">此物流信息由快递100提供</a></div>';
            $("#tbExpressData").append(html);
        });
    }
});

function showExpress() {
    $("#tbExpressData").show();
    location.href = "#delivery-detail";

}

$(function () {
    $(".detail-open").click(function () {
        var t_display = $("#tbExpressData").css("display");
        $("#tbExpressData").toggle();
        if (t_display == 'none') {
            $(".delivery-detail p span").css("background-position", "-223px 5px");
        } else {
            $(".delivery-detail p span").css("background-position", "-247px 5px");
        }

    });
    $(".list-open").click(function () {
        var l_display = $(".order-log .table").css("display");
        $(".order-log .table").toggle();
        if (l_display == 'none') {
            $(".order-log p span").css("background-position", "-223px 5px");
        } else {
            $(".order-log p span").css("background-position", "-247px 5px");
        }

    });
    if ($(".order-info .caption").height() > $(".delivery-info .caption").height()) {
        $(".delivery-info .caption").height($(".order-info .caption").height());
    } else {
        $(".order-info .caption").height($(".delivery-info .caption").height());
    }
    previewImg();
})
function previewImg() {
    $(document).on("click", ".after-service-img img", function () {
        $(".preview-img").show();
        $(".preview-img img").attr("src", $(this).attr("src"));
        $(".cover").show();
    });
    $(".preview-img").click(function () {
        $(this).hide()
        $(".cover").hide();
    });
    $(".cover").click(function () {
        $(".preview-img").hide();
        $(".cover").hide();
    })
}