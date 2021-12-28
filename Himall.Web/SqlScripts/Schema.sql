/*
Navicat MySQL Data Transfer

Source Server         : 192.168.11.106_3306
Source Server Version : 50709
Source Host           : 192.168.11.106:3306
Source Database       : himall

Target Server Type    : MYSQL
Target Server Version : 50709
File Encoding         : 65001

Date: 2021-04-22 18:28:48
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for Himall_Account
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Account`;
CREATE TABLE `Himall_Account` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `AccountDate` datetime NOT NULL COMMENT '出账日期',
  `StartDate` datetime NOT NULL COMMENT '开始时间',
  `EndDate` datetime NOT NULL COMMENT '结束时间',
  `Status` int(11) NOT NULL COMMENT '枚举 0未结账，1已结账',
  `ProductActualPaidAmount` decimal(18,2) NOT NULL COMMENT '商品实付总额',
  `FreightAmount` decimal(18,2) NOT NULL COMMENT '运费',
  `CommissionAmount` decimal(18,2) NOT NULL COMMENT '佣金',
  `RefundCommissionAmount` decimal(18,2) NOT NULL COMMENT '退还佣金',
  `RefundAmount` decimal(18,2) NOT NULL COMMENT '退款金额',
  `AdvancePaymentAmount` decimal(18,2) NOT NULL COMMENT '预付款总额',
  `PeriodSettlement` decimal(18,2) NOT NULL COMMENT '本期应结',
  `Remark` text,
  `Brokerage` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '分销佣金',
  `ReturnBrokerage` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '退还分销佣金',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_AccountDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_AccountDetail`;
CREATE TABLE `Himall_AccountDetail` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AccountId` bigint(20) NOT NULL COMMENT '结算记录外键',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Date` datetime NOT NULL COMMENT '完成日期',
  `OrderDate` datetime NOT NULL COMMENT '订单下单日期',
  `OrderFinshDate` datetime NOT NULL,
  `OrderType` int(11) NOT NULL COMMENT '枚举 完成订单1，退订单0',
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `OrderAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '订单金额',
  `ProductActualPaidAmount` decimal(18,2) NOT NULL COMMENT '商品实付总额',
  `FreightAmount` decimal(18,2) NOT NULL COMMENT '运费金额',
  `TaxAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '税费',
  `IntegralDiscount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '积分抵扣金额',
  `CommissionAmount` decimal(18,2) NOT NULL COMMENT '佣金',
  `RefundTotalAmount` decimal(18,2) NOT NULL COMMENT '退款金额',
  `RefundCommisAmount` decimal(18,2) NOT NULL COMMENT '退还佣金',
  `OrderRefundsDates` varchar(300) NOT NULL COMMENT '退单的日期集合以;分隔',
  `BrokerageAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '分销佣金',
  `ReturnBrokerageAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '退分销佣金',
  `SettlementAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '结算金额',
  `PaymentTypeName` varchar(100) DEFAULT NULL COMMENT '支付类型名称',
  `DiscountAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台优惠券抵扣金额',
  `DiscountAmountReturn` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台优惠券退还金额',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `himall_accountdetails_ibfk_1` (`AccountId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_AccountMeta
-- ----------------------------
DROP TABLE IF EXISTS `Himall_AccountMeta`;
CREATE TABLE `Himall_AccountMeta` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AccountId` bigint(20) NOT NULL,
  `MetaKey` varchar(100) NOT NULL,
  `MetaValue` text NOT NULL,
  `ServiceStartTime` datetime NOT NULL COMMENT '营销服务开始时间',
  `ServiceEndTime` datetime NOT NULL COMMENT '营销服务结束时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Active
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Active`;
CREATE TABLE `Himall_Active` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺编号',
  `ActiveName` varchar(200) NOT NULL COMMENT '活动名称',
  `StartTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime NOT NULL COMMENT '结束时间',
  `IsAllProduct` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否全部商品',
  `IsAllStore` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否全部门店',
  `ActiveType` int(11) NOT NULL COMMENT '活动类型',
  `ActiveStatus` int(11) NOT NULL DEFAULT '0' COMMENT '活动状态',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IDX_Himall_Active_StartTime` (`StartTime`) USING BTREE,
  KEY `IDX_Himall_Active_EndTime` (`EndTime`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='营销活动表';

-- ----------------------------
-- Table structure for Himall_ActiveMarketService
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ActiveMarketService`;
CREATE TABLE `Himall_ActiveMarketService` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TypeId` int(11) NOT NULL COMMENT '营销服务类型ID',
  `ShopId` bigint(20) NOT NULL,
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ActiveProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ActiveProduct`;
CREATE TABLE `Himall_ActiveProduct` (
  `Id` int(11) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ActiveId` bigint(20) NOT NULL COMMENT '活动编号',
  `ProductId` bigint(20) NOT NULL COMMENT '产品编号 -1表示所有商品',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IDX_Himall_Accts_ActiveId` (`ActiveId`) USING BTREE,
  KEY `IDX_Himall_Accts_ProdcutId` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='营销活动商品';

-- ----------------------------
-- Table structure for Himall_Advance
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Advance`;
CREATE TABLE `Himall_Advance` (
  `ID` int(11) NOT NULL AUTO_INCREMENT COMMENT '首页广告设置',
  `IsEnable` tinyint(1) NOT NULL COMMENT '是否开启弹窗广告',
  `Img` varchar(100) NOT NULL COMMENT '广告位图片',
  `Link` varchar(100) NOT NULL COMMENT '图片外联链接',
  `StartTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime NOT NULL COMMENT '结束时间',
  `IsReplay` tinyint(1) NOT NULL COMMENT '是否重复播放',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Agreement
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Agreement`;
CREATE TABLE `Himall_Agreement` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AgreementType` int(4) NOT NULL COMMENT '协议类型 枚举 AgreementType：0买家注册协议，1卖家入驻协议',
  `AgreementContent` text NOT NULL COMMENT '协议内容',
  `LastUpdateTime` datetime NOT NULL COMMENT '最后修改日期',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Anchor
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Anchor`;
CREATE TABLE `Himall_Anchor` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(10) NOT NULL DEFAULT '0' COMMENT '绑定的会员ID',
  `WeChat` varchar(50) NOT NULL COMMENT '微信号',
  `AnchorName` varchar(20) CHARACTER SET utf8 NOT NULL COMMENT '主播名称',
  `CellPhone` varchar(20) DEFAULT NULL COMMENT '电话号码',
  `ShopId` bigint(10) NOT NULL DEFAULT '0' COMMENT '店铺ID',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='主播表';

-- ----------------------------
-- Table structure for Himall_AppBaseSafeSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_AppBaseSafeSetting`;
CREATE TABLE `Himall_AppBaseSafeSetting` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AppKey` varchar(50) NOT NULL,
  `AppSecret` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='app数据基础安全设置';

-- ----------------------------
-- Table structure for Himall_ApplyWithDraw
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ApplyWithDraw`;
CREATE TABLE `Himall_ApplyWithDraw` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MemId` bigint(20) NOT NULL COMMENT '会员ID',
  `NickName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `OpenId` varchar(50) DEFAULT NULL COMMENT 'OpenId',
  `ApplyStatus` int(11) NOT NULL COMMENT '申请状态',
  `ApplyAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '提现金额',
  `ApplyTime` datetime NOT NULL COMMENT '申请时间',
  `ConfirmTime` datetime DEFAULT NULL COMMENT '处理时间',
  `PayTime` datetime DEFAULT NULL COMMENT '付款时间',
  `PayNo` varchar(50) DEFAULT NULL COMMENT '付款流水号',
  `OpUser` varchar(50) DEFAULT NULL COMMENT '操作人',
  `Remark` varchar(200) DEFAULT NULL COMMENT '备注',
  `ApplyType` int(11) NOT NULL DEFAULT '1' COMMENT '提现方式',
  `Poundage` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '手续费',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_AppMessage
-- ----------------------------
DROP TABLE IF EXISTS `Himall_AppMessage`;
CREATE TABLE `Himall_AppMessage` (
  `Id` int(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '商家ID',
  `ShopBranchId` bigint(20) NOT NULL DEFAULT '0' COMMENT '门店ID',
  `TypeId` int(20) NOT NULL COMMENT '消息类型，对应枚举(1=订单，2=售后)',
  `SourceId` bigint(20) NOT NULL COMMENT '数据来源编号，对应订单ID或者售后ID',
  `Content` varchar(200) NOT NULL COMMENT '消息内容',
  `IsRead` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否已读',
  `sendtime` datetime NOT NULL,
  `Title` varchar(50) NOT NULL,
  `OrderPayDate` datetime NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='APP消息通知表';

-- ----------------------------
-- Table structure for Himall_Article
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Article`;
CREATE TABLE `Himall_Article` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `CategoryId` bigint(20) NOT NULL DEFAULT '0' COMMENT '文章分类ID',
  `Title` varchar(100) NOT NULL COMMENT '文章标题',
  `IconUrl` varchar(100) DEFAULT NULL,
  `Content` mediumtext NOT NULL COMMENT '文档内容',
  `AddDate` datetime NOT NULL,
  `DisplaySequence` bigint(20) NOT NULL,
  `Meta_Title` text COMMENT 'SEO标题',
  `Meta_Description` text COMMENT 'SEO说明',
  `Meta_Keywords` text COMMENT 'SEO关键字',
  `IsRelease` tinyint(1) NOT NULL COMMENT '是否显示',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_ArticleCategory_Article` (`CategoryId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ArticleCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ArticleCategory`;
CREATE TABLE `Himall_ArticleCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ParentCategoryId` bigint(20) NOT NULL,
  `Name` varchar(100) DEFAULT NULL COMMENT '文章类型名称',
  `DisplaySequence` bigint(20) NOT NULL COMMENT '显示顺序',
  `IsDefault` tinyint(1) NOT NULL COMMENT '是否为默认',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Attribute
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Attribute`;
CREATE TABLE `Himall_Attribute` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TypeId` bigint(20) NOT NULL,
  `Name` varchar(100) NOT NULL COMMENT '名称',
  `DisplaySequence` bigint(20) NOT NULL,
  `IsMust` tinyint(1) NOT NULL COMMENT '是否为必选',
  `IsMulti` tinyint(1) NOT NULL COMMENT '是否可多选',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Type_Attribute` (`TypeId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_AttributeValue
-- ----------------------------
DROP TABLE IF EXISTS `Himall_AttributeValue`;
CREATE TABLE `Himall_AttributeValue` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AttributeId` bigint(20) NOT NULL COMMENT '属性ID',
  `Value` varchar(100) NOT NULL COMMENT '属性值',
  `DisplaySequence` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Attribute_AttributeValue` (`AttributeId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_AutoReply
-- ----------------------------
DROP TABLE IF EXISTS `Himall_AutoReply`;
CREATE TABLE `Himall_AutoReply` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RuleName` varchar(50) DEFAULT NULL COMMENT '规则名称',
  `Keyword` varchar(30) DEFAULT NULL COMMENT '关键词',
  `MatchType` int(11) NOT NULL COMMENT '匹配方式(模糊，完全匹配)',
  `TextReply` varchar(300) DEFAULT NULL COMMENT '文字回复内容',
  `IsOpen` int(11) NOT NULL DEFAULT '0' COMMENT '是否开启',
  `ReplyType` int(11) NOT NULL COMMENT '消息回复类型-(关注回复，关键词回复，消息自动回复)',
  `ReplyContentType` int(11) NOT NULL DEFAULT '1' COMMENT '消息内容的类型，1=文本，2=图文素材',
  `MediaId` varchar(200) DEFAULT NULL COMMENT '素材ID',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Banner
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Banner`;
CREATE TABLE `Himall_Banner` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `Name` varchar(100) NOT NULL COMMENT '导航名称',
  `Position` int(11) NOT NULL COMMENT '导航显示位置',
  `DisplaySequence` bigint(20) NOT NULL,
  `Url` varchar(1000) NOT NULL COMMENT '跳转URL',
  `Platform` int(11) NOT NULL DEFAULT '0' COMMENT '显示在哪个终端',
  `UrlType` int(11) NOT NULL DEFAULT '0',
  `STATUS` int(11) NOT NULL DEFAULT '1' COMMENT '开启或者关闭',
  `EnableDelete` int(11) NOT NULL DEFAULT '1' COMMENT '能否删除',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Bonus
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Bonus`;
CREATE TABLE `Himall_Bonus` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Type` int(11) NOT NULL COMMENT '类型，活动红包，关注送红包',
  `Style` int(11) NOT NULL COMMENT '样式，模板一（源生风格），模板二',
  `Name` varchar(100) DEFAULT NULL COMMENT '名称',
  `MerchantsName` varchar(50) DEFAULT NULL COMMENT '商户名称',
  `Remark` varchar(200) DEFAULT NULL COMMENT '备注',
  `Blessing` varchar(100) DEFAULT NULL COMMENT '祝福语',
  `TotalPrice` decimal(18,2) NOT NULL COMMENT '总面额',
  `StartTime` datetime NOT NULL COMMENT '开始日期',
  `EndTime` datetime NOT NULL COMMENT '结束日期',
  `QRPath` varchar(100) DEFAULT NULL COMMENT '二维码',
  `PriceType` int(11) NOT NULL COMMENT '是否固定金额',
  `FixedAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '固定金额',
  `RandomAmountStart` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '随机金额起止范围',
  `RandomAmountEnd` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '随机金额起止范围',
  `ReceiveCount` int(11) NOT NULL,
  `ImagePath` varchar(100) DEFAULT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `IsInvalid` tinyint(1) NOT NULL,
  `ReceivePrice` decimal(18,2) NOT NULL,
  `ReceiveHref` varchar(200) NOT NULL,
  `IsAttention` tinyint(1) NOT NULL,
  `IsGuideShare` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_BonusReceive
-- ----------------------------
DROP TABLE IF EXISTS `Himall_BonusReceive`;
CREATE TABLE `Himall_BonusReceive` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `BonusId` bigint(20) NOT NULL COMMENT '红包Id',
  `OpenId` varchar(100) DEFAULT NULL COMMENT '领取人微信Id',
  `ReceiveTime` datetime DEFAULT NULL COMMENT '领取日期',
  `Price` decimal(18,2) NOT NULL COMMENT '领取金额',
  `IsShare` tinyint(1) NOT NULL,
  `IsTransformedDeposit` tinyint(1) NOT NULL COMMENT '红包金额是否已经转入了预存款',
  `UserId` bigint(20) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Reference_1` (`BonusId`) USING BTREE,
  KEY `FK_UserId` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Brand
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Brand`;
CREATE TABLE `Himall_Brand` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL COMMENT '品牌名称',
  `DisplaySequence` bigint(20) NOT NULL COMMENT '顺序',
  `Logo` varchar(1000) DEFAULT NULL COMMENT 'LOGO',
  `RewriteName` varchar(50) DEFAULT NULL COMMENT '未使用',
  `Description` varchar(1000) DEFAULT NULL COMMENT '说明',
  `Meta_Title` varchar(1000) DEFAULT NULL COMMENT 'SEO',
  `Meta_Description` varchar(1000) DEFAULT NULL,
  `Meta_Keywords` varchar(1000) DEFAULT NULL,
  `IsRecommend` tinyint(1) NOT NULL,
  `IsDeleted` bit(1) NOT NULL COMMENT '是否已删除',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Id` (`Id`),
  KEY `Id_2` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_BrowsingHistory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_BrowsingHistory`;
CREATE TABLE `Himall_BrowsingHistory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MemberId` bigint(20) NOT NULL COMMENT '会员ID',
  `ProductId` bigint(20) NOT NULL,
  `BrowseTime` datetime NOT NULL COMMENT '浏览时间',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_BrowsingHistory_Himall_BrowsingHistory` (`MemberId`) USING BTREE,
  KEY `FK_Himall_BrowsingHistory_Himall_Products` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_BusinessCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_BusinessCategory`;
CREATE TABLE `Himall_BusinessCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `CategoryId` bigint(20) NOT NULL COMMENT '分类ID',
  `CommisRate` decimal(8,2) NOT NULL COMMENT '分佣比例',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Category_BusinessCategory` (`CategoryId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_BusinessCategoryApply
-- ----------------------------
DROP TABLE IF EXISTS `Himall_BusinessCategoryApply`;
CREATE TABLE `Himall_BusinessCategoryApply` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `ApplyDate` datetime NOT NULL COMMENT '申请日期',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `AuditedStatus` int(11) NOT NULL COMMENT '审核状态',
  `AuditedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_BusinessCategoryApplyDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_BusinessCategoryApplyDetail`;
CREATE TABLE `Himall_BusinessCategoryApplyDetail` (
  `Id` bigint(11) NOT NULL AUTO_INCREMENT,
  `CommisRate` decimal(8,2) NOT NULL COMMENT '分佣比例',
  `CategoryId` bigint(20) NOT NULL COMMENT '类目ID',
  `ApplyId` bigint(20) NOT NULL COMMENT '申请Id',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FR_BussinessCateApply` (`ApplyId`) USING BTREE,
  KEY `FR_BussinessCateApply_Cid` (`CategoryId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Capital
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Capital`;
CREATE TABLE `Himall_Capital` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MemId` bigint(20) NOT NULL COMMENT '会员ID',
  `Balance` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '可用余额',
  `FreezeAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '冻结资金',
  `ChargeAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '累计充值总金额',
  `PresentAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '累积充值赠送',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CapitalDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CapitalDetail`;
CREATE TABLE `Himall_CapitalDetail` (
  `Id` bigint(20) NOT NULL,
  `CapitalID` bigint(20) NOT NULL COMMENT '资产主表ID',
  `SourceType` int(11) NOT NULL COMMENT '资产类型',
  `Amount` decimal(18,2) NOT NULL COMMENT '金额',
  `SourceData` varchar(100) DEFAULT NULL COMMENT '来源数据',
  `CreateTime` datetime NOT NULL COMMENT '交易时间',
  `Remark` varchar(255) DEFAULT NULL COMMENT '备注',
  `PresentAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '赠送',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Reference_Himall_CapitalDetail` (`CapitalID`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CashDeposit
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CashDeposit`;
CREATE TABLE `Himall_CashDeposit` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `ShopId` bigint(20) NOT NULL COMMENT 'Shop表外键',
  `CurrentBalance` decimal(10,2) NOT NULL DEFAULT '0.00' COMMENT '可用金额',
  `TotalBalance` decimal(10,2) NOT NULL DEFAULT '0.00' COMMENT '已缴纳金额',
  `Date` datetime NOT NULL COMMENT '最后一次缴纳时间',
  `EnableLabels` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否显示标志，只有保证金欠费该字段才有用，默认显示',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_CashDeposit_Himall_Shops` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CashDepositDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CashDepositDetail`;
CREATE TABLE `Himall_CashDepositDetail` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `CashDepositId` bigint(20) NOT NULL DEFAULT '0',
  `AddDate` datetime NOT NULL,
  `Balance` decimal(10,2) NOT NULL DEFAULT '0.00',
  `Operator` varchar(50) NOT NULL COMMENT '操作类型',
  `Description` varchar(1000) DEFAULT NULL COMMENT '说明',
  `RechargeWay` int(11) DEFAULT NULL COMMENT '充值类型（银联、支付宝之类的）',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `KF_Himall_CashDeposit_Himall_CashDepositDetail` (`CashDepositId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Category
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Category`;
CREATE TABLE `Himall_Category` (
  `Id` bigint(20) NOT NULL,
  `Name` varchar(100) NOT NULL COMMENT '分类名称',
  `Icon` varchar(1000) DEFAULT NULL COMMENT '分类图标',
  `DisplaySequence` bigint(20) NOT NULL,
  `SupportVirtualProduct` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否支持虚拟商品(0=否，1=是)',
  `ParentCategoryId` bigint(20) NOT NULL,
  `Depth` int(11) NOT NULL COMMENT '分类的深度',
  `Path` varchar(100) NOT NULL COMMENT '分类的路径（以|分离）',
  `RewriteName` varchar(50) DEFAULT NULL COMMENT '未使用',
  `HasChildren` tinyint(1) NOT NULL COMMENT '是否有子分类',
  `TypeId` bigint(20) NOT NULL DEFAULT '0',
  `CommisRate` decimal(8,2) NOT NULL COMMENT '分佣比例',
  `Meta_Title` varchar(1000) DEFAULT NULL,
  `Meta_Description` varchar(1000) DEFAULT NULL,
  `Meta_Keywords` varchar(1000) DEFAULT NULL,
  `IsDeleted` bit(1) NOT NULL COMMENT '是否已删除',
  `IsShow` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否显示',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Type_Category` (`TypeId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CategoryCashDeposit
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CategoryCashDeposit`;
CREATE TABLE `Himall_CategoryCashDeposit` (
  `Id` bigint(20) NOT NULL COMMENT '主键Id',
  `CategoryId` bigint(20) NOT NULL COMMENT '分类Id',
  `NeedPayCashDeposit` decimal(10,2) NOT NULL DEFAULT '0.00' COMMENT '需要缴纳保证金',
  `EnableNoReasonReturn` tinyint(1) NOT NULL DEFAULT '1' COMMENT '允许七天无理由退货',
  PRIMARY KEY (`CategoryId`) USING BTREE,
  KEY `FK_Himall_CategoriesObligation_Categories` (`CategoryId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ChargeDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ChargeDetail`;
CREATE TABLE `Himall_ChargeDetail` (
  `Id` bigint(20) NOT NULL,
  `MemId` bigint(20) NOT NULL COMMENT '会员ID',
  `ChargeTime` datetime DEFAULT NULL COMMENT '充值时间',
  `ChargeAmount` decimal(18,2) NOT NULL COMMENT '充值金额',
  `ChargeWay` varchar(50) DEFAULT NULL COMMENT '充值方式',
  `ChargeStatus` int(11) NOT NULL COMMENT '充值状态',
  `CreateTime` datetime NOT NULL COMMENT '提交充值时间',
  `PresentAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '赠送',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ChargeDetailShop
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ChargeDetailShop`;
CREATE TABLE `Himall_ChargeDetailShop` (
  `Id` bigint(20) NOT NULL,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ChargeTime` datetime DEFAULT NULL COMMENT '充值时间',
  `ChargeAmount` decimal(18,2) NOT NULL COMMENT '充值金额',
  `ChargeWay` varchar(50) DEFAULT NULL COMMENT '充值方式',
  `ChargeStatus` int(11) NOT NULL COMMENT '充值状态',
  `CreateTime` datetime NOT NULL COMMENT '提交充值时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CityExpressConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CityExpressConfig`;
CREATE TABLE `Himall_CityExpressConfig` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '商家编号',
  `IsEnable` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否开启',
  `source_id` varchar(200) DEFAULT NULL COMMENT '商户号',
  `app_key` varchar(200) DEFAULT NULL COMMENT 'appKey',
  `app_secret` varchar(200) DEFAULT NULL COMMENT 'appSecret',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Collocation
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Collocation`;
CREATE TABLE `Himall_Collocation` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'ID自增',
  `Title` varchar(100) NOT NULL COMMENT '组合购标题',
  `StartTime` datetime NOT NULL COMMENT '开始日期',
  `EndTime` datetime NOT NULL COMMENT '结束日期',
  `ShortDesc` varchar(1000) DEFAULT NULL COMMENT '组合描述',
  `ShopId` bigint(20) NOT NULL COMMENT '组合购店铺ID',
  `CreateTime` datetime DEFAULT NULL COMMENT '添加时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CollocationPoruduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CollocationPoruduct`;
CREATE TABLE `Himall_CollocationPoruduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'ID自增',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `ColloId` bigint(20) NOT NULL COMMENT '组合购ID',
  `IsMain` tinyint(1) NOT NULL COMMENT '是否主商品',
  `DisplaySequence` int(11) NOT NULL COMMENT '排序',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Collocation_CollPoruducts` (`ColloId`) USING BTREE,
  KEY `FK_Product_CollPoruducts` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CollocationSku
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CollocationSku`;
CREATE TABLE `Himall_CollocationSku` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'ID自增',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `SkuID` varchar(100) NOT NULL COMMENT '商品SkuId',
  `ColloProductId` bigint(20) NOT NULL COMMENT '组合商品表ID',
  `Price` decimal(18,2) NOT NULL COMMENT '组合购价格',
  `SkuPirce` decimal(18,2) NOT NULL COMMENT '原始价格',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_CollSkus` (`ProductId`) USING BTREE,
  KEY `FK_ColloPoruducts_CollSkus` (`ColloProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Coupon
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Coupon`;
CREATE TABLE `Himall_Coupon` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '店铺名称',
  `Price` decimal(18,0) NOT NULL COMMENT '价格',
  `PerMax` int(11) NOT NULL COMMENT '最大可领取张数',
  `OrderAmount` decimal(18,0) NOT NULL COMMENT '订单金额（满足多少钱才能使用）',
  `Num` int(11) NOT NULL COMMENT '发行张数',
  `StartTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime NOT NULL,
  `CouponName` varchar(100) NOT NULL COMMENT '优惠券名称',
  `CreateTime` datetime NOT NULL,
  `ReceiveType` int(11) NOT NULL DEFAULT '0' COMMENT '领取方式 0 店铺首页 1 积分兑换 2 主动发放',
  `NeedIntegral` int(11) NOT NULL COMMENT '所需积分',
  `EndIntegralExchange` datetime NOT NULL COMMENT '兑换截止时间',
  `IntegralCover` varchar(200) DEFAULT NULL COMMENT '积分商城封面',
  `IsSyncWeiXin` int(11) NOT NULL DEFAULT '0' COMMENT '是否同步到微信',
  `WXAuditStatus` int(11) NOT NULL DEFAULT '0' COMMENT '微信状态',
  `CardLogId` bigint(20) DEFAULT NULL COMMENT '微信卡券记录号 与微信卡券记录关联',
  `UseArea` int(1) NOT NULL DEFAULT '0' COMMENT '使用范围：0=全场通用，1=部分商品可用',
  `Remark` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_Coupon_Himall_Shops` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CouponProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CouponProduct`;
CREATE TABLE `Himall_CouponProduct` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `CouponId` bigint(20) NOT NULL,
  `ProductId` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `CouponId` (`CouponId`) USING BTREE,
  KEY `ProductId` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CouponRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CouponRecord`;
CREATE TABLE `Himall_CouponRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `CouponId` bigint(20) NOT NULL,
  `CounponSN` varchar(50) NOT NULL COMMENT '优惠券的SN标示',
  `CounponTime` datetime NOT NULL,
  `UserName` varchar(100) NOT NULL COMMENT '用户名称',
  `UserId` bigint(20) NOT NULL,
  `UsedTime` datetime DEFAULT NULL,
  `OrderId` varchar(500) DEFAULT NULL COMMENT '使用的订单ID',
  `ShopId` bigint(20) NOT NULL,
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `CounponStatus` int(11) NOT NULL COMMENT '优惠券状态',
  `WXCodeId` bigint(20) DEFAULT NULL COMMENT '微信Code记录号 与微信卡券投放记录关联',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `fk_couponrecord_couponid` (`CouponId`) USING BTREE,
  KEY `FK_couponrecord_shopid` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CouponSendByRegister
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CouponSendByRegister`;
CREATE TABLE `Himall_CouponSendByRegister` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `Status` int(11) NOT NULL COMMENT '0、关闭；1、开启',
  `Link` varchar(300) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='注册赠送优惠券';

-- ----------------------------
-- Table structure for Himall_CouponSendByRegisterDetailed
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CouponSendByRegisterDetailed`;
CREATE TABLE `Himall_CouponSendByRegisterDetailed` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `CouponRegisterId` bigint(20) NOT NULL COMMENT '注册活动ID',
  `CouponId` bigint(20) NOT NULL COMMENT '优惠券ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Reference_z` (`CouponRegisterId`) USING BTREE,
  KEY `FK_Reference_coupon` (`CouponId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='注册送优惠券关联优惠券';

-- ----------------------------
-- Table structure for Himall_CouponSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CouponSetting`;
CREATE TABLE `Himall_CouponSetting` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PlatForm` int(11) NOT NULL COMMENT '优惠券的发行平台',
  `CouponID` bigint(20) NOT NULL,
  `Display` int(11) NOT NULL COMMENT '是否显示',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_CouponShop
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CouponShop`;
CREATE TABLE `Himall_CouponShop` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `CouponId` bigint(20) NOT NULL,
  `ShopId` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------
-- Table structure for Himall_CustomerService
-- ----------------------------
DROP TABLE IF EXISTS `Himall_CustomerService`;
CREATE TABLE `Himall_CustomerService` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `Tool` int(11) NOT NULL COMMENT '工具类型（QQ、旺旺）',
  `Type` int(11) NOT NULL,
  `Name` varchar(1000) NOT NULL COMMENT '客服名称',
  `AccountCode` varchar(1000) NOT NULL COMMENT '通信账号',
  `TerminalType` int(11) NOT NULL DEFAULT '0' COMMENT '终端类型',
  `ServerStatus` int(11) NOT NULL DEFAULT '1' COMMENT '客服状态',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_DistributionBrokerage
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributionBrokerage`;
CREATE TABLE `Himall_DistributionBrokerage` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '流水号',
  `OrderId` bigint(20) NOT NULL COMMENT '订单编号',
  `OrderItemId` bigint(20) NOT NULL COMMENT '订单项编号',
  `ProductId` bigint(20) NOT NULL COMMENT '商品编号',
  `MemberId` bigint(20) NOT NULL COMMENT '下单会员',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺编号',
  `Quantity` bigint(20) NOT NULL COMMENT '购买数量',
  `RealPayAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '实付金额',
  `BrokerageStatus` int(11) NOT NULL COMMENT '佣金状态',
  `OrderDate` datetime DEFAULT NULL COMMENT '下单时间',
  `SettlementTime` datetime DEFAULT NULL COMMENT '结算时间',
  `SuperiorId1` bigint(20) NOT NULL DEFAULT '0' COMMENT '一级分销员',
  `BrokerageRate1` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '一级分佣比',
  `SuperiorId2` bigint(20) NOT NULL DEFAULT '0' COMMENT '二级分销员',
  `BrokerageRate2` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '二级分佣比',
  `SuperiorId3` bigint(20) NOT NULL DEFAULT '0' COMMENT '三级分销员',
  `BrokerageRate3` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '三级分佣比',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='分销佣金表';

-- ----------------------------
-- Table structure for Himall_DistributionProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributionProduct`;
CREATE TABLE `Himall_DistributionProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ProductId` bigint(20) NOT NULL COMMENT '商品编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺编号',
  `ProductStatus` int(11) NOT NULL COMMENT '商品分销状态',
  `BrokerageRate1` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '一级分佣比',
  `BrokerageRate2` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '二级分佣比',
  `BrokerageRate3` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '三级分佣比',
  `SaleCount` int(11) NOT NULL DEFAULT '0' COMMENT '成交件数',
  `SaleAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '成交金额',
  `SettlementAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '已结算金额',
  `AddDate` datetime DEFAULT NULL COMMENT '添加推广时间',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='分销商品表';

-- ----------------------------
-- Table structure for Himall_DistributionRanking
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributionRanking`;
CREATE TABLE `Himall_DistributionRanking` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `BatchId` bigint(20) NOT NULL,
  `MemberId` bigint(20) NOT NULL COMMENT '销售员ID',
  `Quantity` int(11) NOT NULL COMMENT '成交数量',
  `Amount` decimal(10,2) NOT NULL COMMENT '成交金额',
  `Settlement` decimal(10,2) NOT NULL COMMENT '已结算金额',
  `NoSettlement` decimal(10,2) NOT NULL COMMENT '未结算金额',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_DistributionRankingBatch
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributionRankingBatch`;
CREATE TABLE `Himall_DistributionRankingBatch` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `BeginTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime NOT NULL COMMENT '截止时间',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_DistributionShopRateConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributionShopRateConfig`;
CREATE TABLE `Himall_DistributionShopRateConfig` (
  `Id` int(11) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺编号',
  `BrokerageRate1` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '一级分佣比',
  `BrokerageRate2` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '二级分佣比',
  `BrokerageRate3` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '三级分佣比',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='分销默认佣金比例表';

-- ----------------------------
-- Table structure for Himall_DistributionWithdraw
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributionWithdraw`;
CREATE TABLE `Himall_DistributionWithdraw` (
  `Id` bigint(20) NOT NULL COMMENT '流水号',
  `MemberId` bigint(20) NOT NULL COMMENT '会员ID',
  `WithdrawName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '提现号',
  `WithdrawAccount` varchar(100) DEFAULT NULL COMMENT '提现账号',
  `WithdrawStatus` int(11) NOT NULL COMMENT '提现状态',
  `Amount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '提现金额',
  `ApplyTime` datetime NOT NULL COMMENT '申请时间',
  `ConfirmTime` datetime DEFAULT NULL COMMENT '处理时间',
  `PayTime` datetime DEFAULT NULL COMMENT '付款时间',
  `PayNo` varchar(50) DEFAULT NULL COMMENT '付款流水号',
  `Operator` varchar(50) DEFAULT NULL COMMENT '操作人',
  `Remark` varchar(200) DEFAULT NULL COMMENT '备注',
  `WithdrawType` int(11) NOT NULL DEFAULT '0' COMMENT '提现方式',
  `Poundage` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '手续费',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='分销提现';

-- ----------------------------
-- Table structure for Himall_Distributor
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Distributor`;
CREATE TABLE `Himall_Distributor` (
  `MemberId` bigint(20) NOT NULL COMMENT '编号',
  `SuperiorId` bigint(20) NOT NULL COMMENT '上级编号',
  `GradeId` bigint(20) NOT NULL DEFAULT '0' COMMENT '所属等级',
  `OrderCount` int(11) NOT NULL DEFAULT '0' COMMENT '分销订单数',
  `ShopName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '小店名称',
  `ShopLogo` varchar(200) DEFAULT NULL COMMENT '小店图标',
  `IsShowShopLogo` bit(1) NOT NULL DEFAULT b'1' COMMENT '是否展示小店logo',
  `DistributionStatus` int(11) NOT NULL COMMENT '审核状态',
  `ApplyTime` datetime NOT NULL COMMENT '申请时间',
  `PassTime` datetime DEFAULT NULL COMMENT '通过时间',
  `Remark` varchar(300) DEFAULT NULL COMMENT '备注',
  `SubNumber` int(11) NOT NULL DEFAULT '0' COMMENT '直接下级数',
  `Balance` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '余额',
  `SettlementAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '总结算收入',
  `FreezeAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '冻结金额',
  `WithdrawalsAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '已提现',
  `ProductCount` int(11) NOT NULL DEFAULT '0' COMMENT '分销成交商品数',
  `SaleAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '分销成交金额',
  `SubProductCount` int(11) NOT NULL DEFAULT '0' COMMENT '下级分销成交商品数',
  `SubSaleAmount` decimal(20,2) NOT NULL DEFAULT '0.00' COMMENT '下级分销成交金额',
  PRIMARY KEY (`MemberId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='分销用户表';

-- ----------------------------
-- Table structure for Himall_DistributorGrade
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributorGrade`;
CREATE TABLE `Himall_DistributorGrade` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `GradeName` varchar(20) NOT NULL COMMENT '名称',
  `Quota` decimal(20,2) NOT NULL COMMENT '条件',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='分销员等级表';

-- ----------------------------
-- Table structure for Himall_DistributorRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_DistributorRecord`;
CREATE TABLE `Himall_DistributorRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MemberId` bigint(20) NOT NULL COMMENT '分销员',
  `Type` tinyint(4) NOT NULL COMMENT '流水类型',
  `Amount` decimal(10,2) NOT NULL COMMENT '变更金额',
  `Balance` decimal(10,2) NOT NULL COMMENT '变更后余额',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `Remark` varchar(255) NOT NULL COMMENT '备注',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ExpressElement
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ExpressElement`;
CREATE TABLE `Himall_ExpressElement` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `ExpressId` bigint(20) NOT NULL COMMENT '快递公司ID',
  `ElementType` int(11) NOT NULL COMMENT '元素类型',
  `LeftTopPointX` int(11) NOT NULL COMMENT '面单元素X坐标1',
  `LeftTopPointY` int(11) NOT NULL COMMENT '面单元素Y坐标1',
  `RightBottomPointX` int(11) NOT NULL COMMENT '面单元素X坐标2',
  `RightBottomPointY` int(11) NOT NULL COMMENT '面单元素Y坐标2',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ExpressInfo
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ExpressInfo`;
CREATE TABLE `Himall_ExpressInfo` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `Name` varchar(50) NOT NULL COMMENT '快递名称',
  `TaobaoCode` varchar(50) DEFAULT NULL COMMENT '淘宝编号',
  `Kuaidi100Code` varchar(50) DEFAULT NULL COMMENT '快递100对应物流编号',
  `KuaidiNiaoCode` varchar(50) DEFAULT NULL COMMENT '快递鸟物流公司编号',
  `Width` int(11) NOT NULL COMMENT '快递面单宽度',
  `Height` int(11) NOT NULL COMMENT '快递面单高度',
  `Logo` varchar(100) DEFAULT NULL COMMENT '快递公司logo',
  `BackGroundImage` varchar(100) DEFAULT NULL COMMENT '快递公司面单背景图片',
  `Status` int(11) NOT NULL COMMENT '快递公司状态（0：正常，1：删除）',
  `CreateDate` datetime NOT NULL COMMENT '创建日期',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Favorite
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Favorite`;
CREATE TABLE `Himall_Favorite` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `ProductId` bigint(20) NOT NULL,
  `Tags` varchar(100) DEFAULT NULL COMMENT '分类标签',
  `Date` datetime NOT NULL COMMENT '收藏日期',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Member_Favorite` (`UserId`) USING BTREE,
  KEY `FK_Product_Favorite` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FavoriteShop
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FavoriteShop`;
CREATE TABLE `Himall_FavoriteShop` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `ShopId` bigint(20) NOT NULL,
  `Tags` varchar(100) DEFAULT NULL COMMENT '分类标签',
  `Date` datetime NOT NULL COMMENT '收藏日期',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Himall_FavoriteShop_fk_1` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FightGroup
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FightGroup`;
CREATE TABLE `Himall_FightGroup` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `HeadUserId` bigint(20) NOT NULL COMMENT '团长用户编号',
  `ActiveId` bigint(20) NOT NULL COMMENT '对应活动',
  `LimitedNumber` int(11) NOT NULL COMMENT '参团人数限制',
  `LimitedHour` decimal(18,2) NOT NULL COMMENT '时间限制',
  `JoinedNumber` int(11) NOT NULL COMMENT '已参团人数',
  `IsException` bit(1) NOT NULL COMMENT '是否异常',
  `GroupStatus` int(11) NOT NULL COMMENT '数据状态 初始中  成团中  成功   失败',
  `AddGroupTime` datetime NOT NULL COMMENT '开团时间',
  `OverTime` datetime DEFAULT NULL COMMENT '结束时间 成功或失败的时间',
  `ProductId` bigint(20) NOT NULL DEFAULT '0' COMMENT '商品编号',
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '店铺编号',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='拼团组团详情';

-- ----------------------------
-- Table structure for Himall_FightGroupActive
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FightGroupActive`;
CREATE TABLE `Himall_FightGroupActive` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺编号',
  `ProductId` bigint(20) NOT NULL COMMENT '商品编号',
  `ProductName` varchar(100) DEFAULT NULL COMMENT '商品名称',
  `IconUrl` varchar(100) DEFAULT NULL COMMENT '图片',
  `StartTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime NOT NULL COMMENT '结束时间',
  `LimitedNumber` int(11) NOT NULL DEFAULT '0' COMMENT '参团人数限制',
  `LimitedHour` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '成团时限',
  `LimitQuantity` int(11) NOT NULL DEFAULT '0' COMMENT '数量限制',
  `GroupCount` int(11) NOT NULL DEFAULT '0' COMMENT '成团数量',
  `OkGroupCount` int(11) NOT NULL DEFAULT '0' COMMENT '成功成团数量',
  `AddTime` datetime NOT NULL COMMENT '活动添加时间',
  `ManageAuditStatus` int(11) NOT NULL DEFAULT '0' COMMENT '平台操作状态',
  `ManageRemark` varchar(1000) DEFAULT NULL COMMENT '平台操作说明',
  `ManageDate` datetime DEFAULT NULL COMMENT '平台操作时间',
  `ManagerId` bigint(20) DEFAULT NULL COMMENT '平台操作人',
  `ActiveTimeStatus` int(11) NOT NULL DEFAULT '0' COMMENT '活动当前状态',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='拼团活动';

-- ----------------------------
-- Table structure for Himall_FightGroupActiveItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FightGroupActiveItem`;
CREATE TABLE `Himall_FightGroupActiveItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ActiveId` bigint(20) NOT NULL COMMENT '所属活动',
  `ProductId` bigint(20) NOT NULL COMMENT '商品编号',
  `SkuId` varchar(100) DEFAULT NULL COMMENT '商品SKU',
  `ActivePrice` decimal(18,2) NOT NULL COMMENT '活动售价',
  `ActiveStock` int(20) NOT NULL COMMENT '活动库存',
  `BuyCount` int(11) NOT NULL DEFAULT '0' COMMENT '已售',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='拼团活动项';

-- ----------------------------
-- Table structure for Himall_FightGroupOrder
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FightGroupOrder`;
CREATE TABLE `Himall_FightGroupOrder` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ActiveId` bigint(20) NOT NULL COMMENT '对应活动',
  `ProductId` bigint(20) NOT NULL COMMENT '对应商品',
  `SkuId` varchar(100) DEFAULT NULL COMMENT '商品SKU',
  `GroupId` bigint(20) NOT NULL COMMENT '所属拼团',
  `OrderId` bigint(20) NOT NULL COMMENT '订单时间',
  `OrderUserId` bigint(20) NOT NULL COMMENT '订单用户编号',
  `IsFirstOrder` bit(1) NOT NULL COMMENT '是否团首订单',
  `JoinTime` datetime NOT NULL COMMENT '参团时间',
  `JoinStatus` int(11) NOT NULL COMMENT '参团状态 参团中  成功  失败',
  `OverTime` datetime DEFAULT NULL COMMENT '结束时间 成功或失败的时间',
  `Quantity` bigint(20) NOT NULL DEFAULT '0' COMMENT '购买数量',
  `SalePrice` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '销售价',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='拼团订单';

-- ----------------------------
-- Table structure for Himall_FlashSale
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FlashSale`;
CREATE TABLE `Himall_FlashSale` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Title` varchar(30) NOT NULL,
  `ShopId` bigint(20) NOT NULL,
  `ProductId` bigint(20) NOT NULL,
  `Status` int(11) NOT NULL COMMENT '待审核,进行中,已结束,审核未通过,管理员取消',
  `BeginDate` datetime NOT NULL COMMENT '活动开始日期',
  `EndDate` datetime NOT NULL COMMENT '活动结束日期',
  `LimitCountOfThePeople` int(11) NOT NULL COMMENT '限制每人购买的数量',
  `SaleCount` int(11) NOT NULL COMMENT '仅仅只计算在限时购里的销售数',
  `CategoryName` varchar(255) NOT NULL,
  `ImagePath` varchar(255) NOT NULL,
  `MinPrice` decimal(18,2) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_FSShopId3` (`ShopId`) USING BTREE,
  KEY `FK_FSProductId3` (`ProductId`) USING BTREE,
  KEY `IX_ProductId_Status_BeginDate_EndDate` (`ProductId`,`Status`,`BeginDate`,`EndDate`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FlashSaleConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FlashSaleConfig`;
CREATE TABLE `Himall_FlashSaleConfig` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Preheat` int(11) NOT NULL COMMENT '预热时间',
  `IsNormalPurchase` tinyint(1) NOT NULL COMMENT '是否允许正常购买',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FlashSaleDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FlashSaleDetail`;
CREATE TABLE `Himall_FlashSaleDetail` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL,
  `SkuId` varchar(100) NOT NULL,
  `Price` decimal(18,2) NOT NULL COMMENT '限时购时金额',
  `TotalCount` int(11) NOT NULL DEFAULT '0' COMMENT '活动库存',
  `FlashSaleId` bigint(20) NOT NULL COMMENT '对应FlashSale表主键',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FlashSaleRemind
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FlashSaleRemind`;
CREATE TABLE `Himall_FlashSaleRemind` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OpenId` varchar(200) NOT NULL,
  `RecordDate` datetime NOT NULL,
  `FlashSaleId` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FloorBrand
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FloorBrand`;
CREATE TABLE `Himall_FloorBrand` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FloorId` bigint(20) NOT NULL COMMENT '楼层ID',
  `BrandId` bigint(20) NOT NULL COMMENT '品牌ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Brand_FloorBrand` (`BrandId`) USING BTREE,
  KEY `FK_HomeFloor_FloorBrand` (`FloorId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FloorCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FloorCategory`;
CREATE TABLE `Himall_FloorCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FloorId` bigint(20) NOT NULL,
  `CategoryId` bigint(20) NOT NULL,
  `Depth` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Category_FloorCategory` (`CategoryId`) USING BTREE,
  KEY `FK_HomeFloor_FloorCategory` (`FloorId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FloorProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FloorProduct`;
CREATE TABLE `Himall_FloorProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FloorId` bigint(20) NOT NULL COMMENT '楼层ID',
  `Tab` int(11) NOT NULL COMMENT '楼层标签',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_HomeFloor_FloorProduct` (`FloorId`) USING BTREE,
  KEY `FK_Product_FloorProduct` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FloorTabl
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FloorTabl`;
CREATE TABLE `Himall_FloorTabl` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FloorId` bigint(20) NOT NULL COMMENT '楼层ID',
  `Name` varchar(50) NOT NULL COMMENT '楼层名称',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Id` (`Id`) USING BTREE,
  KEY `FloorIdFK` (`FloorId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FloorTablDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FloorTablDetail`;
CREATE TABLE `Himall_FloorTablDetail` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TabId` bigint(20) NOT NULL COMMENT 'TabID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `TabIdFK` (`TabId`) USING BTREE,
  KEY `ProductIdFK` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FloorTopic
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FloorTopic`;
CREATE TABLE `Himall_FloorTopic` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FloorId` bigint(20) NOT NULL COMMENT '楼层ID',
  `TopicType` int(11) NOT NULL COMMENT '专题类型',
  `TopicImage` varchar(100) NOT NULL COMMENT '专题封面图片',
  `TopicName` varchar(100) NOT NULL COMMENT '专题名称',
  `Url` varchar(1000) NOT NULL COMMENT '专题跳转URL',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_HomeFloor_FloorTopic` (`FloorId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FreightAreaContent
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FreightAreaContent`;
CREATE TABLE `Himall_FreightAreaContent` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FreightTemplateId` bigint(20) NOT NULL COMMENT '运费模板ID',
  `AreaContent` varchar(4000) DEFAULT NULL COMMENT '地区选择',
  `FirstUnit` int(11) NOT NULL COMMENT '首笔单元计量',
  `FirstUnitMonry` float NOT NULL COMMENT '首笔单元费用',
  `AccumulationUnit` int(11) NOT NULL COMMENT '递增单元计量',
  `AccumulationUnitMoney` float NOT NULL COMMENT '递增单元费用',
  `IsDefault` tinyint(4) NOT NULL COMMENT '是否为默认',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Freighttemalate_FreightAreaContent` (`FreightTemplateId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FreightAreaDetail
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FreightAreaDetail`;
CREATE TABLE `Himall_FreightAreaDetail` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FreightTemplateId` bigint(20) NOT NULL COMMENT '运费模板ID',
  `FreightAreaId` bigint(20) NOT NULL COMMENT '模板地区Id',
  `ProvinceId` int(20) NOT NULL COMMENT '省份ID',
  `CityId` int(20) NOT NULL COMMENT '城市ID',
  `CountyId` int(20) NOT NULL COMMENT '区ID',
  `TownIds` varchar(2000) DEFAULT '' COMMENT '乡镇的ID用逗号隔开',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FreightTemplate
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FreightTemplate`;
CREATE TABLE `Himall_FreightTemplate` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) DEFAULT NULL COMMENT '运费模板名称',
  `SourceAddress` int(11) NOT NULL COMMENT '宝贝发货地',
  `SendTime` varchar(100) DEFAULT NULL COMMENT '发送时间',
  `IsFree` int(11) NOT NULL COMMENT '是否商家负责运费',
  `ValuationMethod` int(11) NOT NULL COMMENT '定价方法(按体积、重量计算）',
  `ShippingMethod` int(11) DEFAULT NULL COMMENT '运送类型（物流、快递）',
  `ShopID` bigint(20) NOT NULL COMMENT '店铺ID',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_FullDiscountRule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_FullDiscountRule`;
CREATE TABLE `Himall_FullDiscountRule` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ActiveId` bigint(20) NOT NULL COMMENT '活动编号',
  `Quota` decimal(18,2) NOT NULL COMMENT '条件',
  `Discount` decimal(18,2) NOT NULL COMMENT '优惠',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IDX_Himall_Fules_ActiveId` (`ActiveId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='满减规则';

-- ----------------------------
-- Table structure for Himall_Gift
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Gift`;
CREATE TABLE `Himall_Gift` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `GiftName` varchar(100) NOT NULL COMMENT '名称',
  `NeedIntegral` int(11) NOT NULL COMMENT '需要积分',
  `LimtQuantity` int(11) NOT NULL COMMENT '限制兑换数量 0表示不限兑换数量',
  `StockQuantity` int(11) NOT NULL COMMENT '库存数量',
  `EndDate` datetime NOT NULL COMMENT '兑换结束时间',
  `NeedGrade` int(11) NOT NULL DEFAULT '0' COMMENT '等级要求 0表示不限定',
  `VirtualSales` int(11) NOT NULL DEFAULT '0' COMMENT '虚拟销量',
  `RealSales` int(11) NOT NULL DEFAULT '0' COMMENT '实际销量',
  `SalesStatus` int(11) NOT NULL COMMENT '状态',
  `ImagePath` varchar(100) DEFAULT NULL COMMENT '图片存放地址',
  `Sequence` int(11) NOT NULL DEFAULT '100' COMMENT '顺序 默认100 数字越小越靠前',
  `GiftValue` decimal(8,2) NOT NULL COMMENT '礼品价值',
  `Description` longtext COMMENT '描述',
  `AddDate` datetime NOT NULL COMMENT '添加时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_GiftOrder
-- ----------------------------
DROP TABLE IF EXISTS `Himall_GiftOrder`;
CREATE TABLE `Himall_GiftOrder` (
  `Id` bigint(20) NOT NULL COMMENT '编号',
  `OrderStatus` int(11) NOT NULL COMMENT '订单状态',
  `UserId` bigint(20) NOT NULL COMMENT '用户编号',
  `UserRemark` varchar(200) DEFAULT NULL COMMENT '会员留言',
  `ShipTo` varchar(100) DEFAULT NULL COMMENT '收货人',
  `CellPhone` varchar(100) DEFAULT NULL COMMENT '收货人电话',
  `TopRegionId` int(11) NOT NULL COMMENT '一级地区',
  `RegionId` int(11) NOT NULL COMMENT '地区编号',
  `RegionFullName` varchar(100) DEFAULT NULL COMMENT '地区全称',
  `Address` varchar(100) DEFAULT NULL COMMENT '地址',
  `ExpressCompanyName` varchar(4000) DEFAULT NULL COMMENT '快递公司',
  `ShipOrderNumber` varchar(4000) DEFAULT NULL COMMENT '快递单号',
  `ShippingDate` datetime DEFAULT NULL COMMENT '发货时间',
  `OrderDate` datetime NOT NULL COMMENT '下单时间',
  `FinishDate` datetime DEFAULT NULL COMMENT '完成时间',
  `TotalIntegral` int(11) NOT NULL COMMENT '积分总价',
  `CloseReason` varchar(200) DEFAULT NULL COMMENT '关闭原因',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_GiftOrderItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_GiftOrderItem`;
CREATE TABLE `Himall_GiftOrderItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `OrderId` bigint(20) NOT NULL COMMENT '订单编号',
  `GiftId` bigint(20) NOT NULL COMMENT '礼品编号',
  `Quantity` int(11) NOT NULL COMMENT '数量',
  `SaleIntegral` int(11) NOT NULL COMMENT '积分单价',
  `GiftName` varchar(100) DEFAULT NULL COMMENT '礼品名称',
  `GiftValue` decimal(8,3) NOT NULL COMMENT '礼品价值',
  `ImagePath` varchar(100) DEFAULT NULL COMMENT '图片存放地址',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_Gitem_OrderId` (`OrderId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_HandSlideAd
-- ----------------------------
DROP TABLE IF EXISTS `Himall_HandSlideAd`;
CREATE TABLE `Himall_HandSlideAd` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ImageUrl` varchar(100) NOT NULL COMMENT '图片URL',
  `Url` varchar(1000) NOT NULL COMMENT '图片跳转URL',
  `DisplaySequence` bigint(20) NOT NULL COMMENT '排序',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_HomeCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_HomeCategory`;
CREATE TABLE `Himall_HomeCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `RowId` int(11) NOT NULL COMMENT '分类所属行数',
  `CategoryId` bigint(20) NOT NULL COMMENT '分类ID',
  `Depth` int(11) NOT NULL COMMENT '分类深度(最深3）',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Category_HomeCategory` (`CategoryId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_HomeCategoryRow
-- ----------------------------
DROP TABLE IF EXISTS `Himall_HomeCategoryRow`;
CREATE TABLE `Himall_HomeCategoryRow` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `RowId` int(11) NOT NULL COMMENT '行ID',
  `Image1` varchar(100) NOT NULL COMMENT '所属行推荐图片1',
  `Url1` varchar(100) NOT NULL COMMENT '所属行推荐图片1的URL',
  `Image2` varchar(100) NOT NULL COMMENT '所属行推荐图片2',
  `Url2` varchar(100) NOT NULL COMMENT '所属行推荐图片2的URL',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_HomeFloor
-- ----------------------------
DROP TABLE IF EXISTS `Himall_HomeFloor`;
CREATE TABLE `Himall_HomeFloor` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `FloorName` varchar(100) NOT NULL COMMENT '楼层名称',
  `SubName` varchar(100) DEFAULT NULL COMMENT '楼层小标题',
  `DisplaySequence` bigint(20) NOT NULL COMMENT '显示顺序',
  `IsShow` tinyint(1) NOT NULL COMMENT '是否显示的首页',
  `StyleLevel` int(10) unsigned NOT NULL COMMENT '楼层所属样式（目前支持2套）',
  `DefaultTabName` varchar(50) DEFAULT NULL COMMENT '楼层的默认tab标题',
  `CommodityStyle` int(11) NOT NULL COMMENT '商品样式，0：默认，1：一排5个，2：一排4个',
  `DisplayMode` int(11) NOT NULL COMMENT '显示方式，0=默认，1=平铺展示，2=左右轮播',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Id` (`Id`),
  KEY `Id_2` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ImageAd
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ImageAd`;
CREATE TABLE `Himall_ImageAd` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `ImageUrl` varchar(100) NOT NULL COMMENT '图片的存放URL',
  `Url` varchar(1000) NOT NULL COMMENT '图片的调整地址',
  `IsTransverseAD` tinyint(1) NOT NULL COMMENT '是否是横向长广告',
  `TypeId` int(11) NOT NULL DEFAULT '0' COMMENT '微信头像',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_IntegralMallAd
-- ----------------------------
DROP TABLE IF EXISTS `Himall_IntegralMallAd`;
CREATE TABLE `Himall_IntegralMallAd` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ActivityType` int(11) NOT NULL COMMENT '活动类型',
  `ActivityId` bigint(20) NOT NULL COMMENT '活动编号',
  `Cover` varchar(255) DEFAULT NULL COMMENT '显示图片',
  `ShowStatus` int(11) NOT NULL COMMENT '显示状态',
  `ShowPlatform` int(11) NOT NULL COMMENT '显示平台',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_InviteRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_InviteRecord`;
CREATE TABLE `Himall_InviteRecord` (
  `Id` bigint(11) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(100) NOT NULL COMMENT '用户名',
  `RegName` varchar(100) NOT NULL COMMENT '邀请的用户',
  `InviteIntegral` int(11) NOT NULL COMMENT '邀请获得的积分',
  `RegIntegral` int(11) NOT NULL COMMENT '被邀请获得的积分',
  `RegTime` datetime NOT NULL COMMENT '注册时间',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `RegUserId` bigint(20) NOT NULL COMMENT '被邀请的用户ID',
  `RecordTime` datetime NOT NULL COMMENT '获得积分时间',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `InviteMember` (`UserId`) USING BTREE,
  KEY `RegMember` (`RegUserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_InviteRule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_InviteRule`;
CREATE TABLE `Himall_InviteRule` (
  `Id` bigint(11) NOT NULL AUTO_INCREMENT,
  `InviteIntegral` int(11) NOT NULL COMMENT '邀请能获得的积分',
  `RegIntegral` int(11) NOT NULL COMMENT '被邀请能获得的积分',
  `ShareTitle` varchar(100) DEFAULT NULL COMMENT '分享标题',
  `ShareDesc` varchar(1000) DEFAULT NULL COMMENT '分享详细',
  `ShareIcon` varchar(200) DEFAULT NULL COMMENT '分享图标',
  `ShareRule` varchar(1000) DEFAULT NULL COMMENT '分享规则',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_InvoiceContext
-- ----------------------------
DROP TABLE IF EXISTS `Himall_InvoiceContext`;
CREATE TABLE `Himall_InvoiceContext` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL COMMENT '发票名称',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_InvoiceTitle
-- ----------------------------
DROP TABLE IF EXISTS `Himall_InvoiceTitle`;
CREATE TABLE `Himall_InvoiceTitle` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `InvoiceType` int(11) NOT NULL DEFAULT '1' COMMENT '发票类型（1:普通发票、2:电子发票、3:增值税发票）',
  `Name` varchar(200) DEFAULT NULL COMMENT '抬头名称',
  `Code` varchar(200) DEFAULT NULL COMMENT '税号',
  `InvoiceContext` varchar(50) DEFAULT '0' COMMENT '发票明细',
  `RegisterAddress` varchar(200) DEFAULT NULL COMMENT '注册地址',
  `RegisterPhone` varchar(50) DEFAULT NULL COMMENT '注册电话',
  `BankName` varchar(100) DEFAULT NULL COMMENT '开户银行',
  `BankNo` varchar(50) DEFAULT NULL COMMENT '银行帐号',
  `RealName` varchar(50) DEFAULT NULL COMMENT '收票人姓名',
  `CellPhone` varchar(20) DEFAULT NULL COMMENT '收票人手机号',
  `Email` varchar(50) DEFAULT NULL COMMENT '收票人邮箱',
  `RegionID` int(11) NOT NULL DEFAULT '0' COMMENT '收票人地址区域ID',
  `Address` varchar(100) DEFAULT NULL COMMENT '收票人详细地址',
  `IsDefault` tinyint(4) NOT NULL COMMENT '是否默认',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Label
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Label`;
CREATE TABLE `Himall_Label` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `LabelName` varchar(50) NOT NULL COMMENT '标签名称',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_LiveProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_LiveProduct`;
CREATE TABLE `Himall_LiveProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL COMMENT '商品名称',
  `Url` varchar(300) NOT NULL COMMENT '小程序详情地址',
  `Price` decimal(8,2) NOT NULL COMMENT '商品价格',
  `Image` varchar(500) NOT NULL COMMENT '商品图片',
  `RoomId` bigint(20) NOT NULL COMMENT '直播间ID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `SaleCount` int(11) NOT NULL DEFAULT '0' COMMENT '销售数量',
  `SaleAmount` decimal(8,2) NOT NULL DEFAULT '0.00' COMMENT '销售金额',
  `Price2` decimal(8,2) NOT NULL DEFAULT '0.00' COMMENT '区间价/折扣价',
  `PriceType` int(11) NOT NULL DEFAULT '1' COMMENT '价格类型: 1  一口价  2  区间价   3  折扣价',
  PRIMARY KEY (`Id`),
  KEY `IX_RoomId` (`RoomId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Table structure for Himall_LiveProductLibrary
-- ----------------------------
DROP TABLE IF EXISTS `Himall_LiveProductLibrary`;
CREATE TABLE `Himall_LiveProductLibrary` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(10) NOT NULL COMMENT '商品ID',
  `LiveAuditStatus` int(11) NOT NULL DEFAULT '-1' COMMENT '直播商品库审核状态',
  `ImageMediaId` varchar(100) DEFAULT NULL COMMENT '小程序上传图片MeidaId',
  `GoodsId` bigint(10) NOT NULL DEFAULT '0' COMMENT '小程序直播商品库商品ID',
  `AuditId` bigint(10) NOT NULL DEFAULT '0' COMMENT '小程序直播商品库审核单ID',
  `ApplyLiveTime` datetime NOT NULL COMMENT '提交申请直播商品库时间 用于判断商品图片的MediaId是否还有效',
  `LiveAuditMsg` varchar(200) CHARACTER SET utf8 DEFAULT NULL COMMENT '小程序直播商品审核消息',
  `ShopId` bigint(10) NOT NULL DEFAULT '0' COMMENT '店铺ID',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='直播商品库表';

-- ----------------------------
-- Table structure for Himall_LiveReply
-- ----------------------------
DROP TABLE IF EXISTS `Himall_LiveReply`;
CREATE TABLE `Himall_LiveReply` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoomId` int(11) NOT NULL COMMENT '直播间ID',
  `ExpireTime` datetime NOT NULL,
  `CreateTime` datetime NOT NULL,
  `MediaUrl` varchar(255) NOT NULL COMMENT '回放视频地址',
  PRIMARY KEY (`Id`),
  KEY `IX_Room` (`RoomId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Table structure for Himall_LiveRoom
-- ----------------------------
DROP TABLE IF EXISTS `Himall_LiveRoom`;
CREATE TABLE `Himall_LiveRoom` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Sequence` int(255) NOT NULL DEFAULT '0' COMMENT '序号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `RoomId` bigint(20) NOT NULL COMMENT '直播间ID(小程序生成的ID)',
  `Name` varchar(50) NOT NULL COMMENT '直播间名称',
  `CoverImg` varchar(500) NOT NULL COMMENT '直播间封面',
  `AnchorName` varchar(50) NOT NULL COMMENT '主播姓名',
  `AnchorImg` varchar(500) NOT NULL COMMENT '主播头像',
  `Status` int(11) NOT NULL COMMENT '直播状态：101: 直播中, 102: 未开始, 103: 已结束, 104: 禁播, 105: 暂停中, 106: 异常，107:已过期',
  `StartTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime DEFAULT NULL COMMENT '结束时间',
  `HasReplay` bit(1) NOT NULL COMMENT '是否回放',
  `CartMember` int(11) NOT NULL DEFAULT '0' COMMENT '加购人数',
  `CartCount` int(11) NOT NULL DEFAULT '0' COMMENT '加购次数',
  `PaymentMember` int(11) NOT NULL DEFAULT '0' COMMENT '支付人数',
  `PaymentOrder` int(11) NOT NULL DEFAULT '0' COMMENT '直播订单数',
  `PaymentAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '支付金额',
  `ShareImg` varchar(500) DEFAULT NULL,
  `ShareImgMediaId` varchar(100) DEFAULT NULL COMMENT '分享图片上传小程序端MediaId',
  `AnchorWechat` varchar(50) DEFAULT NULL COMMENT '主播微信号',
  `Type` int(11) NOT NULL DEFAULT '0' COMMENT '直播间类型 【1: 推流，0：手机直播】',
  `ScreenType` int(11) NOT NULL DEFAULT '0' COMMENT '横屏、竖屏 【1：横屏，0：竖屏】（横屏：视频宽高比为16:9、4:3、1.85:1 ；竖屏：视频宽高比为9:16、2:3）',
  `CreateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CloseLike` int(11) NOT NULL DEFAULT '0' COMMENT '是否关闭点赞（若关闭，直播开始后不允许开启）',
  `CloseGoods` int(11) NOT NULL DEFAULT '0',
  `CloseComment` int(11) NOT NULL DEFAULT '0',
  `CoverImgMediaId` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_RoomId` (`RoomId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Table structure for Himall_Log
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Log`;
CREATE TABLE `Himall_Log` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `PageUrl` varchar(1000) NOT NULL,
  `Date` datetime NOT NULL,
  `UserName` varchar(100) NOT NULL,
  `IPAddress` varchar(100) NOT NULL,
  `Description` varchar(1000) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Manager
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Manager`;
CREATE TABLE `Himall_Manager` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `RoleId` bigint(20) NOT NULL COMMENT '角色ID',
  `UserName` varchar(100) NOT NULL COMMENT '用户名称',
  `Password` varchar(100) NOT NULL COMMENT '密码',
  `PasswordSalt` varchar(100) NOT NULL COMMENT '密码加盐',
  `CreateDate` datetime NOT NULL COMMENT '创建日期',
  `Remark` varchar(1000) DEFAULT NULL,
  `RealName` varchar(1000) DEFAULT NULL COMMENT '真实名称',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MarketServiceRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MarketServiceRecord`;
CREATE TABLE `Himall_MarketServiceRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MarketServiceId` bigint(20) NOT NULL,
  `StartTime` datetime NOT NULL COMMENT '开始时间',
  `EndTime` datetime NOT NULL COMMENT '结束时间',
  `BuyTime` datetime NOT NULL COMMENT '购买时间',
  `SettlementFlag` int(16) unsigned zerofill NOT NULL,
  `Price` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '服务购买价格',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_MarketServiceRecord_Himall_ActiveMarketService` (`MarketServiceId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MarketSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MarketSetting`;
CREATE TABLE `Himall_MarketSetting` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `TypeId` int(11) NOT NULL COMMENT '营销类型ID',
  `Price` decimal(18,2) NOT NULL COMMENT '营销使用价格（/月）',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MarketSettingMeta
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MarketSettingMeta`;
CREATE TABLE `Himall_MarketSettingMeta` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `MarketId` int(11) NOT NULL,
  `MetaKey` varchar(100) NOT NULL,
  `MetaValue` text,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Hiamll_MarketSettingMeta_ToSetting` (`MarketId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Member
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Member`;
CREATE TABLE `Himall_Member` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(100) NOT NULL COMMENT '名称',
  `Password` varchar(100) NOT NULL COMMENT '密码',
  `PasswordSalt` varchar(100) NOT NULL COMMENT '密码加盐',
  `Nick` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Sex` int(11) NOT NULL DEFAULT '0' COMMENT '性别',
  `Email` varchar(100) DEFAULT NULL COMMENT '邮件',
  `CreateDate` datetime NOT NULL COMMENT '创建日期',
  `TopRegionId` int(11) NOT NULL COMMENT '省份ID',
  `RegionId` int(11) NOT NULL COMMENT '省市区ID',
  `RealName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CellPhone` varchar(100) DEFAULT NULL COMMENT '电话',
  `QQ` varchar(100) DEFAULT NULL COMMENT 'QQ',
  `Address` varchar(100) DEFAULT NULL COMMENT '街道地址',
  `Disabled` tinyint(1) NOT NULL COMMENT '是否禁用',
  `LastLoginDate` datetime NOT NULL COMMENT '最后登录日期',
  `OrderNumber` int(11) NOT NULL COMMENT '下单次数',
  `TotalAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '总消费金额（不排除退款）',
  `Expenditure` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '总消费金额（不排除退款）',
  `Points` int(11) NOT NULL,
  `Photo` varchar(100) DEFAULT NULL COMMENT '头像',
  `ParentSellerId` bigint(20) NOT NULL DEFAULT '0' COMMENT '商家父账号ID',
  `Remark` varchar(1000) DEFAULT NULL,
  `PayPwd` varchar(100) DEFAULT NULL COMMENT '支付密码',
  `PayPwdSalt` varchar(100) DEFAULT NULL COMMENT '支付密码加密字符',
  `InviteUserId` bigint(20) NOT NULL DEFAULT '0',
  `BirthDay` date DEFAULT NULL COMMENT '会员生日',
  `Occupation` varchar(15) DEFAULT NULL COMMENT '职业',
  `NetAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '净消费金额（排除退款）',
  `LastConsumptionTime` datetime DEFAULT NULL COMMENT '最后消费时间',
  `Platform` int(11) NOT NULL DEFAULT '0' COMMENT '用户来源终端',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IX_UserName` (`UserName`) USING BTREE,
  KEY `IX_Email` (`Email`) USING BTREE,
  KEY `IX_CellPhone` (`CellPhone`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberActivityDegree
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberActivityDegree`;
CREATE TABLE `Himall_MemberActivityDegree` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL DEFAULT '0' COMMENT '会员编号',
  `OneMonth` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否为一个月活跃用户',
  `ThreeMonth` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否为三个月活跃用户',
  `SixMonth` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否为六个月活跃用户',
  `OneMonthEffectiveTime` datetime DEFAULT NULL COMMENT '一个月活跃会员有效时间',
  `ThreeMonthEffectiveTime` datetime DEFAULT NULL COMMENT '三个月活跃会员有效时间',
  `SixMonthEffectiveTime` datetime DEFAULT NULL COMMENT '六个月活跃会员有效时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberBuyCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberBuyCategory`;
CREATE TABLE `Himall_MemberBuyCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '会员ID',
  `CategoryId` bigint(20) NOT NULL COMMENT '类别ID',
  `OrdersCount` int(11) NOT NULL DEFAULT '0' COMMENT '购买次数',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberConsumeStatistic
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberConsumeStatistic`;
CREATE TABLE `Himall_MemberConsumeStatistic` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL,
  `ShopId` bigint(20) NOT NULL COMMENT '门店Id',
  `NetAmount` decimal(10,2) NOT NULL COMMENT '净消费金额(退款需要维护)',
  `OrderNumber` bigint(20) NOT NULL COMMENT '消费次数(退款不维护)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberContact
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberContact`;
CREATE TABLE `Himall_MemberContact` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `UserType` int(11) NOT NULL COMMENT '用户类型(0 Email  1 SMS)',
  `ServiceProvider` varchar(100) NOT NULL COMMENT '插件名称',
  `Contact` varchar(100) NOT NULL COMMENT '联系号码',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberGrade
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberGrade`;
CREATE TABLE `Himall_MemberGrade` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `GradeName` varchar(100) NOT NULL COMMENT '会员等级名称',
  `Integral` int(11) NOT NULL COMMENT '该等级所需积分',
  `Remark` varchar(1000) DEFAULT NULL COMMENT '描述',
  `Discount` decimal(8,2) NOT NULL DEFAULT '10.00',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberGroup
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberGroup`;
CREATE TABLE `Himall_MemberGroup` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '门店编号',
  `StatisticsType` int(11) NOT NULL COMMENT '统计类型',
  `Total` int(11) NOT NULL COMMENT '统计数量',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberIntegral
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberIntegral`;
CREATE TABLE `Himall_MemberIntegral` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MemberId` bigint(20) NOT NULL COMMENT '会员ID',
  `UserName` varchar(100) NOT NULL COMMENT '用户名称',
  `HistoryIntegrals` int(11) NOT NULL COMMENT '用户历史积分',
  `AvailableIntegrals` int(11) NOT NULL COMMENT '用户可用积分',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Member_MemberIntegral` (`MemberId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberIntegralExchangeRule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberIntegralExchangeRule`;
CREATE TABLE `Himall_MemberIntegralExchangeRule` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `IntegralPerMoney` int(11) NOT NULL COMMENT '一块钱对应多少积分',
  `MoneyPerIntegral` int(11) NOT NULL COMMENT '一个积分对应多少钱',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberIntegralRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberIntegralRecord`;
CREATE TABLE `Himall_MemberIntegralRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MemberId` bigint(20) NOT NULL,
  `UserName` varchar(100) CHARACTER SET utf8 COLLATE utf8_unicode_ci NOT NULL COMMENT '用户名称',
  `TypeId` int(11) NOT NULL COMMENT '兑换类型（登录、下单等）',
  `Integral` int(11) NOT NULL COMMENT '积分数量',
  `RecordDate` datetime NOT NULL COMMENT '记录日期',
  `ReMark` varchar(100) DEFAULT NULL COMMENT '说明',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `fk_MemberId_Members` (`MemberId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberIntegralRecordAction
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberIntegralRecordAction`;
CREATE TABLE `Himall_MemberIntegralRecordAction` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `IntegralRecordId` bigint(20) NOT NULL COMMENT '积分兑换ID',
  `VirtualItemTypeId` int(11) NOT NULL COMMENT '兑换虚拟物l类型ID',
  `VirtualItemId` bigint(20) NOT NULL COMMENT '虚拟物ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `fk_IntegralRecordId_MemberIntegralRecord` (`IntegralRecordId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberIntegralRule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberIntegralRule`;
CREATE TABLE `Himall_MemberIntegralRule` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TypeId` int(11) NOT NULL COMMENT '积分规则类型ID',
  `Integral` int(11) NOT NULL COMMENT '规则对应的积分数量',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberLabel
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberLabel`;
CREATE TABLE `Himall_MemberLabel` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT 'Id',
  `MemId` bigint(20) NOT NULL COMMENT '会员ID',
  `LabelId` bigint(20) NOT NULL COMMENT '标签Id',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberOpenId
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberOpenId`;
CREATE TABLE `Himall_MemberOpenId` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `OpenId` varchar(100) DEFAULT NULL COMMENT '微信OpenID',
  `UnionOpenId` varchar(100) DEFAULT NULL COMMENT '开发平台Openid',
  `UnionId` varchar(100) DEFAULT NULL COMMENT '开发平台Unionid',
  `ServiceProvider` varchar(100) NOT NULL COMMENT '插件名称（Himall.Plugin.OAuth.WeiXin）',
  `AppIdType` int(255) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Member_MemberOpenId` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MemberSignIn
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MemberSignIn`;
CREATE TABLE `Himall_MemberSignIn` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `LastSignTime` datetime NOT NULL COMMENT '最近签到时间',
  `DurationDay` int(11) NOT NULL DEFAULT '0' COMMENT '持续签到天数 每周期后清零',
  `DurationDaySum` int(11) NOT NULL DEFAULT '0' COMMENT '持续签到天数总数 非连续周期清零',
  `SignDaySum` bigint(20) NOT NULL DEFAULT '0' COMMENT '签到总天数',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IDX_Himall_MenIn_UserId` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Menu
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Menu`;
CREATE TABLE `Himall_Menu` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ParentId` bigint(20) NOT NULL COMMENT '上级ID',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `Title` varchar(10) NOT NULL COMMENT '标题',
  `Url` varchar(200) DEFAULT NULL COMMENT '链接地址',
  `Depth` smallint(6) NOT NULL COMMENT '深度',
  `Sequence` smallint(6) NOT NULL,
  `FullIdPath` varchar(100) NOT NULL COMMENT '全路径',
  `Platform` int(11) NOT NULL COMMENT '终端',
  `UrlType` int(11) NOT NULL COMMENT 'url类型',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MessageLog
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MessageLog`;
CREATE TABLE `Himall_MessageLog` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `TypeId` varchar(100) DEFAULT NULL,
  `MessageContent` varchar(1000) DEFAULT NULL,
  `SendTime` datetime NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MobileFootMenu
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MobileFootMenu`;
CREATE TABLE `Himall_MobileFootMenu` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) DEFAULT NULL COMMENT '导航名称',
  `Url` varchar(255) DEFAULT NULL COMMENT '链接地址',
  `MenuIcon` varchar(255) DEFAULT NULL COMMENT '显示图片',
  `MenuIconSel` varchar(255) DEFAULT NULL COMMENT '未选中显示图片',
  `Type` tinyint(4) NOT NULL DEFAULT '1' COMMENT '菜单类型（1代表微信、2代表小程序）',
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '店铺Id(0默认是平台)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MobileHomeProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MobileHomeProduct`;
CREATE TABLE `Himall_MobileHomeProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `PlatFormType` int(11) NOT NULL COMMENT '终端类型(微信、WAP）',
  `Sequence` smallint(6) NOT NULL COMMENT '顺序',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_MobileHomeProducts_Himall_Products` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_MobileHomeTopic
-- ----------------------------
DROP TABLE IF EXISTS `Himall_MobileHomeTopic`;
CREATE TABLE `Himall_MobileHomeTopic` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '店铺ID',
  `Platform` int(11) NOT NULL COMMENT '终端',
  `TopicId` bigint(20) NOT NULL COMMENT '专题ID',
  `Sequence` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK__Himall_Mo__Topic__02C769E9` (`TopicId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ModuleProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ModuleProduct`;
CREATE TABLE `Himall_ModuleProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ModuleId` bigint(20) NOT NULL COMMENT '模块ID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `DisplaySequence` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_ModuleProduct` (`ProductId`) USING BTREE,
  KEY `FK_TopicModule_ModuleProduct` (`ModuleId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OpenId
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OpenId`;
CREATE TABLE `Himall_OpenId` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OpenId` varchar(100) NOT NULL,
  `SubscribeTime` date NOT NULL COMMENT '关注时间',
  `IsSubscribe` tinyint(1) NOT NULL COMMENT '是否关注',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Order
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Order`;
CREATE TABLE `Himall_Order` (
  `Id` bigint(20) NOT NULL,
  `OrderStatus` int(11) NOT NULL COMMENT '订单状态 [Description("待付款")]WaitPay = 1,[Description("待发货")]WaitDelivery,[Description("待收货")]WaitReceiving,[Description("已关闭")]Close,[Description("已完成")]Finish',
  `OrderDate` datetime NOT NULL COMMENT '订单创建日期',
  `CloseReason` varchar(1000) DEFAULT NULL COMMENT '关闭原因',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `SellerPhone` varchar(100) DEFAULT NULL COMMENT '商家电话',
  `SellerAddress` varchar(100) DEFAULT NULL COMMENT '商家发货地址',
  `SellerRemark` varchar(1000) DEFAULT NULL COMMENT '商家说明',
  `SellerRemarkFlag` int(11) DEFAULT NULL,
  `UserId` bigint(20) NOT NULL COMMENT '会员ID',
  `UserName` varchar(100) NOT NULL COMMENT '会员名称',
  `UserRemark` varchar(1000) DEFAULT NULL COMMENT '会员留言',
  `ShipTo` varchar(100) NOT NULL COMMENT '收货人',
  `CellPhone` varchar(100) DEFAULT NULL COMMENT '收货人电话',
  `TopRegionId` int(11) NOT NULL COMMENT '收货人地址省份ID',
  `RegionId` int(11) NOT NULL COMMENT '收货人区域ID',
  `RegionFullName` varchar(100) NOT NULL COMMENT '全名的收货地址',
  `Address` varchar(100) NOT NULL COMMENT '收货具体街道信息',
  `ReceiveLongitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '收货地址坐标',
  `ReceiveLatitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '收货地址坐标',
  `ExpressCompanyName` varchar(100) DEFAULT NULL COMMENT '快递公司',
  `Freight` decimal(8,2) NOT NULL COMMENT '运费',
  `ShipOrderNumber` varchar(100) CHARACTER SET utf8mb4 DEFAULT NULL COMMENT '物流订单号',
  `ShippingDate` datetime DEFAULT NULL COMMENT '发货日期',
  `IsPrinted` tinyint(1) NOT NULL COMMENT '是否打印快递单',
  `PaymentTypeName` varchar(100) DEFAULT NULL COMMENT '付款类型名称',
  `PaymentTypeGateway` varchar(100) DEFAULT NULL COMMENT '付款类型使用 插件名称',
  `PaymentType` int(11) NOT NULL,
  `GatewayOrderId` varchar(100) DEFAULT NULL COMMENT '支付接口返回的ID',
  `PayRemark` varchar(1000) DEFAULT NULL COMMENT '付款注释',
  `PayDate` datetime DEFAULT NULL COMMENT '付款日期',
  `Tax` decimal(8,2) NOT NULL COMMENT '税钱，但是未使用',
  `FinishDate` datetime DEFAULT NULL COMMENT '完成订单日期',
  `ProductTotalAmount` decimal(18,2) NOT NULL COMMENT '商品总金额',
  `RefundTotalAmount` decimal(18,2) NOT NULL COMMENT '退款金额',
  `CommisTotalAmount` decimal(18,2) NOT NULL COMMENT '佣金总金额',
  `RefundCommisAmount` decimal(18,2) NOT NULL COMMENT '退还佣金总金额',
  `ActiveType` int(11) NOT NULL DEFAULT '0' COMMENT '未使用',
  `Platform` int(11) NOT NULL DEFAULT '0' COMMENT '来自哪个终端的订单',
  `DiscountAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '针对该订单的优惠金额（用于优惠券）',
  `IntegralDiscount` decimal(18,2) NOT NULL COMMENT '积分优惠金额',
  `OrderType` int(11) NOT NULL DEFAULT '0' COMMENT '订单类型',
  `OrderRemarks` varchar(200) DEFAULT NULL COMMENT '订单备注(买家留言)',
  `LastModifyTime` datetime NOT NULL COMMENT '最后操作时间',
  `DeliveryType` int(11) NOT NULL COMMENT '发货类型(快递配送,到店自提)',
  `ShopBranchId` bigint(20) NOT NULL DEFAULT '0' COMMENT '门店ID',
  `PickupCode` varchar(20) DEFAULT NULL COMMENT '提货码',
  `TotalAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '订单实付金额',
  `ActualPayAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '订单实收金额',
  `FullDiscount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '满额减金额',
  `CapitalAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '预付款支付金额',
  `CouponId` bigint(20) NOT NULL COMMENT '使用的优惠券Id',
  `CancelReason` varchar(200) DEFAULT NULL COMMENT '达达取消发单原因',
  `DadaStatus` int(11) NOT NULL DEFAULT '0' COMMENT '达达状态',
  `PlatCouponId` bigint(20) NOT NULL COMMENT '使用的平台优惠券Id',
  `PlatDiscountAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台优惠券抵扣金额',
  `IsLive` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否直播订单',
  `IsSend` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否发送过短信',
  `IsPushWangDian` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否推送过',
  `WangDianIsSend` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否推送过发货',
  `PushWangDianResult` tinyint(1) NOT NULL DEFAULT '0' COMMENT '旺店通推送结果',
  `MainOrderId` bigint(20) NOT NULL COMMENT '主订单号',
  `CouponType` int(11) DEFAULT NULL COMMENT '优惠券类型（0代表商家券，1代表商家红包）',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderComment
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderComment`;
CREATE TABLE `Himall_OrderComment` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `UserName` varchar(100) NOT NULL COMMENT '用户名称',
  `CommentDate` datetime NOT NULL COMMENT '评价日期',
  `PackMark` int(11) NOT NULL COMMENT '包装评分',
  `DeliveryMark` int(11) NOT NULL COMMENT '物流评分',
  `ServiceMark` int(11) NOT NULL COMMENT '服务评分',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Order_OrderComment` (`OrderId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderComplaint
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderComplaint`;
CREATE TABLE `Himall_OrderComplaint` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `Status` int(11) NOT NULL COMMENT '审核状态',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `ShopPhone` varchar(100) NOT NULL COMMENT '店铺联系方式',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `UserName` varchar(100) NOT NULL COMMENT '用户名称',
  `UserPhone` varchar(100) DEFAULT NULL COMMENT '用户联系方式',
  `ComplaintDate` datetime NOT NULL COMMENT '投诉日期',
  `ComplaintReason` varchar(1000) NOT NULL COMMENT '投诉原因',
  `SellerReply` varchar(1000) DEFAULT NULL COMMENT '商家反馈信息',
  `PlatRemark` varchar(10000) DEFAULT NULL COMMENT '投诉备注',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Order_OrderComplaint` (`OrderId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderExpressData
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderExpressData`;
CREATE TABLE `Himall_OrderExpressData` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `CompanyCode` varchar(50) NOT NULL,
  `ExpressNumber` varchar(50) NOT NULL,
  `DataContent` varchar(2000) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderInvoice
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderInvoice`;
CREATE TABLE `Himall_OrderInvoice` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OrderId` bigint(20) NOT NULL DEFAULT '0' COMMENT '订单编号',
  `InvoiceType` int(11) NOT NULL DEFAULT '0' COMMENT '发票类型（1:普通发票、2:电子发票、3:增值税发票）',
  `InvoiceTitle` varchar(100) DEFAULT NULL COMMENT '发票抬头',
  `InvoiceCode` varchar(200) DEFAULT NULL COMMENT '税号',
  `InvoiceContext` varchar(100) DEFAULT NULL COMMENT '发票明细(个人、公司)',
  `RegisterAddress` varchar(200) DEFAULT NULL COMMENT '注册地址',
  `RegisterPhone` varchar(50) DEFAULT NULL COMMENT '注册电话',
  `BankName` varchar(100) DEFAULT NULL COMMENT '开户银行',
  `BankNo` varchar(50) DEFAULT NULL COMMENT '银行帐号',
  `RealName` varchar(50) DEFAULT NULL COMMENT '收票人姓名',
  `CellPhone` varchar(20) DEFAULT NULL COMMENT '收票人手机号',
  `Email` varchar(50) DEFAULT NULL COMMENT '收票人邮箱',
  `RegionID` int(11) NOT NULL DEFAULT '0' COMMENT '收票人地址区域ID',
  `Address` varchar(100) DEFAULT NULL COMMENT '收票人详细地址',
  `VatInvoiceDay` int(11) NOT NULL DEFAULT '0' COMMENT '订单完成后多少天开具增值税发票',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderItem`;
CREATE TABLE `Himall_OrderItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `RoomId` bigint(20) NOT NULL DEFAULT '0' COMMENT '直播间ID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `SkuId` varchar(100) DEFAULT NULL COMMENT 'SKUId',
  `SKU` varchar(100) DEFAULT NULL COMMENT 'SKU表SKU字段',
  `Quantity` bigint(20) NOT NULL COMMENT '购买数量',
  `ReturnQuantity` bigint(20) NOT NULL COMMENT '退货数量',
  `CostPrice` decimal(18,2) NOT NULL COMMENT '成本价',
  `SalePrice` decimal(18,2) NOT NULL COMMENT '销售价',
  `DiscountAmount` decimal(18,2) NOT NULL COMMENT '优惠金额',
  `RealTotalPrice` decimal(18,2) NOT NULL COMMENT '实际应付金额',
  `RefundPrice` decimal(18,2) NOT NULL COMMENT '退款价格',
  `ProductName` varchar(100) NOT NULL COMMENT '商品名称',
  `Color` varchar(100) DEFAULT NULL COMMENT 'SKU颜色',
  `Size` varchar(100) DEFAULT NULL COMMENT 'SKU尺寸',
  `Version` varchar(100) DEFAULT NULL COMMENT 'SKU版本',
  `ThumbnailsUrl` varchar(100) DEFAULT NULL COMMENT '缩略图',
  `CommisRate` decimal(18,4) NOT NULL COMMENT '分佣比例',
  `EnabledRefundAmount` decimal(18,2) DEFAULT NULL COMMENT '可退金额',
  `IsLimitBuy` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否为限时购商品',
  `EnabledRefundIntegral` decimal(18,2) DEFAULT NULL COMMENT '可退积分抵扣金额',
  `CouponDiscount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '优惠券抵扣金额',
  `FullDiscount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '满额减平摊到订单项的金额',
  `EffectiveDate` datetime DEFAULT NULL COMMENT '核销码生效时间',
  `FlashSaleId` bigint(11) NOT NULL DEFAULT '0' COMMENT '限时购活动ID',
  `PlatCouponDiscount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台优惠券抵扣金额',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Order_OrderItem` (`OrderId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderOperationLog
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderOperationLog`;
CREATE TABLE `Himall_OrderOperationLog` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `Operator` varchar(100) NOT NULL COMMENT '操作者',
  `OperateDate` datetime NOT NULL COMMENT '操作日期',
  `OperateContent` varchar(1000) DEFAULT NULL COMMENT '操作内容',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Order_OrderOperationLog` (`OrderId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderPay
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderPay`;
CREATE TABLE `Himall_OrderPay` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `PayId` bigint(20) NOT NULL,
  `OrderId` bigint(20) NOT NULL,
  `PayState` tinyint(1) unsigned zerofill NOT NULL COMMENT '支付状态',
  `PayTime` datetime DEFAULT NULL COMMENT '支付时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderRefund
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderRefund`;
CREATE TABLE `Himall_OrderRefund` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `OrderItemId` bigint(20) NOT NULL COMMENT '订单详情ID',
  `VerificationCodeIds` varchar(1000) DEFAULT '' COMMENT '核销码ID集合(本次申请哪些核销码退款)',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `Applicant` varchar(100) NOT NULL COMMENT '申请内容',
  `ContactPerson` varchar(100) DEFAULT NULL COMMENT '联系人',
  `ContactCellPhone` varchar(100) DEFAULT NULL COMMENT '联系电话',
  `RefundAccount` varchar(100) DEFAULT NULL COMMENT '退款金额',
  `ApplyDate` datetime NOT NULL COMMENT '申请时间',
  `Amount` decimal(18,2) NOT NULL COMMENT '金额',
  `Reason` varchar(1000) NOT NULL COMMENT '退款原因',
  `ReasonDetail` varchar(1000) DEFAULT NULL COMMENT '退款详情',
  `SellerAuditStatus` int(11) NOT NULL COMMENT '商家审核状态',
  `SellerAuditDate` datetime NOT NULL COMMENT '商家审核时间',
  `SellerRemark` varchar(1000) DEFAULT NULL COMMENT '商家注释',
  `ManagerConfirmStatus` int(11) NOT NULL COMMENT '平台审核状态',
  `ManagerConfirmDate` datetime NOT NULL COMMENT '平台审核时间',
  `ManagerRemark` varchar(1000) DEFAULT NULL COMMENT '平台注释',
  `IsReturn` tinyint(1) NOT NULL COMMENT '是否已经退款',
  `ExpressCompanyName` varchar(100) DEFAULT NULL COMMENT '快递公司',
  `ShipOrderNumber` varchar(100) DEFAULT NULL COMMENT '快递单号',
  `Payee` varchar(200) DEFAULT NULL COMMENT '收款人',
  `PayeeAccount` varchar(200) DEFAULT NULL COMMENT '收款人账户',
  `RefundMode` int(11) NOT NULL COMMENT '退款方式',
  `RefundPayStatus` int(11) NOT NULL DEFAULT '2' COMMENT '退款支付状态',
  `RefundPayType` int(11) NOT NULL COMMENT '退款支付类型',
  `BuyerDeliverDate` datetime DEFAULT NULL COMMENT '买家发货时间',
  `SellerConfirmArrivalDate` datetime DEFAULT NULL COMMENT '卖家确认到货时间',
  `RefundBatchNo` varchar(30) DEFAULT NULL COMMENT '退款批次号',
  `RefundPostTime` datetime DEFAULT NULL COMMENT '退款异步提交时间',
  `ReturnQuantity` bigint(20) NOT NULL DEFAULT '0' COMMENT '退货数量',
  `ReturnPlatCommission` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台佣金退还',
  `ApplyNumber` int(11) NOT NULL COMMENT '申请次数',
  `CertPic1` varchar(200) DEFAULT NULL COMMENT '凭证图片1',
  `CertPic2` varchar(200) DEFAULT NULL COMMENT '凭证图片2',
  `CertPic3` varchar(200) DEFAULT NULL COMMENT '凭证图片3',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_OrderItem_OrderRefund` (`OrderItemId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_OrderRefundLog
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderRefundLog`;
CREATE TABLE `Himall_OrderRefundLog` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `RefundId` bigint(20) NOT NULL COMMENT '售后编号',
  `Operator` varchar(100) NOT NULL COMMENT '操作者',
  `OperateDate` datetime NOT NULL COMMENT '操作日期',
  `OperateContent` varchar(1000) DEFAULT NULL COMMENT '操作内容',
  `ApplyNumber` int(11) NOT NULL COMMENT '申请次数',
  `Step` smallint(6) NOT NULL COMMENT '退款步聚(枚举:CommonModel.Enum.OrderRefundStep)',
  `Remark` varchar(255) DEFAULT NULL COMMENT '备注(买家留言/商家留言/商家拒绝原因/平台退款备注)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='订单售后日志表';

-- ----------------------------
-- Table structure for Himall_OrderVerificationCode
-- ----------------------------
DROP TABLE IF EXISTS `Himall_OrderVerificationCode`;
CREATE TABLE `Himall_OrderVerificationCode` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `OrderItemId` bigint(20) NOT NULL COMMENT '订单项ID',
  `Status` tinyint(4) NOT NULL COMMENT '核销码状态(1=待核销，2=已核销，3=退款中，4=退款完成，5=已过期)',
  `VerificationCode` varchar(15) NOT NULL COMMENT '核销码(12位随机数)',
  `VerificationTime` datetime DEFAULT NULL COMMENT '核销时间',
  `VerificationUser` varchar(50) DEFAULT NULL COMMENT '核销人',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='订单核销码表';

-- ----------------------------
-- Table structure for Himall_PaymentConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PaymentConfig`;
CREATE TABLE `Himall_PaymentConfig` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `IsCashOnDelivery` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_PendingSettlementOrder
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PendingSettlementOrder`;
CREATE TABLE `Himall_PendingSettlementOrder` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '店铺名称',
  `OrderId` bigint(20) NOT NULL COMMENT '订单号',
  `OrderType` int(11) DEFAULT NULL COMMENT '订单类型',
  `OrderAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '订单金额',
  `ProductsAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '商品实付金额',
  `FreightAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '运费',
  `TaxAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '税费',
  `IntegralDiscount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '积分抵扣金额',
  `PlatCommission` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台佣金',
  `DistributorCommission` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '分销佣金',
  `RefundAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '退款金额',
  `RefundDate` datetime DEFAULT NULL COMMENT '退款时间',
  `PlatCommissionReturn` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台佣金退还',
  `DistributorCommissionReturn` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '分销佣金退还',
  `SettlementAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '结算金额',
  `OrderFinshTime` datetime DEFAULT NULL COMMENT '订单完成时间',
  `PaymentTypeName` varchar(100) DEFAULT NULL,
  `CreateDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `DiscountAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台优惠券抵扣金额',
  `DiscountAmountReturn` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '平台优惠券退还金额',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='待结算订单表';

-- ----------------------------
-- Table structure for Himall_PhoneIPCode
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PhoneIPCode`;
CREATE TABLE `Himall_PhoneIPCode` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `SendName` varchar(255) NOT NULL COMMENT '发送Ip或手机号',
  `SendTime` datetime NOT NULL COMMENT '发送时间',
  `SendCount` int(11) NOT NULL COMMENT '发送次数',
  `SendType` int(11) NOT NULL DEFAULT '1' COMMENT '发送类型：1为IP，2为手机号',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------
-- Table structure for Himall_PhotoSpace
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PhotoSpace`;
CREATE TABLE `Himall_PhotoSpace` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `PhotoCategoryId` bigint(20) NOT NULL COMMENT '图片分组ID',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `PhotoName` varchar(2000) DEFAULT NULL COMMENT '图片名称',
  `PhotoPath` varchar(2000) DEFAULT NULL COMMENT '图片路径',
  `FileSize` bigint(20) NOT NULL COMMENT '图片大小',
  `UploadTime` datetime NOT NULL COMMENT '图片上传时间',
  `LastUpdateTime` datetime NOT NULL COMMENT '图片最后更新时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_PhotoSpaceCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PhotoSpaceCategory`;
CREATE TABLE `Himall_PhotoSpaceCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `PhotoSpaceCatrgoryName` varchar(255) DEFAULT NULL COMMENT '图片空间分类名称',
  `DisplaySequence` bigint(20) NOT NULL COMMENT '显示顺序',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_PlatAccount
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PlatAccount`;
CREATE TABLE `Himall_PlatAccount` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `Balance` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '帐户余额',
  `PendingSettlement` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '待结算',
  `Settled` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '已结算',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='平台资金表';

-- ----------------------------
-- Table structure for Himall_PlatAccountItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PlatAccountItem`;
CREATE TABLE `Himall_PlatAccountItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `AccountNo` varchar(50) NOT NULL COMMENT '交易流水号',
  `AccoutID` bigint(20) NOT NULL COMMENT '关联资金编号',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `Amount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '金额',
  `Balance` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '帐户剩余',
  `TradeType` int(4) NOT NULL DEFAULT '0' COMMENT '交易类型',
  `IsIncome` bit(1) NOT NULL COMMENT '是否收入',
  `ReMark` varchar(1000) DEFAULT NULL COMMENT '交易备注',
  `DetailId` varchar(100) DEFAULT NULL COMMENT '详情ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_Pltem_AccoutID` (`AccoutID`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='平台资金流水表';

-- ----------------------------
-- Table structure for Himall_PlatVisit
-- ----------------------------
DROP TABLE IF EXISTS `Himall_PlatVisit`;
CREATE TABLE `Himall_PlatVisit` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `Date` datetime NOT NULL COMMENT '统计日期',
  `VisitCounts` bigint(20) NOT NULL COMMENT '平台浏览数',
  `OrderUserCount` bigint(20) NOT NULL COMMENT '下单人数',
  `OrderCount` bigint(20) NOT NULL COMMENT '订单数',
  `OrderProductCount` bigint(20) NOT NULL COMMENT '下单件数',
  `OrderAmount` decimal(18,2) NOT NULL COMMENT '下单金额',
  `OrderPayUserCount` bigint(20) NOT NULL COMMENT '下单付款人数',
  `OrderPayCount` bigint(20) NOT NULL COMMENT '付款订单数',
  `SaleCounts` bigint(20) NOT NULL COMMENT '付款下单件数',
  `SaleAmounts` decimal(18,2) NOT NULL COMMENT '付款金额',
  `StatisticFlag` bit(1) NOT NULL COMMENT '是否已经统计(0：未统计,1已统计)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Product
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Product`;
CREATE TABLE `Himall_Product` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `CategoryId` bigint(20) NOT NULL COMMENT '分类ID',
  `CategoryPath` varchar(100) NOT NULL COMMENT '分类路径',
  `ProductType` tinyint(4) NOT NULL COMMENT '商品类型(0=实物商品，1=虚拟商品)',
  `TypeId` bigint(20) NOT NULL COMMENT '类型ID',
  `BrandId` bigint(20) NOT NULL COMMENT '品牌ID',
  `ProductName` varchar(100) NOT NULL COMMENT '商品名称',
  `ProductCode` varchar(100) DEFAULT NULL COMMENT '商品编号',
  `ShortDescription` varchar(4000) DEFAULT NULL COMMENT '广告词',
  `SaleStatus` int(11) NOT NULL COMMENT '销售状态',
  `AuditStatus` int(11) NOT NULL COMMENT '审核状态',
  `AddedDate` datetime NOT NULL COMMENT '添加日期',
  `DisplaySequence` bigint(20) NOT NULL COMMENT '显示顺序',
  `ImagePath` varchar(100) DEFAULT NULL COMMENT '存放图片的目录',
  `MarketPrice` decimal(18,2) NOT NULL COMMENT '市场价',
  `MinSalePrice` decimal(18,2) NOT NULL COMMENT '最小销售价',
  `HasSKU` tinyint(1) NOT NULL COMMENT '是否有SKU',
  `VistiCounts` bigint(20) NOT NULL COMMENT '浏览次数',
  `SaleCounts` bigint(20) NOT NULL COMMENT '销售量',
  `FreightTemplateId` bigint(20) NOT NULL COMMENT '运费模板ID',
  `Weight` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '重量',
  `Volume` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '体积',
  `Quantity` int(11) NOT NULL DEFAULT '0' COMMENT '数量',
  `MeasureUnit` varchar(20) DEFAULT NULL COMMENT '计量单位',
  `EditStatus` int(11) NOT NULL DEFAULT '0' COMMENT '修改状态 0 正常 1已修改 2待审核 3 已修改并待审核',
  `IsDeleted` bit(1) NOT NULL COMMENT '是否已删除',
  `MaxBuyCount` int(11) NOT NULL COMMENT '最大购买数',
  `IsOpenLadder` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否开启阶梯价格',
  `ColorAlias` varchar(50) DEFAULT NULL COMMENT '颜色别名',
  `SizeAlias` varchar(50) DEFAULT NULL COMMENT '尺码别名',
  `VersionAlias` varchar(50) DEFAULT NULL COMMENT '版本别名',
  `ShopDisplaySequence` int(11) NOT NULL DEFAULT '0' COMMENT '商家商品序号',
  `VirtualSaleCounts` bigint(20) NOT NULL DEFAULT '0' COMMENT '虚拟销量',
  `VideoPath` varchar(200) DEFAULT NULL COMMENT '商品主图视频',
  `UpdateTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '最后商品修改时间',
  `CheckTime` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '商品审核时间',
  `IsPushGoods` bit(1) NOT NULL DEFAULT b'0' COMMENT '货品档案推送状态0代表未推送，1代表推送成功，2代表推送失败',
  `IsPushArchivesGoods` bit(1) NOT NULL DEFAULT b'0' COMMENT '平台货品推送状态0代表未推送，1代表推送成功，2代表推送失败',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_SHOPID` (`ShopId`) USING BTREE,
  KEY `FK_CategoryId` (`CategoryId`) USING BTREE,
  KEY `IX_SaleStatus` (`SaleStatus`) USING BTREE,
  KEY `IX_AuditStatus` (`AuditStatus`) USING BTREE,
  KEY `IX_ShopId` (`ShopId`) USING BTREE,
  KEY `IX_IsDeleted` (`IsDeleted`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductAttribute
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductAttribute`;
CREATE TABLE `Himall_ProductAttribute` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `AttributeId` bigint(20) NOT NULL COMMENT '属性ID',
  `ValueId` bigint(20) NOT NULL COMMENT '属性值ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Attribute_ProductAttribute` (`AttributeId`) USING BTREE,
  KEY `FK_Product_ProductAttribute` (`ProductId`) USING BTREE,
  KEY `IX_ValueId` (`ValueId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductComment
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductComment`;
CREATE TABLE `Himall_ProductComment` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `SubOrderId` bigint(20) NOT NULL COMMENT '订单详细ID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '店铺名称',
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `UserName` varchar(100) DEFAULT NULL COMMENT '用户名称',
  `Email` varchar(1000) DEFAULT NULL COMMENT 'Email',
  `ReviewContent` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '评价内容',
  `ReviewDate` datetime NOT NULL COMMENT '评价日期',
  `ReviewMark` int(11) NOT NULL COMMENT '评价说明',
  `ReplyContent` varchar(1000) DEFAULT NULL,
  `ReplyDate` datetime DEFAULT NULL,
  `AppendContent` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '追加内容',
  `AppendDate` datetime DEFAULT NULL COMMENT '追加时间',
  `ReplyAppendContent` varchar(1000) DEFAULT NULL COMMENT '追加评论回复',
  `ReplyAppendDate` datetime DEFAULT NULL COMMENT '追加评论回复时间',
  `IsHidden` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_ProductComment` (`ProductId`) USING BTREE,
  KEY `SubOrderId` (`SubOrderId`) USING BTREE,
  KEY `ShopId` (`ShopId`) USING BTREE,
  KEY `UserId` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductCommentImage
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductCommentImage`;
CREATE TABLE `Himall_ProductCommentImage` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增物理主键',
  `CommentImage` varchar(200) NOT NULL COMMENT '评论图片',
  `CommentId` bigint(20) NOT NULL COMMENT '评论ID',
  `CommentType` int(11) NOT NULL COMMENT '评论类型（首次评论/追加评论）',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FR_CommentImages` (`CommentId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductConsultation
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductConsultation`;
CREATE TABLE `Himall_ProductConsultation` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL,
  `ShopId` bigint(20) NOT NULL,
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '店铺名称',
  `UserId` bigint(20) NOT NULL,
  `UserName` varchar(100) DEFAULT NULL COMMENT '用户名称',
  `Email` varchar(1000) DEFAULT NULL,
  `ConsultationContent` varchar(1000) DEFAULT NULL COMMENT '咨询内容',
  `ConsultationDate` datetime NOT NULL COMMENT '咨询时间',
  `ReplyContent` varchar(1000) DEFAULT NULL COMMENT '回复内容',
  `ReplyDate` datetime DEFAULT NULL COMMENT '回复日期',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_ProductConsultation` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductDescription
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductDescription`;
CREATE TABLE `Himall_ProductDescription` (
  `Id` bigint(20) NOT NULL,
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `AuditReason` varchar(1000) DEFAULT NULL COMMENT '审核原因',
  `Description` text COMMENT '详情',
  `DescriptionPrefixId` bigint(20) NOT NULL COMMENT '关联版式',
  `DescriptiondSuffixId` bigint(20) NOT NULL,
  `Meta_Title` varchar(1000) DEFAULT NULL COMMENT 'SEO',
  `Meta_Description` varchar(1000) DEFAULT NULL,
  `Meta_Keywords` varchar(1000) DEFAULT NULL,
  `MobileDescription` text COMMENT '移动端描述',
  PRIMARY KEY (`ProductId`) USING BTREE,
  KEY `FK_Product_ProductDescription` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductDescriptionTemplate
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductDescriptionTemplate`;
CREATE TABLE `Himall_ProductDescriptionTemplate` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `Name` varchar(100) NOT NULL COMMENT '板式名称',
  `Position` int(11) NOT NULL COMMENT '位置（上、下）',
  `Content` text NOT NULL COMMENT 'PC端版式',
  `MobileContent` text COMMENT '移动端版式',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductLadderPrice
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductLadderPrice`;
CREATE TABLE `Himall_ProductLadderPrice` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '阶梯价格ID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `MinBath` int(11) NOT NULL COMMENT '最小批量',
  `MaxBath` int(11) NOT NULL COMMENT '最大批量',
  `Price` decimal(18,2) NOT NULL COMMENT '价格',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductRelationProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductRelationProduct`;
CREATE TABLE `Himall_ProductRelationProduct` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL COMMENT '商品id',
  `Relation` varchar(255) NOT NULL COMMENT '推荐的商品id列表，以‘，’分隔',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='推荐商品';

-- ----------------------------
-- Table structure for Himall_ProductShopCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductShopCategory`;
CREATE TABLE `Himall_ProductShopCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL,
  `ShopCategoryId` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_ProductShopCategory` (`ProductId`) USING BTREE,
  KEY `FK_ShopCategory_ProductShopCategory` (`ShopCategoryId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ProductVisti
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ProductVisti`;
CREATE TABLE `Himall_ProductVisti` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ProductId` bigint(20) NOT NULL,
  `Date` datetime NOT NULL,
  `VistiCounts` bigint(20) NOT NULL COMMENT '浏览次数',
  `VisitUserCounts` bigint(20) NOT NULL COMMENT '浏览人数',
  `PayUserCounts` bigint(20) NOT NULL COMMENT '付款人数',
  `SaleCounts` bigint(20) NOT NULL COMMENT '商品销售数量',
  `SaleAmounts` decimal(18,2) NOT NULL COMMENT '商品销售额',
  `OrderCounts` bigint(20) NOT NULL DEFAULT '0' COMMENT '订单总数',
  `StatisticFlag` bit(1) NOT NULL COMMENT '是否已经统计(0：未统计,1已统计)',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_ProductVisti` (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ReceivingAddressConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ReceivingAddressConfig`;
CREATE TABLE `Himall_ReceivingAddressConfig` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AddressId_City` text,
  `AddressId` text NOT NULL COMMENT '逗号分隔',
  `ShopId` bigint(20) NOT NULL COMMENT '预留字段，防止将来其他商家一并支持货到付款',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_RACShopId` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_RechargePresentRule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_RechargePresentRule`;
CREATE TABLE `Himall_RechargePresentRule` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ChargeAmount` decimal(18,2) NOT NULL COMMENT '充多少',
  `PresentAmount` decimal(18,2) NOT NULL COMMENT '送多少',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COMMENT='充值赠送规则';

-- ----------------------------
-- Table structure for Himall_RefundReason
-- ----------------------------
DROP TABLE IF EXISTS `Himall_RefundReason`;
CREATE TABLE `Himall_RefundReason` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AfterSalesText` varchar(100) DEFAULT NULL COMMENT '售后原因',
  `Sequence` int(11) NOT NULL DEFAULT '100' COMMENT '排序',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='售后原因';

-- ----------------------------
-- Table structure for Himall_Role
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Role`;
CREATE TABLE `Himall_Role` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `RoleName` varchar(100) NOT NULL COMMENT '角色名称',
  `Description` varchar(1000) NOT NULL COMMENT '说明',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_RolePrivilege
-- ----------------------------
DROP TABLE IF EXISTS `Himall_RolePrivilege`;
CREATE TABLE `Himall_RolePrivilege` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Privilege` int(11) NOT NULL COMMENT '权限ID',
  `RoleId` bigint(20) NOT NULL COMMENT '角色ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Role_RolePrivilege` (`RoleId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SearchProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SearchProduct`;
CREATE TABLE `Himall_SearchProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL COMMENT '商品Id',
  `ProductName` varchar(100) NOT NULL DEFAULT '' COMMENT '商品名称',
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '店铺Id',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT '' COMMENT '店铺名称',
  `BrandId` bigint(20) NOT NULL DEFAULT '0' COMMENT '品牌Id',
  `BrandName` varchar(100) DEFAULT '' COMMENT '品牌名称',
  `BrandLogo` varchar(1000) DEFAULT '' COMMENT '品牌Logo',
  `FirstCateId` bigint(20) NOT NULL DEFAULT '0' COMMENT '一级分类Id',
  `FirstCateName` varchar(100) NOT NULL DEFAULT '' COMMENT '一级分类名称',
  `SecondCateId` bigint(20) NOT NULL COMMENT '二级分类Id',
  `SecondCateName` varchar(100) NOT NULL DEFAULT '' COMMENT '二级分类名称',
  `ThirdCateId` bigint(20) NOT NULL COMMENT '三级分类Id',
  `ThirdCateName` varchar(100) NOT NULL DEFAULT '' COMMENT '三级分类名称',
  `AttrValues` text COMMENT '属性值Id用英文逗号分隔',
  `Comments` int(11) NOT NULL DEFAULT '0' COMMENT '评论数',
  `SaleCount` int(11) NOT NULL DEFAULT '0' COMMENT '成交量',
  `SalePrice` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '售价',
  `OnSaleTime` datetime NOT NULL COMMENT '上架时间',
  `ImagePath` varchar(100) NOT NULL DEFAULT '' COMMENT '商品图片地址',
  `CanSearch` bit(1) NOT NULL DEFAULT b'0' COMMENT '可以搜索',
  `ActivityId` int(11) NOT NULL DEFAULT '0',
  `ActiveType` int(11) NOT NULL DEFAULT '0',
  `ProductType` tinyint(4) NOT NULL DEFAULT '0' COMMENT '商品类型(0=实物商品，1=虚拟商品)',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IX_ProductId` (`ProductId`) USING BTREE,
  KEY `IX_ShopId` (`ShopId`) USING BTREE,
  KEY `IX_BrandId` (`BrandId`) USING BTREE,
  KEY `IX_FirstCateId` (`FirstCateId`) USING BTREE,
  KEY `IX_SecondCateId` (`SecondCateId`) USING BTREE,
  KEY `IX_ThirdCateId` (`ThirdCateId`) USING BTREE,
  KEY `IX_Comments` (`Comments`) USING BTREE,
  KEY `IX_SaleCount` (`SaleCount`) USING BTREE,
  KEY `IX_OnSaleTime` (`OnSaleTime`) USING BTREE,
  KEY `IX_CanSearch` (`CanSearch`) USING BTREE,
  KEY `IX_SalePrice` (`SalePrice`) USING BTREE,
  FULLTEXT KEY `ProductName` (`ProductName`) /*!50100 WITH PARSER `ngram` */ 
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SellerSpecificationValue
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SellerSpecificationValue`;
CREATE TABLE `Himall_SellerSpecificationValue` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ValueId` bigint(20) NOT NULL COMMENT '规格值ID',
  `Specification` int(11) NOT NULL COMMENT '规格（颜色、尺寸、版本）',
  `TypeId` bigint(20) NOT NULL COMMENT '类型ID',
  `Value` varchar(100) NOT NULL COMMENT '商家的规格值',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_SpecificationValue_SellerSpecificationValue` (`ValueId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SendMessageRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SendMessageRecord`;
CREATE TABLE `Himall_SendMessageRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MessageType` int(11) NOT NULL COMMENT '消息类别',
  `ContentType` int(11) NOT NULL COMMENT '内容类型',
  `SendContent` varchar(600) NOT NULL COMMENT '发送内容',
  `ToUserLabel` varchar(200) DEFAULT NULL COMMENT '发送对象',
  `SendState` int(11) NOT NULL COMMENT '发送状态',
  `SendTime` datetime NOT NULL COMMENT '发送时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SendmessagerecordCoupon
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SendmessagerecordCoupon`;
CREATE TABLE `Himall_SendmessagerecordCoupon` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MessageId` bigint(20) NOT NULL,
  `CouponId` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Reference_message` (`MessageId`) USING BTREE,
  KEY `FK_Reference_messageCoupon` (`CouponId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='发送优惠券详细';

-- ----------------------------
-- Table structure for Himall_SendmessagerecordCouponSN
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SendmessagerecordCouponSN`;
CREATE TABLE `Himall_SendmessagerecordCouponSN` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MessageId` bigint(20) NOT NULL,
  `CouponSN` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SensitiveWord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SensitiveWord`;
CREATE TABLE `Himall_SensitiveWord` (
  `Id` int(4) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `SensitiveWord` varchar(100) DEFAULT NULL COMMENT '敏感词',
  `CategoryName` varchar(100) DEFAULT NULL COMMENT '敏感词类别',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `Id` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Settled
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Settled`;
CREATE TABLE `Himall_Settled` (
  `ID` bigint(20) NOT NULL AUTO_INCREMENT,
  `BusinessType` int(11) NOT NULL COMMENT '商家类型 0、仅企业可入驻；1、仅个人可入驻；2、企业和个人均可',
  `SettlementAccountType` int(11) NOT NULL COMMENT '商家结算类型 0、仅银行账户；1、仅微信账户；2、银行账户及微信账户均可',
  `TrialDays` int(11) NOT NULL COMMENT '试用天数',
  `IsCity` int(11) NOT NULL COMMENT '地址必填 0、非必填；1、必填',
  `IsPeopleNumber` int(11) NOT NULL COMMENT '人数必填 0、非必填；1、必填',
  `IsAddress` int(11) NOT NULL COMMENT '详细地址必填 0、非必填；1、必填',
  `IsBusinessLicenseCode` int(11) NOT NULL COMMENT '营业执照号必填 0、非必填；1、必填',
  `IsBusinessScope` int(11) NOT NULL COMMENT '经营范围必填 0、非必填；1、必填',
  `IsBusinessLicense` int(11) NOT NULL COMMENT '营业执照必填 0、非必填；1、必填',
  `IsAgencyCode` int(11) NOT NULL COMMENT '机构代码必填 0、非必填；1、必填',
  `IsAgencyCodeLicense` int(11) NOT NULL COMMENT '机构代码证必填 0、非必填；1、必填',
  `IsTaxpayerToProve` int(11) NOT NULL COMMENT '纳税人证明必填 0、非必填；1、必填',
  `CompanyVerificationType` int(11) NOT NULL COMMENT '验证类型 0、验证手机；1、验证邮箱；2、均需验证',
  `IsSName` int(11) NOT NULL COMMENT '个人姓名必填 0、非必填；1、必填',
  `IsSCity` int(11) NOT NULL COMMENT '个人地址必填 0、非必填；1、必填',
  `IsSAddress` int(11) NOT NULL COMMENT '个人详细地址必填 0、非必填；1、必填',
  `IsSIDCard` int(11) NOT NULL COMMENT '个人身份证必填 0、非必填；1、必填',
  `IsSIdCardUrl` int(11) NOT NULL COMMENT '个人身份证上传 0、非必填；1、必填',
  `SelfVerificationType` int(11) NOT NULL COMMENT '个人验证类型 0、验证手机；1、验证邮箱；2、均需验证',
  PRIMARY KEY (`ID`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='入驻设置';

-- ----------------------------
-- Table structure for Himall_ShippingAddress
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShippingAddress`;
CREATE TABLE `Himall_ShippingAddress` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `RegionId` int(11) NOT NULL COMMENT '区域ID',
  `ShipTo` varchar(100) NOT NULL COMMENT '收货人',
  `Address` varchar(100) NOT NULL COMMENT '收货具体街道信息',
  `AddressDetail` varchar(100) DEFAULT NULL COMMENT '地址详情(楼栋-门牌)',
  `Phone` varchar(100) NOT NULL COMMENT '收货人电话',
  `IsDefault` tinyint(1) NOT NULL COMMENT '是否为默认',
  `IsQuick` tinyint(1) NOT NULL COMMENT '是否为轻松购地址',
  `Longitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '经度',
  `Latitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '纬度',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Member_ShippingAddress` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShippingFreeGroup
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShippingFreeGroup`;
CREATE TABLE `Himall_ShippingFreeGroup` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TemplateId` bigint(20) NOT NULL COMMENT '运费模版ID',
  `ConditionType` int(11) NOT NULL COMMENT '包邮条件类型',
  `ConditionNumber` varchar(100) NOT NULL COMMENT '包邮条件值',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShippingFreeRegion
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShippingFreeRegion`;
CREATE TABLE `Himall_ShippingFreeRegion` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TemplateId` bigint(20) NOT NULL,
  `GroupId` bigint(20) NOT NULL,
  `RegionId` int(11) NOT NULL,
  `RegionPath` varchar(50) DEFAULT NULL COMMENT '地区全路径',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Shop
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Shop`;
CREATE TABLE `Himall_Shop` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `GradeId` bigint(20) NOT NULL COMMENT '店铺等级',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `Logo` varchar(100) DEFAULT NULL COMMENT '店铺LOGO路径',
  `SubDomains` varchar(100) DEFAULT NULL COMMENT '预留子域名，未使用',
  `Theme` varchar(100) DEFAULT NULL COMMENT '预留主题，未使用',
  `IsSelf` tinyint(1) NOT NULL COMMENT '是否是官方自营店',
  `ShopStatus` int(11) NOT NULL COMMENT '店铺状态',
  `RefuseReason` varchar(1000) DEFAULT NULL COMMENT '审核拒绝原因',
  `CreateDate` datetime NOT NULL COMMENT '店铺创建日期',
  `EndDate` datetime NOT NULL COMMENT '店铺过期日期',
  `CompanyName` varchar(100) DEFAULT NULL COMMENT '公司名称',
  `CompanyRegionId` int(11) NOT NULL COMMENT '公司省市区',
  `CompanyAddress` varchar(100) DEFAULT NULL COMMENT '公司地址',
  `CompanyPhone` varchar(100) DEFAULT NULL COMMENT '公司电话',
  `CompanyEmployeeCount` int(11) NOT NULL COMMENT '公司员工数量',
  `CompanyRegisteredCapital` decimal(18,2) NOT NULL COMMENT '公司注册资金',
  `ContactsName` varchar(100) DEFAULT NULL COMMENT '联系人姓名',
  `ContactsPhone` varchar(100) DEFAULT NULL COMMENT '联系电话',
  `ContactsEmail` varchar(100) DEFAULT NULL COMMENT '联系Email',
  `OrderPayIsSendSMS` tinyint(2) NOT NULL DEFAULT '0' COMMENT '是否开启支付订单短信通知商家 0否 1是',
  `BusinessLicenceNumber` varchar(100) DEFAULT NULL COMMENT '营业执照号',
  `BusinessLicenceNumberPhoto` varchar(100) NOT NULL COMMENT '营业执照',
  `BusinessLicenceRegionId` int(11) NOT NULL COMMENT '营业执照所在地',
  `BusinessLicenceStart` datetime DEFAULT NULL COMMENT '营业执照有效期开始',
  `BusinessLicenceEnd` datetime DEFAULT NULL COMMENT '营业执照有效期',
  `BusinessSphere` varchar(500) DEFAULT NULL COMMENT '法定经营范围',
  `OrganizationCode` varchar(100) DEFAULT NULL COMMENT '组织机构代码',
  `OrganizationCodePhoto` varchar(100) DEFAULT NULL COMMENT '组织机构执照',
  `GeneralTaxpayerPhot` varchar(100) DEFAULT NULL COMMENT '一般纳税人证明',
  `BankAccountName` varchar(100) DEFAULT NULL COMMENT '银行开户名',
  `BankAccountNumber` varchar(100) DEFAULT NULL COMMENT '公司银行账号',
  `BankName` varchar(100) DEFAULT NULL COMMENT '开户银行支行名称',
  `BankCode` varchar(100) DEFAULT NULL COMMENT '支行联行号',
  `BankRegionId` int(11) NOT NULL COMMENT '开户银行所在地',
  `BankPhoto` varchar(100) DEFAULT NULL,
  `TaxRegistrationCertificate` varchar(100) DEFAULT NULL COMMENT '税务登记证',
  `TaxpayerId` varchar(100) DEFAULT NULL COMMENT '税务登记证号',
  `TaxRegistrationCertificatePhoto` varchar(100) DEFAULT NULL COMMENT '纳税人识别号',
  `PayPhoto` varchar(100) DEFAULT NULL COMMENT '支付凭证',
  `PayRemark` varchar(1000) DEFAULT NULL COMMENT '支付注释',
  `SenderName` varchar(100) DEFAULT NULL COMMENT '商家发货人名称',
  `SenderAddress` varchar(100) DEFAULT NULL COMMENT '商家发货人地址',
  `SenderPhone` varchar(100) DEFAULT NULL COMMENT '商家发货人电话',
  `Freight` decimal(18,2) NOT NULL COMMENT '运费',
  `FreeFreight` decimal(18,2) NOT NULL COMMENT '多少钱开始免运费',
  `Stage` int(11) NOT NULL DEFAULT '0' COMMENT '注册步骤',
  `SenderRegionId` int(11) NOT NULL DEFAULT '0' COMMENT '商家发货人省市区',
  `BusinessLicenseCert` varchar(120) DEFAULT NULL COMMENT '营业执照证书',
  `ProductCert` varchar(120) DEFAULT NULL COMMENT '商品证书',
  `OtherCert` varchar(120) DEFAULT NULL COMMENT '其他证书',
  `legalPerson` varchar(50) DEFAULT NULL COMMENT '法人代表',
  `CompanyFoundingDate` datetime DEFAULT NULL COMMENT '公司成立日期',
  `BusinessType` int(11) NOT NULL DEFAULT '0' COMMENT '0、企业；1、个人',
  `IDCard` varchar(50) DEFAULT '' COMMENT '身份证号',
  `IDCardUrl` varchar(200) DEFAULT '' COMMENT '身份证URL',
  `IDCardUrl2` varchar(200) DEFAULT NULL COMMENT '身份证照片URL2',
  `WeiXinNickName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '微信昵称',
  `WeiXinSex` int(11) DEFAULT '0' COMMENT '微信性别;0、男；1、女',
  `WeiXinAddress` varchar(200) DEFAULT '' COMMENT '微信地区',
  `WeiXinTrueName` varchar(200) DEFAULT '' COMMENT '微信真实姓名',
  `WeiXinOpenId` varchar(200) DEFAULT '' COMMENT '微信标识符',
  `WeiXinImg` varchar(200) DEFAULT NULL,
  `AutoAllotOrder` tinyint(1) NOT NULL COMMENT '商家是否开启自动分配订单',
  `IsAutoPrint` bit(1) NOT NULL DEFAULT b'0' COMMENT '商家是否开启自动打印',
  `PrintCount` int(11) NOT NULL DEFAULT '0' COMMENT '打印张数',
  `IsOpenTopImageAd` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否开启头部图片广告',
  `IsOpenHiChat` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否注册了海商客服平台',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ShopIsSelf` (`IsSelf`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopAccount
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopAccount`;
CREATE TABLE `Himall_ShopAccount` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺Id',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '店铺名称',
  `Balance` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '帐户余额',
  `PendingSettlement` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '待结算',
  `Settled` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '已结算',
  `ReMark` varchar(500) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='店铺资金表';

-- ----------------------------
-- Table structure for Himall_ShopAccountItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopAccountItem`;
CREATE TABLE `Himall_ShopAccountItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci  NOT NULL COMMENT '店铺名称',
  `AccountNo` varchar(50) NOT NULL COMMENT '交易流水号',
  `AccoutID` bigint(20) NOT NULL COMMENT '关联资金编号',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `Amount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '金额',
  `Balance` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '帐户剩余',
  `TradeType` int(4) NOT NULL DEFAULT '0' COMMENT '交易类型',
  `IsIncome` bit(1) NOT NULL COMMENT '是否收入',
  `ReMark` varchar(1000) DEFAULT NULL COMMENT '交易备注',
  `DetailId` varchar(100) DEFAULT NULL COMMENT '详情ID',
  `SettlementCycle` int(11) NOT NULL COMMENT '结算周期(以天为单位)(冗余字段)',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_Shtem_AccoutID` (`AccoutID`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='店铺资金流水表';

-- ----------------------------
-- Table structure for Himall_ShopBonus
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBonus`;
CREATE TABLE `Himall_ShopBonus` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `Name` varchar(40) NOT NULL,
  `Count` int(11) NOT NULL COMMENT '红包数量',
  `RandomAmountStart` decimal(18,2) NOT NULL COMMENT '随机范围Start',
  `RandomAmountEnd` decimal(18,2) NOT NULL COMMENT '随机范围End',
  `UseState` int(11) NOT NULL COMMENT '1:满X元使用  2：没有限制',
  `UsrStatePrice` decimal(18,2) NOT NULL COMMENT '满多少元',
  `GrantPrice` decimal(18,2) NOT NULL COMMENT '满多少元才发放红包',
  `DateStart` datetime NOT NULL,
  `DateEnd` datetime NOT NULL,
  `BonusDateStart` datetime NOT NULL,
  `BonusDateEnd` datetime NOT NULL,
  `ShareTitle` varchar(30) NOT NULL COMMENT '分享',
  `ShareDetail` varchar(150) NOT NULL COMMENT '分享',
  `ShareImg` varchar(200) NOT NULL COMMENT '分享',
  `SynchronizeCard` tinyint(1) NOT NULL COMMENT '是否同步到微信卡包，是的话才出现微信卡卷相关UI',
  `CardTitle` varchar(30) DEFAULT NULL COMMENT '微信卡卷相关',
  `CardColor` varchar(20) DEFAULT NULL COMMENT '微信卡卷相关',
  `CardSubtitle` varchar(30) DEFAULT NULL COMMENT '微信卡卷相关',
  `IsInvalid` tinyint(1) NOT NULL COMMENT '是否失效',
  `ReceiveCount` int(11) NOT NULL COMMENT '领取数量',
  `QRPath` varchar(80) NOT NULL COMMENT '二维码路径',
  `WXCardState` int(255) NOT NULL COMMENT '微信卡卷审核状态',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_zzzShopId` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopBonusGrant
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBonusGrant`;
CREATE TABLE `Himall_ShopBonusGrant` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopBonusId` bigint(20) NOT NULL COMMENT '红包Id',
  `UserId` bigint(20) NOT NULL COMMENT '发放人',
  `OrderId` bigint(20) NOT NULL,
  `BonusQR` varchar(255) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_ShopBonusId` (`ShopBonusId`) USING BTREE,
  KEY `FK_zzzUserID` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopBonusReceive
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBonusReceive`;
CREATE TABLE `Himall_ShopBonusReceive` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `BonusGrantId` bigint(20) NOT NULL COMMENT '红包Id',
  `OpenId` varchar(100) DEFAULT NULL,
  `Price` decimal(18,2) NOT NULL COMMENT '面额',
  `State` int(11) NOT NULL COMMENT '1.未使用  2.已使用  3.已过期',
  `ReceiveTime` datetime NOT NULL COMMENT '领取时间',
  `UsedTime` datetime DEFAULT NULL COMMENT '使用时间',
  `UserId` bigint(20) NOT NULL DEFAULT '0' COMMENT 'UserID',
  `UsedOrderId` bigint(20) DEFAULT NULL COMMENT '使用的订单号',
  `WXName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `WXHead` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_BonusGrantId` (`BonusGrantId`) USING BTREE,
  KEY `FK_useUserID` (`UserId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopBranch
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBranch`;
CREATE TABLE `Himall_ShopBranch` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `ShopId` bigint(20) NOT NULL COMMENT '商家店铺ID',
  `ShopBranchName` varchar(30) NOT NULL COMMENT '门店名称',
  `AddressId` int(11) NOT NULL COMMENT '门店地址ID',
  `AddressPath` varchar(50) DEFAULT NULL COMMENT '所在区域全路径编号(省，市，区)',
  `AddressDetail` varchar(100) DEFAULT NULL,
  `ContactUser` varchar(50) NOT NULL COMMENT '联系人',
  `ContactPhone` varchar(50) NOT NULL COMMENT '联系地址',
  `Status` int(11) NOT NULL COMMENT '门店状态(0:正常，1:冻结)',
  `CreateDate` datetime NOT NULL COMMENT '创建时间',
  `ServeRadius` int(11) NOT NULL DEFAULT '0' COMMENT '服务半径',
  `Longitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '经度',
  `Latitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '维度',
  `ShopImages` varchar(500) DEFAULT NULL,
  `IsStoreDelive` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否门店配送0:否1:是',
  `IsAboveSelf` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否上门自提0:否1:是',
  `StoreOpenStartTime` time NOT NULL DEFAULT '08:00:00' COMMENT '营业起始时间',
  `StoreOpenEndTime` time NOT NULL DEFAULT '20:00:00' COMMENT '营业结束时间',
  `IsRecommend` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否推荐门店',
  `RecommendSequence` bigint(20) NOT NULL DEFAULT '0' COMMENT '推荐排序',
  `DeliveFee` int(11) NOT NULL DEFAULT '0' COMMENT '配送费',
  `DeliveTotalFee` int(11) NOT NULL DEFAULT '0' COMMENT '起送费',
  `FreeMailFee` int(11) NOT NULL DEFAULT '0' COMMENT '包邮金额',
  `DaDaShopId` varchar(100) DEFAULT NULL,
  `IsAutoPrint` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否开启自动打印',
  `PrintCount` int(11) NOT NULL DEFAULT '0' COMMENT '打印张数',
  `IsFreeMail` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否包邮',
  `EnableSellerManager` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否允许商城越权',
  `IsShelvesProduct` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否自动上架商品(0:否、1:是)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='门店信息表';

-- ----------------------------
-- Table structure for Himall_ShopBranchInTag
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBranchInTag`;
CREATE TABLE `Himall_ShopBranchInTag` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopBranchId` bigint(20) NOT NULL COMMENT '门店管理ID',
  `ShopBranchTagId` bigint(20) NOT NULL COMMENT '门店标签关联ID',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_ShopBranchId` (`ShopBranchId`) USING BTREE,
  KEY `FK_ShopBranchTagId` (`ShopBranchTagId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopBranchManager
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBranchManager`;
CREATE TABLE `Himall_ShopBranchManager` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopBranchId` bigint(20) NOT NULL COMMENT '门店表ID',
  `UserName` varchar(100) NOT NULL COMMENT '用户名称',
  `Password` varchar(100) NOT NULL COMMENT '密码',
  `PasswordSalt` varchar(100) NOT NULL COMMENT '密码加盐',
  `CreateDate` datetime NOT NULL COMMENT '创建日期',
  `Remark` varchar(1000) DEFAULT NULL,
  `RealName` varchar(1000) DEFAULT NULL COMMENT '真实名称',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='门店管理员表';

-- ----------------------------
-- Table structure for Himall_ShopBranchSku
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBranchSku`;
CREATE TABLE `Himall_ShopBranchSku` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL COMMENT '商品id(冗余字段)',
  `SkuId` varchar(100) NOT NULL COMMENT 'SKU表Id',
  `ShopId` bigint(20) NOT NULL COMMENT '商家id(冗余字段)',
  `ShopBranchId` bigint(20) NOT NULL COMMENT '门店id',
  `Stock` int(11) NOT NULL COMMENT '库存',
  `Status` int(11) NOT NULL COMMENT '门店SKU状态',
  `CreateDate` datetime NOT NULL COMMENT 'SKU添加时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='商家分店SKU信息';

-- ----------------------------
-- Table structure for Himall_ShopBranchTag
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBranchTag`;
CREATE TABLE `Himall_ShopBranchTag` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '门店标签ID',
  `Title` varchar(30) DEFAULT NULL COMMENT '标签名称',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopBrand
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBrand`;
CREATE TABLE `Himall_ShopBrand` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '商家Id',
  `BrandId` bigint(20) NOT NULL COMMENT '品牌Id',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `ShopId` (`ShopId`) USING BTREE,
  KEY `BrandId` (`BrandId`) USING BTREE,
  KEY `Id` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopBrandApply
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopBrandApply`;
CREATE TABLE `Himall_ShopBrandApply` (
  `Id` int(11) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '商家Id',
  `BrandId` bigint(20) NOT NULL COMMENT '品牌Id',
  `BrandName` varchar(100) DEFAULT NULL COMMENT '品牌名称',
  `Logo` varchar(1000) DEFAULT NULL COMMENT '品牌Logo',
  `Description` varchar(1000) DEFAULT NULL COMMENT '描述',
  `AuthCertificate` varchar(4000) DEFAULT NULL COMMENT '品牌授权证书',
  `ApplyMode` int(11) NOT NULL COMMENT '申请类型 枚举 BrandApplyMode',
  `Remark` varchar(1000) DEFAULT NULL COMMENT '备注',
  `AuditStatus` int(11) NOT NULL COMMENT '审核状态 枚举 BrandAuditStatus',
  `ApplyTime` datetime NOT NULL COMMENT '操作时间',
  `PlatRemark` varchar(255) DEFAULT NULL COMMENT '平台备注',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_ShopId` (`ShopId`) USING BTREE,
  KEY `FK_BrandId` (`BrandId`) USING BTREE,
  KEY `Id` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopCategory
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopCategory`;
CREATE TABLE `Himall_ShopCategory` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ParentCategoryId` bigint(20) NOT NULL COMMENT '上级分类ID',
  `Name` varchar(100) DEFAULT NULL COMMENT '分类名称',
  `DisplaySequence` bigint(20) NOT NULL,
  `IsShow` tinyint(1) NOT NULL COMMENT '是否显示',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopFooter
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopFooter`;
CREATE TABLE `Himall_ShopFooter` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `Footer` varchar(5000) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopGrade
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopGrade`;
CREATE TABLE `Himall_ShopGrade` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL COMMENT '店铺等级名称',
  `ProductLimit` int(11) NOT NULL COMMENT '最大上传商品数量',
  `ImageLimit` int(11) NOT NULL COMMENT '最大图片可使用空间数量',
  `TemplateLimit` int(11) NOT NULL,
  `ChargeStandard` decimal(8,2) NOT NULL,
  `Remark` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopHomeModule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopHomeModule`;
CREATE TABLE `Himall_ShopHomeModule` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `Name` varchar(20) NOT NULL COMMENT '模块名称',
  `IsEnable` tinyint(1) NOT NULL COMMENT '是否启用',
  `DisplaySequence` int(11) NOT NULL COMMENT '排序',
  `Url` varchar(200) DEFAULT NULL COMMENT '楼层链接',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopHomeModuleProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopHomeModuleProduct`;
CREATE TABLE `Himall_ShopHomeModuleProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `HomeModuleId` bigint(20) NOT NULL,
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `DisplaySequence` int(11) NOT NULL COMMENT '排序',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Product_ShopHomeModuleProduct` (`ProductId`) USING BTREE,
  KEY `FK_ShopHomeModule_ShopHomeModuleProduct` (`HomeModuleId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopHomeModuleTopImg
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopHomeModuleTopImg`;
CREATE TABLE `Himall_ShopHomeModuleTopImg` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ImgPath` varchar(200) NOT NULL,
  `Url` varchar(200) DEFAULT NULL,
  `HomeModuleId` bigint(20) NOT NULL,
  `DisplaySequence` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_SFTHomeModuleId` (`HomeModuleId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopInvoiceConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopInvoiceConfig`;
CREATE TABLE `Himall_ShopInvoiceConfig` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '商家ID',
  `IsInvoice` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否提供发票',
  `IsPlainInvoice` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否提供普通发票',
  `IsElectronicInvoice` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否提供电子发票',
  `PlainInvoiceRate` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '普通发票税率',
  `IsVatInvoice` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否提供增值税发票',
  `VatInvoiceDay` int(11) NOT NULL DEFAULT '0' COMMENT '订单完成后多少天开具增值税发票',
  `VatInvoiceRate` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '增值税税率',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopOpenApiSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopOpenApiSetting`;
CREATE TABLE `Himall_ShopOpenApiSetting` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺编号',
  `AppKey` varchar(100) NOT NULL COMMENT 'app_key',
  `AppSecreat` varchar(100) NOT NULL COMMENT 'app_secreat',
  `AddDate` datetime NOT NULL COMMENT '增加时间',
  `LastEditDate` datetime NOT NULL COMMENT '最后重置时间',
  `IsEnable` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否开启',
  `IsRegistered` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否已注册',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShoppingCart
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShoppingCart`;
CREATE TABLE `Himall_ShoppingCart` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL COMMENT '用户ID',
  `ShopBranchId` bigint(20) NOT NULL DEFAULT '0' COMMENT '门店ID',
  `RoomId` bigint(20) NOT NULL DEFAULT '0' COMMENT '直播间ID',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `SkuId` varchar(100) DEFAULT NULL COMMENT 'SKUID',
  `Quantity` bigint(20) NOT NULL COMMENT '购买数量',
  `AddTime` datetime NOT NULL COMMENT '添加时间',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Member_ShoppingCart` (`UserId`) USING BTREE,
  KEY `FK_Product_ShoppingCart` (`ProductId`) USING BTREE,
  KEY `himall_shoppingcarts_ibfk_3` (`ShopBranchId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopRenewRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopRenewRecord`;
CREATE TABLE `Himall_ShopRenewRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `Operator` varchar(100) NOT NULL COMMENT '操作者',
  `OperateDate` datetime NOT NULL COMMENT '操作日期',
  `OperateContent` varchar(1000) DEFAULT NULL COMMENT '操作明细',
  `OperateType` int(1) NOT NULL COMMENT '类型',
  `Amount` decimal(10,2) NOT NULL COMMENT '支付金额',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='店铺续费记录表';

-- ----------------------------
-- Table structure for Himall_ShopShipper
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopShipper`;
CREATE TABLE `Himall_ShopShipper` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '商家编号',
  `IsDefaultSendGoods` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否为默认发货地址',
  `IsDefaultGetGoods` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否默认收货地址',
  `IsDefaultVerification` tinyint(1) DEFAULT '0' COMMENT '默认核销地址',
  `ShipperTag` varchar(100) NOT NULL DEFAULT '' COMMENT '发货点名称',
  `ShipperName` varchar(100) NOT NULL DEFAULT '' COMMENT '发货人',
  `RegionId` int(11) NOT NULL DEFAULT '0' COMMENT '区域ID',
  `Address` varchar(300) NOT NULL DEFAULT '' COMMENT '具体街道信息',
  `TelPhone` varchar(20) DEFAULT '' COMMENT '手机号码',
  `Zipcode` varchar(20) DEFAULT '',
  `WxOpenId` varchar(128) DEFAULT '' COMMENT '微信OpenID用于发信息到微信给发货人',
  `Longitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '经度',
  `Latitude` decimal(18,6) NOT NULL DEFAULT '0.000000' COMMENT '纬度',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopVisti
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopVisti`;
CREATE TABLE `Himall_ShopVisti` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `ShopBranchId` bigint(20) DEFAULT '0' COMMENT '门店编号',
  `Date` datetime NOT NULL COMMENT '日期',
  `VistiCounts` bigint(20) NOT NULL COMMENT '浏览人数',
  `OrderUserCount` bigint(20) NOT NULL COMMENT '下单人数',
  `OrderCount` bigint(20) NOT NULL COMMENT '订单数',
  `OrderProductCount` bigint(20) NOT NULL COMMENT '下单件数',
  `OrderAmount` decimal(18,2) NOT NULL COMMENT '下单金额',
  `OrderPayUserCount` bigint(20) NOT NULL COMMENT '下单付款人数',
  `OrderPayCount` bigint(20) NOT NULL COMMENT '付款订单数',
  `SaleCounts` bigint(20) NOT NULL COMMENT '付款下单件数',
  `SaleAmounts` decimal(18,2) NOT NULL COMMENT '付款金额',
  `OrderRefundProductCount` bigint(20) NOT NULL DEFAULT '0' COMMENT '退款件数',
  `OrderRefundAmount` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '退款金额',
  `OrderRefundCount` bigint(20) NOT NULL DEFAULT '0' COMMENT '退款订单数',
  `UnitPrice` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '件单价',
  `JointRate` decimal(18,2) NOT NULL DEFAULT '0.00' COMMENT '连带率',
  `StatisticFlag` bit(1) NOT NULL COMMENT '是否已经统计(0：未统计,1已统计)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopWdgjSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopWdgjSetting`;
CREATE TABLE `Himall_ShopWdgjSetting` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '主键',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺Id',
  `uCode` varchar(255) NOT NULL COMMENT '接入码',
  `uSign` varchar(255) NOT NULL COMMENT '效验码',
  `IsEnable` tinyint(1) NOT NULL DEFAULT '0' COMMENT '是否开启',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_ShopWithDraw
-- ----------------------------
DROP TABLE IF EXISTS `Himall_ShopWithDraw`;
CREATE TABLE `Himall_ShopWithDraw` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `CashNo` varchar(100) NOT NULL COMMENT '提现流水号',
  `ApplyTime` datetime NOT NULL COMMENT '申请时间',
  `Status` int(11) NOT NULL COMMENT '提现状态',
  `CashType` int(11) NOT NULL COMMENT '提现方式',
  `CashAmount` decimal(18,2) NOT NULL COMMENT '提现金额',
  `Account` varchar(100) NOT NULL COMMENT '提现帐号',
  `AccountName` varchar(100) NOT NULL COMMENT '提现人',
  `SellerId` bigint(20) NOT NULL,
  `SellerName` varchar(100) NOT NULL COMMENT '申请商家用户名',
  `DealTime` datetime DEFAULT NULL COMMENT '处理时间',
  `ShopRemark` varchar(1000) DEFAULT NULL COMMENT '商家备注',
  `PlatRemark` varchar(1000) DEFAULT NULL COMMENT '平台备注',
  `ShopName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT '' COMMENT '商店名称',
  `SerialNo` varchar(200) DEFAULT '' COMMENT '支付商流水号',
  `BankName` varchar(200) DEFAULT NULL COMMENT '银行支行名称,当为银行类型才不允许为空',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='店铺提现表';

-- ----------------------------
-- Table structure for Himall_SiteSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SiteSetting`;
CREATE TABLE `Himall_SiteSetting` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) NOT NULL,
  `Value` varchar(4000) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SiteSignInConfig
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SiteSignInConfig`;
CREATE TABLE `Himall_SiteSignInConfig` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `IsEnable` tinyint(1) NOT NULL COMMENT '开启签到',
  `DayIntegral` int(11) NOT NULL DEFAULT '0' COMMENT '签到获得积分',
  `DurationCycle` int(11) NOT NULL DEFAULT '0' COMMENT '持续周期',
  `DurationReward` int(11) NOT NULL DEFAULT '0' COMMENT '周期额外奖励积分',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SKU
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SKU`;
CREATE TABLE `Himall_SKU` (
  `Id` varchar(100) NOT NULL COMMENT '商品ID_颜色规格ID_颜色规格ID_尺寸规格',
  `AutoId` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '自增主键Id',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `Color` varchar(100) DEFAULT NULL COMMENT '颜色规格',
  `Size` varchar(100) DEFAULT NULL COMMENT '尺寸规格',
  `Version` varchar(100) DEFAULT NULL COMMENT '版本规格',
  `Sku` varchar(100) DEFAULT NULL COMMENT 'SKU',
  `Stock` bigint(20) NOT NULL COMMENT '库存',
  `CostPrice` decimal(18,2) NOT NULL COMMENT '成本价',
  `SalePrice` decimal(18,2) NOT NULL COMMENT '销售价',
  `ShowPic` varchar(200) DEFAULT NULL COMMENT '显示图片',
  `SafeStock` bigint(20) NOT NULL DEFAULT '0' COMMENT '警戒库存',
  `PushWdtState` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否旺店通推送',
  PRIMARY KEY (`AutoId`) USING BTREE,
  KEY `FK_Product_Sku` (`ProductId`) USING BTREE,
  KEY `AutoId` (`AutoId`) USING BTREE,
  KEY `Id` (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SlideAd
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SlideAd`;
CREATE TABLE `Himall_SlideAd` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID，0：平台轮播图  ',
  `ImageUrl` varchar(100) NOT NULL COMMENT '图片保存URL',
  `Url` varchar(1000) NOT NULL COMMENT '图片跳转URL',
  `DisplaySequence` bigint(20) NOT NULL,
  `TypeId` int(11) NOT NULL DEFAULT '0',
  `Description` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_SpecificationValue
-- ----------------------------
DROP TABLE IF EXISTS `Himall_SpecificationValue`;
CREATE TABLE `Himall_SpecificationValue` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Specification` int(11) NOT NULL COMMENT '规格名',
  `TypeId` bigint(20) NOT NULL COMMENT '类型ID',
  `Value` varchar(100) NOT NULL COMMENT '规格值',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Type_SpecificationValue` (`TypeId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_StatisticOrderComment
-- ----------------------------
DROP TABLE IF EXISTS `Himall_StatisticOrderComment`;
CREATE TABLE `Himall_StatisticOrderComment` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL,
  `CommentKey` int(11) NOT NULL COMMENT '评价的枚举（宝贝与描述相符 商家得分）',
  `CommentValue` decimal(10,4) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `himall_statisticordercomments_ibfk_1` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_TemplateVisualizationSetting
-- ----------------------------
DROP TABLE IF EXISTS `Himall_TemplateVisualizationSetting`;
CREATE TABLE `Himall_TemplateVisualizationSetting` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `CurrentTemplateName` varchar(2000) NOT NULL COMMENT '当前使用的模板的名称',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺Id（平台为0）',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Theme
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Theme`;
CREATE TABLE `Himall_Theme` (
  `ThemeId` bigint(20) NOT NULL AUTO_INCREMENT,
  `TypeId` int(11) NOT NULL COMMENT '0、默认主题；1、自定义主题',
  `MainColor` varchar(50) DEFAULT NULL COMMENT '商城主色',
  `SecondaryColor` varchar(50) DEFAULT NULL COMMENT '商城辅色',
  `WritingColor` varchar(50) DEFAULT NULL COMMENT '字体颜色',
  `FrameColor` varchar(50) DEFAULT NULL COMMENT '边框颜色',
  `ClassifiedsColor` varchar(50) DEFAULT NULL COMMENT '商品分类栏',
  `IsUse` bit(1) NOT NULL DEFAULT b'0' COMMENT '是否将主题配色应用至商城',
  PRIMARY KEY (`ThemeId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='主题设置表';

-- ----------------------------
-- Table structure for Himall_Topic
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Topic`;
CREATE TABLE `Himall_Topic` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL COMMENT '专题名称',
  `FrontCoverImage` varchar(100) DEFAULT NULL COMMENT '封面图片',
  `TopImage` varchar(100) NOT NULL COMMENT '主图',
  `BackgroundImage` varchar(100) DEFAULT NULL COMMENT '背景图片',
  `PlatForm` int(11) NOT NULL DEFAULT '0' COMMENT '使用终端',
  `Tags` varchar(100) DEFAULT NULL COMMENT '标签',
  `ShopId` bigint(20) NOT NULL DEFAULT '0' COMMENT '店铺ID',
  `IsRecommend` tinyint(1) unsigned zerofill NOT NULL COMMENT '是否推荐',
  `SelfDefineText` text COMMENT '自定义热点',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_TopicModule
-- ----------------------------
DROP TABLE IF EXISTS `Himall_TopicModule`;
CREATE TABLE `Himall_TopicModule` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TopicId` bigint(20) NOT NULL COMMENT '专题ID',
  `Name` varchar(100) NOT NULL COMMENT '专题名称',
  `TitleAlign` int(11) NOT NULL COMMENT '标题位置 0、left；1、center ；2、right',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Topic_TopicModule` (`TopicId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_Type
-- ----------------------------
DROP TABLE IF EXISTS `Himall_Type`;
CREATE TABLE `Himall_Type` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL COMMENT '类型名称',
  `IsSupportColor` tinyint(1) NOT NULL COMMENT '是否支持颜色规格',
  `IsSupportSize` tinyint(1) NOT NULL COMMENT '是否支持尺寸规格',
  `IsSupportVersion` tinyint(1) NOT NULL COMMENT '是否支持版本规格',
  `IsDeleted` bit(1) NOT NULL COMMENT '是否已删除',
  `ColorAlias` varchar(50) DEFAULT NULL COMMENT '颜色别名',
  `SizeAlias` varchar(50) DEFAULT NULL COMMENT '尺码别名',
  `VersionAlias` varchar(50) DEFAULT NULL COMMENT '规格别名',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_TypeBrand
-- ----------------------------
DROP TABLE IF EXISTS `Himall_TypeBrand`;
CREATE TABLE `Himall_TypeBrand` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TypeId` bigint(20) NOT NULL,
  `BrandId` bigint(20) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Brand_TypeBrand` (`BrandId`) USING BTREE,
  KEY `FK_Type_TypeBrand` (`TypeId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_VerificationRecord
-- ----------------------------
DROP TABLE IF EXISTS `Himall_VerificationRecord`;
CREATE TABLE `Himall_VerificationRecord` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `VerificationCodeIds` varchar(1000) NOT NULL COMMENT '核销码ID集合',
  `VerificationTime` datetime NOT NULL COMMENT '核销时间',
  `VerificationUser` varchar(50) NOT NULL COMMENT '核销人',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='虚拟订单核销记录表';

-- ----------------------------
-- Table structure for Himall_VirtualOrderItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_VirtualOrderItem`;
CREATE TABLE `Himall_VirtualOrderItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `OrderId` bigint(20) NOT NULL COMMENT '订单ID',
  `OrderItemId` bigint(20) NOT NULL COMMENT '订单项ID',
  `VirtualProductItemName` varchar(25) NOT NULL COMMENT '虚拟商品信息项名称',
  `Content` varchar(1000) DEFAULT NULL COMMENT '信息项填写内容',
  `VirtualProductItemType` tinyint(4) NOT NULL COMMENT '信息项类型(1=文本格式，2=日期，3=时间，4=身份证，5=数字格式，6=图片)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='虚拟订单信息项表';

-- ----------------------------
-- Table structure for Himall_VirtualProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_VirtualProduct`;
CREATE TABLE `Himall_VirtualProduct` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `ValidityType` bit(1) NOT NULL COMMENT '商品有效期类型(0=长期有效，1=自定义日期)',
  `StartDate` datetime DEFAULT NULL COMMENT '自定义开始时间',
  `EndDate` datetime DEFAULT NULL COMMENT '自定义结束时间',
  `EffectiveType` tinyint(4) NOT NULL COMMENT '核销码生效类型（1=立即生效，2=付款完成X小时后生效，3=次日生效）',
  `Hour` int(11) NOT NULL COMMENT '付款完成X小时后生效',
  `SupportRefundType` tinyint(4) NOT NULL COMMENT '1=支持有效期内退款，2=支持随时退款，3=不支持退款',
  `UseNotice` varchar(400) DEFAULT '' COMMENT '使用须知',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='虚拟商品表';

-- ----------------------------
-- Table structure for Himall_VirtualProductItem
-- ----------------------------
DROP TABLE IF EXISTS `Himall_VirtualProductItem`;
CREATE TABLE `Himall_VirtualProductItem` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `ProductId` bigint(20) NOT NULL COMMENT '商品ID',
  `Name` varchar(25) NOT NULL COMMENT '信息项标题名称',
  `Type` tinyint(4) NOT NULL COMMENT '信息项类型(1=文本格式，2=日期，3=时间，4=身份证，5=数字格式，6=图片)',
  `Required` bit(1) NOT NULL COMMENT '是否必填(0=否，1=是)',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='虚拟商品信息项表';

-- ----------------------------
-- Table structure for Himall_VShop
-- ----------------------------
DROP TABLE IF EXISTS `Himall_VShop`;
CREATE TABLE `Himall_VShop` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Name` varchar(20) DEFAULT NULL COMMENT '名称',
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `CreateTime` datetime NOT NULL COMMENT '创建日期',
  `VisitNum` int(11) NOT NULL COMMENT '历览次数',
  `buyNum` int(11) NOT NULL COMMENT '购买数量',
  `State` int(11) NOT NULL COMMENT '状态',
  `Logo` varchar(200) DEFAULT NULL COMMENT 'LOGO',
  `BackgroundImage` varchar(200) DEFAULT NULL COMMENT '背景图',
  `Description` varchar(30) DEFAULT NULL COMMENT '详情',
  `Tags` varchar(100) DEFAULT NULL COMMENT '标签',
  `HomePageTitle` varchar(20) DEFAULT NULL COMMENT '微信首页显示的标题',
  `WXLogo` varchar(200) DEFAULT NULL COMMENT '微信Logo',
  `IsOpen` tinyint(1) NOT NULL DEFAULT '1' COMMENT '是否开启微店',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_vshop_shopinfo` (`ShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_VShopExtend
-- ----------------------------
DROP TABLE IF EXISTS `Himall_VShopExtend`;
CREATE TABLE `Himall_VShopExtend` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `VShopId` bigint(20) NOT NULL COMMENT '微店ID',
  `Sequence` int(11) NOT NULL COMMENT '顺序',
  `Type` int(11) NOT NULL COMMENT '微店类型（主推微店、热门微店）',
  `AddTime` datetime NOT NULL COMMENT '添加时间',
  `State` int(11) NOT NULL COMMENT '审核状态',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_vshopextend_vshop` (`VShopId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WeiActivityAward
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WeiActivityAward`;
CREATE TABLE `Himall_WeiActivityAward` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ActivityId` bigint(20) NOT NULL,
  `AwardLevel` int(11) NOT NULL COMMENT '保存字段1-10 分别对应1至10等奖',
  `AwardType` int(11) NOT NULL COMMENT '积分；红包；优惠卷',
  `AwardCount` int(11) NOT NULL,
  `Proportion` float NOT NULL,
  `Integral` int(11) NOT NULL,
  `BonusId` bigint(20) NOT NULL DEFAULT '0',
  `CouponId` bigint(20) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_WeiActivityAward_2` (`ActivityId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WeiActivityInfo
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WeiActivityInfo`;
CREATE TABLE `Himall_WeiActivityInfo` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ActivityTitle` varchar(200) NOT NULL,
  `ActivityType` int(11) NOT NULL,
  `ActivityDetails` varchar(500) NOT NULL,
  `ActivityUrl` varchar(300) NOT NULL,
  `BeginTime` datetime NOT NULL,
  `EndTime` datetime NOT NULL,
  `ParticipationType` int(11) NOT NULL COMMENT '0 共几次 1天几次 2无限制',
  `ParticipationCount` int(11) NOT NULL,
  `ConsumePoint` int(11) NOT NULL COMMENT '0不消耗积分 大于0消耗积分',
  `CodeUrl` varchar(300) DEFAULT NULL,
  `AddDate` datetime NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WeiActivityWinInfo
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WeiActivityWinInfo`;
CREATE TABLE `Himall_WeiActivityWinInfo` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) NOT NULL,
  `ActivityId` bigint(20) NOT NULL,
  `AwardId` bigint(20) NOT NULL,
  `IsWin` tinyint(1) NOT NULL,
  `AwardName` varchar(200) DEFAULT NULL,
  `AddDate` datetime NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_WeiActivityWinInfo_W2` (`ActivityId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WeiXinArticleUrl
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WeiXinArticleUrl`;
CREATE TABLE `Himall_WeiXinArticleUrl` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ProductId` bigint(20) NOT NULL DEFAULT '0' COMMENT '商品编号',
  `SuperiorId` bigint(20) NOT NULL DEFAULT '0' COMMENT '分销员ID',
  `ArticleUrl` varchar(500) NOT NULL COMMENT '微信文章的Url（视频号推广使用）',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WeiXinBasic
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WeiXinBasic`;
CREATE TABLE `Himall_WeiXinBasic` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `Ticket` varchar(200) DEFAULT NULL COMMENT '微信Ticket',
  `TicketOutTime` datetime NOT NULL COMMENT '微信Ticket过期日期',
  `AppId` varchar(50) DEFAULT NULL,
  `AccessToken` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WeiXinMsgTemplate
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WeiXinMsgTemplate`;
CREATE TABLE `Himall_WeiXinMsgTemplate` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `MessageType` int(11) NOT NULL COMMENT '消息类别',
  `TemplateNum` varchar(30) DEFAULT NULL COMMENT '消息模板编号',
  `TemplateId` varchar(100) DEFAULT NULL COMMENT '消息模板ID',
  `UpdateDate` datetime NOT NULL COMMENT '更新日期',
  `IsOpen` tinyint(1) NOT NULL COMMENT '是否启用',
  `UserInWxApplet` tinyint(4) unsigned zerofill NOT NULL DEFAULT '0000' COMMENT '是否小程序微信通知',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WXAccToken
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WXAccToken`;
CREATE TABLE `Himall_WXAccToken` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `AppId` varchar(50) DEFAULT NULL,
  `AccessToken` varchar(150) NOT NULL COMMENT '微信访问令牌',
  `TokenOutTime` datetime NOT NULL COMMENT '微信令牌过期日期',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WXAppletFormData
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WXAppletFormData`;
CREATE TABLE `Himall_WXAppletFormData` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `EventId` bigint(20) NOT NULL COMMENT '事件ID',
  `EventValue` varchar(255) DEFAULT NULL COMMENT '事件值',
  `FormId` varchar(255) DEFAULT NULL COMMENT '事件的表单ID',
  `EventTime` datetime NOT NULL COMMENT '事件时间',
  `ExpireTime` datetime NOT NULL COMMENT 'FormId过期时间',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WXCardCodeLog
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WXCardCodeLog`;
CREATE TABLE `Himall_WXCardCodeLog` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `CardLogId` bigint(20) NOT NULL COMMENT '卡券记录号',
  `CardId` varchar(50) DEFAULT NULL,
  `Code` varchar(50) DEFAULT NULL COMMENT '标识',
  `SendTime` datetime NOT NULL COMMENT '投放时间',
  `CodeStatus` int(11) NOT NULL DEFAULT '0' COMMENT '状态',
  `UsedTime` datetime DEFAULT NULL COMMENT '操作时间 失效、核销、删除时间',
  `CouponType` int(11) NOT NULL COMMENT '红包类型',
  `CouponCodeId` bigint(20) NOT NULL COMMENT '红包记录编号',
  `OpenId` varchar(4000) DEFAULT NULL COMMENT '对应OpenId',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Himall_WXLog_CardLogId` (`CardLogId`) USING BTREE,
  KEY `IDX_Himall_WXLog_CardId` (`CardId`) USING BTREE,
  KEY `IDX_Himall_WXLog_Code` (`Code`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WXCardLog
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WXCardLog`;
CREATE TABLE `Himall_WXCardLog` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT COMMENT '编号',
  `CardId` varchar(50) DEFAULT NULL COMMENT '卡券编号',
  `CardTitle` varchar(50) DEFAULT NULL COMMENT '标题 英文27  汉字 9个',
  `CardSubTitle` varchar(100) DEFAULT NULL COMMENT '副标题 英文54  汉字18个',
  `CardColor` varchar(10) DEFAULT NULL COMMENT '卡券颜色 HasTable',
  `AuditStatus` int(11) NOT NULL DEFAULT '0' COMMENT '审核状态',
  `AppId` varchar(50) DEFAULT NULL,
  `AppSecret` varchar(50) DEFAULT NULL,
  `CouponType` int(11) NOT NULL COMMENT '红包类型',
  `CouponId` bigint(20) NOT NULL COMMENT '红包编号 涉及多表，不做外键',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IDX_Himall_WXCardLog_CardId` (`CardId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WXshop
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WXshop`;
CREATE TABLE `Himall_WXshop` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ShopId` bigint(20) NOT NULL COMMENT '店铺ID',
  `AppId` varchar(30) NOT NULL COMMENT '公众号的APPID',
  `AppSecret` varchar(35) NOT NULL COMMENT '公众号的AppSecret',
  `Token` varchar(30) NOT NULL COMMENT '公众号的Token',
  `FollowUrl` varchar(500) DEFAULT NULL COMMENT '跳转的URL',
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- ----------------------------
-- Table structure for Himall_WXSmallChoiceProduct
-- ----------------------------
DROP TABLE IF EXISTS `Himall_WXSmallChoiceProduct`;
CREATE TABLE `Himall_WXSmallChoiceProduct` (
  `ProductId` int(11) NOT NULL,
  PRIMARY KEY (`ProductId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
