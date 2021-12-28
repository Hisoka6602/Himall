$(function () {
    function drawImage(width, height, maxWidth, maxHeight) {
        let tempWidth = maxWidth
        let tempHeight = maxHeight

        if (width / height >= maxWidth / maxHeight) {
          if (width > maxWidth) {
            tempWidth = maxWidth
            tempHeight = (height * maxWidth) / width
          } else {
            tempWidth = width
            tempHeight = height
          }
        } else {
          if (height > maxHeight) {
            tempHeight = maxHeight
            tempWidth = (width * maxHeight) / height
          } else {
            tempWidth = width
            tempHeight = height
          }
        }
        return {
          width: tempWidth,
          height: tempHeight
        }
    }

    var $advertImgWrap =  $('#advert_img_wrap')
    var $advertImg = $('#advert_img')
    var $advertImgMask = $('#advert_img_mask')
    var defaultAdvertImgUrl = $('#advert_img_default').val()

    if (defaultAdvertImgUrl) {
        getAdvertImgInfo(defaultAdvertImgUrl)

        var me = $("body .j-selectimg");
        var div = me.find('.upload');
        div.addClass('hidden');
        var del = me.find('.del');
        del.removeClass('hide');
    }

    function AdvertClose() {
      $advertImgMask.hide()
      $advertImgWrap.hide()
      $(".opdiv").hide();
    }

    function advertOpen() {
      if (defaultAdvertImgUrl) {
        $advertImgMask.show()
        $advertImgWrap.show()
      }
      $(".opdiv").show();
    }

    function getAdvertImgInfo(url) {
      const newImg = new Image()
      newImg.src = url
      newImg.onload = function () {
        const imgInfo = drawImage(newImg.width, newImg.height, 280, 320)
        $advertImgWrap.css({
          width: imgInfo.width + 'px',
          height: imgInfo.height + 'px'
        })
          $advertImg.attr('src', url)
          if ($("#activeOpenStatus").attr("checked")) {
              advertOpen()
          }
      }
    }

    //选择图片
    $('.j-selectimg').click(function (e) {
        e.stopPropagation();
        var index = $(this).data('index');

        HiShop.popbox.ImgPicker(function (imgs) {
            var me = $("body .upload.pic:eq(0)");
            var div = me.parent().find('.upload_pic');
            me.addClass('hidden');
            div.css('background-image', 'url("' + imgs[0] + '")').find('input:hidden').val(imgs[0]).change();
            div.attr("imagesrc", imgs[0]);
            defaultAdvertImgUrl = imgs[0]
            getAdvertImgInfo(defaultAdvertImgUrl)
            var del = me.parent().find('.del');
            del.removeClass('hide');
        });
    });
    $('.j-selectimg .del').click(function (e) {
        e.stopPropagation();
        var parent = $(this).addClass('hide').closest('.product.pic');
        parent.find('.upload_pic input:hidden').val('').parent().css('background-image', '');
        $(this).siblings('.upload.pic').removeClass('hidden');
        parent.find('.upload_pic').attr("imagesrc", "");

        $advertImgMask.hide()
        $advertImgWrap.hide()
    });


    loadmenu();
    initDateTime();
    if ($("#activeCloseStatus").attr("checked")) {
      AdvertClose()
    }
    if ($("#activeOpenStatus").attr("checked")) {
        advertOpen()
    }
    //点击关闭
    $("#activeCloseStatus").on("click", function () {
        AdvertClose()
    })
    //点击开启
    $("#activeOpenStatus").on("click", function () {
      advertOpen()
    })

    //提交代码
    $("#btn_save").click(function () {
        var isEnableStr = $("input[name='IsEnable']:checked").val();
        var isRepeatShowStr = $("input[name='IsRepeatShow']:checked").val();
        var href = $("#Href").val();
        var hrefobj = null;
        if (href != "") {
            hrefobj = JSON.parse(href);
            if (hrefobj.linkType == 10 || hrefobj.linkType == 23 || hrefobj.linkType == 33) {
                hrefobj.link = $("input[name='customlink']").val();
                hrefobj.smallprog_link = $("input[name='customlink']").val();

            }
            href = JSON.stringify(hrefobj);
        }
        
       
        var startTime = $("#starttime").val();
        var endTime = $("#endtime").val();
        var shareImg = $("#ShareImg").val();
        if (isEnableStr == "1") {
            if (shareImg == "") {
                HiShop.hint("danger", "请上传广告图片");
                return;
            }
            if (href == "") {
                HiShop.hint("danger", "请选择链接");
                return;
            }
            if (startTime == "") {
                HiShop.hint("danger", "请选择开始投放时间");
                return;
            }
            if (endTime == "") {
                HiShop.hint("danger", "请选择结束投放时间");
                return;
            }
        }
        $.post("SavePopupActive", { IsEnable: isEnableStr == "1" ? true : false, Link: href, Img: shareImg, StartTime: startTime, EndTime: endTime, IsReplay: isRepeatShowStr == "1" ? true : false }, function (data) {
            if (data.success == true) {
                HiShop.hint("success", "配置弹窗广告功能成功");
            } else {
                HiShop.hint("danger", data.msg);
            }
        })
    })



})


function initDateTime() {

    $(".start_datetime").datetimepicker({
        language: 'zh-CN',
        format: 'yyyy-mm-dd',
        autoclose: true,
        weekStart: 1,
        minView: 2
    });
    $(".end_datetime").datetimepicker({
        language: 'zh-CN',
        format: 'yyyy-mm-dd',
        autoclose: true,
        weekStart: 1,
        minView: 2
    });
    $('.start_datetime').on('changeDate', function () {
        if ($(".end_datetime").val()) {
            if ($(".start_datetime").val() > $(".end_datetime").val()) {
                $('.end_datetime').val($(".start_datetime").val());
            }
        }

        $('.end_datetime').datetimepicker('setStartDate', $(".start_datetime").val());
    });
}


function loadmenu() {
    var itemobj = {};
    var hrefstr = $("#Href").val();
    if (hrefstr != "") {
        itemobj = JSON.parse(hrefstr);
    }
    $(".advertinghref").html($(_.template($("#tpl_diy_ctrl_typeadv").html(), { item: itemobj })))

    $('.advertinghref').on('click', '.droplist li', function () {
        HiShop.popbox.dplPickerColletion({
            linkType: $(this).data('val'),
            callback: function (item, type) {
                item.linkType = type
                $("#Href").val(JSON.stringify(item));

                $(".advertinghref").html($(_.template($("#tpl_diy_ctrl_typeadv").html(), { item: item })))
            }
        });
    });
}




