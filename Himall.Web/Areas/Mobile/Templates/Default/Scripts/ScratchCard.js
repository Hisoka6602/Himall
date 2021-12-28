var arr = {
    "activityId": 0,
    "userId": 0,
    "awardName": "",
    "awardId": 0,
    "IsWin": 0,
    "integrals": 0,
    "awardType": 0,
    "bonusId": 0,
    "couponId": 0
};

$(document).ready(function () {
    $(".container").removeClass('hide');
    var Swidth = $(document).width();
    $(".scratch-card .scratch-top").width(Swidth);
    $(".scratch-card .scratch-top").height(Swidth * 0.871875);
    $(".scratch-top .submit-area").height(Swidth * 0.29375);
    $(".scratch-top .card-area").height(Swidth * 0.29375);
    $(".scratch-top .submit-area .sb-scratch").css("line-height", "$('.scratch-top .submit-area .sb-scratch').height()")

    var ncode = $(".nd-code");
    if (ncode.length > 0) {
        $(".scratch-top .submit-area .sb-scratch").addClass("mt-btn");
    } else {
        $(".scratch-top .submit-area .sb-scratch").removeClass("mt-btn");
    }


});
$(function () {
    var CardResult = $('.scratch-top .card-area');
    CardResult.scratchOn('/Areas/Mobile/Templates/Default/Images/ggk5.png', { onComplete: scratchComplete });
    function scratchComplete() {
        $("canvas").css("z-index", "1");
        //减少次数
        var _cnumbox = $(".count-num em");
        _cnum = parseInt(_cnumbox.html());
        if (isNaN(_cnum)) {
            _cnum = 0;
        }
        if (_cnum > 0) {
            _cnum--;
        }
        _cnumbox.html(_cnum);
    }//end func
    var returnurl = addurl();
    //校验登录
    //checkLogin(returnurl, function () {
    //    //判断条件 积分
    //    if (parseInt($("#integrals").val()) > parseInt($("#hintegrals").val())) {
    //        //$.dialog.errorTips('积分不足！');
    //        //window.location.href = window.location.href + '?' + Math.round(10);
    //        $(".scratch-top .award-content ").hide();
    //        return;
    //    }
    //});
    $(".submit-area .sb-scratch").click(function () {
        $(this).hide();
        $(this).parent().hide();
        $(".scratch-area canvas").css("display", "block")

        var returnurl = addurl();
        //校验登录
        checkLogin(returnurl, function () {
            //判断条件 积分
            if (parseInt($("#integrals").val()) > parseInt($("#hintegrals").val())) {
                $.dialog.errorTips('积分不足！');
                Refresh();
                $(".scratch-top .award-content ").hide();
                return;
            } else {

                add();
            }
        });
    });
    $(".none-chance .home").click(function () {
        $(".scratch-top .card-area").hide();
        $(".submit-area .nd-code").hide();
        $(".submit-area .sb-scratch").hide()
    });
    $(".none-chance .usecenter").click(function () {
        $(".scratch-top .card-area").hide();
        $(".submit-area .sb-scratch").hide()
        $(".submit-area .nd-code").hide();
    });
    $(".none-chance").on('click', '.submit-close', function () {
        //$(".scratch-top .card-area .award-content").hide();
        //window.location.href = window.location.href + '?' + Math.round(10);
        window.location.href = '/m-wap';
        //$(".none-chance").hide()
    })

});
//调用添加方法
function add() {
    var loading = showLoading();
    $.ajax({
        type: 'post',
        url: "/" + areaName + "/ScratchCard/Add",
        data: { activityId: $("#hActivityId").val() },
        dataType: "json",
        success: function (data) {

            if (data.success) {
                var html = "";

                if (data.data.isWin) {
                    if (data.data.awardType == 0) {
                        html += '<em>+</em> <span id="Name" name="Name"> ' + data.data.awardName + ' 积分</span>';
                    }
                    else if (data.data.awardType == 2) {
                        html += '<span id="Name" name="Name"> ' + data.data.awardName + '</span>';
                    }
                    else {
                        html += '<span id="Name" name="Name"> ' + data.data.awardName + '</span>';//可点击红包
                    }

                    html += '<p class="continue-submit"><a href="javascript:Refresh();">继续刮奖</a></p>';
                }
                else {
                    html += '<span>感谢参与</span>';
                    html += '<p class="continue-submit"><a href="javascript:Refresh();">继续刮奖</a></p>';
                }
                $('.award-content').html(html);
            }
            else {
                $.dialog.errorTips(data.msg);
            }
            loading.close();
        }
    });
}
//生成地址  
function addurl() {
    return "/" + areaName + "/ScratchCard/Index/" + $("#hActivityId").val();
}

