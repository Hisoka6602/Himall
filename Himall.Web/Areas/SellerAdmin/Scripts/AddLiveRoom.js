$(window).keypress(function (e) {
    if (e.keyCode == 13) {
        e.preventDefault();
    }
});

$(document).click(function () {

    $("#SearchList").hide();

});
var SearchJsonData = [];
$(document).ready(function () {

    //移交销售选择框function  begin =====================================
    if ($('#hidDataName').val() != "") {
        $("#txtAnchorWeChat").val($('#hidDataName').val());
        $("#txtAnchorWeChat").attr("datakey", $('#hidDataKey').val());
    }

    if ($("#hidDataKey").val() != "" && $("#hidDataKey").val() != "") {
        var datakey = $("#hidDataKey").val();
        var dataJson = JSON.parse($("#hidDataJson").val());
        $(dataJson).each(function (index, element) {
            if (element.ShowName == datakey) {
                $("#txtAnchorWeChat").val(element.WeChat);
                $("#txtAnchorWeChat").attr("datakey", element.WeChat);
                $("#hidDataName").val(txt);
            }
        });
    }
    $("#SearchList").hide();

    $("#txtAnchorWeChat").keyup(function () {
        var obj = $(this);
        var key = obj.val();
        $('#hidDataName').val("");
        $('#hidDataKey').val("");
        ShowSearchHtml(key, obj);
    });


    $("#txtAnchorWeChat").focus(function () {
        var obj = $(this);
        var key = obj.val();
        ShowSearchHtml(key, obj);

    });


    $("#txtAnchorWeChat").click(function (event) {
        event.stopPropagation();
    });
    $("#SearchList").click(function (event) {
        event.stopPropagation();
    });


    $('#calendarStartTime').datetimepicker({
        language: 'zh-CN',
        format: 'yyyy-mm-dd hh:ii',
        autoclose: true,
        weekStart: 1,
        minView: 0
    });
    $('#calendarEndTime').datetimepicker({
        language: 'zh-CN',
        format: 'yyyy-mm-dd hh:ii',
        autoclose: true,
        weekStart: 1,
        minView: 0
    });

});
//设置选中值
function SetSelectedVal_DataKey(datakey) {
    $(SearchJsonData).each(function (index, element) {
        if (element.UserId == datakey) {
            $("#txtAnchorWeChat").val(element.WeChat);
        }
    });
}
///设置选中值
function SetSelectedVal(obj) {

    $("#txtAnchorWeChat").val($(obj).attr('vname'));
    $("#txtAnchorWeChat").attr("datakey", $(obj).attr('vid'));
    $('#hidDataName').val($(obj).attr('vname'));
    $('#hidDataKey').val($(obj).attr('vid'));
    $("#SearchList").hide();
    $("#UserId").val($(obj).attr('vid'));
    $(SearchJsonData).each(function (index, element) {
        if (element.WeChat == $(obj).attr('vid')) {
            if (element.AnchorName != null && element.AnchorName != "") {
                $("#txtAnchorNickName").val(element.AnchorName);
            }
        }
    });

}
function ShowSearchHtml(key, hidDataKeyobj) {
    var arr = new Array();
    if (key == "") {
        $('#hidDataName').val("");
        $('#hidDataKey').val("");
    }
    if (SearchJsonData.length == 0) {
        $.post("./GetAllAnchors", {}, function (respData) {
            SearchJsonData = respData.rows;
            if (SearchJsonData.length > 0) {
                ShowSearchHtml(key, hidDataKeyobj);
            }
        });
    }
    $(SearchJsonData).each(function (index, element) {
        var txt = element.WeChat; //获取单个texteditform
        var val = element.WeChat; //获取单个value
        if (key == "" || txt.indexOf(key) > -1) {
            arr.push("<a class=\"SearchSalersVal\" href=\"javascript:void(0);\" onclick=\"SetSelectedVal(this)\"  vid=\"" + val + "\" vname=\"" + txt + "\"  >" + txt + "</a>");
        }
    });

    if (arr.length > 0) {
        $("#SearchList").show();
        $("#SearchList").html(arr.join("<br />"));
    }
}
function DateDifference(sTime, eTime, timetype) {
    var usedTime = sTime - eTime;  //两个时间戳相差的毫秒数
    if (usedTime < 0) {
        return 0;
    }
    var intervalYear = sTime.getFullYear() - eTime.getFullYear();
    if (sTime.getMonth() < eTime.getMonth()) {//月份较大则没有一年，需要减掉一年
        intervalYear -= 1;
    }//月份相等，天数较小，没有一年同样需要减掉一年
    else if (sTime.getMonth() == eTime.getMonth()) {
        if (sTime.getDate() < eTime.getDate()) {
            intervalYear -= 1;
        }
        else if (sTime.getDate() == eTime.getDate()) {//日期相等
            if (sTime.getHours() < eTime.getHours()) {
                intervalYear -= 1;
            }
            else if (sTime.getHours() == eTime.getHours()) {//小时相等
                if (sTime.getMinutes() < eTime.getMinutes()) {
                    intervalYear -= 1;
                }
                else if (sTime.getMinutes() == eTime.getMinutes()) {//分钟相等
                    if (sTime.getSeconds() < eTime.getSeconds()) {
                        intervalYear -= 1;
                    }
                }
            }
        }
    }


    var months = intervalYear * 12 + (sTime.getMonth() - eTime.getMonth());

    //日期较小，则不满1个月
    if (sTime.getDate() < eTime.getDate()) {
        months -= 1;
    }
    else if (sTime.getDate() == eTime.getDate()) {//日期相等
        if (sTime.getHours() < eTime.getHours()) {
            months -= 1;
        }
        else if (sTime.getHours() == eTime.getHours()) {//小时相等
            if (sTime.getMinutes() < eTime.getMinutes()) {
                months -= 1;
            }
            else if (sTime.getMinutes() == eTime.getMinutes()) {//分钟相等
                if (sTime.getSeconds() < eTime.getSeconds()) {
                    months -= 1;
                }
            }
        }
    }

    var days = Math.floor(usedTime / (24 * 3600 * 1000));
    //计算出小时数
    var leave1 = usedTime % (24 * 3600 * 1000);    //计算天数后剩余的毫秒数
    var hours = Math.floor(leave1 / (3600 * 1000));
    //计算相差分钟数
    var leave2 = leave1 % (3600 * 1000);        //计算小时数后剩余的毫秒数
    var minutes = Math.floor(leave2 / (60 * 1000));
    var level3 = leave2 % (60 * 1000);//计算分钟后剩余的毫秒数
    var seconds = Math.floor(level3 / 1000);
    var milliseconds = level3 % 1000;//计算秒钟后剩余的毫秒数

    if (timetype == "year" || timetype == "y") {
        return intervalYear;//返回相隔年数
    }
    else if (timetype == "month" || timetype == "M") {//返回相隔月数
        return months;
    }
    else if (timetype == "day" || timetype == "d") {//返回相隔天数
        return days;
    }
    else if (timetype == "hour" || timetype == "h") {//小时数
        return Math.floor(usedTime / (3600 * 1000));
    }
    else if (timetype = "minutes" || timetype == "m") {//分钟
        return Math.floor(usedTime / (60 * 1000));
    }
    else if (timetype == "seconds" || timetype == "s") {
        return Math.floor(usedTime / 1000);
    }
    else {
        var rettime = "";
        if (days > 0) {
            rettime += days + "天";
        }
        if ((rettime != "" && hours >= 0) || (hours > 0)) {
            rettime += hours + "小时";
        }
        if ((rettime != "" && minutes >= 0) || (minutes > 0)) {
            rettime += minutes + "分钟";
        }
        if ((rettime != "" && seconds >= 0) || (seconds > 0)) {
            rettime += seconds + "秒";
        }
        rettime += milliseconds + "毫秒"
        return rettime;
    }
    return time;
}
var lastSubmitTime = new Date();
var submitTimes = 0;
///保存直播间数据
function SaveLiveRoom(isNext) {
    var loading;


    var obj = {};
    if ($("#txtName").val() == "" || GetChineseLen($("#txtName").val().trim()) < 3 || GetChineseLen($("#txtName").val().trim())> 17) {
        $.dialog.alertTips("直播标题长度3-17个汉字(一个汉字等于两个英文字符或特殊字符）");
        return false;
    }
    obj.Name = $("#txtName").val().trim();
    if ($("#hidDataKey").val() == "") {
        $.dialog.alertTips("请选择主播微信帐号");
        return false;
    }
    obj.AnchorWeChat = $("#hidDataKey").val();
    if ($("#txtAnchorNickName").val().trim() == "" || GetChineseLen($("#txtAnchorNickName").val().trim()) > 15 || GetChineseLen($("#txtAnchorNickName").val().trim()) < 2) {
        $.dialog.alertTips("主播昵称不能为空，长度2-15个汉字(一个汉字等于两个英文字符或特殊字符）");
        return false;
    }
    obj.AnchorName = $("#txtAnchorNickName").val().trim();
    var startTimeStr = $("#calendarStartTime").val();
    if (startTimeStr != "") {
        var nowTime = new Date();

        var startTime = new Date(startTimeStr);
        var intervalMinutes = DateDifference(startTime, nowTime, "minutes");
        var intervalDays = DateDifference(startTime, nowTime, "day");
        if (intervalMinutes < 10 || intervalDays > 180) {
            $.dialog.alertTips("开播时间必须在6个月之内，且在当前时间10分钟之后");
            return false;
        }
    }
    else {
        $.dialog.alertTips("请选择开播开始时间");
        return false;
    }
    var endTimeStr = $("#calendarEndTime").val().trim();
    if (endTimeStr == "") {
        $.dialog.alertTips("请选择开播结束时间");
        return false;
    }
    var startTime = new Date(startTimeStr);

    var endTime = new Date(endTimeStr);
    var intervalMinutes = DateDifference(endTime, startTime, "minutes");
    var intervalHours = DateDifference(endTime, startTime, "hour");
    if (intervalMinutes < 30 || intervalHours > 24) {
        $.dialog.alertTips("开播开始和结束时间不能短于30分钟，不超过24小时");
        return false;
    }
    obj.StartTime = startTimeStr;
    obj.EndTime = endTimeStr;
    var coverImage = $('#coverImage').val();
    if (coverImage == "") {
        $.dialog.alertTips("直播间背景墙图片");
        return false;
    }
    obj.CoverImg = coverImage;
    var shareImage = $('#ShareImg').val();
    if (shareImage == "") {
        $.dialog.alertTips("请上传分享卡片封面");
        return false;
    }
    obj.ShareImg = shareImage;
    //30秒内重复点击直接返回false
    var tempDate = new Date();
    if ((tempDate.getTime() - lastSubmitTime.getTime()) < 30000 && submitTimes > 0) {
        lastSubmitTime = new Date();
        submitTimes += 1;
        return false;
    }
    obj.CloseComment = $("#chkCloseComment").prop("checked") ? 0 : 1;
    obj.CloseGoods = $("#chkCloseGoods").prop("checked") ? 0 : 1;
    obj.CloseLike = $("#chkCloseLike").prop("checked") ? 0 : 1;
    isposting = true;
    loading = showLoading();
    $("#btnSave").text("提交中...");
    lastSubmitTime = new Date();
    submitTimes += 1;
    $.post('SaveLiveRoom', obj, function (data) {
        isposting = false;
        $("#btnSave").text("保 存");
        loading.close();
        if (data.success == true) {
            var title = "添加直播间成功!";
            if (data.msg && data.msg.length > 0) {
                title += data.msg;
            }
            $.dialog.tips(title, function () {
                if (isNext) {
                    location.href = "LiveProduct";
                }
                else {
                    location.href = "Management";
                }
            });
        } else {
            $.dialog.tips(data.msg);
        }
    });
}
$(document).ready(function (e) {
    $("#btnSave").click(function (e) {
        SaveLiveRoom(false);
    });
    $("#btnNext").click(function (e) {
        SaveLiveRoom(true);
    });

    $("#chkCloseComment,#chkCloseLike,#chkCloseGoods").change(function (e) {
        var filename = "";
        if ($("#chkCloseLike").prop("checked")) {
            filename = "_1"
        }
        if ($("#chkCloseComment").prop("checked")) {
            filename += "_2";
        }
        if ($("#chkCloseGoods").prop("checked")) {
            filename += "_3";
        }
        if (filename == "") {
            filename = "_0"
        }
        filename += ".png"
        $(".backwallbgbox").css("background-image", "url(" + "/Areas/SellerAdmin/content/images/clientBg" + filename + ")");
    });
    $(document).on("mouseenter", ".queryImg", function () { //这里通过指定资源ID
        if ($(this).parents(".r_title").find(".tipBox").length == 1) {
            $(this).parents(".r_title").find(".tipBox").css("display", "block");
        }
        else if ($(this).next(".tipBox").length == 1) {
            $(this).next(".tipBox").css("display", "block");
        }
    });
    $(document).on("mouseleave", ".queryImg", function () { //这里通过指定资源ID
        if ($(this).parents(".r_title").find(".tipBox").length == 1) {
            $(this).parents(".r_title").find(".tipBox").css("display", "none");
        }
        else if ($(this).next(".tipBox").length == 1) {
            $(this).next(".tipBox").css("display", "none");
        }
    });
    $(".start_datetime").datetimepicker({
        language: 'zh-CN',
        format: 'yyyy-mm-dd hh:ii:ss',
        autoclose: true,
        weekStart: 1,
        minView: 0
    });
    $(".end_datetime").datetimepicker({
        language: 'zh-CN',
        format: 'yyyy-mm-dd hh:ii:ss',
        autoclose: true,
        weekStart: 1,
        minView: 0
    });
    $('.end_datetime').datetimepicker('setStartDate', $(".start_datetime").val());
    $('.start_datetime').datetimepicker('setStartDate', $(".start_datetime").val());
    //上传图片按钮
    $('body').on('click', '.upload.pic', function () {
        var me = $(this);
        var div = me.parent().find('.upload_pic');
        var resetBtn = me.parent().find('.reset.pic');
        var delBtn = me.parent().find('.del');
        var uploadobj = $(div).attr("id");
        $.uploadImage({
            url: '/common/publicOperation/UploadPic',
            maxSize: 2,
            success: function (result) {
                if (result) {
                    resetBtn.removeClass('hidden');
                    delBtn.removeClass('hide');
                    div.css('background-image', 'url("' + result + '")').find('input:hidden').val(result).change();
                    if (uploadobj == "upload_shareimg") {

                        $("#sharebgBox").css("background-image", "url(" + result + ")");
                    }
                    else {
                        $(".backwallbox").css("background-image", "url(" + result + ")");
                    }

                }
            },
            error: function () {
                $.dialog.alertTips('图片上传出错,请重新上传！');
            }
        }).upload();
    });

    $('body').on('click', '.reset.pic', function () {
        var me = $(this);

        var div = me.parent().find('.upload_pic');
        var uploadobj = $(div).attr("id");
        div.css('background-image', '').find('input:hidden').val('').change();
        if (uploadobj == "upload_shareimg") {

            $("#sharebgBox").css("background-image", "url('')");
        }
        else {
            $(".backwallbox").css("background-image", "url('')");
        }

        me.addClass('hidden');
    });
    $('.product.pic .upload_pic input:hidden').each(function () {
        var url = $(this).val();
        if (url == null || url.length == 0)
            return;
        var img = $('<img src="{0}" style="margin-top:100%"/>'.format($(this).val()));
        img.load(function () {
            $(this).closest('.product.pic').find('.del').removeClass('hide');
        }).appendTo(this);
    });

    $('.product.pic .del').click(function () {
        var div = $(this).parent().find('.upload_pic');
        var uploadobj = $(div).attr("id");
        var parent = $(this).addClass('hide').closest('.product.pic');
        parent.find('.upload_pic input:hidden').val('').parent().css('background-image', '');
        if (uploadobj == "upload_shareimg") {

            $("#sharebgBox").css("background-image", "url('')");
        }
        else {
            $(".backwallbox").css("background-image", "url('')");
        }
    });
});
