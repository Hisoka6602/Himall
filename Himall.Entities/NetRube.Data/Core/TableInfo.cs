// ***********************************************************************
// 程序集			: NetRube.Data
// 文件名			: TableInfo.cs
// 作者				: NetRube
// 创建时间			: 2013-08-05
//
// 最后修改者		: NetRube
// 最后修改时间		: 2013-11-05
// ***********************************************************************

// PetaPoco - A Tiny ORMish thing for your POCO's.
// Copyright © 2011-2012 Topten Software.  All Rights Reserved.

using PetaPoco;
using System;

/// <summary>
/// Data 命名空间
/// </summary>
namespace NetRube.Data
{
    /// <summary>
    /// 数据表信息
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// 获取或设置数据表名称
        /// </summary>
        /// <value>数据表名称</value>
        public string TableName
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置数据表主键名称
        /// </summary>
        /// <value>数据表主键名称</value>
        public string PrimaryKey
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否为自增主键
        /// </summary>
        /// <value>如果为自增主键，则该值为 <c>true</c>；否则为 <c>false</c>。</value>
        public bool AutoIncrement
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置作为 Oracle 自增主键的序列字段名
        /// </summary>
        /// <value>Oracle 自增主键的序列字段名</value>
        public string SequenceName
        {
            get;
            set;
        }

        /// <summary>
        /// 是否为数据库实体
        /// </summary>
        public bool IsTable { get; set; }


        /// <summary>
        /// 从实体类型生成数据表信息
        /// </summary>
        /// <param name="t">实体类型</param>
        /// <returns>数据表信息</returns>
        public static TableInfo FromPoco(Type t)
        {
            TableInfo ti = new TableInfo();

            // Get the table name
            var a = t.GetCustomAttributes(typeof(TableNameAttribute), true);
            ti.IsTable = a.Length > 0;
            ti.TableName = ti.IsTable ? (a[0] as TableNameAttribute).Value : t.Name;

            // Get the primary key
            a = t.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
            ti.PrimaryKey = a.Length == 0 ? "ID" : (a[0] as PrimaryKeyAttribute).Value;
            ti.SequenceName = a.Length == 0 ? null : (a[0] as PrimaryKeyAttribute).SequenceName;
            ti.AutoIncrement = a.Length == 0 ? false : (a[0] as PrimaryKeyAttribute).AutoIncrement;

            return ti;
        }
    }
}