Array.prototype.contains = function (obj) {
    var flag = false;
    this.forEach(function (item, i) {
        if (item == obj) {
            flag = true;
            return flag;
        }
    });
    return flag;
};
$.productSelector = {
    params: { auditStatus: 2, selectedProductIds: [], isShopCategory: false, exceptProductIds: [] },
    serviceType: 'admin',
    multiSelect: true,
    selectedProducts: [],
    html: '<div id="_productSelector" class="goods-choose clearfix">\
            <div class="choose-left" style="height:auto;">\
                <div class="choose-search">\
                	<div class="form-group">\
                        <label class="label-inline" for="">商品分类</label>\
                        <select class="form-control input-ssm"></select>\
                        <select class="form-control input-ssm"></select>\
                        <select class="form-control input-ssm"></select>\
                    </div>\
                    <div class="form-group" >\
                        <input class="form-control input-ssm shopname" type="text" id="" placeholder="商家名称" >\
                    </div>\
                   	<div class="form-group" >\
                        <input class="form-control input-ssm productname" type="text" id="" placeholder="商品名称" >\
                    </div>\
                    <button type="button" class="btn btn-primary btn-ssm">搜索</button>\
                </div>\
                <table class="table table-bordered table-choose"></table>\
            </div>\
            <div class="choose-right">\
                <ul class="clearfix"></ul>\
            </div>\
            \
        </div>',
    operationButtons: null,
    loadedProducts: null,
    reload: function (selectedProductIds, exceptProductIds) {
        this.loadedProducts = [];
        var url;
        if (this.serviceType == 'admin')
            url = '/admin/product/list';
        else if (this.serviceType == 'selleradmin')
            url = '/selleradmin/product/Browse';
        else if (this.serviceType == 'distribution')
            url = '/Admin/DistributionShop/ProductLists';
        else
            url = '/product/list';

        $.productSelector.params.selectedProductIds = selectedProductIds;
        $.productSelector.params.exceptProductIds = exceptProductIds;
        var columns = [
             { checkbox: true, width: 50 },
                {
                    field: "name", title: '商品', width: 320, align: "left",
                    formatter: function (value, row, index) {
                        var html = '<img src="' + row.imgUrl + '"/><span class="overflow-ellipsis">' + row.name + '</span>';
                        return html;
                    }
            },
            {
                field: "stock", title: "库存", width: 80,
            },
            {
                field: "price", title: "售价", width: 80,
                formatter: function (value, row, index) {
                    var html = '￥' + row.price.toFixed(2);
                    return html;
                }
            },
            {
                field: "shopName", title: "商家名称", width: 100, align: "center"
            },
            {
                field: "s", title: "操作", width: 86, align: "center",
                formatter: function (value, row, index) {
                    $.productSelector.loadedProducts[row.id.toString()] = row;
                    var html = '<span class="btn-a" productId="' + row.id + '">';
                    if ($.productSelector.params.selectedProductIds && $.productSelector.params.selectedProductIds.indexOf(row.id) > -1)
                        html += '已选择';
                    else
                        html += '<a productId="' + row.id + '" href="javascript:;" onclick="$.productSelector.select(' + row.id + ',this)" class="active" >选择';
                    html += '</a></span>';
                    return html;
                },
                styler: function () {
                    return 'td-operate';
                }
            }
        ];

        var start, end;
        var newColumns = [];
        if (this.multiSelect) {
            start = 1;
            end = columns.length;
        }
        else {
            start = 0;
            end = columns.length - 1;
        }
        for (var i = start; i < end; i++) {
            newColumns.push(columns[i]);
        }
        columns = newColumns;
        $("#_productSelector table").hiMallDatagrid({
            url: url,
            nowrap: false,
            rownumbers: true,
            NoDataMsg: '没有找到符合条件的数据',
            border: false,
            fit: true,
            fitColumns: true,
            pagination: true,
            hasCheckbox: this.multiSelect,
            singleSelect: !this.multiSelect,
            idField: "id",
            pageSize: 7,
            pagePosition: 'bottom',
            pageNumber: 1,
            queryParams: this.params,
            operationButtons: this.operationButtons,
            columns: [columns]
        });
        debugger;
        if (selectedProductIds && selectedProductIds.length > 0) {
            $.post(url, { Ids: selectedProductIds.toString(), page: 1, rows: selectedProductIds.length }, function (products) {
                for (var i = 0; i < selectedProductIds.length; i++) {
                    for (var j = 0; j < products.rows.length; j++) {
                        var product = products.rows[j];
                        if (selectedProductIds[i] == product.id) {
                            $.productSelector.selectedProducts[product.id] = product;
                            var li = '<li productId="' + product.id + '">\
                            <a href="javascript:;" ><img src="' + product.imgUrl + '"/></a>\
                            <i type="del">×</i>\
                             </li>';
                            $("#_productSelector ul").append(li);
                            break;
                        }
                    }
                }
            }, "json");
        }

        $("#_productSelector .choose-search button").unbind('click').click(function () {
            var keyWords = $("#_productSelector .choose-search input.productname").val();
            keyWords = $.trim(keyWords);
            $.productSelector.params.keyWords = keyWords;
            var shopName = $("#_productSelector .choose-search input.shopname").val();
            shopName = $.trim(shopName);
            $.productSelector.params.shopName = shopName;
            $("#_productSelector table").hiMallDatagrid('reload', $.productSelector.params);
        });

    },
    select: function (productId, sender, needScroll) {
        var product = this.loadedProducts[productId];
        this.selectedProducts[productId] = product;
        if (!this.params.selectedProductIds)
            this.params.selectedProductIds = [];
        this.params.selectedProductIds.push(productId);
        var li = '<li productId="' + productId + '">\
                        <a href="javascript:;"><img src="' + product.imgUrl + '"/></a>\
                        <i type="del">×</i>\
                  </li>';
        $("#_productSelector ul").append(li);
        var span = $(sender).parent();
        $(sender).remove();
        span.html('已选择');
        if (needScroll != false)
            $('.choose-right').scrollTop($('.choose-right ul').height() - $('.choose-right').height());
    },
    //选择当前页所有未选择的商品
    selectAll: function () {
        var _this = this;
        $('#_productSelector table a.active[productId]').each(function () {
            var pid = $(this).attr('productId');
            if (_this.params.selectedProductIds != null && _this.params.selectedProductIds.contains(pid))
                return;

            _this.select(pid, this, false);
        });
    },
    removeAll: function () {
        var _this = this;
        $('#_productSelector table span.btn-a[productId]').each(function () {
            var pid = $(this).attr('productId');
            _this.removeProduct(pid);
        });
    },
    removeProduct: function (productId) {
        $('#_productSelector ul li[productId="' + productId + '"]').remove();
        this.selectedProducts.remove(productId);
        if (this.params.selectedProductIds != null)
            this.params.selectedProductIds.remove(productId);
        var btn = $('span[productId="' + productId + '"]');
        if (btn) {
            btn.html(
            '<a productId="' + productId + '" href="javascript:;" onclick="$.productSelector.select(' + productId + ',this)" class="active" >选择');
            btn.addClass('active');
        }
    },
    clear: function () {
        this.selectedProducts = [];
        $("#_productSelector ul").empty();
        var shopCategory = this.params.isShopCategory;
        this.params = { auditStatus: 2, saleStatus: 1, selectedProductIds: [], isShopCategory: shopCategory };
    },
    getSelected: function () {
        var products = [];
        if (this.multiSelect) {
            //$.each(this.selectedProducts, function (i, product) {
            //    if (product)
            //        products.push(product);
            //});
            //按选择的顺序返回，原来是根据商品ID来排序
            var selectedProds = this.selectedProducts;
            $.each(this.params.selectedProductIds, function (i, id) {
                if (id && selectedProds[id]) {
                    products.push(selectedProds[id]);
                }
            });
        }
        else {
            products.push($("#_productSelector table").hiMallDatagrid('getSelected'));
        }
        return products;
    },
    show: function (selectedProductIds, onSelectFinishedCallBack, serviceType, multiSelect, exceptProductIds, operationButtons) {
        /// <param name="serviceType" type="String">平台：admin,商家：selleradmin,默认为平台</param>
        /// <param name="multiSelect" type="Bool">是否多选，默认为True</param>
        if (serviceType)
            this.serviceType = serviceType;
        if (multiSelect != null)
            this.multiSelect = multiSelect;
        this.operationButtons = operationButtons;

        $.dialog({
            title: '商品选择',
            lock: true,
            content: this.html,
            padding: '0',
            okVal: '确认选择',
            ok: function () {
                var result = onSelectFinishedCallBack && onSelectFinishedCallBack($.productSelector.getSelected());
                if (result != false) {
                    $.productSelector.clear();
                    $('#_productSelector').remove();
                }
                else {
                    return false;
                }
            },
            close: function () {
                $.productSelector.clear();
                $('#_productSelector').remove();
            }
        });

        if (!this.multiSelect) {
            $('.choose-right').hide();
            $('.choose-left').css('width', '100%');
        }

        var curl = "";
        //if (this.serviceType.toLowerCase() == "web")
        //{
        //    curl = "/category/getCategory";
        //}
        //else if (this.serviceType.toLowerCase() == "distribution")
        //{
        //    curl = "/admin/category/getCategory";
        //}
        //else
        //{
        //    curl = "/" + this.serviceType + "/category/getCategory";
        //}
        if (this.params.isShopCategory) {
            curl = "/selleradmin/category/getCategory";
        }
        else {
            curl = "/category/getCategory";
        }
        $('#_productSelector select').himallLinkage({
            url: curl,
            enableDefaultItem: true,
            defaultItemsText: '全部',
            onChange: function (level, value) {
                var categoryId = value;
                if (level > 0 && !value) {
                    var parentLevel = level - 1;
                    categoryId = $('#_productSelector select').himallLinkage('value', parentLevel);
                    if (parentLevel > 0 && !categoryId) {
                        parentLevel--;
                        categoryId = $('#_productSelector select').himallLinkage('value', parentLevel);
                    }
                }
                $.productSelector.params.categoryId = categoryId;
                //if (this.serviceType != 'admin')
                //    $.productSelector.params.isShopCategory = true;
            }
        });

        //注册删除事件
        $('#_productSelector').on('click', 'i[type="del"]', function () {
            var parent = $(this).parent();
            $.productSelector.removeProduct(parent.attr('productId'));

        });
        //注册全选事件
        $("#_productSelector").on('click', "input[type='checkbox'][class='check-all']", function () {
            if ($(this).is(':checked')) {
                $.productSelector.selectAll();
            } else {
                $.productSelector.removeAll();
            }

        });
        //this.clear();
        this.reload(selectedProductIds, exceptProductIds);

        if (+[1, ]) {
            $(".choose-right").niceScroll({
                cursorcolor: "#7B7C7E",
                cursorwidth: 6
            });
        }
    }
};
