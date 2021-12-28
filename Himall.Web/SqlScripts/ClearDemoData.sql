set foreign_key_checks=0;
DELETE FROM himall_accountdetail WHERE ID>=532 AND ID<=546;
DELETE FROM himall_account WHERE ID>=157 AND ID<=162;
DELETE FROM himall_activemarketservice WHERE ID>=38 AND ID<=42;

DELETE FROM himall_articlecategory WHERE ID>=9 AND ID<=17;

DELETE FROM himall_article WHERE ID>=88 AND ID<=92;

DELETE FROM himall_attributevalue WHERE ID>=810 AND ID<=852;
DELETE FROM himall_attribute WHERE ID>=192 AND ID<=204;

DELETE FROM himall_banner WHERE ID>=73 AND ID<=81 AND  DisplaySequence>0;

DELETE FROM himall_brand WHERE ID>=319 AND ID<=367 ;
/*
DELETE FROM himall_category WHERE ID>=1 AND ID<=159 ;
*/
DELETE FROM himall_categorycashdeposit WHERE CategoryId>=1 AND CategoryId<=159 ;

DELETE FROM himall_chargedetailshop WHERE  ID in ('17021411310728770','17021411323326291');

/*×éºÏ¹º*/
DELETE c FROM himall_collocationporuduct a INNER JOIN himall_collocation b on a.colloid=b.id
INNER JOIN himall_collocationsku c on c.colloproductid=a.id
WHERE  b.Id>=21 AND b.Id<=25
;
DELETE a FROM himall_collocationporuduct a INNER JOIN himall_collocation b on a.colloid=b.id
WHERE  b.Id>=21 AND b.Id<=25
;
DELETE FROM himall_collocation WHERE ID>=21 AND ID<=25
;
DELETE b FROM Himall_Coupon a INNER JOIN himall_couponsetting b on a.Id=b.couponid WHERE  a.Id>=59 AND a.Id<=64;
DELETE FROM Himall_Coupon WHERE Id>=59 AND Id<=64;

DELETE FROM himall_couponrecord WHERE ID IN (884,885);


DELETE FROM himall_fightgroupactiveitem WHERE ActiveId>=26 AND ActiveId<=30 ;
DELETE FROM himall_fightgroupactive WHERE  Id>=26 AND Id<=30;

DELETE FROM himall_flashsaledetail WHERE  FlashSaleId>=26 AND FlashSaleId<=30 ;
DELETE FROM himall_flashsale WHERE Id>=34 AND Id<=50 ;
DELETE FROM himall_flashsaleconfig  ;

/*
DELETE b FROM himall_freighttemplate a INNER JOIN himall_freightareadetail b on a.Id=b.FreightTemplateId
WHERE  a.Id>=166 AND a.Id<=169
;
DELETE b FROM himall_freighttemplate a INNER JOIN himall_freightareacontent b on a.Id=b.FreightTemplateId
WHERE  a.Id>=166 AND a.Id<=169
;
DELETE FROM himall_freighttemplate WHERE Id>=166 AND Id<=169 ;
*/
DELETE FROM himall_gift WHERE Id>=68 AND Id<=77 ;

DELETE FROM himall_integralmallad WHERE Id>=3 AND Id<=4 ;

DELETE FROM himall_label WHERE Id>=33 AND Id<=35 ;

DELETE FROM himall_log WHERE Id>=4608 AND Id<=5024 ;

DELETE FROM himall_marketservicerecord WHERE Id>=101 AND Id<=107 ;
DELETE FROM himall_marketsettingmeta WHERE Id>=5 AND Id<=5 ;

DELETE FROM himall_memberintegralexchangerule WHERE Id>=2 AND Id<=2 ;

DELETE FROM himall_menu WHERE Id>=65 AND Id<=72 ;

DELETE FROM himall_messagelog WHERE Id>=875 AND Id<=878 ;

DELETE FROM himall_mobilehomeproduct WHERE Id>=104 AND Id<=134 ;

DELETE FROM himall_moduleproduct WHERE Id>=723 AND Id<=963 ;

DELETE FROM himall_photospace WHERE Id>=102 AND Id<=538 ;
DELETE FROM himall_photospacecategory WHERE Id>=3 AND Id<=3 ;

DELETE FROM himall_plataccountitem WHERE AccoutId>=1 AND AccoutId<=1 ;

DELETE FROM himall_plataccount WHERE Id>=1 AND Id<=1 ;

DELETE FROM himall_productattribute WHERE  ProductId>=699 AND ProductId<=749 ;
DELETE FROM himall_productdescription WHERE ProductId>=699 AND ProductId<=749 ;
DELETE FROM himall_sku  WHERE ProductId>=699 AND ProductId<=749 ;
DELETE FROM himall_product WHERE Id>=699 AND Id<=749 ;
DELETE FROM himall_browsinghistory WHERE ProductId>=699 AND ProductId<=749 ;
DELETE FROM himall_favorite WHERE ProductId>=699 AND ProductId<=749 ;


DELETE FROM himall_productshopcategory WHERE shopcategoryid>=350 AND shopcategoryid<=360 ;
/*DELETE FROM himall_shopcategory WHERE Id>=350 AND Id<=360 ;*/

DELETE FROM himall_receivingaddressconfig WHERE Id>=1 AND Id<=1 ;
DELETE FROM himall_refundreason WHERE Id>=28 AND Id<=30 ;

DELETE FROM himall_roleprivilege WHERE roleid>=46 AND roleid<=49 ;
DELETE FROM himall_role WHERE Id>=46 AND Id<=49 ;

DELETE FROM himall_settled WHERE Id>=2 AND Id<=2 ;


DELETE FROM himall_shopbonus WHERE Id>=8 AND Id<=8 ;

DELETE FROM himall_shopaccountitem WHERE shopid>=1 AND shopid<=1 ;
DELETE FROM himall_shopaccount WHERE Id>=1 AND Id<=1 ;

DELETE FROM himall_shopbranchmanager WHERE  shopbranchId>=26 AND shopbranchId<=26 ;
DELETE FROM himall_shopbranchsku WHERE ShopBranchId>=26 AND ShopBranchId<=26 ;
DELETE FROM Himall_ShopBranch WHERE  ShopID>=1 AND ShopID<=1 ;

DELETE FROM himall_shopfooter WHERE Id>=19 AND Id<=19 ;

DELETE FROM himall_shophomemoduleproduct WHERE homemoduleid>=29 AND homemoduleid<=31 ;
DELETE FROM himall_shophomemodule WHERE Id>=29 AND Id<=31 ;

DELETE FROM himall_sitesigninconfig WHERE Id>=2 AND Id<=2 ;

DELETE FROM himall_slidead  WHERE Id>=105 AND Id<=132 ;

/*DELETE FROM himall_specificationvalue  WHERE Id>=646 AND Id<=681 ;*/

DELETE FROM himall_topicmodule WHERE topicid>=54 AND topicid<=63 ;
DELETE FROM himall_topic WHERE id>=54 AND id<=63;

/*DELETE FROM himall_typebrand WHERE typeid>=82 AND typeid<=96;*/

/*DELETE FROM Himall_Type WHERE Id>=82 AND Id<=96 ;*/

DELETE FROM himall_weiactivityaward WHERE activityid>=154 AND activityid<=155;

DELETE FROM himall_weiactivityinfo WHERE Id>=154 AND Id<=155 ;

DELETE FROM himall_weixinbasic WHERE Id>=3 AND Id<=3 ;


DELETE FROM himall_weixinmsgtemplate WHERE Id>=41 AND Id<=58 ;

DELETE FROM himall_homecategory  WHERE Id>=2450 AND Id<=2518 ;

DELETE FROM himall_homefloor WHERE Id>=151 AND Id<=167 ;

DELETE FROM himall_floorbrand WHERE FloorId>=151 AND FloorId<=167 ;

DELETE FROM himall_floortopic WHERE FloorId>=151 AND FloorId<=167 ;
DELETE FROM himall_floorproduct WHERE FloorId>=151 AND FloorId<=167 ;


DELETE FROM himall_searchproduct WHERE ProductId>=699 AND ProductId<=749 ;

UPDATE `Himall_SiteSetting` SET `Value`='false' WHERE `Key`='IsCanClearDemoData';

set foreign_key_checks=1;