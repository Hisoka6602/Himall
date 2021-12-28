var defaultDate = new Date();
defaultDate.setDate(defaultDate.getDate() - 1);

$(function () {
  document.onkeydown = function () {
    if (window.event && window.event.keyCode == 13) {
      window.event.returnValue = false;
    }
  }

  $('#searchButton').click(GetData);
  GetData();
});
function ExportExecl() {
  $("#search_form").attr("action", "/admin/live/ExportProductToExcel");
  $("#search_form").submit();
}
function GetData() {
  $(function () {
    var name = $('#txtProductName').val();
    var roomId = $('#hidRoomId').val();
    $("#list").hiMallDatagrid({
      url: './LiveProductList',
      nowrap: true,
      rownumbers: true,
      NoDataMsg: '没有找到符合条件的数据',
      border: false,
      fit: true,
      fitColumns: true,
      pagination: true,
      idField: "id",
      pageSize: 15,
      pagePosition: 'bottom',
      pageNumber: 1,
      queryParams: { ProductName: name, RoomId: roomId },
      columns:
        [[
          {
            field: "Name", title: "商品", align: "left"
          },
          {
            field: "Price", title: "价格", width: 200, formatter: function (value, row, index) {
              var html = "<span>" + row.Price.toFixed(2) + "</span>";
              return html;
            }
          },
          {
            field: "SaleCount", title: "本场直播销售数量", width: 200
          },
          {
            field: "SaleAmount", title: "本场直播销售金额", width: 200, formatter: function (value, row, index) {
              var html = "<span>" + row.SaleAmount.toFixed(2) + "</span>";
              return html;
            }
          }
        ]]
    });
  });
}




