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

function GetData() {
    $(function () {
        var name = $('#txtProductName').val();
        var auditStatus = $('#ddlAuditStatus').val();
        $("#list").hiMallDatagrid({
            url: './LiveProductLibaray',
            nowrap: true,
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
            queryParams: { ProductName: name, AuditStatus: auditStatus },
            columns:
                [[
                    { checkbox: true, width: 39 },
                    { field: "ProductId", hidden: true },
                    {
                        field: "name", title: '商品', width: 250, align: "left",
                        formatter: function (value, row, index) {
                            var html = '<img width="40" height="40" src="' + row.Image + '" style="" /><span class="overflow-ellipsis" style="width:150px"><a title="' + row.Name + '" href="/product/detail/' + row.ProductId + '" target="_blank" >' + row.Name + '</a>';
                            if (row.PriceType == 1) {
                                html = html + '<p>￥' + row.Price.toFixed(2) + '</p></span>';
                            }
                            else {
                                html = html + '<p>￥' + row.Price.toFixed(2) + ' - ' + row.Price2.toFixed(2) + '</p></span>';
                            }

                            return html;
                        }
                    },

                    {
                        field: "AuditStatusStr", title: "审核状态", width: 200,
                        formatter: function (value, row, index) {
                            var html = ["<div style=\"position:relative\">" + row.AuditStatusStr];
                            if (row.LiveAuditStatus == 3 || (row.LiveAuditStatus == 0 && row.LiveAuditMsg == "撤回审核")) {
                                html.push("<div class=\"queryDiv\"><img class=\"queryImg\" src=\"/Images/ic_query.png\" alt=\"\" />");
                                html.push("<div class=\"tipBox\"><p>");
                                if ((row.LiveAuditStatus == 0 && row.LiveAuditMsg == "撤回审核")) {
                                    html.push("该商品已撤回审核,如有需要,请让平台再次提审");
                                }
                                else {
                                    html.push(row.LiveAuditMsg == "" ? "提审失败,请修改商品信息重新提交审核" : row.LiveAuditMsg);
                                }
                                html.push("</p></div></div>");
                            }
                            html.push("</div>")
                            return html.join("");
                        }

                    },
                    {
                        field: "operation", title: '操作', width: 120,
                        formatter: function (value, row, index) {
                            var html = ["<span class=\"btn-a text-left inline-block\">"];
                            var item = row;
                            if (row.LiveAuditStatus != -1 && row.LiveAuditStatus != 1 || (row.LiveAuditStatus == 0 && row.LiveAuditMsg == "撤回审核") && row.GoodsId > 0) {
                                html.push("<a href=\"javascript:void(0);\" onclick=\"ToDelete(" + row.ProductId + ")\">删除</a>");
                            }
                            if (item.GoodsId == 0 || item.AuditId == 0) {
                                html.push("<a href=\"javascript:void(0);\" onclick=\"ToRemove(" + row.ProductId + ")\">移除</a>");
                            }
                           

                            html.push("</span>");
                            return html.join("");
                        }
                    }
                ]]
        });
    });
}
///删除
function ToDelete(productIds) {
    var ids = [];
    if (productIds != undefined && productIds != "" && productIds > 0) {
        ids.push(productIds);
    }
    else {
        var selecteds = $("#list").hiMallDatagrid('getSelections');
        $.each(selecteds, function () {
            ids.push(this.ProductId);
        });

    }
    if (ids.length <= 0) {
        $.dialog.errorTips('请选择要从直播商品库中删除的商品！');
        return false;
    }
    $.dialog.confirm('确认要将选择的商品从直播库中删除吗', function () {
        var loading = showLoading();
        $.post('../Live/DelLiveProduct', { productIds: ids.join(',') }, function (result) {
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
///移除
function ToRemove(productIds) {
    var ids = [];
    if (productIds != undefined && productIds != "" && productIds > 0) {
        ids.push(productIds);
    }
    else {
        var selecteds = $("#list").hiMallDatagrid('getSelections');
        $.each(selecteds, function () {
            ids.push(this.ProductId);
        });

    }
    if (ids.length == 0) {
        $.dialog.errorTips("请选择要从直播商品库中移除的商品！");
        return false;
    }
    $.dialog.confirm('确认要将选择的商品从商品库移除吗', function () {
        var loading = showLoading();
        $.post('/SellerAdmin/Live/RemoveProduct', { productIds: ids.join(',') }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('移除成功');
                location.reload();
            }
            else
                $.dialog.errorTips('移除失败！' + result.msg);
        });
    });
}

function ToExportIn() {
    $.post('/SellerAdmin/Live/GetInLibarayProductIds', {}, function (data) {
        $.productSelector.params.isShopCategory = true;

        $.productSelector.show(data, function (selectedProducts) {
            var ids = [];
            $.each(selectedProducts, function () {
                ids.push(this.id);
            });
            var loading = showLoading();
            $.post('/SellerAdmin/Live/ProductToLiveProductLibrary', { productIds: ids.toString() }, function (data) {
                loading.close();
                if (data.success)
                    $("#list").hiMallDatagrid('reload', {});
                else
                    $.dialog.success(data.msg);
            });
        }, 'selleradmin', null, null, null, true, 'LiveProductLibaray');
    });
}
