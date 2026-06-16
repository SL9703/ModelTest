using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelTest
{
    public sealed class WinSocketServiceInvoker
    {
        private readonly WinSocketServer _server;
        private readonly Action<string> _log;

        public sealed class ServerLoginResult
        {
            public bool Connected { get; init; }
            public int ConnectCode { get; init; }
            public int RandCode { get; init; }
            public string RandText { get; init; } = string.Empty;
        }

        public sealed class IdentityHeartbeatResult
        {
            public int Code { get; init; }
            public string OutRand { get; init; } = string.Empty;
            public string OutEndata { get; init; } = string.Empty;
        }

        private static readonly Dictionary<string, string> ServiceDescriptions = new()
        {
            ["RESAM_Formal_GetKeyData_AppLayer"] = "int iOperateMode,char * cTESAMID, char * cSessionKey,int cTaskType, char * cTaskData, char * cOutSID,char * cOutAttachData, char * cOutData ,char * cOutMAC",
            ["CloseDevice"] = "连接密码机，用于断开服务器或密码机连接；无参数",
            ["ClseUsbkey"] = "释放服务器登录权限，兼容 09 版电能表使用的函数；无参数",
            ["Meter_Formal_DataClear1"] = "电表清零函数，用于远程费控电能表清零；输入参数：int Flag(0:公钥;1:私钥;10:双协议公钥;11:双协议私钥), char *PutRand(随机数2，电表身份认证成功返回，4字节), char *PutDiv(分散因子，8字节，“0000”+表号), char *PutData(清零数据)；输出参数：char *OutData(20字节密文)",
            ["Meter_Formal_DataClear2"] = "事件或需量清零函数，用于电能表事件或需量清零；输入参数：int Flag(13版：0公钥、1私钥；16版：1私钥；10面向对象公钥；11面向对象私钥), char *PutRand(随机数2，电表身份认证成功返回，4字节), char *PutDiv(分散因子，8字节，“0000”+表号), char *PutData(清零数据)；输出参数：char *OutData(20字节密文)",
            ["Meter_Formal_IdentityAuthentication"] = "电表身份认证；输入参数：int Flag(0:公钥状态;1:私钥状态), char *PutDiv(分散因子，长度16，“0000”+表号)；输出参数：char *OutRand(随机数1，长度16), char *OutEndata(密文，长度16)",
            ["Meter_Formal_UserControl"] = "用户获取控制命令密文；输入参数：int Flag(0:公钥状态;1:私钥状态，需要特殊授权), char *PutRand(随机数2，长度8), char *PutDiv(分散因子，长度16，“0000”+表号), char *PutESAMNo(ESAM序列号，复位信息后8字节，长度16), char *PutData(跳闸或合闸控制命令明文)；输出参数：char *OutEndata(密文)",
            ["Meter_Formal_ParameterUpdate"] = "电能表一类参数 MAC 计算；输入参数：int Flag(0:公钥状态;1:私钥状态，需要特殊授权), char *PutRand(随机数2，长度8), char *PutDiv(分散因子，长度16，“0000”+表号), char *PutApdu(指令数据，长度10，面向对象命令头832A84+1字节起始+2字节LC), char *PutData(一类参数明文)；输出参数：char *OutEndata(明文数据+MAC，长度=明文长度+8)",
            ["Meter_Formal_ParameterElseUpdate"] = "远程二类参数设置加密；输入参数：int Flag(0:公钥状态;1:私钥状态，需要特殊授权), char *PutRand(随机数2，长度8), char *PutDiv(分散因子，长度16，“0000”+表号), char *PutApdu(指令数据，长度10，面向对象命令头8012C083+2字节LC), char *PutData(二类参数明文)；输出参数：char *OutEndata(密文)",
            ["Meter_Formal_InfraredAuth"] = "红外认证，用于获取红外认证密文和随机数2，红外认证前先进行红外查询；输入参数：int Flag(0:公钥状态), char *PutDiv(8字节分散因子，“0000”+表号), char *PutESAMNo(8字节ESAM序列号，电能表红外查询命令返回), char *PutRand1(8字节随机数1，创建随机数函数返回), char *PutRand1Endata(8字节随机数1密文，电能表红外查询命令返回)；输出参数：char *PutRand2(8字节随机数2), char *OutRand2Endata(8字节随机数2密文)",
            ["Meter_Formal_MacCheck"] = "数据回抄 MAC 校验；输入参数：int Flag(0:公钥状态;1:私钥状态), char *PutRand(随机数1高4字节), char *PutDiv(8字节分散因子，“0000”+表号), char *PutApdu(5字节命令头，04D686+起始地址+Len，Len为数据长度+0x0C；钱包状态查询可用8048000000), char *PutData(数据回抄返回数据), char *PutMac(4字节数据回抄返回MAC)",
            ["Meter_Formal_KeyUpdateV2"] = "密钥更新函数，用于2013标准电能表远程密钥更新；输入参数：int PutKeySum(密钥总条数，固定20), char *PutKeystate(01密钥下装;00密钥恢复，需要特殊授权), char *PutKeyid(密钥编号0x00-0x13，每次最多4条，如00010203), char *PutRand(随机数2，4字节), char *PutDiv(8字节分散因子，“0000”+表号), char *PutESAMNo(8字节ESAM序列号), char *PutChipInfor(芯片发行信息文件001A数据，005AH字节，不含MAC)；输出参数：char *OutData(4*(4字节密钥信息+32字节密钥密文)+4字节MAC)",
            ["Obj_Meter_Formal_SetESAMData"] = "设置ESAM参数，用于设置表号、当前套电价文件、备用套电价文件、ESAM存储标识等ESAM中存储的数据；输入参数：int InKeyState, int InOperateMode, char *cESAMNO(可为空), char *cSessionKey, char *cMeterNo(8Byte表号，不足前补0), char *cESAMRand, char *cData(4Byte OAD + 1Byte内容LEN + 内容)；输出参数：char *OutSID, char *OutAddData, char *OutData, char *OutMAC",
            ["Obj_Meter_Formal_GetESAMData"] = "抄读ESAM参数，用于抄读表号、当前套电价文件、备用套电价文件、ESAM存储标识等ESAM数据；输入参数：int iKeyState, int iOperateMode, char *cMeterNo(8Byte表号，不足前补0), char *cOAD；输出参数：char *cOutRandHost, char *cOutSID, char *cOutAttachData",
            ["Obj_Meter_Formal_VerifyESAMData"] = "抄读ESAM参数验证，用于验证从ESAM中抄读的表号、电价文件、存储标识等数据；输入参数：int InKeyState, int iOperateMode(目前只支持1), char *cESAMNO, char *cMeterNo(8Byte表号，不足前补0), char *cRandHost, char *cOAD, char *cTaskData(明文或密文数据), char *cMAC(4Byte MAC)；输出参数：char *cOutData(明文数据)",
            ["Obj_Meter_Formal_GetPurseData"] = "钱包操作，用于钱包初始化、充值、退费；输入参数：int iOperateMode, char *cESAMNO(可为空), char *cSessionKey, int cTaskType(任务序编号：9钱包初始化、10钱包充值、11钱包退费), char *cTaskData(数据明文，包含预置金额4Byte)；输出参数：char *cOutSID, char *cOutAttachData, char *cOutData, char *cOutMAC",
            ["Obj_Meter_Formal_GetMeterSetData"] = "获取电能表任务数据；输入参数：int iOperateMode, char *cESAMNO, char *cSessionKey, char *cTaskData(数据明文NBytes)；输出参数：char *cOutSID, char *cOutAttachData, char *cOutData, char *cOutMAC",
            ["Obj_Meter_Formal_GetGrpBrdCstData"] = "获取电能表广播数据；输入参数：int iKeyState, int iOperateMode, char *cESAMNO, char *cGrpKID, char *cDIV(组地址8Bytes), char *AGSEQ(4Bytes), char *cBrdCstData(广播明文)；输出参数：char *cOutSID, char *cOutAttachData, char *cOutData, char *cOutMac",
            ["Obj_Meter_Formal_GetTrmKeyData"] = "电能表对称密钥更新；输入参数：int iKeyState(1更新到私钥，0恢复到初始密钥), char *cESAMNO, char *cSessionKey, char *cMeterNo(8Byte表号), char *cKeyType(密钥类型，00应用密钥)；输出参数：char *cOutSID, char *cOutAttachData, char *cOutTrmKeyData, char *cOutMAC",
            ["Obj_Meter_Formal_InitTrmKeyData"] = "电表对称密钥初始化；输入参数：int iKeyState, char *cESAMNO, char *cSessionKey, char *cMeterNo(8Bytes表号), char *cKeyType(00应用密钥)；输出参数：char *cOutSID, char *cOutAttachData, char *cOutTrmKeyData, char *cOutMAC",
            ["Obj_Meter_Formal_GetESAMFileData"] = "抄读ESAM中文件的数据；输入参数：int InKeyState, int iOperateMode(目前只支持1), char *MeterNo(8Byte表号，不足前补0), char *FileName(2Byte文件名), char *FileLenth(2Byte读取长度)；输出参数：char *cOutRandHost, char *cOutSID, char *cOutAttachData",
            ["Obj_Terminal_Formal_GetTrmKeyData"] = "char* iKeyVersion 密钥更新的目标状态 “00000000000000000000000000000000” 表示恢复到公钥，其他相同长度非全零数据表示更新到私钥\r\nchar* strEsamNo ESAM 序列号\r\nchar* strSessionKey 会话密钥\r\nchar* cTerminalAddress 终端地址(8 Bytes)\r\nchar* strKeyType 密钥类型，00 应用密钥，01 链路密钥",
            ["Obj_Terminal_Formal_InitSession"] = "输入参数:iKeyState，cTESAMID，cASCTR，cFLG，cMasterCert",
            ["Obj_Terminal_Formal_InitSession_RH"] = "输入参数:int iKeyState，string cTESAMID，string cASCT，string cMasterCert",
            ["Obj_Terminal_Formal_VerifySession_RH"] = "输入参数:int iKeyState, char *cTESAMNO, char *cRandHost, char *cSessionData, char *cSign, char *cTerminalCert",
            ["Obj_Terminal_Formal_GetSessionData_RH"] = "输入参数:char *cTESAMNO, int iOperateMode, char *KeyID, char *cSessionKey, int cTaskType, char *cTaskData",
            ["Obj_Terminal_Formal_VerifyData_RH"] = "输入参数:int iKeyState, int iOperateMode, char *cTESAMNO, char *cSessionKey, char *cTerminalCert, char *cTaskData, char *cSign",
            ["Obj_Terminal_Formal_GetTrmKeyData_RH"] = "输入参数:int iKeyState, char *cTESAMNO, char *cIV, char *InKid, char *InCert",
            ["Obj_Terminal_Formal_GetCACertificateData_RH"] = "输入参数:int iKeyState, char *cTESAMNO, char *cSessionKey, int iCertType, char *cCert",
            ["Obj_Terminal_Formal_GetSessionData"] = "输入参数:int iOperateMode, char cTeasmid, char cSessionKey, int cTaskType, char cTaskData",
            ["Obj_Terminal_Formal_GetTerminalSetData"] = "输入参数:int iOperateMode, char cTeasmid, char cSessionKey, char cTaskData",
            ["Obj_Terminal_Formal_GetGrpBrdCstData"] = "输入参数:int iKeyState, int iOperateMode, char cTESAMID, char cGrpKID, char cDIV, char aGSEQ, char cBrdCstData",
            ["Obj_Terminal_Formal_VerifyTerminalData"] = "输入参数:int ikeyState, int iOperateMode, char cTeasmid, char cSessionKey, char cTaskData, char cMac",
            ["Obj_Terminal_Formal_VerifyReadData"] = "输入参数:int iKeyState, int iOperateMode, char cTESAMID, char cRandHost, char cReadData, char cMac, char cOutData, char cOutRSPCTR",
            ["Obj_Terminal_Formal_VerifyReportData"] = "输入参数:int iKeyState, int iOperateMode, char cTESAMID, char cRandT, char cReportData, char cMac",
            ["Obj_Terminal_Formal_GetResponseData"] = "输入参数:int iKeyState, int iOperateMode, char cTESAMID, char cRandT, char cReportData",
            ["Obj_Terminal_Formal_InitTrmKeyData"] = "终端对称密钥初始化；输入参数:int iKeyState, char *cTESAMID, char *cSessionKey, char *cTerminalAddress(8Bytes), char *cKeyType(00应用密钥)",
            ["Obj_Terminal_Formal_GetCACertificateData"] = "获取更新证书信息；输入参数:int iKeyState, char *cTESAMID, char *cSessionKey, char *cCerType(证书类型)",
            ["Obj_Send_Formal_DataForGetKey"] = "输入参数:string InDeviceType, string cTastType, string cKeyState, string cTeasmid, string InMeterNo, string cSessionKey",
            ["Terminal_Formal_GetR1"] = "获取随机数1；无输入参数；输出参数：char *OutR1(16字节随机数)",
            ["Terminal_Formal_SessionInitRec"] = "会话初始化或恢复；输入参数：char *PutState(00第一套密钥/01第二套密钥), char *PutTESAMNo(8字节TESAM序列号), char *PutVersionNum(1字节版本号), char *PutSessionID(00新建注册/01恢复), char *PutR1(16字节随机数1)；输出参数：char *OutMasterCertificate(主站证书), char *OutEncR1(16字节随机数1密文), char *OutMac(4字节MAC), char *OutSign1(64字节签名)",
            ["Terminal_Formal_SessionKeyConsult"] = "会话密钥协商；输入参数：char *PutState, char *PutTESAMNo, char *PutVersionNum, char *PutSessionID, char *PutCRLCertificateNo(16字节), char *PutMasterCertificateNo(16字节), char *PutTerminalCertificate, char *PutEncR2, char *PutSign2, char *PutR1；输出参数：char *OutEncM1, char *OutSign3, char *OutMac2, char *OutSign4",
            ["Terminal_Formal_SessionConsultVerify"] = "会话协商验证；输入参数：char *PutR3(16字节随机数3), char *PutMac3(4字节MAC)",
            ["Terminal_Formal_SessionRecoveryVerify"] = "会话恢复验证；输入参数：char *PutState, char *PutTESAMNo, char *PutVersionNum, char *PutSessionID, char *PutEncR2, char *PutR3, char *PutMac",
            ["Terminal_Formal_MACVerify"] = "单地址数据MAC计算；输入参数：char *PutState, char *PutTESAMNo, char *PutFnType(01复位/04设参/05控制/10数据转发/0F文件传输), char *PutData；输出参数：char *OutMac(4字节MAC)",
            ["Terminal_Formal_ExternalAuth"] = "内外部认证；输入参数：char *PutState, char *PutTESAMNo, char *PutR4, char *PutEncR4, char *PutR5；输出参数：char *OutEncR5(16字节随机数5密文)",
            ["Terminal_Formal_CertificateStateChange"] = "证书状态切换；输入参数：char *PutState, char *PutTESAMNo, char *PutCertificateState(00测试证书/01正式证书), char *PutR6；输出参数：char *OutEncR6, char *OutMac",
            ["Terminal_Formal_SetOfflineCounter"] = "设置离线计数器；输入参数：char *PutState, char *PutTESAMNo, char *PutCounter(4字节计数器)；输出参数：char *OutEncCounter(20字节密文)",
            ["Terminal_Formal_ChangeDataAuthorize"] = "转加密授权；无输入参数；输出参数：char *OutData(32字节转加密授权数据)",
            ["Terminal_Formal_GetCipherMeterKey"] = "获取电表密钥密文；输入参数：char *PutMeterState(1字节，00公开密钥/01交易密钥), char *PutMeterNo(0000+电表表号，共8字节), int PutTaskType(0身份认证/1对时/2红外认证)；输出参数：char *OutMeterEncKey(32字节密钥密文)",
            ["Terminal_Formal_EncTaskData"] = "任务数据加密；输入参数：int PutInDataType(对时任务当前为0), char *PutTaskData(任务数据，小于2K)；输出参数：char *OutTaskData(任务密文)",
            ["Terminal_Formal_GroupBroadcast"] = "组广播数据MAC计算；输入参数：char *PutState, char *PutTESAMNo, char *PutFnType, int PutOutDataType(0明文MAC/1密文/2密文+MAC), char *PutGroupAdrass(2字节组地址), char *PutMtime(6字节，默认130202224622), char *PutBroadcastData；输出参数：char *OutMac",
            ["Terminal_Formal_SystemBroadcast"] = "系统广播数据MAC计算；输入参数同组广播，TESAM密钥和向量不同；输出参数：char *OutMac",
            ["Terminal_Formal_SymmetricKeyUpdate"] = "终端对称密钥修改；输入参数：char *PutState(00第一套/01第二套), char *PutTESAMNo；输出参数：char *OutKeyNum, char *OutEncKeyData",
            ["Terminal_Formal_CACertificateUpdate"] = "证书更新；输入参数：char *PutCertificateState(00测试证书/01正式证书交易状态/11恢复正式证书初始状态), char *PutCertificateType(01 CRL证书)；输出参数：char *OutEncCertificateData",
            ["Terminal_Formal_SymmetricKeyUpdateCT"] = "巡检仪终端对称密钥更新；输入参数：char *PutState, char *PutTESAMNo；输出参数：char *OutKeyNum, char *OutEncKeyData",
            ["CT_Terminal_Formal_GetCTESAMKey"] = "回路状态巡检仪密钥更新；输入参数：int InFlag, char *InKeyid, char *InCTTESAMNo, char *InCTESAMNo, char *InRand；输出参数：char *OutKey, char *OutMac",
            ["CT_Terminal_Formal_CalCTESAMMac"] = "主站对巡检仪下发报文MAC计算；输入参数：int InFlag, char *InCTESAMNo, char *InRand, char *InData；输出参数：char *OutMac",
            ["CT_Terminal_Formal_CalCTTESAMMac"] = "主站对向终端下发报文MAC计算；输入参数：char *PutState, char *InCTTESAMNo, char *InRand, char *InData；输出参数：char *OutMac",
            ["CT_Terminal_Formal_CalVerifyCTESAMMac"] = "巡检仪查询数据的MAC验证；输入参数：int InFlag, char *InCTESAMNo, char *InRand, char *InData, char *InMac",
            ["CT_Terminal_Formal_CalVerifyCTTESAMMac"] = "终端查询数据的MAC验证；输入参数：char *PutState, char *InCTTESAMNo, char *InRand, char *InData, char *InMac",
            ["RDID_Formal_RFIDChangeKey"] = "超高频标签密钥更新；输入参数：char *PutKeyState, char *PutDiv；输出参数：char *OutKey(16字节)",
            ["RDID_Formal_RFIDEncrptData"] = "超高频标签数据加密和MAC计算；输入参数：char *PutKeyState, char *PutDiv, char *PutData, char *PutOPInfor；输出参数：char *OutEncData(16字节), char *OutMAC2(4字节)",
            ["RDID_Formal_RFIDDisEncrptData"] = "超高频标签数据解密和MAC验证；输入参数：char *PutKeyState, char *PutDiv, char *PutEncData, char *PutOPInfor, char *PutMAC；输出参数：char *OutData",
            ["RDID_Formal_RFIDCheckData"] = "超高频标签数据MAC校验；输入参数：char *InKeyState, int InDataNo, char *PutDiv, char *PutData, char *PutMac",
            ["RH_InternalSign"] = "主站签名；输入参数：int sConnect=0, char *cTESAMID, int iKeyIndex=1, char *cCert, char *inData；输出参数：char *OutSign",
            ["RH_VerifySig"] = "主站验证签名；输入参数：int sConnect=0, char *cTESAMID, char *cCert, char *inData, char *InSign",
            ["RH_EncData"] = "安全传输加密；输入参数：int sConnect=1, char *cTESAMID, char *cKeyVer, char *cKeyIndex=0001, char *cDivData, char *cIvData, char *cInData；输出参数：char *cOutEncData, char *cOutMac",
            ["RH_DisEncData"] = "安全传输解密；输入参数：int sConnect=1, char *cTESAMID, char *cKeyVer, char *cKeyIndex=0001, char *cDivData, char *cIvData, char *cInEncData, char *cInMac；输出参数：char *cOutData",
            ["RH_Terminal_Formal_GetTrmKeyData"] = "配电终端对称密钥更新；输入参数：int iKeyState, char *cTESAMID, char *cIV, char *InKid=0001, char *InCert；输出参数：char *ucOutTrmKeyData, char *cOutSign",
            ["Obj_Meter_Formal_InitSession"] = "主站会话协商，用于建立应用连接时产生密文1和客户机签名1；输入参数：int iKeyState, char *cDiv(8Byte分散因子，iKeyState=0为芯片序列号，iKeyState=1为表号), char *cASCTR, char *cFLG(应用密钥产生标识，1Byte，默认01)；输出参数：char *cOutRandHost(16Byte主站随机数), char *cOutSessionInit(会话协商数据/密文1), char *cOutSign(4Byte客户机签名1)",
            ["Obj_Meter_Formal_VerifySession"] = "主站会话协商验证，用于验证设备返回的会话协商数据并产生会话密钥；输入参数：int iKeyState, char *cDiv(8Byte分散因子，iKeyState=0为芯片序列号，iKeyState=1为表号且不足8字节前补0), char *cRandHost(16Byte主站随机数R1), char *cSessionData(48Byte终端返回密文2), char *cSign(4Byte终端返回签名2)；输出参数：char *cOutSessionKey(会话密钥)",
            ["Obj_Meter_Formal_VerifyReadData"] = "抄读数据验证，主站验证电能表返回的抄读数据；输入参数：int iKeyState, int iOperateMode, char *cMeterNo, char *cRandHost(16Byte主站随机数), char *cReadData(抄读数据), char *cMac(MAC数据)；输出参数：char *cOutData(明文抄读数据，iOperateMode=1时为空)",
            ["Obj_Meter_Formal_VerifyReportData"] = "上报数据验证，设备主动上报数据时主站验证数据合法性；输入参数：int iKeyState(=1), int iOperateMode, char *cMeterNo, char *cRandT(12B终端随机数), char *cReportData(上报数据), char *cMac(MAC数据)；输出参数：char *cOutData(明文数据，iOperateMode=1时为空), char *cOutRSPCTR(主动上报随机数)",
            ["Obj_Meter_Formal_GetResponseData"] = "上报数据返回报文加密，用于设备主动上报时主站返回帧数据加密计算；输入参数：int iKeyState(=1), int iOperateMode, char *cMeterNo, char *RandHost(上报随机数，12Byte), char *cReportData(上报数据)；输出参数：char *OutSID, char *OutAttachData, char *cOutData, char *ucOutMac",
            ["Obj_Meter_Formal_GenReadData"] = "输入参数:string _iKeyVersion,string _strEsamNo,string _strMeterNo,string _iOperateMode,string _randHost,string _cReadData",
            ["Obj_Meter_Formal_GetSessionData"] = "安全传输加密，用于具体业务数据加密计算；输入参数：int iOperateMode, char *cESAMNO(可为空), char *cSessionKey(会话密钥), int cTaskType(2设置会话失效门限/4安全模式参数/5时区费率时段对时/8拉闸任务/3其它数据加密), char *cTaskData(数据明文NByte)；输出参数：char *cOutSID, char *cOutAttachData, char *cOutData, char *cOutMAC",
            ["Obj_Meter_Formal_VerifyMeterData"] = "安全传输解密，用于电能表返回帧数据解密验证；输入参数：int iKeyState(=1), int iOperateMode, char *cESAMNO(可为空), char *cSessionKey, char *cTaskData(数据), char *cMac(MAC数据)；输出参数：char *cOutData(数据明文)",
            ["Obj_Terminal_Formal_GetSessionDataForMeter"] = "输入参数:int cOperateMode,string cTESAMID,string cSessionKey,int iTaskType,string cApdu,string cTaskData",
        };

        private static readonly Dictionary<int, string> ErrorMessages = new()
        {
            [45] = "密码机密钥错",
            [48] = "无设备或设备无效",
            [56] = "创建 socket 句柄失败",
            [57] = "连接服务器失败",
            [64] = "客户端发送数据失败",
            [65] = "客户端接收数据失败",
            [100] = "打开设备失败",
            [160] = "连接密码机失败",
            [161] = "操作权限不够",
            [162] = "USBKey 不是操作员",
            [163] = "服务器发送数据失败",
            [164] = "服务端接收报文失败",
            [165] = "密码机加密数据失败",
            [166] = "密码机导出密钥失败",
            [167] = "密码机计算 MAC 失败",
            [168] = "服务器已断开连接",
            [169] = "数据无效",
            [170] = "密码机收发报文错误",
            [171] = "密码机故障",
            [172] = "数据库出错",
            [1100] = "系统认证错误",
            [1107] = "USBKey 权限不正确",
            [1206] = "签名数据错误",
        };

        public WinSocketServiceInvoker(WinSocketServer server, Action<string> log)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public string GetParameterDescription(string? serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return string.Empty;
            }

            return ServiceDescriptions.TryGetValue(serviceName, out var description)
                ? $"选择加密机函数 {serviceName}，调用接口参数：\r\n{description}"
                : $"选择加密机函数 {serviceName}，当前未配置参数说明。";
        }

        public void Execute(string? serviceName, string rawParameterText)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                _log("请右上角选择加密算法！");
                return;
            }

            var args = ParseArguments(rawParameterText);
            var buffers = new OutputBuffers();

            try
            {
                _log($"调用接口：{serviceName}开始加密计算");
                var result = Invoke(serviceName, args, buffers);
                PrintResult(serviceName, result);
            }
            catch (Exception ex)
            {
                _log($"调用接口：{serviceName}异常：{ex.Message}");
            }
        }

        public ServerLoginResult ConnectServerAndGetRandom(string ip, string port)
        {
            _log("开始连接加密服务器！！！");

            var connectResult = _server.ConnectDeviceEx(ip, port, "8000");
            if (!connectResult.Success)
            {
                _log($"连接加密服务器失败，错误码：{connectResult.Code}，错误说明：{GetErrorMessage(connectResult.Code)}");
                return new ServerLoginResult
                {
                    Connected = false,
                    ConnectCode = connectResult.Code,
                    RandCode = 0
                };
            }

            _log("连接加密服务器成功！");
            var outRand = new byte[128];
            var randResult = _server.CreateRandEx(16, outRand);
            return new ServerLoginResult
            {
                Connected = true,
                ConnectCode = connectResult.Code,
                RandCode = randResult.Code,
                RandText = randResult.Success ? System.Text.Encoding.Default.GetString(outRand).TrimEnd('\0', ' ') : string.Empty
            };
        }

        public IdentityHeartbeatResult SendIdentityHeartbeat(int flag, string putDiv)
        {
            var outRand = new byte[256];
            var outEndata = new byte[256];
            var result = _server.CallMeter_Formal_IdentityAuthentication(flag, putDiv, outRand, outEndata);

            return new IdentityHeartbeatResult
            {
                Code = result.Code,
                OutRand = DecodeOutput(outRand),
                OutEndata = DecodeOutput(outEndata)
            };
        }

        private WinSocketServer.DllResult Invoke(string serviceName, string[] args, OutputBuffers buffers)
        {
            switch (serviceName)
            {
                case "RESAM_Formal_GetKeyData_AppLayer":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iOperateMode", args[0]), ("cTESAMID", args[1]), ("cSessionKey", args[2]), ("cTaskType", args[3]), ("cTaskData", args[4]));
                    return _server.CallReSAM_Formal_GetKeyData_AppLayer(ParseInt(args[0], "iOperateMode"), args[1], args[2], ParseInt(args[3], "cTaskType"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "CloseDevice":
                    return _server.CloseDeviceEx();
                case "ClseUsbkey":
                    var closeUsbKeyResult = _server.ClseUsbkeyEx();
                    return new WinSocketServer.DllResult(closeUsbKeyResult == 0, closeUsbKeyResult);
                case "Meter_Formal_DataClear1":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutData", args[3]));
                    return _server.CallMeter_Formal_DataClear1(ParseInt(args[0], "Flag"), args[1], args[2], args[3], buffers.OutData);
                case "Meter_Formal_DataClear2":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutData", args[3]));
                    return _server.CallMeter_Formal_DataClear2(ParseInt(args[0], "Flag"), args[1], args[2], args[3], buffers.OutData);
                case "Meter_Formal_IdentityAuthentication":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("Flag", args[0]), ("PutDiv", args[1]));
                    return _server.CallMeter_Formal_IdentityAuthentication(ParseInt(args[0], "Flag"), args[1], buffers.OutSID, buffers.OutData);
                case "Meter_Formal_UserControl":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutESAMNo", args[3]), ("PutData", args[4]));
                    return _server.CallMeter_Formal_UserControl(ParseInt(args[0], "Flag"), args[1], args[2], args[3], args[4], buffers.OutData);
                case "Meter_Formal_ParameterUpdate":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutApdu", args[3]), ("PutData", args[4]));
                    return _server.CallMeter_Formal_ParameterUpdate(ParseInt(args[0], "Flag"), args[1], args[2], args[3], args[4], buffers.OutData);
                case "Meter_Formal_ParameterElseUpdate":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutApdu", args[3]), ("PutData", args[4]));
                    return _server.CallMeter_Formal_ParameterElseUpdate(ParseInt(args[0], "Flag"), args[1], args[2], args[3], args[4], buffers.OutData);
                case "Meter_Formal_InfraredAuth":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("Flag", args[0]), ("PutDiv", args[1]), ("PutESAMNo", args[2]), ("PutRand1", args[3]), ("PutRand1Endata", args[4]));
                    return _server.CallMeter_Formal_InfraredAuth(ParseInt(args[0], "Flag"), args[1], args[2], args[3], args[4], buffers.OutSID, buffers.OutData);
                case "Meter_Formal_MacCheck":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutApdu", args[3]), ("PutData", args[4]), ("PutMac", args[5]));
                    return _server.CallMeter_Formal_MacCheck(ParseInt(args[0], "Flag"), args[1], args[2], args[3], args[4], args[5]);
                case "Meter_Formal_KeyUpdateV2":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("PutKeySum", args[0]), ("PutKeystate", args[1]), ("PutKeyid", args[2]), ("PutRand", args[3]), ("PutDiv", args[4]), ("PutESAMNo", args[5]), ("PutChipInfor", args[6]));
                    return _server.CallMeter_Formal_KeyUpdateV2(ParseInt(args[0], "PutKeySum"), args[1], args[2], args[3], args[4], args[5], args[6], buffers.OutData);
                case "Obj_Meter_Formal_SetESAMData":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("InKeyState", args[0]), ("InOperateMode", args[1]), ("cESAMNO", args[2]), ("cSessionKey", args[3]), ("cMeterNo", args[4]), ("cESAMRand", args[5]), ("cData", args[6]));
                    return _server.CallObj_Meter_Formal_SetESAMData(ParseInt(args[0], "InKeyState"), ParseInt(args[1], "InOperateMode"), args[2], args[3], args[4], args[5], args[6],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetESAMData":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cMeterNo", args[2]), ("cOAD", args[3]));
                    return _server.CallObj_Meter_Formal_GetESAMData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData);
                case "Obj_Meter_Formal_VerifyESAMData":
                    RequireArgs(serviceName, args, 8);
                    LogArgs(("InKeyState", args[0]), ("iOperateMode", args[1]), ("cESAMNO", args[2]), ("cMeterNo", args[3]), ("cRandHost", args[4]), ("cOAD", args[5]), ("cTaskData", args[6]), ("cMAC", args[7]));
                    return _server.CallObj_Meter_Formal_VerifyESAMData(ParseInt(args[0], "InKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], args[6], args[7],
                        buffers.OutData);
                case "Obj_Meter_Formal_GetPurseData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iOperateMode", args[0]), ("cESAMNO", args[1]), ("cSessionKey", args[2]), ("cTaskType", args[3]), ("cTaskData", args[4]));
                    return _server.CallObj_Meter_Formal_GetPurseData(ParseInt(args[0], "iOperateMode"), args[1], args[2], ParseInt(args[3], "cTaskType"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetMeterSetData":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iOperateMode", args[0]), ("cESAMNO", args[1]), ("cSessionKey", args[2]), ("cTaskData", args[3]));
                    return _server.CallObj_Meter_Formal_GetMeterSetData(ParseInt(args[0], "iOperateMode"), args[1], args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetGrpBrdCstData":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cESAMNO", args[2]), ("cGrpKID", args[3]), ("cDIV", args[4]), ("AGSEQ", args[5]), ("cBrdCstData", args[6]));
                    return _server.CallObj_Meter_Formal_GetGrpBrdCstData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], args[6],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetTrmKeyData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cESAMNO", args[1]), ("cSessionKey", args[2]), ("cMeterNo", args[3]), ("cKeyType", args[4]));
                    return _server.CallObj_Meter_Formal_GetTrmKeyData(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_InitTrmKeyData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cESAMNO", args[1]), ("cSessionKey", args[2]), ("cMeterNo", args[3]), ("cKeyType", args[4]));
                    return _server.CallObj_Meter_Formal_InitTrmKeyData(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetESAMFileData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("InKeyState", args[0]), ("iOperateMode", args[1]), ("MeterNo", args[2]), ("FileName", args[3]), ("FileLenth", args[4]));
                    return _server.CallObj_Meter_Formal_GetESAMFileData(ParseInt(args[0], "InKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData);
                case "Obj_Terminal_Formal_GetTrmKeyData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyVersion", args[0]), ("strEsamNo", args[1]), ("strSessionKey", args[2]), ("cTerminalAddress", args[3]), ("strKeyType", args[4]));
                    return _server.CallObj_Terminal_Formal_GetTrmKeyData(ParseInt(args[0], "iKeyVersion"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_InitSession":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyVersion", args[0]), ("cTESAMID", args[1]), ("cASCTR", args[2]), ("cFLG", args[3]), ("cMasterCert", args[4]));
                    return _server.CallObj_Terminal_Formal_InitSession(ParseInt(args[0], "iKeyVersion"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData);
                case "Obj_Terminal_Formal_InitSession_RH":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iKeyState", args[0]), ("cTESAMNO", args[1]), ("cASCTR", args[2]), ("cMasterCert", args[3]));
                    return _server.CallObj_Terminal_Formal_InitSession_RH(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData);
                case "Obj_Terminal_Formal_VerifySession_RH":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("cTESAMNO", args[1]), ("cRandHost", args[2]), ("cSessionData", args[3]), ("cSign", args[4]), ("cTerminalCert", args[5]));
                    return _server.CallObj_Terminal_Formal_VerifySession_RH(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4], args[5], buffers.OutData);
                case "Obj_Terminal_Formal_GetSessionData_RH":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("cTESAMNO", args[0]), ("iOperateMode", args[1]), ("KeyID", args[2]), ("cSessionKey", args[3]), ("cTaskType", args[4]), ("cTaskData", args[5]));
                    return _server.CallObj_Terminal_Formal_GetSessionData_RH(args[0], ParseInt(args[1], "iOperateMode"), args[2], args[3], ParseInt(args[4], "cTaskType"), args[5],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_VerifyData_RH":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cTESAMNO", args[2]), ("cSessionKey", args[3]), ("cTerminalCert", args[4]), ("cTaskData", args[5]), ("cSign", args[6]));
                    return _server.CallObj_Terminal_Formal_VerifyData_RH(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], args[6], buffers.OutData);
                case "Obj_Terminal_Formal_GetTrmKeyData_RH":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cTESAMNO", args[1]), ("cIV", args[2]), ("InKid", args[3]), ("InCert", args[4]));
                    return _server.CallObj_Terminal_Formal_GetTrmKeyData_RH(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4],
                        buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetCACertificateData_RH":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cTESAMNO", args[1]), ("cSessionKey", args[2]), ("iCertType", args[3]), ("cCert", args[4]));
                    return _server.CallObj_Terminal_Formal_GetCACertificateData_RH(ParseInt(args[0], "iKeyState"), args[1], args[2], ParseInt(args[3], "iCertType"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "RH_InternalSign":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("sConnect", args[0]), ("cTESAMID", args[1]), ("iKeyIndex", args[2]), ("cCert", args[3]), ("inData", args[4]));
                    return _server.CallRH_InternalSign(ParseInt(args[0], "sConnect"), args[1], ParseInt(args[2], "iKeyIndex"), args[3], args[4], buffers.OutData);
                case "RH_VerifySig":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("sConnect", args[0]), ("cTESAMID", args[1]), ("cCert", args[2]), ("inData", args[3]), ("InSign", args[4]));
                    return _server.CallRH_VerifySig(ParseInt(args[0], "sConnect"), args[1], args[2], args[3], args[4]);
                case "RH_EncData":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("sConnect", args[0]), ("cTESAMID", args[1]), ("cKeyVer", args[2]), ("cKeyIndex", args[3]), ("cDivData", args[4]), ("cIvData", args[5]), ("cInData", args[6]));
                    return _server.CallRH_EncData(ParseInt(args[0], "sConnect"), args[1], args[2], args[3], args[4], args[5], args[6],
                        buffers.OutData, buffers.OutMAC);
                case "RH_DisEncData":
                    RequireArgs(serviceName, args, 8);
                    LogArgs(("sConnect", args[0]), ("cTESAMID", args[1]), ("cKeyVer", args[2]), ("cKeyIndex", args[3]), ("cDivData", args[4]), ("cIvData", args[5]), ("cInEncData", args[6]), ("cInMac", args[7]));
                    return _server.CallRH_DisEncData(ParseInt(args[0], "sConnect"), args[1], args[2], args[3], args[4], args[5], args[6], args[7], buffers.OutData);
                case "RH_Terminal_Formal_GetTrmKeyData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cTESAMID", args[1]), ("cIV", args[2]), ("InKid", args[3]), ("InCert", args[4]));
                    return _server.CallRH_Terminal_Formal_GetTrmKeyData(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4],
                        buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetSessionData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iOperateMode", args[0]), ("cEasmid", args[1]), ("cSessionKey", args[2]), ("cTasktype", args[3]), ("cTaskData", args[4]));
                    return _server.CallObj_Terminal_Formal_GetSessionData(ParseInt(args[0], "iOperateMode"), args[1], args[2], ParseInt(args[3], "cTasktype"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetTerminalSetData":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iOperateMode", args[0]), ("cEasmid", args[1]), ("cSessionKey", args[2]), ("cTaskData", args[3]));
                    return _server.CallObj_Terminal_Formal_GetTerminalSetData(ParseInt(args[0], "iOperateMode"), args[1], args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetGrpBrdCstData":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cTESAMID", args[2]), ("cGrpKID", args[3]), ("cDIV", args[4]), ("AGSEQ", args[5]), ("cBrdCstData", args[6]));
                    return _server.CallObj_Terminal_Formal_GetGrpBrdCstData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], args[6],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_VerifyTerminalData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("ikeyState", args[0]), ("iOperateMode", args[1]), ("cEasmid", args[2]), ("cSessionKey", args[3]), ("cTaskData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Terminal_Formal_VerifyTerminalData(ParseInt(args[0], "ikeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], buffers.OutData);
                case "Obj_Terminal_Formal_VerifyReadData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cTESAMID", args[2]), ("cRandHost", args[3]), ("cReadData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Terminal_Formal_VerifyReadData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5],
                        buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_VerifyReportData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cTESAMID", args[2]), ("cRandT", args[3]), ("cReportData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Terminal_Formal_VerifyReportData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5],
                        buffers.OutData);
                case "Obj_Terminal_Formal_GetResponseData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cTESAMID", args[2]), ("RandHost", args[3]), ("cReportData", args[4]));
                    return _server.CallObj_Terminal_Formal_GetResponseData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_InitTrmKeyData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cTESAMID", args[1]), ("cSessionKey", args[2]), ("cTerminalAddress", args[3]), ("cKeyType", args[4]));
                    return _server.CallObj_Terminal_Formal_InitTrmKeyData(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetCACertificateData":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iKeyState", args[0]), ("cTESAMID", args[1]), ("cSessionKey", args[2]), ("cCerType", args[3]));
                    return _server.CallObj_Terminal_Formal_GetCACertificateData(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Send_Formal_DataForGetKey":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("InDeviceType", args[0]), ("cTastType", args[1]), ("cKeyState", args[2]), ("cTESAMID", args[3]), ("InMeterNo", args[4]), ("cSessionKey", args[5]));
                    return _server.CallObj_Send_Formal_DataForGetKey(args[0], args[1], args[2], args[3], args[4], args[5],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Terminal_Formal_GetR1":
                    return _server.CallTerminal_Formal_GetR1(buffers.OutData);
                case "Terminal_Formal_SessionInitRec":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutVersionNum", args[2]), ("PutSessionID", args[3]), ("PutR1", args[4]));
                    return _server.CallTerminal_Formal_SessionInitRec(args[0], args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Terminal_Formal_SessionKeyConsult":
                    RequireArgs(serviceName, args, 10);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutVersionNum", args[2]), ("PutSessionID", args[3]),
                            ("PutCRLCertificateNo", args[4]), ("PutMasterCertificateNo", args[5]), ("PutTerminalCertificate", args[6]),
                            ("PutEncR2", args[7]), ("PutSign2", args[8]), ("PutR1", args[9]));
                    return _server.CallTerminal_Formal_SessionKeyConsult(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Terminal_Formal_SessionConsultVerify":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("PutR3", args[0]), ("PutMac3", args[1]));
                    return _server.CallTerminal_Formal_SessionConsultVerify(args[0], args[1]);
                case "Terminal_Formal_SessionRecoveryVerify":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutVersionNum", args[2]), ("PutSessionID", args[3]), ("PutEncR2", args[4]), ("PutR3", args[5]), ("PutMac", args[6]));
                    return _server.CallTerminal_Formal_SessionRecoveryVerify(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                case "Terminal_Formal_MACVerify":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutFnType", args[2]), ("PutData", args[3]));
                    return _server.CallTerminal_Formal_MACVerify(args[0], args[1], args[2], args[3], buffers.OutMAC);
                case "Terminal_Formal_ExternalAuth":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutR4", args[2]), ("PutEncR4", args[3]), ("PutR5", args[4]));
                    return _server.CallTerminal_Formal_ExternalAuth(args[0], args[1], args[2], args[3], args[4], buffers.OutData);
                case "Terminal_Formal_CertificateStateChange":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutCertificateState", args[2]), ("PutR6", args[3]));
                    return _server.CallTerminal_Formal_CertificateStateChange(args[0], args[1], args[2], args[3], buffers.OutData, buffers.OutMAC);
                case "Terminal_Formal_SetOfflineCounter":
                    RequireArgs(serviceName, args, 3);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutCounter", args[2]));
                    return _server.CallTerminal_Formal_SetOfflineCounter(args[0], args[1], args[2], buffers.OutData);
                case "Terminal_Formal_ChangeDataAuthorize":
                    return _server.CallTerminal_Formal_ChangeDataAuthorize(buffers.OutData);
                case "Terminal_Formal_GetCipherMeterKey":
                    RequireArgs(serviceName, args, 3);
                    LogArgs(("PutMeterState", args[0]), ("PutMeterNo", args[1]), ("PutTaskType", args[2]));
                    return _server.CallTerminal_Formal_GetCipherMeterKey(args[0], args[1], ParseInt(args[2], "PutTaskType"), buffers.OutData);
                case "Terminal_Formal_EncTaskData":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("PutInDataType", args[0]), ("PutTaskData", args[1]));
                    return _server.CallTerminal_Formal_EncTaskData(ParseInt(args[0], "PutInDataType"), args[1], buffers.OutData);
                case "Terminal_Formal_GroupBroadcast":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutFnType", args[2]), ("PutOutDataType", args[3]), ("PutGroupAdrass", args[4]), ("PutMtime", args[5]), ("PutBroadcastData", args[6]));
                    return _server.CallTerminal_Formal_GroupBroadcast(args[0], args[1], args[2], ParseInt(args[3], "PutOutDataType"), args[4], args[5], args[6], buffers.OutMAC);
                case "Terminal_Formal_SystemBroadcast":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]), ("PutFnType", args[2]), ("PutOutDataType", args[3]), ("PutGroupAdrass", args[4]), ("PutMtime", args[5]), ("PutBroadcastData", args[6]));
                    return _server.CallTerminal_Formal_SystemBroadcast(args[0], args[1], args[2], ParseInt(args[3], "PutOutDataType"), args[4], args[5], args[6], buffers.OutMAC);
                case "Terminal_Formal_SymmetricKeyUpdate":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]));
                    return _server.CallTerminal_Formal_SymmetricKeyUpdate(args[0], args[1], buffers.OutSID, buffers.OutData);
                case "Terminal_Formal_CACertificateUpdate":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("PutCertificateState", args[0]), ("PutCertificateType", args[1]));
                    return _server.CallTerminal_Formal_CACertificateUpdate(args[0], args[1], buffers.OutData);
                case "Terminal_Formal_SymmetricKeyUpdateCT":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("PutState", args[0]), ("PutTESAMNo", args[1]));
                    return _server.CallTerminal_Formal_SymmetricKeyUpdateCT(args[0], args[1], buffers.OutSID, buffers.OutData);
                case "CT_Terminal_Formal_GetCTESAMKey":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("InFlag", args[0]), ("InKeyid", args[1]), ("InCTTESAMNo", args[2]), ("InCTESAMNo", args[3]), ("InRand", args[4]));
                    return _server.CallCT_Terminal_Formal_GetCTESAMKey(ParseInt(args[0], "InFlag"), args[1], args[2], args[3], args[4], buffers.OutData, buffers.OutMAC);
                case "CT_Terminal_Formal_CalCTESAMMac":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("InFlag", args[0]), ("InCTESAMNo", args[1]), ("InRand", args[2]), ("InData", args[3]));
                    return _server.CallCT_Terminal_Formal_CalCTESAMMac(ParseInt(args[0], "InFlag"), args[1], args[2], args[3], buffers.OutMAC);
                case "CT_Terminal_Formal_CalCTTESAMMac":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("PutState", args[0]), ("InCTTESAMNo", args[1]), ("InRand", args[2]), ("InData", args[3]));
                    return _server.CallCT_Terminal_Formal_CalCTTESAMMac(args[0], args[1], args[2], args[3], buffers.OutMAC);
                case "CT_Terminal_Formal_CalVerifyCTESAMMac":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("InFlag", args[0]), ("InCTESAMNo", args[1]), ("InRand", args[2]), ("InData", args[3]), ("InMac", args[4]));
                    return _server.CallCT_Terminal_Formal_CalVerifyCTESAMMac(ParseInt(args[0], "InFlag"), args[1], args[2], args[3], args[4]);
                case "CT_Terminal_Formal_CalVerifyCTTESAMMac":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("PutState", args[0]), ("InCTTESAMNo", args[1]), ("InRand", args[2]), ("InData", args[3]), ("InMac", args[4]));
                    return _server.CallCT_Terminal_Formal_CalVerifyCTTESAMMac(args[0], args[1], args[2], args[3], args[4]);
                case "RDID_Formal_RFIDChangeKey":
                    RequireArgs(serviceName, args, 2);
                    LogArgs(("PutKeyState", args[0]), ("PutDiv", args[1]));
                    return _server.CallRDID_Formal_RFIDChangeKey(args[0], args[1], buffers.OutData);
                case "RDID_Formal_RFIDEncrptData":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("PutKeyState", args[0]), ("PutDiv", args[1]), ("PutData", args[2]), ("PutOPInfor", args[3]));
                    return _server.CallRDID_Formal_RFIDEncrptData(args[0], args[1], args[2], args[3], buffers.OutData, buffers.OutMAC);
                case "RDID_Formal_RFIDDisEncrptData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("PutKeyState", args[0]), ("PutDiv", args[1]), ("PutEncData", args[2]), ("PutOPInfor", args[3]), ("PutMAC", args[4]));
                    return _server.CallRDID_Formal_RFIDDisEncrptData(args[0], args[1], args[2], args[3], args[4], buffers.OutData);
                case "RDID_Formal_RFIDCheckData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("InKeyState", args[0]), ("InDataNo", args[1]), ("PutDiv", args[2]), ("PutData", args[3]), ("PutMac", args[4]));
                    return _server.CallRDID_Formal_RFIDCheckData(args[0], ParseInt(args[1], "InDataNo"), args[2], args[3], args[4]);
                case "Obj_Meter_Formal_GenReadData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyVersion", args[0]), ("strEsamNo", args[1]), ("strMeterNo", args[2]), ("iOperateMode", args[3]), ("randHost", args[4]), ("cReadData", args[5]));
                    return _server.CallObj_Meter_Formal_GenReadData(args[0], args[1], args[2], args[3], args[4], args[5], buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetSessionData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iOperateMode", args[0]), ("cESAMNO", args[1]), ("cSessionKey", args[2]), ("cTaskType", args[3]), ("cTaskData", args[4]));
                    return _server.CallObj_Meter_Formal_GetSessionData(ParseInt(args[0], "iOperateMode"), args[1], args[2], ParseInt(args[3], "cTaskType"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_VerifyMeterData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cESAMNO", args[2]), ("cSessionKey", args[3]), ("cTaskData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Meter_Formal_VerifyMeterData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], buffers.OutData);
                case "Obj_Terminal_Formal_GetSessionDataForMeter":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("cOperateMode", args[0]), ("cTESAMID", args[1]), ("cSessionKey", args[2]), ("iTaskType", args[3]), ("cApdu", args[4]), ("cTaskData", args[5]));
                    return _server.CallObj_Terminal_Formal_GetSessionDataForMeter(ParseInt(args[0], "cOperateMode"), args[1], args[2], ParseInt(args[3], "iTaskType"), args[4], args[5],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_InitSession":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iKeyState", args[0]), ("cDiv", args[1]), ("cAsctr", args[2]), ("cFlag", args[3]));
                    return _server.CallObj_Meter_Formal_InitSession(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3],
                        buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_VerifySession":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("cDiv", args[1]), ("cRandHost", args[2]), ("cSessionData", args[3]), ("cSign", args[4]));
                    return _server.CallObj_Meter_Formal_VerifySession(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3], args[4], buffers.OutData);
                case "Obj_Meter_Formal_VerifyReadData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cMeterNo", args[2]), ("cRandHost", args[3]), ("cReadData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Meter_Formal_VerifyReadData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], buffers.OutData);
                case "Obj_Meter_Formal_VerifyReportData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cMeterNo", args[2]), ("cRandT", args[3]), ("cReportData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Meter_Formal_VerifyReportData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5],
                        buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GetResponseData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cMeterNo", args[2]), ("RandHost", args[3]), ("cReportData", args[4]));
                    return _server.CallObj_Meter_Formal_GetResponseData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                default:
                    _log($"接口 {serviceName} 暂未实现调用分发，只在接口列表中展示。");
                    return WinSocketServer.DllResult.Fail();
            }
        }

        private void PrintResult(string serviceName, WinSocketServer.DllResult result)
        {
            if (result.Code == 0)
            {
                _log($"调用接口：{serviceName}成功,返回值：{result.Code}");
                return;
            }

            _log($"调用接口：{serviceName}失败,返回值：{result.Code}，错误说明：{GetErrorMessage(result.Code)}");
        }

        private static string GetErrorMessage(int code)
        {
            if (ErrorMessages.TryGetValue(code, out var message))
            {
                return message;
            }

            return code switch
            {
                >= 700 and <= 712 => "客户端导出密钥失败",
                >= 800 and <= 810 => "计算 MAC 失败",
                >= 900 and <= 910 => "加密数据失败",
                >= 1000 and <= 1010 => "数据长度错",
                >= 1108 and <= 1111 => "操作 USBKey 失败",
                _ => "未知错误码"
            };
        }

        private void LogArgs(params (string Name, string Value)[] args)
        {
            _log(string.Join("\r\n", args.Select(arg => $"{arg.Name} = {arg.Value}")));
        }

        private static string[] ParseArguments(string rawParameterText)
        {
            return (rawParameterText ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
        }

        private static void RequireArgs(string serviceName, string[] args, int requiredCount)
        {
            if (args.Length < requiredCount)
            {
                throw new ArgumentException($"{serviceName} 至少需要 {requiredCount} 个参数，当前只有 {args.Length} 个。");
            }
        }

        private static int ParseInt(string value, string name)
        {
            if (!int.TryParse(value, out var result))
            {
                throw new ArgumentException($"{name} 必须是整数，当前值：{value}");
            }

            return result;
        }

        private static string DecodeOutput(byte[] data)
        {
            return System.Text.Encoding.Default.GetString(data).TrimEnd('\0', ' ');
        }

        private sealed class OutputBuffers
        {
            public byte[] OutSID { get; } = new byte[4096];
            public byte[] OutAttachData { get; } = new byte[4096];
            public byte[] OutData { get; } = new byte[4096];
            public byte[] OutMAC { get; } = new byte[4096];
        }
    }
}
