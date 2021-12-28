var defaultDate = new Date();
defaultDate.setDate(defaultDate.getDate() - 1);

$(function () {
  document.onkeydown = function () {
    if (window.event && window.event.keyCode == 13) {
      window.event.returnValue = false;
    }
  }
  initPreviewImg();
  $('#inputStartDate').daterangepicker({
    format: 'YYYY-MM-DD',
    startDate: defaultDate, endDate: defaultDate,
    //opens: 'left',
    locale: {
      cancelLabel: "清空"
    }
  }).on('cancel.daterangepicker', function (ev, picker) {
    $("#inputStartDate").val("");
    $(".start_datetime").val("");
    $(".end_datetime").val("");
  }).bind('apply.daterangepicker', function (event, obj) {
    if ($('#inputStartDate').val() == "" || $('#inputStartDate').val() == "YYYY/MM/DD — YYYY/MM/DD") {
      return false;
    }
    var strArr = $('#inputStartDate').val().split("-");
    var startDate = strArr[0] + "-" + strArr[1] + "-" + strArr[2];
    var endDate = strArr[3] + "-" + strArr[4] + "-" + strArr[5];
    if (startDate != '' && endDate != '') {
      $(".start_datetime").val(startDate);
      $(".end_datetime").val(endDate);
    }
    else {
      $.dialog.tips('请选择时间范围');
    }
  });


  $('#searchButton').click(GetData);
  GetData();


  //上传图片按钮
  $('body').on('click', '.upload.pic', function () {
    var me = $(this);
    var div = me.parent().find('.upload_pic');
    var resetBtn = me.parent().find('.reset.pic');
    var delBtn = me.parent().find('.del');

    $.uploadImage({
      url: '/common/publicOperation/UploadPic',
      maxSize: 2,
      success: function (result) {
        if (result) {
          me.addClass('hidden');
          resetBtn.removeClass('hidden');
          delBtn.removeClass('hide');
          div.css('background-image', 'url("' + result + '")').find('input:hidden').val(result).change();
          div.attr("imagesrc", result);
          initPreviewImg();
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
    div.css('background-image', '').find('input:hidden').val('').change();
    me.addClass('hidden');
  });

  $('.product.pic .upload_pic input:hidden').each(function () {
    var url = $(this).val();
    if (url === null || url.length === 0)
      return;
    var img = $('<img src="{0}" style="margin-top:100%"/>'.format($(this).val()));
    img.load(function () {
      $(this).closest('.product.pic').find('.del').removeClass('hide');
      $(this).closest('.product.pic').find('.btn').addClass('hidden');
    }).appendTo(this);
  });

  $('.product.pic .del').click(function () {
    var parent = $(this).addClass('hide').closest('.product.pic');
    parent.find('.upload_pic input:hidden').val('').parent().css('background-image', '');
    $(this).siblings('.upload.pic').removeClass('hidden');
    parent.find('.upload_pic').attr("imagesrc", "");
  });
});
function ExportExecl() {
  $("#search_form").attr("action", "/SellerAdmin/live/ExportToExcel");
  $("#search_form").submit();
}
function GetData() {
  var name = $('#txtName').val();
  var anchorName = $('#txtAnchorName').val();
  var startDate = $(".start_datetime").val();
  var endDate = $(".end_datetime").val();
  var status = $("#ddlRoomStatus").val();
  var franchiseeId = $("[name=franchiseeId]").val();
  $("#list").hiMallDatagrid({
    url: './list',
    nowrap: false,
    rownumbers: true,
    NoDataMsg: '没有找到符合条件的数据',
    border: false,
    fit: true,
    fitColumns: true,
    pagination: true,
    idField: "Id",
    pageSize: 15,
    pagePosition: 'bottom',
    pageNumber: 1,
    queryParams: { Name: name, AnchorName: anchorName, StartTime: startDate, EndTime: endDate, Status: status, franchiseeId: franchiseeId },
    columns:
      [[
        {
          field: "Sequence", sort: true, title: '序号', width: 50,
          formatter: function (value, row, index) {
            return ' <input class="text-order" style="width:40px;margin:0;" type="text" value="' + value + '" roomId="' + row.RoomId + '" />';
          }
        },       
        { field: "Name", title: '直播标题', align: 'left' },
        { field: "AnchorName", title: "主播昵称", width: '100' },
        { field: "StatusDesc", title: "状态", width: '50' },
        { field: "StartTimeStr", sort: true, title: "开播时间", width: '80' },
        { field: "CartMember", sort: true, title: "加购人数", width: "70" },
        { field: "CartCount", sort: true, title: "加购次数", width: "70" },
        { field: "PaymentMember", sort: true, title: "支付人数", width: "70" },
        { field: "PaymentOrder", sort: true, title: "支付订单", width: "70" },
        {
          field: "PaymentAmount", sort: true, title: "支付金额", width: "70", formatter: function (value, row, index) {
            var html = "<span>" + row.PaymentAmount.toFixed(2) + "</span>";
            return html;
          }
        },
        {
          field: "operation", title: '操作', width: 200,
          formatter: function (value, row, index) {
            var id = row.RoomId.toString();
              var html = ["<span class=\"btn-a text-left inline-block\">"];
              if (row.RoomId > 0) {
                  html.push("<a href=\"javascript:void(0);\" onclick=\"SetBanner(" + id + ",'" + row.CoverImg + "')\">设置封面图</a>");
                  html.push("<a href=\"/SellerAdmin/Live/LiveProduct?roomId=" + id + "\" target='_blank'>直播商品</a>");
              }
              if (row.Status != 0) {
                  html.push('<a href="javascript:void(0);"onclick="DelLiveReplay(' + id + ')" >删除回放</a>');
              }
              html.push('<a href="javascript:void(0);"onclick="DelLiveRoom(' + row.Id + ')" >删除直播</a>');
            html.push("</span>");
            return html.join("");
          }
        }
      ]]
  });

  orderTextEventBind();
}

function SaveBanner(roomId, url) {

  $.ajax({
    type: "post",
    url: "/SellerAdmin/Live/SetBanner",
    data: { roomId: roomId, banner: url },
    dataType: "json",
    success: function (result) {
      if (result.success) {
        $.dialog.succeedTips("设置成功！");
      }
      else
        $.dialog.errorTips(result.msg);
    },
    error: function (result) {
      $.dialog.errorTips('设置失败！');
    }
  });
}
//分配商家
function DistShop(roomId) {
  $.dialog({
    title: '分配商家',
    lock: true,
    id: 'franchiseeBox',
    content: document.getElementById("dialog_shop"),
    padding: '0 20px',
    width: "300px",
    okVal: '保存',
    ok: function () {
      var val = $("#dialog_shop #shop").val();
      SetShop(roomId, val);
    }
  });
}
//设置商家
function SetShop(roomId, shopId) {
  $.ajax({
    type: "post",
    url: "/SellerAdmin/Live/SetShop",
    data: { roomId: roomId, shopId: shopId },
    dataType: "json",
    success: function (result) {
      if (result.success)
        $.dialog.succeedTips("设置成功！");
      else
        $.dialog.errorTips(result.msg);
      GetData();
    },
    error: function (result) {
      $.dialog.errorTips('设置失败！');
    }
  });

}
function initPreviewImg() {
  $(".upload_pic").click(function () {
    var url = $(this).attr("imagesrc");
    if (typeof (url) == undefined || url.length == 0) {
      return;
    }
    $(".preview-img").show();
    $(".preview-img img").attr("src", url);
    $(".cover").show();
  });
  $(".preview-img").click(function () {
    $(this).hide();
    $(".cover").hide();
  });
  $(".cover").click(function () {
    $(".preview-img").hide();
    $(".cover").hide();
  });
}


function SetBanner(roomId, url) {
  $.dialog({
    title: '封面设置',
    lock: true,
    id: 'liveBanner',
    content: document.getElementById("banner-form"),
    padding: '0 40px',
    width: "350px",
    okVal: '保存',
    init: function () {
      if (url) {
        var me = $(".upload.pic").first();
        var div = me.parent().find('.upload_pic');
        var resetBtn = me.parent().find('.reset.pic');
        var delBtn = me.parent().find('.del');
        me.addClass('hidden');
        resetBtn.removeClass('hidden');
        delBtn.removeClass('hide');
        div.css('background-image', 'url("' + url + '")').find('input:hidden').val(url).change();
        div.attr("imagesrc", url);
      }
    },
    ok: function () {
      var banner = $("input:hidden").first().val();
      SaveBanner(roomId, banner);
    }
  });
}

function reload(pageNo) {

  $("#list").hiMallDatagrid('reload', { pageNumber: pageNo });
}

//删除直播间
function DelLiveRoom(id) {
    $.dialog.confirm('确认要删除直播间吗,删除直播间将同时删除直播间相关数据，包括统计数据、商品数据、以及回放数据,建议先删除小程序后台的直播间，否则直播间数据可能会再次同步过来', function () {
        var loading = showLoading();
        $.post('../Live/DelLiveRoom', { Id: id }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('删除成功');
                location.reload();
            }
            else
                $.dialog.errorTips(result.msg);
        });
    });
}
//删除回放数据
function DelLiveReplay(roomId) {
    $.dialog.confirm('确认要删除回放数据吗,,建议先删除小程序后台的直播间数据，否则回放数据可能会再次同步过来', function () {
        var loading = showLoading();
        $.post('../Live/DelLiveReplay', { RoomId: roomId }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('删除成功');
                location.reload();
            }
            else
                $.dialog.errorTips('删除失败！' + result.msg);
        });
    });
}
function orderTextEventBind() {
  var _order = 0;
  $('.container').on('focus', '.text-order', function () {
    _order = parseInt($(this).val());
  });
  $('.container').on('blur', '.text-order', function () {
    var id = $(this).attr("roomId");
    if ($(this).hasClass('text-order')) {
      if (isNaN($(this).val()) || parseInt($(this).val()) < 0) {
        $.dialog.errorTips("您输入的序号不合法,此项只能是大于零的整数！");
        $(this).val(_order);
      } else {
        if (parseInt($(this).val()) === _order) return;

        //更新序号
        var loading = showLoading();
        ajaxRequest({
          type: 'POST',
          url: "/SellerAdmin/Live/SetSequence",
          param: { roomId: id, sequence: parseInt($(this).val()) },
          dataType: "json",
          success: function (data) {
            loading.close();
            if (data.success == true) {
              $.dialog.tips('更新成功！');
              var pageNo = $("#list").hiMallDatagrid('options').pageNumber;
              reload(pageNo);
            }
            else {
              $.dialog.errorTips(data.msg, function () { location.reload(); });
            }
          }
        });
      }
    }
  });
}

