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
        var shopId = $('#ddlShop').val();
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
            queryParams: { ProductName: name, ShopId: shopId, AuditStatus: auditStatus },
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
                        field: "ShopName", title: "店铺名称", width: 200
                    },
                    {
                        field: "AuditStatusStr", title: "审核状态", width: 200,
                        formatter: function (value, row, index) {
                            var html = ["<div style=\"position:relative\">" + row.AuditStatusStr];
                            if (row.LiveAuditStatus == 3 || (row.LiveAuditStatus == 0 && row.LiveAuditMsg == "撤回审核")) {
                                html.push("<div class=\"queryDiv\"><img class=\"queryImg\" src=\"/Images/ic_query.png\" alt=\"\" />");
                                html.push("<div class=\"tipBox\"><p>");
                                if ((row.LiveAuditStatus == 0 && row.LiveAuditMsg == "撤回审核")) {
                                    html.push("该商品已撤回审核,如有需要,请再次单独提审");
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
                            if (item.LiveAuditStatus == -1) {
                                html.push("<a href=\"javascript:void(0);\" onclick=\"ToAudit(" + row.ProductId + ")\">提交审核</a>");
                            }
                            if (item.LiveAuditStatus == 0 && item.LiveAuditMsg == "撤回审核" && item.GoodsId > 0) {
                                html.push("<a href=\"javascript:void(0);\" onclick=\"ToReAudit(" + row.ProductId + ")\">重新审核</a>");
                            }
                            if (item.GoodsId == 0 || item.AuditId == 0) {
                                html.push("<a href=\"javascript:void(0);\" onclick=\"ToRemove(" + row.ProductId + ")\">移除</a>");
                            }
                            if ((item.LiveAuditStatus == 0 || item.LiveAuditStatus == 1) && item.GoodsId > 0 && item.LiveAuditMsg != "撤回审核") {
                                html.push("<a href=\"javascript:void(0);\" onclick=\"ToReCall(" + row.ProductId + ")\">撤回审核</a>");
                            }
                            html.push("</span>");
                            return html.join("");
                        }
                    }
                ]]
        });
    });
}

function ToRemove(productIds) {
    if (productIds == "") {
        $.dialog.errorTips("请选择要从直播商品库中移除的商品！");
        return false;
    }
    $.dialog.confirm('确认要将选择的商品从商品库移除吗', function () {
        var loading = showLoading();
        $.post('../Live/RemoveProduct', { productIds: productIds  }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('移除成功');
                location.reload();
            }
            else
                $.dialog.errorTips(result.msg);
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
///同步商品审核状态
function SyncData(productIds) {
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

    var loading = showLoading();
    $.post('../Live/SynchorLiveProductStatus', { productIds: ids.join(',') }, function (result) {
        loading.close();
        if (result.success) {
            $.dialog.succeedTips('同步成功');
            location.reload();
        }
        else
            $.dialog.errorTips(result.msg);
    });

}
///撤回审核
function ToReCall(productIds) {
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
        $.dialog.errorTips('请选择要撤回审核的商品！');
        return false;
    }
    $.dialog.confirm('确认要将选择的商品撤回审核吗', function () {
        var loading = showLoading();
        $.post('../Live/ReCallAudit', { productIds: ids.join(',') }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('撤回审核成功');
                location.reload();
            }
            else
                $.dialog.errorTips(result.msg);
        });
    });
}

//批量再次发起提审，只针对已撤回的商品有效
function ToReAudit(productIds) {

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
    $.dialog.confirm('确认将已撤回提审的商品再次发起提审申请吗', function () {
        var loading = showLoading();
        $.post('../Live/ReApplyAudit', { productIds: ids.join(',') }, function (result) {
            loading.close();

            if (result.success) {
                $.dialog.succeedTips('提审成功');
                location.reload();
            }
            else
                $.dialog.errorTips(result.msg);
        });
    });
}


//批量提审
function ToAudit(productIds) {

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
    $.dialog.confirm('确认将选择的商品全部提交审核吗,该操作只能提交未提交过审核的商品', function () {
        var loading = showLoading();
        $.post('../Live/SubmitToAudit', { productIds: ids.join(',') }, function (result) {
            loading.close();
            if (result.success) {
                $.dialog.succeedTips('提审成功');
                location.reload();
            }
            else
                $.dialog.errorTips(result.msg);
        });
    });
}



