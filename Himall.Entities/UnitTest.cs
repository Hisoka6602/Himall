using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetRube.Data;
using System;
using System.Collections.Generic;
using static Himall.Entities.OrderInfo;

namespace Himall.Entities
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod()
        {
            var t = 23.4M;
            var x = string.Format("{00:F}", t);
            var db = DbFactory.Default.Get<ProductInfo>(p => p.ShopId == 1 && p.TypeId == 2);
            var data = db.ToList();
            var sql = DbFactory.Default.LastSQL;
            Console.WriteLine(DbFactory.Default.LastCommand);
        }

        /// <summary>
        /// 基础演示 添删改查
        /// </summary>
        public void Basics()
        {
            //添加一个对象
            var member = new MemberInfo
            {
                UserName = "张三"
            };
            DbFactory.Default.Add(member);//添加

            //添加多个对象
            var items = new List<MemberGradeInfo>
            {
                new MemberGradeInfo { GradeName = "黄金" },
                new MemberGradeInfo { GradeName = "铂金" },
                new MemberGradeInfo { GradeName = "钻石" }
            };
            DbFactory.Default.AddRange(items);


            var grade = DbFactory.Default.Get<MemberGradeInfo>().Where(p => p.GradeName == "钻石").FirstOrDefault();

            DbFactory.Default.Delete(grade);//根据实体主键 删除实体

            //删除 delete from himall_MemberGrade where GradeName = '黄金'
            var isSuccess = DbFactory.Default.Del<MemberGradeInfo>().Where(p => p.GradeName == "黄金").Succeed();

            //删除 立即执行
            var changeRows = DbFactory.Default.Del<MemberGradeInfo>(p => p.GradeName == "铂金");


            //修改 实体更新 update himall_Capital set balance=100,presentAmount=100 where id = 1
            var capital = DbFactory.Default.Get<CapitalInfo>().Where(p => p.Id == 1).FirstOrDefault();
            capital.Balance = 100;
            capital.PresentAmount = 100;
            DbFactory.Default.Update(capital); //根据主键key 更新数据

            //修改 批量修改 update himall_sku set safeStock = 5 where productid = 1
            DbFactory.Default.Set<SKUInfo>()
                .Set(p => p.SafeStock, 5)
                .Where(p => p.ProductId == 1)
                .Succeed();

            //修改 增量修改 update Himall_Sku set Stock = Stock + 100 where ProductId = 1
            DbFactory.Default.Set<SKUInfo>()
                .Set(p => p.Stock, s => s.Stock + 100)
                .Where(p => p.ProductId == 1)
                .Succeed();

            //保存
            var member1 = new MemberInfo
            {
                Id = 4,
                UserName = "李四"
            };
            DbFactory.Default.Save(member1);//如主键存在 则更新,如主键不存在则插入 


            //查询 集合 select * from himall_product wher CategoryId == 1
            var products = DbFactory.Default.Get<ProductInfo>().Where(p => p.CategoryId == 1).ToList();
            //查询 单行
            var product = DbFactory.Default.Get<ProductInfo>().Where(p => p.Id == 1).FirstOrDefault();
        }

        /// <summary>
        /// 组合查询
        /// </summary>
        public void CombiningQueries() {
            var query = new
            {
                Status = new[]
                {
                    OrderOperateStatus.WaitPay,
                    OrderOperateStatus.WaitSelfPickUp
                },
                Shop = 1,
                ShopName = "测试商品",
                PageNo = 1,
                PageSzie = 10
            };

            var db = DbFactory.Default.Get<OrderInfo>(); // 声明查询主表

            if (query.Shop > 0)
                db.Where(p => p.ShopId == query.Shop);// where子句 添加一个筛选条件 
                                                      //注:与EF不同  不需要 db = db.Where(p => p.ShopId == query.Shop);

            if (query.Status !=null)
                db.Where(p => p.OrderStatus.ExIn(query.Status)); //where子句 in查询  where OrderStatus in (1,6)
                                                                // .ExNotIn  .ExIsNull .ExIsNotNull

            if (!string.IsNullOrEmpty(query.ShopName)) {
                db.Where(p => p.ShopName.Contains(query.ShopName)); //where子句 like 查询 where ShopName like '%'+query.ShopName+'%'

                //扩展
                //db.Where(p => p.ShopName.StartsWith(query.ShopName));// where ShopName like query.ShopName+'%'
                //db.Where(p => p.ShopName.EndsWith(query.ShopName));// where ShopName like '%'+query.ShopName
            }
            
            //获取符合条件记录
            var list = db.ToList();

            //获取分页记录  PageNo:从1开始
            var pagedList = db.ToPagedList(query.PageNo, query.PageSzie);
            var rows = pagedList;//pagedList 为 List<T> 扩展类型
            var total = pagedList.TotalRecordCount; //新增属性 pagedList.TotalRecordCount 总行数
        }


        /// <summary>
        /// 演示数据模型
        /// </summary>
        internal class DomeOrderItemDTO
        {
            public long Id { get; set; }
            public decimal Price { get; set; }
            public long Quantity { get; set; }
            public DateTime PayDate { get; set; }
            public OrderOperateStatus Status { get; set; }
            public string Name { get; set; }
        }
        /// <summary>
        /// 多表链接查询查询
        /// </summary>
        public void MultiTableQueries()
        {
            // select * from himall_orderitem oi left join himall_order o on io.OrderId=o.Id
            // OrderItemInfo 为查询主表 OrderInfo 为关联从表
            var db = DbFactory.Default.Get<OrderItemInfo>()
                        .LeftJoin<OrderInfo>((oi, o) => oi.OrderId == o.Id) // 注： (a,b)=>...  a:主表 b:从表
                        .LeftJoin<ProductInfo>((oi, p) => oi.ProductId == p.Id);

            //筛选主表条件 where oi.ProductId = 1
            db.Where(p => p.ProductId == 1);

            //筛选从表条件 and o.OrderStatus == 1
            db.Where<OrderInfo>(p => p.OrderStatus == OrderOperateStatus.WaitPay);

            //查询字段 选择 
            //注:当Select子句缺省时 查询默认使用 主表.* 如包含Select子句 则需要显式声明所需字段
            db.Select(p => new { p.Id, Price = p.SalePrice, p.Quantity, ActualQuantity = p.Quantity-p.ReturnQuantity  });//取响应字段 可取别名 select oi.SalePrice as Price
            db.Select<OrderInfo>(p => new { p.PayDate, Status = p.OrderStatus });
            db.Select<ProductInfo>(p => new { Name = p.ProductName });

            //排序 多个Order子句 将按顺序排列 下为 Order by oi.SalePrice,o.PayDate desc
            db.OrderBy(p => p.SalePrice);
            db.OrderByDescending<OrderInfo>(p => p.PayDate);
            db.OrderByDescending(p => "ActualQuantity");//如排序字段为 计算字段 直接使用别名字符串

            //获取查询结果 使用DTO数据模型 装载
            //依照 查询字段名与实体属性名称对应约定
            var result = db.ToList<DomeOrderItemDTO>();

            //other: 所有子句(Where,Select,ToList)当泛型参数<T>缺省时 统一都为 主表类型 本示例皆为 <OrderItemInfo>
        }


        internal class DomeProductDTO {
            public long ProductId { get; set; }
            public long TotalStock { get; set; }
            public decimal Average { get; set; }
        }
        /// <summary>
        /// 其他查询(Group子句,聚合函数,子查询)
        /// </summary>
        public void OtherQueries()
        {
            //聚合查询 select ProductId,sum(Stock) as TotalStock from Sku group by ProductId
            var result1 = DbFactory.Default.Get<SKUInfo>()
                .GroupBy(p => p.ProductId)
                .Select(p => new
                {
                    p.ProductId,
                    Total = p.Stock.ExSum(), //sum函数 
                    Stock = (p.Stock - p.SafeStock).ExSum(),//表达式计算 sum(Stock - SafeStock)
                    Average = p.SalePrice.ExAvg(),//avg函数
                    Count = p.ProductId.ExCount(false),//count函数  false:count(0),true count(ProductId)
                    Max = p.SalePrice.ExMax(),//max函数
                    Min = p.SalePrice.ExMin(),//min函数 
                    SafeStock = p.SafeStock.ExIfNull(0),//ifnull(SafeStock,0) 函数
                }).ToList<DomeProductDTO>();

            //子查询
            var categories = DbFactory.Default.Get<CategoryInfo>().Where(p => p.ParentCategoryId == 0).Select(p=>p.Id); //注:这里必须包含 唯一字段Select子句 这里没有ToList
            var result2 = DbFactory.Default.Get<ProductInfo>().Where(p => p.CategoryId.ExIn(categories)).ToList(); // .ExIn .ExNotIn  where CategoryId in (selet id from Category where ParentCategoryId=0)


            //子查询 select id,(select sum(stock) from Himall_SKU where productId=p.Id) as totalStock from Himall_Product p where id = 1609
            var stock = DbFactory.Default.Get<SKUInfo>()
                .Where<ProductInfo>((s, p) => s.ProductId == p.Id)
                .Select(p => p.Stock.ExSum());//子句 select sum(stock) from Himall_SKU where productId=p.Id 

            var result0 = DbFactory.Default.Get<ProductInfo>()
                .Where(p => p.Id == 1609)
                .Select(p => new
                {
                    p.Id,
                    TotalStock = stock.ExResolve<long>()//子查询转化
                }).ToList();

            //集合查询
            var products = new List<long> { 1, 2, 3, 4, 5 };
            var result3 = DbFactory.Default.Get<ProductInfo>().Where(p => p.Id.ExIn(products)).ToList();//注:与EF不同  EF:products.Contains(p.Id)

            //数据行是否存在 bool类型
            var result= DbFactory.Default.Get<MemberInfo>().Where(p => p.Id == 101).Exist();

            //数据总行数
            var total = DbFactory.Default.Get<SKUInfo>().Where(p => p.ProductId == 1).Count();

            //忽略前10行 取10~20行数据
            var result4 = DbFactory.Default.Get<OrderInfo>().Where(p => p.OrderDate > DateTime.Now.Date)
                .Skip(10)//忽略10
                .Take(10)//取10
                .ToList();

        }
        /// <summary>
        /// 事物执行
        /// </summary>

        public void Transaction() {
          
            var detail = new CapitalDetailInfo
            {
                CapitalID = 1,
                Amount = 100,
                CreateTime = DateTime.Now,
                SourceType = CapitalDetailInfo.CapitalDetailType.ChargeAmount,
                Remark = "示例充值"
            };
            //简单事物  完成自动提交， 错误将抛出异常
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Add(detail); //添加充值详情
                DbFactory.Default.Set<CapitalInfo>() //增加账户金额
                .Where(p => p.Id == detail.CapitalID)
                .Set(p => p.Balance, b => b.Balance + detail.Amount)
                .Succeed();
            });

            //自定义事物异常处理

            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Add(detail); //添加充值详情
                DbFactory.Default.Set<CapitalInfo>() //增加账户金额
                .Where(p => p.Id == detail.CapitalID)
                .Set(p => p.Balance, b => b.Balance + detail.Amount)
                .Succeed();
                return true;
            }, () => {
                //事物完成 回调方法(可选参数)
            }, (ex) => {
                //事物失败 回调方法(可选参数)
            });
        }

        /// <summary>
        /// 其他
        /// </summary>
        public void Other()
        {
            //调试属性
            var sql = DbFactory.Default.LastSQL;//最后执行SQL语句
            var command = DbFactory.Default.LastCommand;//最后执行 SQL语句与参数集合
        }
    }
}