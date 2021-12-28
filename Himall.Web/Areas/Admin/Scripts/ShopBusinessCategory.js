// JavaScript source code
var categoryArray = new Array();
$(function () {
    Query();

    $("#searchBtn").click(function (e) {
        Query();
        searchClose(e);
    });

    var _order;
    $('.container').on('focus', '.text-order', function () {
        _order = parseFloat($(this).val());
    });

    $('.container').on('blur', '.text-order', function () {
        if ($(this).hasClass('text-order')) {
            if (isNaN($(this).val()) || parseInt($(this).val()) <= -1 || parseInt($(this).val()) >= 100) {
                $.dialog({
                    title: '更新分类信息',
                    lock: true,
                    width: '400px',
                    padding: '20px 60px',
                    content: ['<div class="dialog-form">您输入的分佣比例不合法,此项只能是大于0且小于100的数字.</div>'].join(''),
                    button: [
                    {
                        name: '关闭',
                    }]
                });
                $(this).val(_order);
            } else {
                //if (parseFloat($(this).val()) === _order) return;
                //var id = $(this).parents("tr.bcategoryLines").attr('id');
                //var commisRate = $(this).val();
                //var businessCategroyId = $(this).parents("tr.bcategoryLines").attr('businessCategoryId');
                //categoryArray.remove(id + '|' + _order);
                //categoryArray.push(id + '|' + commisRate);
                ////$.post('UpdateShopCommisRate', { businessCategoryId: businessCategroyId, commisRate: commisRate }, function (result) {
                ////    if (result.success)
                ////        $.dialog.tips('保存成功');
                ////    else
                ////        $.dialog.errorTips('保存失败！' + result.msg);
                ////})
            }
        }
    });

    var categoryId;
    var categoryName = new Array();
    $('.add-business').click(function () {
        $.dialog({
            title: '新增经营类目',
            lock: true,
            id: 'addBusiness',
            content: ['<div class="dialog-form">',
            '<div class="form-group">',
                '<label class="label-inline fl" for="">经营类目</label>',
                '<select id="category1" class="form-control input-sm select-sort"><option></option></select>',
                '<select id="category2" class="form-control input-sm select-sort"><option></option></select>',
                '<select id="category3" class="form-control input-sm select-sort"><option></option></select>',
            '</div>',
            '<div class="form-group">',
                '<label class="label-inline fl" for="">分佣比例</label>',
                '<input class="form-control input-sm input-num" type="text" id="CommisRate"> %',
            '</div>',
        '</div>'].join(''),
            padding: '0 40px',
            okVal: '确认',
            ok: function () {
                var reg = /^[-+]?(0|[1-9]\d*)(\.\d+)?$/;
                if (categoryName.length < 3) {
                    $.dialog.errorTips("请选择完整后再试！");
                    return false;
                }
                var rate = $("#CommisRate").val();
                if (!$("#CommisRate").val()) {
                    $.dialog.errorTips("请填写分佣比例！");
                    return false;
                }
                if (reg.test($("#CommisRate").val()) == false) {
                    $.dialog.errorTips("请填写正确的分佣比例");
                    return false;
                }
                if (isNaN(rate) || parseInt(rate) <= -1 || parseInt(rate) >= 100){
                    $.dialog.errorTips("请填写正确的分佣比例,0-100之间");
                    return false;
                }

                var loading = showLoading();
                var yn = false;
                $.ajax({
                    type: 'POST',
                    url: "./AddShopCommisRate",
                    data: { shopId: $("#shopId").val(), businessCategoryId: categoryId, commisRate: $("#CommisRate").val() },
                    dataType: "json",
                    async: false,
                    success: function (data) {
                        if (data.success == true) {
                            var pageNo = $("#shopDatagrid").hiMallDatagrid('options').pageNumber;
                            $("#shopDatagrid").hiMallDatagrid('reload', { pageNumber: pageNo });
                            loading.close();
                            yn = true;
                        } else {
                            loading.close();
                            $.dialog.errorTips(data.msg);
                            yn = false;
                        }
                    }, error: function () { loading.close(); }
                });
                return yn;
            }
        });
        $('#category1,#category2,#category3').himallLinkage({
            url: '../category/GetValidCategories',
            enableDefaultItem: true,
            defaultItemsText: '请选择',
            onChange: function (level, value, text) {
                categoryId = value;
                if (value) {
                    var categoryNames = [];
                    for (var i = 0; i < level; i++)
                        categoryNames.push(categoryName[i]);
                    categoryNames.push(text);
                    categoryName = categoryNames;
                }
                if (level == 2) {
                    var loading = showLoading();
                    ajaxRequest({
                        type: 'GET',
                        url: "./GetCategoryCommisRate",
                        param: { Id: value },
                        dataType: "json",
                        success: function (data) {
                            loading.close();
                            if (data.success == true) {
                                $("#CommisRate").val(data.CommisRate);
                            }
                        }, error: function () {
                            loading.close();
                        }
                    });
                }
            }
        });
    });


    $("#SaveBtn").click(function () {
        categoryArray = new Array();
        $('.container .text-order').each(function () {
            var commisRate = $(this).val();
            if (isNaN($(this).val()) || parseInt($(this).val()) <= -1 || parseInt($(this).val()) >= 100) {
                return true;//跳过执行下一句
            }

            categoryArray.push($(this).attr("businesscategoryid") + '|' + commisRate);
        })

        var loading = showLoading();
        ajaxRequest({
            type: 'POST',
            url: "./SaveBusinessCategory",
            param: { shopId: $("#shopId").val(), bcategory: categoryArray.join(',') },
            dataType: "json",
            success: function (data) {
                if (data.success == true) {
                    //location.href = "./Management";
                    $.dialog.tips('保存成功');
                }
                loading.close();
            }, error: function () { loading.close(); }
        });
    });

});

function Query() {
    var shopId = $("#shopId").val();
    var categoryName = $("#categoryName").val();

    $("#shopDatagrid").hiMallDatagrid({
        url: "./BusinessCategoryList",
        singleSelect: true,
        pagination: true,
        NoDataMsg: '没有找到符合条件的数据',
        idField: "Id",
        pageSize: 15,
        pageNumber: 1,
        queryParams: { ShopId: shopId, CategoryName: categoryName },
        toolbar: "#shopToolBar",
        columns:
            [[

                { field: "Id", title: "Id", hidden: true },
                { field: "CategoryName", title: "经营类目" },
                {
                    field: "CommisRate", title: '分佣比例',
                    formatter: function (value, row, index) {
                        return ' <input class="text-order no-m commisRate" type="text" value="' + (value) + '" businesscategoryid="' + row.Id + '" /> %';
                    }
                },
                {
                    field: "operation", operation: true, title: "操作",
                    formatter: function (value, row, index) {
                        var html = '<span class="btn-a">';
                        html += '<a class="a-del" title="预览" onclick="deleteCategoryLine(this);" categoryid="' + row.CategoryId + '">删除</a>';

                        html += "</span>";
                        return html;
                    }
                }
            ]]
    });
};

function deleteCategoryLine(obj) {
    var loading = showLoading();
    ajaxRequest({
        type: 'POST',
        url: "./CanDeleteBusinessCategory",
        param: { id: $("#shopId").val(), cid: $(obj).attr('categoryid') },
        dataType: "json",
        success: function (data) {
            if (data.success == true) {
                //var commisRate = $(obj).parents("td").prev().find('.commisRate').val();
                ////console.log(id + '|' + parseInt( commisRate));
                //categoryArray.remove(id + '|' + parseInt(commisRate));
                //$("tr#" + id).remove();
                ////console.log(categoryArray);
                $.dialog.tips(data.msg);
                var pageNo = $("#shopDatagrid").hiMallDatagrid('options').pageNumber;
                $("#shopDatagrid").hiMallDatagrid('reload', { pageNumber: pageNo });
            } else {
                $.dialog.errorTips('不可以删除，用户有运营商品被购买！');
            }
            loading.close();
        }, error: function () { loading.close(); }
    });
}

