$(function() {
  var iscommunityhomevisualconfig = false;
  var currentUrl = location.href.toLowerCase();
  if (currentUrl.indexOf('shopbranch/communityhomevisualconfig') != -1) {
    iscommunityhomevisualconfig = true;
  }
  if (iscommunityhomevisualconfig) {
    var modulecommunityhomevisual = {
      id: HiShop.DIY.getTimestamp(), //模块ID
      type: 99, //模块类型
      draggable: false, //是否可拖动
      sort: 0, //排序
      content: '' //模块内容
    };
    HiShop.DIY.add(modulecommunityhomevisual, false);
  }

  setTimeout(function() {
    abc();
  }, 1000);

  function abc() {
    if (currentUrl.indexOf('producttemplate/edittemplate') != -1 && document.getElementById('productGoods') === null) {
      modulecommunityhomevisual = {
        id: HiShop.DIY.getTimestamp(), //模块ID
        type: 98, //模块类型
        draggable: true, //是否可拖动
        sort: 0, //排序
        noEdit: true,
        content: '' //模块内容
      };
      HiShop.DIY.add(modulecommunityhomevisual, false);
    }
  }



  //添加一个模块
  $('.j-diy-addModule').click(function() {
    var type = $(this).data('type'); //获取模块类型
    var strUrl = window.location.toLocaleString().toLocaleLowerCase();
    var clientName = 'wapshop';
    if (strUrl.replace('/vshop/', '').length < strUrl.length) {
      clientName = 'vshop';
    } else if (strUrl.replace('/wapshop/', '').length < strUrl.length) {
      clientName = 'wapshop';
    } else if (strUrl.replace('/alioh/', '').length < strUrl.length) {
      clientName = 'alioh';
    } else if (strUrl.replace('/wechatapplet/', '').length < strUrl.length) {
      clientName = 'xcxshop';
    } else if (strUrl.replace('appshop', '').length < strUrl.length) {
      clientName = 'appshop';
    }
    //默认数据
    var moduleData = {
      id: HiShop.DIY.getTimestamp(), //模块ID
      type: type, //模块类型
      draggable: true, //是否可拖动
      sort: 0, //排序
      content: null //模块内容
    };
    //根据模块类型设置默认值
    switch (type) {
      // 图片广告
      case 1:
        moduleData.content = {
          images: [],
          showType: 1,
          lineCount: 4,
          imgStyle: 0,
          imgRadius: 0,
          pageMargin: 0,
          imgMargin: 0,
          fill: 1,
          indicatorStyle: 1,
          heightType: 0,
          height: 200,
          bgColor: '',
          verticalMargin: 0,
          moduleRadius: 0
        }
        moduleData.temp = {
          width: 0
        }
        break;

        //图文导航
      case 2:
        moduleData.content = {
          showType: 1,
          scroll: 0,
          lineCount: 4,
          bgColor: '#FFFFFF',
          textColor: '#000000',
          images: [],
          verticalMargin: 0,
          pageMargin: 0,
          imgMargin: 0,
          moduleRadius: 0,
          heightType: 0,
          height: 100
        }
        moduleData.temp = {
          width: 0,
          height: 0
        }
        break;

        //富文本
      case 3:
        moduleData.ue = null;
        moduleData.content = {
          fulltext: '',
          bgColor: '#fff',
          fullScreen: false
        };
        break;

        //魔方
      case 4:
        moduleData.content = {
          showType: 1,
          cellSelected: [],
          rowColumn: 4,
          images: [{
            imgSrc: '',
            title: '',
            link: { name: '' },
            width: 160,
            height: 160,
            top: 0,
            left: 0
          }, {
            imgSrc: '',
            title: '',
            link: { name: '' },
            width: 160,
            height: 160,
            top: 0,
            left: 0
          }],
          bgColor: '',
          pageMargin: 0,
          verticalMargin: 0,
          imgMargin: 0,
        };
        moduleData.temp = {
          targetIndex: -1,
          imgRatio: 1,
          cellStart: '',
          cellSelecting: [],
          models: [{
            value: 1,
            name: '一行二个',
            len: 2,
            row: 2,
            column: 1,
            width: [160, 160],
            height: [160, 160],
            top: [0, 0, 0],
            left: [0, 160]
          },
          {
            value: 2,
            name: '一行三个',
            len: 3,
            row: 3,
            column: 1,
            width: [106.666666, 106.666666, 106.666666],
            height: [106.666666, 106.666666, 106.666666],
            top: [0, 0, 0],
            left: [0, 106.666666, 213.33333]
          },
          {
            value: 3,
            name: '一行四个',
            len: 4,
            row: 4,
            column: 1,
            width: [80, 80, 80, 80],
            height: [80, 80, 80, 80],
            top: [0, 0, 0, 0],
            left: [0, 80, 160, 240]
          },
          {
            value: 4,
            name: '二左二右',
            len: 4,
            row: 4,
            column: 4,
            width: [160, 160, 160, 160],
            height: [160, 160, 160, 160],
            top: [0, 0, 160, 160],
            left: [0, 160, 0, 160]
          },
          {
            value: 5,
            name: '一左二右',
            len: 3,
            row: 4,
            column: 4,
            width: [160, 160, 160],
            height: [320, 160, 160],
            top: [0, 0, 160],
            left: [0, 160, 160]
          },
          {
            value: 6,
            name: '一上二下',
            len: 3,
            row: 4,
            column: 4,
            width: [320, 160, 160],
            height: [160, 160, 160],
            top: [0, 160, 160],
            left: [0, 0, 160]
          },
          {
            value: 7,
            name: '一左三右',
            len: 4,
            row: 4,
            column: 4,
            width: [160, 160, 80, 80],
            height: [320, 160, 160, 160],
            top: [0, 0, 160, 160],
            left: [0, 160, 160, 240]
          },
          {
            value: 8,
            name: '自定义',
            len: 0
          }]
        }
        break;

        //文字链接
      case 5:
        moduleData.content = {
          dataset: [
          {
            linkType: 0,
            link: '',
            pc_link: '',
            smallprog_link: '',
            title: '',
            showtitle: ''
          }]
        }
        break;

        //分割线
      case 6:
        moduleData.content = {
          color: '#fb1438',
          margin: '0',
          style: 'solid',
          thickness: '3'
        }
        break;

        //辅助空白
      case 7:
        moduleData.content = {
          height: 10,
          bgColor: '#f7f8f9'
        }
        break;

        //商品
      case 8:
        const url = window.location.href
        const path = url.split('//')[0]+'//'+url.split('//')[1].split('/')[0]+'/'
        moduleData.content = {
          goodslist: [],
          imgHref:path,
		      productType:0,
          showType: 1,
          bgColor: '',
          moduleRadius:0,
          pageMargin:4,
          verticalMargin:0,
          productMargin:8,
          listStyle:1,
          radius:0,
          imgRatio:1,
          imgFill:1,
          fontStyle:0,
          textAlign: 0,
          nameLine: 1,
          nameIcon: '',
          showName:true,
          showDesc:false,
          showPrice:true,
          showBtn:true,
          showTag:false,
          showNameIcon: false,
          btnStyle: 1,
          btnText: '购买',
          btnImg: '',
          tagStyle: 1,
          nameColor:'#666666',
          priceColor:'#fb1438',
          tagImg: '',
        }
        break;

      //搜索
		case 9:
		  moduleData.content = {
			height: 28,
			text:'',
			verticalMargin:12,
			moduleRadius:0,
			moduleText:0,
			frameColor:'#F3F3F3',
			textColor: '#BDBDBD',
			bgColor: '#ffffff'
		  }
		  break;
	//优惠券
		case 10:
		  moduleData.content = {
			coupons: [],
			couponIds: [],
			count: 5,
			bgColor: '',
			showType: 1,
			couponType: 0,
			moduleRadius: 0,
			verticalMargin: 0
		  }
		  break;
	  //直播间
		case 11:
		  moduleData.content = {
			layout: 1,
			goodslist: []
		  }
		  break;
	//限时折扣
	case 14:
	    moduleData.content = {
	        goodslist: [],
	        showType: 2,
	        pageMargin: 4,
	        productMargin: 8,
	        verticalMargin: 0,
	        listStyle: 1,
	        radius: 0,
	        imgRatio: 1,
	        imgFill: 1,
	        fontStyle: 0,
	        textAlign: 0,
	        nameLine: 1,
	        nameIcon: '',
	        showNameIcon: false,
	        showName: true,
	        showProcess: true,
	        showOldPrice: true,
	        showStock: false,
	        showTime: true,
	        nameColor: '',
	        descColor: '',
	        showPrice: true,
	        priceColor: '#fb1438',
	        showBtn: true,
	        btnStyle: 1,
	        btnText: '购买',
	        btnImg: '',
	        bgColor: '',
	        moduleRadius: 0
	    }
	break;
	//拼团
	case 15:
	    moduleData.content = {
	        goodslist: [],
	        showType: 2,
	        pageMargin: 4,
	        productMargin: 8,
	        verticalMargin: 0,
	        listStyle: 1,
	        radius: 0,
	        imgRatio: 1,
	        imgFill: 1,
	        fontStyle: 0,
	        textAlign: 0,
	        nameLine: 1,
	        nameIcon: '',
	        showNameIcon: false,
	        showName: true,
	        showOldPrice: true,
	        showTime: true,
	        nameColor: '',
	        descColor: '',
	        showPrice: true,
	        priceColor: '#fb1438',
	        showBtn: true,
	        btnStyle: 1,
	        btnText: '购买',
	        btnImg: '',
	        bgColor: '',
	        moduleRadius: 0
	    }
	    break;
    }

    //添加模块
    HiShop.DIY.add(moduleData, true);
  });

  //初始化布局拖动事件
  $('#diy-phone .drag').sortable({
    revert: true,
    placeholder: 'drag-highlight',
    stop: function(event, ui) {
      HiShop.DIY.repositionCtrl(ui.item, $('.diy-ctrl-item[data-origin="item"]')); //重置ctrl的位置
    }
  }).disableSelection();

  //编辑页面标题
  $('.j-pagetitle').click(function() {
    $('.diy-ctrl-item[data-origin="pagetitle"]').show().siblings('.diy-ctrl-item[data-origin="item"]').hide();
  });

  //编辑页面标题同步到手机视图中
  $('.j-pagetitle-ipt').change(function() {
    $('.j-pagetitle').text($(this).val());
  });
  //商品详情编辑页，且没有保存任何可视化数据的情况下才会默认加载编辑器
  if ($('#isproductdetail').val() == 1 && $('#isloaddetail').val() == 0) {
    $('.j-diy-addModule').eq(0).click();
  }
});