$.ShopSelector = {
    params: { selectedShopIds: [], exceptShopIds:[],shopName:""},
    serviceType: 'admin',
    multiSelect: true,
    selectedShops: [],
    controller: 'collocation',
    html: '<div id="_shopSelector" class="goods-choose clearfix">\
            <div class="choose-left">\
                <div class="choose-search">\
                	<div class="form-group">\
                        <input class="form-control input-ssm" type="text" id="" placeholder="商家名称">\
                    </div>\
                    <button type="button" class="btn btn-warning btn-ssm">搜索</button>\
                </div>\
                <table class="table table-bordered table-choose"></table>\
            </div>\
            <div class="choose-right">\
                \
                <ul class="clearfix">\
                    <li>\
                        <a href="javascript:;"><img src="../Images/60x60.png"/></a>\
                        <i class="glyphicon glyphicon-remove"></i>\
                    </li>\
                </ul>\
            </div>\
            \
        </div>\
',

    loadedshops: null,
    reload: function (selectedShopIds, exceptShopIds) {
        this.loadedshops = [];
        var url = '/admin/shop/ShopList';

        $.ShopSelector.params.selectedShopIds = selectedShopIds;
        $.ShopSelector.params.exceptShopIds = exceptShopIds;
        
        var columns = [
            { checkbox: true, width: 50 },
            {
                field: "Name", title: '商家', width: 300, align: "center",
            },
            {
                field: "ShopGrade", title: "等级", width: 100, align: "center",
            },
            {
                field: "s", title: "操作", width: 86, align: "center",
                formatter: function (value, row, index) {
                    $.ShopSelector.loadedshops[row.Id.toString()] = row;
                    var html = '<span class="btn-a" shopId="' + row.Id + '">';
                    if ($.ShopSelector.params.selectedShopIds && $.ShopSelector.params.selectedShopIds.indexOf(row.Id) > -1)
                        html += '已选择';
                    else
                        html += '<a shopId="' + row.Id + '" href="javascript:;" onclick="$.ShopSelector.select(' + row.Id + ',this)" class="active" >选择';
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


        $("#_shopSelector table").hiMallDatagrid({
            url: url,
            nowrap: false,
            rownumbers: true,
            NoDataMsg: '没有找到符合条件的数据',
            border: false,
            fit: true,
            fitColumns: true,
            pagination: true,
            hasCheckbox: !this.multiSelect,
            singleSelect: !this.multiSelect,
            idField: "id",
            pageSize: 7,
            pagePosition: 'bottom',
            pageNumber: 1,
            queryParams: this.params,
            columns: [columns]
        });

        if (selectedShopIds) {
            $.post(url, { Ids: selectedShopIds.toString(), page: 1, rows: selectedShopIds.length }, function (shops) {
                $.each(shops.rows, function (i, shop) {
                    for (var i = selectedShopIds.length - 1; i >= 0; i--) {
                        if (selectedShopIds[i] == shop.Id) {
                            $.ShopSelector.selectedShops[shop.Id] = shop;
                            var li = '<li shopId="' + shop.Id + '" style="width:120px">\
                            <a href="javascript:;" >'+ shop.Name+'</a>\
                            <i type="del" style="float:right">删除</i>\
                             </li>';
                            $("#_shopSelector ul").append(li);
                        }
                    }
                });
            }, "json");
        }

        $("#_shopSelector .choose-search button").unbind('click').click(function () {
            var keyWords = $("#_shopSelector .choose-search input").val();
            keyWords = $.trim(keyWords);
            $.ShopSelector.params.shopName = keyWords;
            $("#_shopSelector table").hiMallDatagrid('reload', $.ShopSelector.params);
        });

    },
    select: function (shopId, sender) {
        var shop = this.loadedshops[shopId];
        this.selectedShops[shopId] = shop;
        if (!this.params.selectedShopIds)
            this.params.selectedShopIds = [];
        this.params.selectedShopIds.push(shopId);
        var li = '<li shopId="' + shopId + '" style="width:120px">\
                        <a href="javascript:;">'+ shop.Name +'</a>\
                        <i type="del" style="float:right">删除</i>\
                  </li>';
        $("#_shopSelector ul").append(li);
        var span = $(sender).parent();
        $(sender).remove();
        span.html('已选择');
        $('.choose-right').scrollTop($('.choose-right ul').height() - $('.choose-right').height());
    },
    removeShop: function (shopId) {
        $('#_shopSelector ul li[shopId="' + shopId + '"]').remove();
        var removedshops = [];
        var shopIds = [];
        $.each(this.selectedShops, function (i, shop) {
            if (shop && shop.Id != shopId) {
                removedshops[shop.Id] = shop;
                shopIds.push(shop.Id);
            }
        });
        this.selectedShops = removedshops;
        this.params.selectedShopIds = shopIds;
        var btn = $('span[shopId="' + shopId + '"]');
        if (btn) {
            btn.html(
                '<a shopId="' + shopId + '" href="javascript:;" onclick="$.ShopSelector.select(' + shopId + ',this)" class="active" >选择');
            btn.addClass('active');
        }
    },
    clear: function () {
        this.selectedShops = [];
        $("#_shopSelector ul").empty();
        this.params = { selectedShopIds: [] };
    },
    getSelected: function () {
        var shops = [];
        if (this.multiSelect) {
            $.each(this.selectedShops, function (i, shop) {
                if (shop)
                    shops.push(shop);
            });
        }
        else {
            shops.push($("#_shopSelector table").hiMallDatagrid('getSelected'));
        }
        return shops;
    },
    show: function (selectedShopIds, onSelectFinishedCallBack, serviceType, multiSelect, exceptShopIds, controller) {
        /// <param name="serviceType" type="String">平台：admin,商家：selleradmin,默认为平台</param>
        /// <param name="multiSelect" type="Bool">是否多选，默认为True</param>
        if (serviceType)
            this.serviceType = serviceType;
        if (multiSelect != null)
            this.multiSelect = multiSelect;
        if (controller != null)
            this.controller = controller;

        $.dialog({
            title: '商家选择',
            lock: true,
            content: this.html,
            padding: '0',
            okVal: '确认选择',
            ok: function () {
                onSelectFinishedCallBack && onSelectFinishedCallBack($.ShopSelector.getSelected());
                $.ShopSelector.clear();
                $('#_shopSelector').remove();
            },
            close: function () {
                $.ShopSelector.clear();
                $('#_shopSelector').remove();
            }
        });

        if (!this.multiSelect) {
            $('.choose-right').hide();
            $('.choose-left').css('width', '100%');
        }

        //var curl = "";
        //if (this.serviceType.toLowerCase() == "web") {
        //    curl = "/category/getCategory";
        //}
        //else {
        //    curl = "/" + this.serviceType + "/category/getCategory";
        //}
        
        //注册删除事件
        $('#_shopSelector').on('click', 'i[type="del"]', function () {
            var parent = $(this).parent();
            $.ShopSelector.removeShop(parent.attr('shopId'));

        });

        this.clear();
        this.reload(selectedShopIds, exceptShopIds);

        if (+[1,]) {
            $(".choose-right").niceScroll({
                cursorcolor: "#7B7C7E",
                cursorwidth: 6,
            });
        }
    }
};
