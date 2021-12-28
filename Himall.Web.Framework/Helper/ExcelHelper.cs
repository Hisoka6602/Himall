using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Data;
using NPOI.SS.UserModel;

namespace Himall.Web.Framework
{
    /// <summary>
    /// Excel帮助类(NPOI)
    /// </summary>
    public class ExcelHelper
    {
        DownloadHelper downHelper = null;
        public ExcelHelper()
        {
            downHelper = new DownloadHelper();
        }

        /// <summary>
        /// 从excle导入到数据集，excle中的工作表对应dataset中的table，工作表名和列名分别对应table中的表名和列名
        /// </summary>
        /// <param name="path">Web服务器上的文件物理路径</param>
        /// <returns>DataSet</returns>
        public DataSet ExcelToDataSet(string path)
        {
            DataSet ds = new DataSet();
            IWorkbook wb = WorkbookFactory.Create(path);
            for (int sheetIndex = 0; sheetIndex < wb.NumberOfSheets; sheetIndex++)
            {
                
                //按索引获取表格
                ISheet sheet = wb.GetSheetAt(sheetIndex);
                if (sheet.SheetName != "模板说明") {
                    DataTable dt = new DataTable(sheet.SheetName);
                    if (sheet.PhysicalNumberOfRows < 1)
                    {
                        continue;
                    }
                    int rowsCount = sheet.LastRowNum + 1;//总行数
                    int columnCount = 0;//总列数
                    IRow headRow = sheet.GetRow(0);
                    if (headRow != null)
                    {
                        columnCount = headRow.LastCellNum-1;
                    }
                    //添加列，读从索引为0的行列值
                    for (int i = 0; i < columnCount; i++)
                    {
                        IRow currentRow = sheet.GetRow(0);
                        if (currentRow != null)
                        {
                            if (i <= currentRow.Cells.Count())
                            {
                                ICell cell = currentRow.GetCell(i);
                                if (cell != null)
                                {
                                    dt.Columns.Add(cell.ToString());
                                }
                            }

                        }
                    }
                    //添加行，从索引为1的行开始
                    for (int i = 1; i < rowsCount; i++)
                    {
                        DataRow dr = dt.NewRow();
                        IRow currentRow = sheet.GetRow(i);
                        if (currentRow != null)
                        {
                            for (int j = 0; j < columnCount; j++)
                            {
                                ICell cell = currentRow.GetCell(j);
                                if (cell != null)
                                {
                                    dr.SetField(j, cell.ToString());
                                }
                            }
                        }
                        dt.Rows.Add(dr);
                    }
                    ds.Tables.Add(dt);
                }
                
            }
            return ds;
        }



        /// <summary>
        /// 将数据集中的数据导入到excel中，多个table对应的导入到excel对应多个工作表
        /// </summary>
        /// <param name="ds">要导出到excle中的数据集，数据集中表名和字段名在excel中对应工作表名和标题名称</param>
        /// <param name="fileName">保存的文件名，后缀名为.xls或.xlsx</param>
        public void DataSetToExcel(DataSet ds, string fileName)
        {
            if (ds != null && ds.Tables.Count > 0)
            {
                IWorkbook wb = CreateSheet(fileName);
                foreach (DataTable dt in ds.Tables)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        ImportToWorkbook(dt, ref wb);
                    }
                }
                downHelper.DownloadByOutputStreamBlock(new MemoryStream(ToByte(wb)), fileName);
            }
        }

        /// <summary>
        /// 将数据集中的数据导入到excel中，多个table对应的导入到excel对应多个工作表
        /// </summary>
        /// <param name="ds">要导出到excle中的数据集，数据集中表名和字段名在excel中对应工作表名和标题名称</param>
        /// <param name="path">文件虚拟路径</param>
        /// <param name="fileName">保存的文件名，后缀名为.xls或.xlsx</param>
        public void DataSetToExcel(DataSet ds, string path, string fileName)
        {
            if (ds != null && ds.Tables.Count > 0)
            {
                IWorkbook wb = CreateSheet(fileName);
                foreach (DataTable dt in ds.Tables)
                {
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        ImportToWorkbook(dt, ref wb);
                    }
                }
                string fullPath = HttpContext.Current.Server.MapPath(path);
                using (FileStream fileStream = new FileStream(fullPath + "\\" + fileName, FileMode.Create, FileAccess.Write))
                {
                    wb.Write(fileStream);
                    fileStream.Flush();
                }
            }
        }

        /// <summary>
        /// 将集合中的数据导入到excle中，不同的集合对应excel中的不同的工作表
        /// </summary>
        /// <param name="listArray">不同对象的集合,集合中的对象可以通过设置特性来关联列名</param>
        /// <param name="fileName">保存的文件名，后缀名为.xls或.xlsx</param>
        public void ListToExcel(IList[] listArray, string fileName)
        {
            DataSetToExcel(ConvertToDataSet(listArray), fileName);
        }

        /// <summary>
        /// 将数据导入到excel中
        /// </summary>
        /// <param name="dt">要导出到excle中的数据表，表名和字段名在excel中对应工作表名和标题名称</param>
        /// <param name="fileName">保存的文件名，后缀名为.xls或.xlsx</param>
        public void DataTableToExcel(DataTable dt, string fileName,bool isdownload=true)
        {
            if (dt != null && dt.Rows.Count > 0)
            {
                IWorkbook wb = CreateSheet(fileName);
                ImportToWorkbook(dt, ref wb);
                if (isdownload)
                {
                    downHelper.DownloadByOutputStreamBlock(new MemoryStream(ToByte(wb)), fileName);
                }
                else {
                    WriteToFile(wb, fileName);
                }
              
            }
        }

        public void WriteToFile(IWorkbook workbook, string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                workbook.Write(fs);
                fs.Close();
            }
        }

        /// <summary>
        /// 将IList转换成DataSet
        /// </summary>
        /// <param name="listArray"></param>
        /// <returns></returns>
        private DataSet ConvertToDataSet(IList[] listArray)
        {
            DataSet ds = new DataSet();
            foreach (IList list in listArray)
            {
                if (list != null && list.Count > 0)
                {
                    object obj = list[0];
                    string tableName = obj.GetType().Name;
                    object[] classInfos = obj.GetType().GetCustomAttributes(typeof(EntityMappingAttribute), true);
                    if (classInfos.Length > 0)
                    {
                        tableName = ((EntityMappingAttribute)classInfos[0]).Name;
                    }
                    //创建表
                    DataTable dt = new DataTable(tableName);
                    //添加列
                    PropertyInfo[] propertyInfos = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (PropertyInfo propertyInfo in propertyInfos)
                    {
                        object[] attributes = propertyInfo.GetCustomAttributes(typeof(EntityMappingAttribute), true);
                        if (attributes.Length > 0)
                        {
                            dt.Columns.Add(((EntityMappingAttribute)attributes[0]).Name);
                        }
                        else
                        {
                            dt.Columns.Add(propertyInfo.Name);
                        }
                    }
                    //添加数据
                    for (int i = 0; i < list.Count; i++)
                    {
                        DataRow dr = dt.NewRow();
                        object objTemp = list[i];
                        PropertyInfo[] propertyInfosTemp = objTemp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        for (int j = 0; j < propertyInfosTemp.Count(); j++)
                        {
                            dr.SetField(j, propertyInfosTemp[j].GetValue(obj, null));
                        }
                        dt.Rows.Add(dr);
                    }
                    ds.Tables.Add(dt);
                }
            }
            return ds;
        }

        /// <summary>
        /// 导入DataTable到Workbook，并自适应列宽。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="wb"></param>
        private void ImportToWorkbook(DataTable dt, ref IWorkbook wb)
        {
            string sheetName = string.IsNullOrEmpty(dt.TableName) == false ? dt.TableName : "Sheet" + (wb.NumberOfSheets + 1).ToString();
            //创建工作表
            ISheet sheet = wb.CreateSheet(sheetName);
            //添加标题
            IRow titleRow = sheet.CreateRow(0);
            SetRow(titleRow, GetCloumnNames(dt), GetCellStyle(sheet.Workbook, FontBoldWeight.Bold));
            //添加数据行
            IRow dataRow = null;
            ICellStyle ics = GetCellStyle(sheet.Workbook);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dataRow = sheet.CreateRow(i + 1);
                SetRow(dataRow, GetRowValues(dt.Rows[i]), ics);
            }
            //设置表格自适应宽度
            AutoSizeColumn(sheet);
        }

        /// <summary>
        /// 将Workbook写入到内存流并输出字节数组
        /// </summary>
        /// <param name="wb"></param>
        /// <returns></returns>
        private byte[] ToByte(IWorkbook wb)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                //XSSFWorkbook即读取.xlsx文件返回的MemoryStream是关闭，但是可以ToArray()，这是NPOI的Bug
                wb.Write(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 创建Workbook的Sheet
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IWorkbook CreateSheet(string path)
        {
            IWorkbook wb = new NPOI.HSSF.UserModel.HSSFWorkbook(); ;
            string extension = Path.GetExtension(path).ToLower();
            if (extension == ".xls")
            {
                wb = new NPOI.HSSF.UserModel.HSSFWorkbook();
            }
            else if (extension == ".xlsx")
            {
                wb = new NPOI.XSSF.UserModel.XSSFWorkbook();
            }
            return wb;
        }

        /// <summary>
        /// 获取指定列的最大列宽
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        private int GetWidth(DataTable dt, int columnIndex)
        {
            IList<int> lengths = new List<int>();
            foreach (DataRow dr in dt.Rows)
            {
                lengths.Add(Convert.ToString(dr[columnIndex]).Length * 256);
            }
            return lengths.Max();
        }

        /// <summary>
        /// 获取DataRow的所有列的值的集合
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        private IList<string> GetRowValues(DataRow dr)
        {
            List<string> rowValues = new List<string>();
            for (int i = 0; i < dr.Table.Columns.Count; i++)
            {
                if (dr[i] != null && dr[i] != DBNull.Value)
                {
                    rowValues.Add(dr[i].ToString());
                }
                else
                {
                    rowValues.Add(string.Empty);
                }
            }
            return rowValues;
        }

        /// <summary>
        /// 获取DataTable的列名集合
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private IList<string> GetCloumnNames(DataTable dt)
        {
            List<string> columnNames = new List<string>();
            foreach (DataColumn dc in dt.Columns)
            {
                columnNames.Add(dc.ColumnName);
            }
            return columnNames;
        }

        /// <summary>
        /// 设置Row的Cell值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="values"></param>
        private void SetRow(IRow row, IList<string> values)
        {
            SetRow(row, values, null);
        }

        /// <summary>
        /// 设置Row的Cell值和CellStyle
        /// </summary>
        /// <param name="row"></param>
        /// <param name="values"></param>
        /// <param name="cellStyle"></param>
        private void SetRow(IRow row, IList<string> values, ICellStyle cellStyle)
        {
            for (int i = 0; i < values.Count; i++)
            {
                ICell cell = row.CreateCell(i);
                cell.SetCellValue(values[i]);
                if (cellStyle != null)
                {
                    cell.CellStyle = cellStyle;
                }
            }
        }

        /// <summary>
        /// 获取Cell的样式
        /// </summary>
        /// <param name="wb"></param>
        /// <returns></returns>
        private ICellStyle GetCellStyle(IWorkbook wb)
        {
            return GetCellStyle(wb, FontBoldWeight.None);
        }

        /// <summary>
        /// 获取Cell的样式
        /// </summary>
        /// <param name="wb"></param>
        /// <param name="boldweight"></param>
        /// <returns></returns>
        private ICellStyle GetCellStyle(IWorkbook wb, FontBoldWeight boldweight)
        {
            ICellStyle cellStyle = wb.CreateCellStyle();

            //字体样式
            IFont font = wb.CreateFont();
            font.FontHeightInPoints = 10;
            font.FontName = "微软雅黑";
            font.Color = (short)FontColor.Normal;
            font.Boldweight = (short)boldweight;
            cellStyle.SetFont(font);

            //对齐方式
            cellStyle.Alignment = HorizontalAlignment.Center;
            cellStyle.VerticalAlignment = VerticalAlignment.Center;

            //边框样式
            cellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

            //设置背景色
            cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.White.Index;
            cellStyle.FillPattern = FillPattern.SolidForeground;

            //是否自动换行
            cellStyle.WrapText = false;

            //缩进
            cellStyle.Indention = 0;

            return cellStyle;
        }

        /// <summary>
        /// 自适应列宽
        /// </summary>
        /// <param name="sheet"></param>
        private void AutoSizeColumn(ISheet sheet)
        {
            IRow headRow = sheet.GetRow(0);
            if (headRow != null)
            {
                for (int columnNum = 0; columnNum < headRow.LastCellNum; columnNum++)
                {
                    sheet.AutoSizeColumn(columnNum);
                }
            }
        }

        /// <summary>
        /// 自适应列宽
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="columnNum"></param>
        private void AutoSizeColumn(ISheet sheet, int columnNum)
        {
            int columnWidth = sheet.GetColumnWidth(columnNum) / 256;
            for (int rowNum = 0; rowNum <= sheet.LastRowNum; rowNum++)
            {
                IRow currentRow = sheet.GetRow(rowNum);
                if (currentRow != null)
                {
                    ICell currentCell = currentRow.GetCell(columnNum);

                    if (currentCell != null)
                    {
                        int length = System.Text.Encoding.Default.GetBytes(currentCell.ToString()).Length;
                        if (columnWidth < length)
                        {
                            columnWidth = length;
                        }
                    }
                }
            }
            sheet.SetColumnWidth(columnNum, columnWidth * 256);
        }

        /// <summary>
        /// XLS添加td 用来区分列
        /// </summary>
        /// <param name="argFields">字段</param>
        /// <param name="istext">列是否文本类型</param>
        /// <returns>添加后内容</returns>
        public static string GetXLSFieldsTD(object argFields, bool istext, int rowspan = 1, int colspan = 1)
        {
            #region 它类型如是特殊 没内容用空代替
            if (null == argFields)
            {
                argFields = string.Empty;
            }
            else
            {
                switch (argFields.GetType().ToString())
                {
                    case "System.DateTime"://是日期
                        DateTime? dttime = Convert.ToDateTime(argFields);
                        argFields = (dttime == null || dttime.Equals("0001/1/1 0:00:00")) ? "" : argFields;//它没日期则 给他一个空字符
                                                                                                           //istext = true;//日期 让文本列显示
                        break;
                }
            }
            #endregion

            string strstyle = istext ? " style='vnd.ms-excel.numberformat:@'" : "";
            return string.Format("<td{0} {2} {3}>{1}</td>", strstyle, argFields, colspan > 1 ? "colspan = '" + colspan + "'" : "", rowspan > 1 ? "rowspan='" + rowspan + "'" : "");
        }
    }
}