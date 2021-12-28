
$(function () {
    checkPayDone();
});

function checkPayDone() {
    var orderIds = QueryString('orderIds');
    var type = QueryString('type') || '';
    if (type == 'charge') {
        $.getJSON('/PayState/CheckCharge', { orderIds: orderIds }, function (result) {
            if (result.success) {
                $.dialog.succeedTips('支付成功!', function () {
                    location.href = "/userCenter?url=%2fusercapital%2f&tar=usercapital";
                });
            }
            else {
                setTimeout(checkPayDone, 100);
            }
        });
    } else if (type == 'shop' || type == 'shopcashdeposit' || type == 'shopcharge') {
        var okurl = "/SellerAdmin/shop/Renew?url=%2fshop%2f&tar=shoprenewrecords";//店铺续费
        if (type == 'shopcashdeposit')
            okurl = "/SellerAdmin/CashDeposit/Management?url=%2fshop%2f&tar=cashok";//店铺交保证金
        if (type == 'shopcharge')
            okurl = "/SellerAdmin";//店铺交保证金
        $.getJSON('/PayState/CheckShop', { orderIds: orderIds }, function (result) {
            if (result.success) {
                $.dialog.succeedTips('支付成功!', function () {
                    location.href = okurl;
                });
            }
            else {
                setTimeout(checkPayDone, 100);
            }
        });
    }
    else {
        $.getJSON('/PayState/Check', { orderIds: orderIds }, function (result) {
            if (result.success) {
                $.dialog.succeedTips('支付成功!', function () {
                    urlparams = "/userorder?orderids=" + orderIds +"&tar=userorder";
                    location.href = "/userCenter?url=" + escape(urlparams);
                });
            }
            else {
                setTimeout(checkPayDone, 100);
            }
        });
    }
}

