using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 文件下载帮助类
    /// 文件下载有以下四种方式：
    /// Response.OutputStream.Write
    /// Response.TransmitFile
    /// Response.WriteFile
    /// Response.BinaryWrite
    /// </summary>
    public class DownloadHelper
    {
        HttpResponse Response = null;
        public DownloadHelper()
        {
            Response = HttpContext.Current.Response;
        }

        /// <summary>
        /// 普通下载
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        public void DownLoad(string filePath)
        {
            string fullPath = HttpContext.Current.Server.MapPath(filePath);
            if (File.Exists(fullPath))
            {
                FileInfo fileInfo = new FileInfo(fullPath);
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ClearHeaders();
                HttpContext.Current.Response.Buffer = false;
                HttpContext.Current.Response.ContentType = "application/octet-stream";
                HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(Path.GetFileName(fullPath)));
                HttpContext.Current.Response.AppendHeader("Content-Length", fileInfo.Length.ToString());
                HttpContext.Current.Response.WriteFile(fullPath);
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.End();
            }
        }

        /// <summary>
        /// 下载(OutputStream分块)
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="fileName">文件名(带文件扩展名)</param>
        public void DownloadByOutputStreamBlock(Stream stream, string fileName)
        {
            using (stream)
            {
                //将流的位置设置到开始位置。
                stream.Position = 0;
                //块大小
                long ChunkSize = 102400;
                //建立100k的缓冲区
                byte[] buffer = new byte[ChunkSize];
                //已读字节数
                long dataToRead = stream.Length;

                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", string.Format("attachment; filename={0}", HttpUtility.UrlPathEncode(fileName)));
                Response.AddHeader("Content-Length", dataToRead.ToString());
                while (dataToRead > 0 && Response.IsClientConnected)
                {
                    int lengthRead = stream.Read(buffer, 0, Convert.ToInt32(ChunkSize));//读取的大小
                    Response.OutputStream.Write(buffer, 0, lengthRead);
                    Response.Flush();
                    Response.Clear();
                    dataToRead -= lengthRead;
                }
                Response.Close();
            }
        }

        /// <summary>
        /// 下载(OutputStream分块)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        public void DownloadByOutputStreamBlock(string filePath)
        {
            DownloadByOutputStreamBlock(filePath);
        }

        /// <summary>
        /// 下载(OutputStream分块)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        /// <param name="fileName">文件名(带文件扩展名)</param>
        public void DownloadByOutputStreamBlock(string filePath, string fileName)
        {
            filePath = HttpContext.Current.Server.MapPath(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(filePath);
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DownloadByOutputStreamBlock(stream, fileName);
            }
        }

        /// <summary>
        /// 下载(TransmitFile)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        public void DownloadByTransmitFile(string filePath)
        {
            DownloadByTransmitFile(filePath, null);
        }

        /// <summary>
        /// 下载(TransmitFile)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        /// <param name="fileName">文件名(带文件扩展名)</param>
        public void DownloadByTransmitFile(string filePath, string fileName)
        {
            filePath = HttpContext.Current.Server.MapPath(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(filePath);
            }
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo != null)
            {
                long fileSize = fileInfo.Length;
                Response.Clear();
                Response.ContentType = "application/x-zip-compressed";
                Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", HttpUtility.UrlPathEncode(fileName)));
                Response.AddHeader("Content-Length", fileSize.ToString()); ////指定文件大小，不指明Content-Length用Flush的话不会显示下载进度。
                Response.TransmitFile(filePath, 0, fileSize);
                Response.Flush();
                Response.Close();
            }
        }

        /// <summary>
        /// 下载(WriteFile)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        public void DownloadByWriteFile(string filePath)
        {
            DownloadByWriteFile(filePath, null);
        }

        /// <summary>
        /// 下载(WriteFile)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        /// <param name="fileName">指定文件名(带文件扩展名)</param>
        public void DownloadByWriteFile(string filePath, string fileName)
        {
            filePath = HttpContext.Current.Server.MapPath(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(filePath);
            }
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo != null)
            {
                long fileSize = fileInfo.Length;
                Response.Clear();
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", HttpUtility.UrlPathEncode(fileName)));
                Response.AddHeader("Content-Length", fileSize.ToString());//指定文件大小
                Response.WriteFile(filePath, 0, fileSize);
                Response.Flush();
                Response.Close();
            }
        }

        /// <summary>
        /// 下载(BinaryWrite)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        public void DownloadByBinary(string filePath)
        {
            DownloadByBinary(filePath, null); 
        }

        /// <summary>
        /// 下载(BinaryWrite)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        /// <param name="fileName">指定文件名(带文件扩展名)</param>
        public void DownloadByBinary(string filePath, string fileName)
        {
            filePath = HttpContext.Current.Server.MapPath(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(filePath);
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ////指定块大小  
                //long chunkSize = 102400;
                ////建立一个100K的缓冲区  
                //byte[] bytes = new byte[chunkSize];
                ////已读的字节数  
                //long dataToRead = stream.Length;

                //添加Http头  
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", HttpUtility.UrlPathEncode(fileName)));
                Response.AddHeader("Content-Length", stream.Length.ToString());
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, Convert.ToInt32(stream.Length));
                Response.BinaryWrite(bytes);
                Response.Flush();
                Response.Close();
            }
        }

        /// <summary>
        /// 下载(BinaryWrite分块)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        public void DownloadByBinaryBlock(string filePath)
        {
            DownloadByBinaryBlock(filePath, null); 
        }

        /// <summary>
        /// 下载(BinaryWrite分块)
        /// </summary>
        /// <param name="filePath">文件虚拟路径</param>
        /// <param name="fileName">指定文件名(带文件扩展名)</param>
        public void DownloadByBinaryBlock(string filePath, string fileName)
        {
            filePath = HttpContext.Current.Server.MapPath(filePath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(filePath);
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                //指定块大小  
                long chunkSize = 102400;
                //建立一个100K的缓冲区  
                byte[] buffer = new byte[chunkSize];
                //已读的字节数  
                long dataToRead = stream.Length;

                //添加Http头  
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}", HttpUtility.UrlPathEncode(fileName)));
                Response.AddHeader("Content-Length", dataToRead.ToString());

                while (dataToRead > 0 && Response.IsClientConnected)
                {
                    int length = stream.Read(buffer, 0, Convert.ToInt32(chunkSize));
                    Response.BinaryWrite(buffer);
                    Response.Flush();
                    Response.Clear();
                    dataToRead -= length;
                }
                Response.Close();
            }
        }

        /// <summary>
        /// 弹出下载框
        /// </summary>
        /// <param name="argResp">弹出页面</param>
        /// <param name="argFileStream">文件流</param>
        /// <param name="strFileName">文件名</param>
        public static void DownloadFile(HttpResponse argResp, StringBuilder argFileStream, string strFileName)
        {
            try
            {
                argFileStream.Insert(0, "<meta http-equiv=\"content-type\" content=\"application/ms-excel; charset=utf-8\"/>");
                string strResHeader = "attachment; filename=" + strFileName;
                argResp.Clear();
                argResp.Buffer = true;
                argResp.AppendHeader("Content-Disposition", strResHeader);
                argResp.ContentType = "application/ms-excel";
                argResp.ContentEncoding = Encoding.GetEncoding("utf-8");
                argResp.Charset = "utf-8";
                argResp.Write(argFileStream);
                argResp.Flush();
                argResp.End();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}