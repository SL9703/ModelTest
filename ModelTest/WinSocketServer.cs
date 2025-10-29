using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class WinSocketServer
    {
        public byte[] RandNum;
        /// <summary>
        /// 登录加密机服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="Ctime"></param>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int ConnectDevice(string ip, string port, string Ctime);

        public int ConnectDeviceEx(string ip, string port, string Ctime)
        {
            try
            {
                return ConnectDevice(ip, port, Ctime);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 产生随机数
        /// </summary>
        /// <param name="InRand"></param>
        /// <param name="OutRand"></param>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int CreateRand([In,Out] int InRand, [Out] byte[] OutRand);
        //byte[] OutRand = new byte[128];
        public int CreateRandEx(int InRand, [Out] byte[] OutRand)
        {
            
            try
            {
                RandNum = new byte[128];
                int result =  CreateRand(InRand, OutRand);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [DllImport("WinSocketServer.dll")]
        private static extern int RESAM_Formal_GetKeyData_AppLayer([In, Out] 
        int iOperateMode,string cTESAMID,string cSessionKey, int cTaskType, string cTaskData,
        [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        public int ReSAM_Formal_GetKeyData_AppLayer(
            int iOperateMode, string cTESAMID, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC)
        {
            try
            {
                return RESAM_Formal_GetKeyData_AppLayer(iOperateMode, cTESAMID, cSessionKey, cTaskType, cTaskData,
                 cOutSID, cOutAttachData, cOutData, cOutMAC);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_SetESAMData([In, Out]
            int InKeyState,
            int InOperateMode,
            string cESAMNO,
            string cSessionKey,
            string cMeterNo,
            string cESAMRand,
            string cData,
           [Out] byte[] OutSID,
           [Out] byte[] OutAddData,
           [Out] byte[] OutData,
           [Out] byte[] OutMAC);
        public int Call_Obj_Meter_Formal_SetESAMData(int InKeyState, int InOperateMode, string cESAMNO, string cSessionKey,
            string cMeterNo, string cESAMRand, string cData, [Out] byte[] OutSID, [Out] byte[] OutAddData,
            [Out] byte[] OutData, [Out] byte[] OutMAC)
        {
            try
            {
                return Obj_Meter_Formal_SetESAMData(InKeyState, InOperateMode, cESAMNO, cSessionKey,
             cMeterNo, cESAMRand, cData, OutSID, OutAddData,
             OutData, OutMAC);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 断开密码机
        /// </summary>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int CloseDevice();
        public int CloseDeviceEx()
        {
            try
            {
                return CloseDevice();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 释放服务器登录权限，兼容 09 版电能表使用的函数。
        /// </summary>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int ClseUsbkey();
        public int ClseUsbkeyEx()
        {
            try
            {
                return ClseUsbkeyEx();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
