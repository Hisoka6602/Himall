var HiShop = HiShop ? HiShop : {};
var areaPath = "/Admin";//默认平台后台
HiShop.getAreaName = function () {
    if (window.location.href.toLocaleLowerCase().indexOf("/admin") > 0) {
        return "areaName=admin";
    } else {
        return "areaName=selleradmin";
    }
},
    $(function () {
        var areaName = HiShop.getAreaName();
        if (areaName === 'areaName=admin') {
            var IsOpenStore = $("#IsOpenStore").val().toLocaleLowerCase()
        } else if (areaName == "areaName=selleradmin") {
            areaPath = "/SellerAdmin"; //商家后台
        }

        //模块类型1 广告图片
        HiShop.DIY.Unit.event_type1 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type1').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type1').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type1($ctrl, data);
            }

            var calcWidth = function () {
                var totalWidth = 375 - data.content.pageMargin * 2,
                    count = data.content.lineCount;
                if (data.content.showType === 3) {
                    count = 2
                } else if (data.content.showType === 4) {
                    count = 3
                }
                data.temp.width = parseInt(totalWidth / count + totalWidth / count / count)
            }

            // 切换模板
            $ctrl.find('.layout-options li').click(function () {
                data.content.showType = $(this).data('type');
                calcWidth()
                reRender()
            })

            $ctrl.find('input[name="heightType"]').change(function () {
                data.content.heightType = parseInt($(this).val());
                reRender();
            });

            $ctrl.find('input[name="lineCount"]').change(function () {
                data.content.lineCount = parseInt($(this).val());
                calcWidth()
                reRender();
            });

            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });

            $ctrl.find('input[name="imgHeight"]').change(function () {
                data.content.height = parseInt($(this).val() || 0);
                reRender();
            });

            $ctrl.find('input[name="imgStyle"]').change(function () {
                data.content.imgStyle = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="imgRadius"]').change(function () {
                data.content.imgRadius = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="fill"]').change(function () {
                data.content.fill = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="indicatorStyle"]').change(function () {
                data.content.indicatorStyle = parseInt($(this).val());
                reRender();
            });


            $ctrl.find('.imgHeight').slider({
                min: 0,
                max: 375,
                step: 1,
                animate: 'fast',
                value: data.content.height,
                slide: function (event, ui) {
                    $ctrl.find('input[name="imgHeight"]').val(ui.value)
                },
                stop: function (event, ui) {
                    data.content.height = parseInt(ui.value);
                    reRender()
                }
            });

            //这里需要加载颜色选择器效果
            $ctrl.find('.bgColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    reRender();
                }
            });

            $ctrl.find('.pageMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.pageMargin,
                slide: function (event, ui) {
                    $ctrl.find('.pageMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.pageMargin = parseInt(ui.value);
                    calcWidth()
                    reRender()
                }
            });

            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.verticalMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });

            $ctrl.find('.imgMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.imgMargin,
                slide: function (event, ui) {
                    $ctrl.find('.imgMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.imgMargin = parseInt(ui.value);
                    reRender()
                }
            });

            //改变标题
            $ctrl.find('input[name="title"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.images[index].showtitle = $(this).val();
                reRender();
            });
            //改变链接
            $ctrl.find('.droplist li').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                HiShop.popbox.dplPickerColletion({
                    linkType: $(this).data('val'),
                    callback: function (item, type) {
                        data.content.images[index].title = item.title;
                        data.content.images[index].showtitle = item.title;
                        data.content.images[index].link = item.link;
                        data.content.images[index].pc_link = item.pc_link;
                        data.content.images[index].smallprog_link = item.smallprog_link;
                        data.content.images[index].linkType = type;
                        if (!data.content.images[index].imgSrc && item.pic) {
                            data.content.images[index].imgSrc = item.pic;
                        }
                        reRender();
                    }
                });
            });

            //选择图片
            $ctrl.find('.j-selectimg').click(function (e) {
                e.stopPropagation();
                var index = $(this).data('index');

                HiShop.popbox.ImgPicker(function (imgs) {
                    data.content.images[index].imgSrc = imgs[0]; //获取第一张图片
                    reRender();
                });
            });

            //自定义链接
            $ctrl.find('input[name="customlink"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.images[index].link = $(this).val();
                data.content.images[index].smallprog_link = $(this).val();
            });

            //上移
            $ctrl.find('.j-moveup').click(function (e) {
                e.stopPropagation();
                var index = $(this).data('index');

                if (index == 0) return; //第一个导航不可再向上移动

                //替换缓存数组中的位置
                var tmpdata = data.content.images.slice(index, index + 1)[0];
                data.content.images.splice(index, 1);
                data.content.images.splice(index - 1, 0, tmpdata);

                reRender(); //更新视图
            });

            //下移
            $ctrl.find('.j-movedown').click(function (e) {
                e.stopPropagation();
                var index = $(this).data('index'),
                    len = data.content.images.length;

                if (index == len - 1) return; //最后一个导航不可再向下移动

                //替换缓存数组中的位置
                var tmpdata = data.content.images.slice(index, index + 1)[0];
                data.content.images.splice(index, 1);
                data.content.images.splice(index + 1, 0, tmpdata);

                reRender(); //更新视图
            });

            //添加
            $ctrl.find('.ctrl-item-list-add').click(function () {
                var newdata = {
                    linkType: 0,
                    link: '',
                    smallprog_link: '',
                    showtitle: '',
                    imgSrc: '',
                    title: '',
                    rects: []
                };
                data.content.images.push(newdata);
                reRender();
            });

            //删除
            $ctrl.find('.j-del').click(function () {
                var index = $(this).data('index');
                data.content.images.splice(index, 1);
                reRender();
            });

            var mapEditing = {},
                mapEditIndex = 0;
            // 热区初始化
            var initMapItem = function (id) {
                $('#' + id).draggable({
                    containment: "#map-container",
                    stop: function (event, ui) {
                        var index = ui.helper.index() - 1;
                        mapEditing.rects[index].top = ui.position.top
                        mapEditing.rects[index].left = ui.position.left
                    }
                }).resizable({
                    containment: "#map-container",
                    handles: 'all',
                    minHeight: 50,
                    minWidth: 50,
                    stop: function (event, ui) {
                        var index = ui.helper.index() - 1;
                        mapEditing.rects[index].width = ui.size.width
                        mapEditing.rects[index].height = ui.size.height
                    }
                })
            }

            $(document).off('click', '.map-btn.del')
            $(document).on('click', '.map-btn.del', function () {
                var index = $(this).parent().index() - 1;
                mapEditing.rects.splice(index, 1)
                $(this).parent().remove()
            })

            //改变链接
            $(document).off('click', '.map-btn .droplist-menu li')
            $(document).on('click', '.map-btn .droplist-menu li', function () {
                var index = $(this).parents('.map-block').index() - 1;
                HiShop.popbox.dplPickerColletion({
                    linkType: $(this).data('val'),
                    callback: function (item, type) {
                        mapEditing.rects[index].title = item.title;
                        mapEditing.rects[index].link = item.link;
                        mapEditing.rects[index].smallprog_link = item.smallprog_link;
                        mapEditing.rects[index].linkType = type;
                        $(".map-editor .map-block").eq(index).find('p').text(item.title)
                    }
                });
            });

            $ctrl.find('.map-list li').click(function () {
                mapEditIndex = $(this).data('index');
                mapEditing = data.content.images[mapEditIndex];
                var tpl_map = $('#tpl_diy_con_mapedit').html();
                var $renderMap = $(_.template(tpl_map, mapEditing));
                $renderMap.find('.map-img').height($(this).height() * 500 / 350)
                $.jBox.show({
                    title: '热区编辑器',
                    content: $renderMap,
                    btnOK: {
                        show: true,
                        text: '添加热区',
                        onBtnClick: function () {
                            var id = new Date().getTime();
                            mapEditing.rects.push({
                                id: id,
                                top: 0,
                                left: 0,
                                width: 128,
                                height: 128
                            })
                            if (HiShop.getAreaName() === 'areaName=selleradmin') {
                                $(".map-editor #map-container").append('<div class="map-block" id=' + id + '><p>请选择链接</p><div class="map-btn map-link">链接' +
                                    '<ul class="droplist-menu">' +
                                    '<li data-val="1"><a href="javascript:;">' + HiShop.linkType[1] + '</a></li>' +
                                    '<li data-val="2"><a href="javascript:;">' + HiShop.linkType[2] + '</a></li>' +
                                    '<li data-val="5"><a href="javascript:;">' + HiShop.linkType[5] + '</a></li>' +
                                    '<li data-val="6"><a href="javascript:;">' + HiShop.linkType[6] + '</a></li>' +
                                    '<li data-val="3"><a href="javascript:;">' + HiShop.linkType[3] + '</a></li>' +
                                    '<li data-val="9"><a href="javascript:;">' + HiShop.linkType[9] + '</a></li>' +
                                    '<li data-val="40"><a href="javascript:;">' + HiShop.linkType[40] + '</a></li>' +
                                    '</ul>' +
                                    '</div><div class="map-btn del">删除</div></div>');
                            } else {
                                if (JSON.parse(IsOpenStore)) {
                                    $(".map-editor #map-container").append('<div class="map-block" id=' + id + '><p>请选择链接</p><div class="map-btn map-link">链接' +
                                        '<ul class="droplist-menu">' +
                                        '<li data-val="1"><a href="javascript:;">' + HiShop.linkType[1] + '</a></li>' +
                                        '<li data-val="2"><a href="javascript:;">' + HiShop.linkType[2] + '</a></li>' +
                                        '<li data-val="3"><a href="javascript:;">' + HiShop.linkType[3] + '</a></li>' +
                                        '<li data-val="5"><a href="javascript:;">' + HiShop.linkType[5] + '</a></li>' +
                                        '<li data-val="6"><a href="javascript:;">' + HiShop.linkType[6] + '</a></li>' +
                                        '<li data-val="9"><a href="javascript:;">' + HiShop.linkType[9] + '</a></li>' +
                                        '<li data-val="15"><a href="javascript:;">' + HiShop.linkType[15] + '</a></li>' +
                                        '<li data-val="24"><a href="javascript:;">' + HiShop.linkType[24] + '</a></li>' +
                                        '<li data-val="26"><a href="javascript:;">' + HiShop.linkType[26] + '</a></li>' +
                                        '<li data-val="28"><a href="javascript:;">' + HiShop.linkType[28] + '</a></li>' +
                                        '<li data-val="30"><a href="javascript:;">' + HiShop.linkType[30] + '</a></li>' +
                                        '<li data-val="31"><a href="javascript:;">' + HiShop.linkType[31] + '</a></li>' +
                                        '<li data-val="40"><a href="javascript:;">' + HiShop.linkType[40] + '</a></li>' +
                                        '<li data-val="41"><a href="javascript:;">' + HiShop.linkType[41] + '</a></li>' +
                                        '<li data-val="42"><a href="javascript:;">' + HiShop.linkType[42] + '</a></li>' +
                                        '</ul>' +
                                        '</div><div class="map-btn del">删除</div></div>');
                                } else {
                                    $(".map-editor #map-container").append('<div class="map-block" id=' + id + '><p>请选择链接</p><div class="map-btn map-link">链接' +
                                        '<ul class="droplist-menu">' +
                                        '<li data-val="1"><a href="javascript:;">' + HiShop.linkType[1] + '</a></li>' +
                                        '<li data-val="2"><a href="javascript:;">' + HiShop.linkType[2] + '</a></li>' +
                                        '<li data-val="3"><a href="javascript:;">' + HiShop.linkType[3] + '</a></li>' +
                                        '<li data-val="5"><a href="javascript:;">' + HiShop.linkType[5] + '</a></li>' +
                                        '<li data-val="6"><a href="javascript:;">' + HiShop.linkType[6] + '</a></li>' +
                                        '<li data-val="9"><a href="javascript:;">' + HiShop.linkType[9] + '</a></li>' +
                                        '<li data-val="15"><a href="javascript:;">' + HiShop.linkType[15] + '</a></li>' +
                                        '<li data-val="24"><a href="javascript:;">' + HiShop.linkType[24] + '</a></li>' +
                                        '<li data-val="26"><a href="javascript:;">' + HiShop.linkType[26] + '</a></li>' +
                                        '<li data-val="30"><a href="javascript:;">' + HiShop.linkType[30] + '</a></li>' +
                                        '<li data-val="31"><a href="javascript:;">' + HiShop.linkType[31] + '</a></li>' +
                                        '<li data-val="40"><a href="javascript:;">' + HiShop.linkType[40] + '</a></li>' +
                                        '<li data-val="41"><a href="javascript:;">' + HiShop.linkType[41] + '</a></li>' +
                                        '<li data-val="42"><a href="javascript:;">' + HiShop.linkType[42] + '</a></li>' +
                                        '</ul>' +
                                        '</div><div class="map-btn del">删除</div></div>');
                                }

                            }

                            initMapItem(id);
                        }
                    },
                    btnCancel: {
                        show: true,
                        text: '保存',
                        onBtnClick: function (a) {
                            data.content.images[mapEditIndex].rects = mapEditing.rects
                            $.jBox.close(a)
                            reRender()
                        }
                    },
                    onOpen: function () {
                        mapEditing.rects.forEach(function (item) {
                            initMapItem(item.id)
                        })
                    }
                })
            })

        };

        //模块类型2 图文导航
        HiShop.DIY.Unit.event_type2 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type2').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type2').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            var calcWidth = function () {
                var count = data.content.lineCount
                var total = 375 - data.content.pageMargin * 2
                data.temp.width = parseInt(total / count + total / count / count)
            }
            var calcHeight = function () {
                var total = 375 - data.content.pageMargin * 2
                var height = total / data.content.images.length - data.content.imgMargin
                if (data.content.heightType) {
                    height = data.content.height
                }
                data.temp.height = height
            }

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);
                HiShop.DIY.Unit.event_type2($ctrl, data);
            }

            //改变标题
            $ctrl.find('input[name="title"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.images[index].showtitle = $(this).val();
                reRender();
            });

            //改变链接
            $ctrl.find('.droplist li').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                HiShop.popbox.dplPickerColletion({
                    linkType: $(this).data('val'),
                    callback: function (item, type) {
                        data.content.images[index].title = item.title;
                        data.content.images[index].showtitle = item.title;
                        data.content.images[index].link = item.link;
                        data.content.images[index].pc_link = item.pc_link;
                        data.content.images[index].smallprog_link = item.smallprog_link;
                        data.content.images[index].linkType = type;
                        if (!data.content.images[index].imgSrc && item.pic) {
                            data.content.images[index].imgSrc = item.pic;
                        }
                        reRender();
                    }
                });
            });

            //选择图片
            $ctrl.find('.j-selectimg').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                HiShop.popbox.ImgPicker(function (imgs) {
                    data.content.images[index].imgSrc = imgs[0]; //获取第一张图片
                    reRender();
                });
            });

            //自定义链接
            $ctrl.find('input[name="customlink"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.images[index].link = $(this).val();
                data.content.images[index].smallprog_link = $(this).val();
            });

            //上移
            $ctrl.find('.j-moveup').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                if (index == 0) return; //第一个导航不可再向上移动

                //替换缓存数组中的位置
                var tmpdata = data.content.images.slice(index, index + 1)[0];
                data.content.images.splice(index, 1);
                data.content.images.splice(index - 1, 0, tmpdata);

                reRender(); //更新视图
            });

            //下移
            $ctrl.find('.j-movedown').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index(),
                    len = data.content.images.length;

                if (index == len - 1) return; //最后一个导航不可再向下移动

                //替换缓存数组中的位置
                var tmpdata = data.content.images.slice(index, index + 1)[0];
                data.content.images.splice(index, 1);
                data.content.images.splice(index + 1, 0, tmpdata);

                reRender(); //更新视图
            });

            //添加
            $ctrl.find('.ctrl-item-list-add').click(function () {
                var newdata = {
                    linkType: 0,
                    link: '',
                    smallprog_link: '',
                    title: '',
                    showtitle: '',
                    imgSrc: ''
                };
                data.content.images.push(newdata);
                calcHeight()
                reRender();
            });

            //删除
            $ctrl.find('.j-del').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.images.splice(index, 1);
                calcHeight()
                reRender();
            });

            // 切换模板
            $ctrl.find('.layout-options li').click(function () {
                data.content.showType = $(this).data('type')
                reRender()
            })

            $ctrl.find('input[name="heightType"]').change(function () {
                data.content.heightType = parseInt($(this).val());
                calcHeight()
                reRender();
            });

            $ctrl.find('input[name="scroll"]').change(function () {
                data.content.scroll = parseInt($(this).val());
                calcWidth()
                calcHeight()
                reRender();
            });

            $ctrl.find('select[name="thickness"]').change(function () {
                data.content.lineCount = parseInt($(this).val());
                calcWidth()
                reRender();
            });

            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });

            $ctrl.find('input[name="imgHeight"]').change(function () {
                data.content.height = parseInt($(this).val() || 0);
                calcHeight()
                reRender();
            });

            $ctrl.find('.imgHeight').slider({
                min: 0,
                max: 375,
                step: 1,
                animate: 'fast',
                value: data.content.height,
                slide: function (event, ui) {
                    $ctrl.find('input[name="imgHeight"]').val(ui.value)
                },
                stop: function (event, ui) {
                    data.content.height = parseInt(ui.value);
                    calcHeight()
                    reRender()
                }
            });

            //这里需要加载颜色选择器效果
            $ctrl.find('.bgColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    reRender();
                }
            });

            $ctrl.find('.textColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.textColor = hex;
                    reRender();
                }
            });

            $ctrl.find('.pageMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.pageMargin,
                slide: function (event, ui) {
                    $ctrl.find('.pageMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.pageMargin = parseInt(ui.value);
                    calcHeight()
                    calcWidth()
                    reRender()
                }
            });

            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.verticalMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });

            $ctrl.find('.imgMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.imgMargin,
                slide: function (event, ui) {
                    $ctrl.find('.imgMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.imgMargin = parseInt(ui.value);
                    calcHeight()
                    reRender()
                }
            });
        };

        //模块类型3 富文本
        HiShop.DIY.Unit.event_type3 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom; //控制内容

            //如果之前存在编辑器则销毁
            if (data.ue) {
                data.ue.destroy();
            }
            data.ue = UE.getEditor('editor' + data.id); //创建编辑器
            data.ue.ready(function () {
                data.ue.setContent(HiShop.DIY.Unit.html_decode(data.content.fulltext)); //设置编辑器的默认值

                data.ue.execCommand('fontsize', '12px');
                //data.ue.focus(true); //编辑器获得焦点
                //当值改变时反应到手机视图中
                var reSetVal = function () {
                    var val = data.ue.getContent();
                    $conitem.find('.fulltext').html(val || '<p>请点击此处编辑文本内容，您可以自由改变文本样式</p><p>也可以在文本中添加图片、视频以及超链接等内容；</p>'); //更新到手机视图
                    data.content.fulltext = HiShop.DIY.Unit.html_encode(val); //更新到缓存
                }
                data.ue.addListener('selectionchange', reSetVal);
                data.ue.addListener('contentChange', reSetVal);
            });
            UE.Editor.prototype.placeholder = function (justPlainText) {
                var _editor = this;
                _editor.addListener('focus', function () {
                    var localHtml = HiShop.DIY.Unit.html_decode(_editor.getContent());
                    if ($.trim(localHtml) === $.trim(justPlainText)) {
                        _editor.setContent(' ');
                    }
                });
            };
            data.ue.placeholder('');

            //这里需要加载颜色选择器效果
            $ctrl.find('.color-select').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    $ctrl.find('.color-select span').css('background', hex)
                    $conitem.find('.fulltext').css('background', hex)
                }
            });

            //设置全屏
            $ctrl.find('input[name="fullScreen"]').change(function () {
                data.content.fullScreen = this.checked;
                $conitem.find('.fulltext').css('padding', this.checked ? '0' : '10px')
            });
        };

        //模块类型4 魔方
        HiShop.DIY.Unit.event_type4 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type4').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type4').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);
                HiShop.DIY.Unit.event_type4($ctrl, data);
            }
            //改变标题
            $ctrl.find('input[name="title"]').change(function () {
                data.content.images[data.temp.targetIndex].title = $(this).val();
            });

            //改变链接
            $ctrl.find('.droplist li').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                HiShop.popbox.dplPickerColletion({
                    linkType: $(this).data('val'),
                    callback: function (item, type) {
                        data.content.images[data.temp.targetIndex].title = item.title;
                        data.content.images[data.temp.targetIndex].link.name = item.title;
                        data.content.images[data.temp.targetIndex].link.link = item.link;
                        data.content.images[data.temp.targetIndex].link.pc_link = item.pc_link;
                        data.content.images[data.temp.targetIndex].link.smallprog_link = item.smallprog_link;
                        data.content.images[data.temp.targetIndex].link.linkType = type;
                        if (!data.content.images[data.temp.targetIndex].imgSrc && item.pic) {
                            data.content.images[data.temp.targetIndex].imgSrc = item.pic;
                        }
                        reRender();
                    }
                });
            });

            //自定义链接
            $ctrl.find('input[name="customlink"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.images[data.temp.targetIndex].link.link = $(this).val();
                data.content.images[data.temp.targetIndex].link.smallprog_link = $(this).val();
            });

            //选择图片
            $ctrl.find('.j-selectimg').click(function () {
                HiShop.popbox.ImgPicker(function (imgs) {
                    data.content.images[data.temp.targetIndex].imgSrc = imgs[0]; //获取第一张图片
                    if (data.temp.targetIndex === 0 && data.content.showType < 4) {
                        var img = new Image();
                        img.src = imgs[0];
                        img.onload = function () {
                            data.temp.imgRatio = img.height / img.width
                            data.content.images.forEach(function (item, index) {
                                if (item) {
                                    item.height = data.temp.imgRatio * data.temp.models[data.content.showType - 1].height[index]
                                }
                            })
                            reRender();
                        }
                    } else {
                        reRender();
                    }
                });
            });

            //切换模板
            $ctrl.find('.models-btn li').click(function () {
                var index = $(this).index()
                var model = data.temp.models[index]
                data.content.showType = model.value
                var images = data.content.images
                images.length = model.len

                // 重置数据
                if (model.value === 8) {
                    images = []
                    data.content.cellSelected = []
                    data.content.rowColumn = 4
                    data.temp.targetIndex = -1
                } else {
                    data.temp.targetIndex = 0
                    for (var i = 0; i < images.length; i++) {
                        if (!images[i]) {
                            images[i] = { imgSrc: '', title: '', link: { name: '' } }
                        }
                        images[i].width = data.temp.models[data.content.showType - 1].width[i]
                        images[i].height = data.temp.models[data.content.showType - 1].height[i]
                        images[i].top = data.temp.models[data.content.showType - 1].top[i]
                        images[i].left = data.temp.models[data.content.showType - 1].left[i]
                    }

                    // 前三种模式计算高度
                    if (data.content.showType < 4) {
                        if (images[0] && images[0].imgSrc) {
                            images.forEach(function (item, index) {
                                if (item) {
                                    item.height = data.temp.imgRatio * data.temp.models[data.content.showType - 1].width[index]
                                }
                            })
                        }
                    }
                }

                data.content.images = images
                reRender()
            });

            $ctrl.find('.cube-item').click(function () {
                var index = $(this).index()
                if (data.content.showType !== 8) {
                    if (!data.content.images[index]) {
                        data.content.images[index] = {
                            imgSrc: '',
                            title: '',
                            link: { name: '' }
                        }
                    }
                    data.content.images[index].width = data.temp.models[data.content.showType - 1].width[index]
                    data.content.images[index].height = data.content.showType < 4 && data.content.images[0].imgSrc ? data.content.images[0].height : data.temp.models[data.content.showType - 1].height[index]
                    data.content.images[index].top = data.temp.models[data.content.showType - 1].top[index]
                    data.content.images[index].left = data.temp.models[data.content.showType - 1].left[index]
                } else {
                    data.temp.cellStart = ''
                    data.temp.cellSelecting = []
                }
                data.temp.targetIndex = index
                reRender()
            })

            //切换魔方密度
            $ctrl.find('select[name="thickness"]').change(function () {
                data.temp.targetIndex = -1
                data.content.cellSelected = []
                data.content.images = []
                data.content.rowColumn = $(this).val()
                reRender()
            });

            // 魔方自定义
            $ctrl.find('.cube-custom li').click(function () {
                var x = $(this).data('x'),
                    y = $(this).data('y');
                if (!data.temp.cellStart) {
                    data.temp.cellStart = x + '_' + y
                    data.temp.cellSelecting.push(x + '_' + y)
                    $(this).attr('class', 'selecting')
                } else {
                    // 计算选中区域位置
                    var cellSize = 320 / data.content.rowColumn
                    var posStart = data.temp.cellSelecting[data.temp.cellSelecting.length - 1].split('_')
                    var posEnd = data.temp.cellSelecting[0].split('_')
                    var width = (posEnd[0] - posStart[0] + 1) * cellSize
                    var height = (posEnd[1] - posStart[1] + 1) * cellSize

                    var left = posStart[0] * cellSize
                    var top = posStart[1] * cellSize

                    data.content.images.push({
                        width,
                        height,
                        left,
                        top,
                        imgSrc: '',
                        title: '',
                        link: {
                            name: ''
                        },
                        posStart,
                        posEnd
                    })
                    data.temp.targetIndex = data.content.images.length - 1
                    data.content.cellSelected.push(...data.temp.cellSelecting)
                    data.temp.cellStart = ''
                    data.temp.cellSelecting = []
                    reRender()
                }
            })
            $ctrl.find('.cube-custom li').mouseenter(function () {
                var x = $(this).data('x'),
                    y = $(this).data('y');
                if (data.temp.cellStart) {
                    var selecting = []
                    var xStart = parseInt(data.temp.cellStart.split('_')[0])
                    var yStart = parseInt(data.temp.cellStart.split('_')[1])

                    var minX = Math.min(xStart, x)
                    var maxX = Math.max(xStart, x)
                    var minY = Math.min(yStart, y)
                    var maxY = Math.max(yStart, y)

                    var hasSelect = false
                    for (var i = maxX; i >= minX; i--) {
                        for (var j = maxY; j >= minY; j--) {
                            var cell = i + '_' + j
                            if (!selecting.indexOf(cell) > -1) {
                                selecting.push(cell)
                            }
                            if (data.content.cellSelected.indexOf(cell) > -1) {
                                hasSelect = true
                                break
                            }
                        }
                    }

                    if (!hasSelect) {
                        data.temp.cellSelecting = selecting
                        $ctrl.find('.cube-row').each(function (x) {
                            $(this).find('li').each(function (y) {
                                if (data.temp.cellSelecting.indexOf(x + '_' + y) > -1) {
                                    $(this).attr('class', 'selecting')
                                } else {
                                    $(this).attr('class', '')
                                }
                            })
                        })
                    }
                }
                // reRender()
            })
            $ctrl.find('.cube-custom').mouseleave(function () {
                data.temp.cellStart = ''
                data.temp.cellSelecting = []
                $ctrl.find('.cube-row li').each(function () {
                    $(this).attr('class', '')
                })
            })
            $ctrl.find('.cube-item i').click(function () {
                var index = $(this).data('index'),
                    curItem = data.content.images[index]
                data.temp.targetIndex = data.content.images.length > 1 ? 0 : -1
                data.content.images.splice(index, 1)

                // 删除图片块释放格子
                for (var i = curItem.posStart[0]; i <= curItem.posEnd[0]; i++) {
                    for (var j = curItem.posStart[1]; j <= curItem.posEnd[1]; j++) {
                        var cell = i + '_' + j
                        data.content.cellSelected.splice(data.content.cellSelected.indexOf(cell), 1)
                    }
                }
                reRender()
            })

            //这里需要加载颜色选择器效果
            $ctrl.find('.color-select').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    reRender();
                }
            });

            $ctrl.find('.pageMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.pageMargin,
                slide: function (event, ui) {
                    $ctrl.find('.pageMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.pageMargin = parseInt(ui.value);
                    reRender()
                }
            });

            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.verticalMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });

            $ctrl.find('.imgMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.imgMargin,
                slide: function (event, ui) {
                    $ctrl.find('.imgMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.imgMargin = parseInt(ui.value);
                    reRender()
                }
            });

        };

        //模块类型5 文字链接
        HiShop.DIY.Unit.event_type5 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type5').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type5').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);
                HiShop.DIY.Unit.event_type5($ctrl, data);
            }

            //改变标题
            $ctrl.find('input[name="title"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.dataset[index].showtitle = $(this).val();
                reRender();
            });

            //改变链接
            $ctrl.find('.droplist li').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                HiShop.popbox.dplPickerColletion({
                    linkType: $(this).data('val'),
                    callback: function (item, type) {
                        data.content.dataset[index].title = item.title;
                        data.content.dataset[index].showtitle = item.title;
                        data.content.dataset[index].link = item.link;
                        data.content.dataset[index].pc_link = item.pc_link;
                        data.content.dataset[index].smallprog_link = item.smallprog_link;
                        data.content.dataset[index].linkType = type;
                        reRender();
                    }
                });
            });

            //自定义链接
            $ctrl.find('input[name="customlink"]').change(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.dataset[index].link = $(this).val();
                data.content.dataset[index].smallprog_link = $(this).val();
            });

            //上移
            $ctrl.find('.j-moveup').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();

                if (index == 0) return; //第一个导航不可再向上移动

                //替换缓存数组中的位置
                var tmpdata = data.content.dataset.slice(index, index + 1)[0];
                data.content.dataset.splice(index, 1);
                data.content.dataset.splice(index - 1, 0, tmpdata);

                reRender(); //更新视图
            });

            //下移
            $ctrl.find('.j-movedown').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index(),
                    len = data.content.dataset.length;

                if (index == len - 1) return; //最后一个导航不可再向下移动

                //替换缓存数组中的位置
                var tmpdata = data.content.dataset.slice(index, index + 1)[0];
                data.content.dataset.splice(index, 1);
                data.content.dataset.splice(index + 1, 0, tmpdata);

                reRender(); //更新视图
            });

            //添加
            $ctrl.find('.ctrl-item-list-add').click(function () {
                var newdata = {
                    linkType: 0,
                    link: '',
                    smallprog_link: '',
                    title: '',
                    showtitle: ''
                };
                data.content.dataset.push(newdata);
                reRender();
            });

            //删除
            $ctrl.find('.j-del').click(function () {
                var index = $(this).parents('li.ctrl-item-list-li').index();
                data.content.dataset.splice(index, 1);
                reRender();
            });
        };
        //模块类型6 辅助线
        HiShop.DIY.Unit.event_type6 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type6').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type6').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type6($ctrl, data);
            }

            //这里需要加载颜色选择器效果
            $ctrl.find('.color-select').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.color = hex;
                    reRender();
                }
            });

            //设置边距
            $ctrl.find('input[name="margin"]').change(function () {
                data.content.margin = $(this).val();
                reRender();
            });

            // 设置实、虚、点线
            $ctrl.find('input[name="style"]').change(function () {
                data.content.style = $(this).val();
                reRender();
            });
            // 设置粗、中等、细
            $ctrl.find('input[name="thickness"]').change(function () {
                data.content.thickness = $(this).val();
                reRender();
            });

        };
        //模块类型7 辅助空白
        HiShop.DIY.Unit.event_type7 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type7').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type7').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type7($ctrl, data);
            }

            $ctrl.find('#slider').slider({
                min: 10,
                max: 100,
                step: 1,
                animate: 'fast',
                value: data.content.height,
                slide: function (event, ui) {
                    $conitem.find('.custom-space').css('height', ui.value);
                    $ctrl.find('.j-ctrl-showheight').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.height = parseInt(ui.value);
                }
            });

            //这里需要加载颜色选择器效果
            $ctrl.find('.color-select').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    $ctrl.find('.color-select span').css('background', hex)
                    $conitem.find('.custom-space').css('backgroundColor', hex)
                }
            });
        };
        //模块类型8 商品
        HiShop.DIY.Unit.event_type8 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type8').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type8').html(); //控制内容模板

            data.dom_ctrl = ctrldom;
            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.component-preview').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type8($ctrl, data);
            }

            //添加商品
            $ctrl.find(".j-addgoods").click(function () {
                HiShop.popbox.GoodsAndGroupPicker("goodsMulti", function (list) {
                    data.content.goodslist = [];//xlx修改为清除上次选择，直接传入方法内处理
                    _.each(list, function (goods) {
                        data.content.goodslist.push(goods);
                    });
                    reRender();
                }, data.content.goodslist);
                return false;
            });
            //删除商品
            $ctrl.find(".j-delgoods").click(function () {
                var index = $(this).parents("li").index();
                data.content.goodslist.splice(index, 1);
                reRender();
                return false;
            });
            // 选择商品
            $ctrl.find('input[name="productType"]').change(function () {
                var value = parseInt($(this).val());
                data.content.productType = value;
                data.content.goodslist = [];
                if (value == 1) {
                    loadCategory()
                    data.content.sort = '';
                    data.content.isasc = true;
                    data.content.productCount = 2;
                    data.content.categoryId = 0
                }//选择分类重置
                reRender();
            });
            // 加载分类
            var loadCategory = function () {
                $.ajax({
                    url: HiShop.Config.AjaxUrl.Category,
                    type: "post",
                    dataType: "json",
                    data: {
                        type: 1,
                        platform: 1
                    },
                    success: function (res) {
                        data.content.categories = res.list;
                        reRender();
                    }
                })
            };
            // 商品分类变化
            $ctrl.find('select[name="categoryId"]').change(function () {
                var value = parseInt($(this).val());
                data.content.categoryId = value;
                laodProduct(data.content.categoryId, data.content.productCount, data.content.sort, data.content.isasc)
            })
            // 商品数量
            $ctrl.find('input[name="productCount"]').change(function () {
                var value = parseInt($(this).val());
                if (value > 50) {
                    value = 50
                }
                if (!value || value <= 0) {
                    value = 2
                }
                data.content.productCount = value;
                laodProduct(data.content.categoryId, data.content.productCount, data.content.sort, data.content.isasc)
            })
            // 商品排序
            $ctrl.find('select[name="sortType"]').change(function () {
                var value = parseInt($(this).val())
                data.content.sortType = value
                switch (value) {
                    case 1:
                        data.content.sort = 'addeddate';
                        data.content.isasc = false;
                        break;
                    case 2:
                        data.content.sort = 'addeddate';
                        data.content.isasc = true;
                        break;
                    case 3:
                        data.content.sort = 'salecounts';
                        data.content.isasc = false;
                        break;
                    case 4:
                        data.content.sort = 'salecounts';
                        data.content.isasc = true;
                        break;
                    case 5:
                        data.content.sort = 'displaysale';
                        data.content.isasc = false;
                        break;
                    case 6:
                        data.content.sort = 'displaysale';
                        data.content.isasc = true;
                        break;
                    case 7:
                        data.content.sort = 'saleprice';
                        data.content.isasc = false;
                        break;
                    case 8:
                        data.content.sort = 'saleprice';
                        data.content.isasc = true;
                        break;
                    case 9:
                        data.content.sort = 'displaysequence';
                        data.content.isasc = false;
                        break;
                    case 10:
                        data.content.sort = 'displaysequence';
                        data.content.isasc = true;
                        break;
                }
                laodProduct(data.content.categoryId, data.content.productCount, data.content.sort, data.content.isasc)
            })
            // 根据分类获取商品
            var laodProduct = function (categoryId, productCount, sort, isasc) {
                $.ajax({
                    url: HiShop.Config.AjaxUrl.GoodsList,
                    type: "post",
                    dataType: "json",
                    data: {
                        p: 1,
                        platform: 1,
                        size: productCount || 2,
                        categoryId: categoryId || 0,
                        sort: sort || '',
                        isasc: isasc,
                    },
                    success: function (res) {
                        data.content.goodslist = [];
                        const oddArr = [];
                        const evenArr = [];
                        if (data.content.listStyle != 6) {
                            _.each(res.list, function (goods) {
                                data.content.goodslist.push(goods);
                            });
                        } else {
                            res.list.map((item, index) => {
                                if ((index) % 2) {
                                    evenArr.push(item)
                                } else {
                                    oddArr.push(item)
                                }
                            })
                            data.content.goodslist = [...evenArr, ...oddArr]
                        }
                        reRender();
                    }
                })
            }
            //列表样式
            $ctrl.find('input[name="showType"]').change(function () {
                var value = parseInt($(this).val())
                data.content.showType = value;
                // 一行3个、横向滚动只能是图标按钮
                if ((value === 3 || value === 6) && data.content.btnStyle > 4) {
                    data.content.btnStyle = 1
                }
                if (value !== 3 && value !== 6 && data.content.btnStyle < 5) {
                    data.content.btnStyle = 5
                }
                // 只有一行两个允许5、6商品样式
                if (value !== 2 && data.content.listStyle > 4) {
                    data.content.listStyle = 3
                }
                // 一行3个、横向滚动不能显示按钮
                if ((value === 3 || value === 6) && data.content.textAlign) {
                    data.content.showBtn = false
                }
                // 详细列表必须商品名称，不能居中，只能1：1图片
                if (value === 4) {
                    data.content.showName = true
                    data.content.textAlign = 0
                    data.content.imgRatio = 1
                }
                reRender();
            });
            //选择图片
            $ctrl.find('.choose-img').click(function (e) {
                e.stopPropagation();
                var type = $(this).data('type');
                if (type == 'tagImg') {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.tagImg = imgs[0];
                        reRender();
                    });
                } else if (type == 'btnImg') {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.btnImg = imgs[0];
                        reRender();
                    });
                } else {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.nameIcon = imgs[0];
                        reRender();
                    });
                }
            });
            //背景颜色
            $ctrl.find('.bgColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    $ctrl.find('.bgColor span').css('background', hex)
                    reRender()
                }
            });
            // 重置背景色
            $ctrl.find('.reset-bg').click(function () {
                data.content.bgColor = ''
                $ctrl.find('.bgColor span').css('background', '')
                reRender()
            })
            //商品名颜色
            $ctrl.find('.nameColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.nameColor = hex;
                    $ctrl.find('.nameColor span').css('background', hex)
                    reRender()
                }
            });
            //卖点/描述颜色
            $ctrl.find('.descColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.descColor = hex;
                    $ctrl.find('.descColor span').css('background', hex)
                    reRender()
                }
            });
            //价格颜色
            $ctrl.find('.priceColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.priceColor = hex;
                    $ctrl.find('.priceColor span').css('background', hex)
                    reRender()
                }
            });
            //显示内容
            $ctrl.find('input[name="showName"]').change(function () {
                data.content.showName = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showPrice"]').change(function () {
                data.content.showPrice = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showBtn"]').change(function () {
                data.content.showBtn = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showDesc"]').change(function () {
                data.content.showDesc = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showTag"]').change(function () {
                data.content.showTag = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showNameIcon"]').change(function () {
                data.content.showNameIcon = this.checked;
                reRender();
            });
            $ctrl.find('input[name="nameLine"]').change(function () {
                data.content.nameLine = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="tagStyle"]').change(function () {
                data.content.tagStyle = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="btnStyle"]').change(function () {
                data.content.btnStyle = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="btnText"]').change(function () {
                data.content.btnText = $(this).val();
                reRender();
            });

            // 直角圆角
            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });

            // 页面边距
            $ctrl.find('.pageMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.pageMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-pageMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.pageMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 上下边距
            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-verticalMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 商品间距
            $ctrl.find('.productMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.productMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-productMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.productMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 商品样式
            $ctrl.find('input[name="listStyle"]').change(function () {
                var value = parseInt($(this).val())
                data.content.listStyle = value;
                // 促销样式不能居中、显示标题及描述
                if (value === 5) {
                    data.content.textAlign = 0
                    data.content.showName = false
                    data.content.showDesc = false
                }
                reRender();
            });
            // 商品倒角
            $ctrl.find('input[name="radius"]').change(function () {
                data.content.radius = parseInt($(this).val());
                reRender();
            });
            // 图片比例
            $ctrl.find('input[name="imgRatio"]').change(function () {
                data.content.imgRatio = parseFloat($(this).val());
                reRender();
            });
            // 图片填充
            $ctrl.find('input[name="imgFill"]').change(function () {
                data.content.imgFill = parseInt($(this).val());
                reRender();
            });
            // 文本样式
            $ctrl.find('input[name="fontStyle"]').change(function () {
                data.content.fontStyle = parseInt($(this).val());
                reRender();
            });
            // 文本对齐
            $ctrl.find('input[name="textAlign"]').change(function () {
                var value = parseInt($(this).val())
                data.content.textAlign = value;
                // 居中只能使用非图标按钮，一行3个、横向滚动不需要设置
                if (value && data.content.btnStyle < 5 && data.content.showType !== 3 && data.content.showType !== 6) {
                    data.content.btnStyle = 5
                }
                // 居中且一行3个不能显示按钮
                if (value && (data.content.showType === 3 || data.content.showType === 6)) {
                    data.content.showBtn = false
                }
                reRender();
            });
        };
        //模块类型9 搜索
        HiShop.DIY.Unit.event_type9 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type9').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type9').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.cap-search-box').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type9($ctrl, data);
            }
            //预设文本
            $ctrl.find('input[name="title"]').change(function () {
                data.content.text = $(this).val();
                reRender();
            });
            // 直角圆角
            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });
            // 对齐方式
            $ctrl.find('input[name="moduleText"]').change(function () {
                data.content.moduleText = parseInt($(this).val());
                reRender();
            })
            // 上下距离
            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-verticalMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 高度
            $ctrl.find('.height').slider({
                min: 28,
                max: 40,
                step: 1,
                animate: 'fast',
                value: data.content.height,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-height').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.height = parseInt(ui.value);
                    reRender()
                }
            });
            //这里需要加载颜色选择器效果
            $ctrl.find('.bgColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    $ctrl.find('.bgColor span').css('background', hex)
                    reRender()
                }
            });
            $ctrl.find('.textColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.textColor = hex;
                    $ctrl.find('.textColor span').css('background', hex)
                    reRender()
                }
            });
            $ctrl.find('.frameColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.frameColor = hex;
                    $ctrl.find('.frameColor span').css('background', hex)
                    reRender()
                }
            });
        };

        // 模块类型10 优惠券
        HiShop.DIY.Unit.event_type10 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type10').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type10').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function (callback) {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type10($ctrl, data);
                if (callback) callback();
            }

            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });

            var loadCoupon = function () {
                $.ajax({
                    url: HiShop.Config.AjaxUrl.Coupons,
                    type: "post",
                    dataType: "json",
                    data: {
                        p: 1,
                        size: data.content.count,
                        franchiseeId: parseInt($('#vfranchiseeId').val())
                    },
                    success: function (res) {
                        data.content.coupons = res.list;
                        reRender();
                    }
                })
            }

            $ctrl.find('input[name="couponType"]').change(function () {
                var couponType = parseInt($(this).val());
                data.content.couponType = couponType;
                if (couponType) {
                    loadCoupon()
                } else {
                    data.content.coupons = [];
                    reRender();
                }
            });

            $ctrl.find('input[name="count"]').change(function () {
                data.content.count = isNaN(parseInt($(this).val())) ? 1 : parseInt($(this).val());
                loadCoupon()
            });

            $ctrl.find('input[name="showType"]').change(function () {
                data.content.showType = parseInt($(this).val());
                reRender();
            });

            //这里需要加载颜色选择器效果
            $ctrl.find('.color-select').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    reRender();
                }
            });

            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.verticalMargin').next().text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });


            //添加优惠券
            $ctrl.find('.btn-list-add').click(function () {
                HiShop.popbox.CouponPicker('all', function (coupon) {
                    data.content.coupons.push(coupon)
                    reRender();
                }, data.content.coupons);
            });

            //删除
            $ctrl.find('.icon-icon_trash').click(function () {
                var index = $(this).data('index');
                data.content.coupons.splice(index, 1);
                reRender();
            });
        };

        // 直播组件菜单
        HiShop.DIY.Unit.event_type11 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type11').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type11').html(); //控制内容模板

            data.dom_ctrl = ctrldom;

            //重新渲染数据
            var reRender = function (callback) {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.members_con').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);

                HiShop.DIY.Unit.event_type11($ctrl, data);
                if (callback) callback();
            }
            //改变布局
            $ctrl.find('input[name="layout"]').change(function () {
                var val = parseInt($(this).val());

                data.content.layout = val; //同步数据到缓存
                reRender();
            });

            //删除商品
            $ctrl.find('.j-delgoods').click(function () {
                var index = $(this).parents('li').index();
                data.content.goodslist.splice(index, 1);
                reRender();
                return false;
            });

            //添加直播间
            $ctrl.find('.j-addlives').click(function () {

                HiShop.popbox.LivesPicker('goodsMulti', function (list) {
                    data.content.goodslist = []; //xlx修改为清除上次选择，直接传入方法内处理
                    _.each(list, function (goods) {
                        data.content.goodslist.push(goods);
                    });
                    reRender();
                }, data.content.goodslist);
                return false;
            });
        };

        //模块类型14 限时折扣
        HiShop.DIY.Unit.event_type14 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type14').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type14').html(); //控制内容模板

            data.dom_ctrl = ctrldom;
            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.component-preview').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);
                HiShop.DIY.Unit.event_type14($ctrl, data);
            }

            //添加商品
            $ctrl.find(".j-addgoods").click(function () {
                HiShop.popbox.GoodsAndGroupPicker("goodsMulti", function (list) {
                    data.content.goodslist = [];//xlx修改为清除上次选择，直接传入方法内处理
                    _.each(list, function (goods) {
                        goods.percentage = 100 - goods.number / (goods.stock + goods.number) * 100
                        goods.subtractPrice = floatSub(goods.saleprice, goods.price)
                        goods.timeText = countDownTime(goods.beginSec > 0 ? goods.beginSec : goods.endSec)
                        data.content.goodslist.push(goods);
                    });
                    reRender();
                }, data.content.goodslist, areaPath + "/TemplateVisualizationAjax/Hi_Ajax_LimitBuy");
                return false;
            });

            var floatSub = function (arg1, arg2) {
                var r1, r2, m, n;
                try {
                    r1 = arg1.toString().split(".")[1].length
                } catch (e) { r1 = 0 }
                try {
                    r2 = arg2.toString().split(".")[1].length
                } catch (e) { r2 = 0 }
                m = Math.pow(10, Math.max(r1, r2));
                //动态控制精度长度
                n = (r1 >= r2) ? r1 : r2;
                return ((arg1 * m - arg2 * m) / m).toFixed(n);
            }
            var countDownTime = function (time) {
                time = Math.abs(time)
                var day = 0, hour = 0, minute = 0, second = 0;
                day = Math.floor(time / (24 * 60 * 60))
                hour = Math.floor(time / (60 * 60) - (day * 24))
                minute = Math.floor(time / 60 - (day * 24 * 60) - (hour * 60))
                second = Math.floor(time - (day * 24 * 60 * 60) - (hour * 60 * 60) - (minute * 60))
                if (hour < 10) {
                    hour = '0' + hour
                }
                if (minute < 10) {
                    minute = '0' + minute
                }
                if (second < 10) {
                    second = '0' + second
                }
                return {
                    day,
                    hour,
                    minute,
                    second
                }
            }
            //删除商品
            $ctrl.find(".j-delgoods").click(function () {
                var index = $(this).parents("li").index();
                data.content.goodslist.splice(index, 1);
                reRender();
                return false;
            });

            //列表样式
            $ctrl.find('input[name="showType"]').change(function () {
                var value = parseInt($(this).val())
                data.content.showType = value;
                // 一行3个、横向滚动只能是图标按钮
                if ((value === 3 || value === 6) && data.content.btnStyle > 4) {
                    data.content.btnStyle = 1
                }
                if (value !== 3 && value !== 6 && data.content.btnStyle < 5) {
                    data.content.btnStyle = 5
                }
                // 只有一行两个允许5、6商品样式
                if (value !== 2 && data.content.listStyle > 4) {
                    data.content.listStyle = 3
                }
                // 一行3个、横向滚动不能显示按钮
                if ((value === 3 || value === 6) && data.content.textAlign) {
                    data.content.showBtn = false
                }
                // 详细列表必须商品名称，不能居中，只能1：1图片
                if (value === 4) {
                    data.content.showName = true
                    data.content.textAlign = 0
                    data.content.imgRatio = 1
                }
                reRender();
            });
            //选择图片
            $ctrl.find('.choose-img').click(function (e) {
                e.stopPropagation();
                var type = $(this).data('type');
                if (type == 'tagImg') {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.tagImg = imgs[0];
                        reRender();
                    });
                } else if (type == 'btnImg') {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.btnImg = imgs[0];
                        reRender();
                    });
                } else {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.nameIcon = imgs[0];
                        reRender();
                    });
                }
            });
            //背景颜色
            $ctrl.find('.bgColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    $ctrl.find('.bgColor span').css('background', hex)
                    reRender()
                }
            });
            // 重置背景色
            $ctrl.find('.reset-bg').click(function () {
                data.content.bgColor = ''
                $ctrl.find('.bgColor span').css('background', '')
                reRender()
            })
            //商品名颜色
            $ctrl.find('.nameColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.nameColor = hex;
                    $ctrl.find('.nameColor span').css('background', hex)
                    reRender()
                }
            });
            //价格颜色
            $ctrl.find('.priceColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.priceColor = hex;
                    $ctrl.find('.priceColor span').css('background', hex)
                    reRender()
                }
            });
            //显示内容
            $ctrl.find('input[name="showName"]').change(function () {
                data.content.showName = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showOldPrice"]').change(function () {
                data.content.showOldPrice = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showStock"]').change(function () {
                data.content.showStock = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showTime"]').change(function () {
                data.content.showTime = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showProcess"]').change(function () {
                data.content.showProcess = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showPrice"]').change(function () {
                data.content.showPrice = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showBtn"]').change(function () {
                data.content.showBtn = this.checked;
                reRender();
            });

            $ctrl.find('input[name="showNameIcon"]').change(function () {
                data.content.showNameIcon = this.checked;
                reRender();
            });
            $ctrl.find('input[name="nameLine"]').change(function () {
                data.content.nameLine = parseInt($(this).val());
                reRender();
            });

            $ctrl.find('input[name="btnStyle"]').change(function () {
                data.content.btnStyle = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="btnText"]').change(function () {
                data.content.btnText = $(this).val();
                reRender();
            });

            // 直角圆角
            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });

            // 页面边距
            $ctrl.find('.pageMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.pageMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-pageMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.pageMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 上下边距
            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-verticalMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 商品间距
            $ctrl.find('.productMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.productMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-productMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.productMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 商品样式
            $ctrl.find('input[name="listStyle"]').change(function () {
                var value = parseInt($(this).val())
                data.content.listStyle = value;
                // 促销样式不能居中、显示标题及描述
                if (value === 5) {
                    data.content.textAlign = 0
                    data.content.showName = false
                }
                reRender();
            });
            // 商品倒角
            $ctrl.find('input[name="radius"]').change(function () {
                data.content.radius = parseInt($(this).val());
                reRender();
            });
            // 图片比例
            $ctrl.find('input[name="imgRatio"]').change(function () {
                data.content.imgRatio = parseFloat($(this).val());
                reRender();
            });
            // 图片填充
            $ctrl.find('input[name="imgFill"]').change(function () {
                data.content.imgFill = parseInt($(this).val());
                reRender();
            });
            // 文本样式
            $ctrl.find('input[name="fontStyle"]').change(function () {
                data.content.fontStyle = parseInt($(this).val());
                reRender();
            });
            // 文本对齐
            $ctrl.find('input[name="textAlign"]').change(function () {
                var value = parseInt($(this).val())
                data.content.textAlign = value;
                // 居中只能使用非图标按钮，一行3个、横向滚动不需要设置
                if (value && data.content.btnStyle < 5 && data.content.showType !== 3 && data.content.showType !== 6) {
                    data.content.btnStyle = 5
                }
                // 居中且一行3个不能显示按钮
                if (value && (data.content.showType === 3 || data.content.showType === 6)) {
                    data.content.showBtn = false
                }
                reRender();
            });
        };
        //模块类型15 拼团
        HiShop.DIY.Unit.event_type15 = function (ctrldom, data) {
            var $conitem = data.dom_conitem, //手机内容
                $ctrl = ctrldom, //控制内容
                tpl_con = $('#tpl_diy_con_type15').html(), //手机内容模板
                tpl_ctrl = $('#tpl_diy_ctrl_type15').html(); //控制内容模板

            data.dom_ctrl = ctrldom;
            //重新渲染数据
            var reRender = function () {
                var $render = $(_.template(tpl_con, data));
                $conitem.find('.component-preview').remove().end().append($render);

                var $render_ctrl = $(_.template(tpl_ctrl, data));
                $ctrl.empty().append($render_ctrl);
                HiShop.DIY.Unit.event_type15($ctrl, data);
            }

            //添加商品
            $ctrl.find(".j-addgoods").click(function () {
                HiShop.popbox.GoodsAndGroupPicker("goodsMulti", function (list) {
                    data.content.goodslist = [];//xlx修改为清除上次选择，直接传入方法内处理
                    _.each(list, function (goods) {
                        goods.timeText = countDownTime(goods.beginSec > 0 ? goods.beginSec : goods.endSec)
                        data.content.goodslist.push(goods);
                    });
                    reRender();
                }, data.content.goodslist, areaPath + "/TemplateVisualizationAjax/Hi_Ajax_FightGroup");
                return false;
            });
            var countDownTime = function (time) {
                time = Math.abs(time)
                var day = 0, hour = 0, minute = 0, second = 0;
                day = Math.floor(time / (24 * 60 * 60))
                hour = Math.floor(time / (60 * 60) - (day * 24))
                minute = Math.floor(time / 60 - (day * 24 * 60) - (hour * 60))
                second = Math.floor(time - (day * 24 * 60 * 60) - (hour * 60 * 60) - (minute * 60))
                if (hour < 10) {
                    hour = '0' + hour
                }
                if (minute < 10) {
                    minute = '0' + minute
                }
                if (second < 10) {
                    second = '0' + second
                }
                return {
                    day,
                    hour,
                    minute,
                    second
                }
            }
            //删除商品
            $ctrl.find(".j-delgoods").click(function () {
                var index = $(this).parents("li").index();
                data.content.goodslist.splice(index, 1);
                reRender();
                return false;
            });

            //列表样式
            $ctrl.find('input[name="showType"]').change(function () {
                var value = parseInt($(this).val())
                data.content.showType = value;
                // 一行3个、横向滚动只能是图标按钮
                if ((value === 3 || value === 6) && data.content.btnStyle > 4) {
                    data.content.btnStyle = 1
                }
                if (value !== 3 && value !== 6 && data.content.btnStyle < 5) {
                    data.content.btnStyle = 5
                }
                // 只有一行两个允许5、6商品样式
                if (value !== 2 && data.content.listStyle > 4) {
                    data.content.listStyle = 3
                }
                // 一行3个、横向滚动不能显示按钮
                if ((value === 3 || value === 6) && data.content.textAlign) {
                    data.content.showBtn = false
                }
                // 详细列表必须商品名称，不能居中，只能1：1图片
                if (value === 4) {
                    data.content.showName = true
                    data.content.textAlign = 0
                    data.content.imgRatio = 1
                }
                reRender();
            });
            //选择图片
            $ctrl.find('.choose-img').click(function (e) {
                e.stopPropagation();
                var type = $(this).data('type');
                if (type == 'tagImg') {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.tagImg = imgs[0];
                        reRender();
                    });
                } else if (type == 'btnImg') {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.btnImg = imgs[0];
                        reRender();
                    });
                } else {
                    HiShop.popbox.ImgPicker(function (imgs) {
                        data.content.nameIcon = imgs[0];
                        reRender();
                    });
                }
            });
            //背景颜色
            $ctrl.find('.bgColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.bgColor = hex;
                    $ctrl.find('.bgColor span').css('background', hex)
                    reRender()
                }
            });
            // 重置背景色
            $ctrl.find('.reset-bg').click(function () {
                data.content.bgColor = ''
                $ctrl.find('.bgColor span').css('background', '')
                reRender()
            })
            //商品名颜色
            $ctrl.find('.nameColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.nameColor = hex;
                    $ctrl.find('.nameColor span').css('background', hex)
                    reRender()
                }
            });
            //价格颜色
            $ctrl.find('.priceColor').ColorPicker({
                onShow: function (colpkr) {
                    $(colpkr).fadeIn(500).siblings('.colorpicker').remove();
                    return false;
                },
                onHide: function (colpkr) {
                    $(colpkr).fadeOut(500);
                    return false;
                },
                onChange: function (hsb, hex, rgb) {
                    var hex = '#' + hex;
                    data.content.priceColor = hex;
                    $ctrl.find('.priceColor span').css('background', hex)
                    reRender()
                }
            });
            //显示内容
            $ctrl.find('input[name="showName"]').change(function () {
                data.content.showName = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showOldPrice"]').change(function () {
                data.content.showOldPrice = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showStock"]').change(function () {
                data.content.showStock = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showTime"]').change(function () {
                data.content.showTime = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showProcess"]').change(function () {
                data.content.showProcess = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showPrice"]').change(function () {
                data.content.showPrice = this.checked;
                reRender();
            });
            $ctrl.find('input[name="showBtn"]').change(function () {
                data.content.showBtn = this.checked;
                reRender();
            });

            $ctrl.find('input[name="showNameIcon"]').change(function () {
                data.content.showNameIcon = this.checked;
                reRender();
            });
            $ctrl.find('input[name="nameLine"]').change(function () {
                data.content.nameLine = parseInt($(this).val());
                reRender();
            });

            $ctrl.find('input[name="btnStyle"]').change(function () {
                data.content.btnStyle = parseInt($(this).val());
                reRender();
            });
            $ctrl.find('input[name="btnText"]').change(function () {
                data.content.btnText = $(this).val();
                reRender();
            });

            // 直角圆角
            $ctrl.find('input[name="moduleRadius"]').change(function () {
                data.content.moduleRadius = parseInt($(this).val());
                reRender();
            });

            // 页面边距
            $ctrl.find('.pageMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.pageMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-pageMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.pageMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 上下边距
            $ctrl.find('.verticalMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.verticalMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-verticalMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.verticalMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 商品间距
            $ctrl.find('.productMargin').slider({
                min: 0,
                max: 30,
                step: 1,
                animate: 'fast',
                value: data.content.productMargin,
                slide: function (event, ui) {
                    $ctrl.find('.j-ctrl-productMargin').text(ui.value + '像素');
                },
                stop: function (event, ui) {
                    data.content.productMargin = parseInt(ui.value);
                    reRender()
                }
            });
            // 商品样式
            $ctrl.find('input[name="listStyle"]').change(function () {
                var value = parseInt($(this).val())
                data.content.listStyle = value;
                // 促销样式不能居中、显示标题及描述
                if (value === 5) {
                    data.content.textAlign = 0
                    data.content.showName = false
                }
                reRender();
            });
            // 商品倒角
            $ctrl.find('input[name="radius"]').change(function () {
                data.content.radius = parseInt($(this).val());
                reRender();
            });
            // 图片比例
            $ctrl.find('input[name="imgRatio"]').change(function () {
                data.content.imgRatio = parseFloat($(this).val());
                reRender();
            });
            // 图片填充
            $ctrl.find('input[name="imgFill"]').change(function () {
                data.content.imgFill = parseInt($(this).val());
                reRender();
            });
            // 文本样式
            $ctrl.find('input[name="fontStyle"]').change(function () {
                data.content.fontStyle = parseInt($(this).val());
                reRender();
            });
            // 文本对齐
            $ctrl.find('input[name="textAlign"]').change(function () {
                var value = parseInt($(this).val())
                data.content.textAlign = value;
                // 居中只能使用非图标按钮，一行3个、横向滚动不需要设置
                if (value && data.content.btnStyle < 5 && data.content.showType !== 3 && data.content.showType !== 6) {
                    data.content.btnStyle = 5
                }
                // 居中且一行3个不能显示按钮
                if (value && (data.content.showType === 3 || data.content.showType === 6)) {
                    data.content.showBtn = false
                }
                reRender();
            });
        };
    });