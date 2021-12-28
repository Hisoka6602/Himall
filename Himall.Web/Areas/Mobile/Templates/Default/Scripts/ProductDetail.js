﻿function getProductId() {
    return $('#gid').val();
}

//关注
$(function () {
    var pid = getProductId();
    var categoryId = $('#categoryId').val();
    $('.plus-one').text(0).hide();
    //var addFavorite = QueryString('addFavorite');
    //if (addFavorite)
    //    addFavoriteFun();

    returnFavoriteHref = "/" + areaName + "/Product/Detail/" + pid;
    //returnFavoriteHref = encodeURIComponent(returnFavoriteHref);

    // 获取商品价格
    GetNeedRefreshProductInfo();
    ShowPromotion();
    LoadActives();
    loadProductCommentShow();


    $('#colorSpec .enabled').click(function () {
        if ($(this).data('img') != '') {
            $('.thumb img')[0].src = $(this).data('img');
        }
    });

    LogProduct();

    $("#bt_govshop").click(function () {
        var _t = $(this);
        var vshopid = _t.attr("vshopid");
        if (vshopid.length < 1) {
            vshopid = -1;
        }
        vshopid = Number(vshopid);
        if (isNaN(vshopid)) {
            vshopid = -1;
        }
        if (vshopid < 1) {
            $.dialog.errorTips("商家暂未开通微店！");
            return false;
        }
    });

    //获取推荐商品
    $.get("/" + areaName + '/product/GetHotProduct?productId={0}&categoryId={1}'.format(pid, categoryId), function (data) {
        if (data && data.length > 0) {
            var relationProducts = $('#relationProducts');
            if (relationProducts.length > 0) {

                var template = relationProducts.children('template').html();
                var html = '';
                for (var i = 0; i < data.length; i++) {
                    var product = data[i];
                    if (product.id != pid) {//不显示当前商品
                        html += template.formatProperty(product);
                    }
                }

                relationProducts.children(':not(template)').remove();
                relationProducts.append(html);
                if (html.length > 0) {//有推荐商品，则显示
                    relationProducts.parent().show();
                }
            }
        }
    });

    //开启阶梯价不显示门店信息
    $.get('/' + areaName + '/product/GetIsOpenLadder?productId={0}'.format(pid), function (result) {
        if (!result.data.isOpenLadder) {
            getIsOpenStore(); //授权了门店，获取用户定位,取距离用户最近门店
        }
    });
    $(".cover").on('click', function () {
        $(".cover").hide();
    });
    $(".goods-info").on('click', "#dis-share", function () {//分享按钮
        $(".cover").show();
    });
})
function getIsOpenStore() {
    $.get('/' + areaName + '/product/GetIsOpenStore', function (result) {
        if (result.success == true) {
            if (result.data.isOpenStore == true) {
                var mapkey = $("#hidQQMapKey").val();
                if (mapkey == null || mapkey == "")
                    mapkey = "SYJBZ-DSLR3-IWX3Q-3XNTM-ELURH-23FTP";
                var geolocation = new qq.maps.Geolocation(mapkey, "myapp");
                if (geolocation) {
                    geolocation.getLocation(getPositionSuccess, ShowError)
                }
                else {
                    $.dialog.tips("请在系统设置中打开“定位服务“允许Himall商城获取您的位置");
                }
                /*if (navigator.geolocation){ 
                    navigator.geolocation.getCurrentPosition(function(position){
                        var lat = position.coords.latitude; //纬度 
                        var lag = position.coords.longitude; //经度 
                        fromLatLng = lat+','+lag;
                        GetStoresInfo(fromLatLng);
                                GetIsSelfDelivery(fromLatLng);
                    },function(error){
                        switch(error.code) { 
                            case error.PERMISSION_DENIED: 
                              console.log("定位失败,用户拒绝请求地理定位"); 
                              break; 
                            case error.POSITION_UNAVAILABLE: 
                              console.log("定位失败,位置信息是不可用"); 
                              break; 
                            case error.TIMEOUT: 
                              console.log("定位失败,请求获取用户位置超时"); 
                              break; 
                            case error.UNKNOWN_ERROR: 
                              console.log("定位失败,定位系统失效"); 
                              break; 
                        } 
                    });
                }else{
                    $.dialog.tips("该浏览器不支持地理定位");
                }*/
            }
        }
    });
}
//获取定位
function getPositionSuccess(position) {
    var lat = position.lat;
    var lng = position.lng;
    var fromLatLng = lat + ',' + lng;
    GetStoresInfo(fromLatLng);
    GetIsSelfDelivery(fromLatLng);
}
//定位错误
function ShowError(error) {
    if (error && error.code) {
        switch (error.code) {
            case error.PERMISSION_DENIED:
                break;
            case error.POSITION_UNAVAILABLE:
                break;
            case error.TIMEOUT:
                break;
            case error.UNKNOWN_ERROR:
                break;
        }
    }
}
//获取门店信息
function GetStoresInfo(fromLatLng) {
    if (fromLatLng == "" || fromLatLng == undefined) {
        $.dialog.tips('无法获取您的当前位置，请确认是否开启定位服务');
        return;
    }
    var shopId = $("#shopId").val();
    var pid = getProductId();
    $.get('/' + areaName + '/product/GetStroreInfo?productId={0}&shopId={1}&fromLatLng={2}'.format(pid, shopId, fromLatLng), function (result) {
        if (result.success == true) {
            var data = result.data;
            if (typeof (data.storeInfo) != "undefined" && data.storeInfo != null) {
                $(".j_storeName").html(data.storeInfo.shopBranchName);
                $(".j_storesInfo h1").click(function () {
                    window.location.href = '/m-wap/shopbranch/Index/' + data.storeInfo.id + '?productId=' + pid;
                });
                $(".j_storelist").click(function () {
                    window.location.href = '/m-wap/shopbranch/productstorelist/' + pid + '?shopId=' + shopId;
                });
                $(".j_distanceUnit").html("(" + data.storeInfo.distanceUnit + ")");
                $(".j_storeAddress").html(data.storeInfo.addressDetail);
                $(".j_storelist").html("查看全部" + data.total + "家门店");
                $(".j_storesInfo").show();
            }
        } else {
            $.dialog.tips(result.msg);
        }
    });
}
//获取是否允许到店自提图标
function GetIsSelfDelivery(fromLatLng) {
    if (fromLatLng == "" || fromLatLng == undefined) {
        $.dialog.tips('无法获取您的当前位置，请确认是否开启定位服务');
        return;
    }
    var shopId = $("#shopId").val();
    var pid = $("#productId").val();
    $.get('/' + areaName + '/product/GetIsSelfDelivery?productId={0}&shopId={1}&fromLatLng={2}'.format(pid, shopId, fromLatLng), function (result) {
        var data = result.data;
        if (typeof (data) != "undefined" && data != null && data == 1) {
            $(".j_ensary").append('<li class="flex-center"><i>到店自提</i></li>');
        }
    });
}
function GetNeedRefreshProductInfo() {
    var shopId = $("#shopId").val();
    var gid = $('#gid').val();
    $.ajax({
        type: 'post',
        url: '/' + areaName + '/product/GetNeedRefreshProductInfo',
        data: { id: gid, shopId: shopId },
        dataType: 'json',
        cache: true,// 开启ajax缓存
        success: function (result) {
            if (result.success) {

                var winxinShareArgs = window.winxinShareArgs;

                if (winxinShareArgs && result.data && result.data.WeiXinShareArgs) {
                    winxinShareArgs = $.extend({
                        appId: result.data.WeiXinShareArgs.AppId,
                        timestamp: result.data.WeiXinShareArgs.Timestamp,
                        noncestr: result.data.WeiXinShareArgs.NonceStr,
                        signature: result.data.WeiXinShareArgs.Signature
                    }, winxinShareArgs);
                    //初始化微信分享
                    initWinxin(winxinShareArgs);
                } else if (loading) {
                    loading.close();//关闭预加载弹出
                }

                var data = result.data;
                var actionBarHtml = "";
                if (data.isSaleCountOnOff == false) {
                    $("#saleCounts").parent().hide();
                } else {
                    $("#saleCounts").text(data.saleCounts);
                }
                $("#freight").text(data.freight);
                $("#jd-saleprice").html("<sub>￥</sub>" + data.price);
                $("#poster-price").html(data.price);//海报里显示价格

                if (data.saleStatus == 1 && data.auditStatus == 2) {
                    if (data.stock > 0) {
                        if (data.productType == 1) {
                            if (data.isoverdue) {
                                actionBarHtml += " <button class=\"cell nobuy\">已 过 期</button>";
                            } else {
                                actionBarHtml += " <button class=\"cell buy\">立即购买</button>";
                            }
                        } else {
                            actionBarHtml += " <button class=\"cell cart\">加入购物车</button><button class=\"cell buy\">立即购买</button>";
                        }
                    } else {
                        actionBarHtml += "<button class=\"cell nobuy\">已 售 罄</button>";
                    }
                } else {
                    actionBarHtml += "<button class=\"cell nobuy\">已 下 架</button>";
                }

                if (data.isFavorite) {
                    actionBarHtml += "<div id=\"favoriteProduct\" class=\"cell addfav icon-fav-c active\">已收藏</div>";
                } else {
                    actionBarHtml += "<div id=\"favoriteProduct\" class=\"cell addfav icon-fav-c\">收藏</div>";
                }
                $("#s-actionbar").append(actionBarHtml);
                var favoriteShopCount = data.favoriteShopCount || 0;
                $('#favoriteShopCount').html(favoriteShopCount);//刷新店铺关注人数
                $("#favoriteProduct").click(function () {
                    checkLogin(returnFavoriteHref, function () {
                        addFavoriteFun();
                    }, shopId);
                });

                loadPriceLadderPrices(data.LadderPrices, data.measureUnit);

                loadSkus();
                if (data.saleStatus == 1 && data.auditStatus == 2 && data.stock > 0 && data.LadderPrices <= 0) {
                    loadProductCollocation();
                }
                if (data.productType == 0) {
                    ShowDepotAddress(data.freightTemplateId);
                } else {
                    $("#showDeportAddress").remove();
                }
                if (data.isFavoriteShop) {
                    $("#addShopFavorite").addClass("fav-yes");
                    $("#addShopFavorite").text("已关注");
                } else {
                    $("#addShopFavorite").removeClass("fav-yes");
                    $("#addShopFavorite").text("关注");
                }
                //进入店铺
                var bt_govshop = $("#bt_govshop");
                bt_govshop.attr("vshopid", data.vShopId);
                bt_govshop.attr("href", "/" + areaName + "/vshop/detail/" + data.vShopId);
            }
        },
        error: function (e) {
            //alert(e);
        }
    });
}

function loadPriceLadderPrices(prices, unit) {
    var pricesHtml = '';
    var countsHtml = '';
    if (prices.length > 0) {
        $(prices).each(function (i) {
            pricesHtml += '<li><em>￥</em>' + this.Price + '</li>';//价格
            if (i == prices.length - 1)
                countsHtml += '<li><i>≥</i>' + this.MinBath + unit + '</li>';
            else {
                if (this.MinBath == this.MaxBath) {
                    countsHtml += '<li>' + this.MinBath + unit + '</li>';
                } else {
                    countsHtml += '<li>' + this.MinBath + '<i>-</i>' + this.MaxBath + unit + '</li>';
                }
            }

            if (i == 0) {
                $("#ftposterprice").html(this.Price);//海报价格
            }
        });
    }
    $('#ladder-prices').html(pricesHtml);
    $('#ladder-counts').html(countsHtml);
}

function loadSkus() {
    var gid = $('#gid').val();
    $.ajax({
        type: 'post',
        url: '/' + areaName + '/product/ShowSkuInfo',
        data: { id: gid },
        dataType: 'text',
        cache: true,// 开启ajax缓存
        success: function (data) {
            $("#J_shop_att").html(data);
            //购买数量加减
            $('.wrap-num .glyphicon-minus').click(function () {
                if (parseInt($('#buy-num').val()) > 1) {
                    $('#buy-num').val(parseInt($('#buy-num').val()) - 1);
                    checkBuyNum();
                }
            });
            $('.wrap-num .glyphicon-plus').click(function () {
                $('#buy-num').val(parseInt($('#buy-num').val() || 0) + 1);
                checkBuyNum();
            });

            $("#buy-num").blur(function () {
                checkBuyNum();
            });

            $("#easyBuyBtn,#easyBuyBtn2").click(function () {
                easybuy();
            });

            $("#addToCart,#addToCart2").click(function () {
                addcart();
            });

            // 点击关闭
            $('.modul-popup').on('click', function (event) {
                if ($(event.target).is('.modul-popup-close') || $(event.target).is('.modul-popup')) {
                    $(this).removeClass('is-visible');
                }
            });

            // 属性选择弹窗
            $('.att-popup-trigger, .cart, .buy, .limi-btn').on('click', function (event) {
                $('#J_shop_att .modul-popup').addClass('is-visible');
                $('#J_timeBuy_att .modul-popup').addClass('is-visible');
                if ($(event.target).is('.cart')) {
                    $('.att-popup-footer button').css('display', 'none');
                    $('#addToCart').removeAttr('style');
                }
                else if ($(event.target).is('.buy')) {
                    $('.att-popup-footer button').css('display', 'none');
                    $('#easyBuyBtn').removeAttr('style');
                }
                else if ($(event.target).is('.att-popup-trigger') || $(event.target).is('.limi-btn')) {
                    $('.att-popup-footer button').css('display', 'none');
                    $('#easyBuyBtn2, #addToCart2, #justBuy').removeAttr('style');
                }
            });

            //初始化sku操作
            $('#choose').himallSku({
                data: { pId: $('#gid').val() },
                resultClass: {
                    price: '#jd-price',
                    stock: '#stockNum',
                    minmath: '#minMath'
                },
                ajaxUrl: $('#skuUrl').val(),
                skuPosition: 'skuArray'
            });
        },
        error: function (e) {
            //alert(e);
        }
    });
}
$(function () {
    $("body").on("click", "#colorSpec .enabled", function () {
        var _t = $(this);
        var img = _t.data("img");
        var imgbox = $(".img-responsive");
        console.log(imgbox);
        if (img && img.length > 0 && imgbox.length > 0) {
            imgbox.attr("src", img);
        }
        else {
            var defaultImg = $("#hidProductDefaultImg").val();
            imgbox.attr("src", defaultImg);
        }
    });
})
function loadProductCommentShow() {
    var gid = $('#gid').val();
    $.ajax({
        type: 'post',
        url: '/' + areaName + '/product/ProductCommentShow',
        data: { id: gid },
        dataType: 'text',
        cache: true,// 开启ajax缓存
        success: function (data) {
            $("#productCommentShow").html(data);
        },
        error: function (e) {
            //alert(e);
        }
    });
}

function goHref(url) {
    window.location.href = '' + url;
}
function loadProductCollocation() {
    var id = $('#gid').val();
    $.post('/' + areaName + '/Product/GetProductColloCation', { productid: id }, function (dt) {
        var html = [];
        if (dt != null && dt.data > 0) {
            var count = dt.data > 10 ? 10 : dt.data;
            html.push('<table class="promotion" width="100%">');
            html.push('<tr onclick=goHref("../ProductColloCation?productId=' + id + '")>');
            html.push('<td width="1"><span class="promotion-tip">组合购</span></td>');
            html.push('<td align="left"><span  class="spec-arrow" style="margin-right:10px; float: left; width: 97%;"><a style="color:#747474; width: 85%; float: left;" href="javascript:;">共' + count + '个组合购 </a></span></td>');
            html.push('</tr>');
            html.push('</table>');
        }
        if (html.length == 0) {
            $('#productColloCation').remove();
        } else {
            $('#productColloCation').html(html.join(""));
        }
    })
}

function ShowPromotion() {
    var shopId = $("#shopId").val();
    $.ajax({
        type: 'post',
        url: '/' + areaName + '/VShop/GetPromotions',
        data: { id: shopId },
        dataType: 'json',
        cache: true,// 开启ajax缓存
        success: function (result) {
            var promotionHtml = "";
            var data = result.data
            if (data.CouponCount > 0) {
                promotionHtml += "<li class=\"promotion-coupon\">" +
                    "<a href=\"javascript:;\" class=\"item-navigate-right J_coupon_trigger\" onclick=\"ShowCoupon(" + shopId + ")\">" +
                    "<span id=\"couponTip\"><i class=\"coupon-tip\">优惠券</i>" + data.CouponCount + " 张券</span>" +
                    "</a>" +
                    "</li>"
            }
            var productType = $("#productType").val();
            if (productType == 1 && promotionHtml == "") {
                //$("#divCoupon").remove();
            } else {
                $("#showPromotion").prepend(promotionHtml);
            }
            // 显示优惠券弹窗
            $('.J_coupon_trigger').on('click', function () {
                $('#ShopCoupon .modul-popup').addClass('is-visible');
            });
        },
        error: function (e) {
            //alert(e);
        }
    });
}

function ShowDepotAddress(ftid) {
    $.ajax({
        type: 'post',
        url: '/' + areaName + '/product/ShowDepotAddress',
        data: { ftid: ftid },
        dataType: 'text',
        cache: true,// 开启ajax缓存
        success: function (data) {
            $("#showDeportAddress").html(data);
        },
        error: function (e) {
            //alert(e);
        }
    });
}

function canBuy(productId) {
    var count = parseInt($("#buy-num").val());
    if (isNaN(count) || count <= 0)
        return;

    var result = false;
    $.ajax({
        url: '/m-wap/product/canbuy?productId={0}&count={1}'.format(productId, count),
        async: false,
        success: function (obj) {
            if (obj) {
                var data = obj.data;
                result = data.result;
                if ($.notNullOrEmpty(data.message)) {
                    $.dialog.errorTips(data.message);
                }
            }
        }
    });

    return result;
}

function easybuy() {
    var pid = getProductId();

    var has = $("#has").val();
    if (has != 1) return;
    var len = $('#choose .spec .selected').length;
    if (len === $("div.spec").length) {
        returnHref = "/" + areaName + "/Product/Detail/" + pid + "?returnUrl=" + encodeURIComponent(getQueryString("returnUrl"));
        returnHref = encodeURIComponent(returnHref);
        var sku = getskuid();
        if (checkBuyNum()) {
            var num = $("#buy-num").val();
            num = parseFloatOrZero(num);
            if (num <= 0) {
                $.dialog.errorTips("请输入购买数量！");
                return false;
            }
            var minMath = parseFloatOrZero($('#minMath').text());
            if (minMath > 0 && num < minMath) {
                $.dialog.errorTips("必须满足最小批量才能购买！");
                return false;
            }
            var isLTBuy = false;
            try {
                isLTBuy = isLimitTimeBuy || false;
            } catch (ex) {
                isLTBuy = false;
            }
            if (isLTBuy == true) {
                var maxnum = $("#maxSaleCount").val();
                maxnum = parseFloatOrZero(maxnum);
                if (maxnum > 0) {
                    if (num > maxnum) {
                        $.dialog.errorTips("每ID限购" + maxnum + "件！");
                        return false;
                    }
                } else {
                    if (num < 0) {
                        $.dialog.errorTips("请输入购买数量！");
                        return false;
                    }
                }
            }
            checkLogin(returnHref, function () {
                if (canBuy(pid) == false)
                    return false;

                //var url = "/" + areaName + "/Order/Submit?skuIds=" + sku + "&counts=" + num;
                //location.href = url;

                //var url = '/common/site/pay?area=mobile&platform=' + areaName.replace('m-', '') + '&controller=order&action=submit&skuIds={0}&counts={1}&returnUrl={2}'.format(sku, num, returnHref);

                var pass = true;
                var items = [];
                $("div.virtual").each(function () {
                    var obj = new Object();
                    obj.VirtualProductItemName = $(this).find(".j_name").html();
                    obj.VirtualProductItemType = $(this).attr("ptype");
                    obj.Content = $.trim($(this).find("input[type=text]").val());
                    if (obj.VirtualProductItemType == 6) {
                        var itemid = $(this).data("itemid");
                        obj.Content = $(this).find("#VProductItemImages_" + itemid).himallUpload('getImgSrc').join(',');
                    }

                    if ($(this).hasClass("j_required")) {
                        if (obj.Content.length == 0) {
                            pass = false;
                            var name = $(this).find(".j_name").html();
                            if (obj.VirtualProductItemType == 6) {
                                $.dialog.errorTips("请上传图片");
                            } else {
                                $.dialog.errorTips("请输入" + name);
                            }
                            $(this).find("input[type=text]").focus();
                            return false;
                        }
                    }
                    if (obj.VirtualProductItemType == 4) {
                        var format = /^(([1][1-5])|([2][1-3])|([3][1-7])|([4][1-6])|([5][0-4])|([6][1-5])|([7][1])|([8][1-2]))\d{4}(([1][9]\d{2})|([2]\d{3}))(([0][1-9])|([1][0-2]))(([0][1-9])|([1-2][0-9])|([3][0-1]))\d{3}[0-9xX]$/;
                        //身份证码规则校验
                        if (obj.Content.length > 0 && !format.test(obj.Content)) {
                            pass = false;
                            $.dialog.errorTips("身份证输入的字符不符合规则！");
                            $(this).find("input[type=text]").focus();
                            return;
                        }
                    }
                    items.push(obj);
                });
                if (!pass) {
                    return false;
                }
                var virtualProductItemJson = JSON.stringify(items);
                var productType = $("#productType").val();
                window.localStorage.setItem("virtualProductItemJson", virtualProductItemJson);//将用户信息存储在localStorage
                var url = '/common/site/pay?area=mobile&platform=' + areaName.replace('m-', '') + '&controller=order&action=submit&skuIds={0}&counts={1}&productType={2}&productId={3}'.format(sku, num, productType, pid);
                //检测是否开启了手机号强制绑定
                $.post("/" + areaName + "/Member/IsConBindSms", null, function (result) {
                    if (result.success) {
                        location.href = url + "&returnUrl=" + returnHref;
                    } else {//说明开启了强制绑定手机号，而用户没有绑定，则需要先去绑定
                        location.href = "/" + areaName + "/Member/BindPhone?returnUrl=" + encodeURIComponent(url) + "&type=1";
                    }
                });


            }, shopid);
        }
    } else {
        $.dialog.errorTips('请选择商品规格');
    }
}

function addcart() {
    var pid = getProductId();

    var has = $("#has").val();
    if (has != 1) return;
    var len = $('#choose .spec .selected').length;
    if (len === $("div.spec").length) {
        returnHref = "/" + areaName + "/Product/Detail/" + pid;
        returnHref = encodeURIComponent(returnHref);
        var sku = getskuid();
        if (canBuy(pid) == false)
            return false;
        if (checkBuyNum()) {
            var num = $("#buy-num").val();
            if (num.length == 0) {
                $.dialog.errorTips("请输入购买数量！");
                return false;
            }
            checkLogin(returnHref, function () {
                addToCart(sku, num, function () {
                    $('.modul-popup').removeClass('is-visible');
                    $('.plus-one').text($('#buy-num').val()).css({
                        top: 0,
                        opacity: 0,
                        display: 'block'
                    }).animate({
                        'top': '-15px',
                        'opacity': '1'
                    }, 500).fadeOut();
                });
            }, shopid);
        }

    } else {
        $.dialog.errorTips('请选择商品规格');

    }
}

function LogProduct() {
    var pid = getProductId();

    $.ajax({
        type: 'post',
        url: '/' + areaName + '/Product/LogProduct',
        data: {
            pid: pid
        },
        dataType: 'json',
        cache: false, // 开启ajax缓存
        success: function (data) {
            if (data) {
                //console.log(data);
            }
        },
        error: function (e) {
            //alert(e);
        }
    });
}

//转换0
function parseFloatOrZero(n) {
    result = 0;
    try {
        if (n.length < 1) n = 0;
        if (isNaN(n)) n = 0;
        result = parseFloat(n);
    } catch (ex) {
        result = 0;
    }
    return result;
}

function checkBuyNum() {
    var stockNum = parseInt($('#stockNum').text());
    var maxBuyCountlbl = $('#maxBuyCount');
    var maxBuyNum = maxBuyCountlbl.length > 0 ? parseInt(maxBuyCountlbl.data('value')) : 0;
    var maxnum = maxBuyNum > 0 ? Math.min(stockNum, maxBuyNum) : stockNum;

    var minBath = parseInt($('#minMath').text());
    if (minBath > 0) {
        maxnum = stockNum;
        $.ajax({
            type: 'get',
            url: '/' + areaName + '/Product/GetChangeNum',
            dataType: 'json',
            data: { pid: getProductId(), buyNum: $("#buy-num").val() },
            success: function (result) {
                if (result.success) {
                    $('#jd-price').text(result.data.price);
                }
            }
        });
    }

    return checkSkuBuyNum($("#buy-num"), maxnum);
}

function getskuid() {
    var gid = $("#gid").val();
    return getskuidbypid(gid);
}

function addFavoriteFun() {
    var _this = $('#favoriteProduct'),
        url,
        text;
    if (_this.hasClass("active")) {
        url = "/" + areaName + "/Product/DeleteFavoriteProduct";
        text = '收藏';
    } else {
        url = "/" + areaName + "/Product/AddFavoriteProduct";
        text = '已收藏';
    }
    $.post(url, {
        pid: getProductId()
    }, function (result) {
        if (result.success == true) {
            _this.toggleClass('active').text(text);
            $.dialog.succeedTips(result.msg, '', 1);
        }
    });
}

// ESC关闭弹出层
function escClose(obj, claName) {
    $(document).keyup(function (event) {
        if (event.which == '27') {
            $(obj).removeClass(claName);
        }
    });
}
escClose('.modul-popup', 'is-visible');
escClose('#J_pbuy_cover', 'hmui-cover-show');

// 商品无属性隐藏已选择
if ($('#choose').length == 0) {
    $('#choose-result').css('display', 'none');
}

function setScrollAttr() {
    var DocW = $('#slides').width();
    $('.detail-bd').css('marginTop', DocW);
}
setScrollAttr();
$(window).resize(setScrollAttr);

var shopid = $("#shopId").val();
var isLimitTimeBuy = true;
$(function () {
    $('.j_preview').click(function () {
        var video = this.querySelector('video'),
            btn = $(this).find('.fd_gif');
        if (video.paused) {
            video.play();
            btn.fadeOut();
        } else {
            video.pause();
            btn.fadeIn();
        }
    });


    function setSrc() {
        slidesImgs.each(function () {
            $(this).attr('src', $(this).data('src'));
        });
    }
    var slides = $('#slides');
    var slidesImgs = $('#slides img');
    slidesImgs.eq(0).attr('src', slidesImgs.eq(0).data('src'));
    // 焦点图滚动
    if (slides.children().length == 0) {
        slides.hide();
    }
    if (slides.children().length > 1) {
        slides.slidesjs({
            width: 640,
            height: 640,
            navigation: false,
            play: {
                active: false,
                auto: false,
                interval: 4000,
                swap: true
            },
            callback: {
                loaded: function (number) {
                    timmer = setTimeout(function () {
                        setSrc();
                    }, 5000);
                },
                complete: function (number) {
                    if (number == 2) {
                        setSrc();
                        clearTimeout(timmer);
                    }
                }
            }
        });
    } else {
        slides.css({ 'height': $(document).width() })
    }

    $('#slides').FlyZommImg({ miscellaneous: false, showBoxSpeed: 0 });
});

//var flag;

//function gotoProductImg() {
//    if (flag == 1) {
//        return false;
//    }
//    else {
//        loadProductImg();
//        flag = 1;
//    }
//}

////自动加载
//$(window).scroll(function () {
//    var scrollTop = $(this).scrollTop();
//    var scrollHeight = $(document).height();
//    var windowHeight = $(this).height();
//    if (scrollTop + windowHeight >= scrollHeight) {
//        gotoProductImg();
//    }
//});

//function loadProductImg() {
//    //在商品详情子页面里已有处理，重复加载了
//    //$(".goods-img").append('<h4><a name="top">商品图文详情</a></h4>' + $("#gid_desc").val());
//}

function notOpenVShop() {
    $.dialog.tips('暂未开通微店');
}

// 优惠券请求数据
function ShowCoupon(shopId) {
    var pid = getProductId();

    var returnurl = "/" + areaName + "/Product/Detail/" + pid;
    checkLogin(returnurl, function () {
        var url = $('#couponUrl').val();
        $.ajax({
            url: url,
            data: { shopid: shopId },
            async: false,
            cache: false,
            success: function (data) {
                $("#ShopCoupon").html(data);
                // 关闭优惠券弹窗
                $('#ShopCoupon .modul-popup').on('click', function (event) {
                    if ($(event.target).is('.modul-popup-close') || $(event.target).is('.modul-popup')) {
                        $(this).removeClass('is-visible');
                    }
                });
            }
        });
    }, shopid);
}
function showPortfolio() {
    $('#portfolioCoupon .modul-popup').addClass('is-visible');
    // 关闭组合购弹窗
    $('#portfolioCoupon .modul-popup').on('click', function (event) {
        if ($(event.target).is('.modul-popup-close') || $(event.target).is('.modul-popup')) {
            $(this).removeClass('is-visible');
        }
    });
}


function LoadActives() {
    var url = '/' + areaName + '/product/GetProductActives';
    var productType = $("#productType").val();
    $.post(url, { shopid: $("#shopId").val(), productId: $("#gid").val() }, function (data) {
        var canshow = false;
        var actbom = $("#activespan");
        var acttbom = $("#activeList");
        var actpbom = actbom.parents(".promotion-coupon");
        actpbom.hide();
        var acthtml = [];
        if (productType == 0 && parseFloat(data.freeFreight) > 0) {
            canshow = true;
            acthtml.push("满" + data.freeFreight + "免运费");
            acttbom.append("<tr><td><strong>满额包邮</strong></td> <td>满 <span>￥" + data.freeFreight + "</span> 免运费</td></tr>");
        }
        if (data.FullDiscount != undefined && data.FullDiscount != null) {
            canshow = true;
            var html = " <tr><td><strong>满额减</strong></td><td>"
            $(data.FullDiscount.Rules).each(function (index, item) {
                if (index == 0) {
                    acthtml.push("满" + item.Quota.toFixed(2) + "减" + item.Discount.toFixed(2));
                }
                html += ' <p>满 <span>￥' + item.Quota.toFixed(2) + '</span> 减 <span>￥' + item.Discount.toFixed(2) + (index == data.FullDiscount.Rules.length - 1 ? '' : ';') + '</span>';
            });
            html += '</td></tr>';
            acttbom.append(html);
        }
        if (data.ProductBonus != undefined && data.ProductBonus != null && typeof data.ProductBonus.GrantPrice != "undefined") {
            canshow = true;
            acthtml.push("满" + data.ProductBonus.GrantPrice + "送" + data.ProductBonus.Count + "个代金红包");
            var html = "<tr><td><strong>满就送</strong></td> <td>满 <span>￥" + data.ProductBonus.GrantPrice + "元</span>送红包（" + data.ProductBonus.Count + "个" + data.ProductBonus.RandomAmountStart + '—' + data.ProductBonus.RandomAmountEnd + "元代金券红包）</td></tr>";
            acttbom.append(html);
        }
        actbom.append(acthtml.join(","));
        if (canshow) {
            actpbom.show();
        }

    });
}