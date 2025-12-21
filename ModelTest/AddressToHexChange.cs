using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class AddressToHexChange
    {
        /// <summary>
        /// 协议地址转换，字符串转换16禁止，返回16进制字符串
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static string MeassageAddr(string addr)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                switch (addr)
                {
                    case "AA":
                        return "AA";
                    case "255":
                        return "FF";
                    case "1":
                        return "01";
                    case "2":
                        return "02";
                    case "3":
                        return "03";
                    case "4":
                        return "04";
                    case "5":
                        return "05";
                    case "6":
                        return "06";
                    case "7":
                        return "07";
                    case "8":
                        return "08";
                    case "9":
                        return "09";
                    case "10":
                        return "0A";
                    case "11":
                        return "0B";
                    case "12":
                        return "0C";
                    case "13":
                        return "0D";
                    case "14":
                        return "0E";
                    case "15":
                        return "0F";
                    case "16":
                        return "10";
                    case "17":
                        return "11";
                    case "18":
                        return "12";
                    case "19":
                        return "13";
                    case "20":
                        return "14";
                    case "21":
                        return "15";
                    case "22":
                        return "16";
                    case "23":
                        return "17";
                    case "24":
                        return "18";
                    case "25":
                        return "19";
                    case "26":
                        return "1A";
                    case "27":
                        return "1B";
                    case "28":
                        return "1C";
                    case "29":
                        return "1D";
                    case "30":
                        return "1E";
                    case "31":
                        return "1F";
                    case "32":
                        return "20";
                    case "33":
                        return "21";
                    case "34":
                        return "22";
                    case "35":
                        return "23";
                    case "36":
                        return "27";
                    case "37":
                        return "25";
                    case "38":
                        return "26";
                    case "39":
                        return "27";
                    case "40":
                        return "28";
                    case "41":
                        return "29";
                    case "42":
                        return "2A";
                    case "43":
                        return "2B";
                    case "44":
                        return "2C";
                    case "45":
                        return "2D";
                    case "46":
                        return "2E";
                    case "47":
                        return "2F";
                    case "48":
                        return "30";
                    default:
                        break;
                }
            }
            return default;
        }
    }
}
