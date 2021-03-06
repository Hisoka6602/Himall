// ***********************************************************************
// 程序集			: NetRube.Data
// 文件名			: ExpandoColumn.cs
// 作者				: NetRube
// 创建时间			: 2013-08-05
//
// 最后修改者		: NetRube
// 最后修改时间		: 2013-11-05
// ***********************************************************************

// PetaPoco - A Tiny ORMish thing for your POCO's.
// Copyright © 2011-2012 Topten Software.  All Rights Reserved.

using System.Collections.Generic;

/// <summary>
/// Internal 命名空间
/// </summary>
namespace NetRube.Data.Internal
{
	internal class ExpandoColumn : PocoColumn
	{
		public override void SetValue(object target, object val) { (target as IDictionary<string, object>)[ColumnName] = val; }
		public override object GetValue(object target)
		{
			object val = null;
			(target as IDictionary<string, object>).TryGetValue(ColumnName, out val);
			return val;
		}
		public override object ChangeType(object val) { return val; }
	}
}