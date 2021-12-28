using Himall.CommonModel;
using Himall.Entities;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Himall.Service
{
    public class ServiceBase
    {
        #region 字段



        #endregion

        #region 构造函数

        static ServiceBase()
        {

        }

        #endregion

        #region 属性



        #endregion

        #region 方法

        public void Dispose()
        {
        }

        #endregion
    }

    public class ServiceBase<T> : ServiceBase where T : IModel
    {
        private static object _lock = new object();
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public T Get(Expression<Func<T, bool>> expression)
        {
            return DbFactory.Default.Get<T>(expression).FirstOrDefault();
        }
        /// <summary>
        /// 获取总数
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public int GetCount(Expression<Func<T, bool>> expression)
        {
            return DbFactory.Default.Get<T>(expression).Count();
        }
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public List<T> GetList(Expression<Func<T, bool>> expression)
        {
            return DbFactory.Default.Get<T>(expression).ToList();
        }
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public List<T> GetList(Expression<Func<T, bool>> expression, Expression<Func<T, dynamic>> select)
        {
            var sql = DbFactory.Default.Get<T>(expression);
            if (select != null)
            {
                sql.Select(select);
            }
            return sql.ToList();
        }
        /// <summary>
        /// 获取分页
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public QueryPageModel<T> GetPageList(Expression<Func<T, bool>> expression, int page, int pagesize)
        {
            var result = new QueryPageModel<T>();
            var list = DbFactory.Default.Get<T>().Where(expression).ToPagedList(page, pagesize);
            result.Models = list;
            result.Total = list.TotalRecordCount;
            return result;
        }
        /// <summary>
        /// 是否成立
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public bool Any(Expression<Func<T, bool>> expression)
        {
            return DbFactory.Default.Get<T>(expression).Exist();
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="data"></param>
        /// <param name="need_lock">是否加锁</param>
        public virtual void Add(T data)
        {
            DbFactory.Default.Add(data);
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="need_lock">是否加锁</param>
        public virtual void Update(T data)
        {
            DbFactory.Default.Update(data);
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="need_lock">是否加锁</param>
        public virtual void Delete(Expression<Func<T, bool>> expression)
        {
            DbFactory.Default.Del<T>()
                .Where(expression)
                .Succeed();
        }
        /// <summary>
        /// 执行原生sql
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual IEnumerable<T> Query<T>(string sql)
        {
            return DbFactory.Default.Query<T>(sql);
        }
    }
}
