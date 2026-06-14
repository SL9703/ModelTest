using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Tools
{
    public class Confighelper
    {
        [DllImport("kernel32")]
        private static extern long GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filepath);
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);
        /// <summary>
        /// 读配置文件
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="def"></param>
        /// <param name="size"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static String ReadIni(string section, string key, string def, int size, string filepath)
        {
            StringBuilder sb = new StringBuilder();
            long l = GetPrivateProfileString(section, key, def, sb, size,  filepath);
            return sb.ToString();
        }
        /// <summary>
        /// 写入数据到配置文件
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static long WriteIni(string section, string key, string val, string filepath)
        {
            return WritePrivateProfileString(section, key, val, filepath);
        }
        // 删除ini文件下所有段落
        public void ClearAllSection(string filepath)
        {
            WriteIni(null, null, null, filepath);
        }
        //删除ini文件下personal段落下的所有键
        public void ClearSection(string Section, string filepath)
        {
            WriteIni(Section, null, null, filepath);
        }


        /// <summary>
        /// 获取某个指定节点(Section)中所有KEY和Value
        /// </summary>
        /// <param name="lpAppName">节点名称</param>
        /// <param name="lpReturnedString">返回值的内存地址,每个之间用\0分隔</param>
        /// <param name="nSize">内存大小(characters)</param>
        /// <param name="lpFileName">Ini文件</param>
        /// <returns>内容的实际长度,为0表示没有内容,为nSize-2表示内存大小不够</returns>
        [DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]

        private static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);
        /// <summary>
        /// 获取INI文件指定Section节点下的数据，并返回 Dictionary 对象
        /// </summary>
        /// <param name="iniFile">ini文件路径</param>
        /// <param name="section">section节点</param>
        /// <returns></returns>
        public static Dictionary<string, string> GetSectionDic(string iniFile, string section)
        {
            uint MAX_BUFFER = 32767;
            string[] items = null;
            //分配内存
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER * sizeof(char));
            uint bytesReturned = GetPrivateProfileSection(section, pReturnedString, MAX_BUFFER, iniFile);
            if (!(bytesReturned == MAX_BUFFER - 2) || (bytesReturned == 0))
            {
                string returnedString = Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned);
                items = returnedString.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            }
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pReturnedString);     //释放内存
                                                                                       //无结果返回空
            if (items == null)
            {
                return null;
            }
            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (string item in items)
            {
                if (!item.Contains("="))
                {
                    continue;
                }
                string[] part = item.Split('=');
                dic.Add(part[0], part[1]);
            }
            return dic;
        }
    }
}
