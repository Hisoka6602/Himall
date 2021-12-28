$(function () {
    var lourl = location.pathname.toLowerCase();
    var url = (lourl == '/m-weixin' || lourl == '/m-wap') ? ('/' + areaName + '/CustomerServices/PlatCustomerServices') : "";//首页调用平台客服
    if (url == "" && $("#shopId").val() != null && $("#shopId").val() != "" && (lourl.indexOf("/limittimebuy/detail") != -1 || lourl.indexOf("/product/detail") != -1 || lourl.indexOf("/comment/appendcomment")!=-1)) {//商品详细页以及评论调用商家客服
        url = '/' + areaName + '/CustomerServices/ShopCustomerServices?shopId=' + $("#shopId").val() + "&productId=" + $("#gid").val();
    }
    if (url.length > 0) {
		$.ajax({
			url: url,
			async: false,
			success: function (data) {		
                $("#_Aside_CustomServices").html(data);
                if ($("#sh_poster").length > 0 && data != null && data.length>40) {//商品详细页才有(它有空格等字符大概32长度，所以用40判断)
                    $("#sh_poster").css("bottom", "210px");
                }
			}
		});
    } else {
        if ($("#sh_poster").length > 0) {
            var servicehtml = $("#_Aside_CustomServices .service-online").html();
            if (servicehtml != null && servicehtml.length > 40)
                $("#sh_poster").css("bottom", "210px");//有客户则海报图上移
        }
    }
    $("#_Aside_CustomServices span.lb-1 i").after("<em style='color:#eee; display:inline-display'>客服</em>");


    $("#two-service").toggle(function() {
        var qq_len = $(".s-line>a>span").length;
        if (qq_len > 0) {
            $(".s-line").css("visibility", "visible");
            $(".line-btn").css("background-color", "rgba(1,21,25,.5)")
            $(".line-btn em").css("display", "none");
            $(".line-btn i").css("margin-top", "7px")
        }
    }, function() {
        $(".s-line").css("visibility", "hidden");
        $(".line-btn").css("background-color", "rgba(1,21,25,.24)")
        $(".line-btn em").css("display", "block");
        $(".line-btn i").css("margin-top", "0")
    });
});


function OpenHiChat(chaturl) {
    checkLogin(location.href,function () {
        window.open("/" + areaName + chaturl);
    });
}
