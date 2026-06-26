using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Protocol;

namespace ModelTest.CustomControl
{
    public partial class TerminalV2UserControl : UserControl
    {
        public delegate void UpdateMainFormDelegate(string message);

        public event UpdateMainFormDelegate? OnUpdateRequestedTerminalV2Log;

        private EnhancedTcpClient? _tcpClient;
        private readonly DetectionBoardProtocolV2 _protocol = new();
        private bool _isDisposing;

        public TerminalV2UserControl()
        {
            InitializeComponent();
            measureModuleTypeComboBox.SelectedIndex = 0;
            measureReadItemComboBox.SelectedIndex = 0;
            measureModeComboBox.SelectedIndex = 0;
            virtualModuleModeComboBox.SelectedIndex = 0;
            virtualUsbStateComboBox.SelectedIndex = 0;
            virtualLoadTypeComboBox.SelectedIndex = 0;
            virtualLoadStateComboBox.SelectedIndex = 0;
            virtualRippleStateComboBox.SelectedIndex = 0;
            virtualVoltageModeComboBox.SelectedIndex = 0;
            virtualStatusPinComboBox.SelectedIndex = 0;
            virtualPinTypeComboBox.SelectedIndex = 0;
            meterVoltageLoopPowerComboBox.SelectedIndex = 0;
            LoadVirtualPinSequenceItems();
            LoadVirtualModuleTypeItems(0x01);
            chkVoltageUa.Checked = true;
            chkVoltageUb.Checked = true;
            chkVoltageUc.Checked = true;
            chkCurrentIa.Checked = true;
            chkCurrentIb.Checked = true;
            chkCurrentIc.Checked = true;
            chkCurrentIn.Checked = false;
            InitializeRemoteControlGroup();
            InitializeControlGroup();
            InitializeErrorInstrumentGroup();
            BindErrorInstrument();
            AlignVirtualModuleGroupToPanelRight();
        }

        private void operationPanel_SizeChanged(object? sender, EventArgs e)
        {
            AlignVirtualModuleGroupToPanelRight();
        }

        private void AlignVirtualModuleGroupToPanelRight()
        {
            const int rightMargin = 24;
            int x = Math.Max(rightMargin, operationPanel.ClientSize.Width - groupVirtualModuleControl.Width - rightMargin);
            groupVirtualModuleControl.Location = new Point(x, groupVirtualModuleControl.Location.Y);
        }

        private void stationTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            e.Handled = !char.IsDigit(e.KeyChar);
        }

        private void moduleNumberTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
            {
                return;
            }

            e.Handled = !char.IsDigit(e.KeyChar);
        }

        private async void connectButton_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected == true)
            {
                DisconnectTcpClient();
                return;
            }

            string ip = ipTextBox.Text.Trim();
            string portText = portTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(portText))
            {
                PublishLog("请输入IP地址和端口号！");
                return;
            }

            if (!int.TryParse(portText, out int port))
            {
                PublishLog("端口号格式不正确！");
                return;
            }

            if (!TryGetStationHexText(out string stationHex))
            {
                return;
            }

            EnsureTcpClient();
            SetConnectionUiState("连接中", false);
            PublishLog($"终端测试单元V2正在连接 {ip}:{port}，表位HEX：{stationHex}");

            bool connected = await _tcpClient!.ConnectAsync(ip, port);
            if (connected)
            {
                SetConnectionUiState($"已连接：{_tcpClient.ServerEndpoint}", true);
                PublishLog($"终端测试单元V2连接成功：{_tcpClient.ServerEndpoint}");
                return;
            }

            _tcpClient = null;
            SetConnectionUiState("未连接", false);
            PublishLog($"终端测试单元V2连接失败：{ip}:{port}");
        }

        private async void btnResetMcu_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out string stationHex))
            {
                return;
            }

            byte[] frame = _protocol.BuildTerminalResetFrame(stationAddress);
            PublishLog($"发送终端复位命令：{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            if (sent)
            {
                PublishLog("终端复位命令发送成功。");
            }
            else
            {
                PublishLog("终端复位命令发送失败。");
            }
        }

        private async void btnReadVersion_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out string stationHex))
            {
                return;
            }

            byte[] frame = _protocol.BuildTerminalVersionFrame(stationAddress);
            PublishLog($"发送读取版本命令：{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            if (sent)
            {
                PublishLog("读取版本命令发送成功。");
            }
            else
            {
                PublishLog("读取版本命令发送失败。");
            }
        }

        private async void btnDcPowerOn_Click(object sender, EventArgs e)
        {
            await SendRealModulePowerCommandAsync(isDcPower: true, powerOn: true);
        }

        private async void btnDcPowerOff_Click(object sender, EventArgs e)
        {
            await SendRealModulePowerCommandAsync(isDcPower: true, powerOn: false);
        }

        private async void btnAcPowerOn_Click(object sender, EventArgs e)
        {
            await SendRealModulePowerCommandAsync(isDcPower: false, powerOn: true);
        }

        private async void btnAcPowerOff_Click(object sender, EventArgs e)
        {
            await SendRealModulePowerCommandAsync(isDcPower: false, powerOn: false);
        }

        private async Task SendRealModulePowerCommandAsync(bool isDcPower, bool powerOn)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            if (!TryGetModuleNumber(out byte moduleNumber))
            {
                return;
            }

            byte[] frame = isDcPower
                ? _protocol.BuildRealModuleDcPowerFrame(stationAddress, moduleNumber, powerOn)
                : _protocol.BuildRealModuleAcPowerFrame(stationAddress, moduleNumber, powerOn);

            string powerType = isDcPower ? "真实模组直流" : "真实模组交流220V";
            string action = powerOn ? "上电" : "断电";
            PublishLog($"发送{powerType}{action}命令：模块={moduleNumber}，{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? $"{powerType}{action}命令发送成功。" : $"{powerType}{action}命令发送失败。");
        }

        private async void btnCarrierDcPowerOn_Click(object sender, EventArgs e)
        {
            await SendCarrierModulePowerCommandAsync(isDcPower: true, powerOn: true);
        }

        private async void btnCarrierDcPowerOff_Click(object sender, EventArgs e)
        {
            await SendCarrierModulePowerCommandAsync(isDcPower: true, powerOn: false);
        }

        private async void btnCarrierAcPowerOn_Click(object sender, EventArgs e)
        {
            await SendCarrierModulePowerCommandAsync(isDcPower: false, powerOn: true);
        }

        private async void btnCarrierAcPowerOff_Click(object sender, EventArgs e)
        {
            await SendCarrierModulePowerCommandAsync(isDcPower: false, powerOn: false);
        }

        private async Task SendCarrierModulePowerCommandAsync(bool isDcPower, bool powerOn)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            if (!TryGetModuleNumber(out byte moduleNumber))
            {
                return;
            }

            byte[] frame = isDcPower
                ? _protocol.BuildCarrierModuleDcPowerFrame(stationAddress, moduleNumber, powerOn)
                : _protocol.BuildCarrierModuleAcPowerFrame(stationAddress, moduleNumber, powerOn);

            string powerType = isDcPower ? "载波模组直流" : "载波模组交流220V";
            string action = powerOn ? "上电" : "断电";
            PublishLog($"发送{powerType}{action}命令：模块={moduleNumber}，{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? $"{powerType}{action}命令发送成功。" : $"{powerType}{action}命令发送失败。");
        }

        private async void btnSetRstHigh_Click(object sender, EventArgs e)
        {
            await SendCarrierPinLevelSetCommandAsync(pinType: 0x01, pinName: "RST", highLevel: true);
        }

        private async void btnSetRstLow_Click(object sender, EventArgs e)
        {
            await SendCarrierPinLevelSetCommandAsync(pinType: 0x01, pinName: "RST", highLevel: false);
        }

        private async void btnSetSetHigh_Click(object sender, EventArgs e)
        {
            await SendCarrierPinLevelSetCommandAsync(pinType: 0x02, pinName: "SET", highLevel: true);
        }

        private async void btnSetSetLow_Click(object sender, EventArgs e)
        {
            await SendCarrierPinLevelSetCommandAsync(pinType: 0x02, pinName: "SET", highLevel: false);
        }

        private async void btnSetEventHigh_Click(object sender, EventArgs e)
        {
            await SendCarrierPinLevelSetCommandAsync(pinType: 0x03, pinName: "EVENT", highLevel: true);
        }

        private async void btnSetEventLow_Click(object sender, EventArgs e)
        {
            await SendCarrierPinLevelSetCommandAsync(pinType: 0x03, pinName: "EVENT", highLevel: false);
        }

        private async void btnReadStaPin_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            if (!TryGetModuleNumber(out byte moduleNumber))
            {
                return;
            }

            byte[] frame = _protocol.BuildCarrierModulePinLevelReadFrame(stationAddress, moduleNumber);
            PublishLog($"发送读取载波模组STA引脚电平命令：模块={moduleNumber}，{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? "读取载波模组STA引脚电平命令发送成功。" : "读取载波模组STA引脚电平命令发送失败。");
        }

        private async void btnMeasureSend_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            if (!TryGetModuleNumber(out byte moduleNumber))
            {
                return;
            }

            if (!TryGetMeasureModuleType(out byte moduleType))
            {
                return;
            }

            if (!TryGetMeasureReadItem(out byte readItem))
            {
                return;
            }

            if (!TryGetMeasureModeOrRate(out byte modeOrRate))
            {
                return;
            }

            byte[] frame = _protocol.BuildCarrierModuleDcMeasureFrame(stationAddress, moduleType, moduleNumber, readItem, modeOrRate);
            PublishLog($"发送读取模组直流电压/电流/功耗命令：{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? "读取模组直流电压/电流/功耗命令发送成功。" : "读取模组直流电压/电流/功耗命令发送失败。");
        }

        private async void btnSwitchVirtualModule_Click(object sender, EventArgs e)
        {
            await SendModuleModeSwitchCommandAsync(moduleMode: 0x01, modeName: "虚拟模组");
        }

        private async void btnSwitchRealModule_Click(object sender, EventArgs e)
        {
            await SendModuleModeSwitchCommandAsync(moduleMode: 0x02, modeName: "真实模组");
        }

        private void virtualModuleModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadVirtualModuleTypeItems(GetVirtualModuleMode());
        }

        private async void btnSetVirtualModuleType_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte mode = GetVirtualModuleMode();
            if (!TryGetVirtualModuleTypeCode(out byte typeCode))
            {
                return;
            }

            byte[] frame = _protocol.BuildVirtualModuleTypeFrame(stationAddress, mode, typeCode);
            PublishLog($"发送设置虚拟模组类型命令：模式={mode:X2} 类型={typeCode:X2}，{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? "设置虚拟模组类型命令发送成功。" : "设置虚拟模组类型命令发送失败。");
        }

        private async void btnSetVirtualUsbState_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte usbState = GetVirtualUsbState();
            byte[] frame = _protocol.BuildVirtualModuleUsbStateFrame(stationAddress, usbState);
            await SendVirtualModuleFrameAsync(frame, $"设置虚拟模组USB连接状态命令：状态={usbState:X2}({virtualUsbStateComboBox.Text})");
        }

        private async void btnSetVirtualLoad_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte loadType = (byte)virtualLoadTypeComboBox.SelectedIndex;
            bool enabled = virtualLoadStateComboBox.SelectedIndex == 1;
            byte[] frame = _protocol.BuildVirtualModuleLoadFrame(stationAddress, loadType, enabled);
            string stateName = enabled ? "打开带载" : "关闭带载";
            await SendVirtualModuleFrameAsync(frame, $"设置虚拟模组带载命令：类型={loadType:X2}({virtualLoadTypeComboBox.Text}) 状态={stateName}");
        }

        private async void btnSetVirtualRipple_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte rippleState = (byte)virtualRippleStateComboBox.SelectedIndex;
            byte[] frame = _protocol.BuildVirtualModuleRippleConnectionFrame(stationAddress, rippleState);
            await SendVirtualModuleFrameAsync(frame, $"设置虚拟模组纹波连接命令：状态={rippleState:X2}({virtualRippleStateComboBox.Text})");
        }

        private async void btnReadVirtualVoltage_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte readMode = GetVirtualVoltageReadMode();
            byte[] frame = _protocol.BuildVirtualModuleInterfaceVoltageFrame(stationAddress, readMode);
            await SendVirtualModuleFrameAsync(frame, $"读取虚拟模组接口电压命令：模式={readMode:X2}({virtualVoltageModeComboBox.Text})");
        }

        private async void btnSetVirtualStatusPin_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte moduleState = (byte)virtualStatusPinComboBox.SelectedIndex;
            byte[] frame = _protocol.BuildVirtualModuleStatusPinFrame(stationAddress, moduleState);
            await SendVirtualModuleFrameAsync(frame, $"设置虚拟模块状态管脚命令：状态={moduleState:X2}({virtualStatusPinComboBox.Text})");
        }

        private async void btnReadVirtualPinTime_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte pinType = GetVirtualPinType();
            byte sequence = (byte)(virtualPinSequenceComboBox.SelectedIndex + 1);
            byte[] frame = _protocol.BuildVirtualModulePinLevelAndTimeFrame(stationAddress, pinType, sequence);
            await SendVirtualModuleFrameAsync(frame, $"读取虚拟模块引脚电平和发生时间命令：引脚={pinType:X2}({virtualPinTypeComboBox.Text}) 序号={sequence}");
        }

        private async void btnClearVirtualPinCache_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte clearMask = 0;
            if (virtualClearOnOffCheckBox.Checked)
            {
                clearMask |= 0x01;
            }

            if (virtualClearRstCheckBox.Checked)
            {
                clearMask |= 0x02;
            }

            byte[] frame = _protocol.BuildVirtualModulePinCacheClearFrame(stationAddress, clearMask);
            await SendVirtualModuleFrameAsync(frame, $"清空虚拟模块引脚电平和发生时间缓存命令：掩码={clearMask:X2}");
        }

        private async void btnSetVirtualActiveReport_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte[] frame = _protocol.BuildVirtualModuleActiveReportModeFrame(stationAddress, 0x01);
            await SendVirtualModuleFrameAsync(frame, "设置虚拟模组主动上报运行模式命令：模式=01(串口2主动上报)");
        }

        private async void btnVoltageOn_Click(object sender, EventArgs e)
        {
            await SendVoltageControlAsync(true);
        }

        private async void btnVoltageOff_Click(object sender, EventArgs e)
        {
            await SendVoltageControlAsync(false);
        }

        private async void btnCurrentOn_Click(object sender, EventArgs e)
        {
            await SendCurrentControlAsync(true);
        }

        private async void btnCurrentOff_Click(object sender, EventArgs e)
        {
            await SendCurrentControlAsync(false);
        }

        private async void btnSwitchCurrentLoop_Click(object sender, EventArgs e)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte switchState;
            if (chkCurrentIn.Checked)
            {
                switchState = 0x01;
            }
            else if (chkCurrentIc.Checked)
            {
                switchState = 0x00;
            }
            else
            {
                PublishLog("请选择 IN 或 IC。");
                return;
            }

            byte[] frame = _protocol.BuildNeutralCurrentSwitchFrame(stationAddress, switchState);
            string loopName = switchState == 0x01 ? "IN侧" : "IC侧";
            await SendVirtualModuleFrameAsync(frame, $"零线电流切换命令：{loopName}");
        }

        private async void btnReadMeterVoltageLoopPower_Click(object sender, EventArgs e)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte operation = GetMeterVoltageLoopPowerOperation();
            byte[] frame = _protocol.BuildMeterVoltageLoopPowerFrame(stationAddress, operation);
            await SendVirtualModuleFrameAsync(frame, $"读取表位电压回路功率命令：操作={operation:X2}({meterVoltageLoopPowerComboBox.Text})");
        }

        private async void btnRemoteSignalStateSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            ushort stateMask = BuildRemoteSignalStateMask();
            byte[] frame = _protocol.BuildRemoteSignalStateFrame(stationAddress, stateMask);
            await SendVirtualModuleFrameAsync(frame, $"遥信状态量控制命令：掩码={stateMask:X4}");
        }

        private async void btnPulseParameterSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseUInt32(pulseFrequencyTextBox, "输出脉冲频率", out uint frequency) ||
                !TryParseUInt32(pulseCountTextBox, "输出脉冲个数", out uint count) ||
                !TryParseByte(pulseDutyTextBox, "脉冲占空比", out byte dutyCycle))
            {
                return;
            }

            byte channelMask = BuildCheckBoxMask(remotePulseChannelCheckBoxes);
            bool start = pulseStartComboBox?.Text.Trim() != "停止脉冲";
            byte[] frame = _protocol.BuildPulseParameterFrame(stationAddress, channelMask, start, frequency, count, dutyCycle);
            await SendVirtualModuleFrameAsync(frame, $"脉冲参数设置状态命令：通道={channelMask:X2} 状态={(start ? "启动" : "停止")} 频率={frequency}Hz 个数={count} 占空比={dutyCycle}");
        }

        private async void btnPortSwitchSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte switchType = portSwitchTypeComboBox?.Text.Trim() switch
            {
                "485-3/232切换" => 0x03,
                "485-4/232切换" => 0x04,
                _ => 0x02
            };
            byte target = portSwitchTargetComboBox?.Text.Contains("232", StringComparison.OrdinalIgnoreCase) == true ||
                portSwitchTargetComboBox?.Text.Contains("CAN", StringComparison.OrdinalIgnoreCase) == true
                    ? (byte)0x01
                    : (byte)0x00;

            byte[] frame = _protocol.BuildTerminalPortSwitchFrame(stationAddress, switchType, target);
            await SendVirtualModuleFrameAsync(frame, $"切换终端RS485/RS232/CAN命令：类型={switchType:X2} 目标={target:X2}");
        }

        private async void btnUsbStateSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            bool enabled = usbStateComboBox?.Text.Trim() == "打开USB";
            byte[] frame = _protocol.BuildUsbSwitchFrame(stationAddress, enabled);
            await SendVirtualModuleFrameAsync(frame, $"切换USB状态命令：{(enabled ? "打开USB" : "关闭USB")}");
        }

        private async void btnRemoteInterferenceSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseUInt16(remoteInterferenceDurationTextBox, "干扰脉冲持续时间", out ushort duration))
            {
                return;
            }

            byte channelMask = BuildCheckBoxMask(remoteTestChannelCheckBoxes);
            byte[] frame = _protocol.BuildRemoteSignalInterferencePulseFrame(stationAddress, channelMask, duration);
            await SendVirtualModuleFrameAsync(frame, $"遥信干扰脉冲测试命令：通道={channelMask:X2} 持续={duration}ms");
        }

        private async void btnRemoteDebounceSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseUInt16(remoteDebounceDurationTextBox, "防抖输出持续时间", out ushort duration) ||
                !TryParseByte(remoteDebounceIntervalTextBox, "两路遥信触发间隔", out byte interval))
            {
                return;
            }

            byte channelMask = BuildCheckBoxMask(remoteTestChannelCheckBoxes);
            byte[] frame = _protocol.BuildRemoteSignalDebounceFrame(stationAddress, channelMask, duration, interval);
            await SendVirtualModuleFrameAsync(frame, $"遥信防抖测试命令：通道={channelMask:X2} 持续={duration}ms 间隔={interval}ms");
        }

        private async void btnRemoteAvalancheSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseByte(remoteAvalancheCountTextBox, "雪崩变位次数", out byte changeCount) ||
                !TryParseByte(remoteAvalancheIntervalTextBox, "雪崩间隔时间", out byte intervalSeconds))
            {
                return;
            }

            byte channelMask = BuildCheckBoxMask(remoteTestChannelCheckBoxes);
            byte[] frame = _protocol.BuildRemoteSignalAvalancheFrame(stationAddress, channelMask, changeCount, intervalSeconds);
            await SendVirtualModuleFrameAsync(frame, $"遥信雪崩测试命令：通道={channelMask:X2} 次数={changeCount} 间隔={intervalSeconds}s");
        }

        private async void btnCanBaudRateSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte canMask = 0;
            if (can1CheckBox?.Checked == true) canMask |= 0x01;
            if (can2CheckBox?.Checked == true) canMask |= 0x02;

            if (!ushort.TryParse((canBaudRateComboBox?.Text ?? "125K").TrimEnd('K', 'k'), out ushort baudRate))
            {
                PublishLog("CAN波特率格式不正确。");
                return;
            }

            byte[] frame = _protocol.BuildCanBaudRateFrame(stationAddress, canMask, baudRate);
            await SendVirtualModuleFrameAsync(frame, $"修改CAN波特率命令：通道={canMask:X2} 波特率={baudRate}K");
        }

        private async void btnTemperatureHumidityRead_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte[] frame = _protocol.BuildTemperatureHumidityReadFrame(stationAddress);
            await SendVirtualModuleFrameAsync(frame, "温湿度数据读取命令");
        }

        private async void btnRemoteControlStatusRead_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte[] frame = _protocol.BuildRemoteControlStatusReadFrame(stationAddress);
            await SendVirtualModuleFrameAsync(frame, "遥控状态量读取命令");
        }

        private async void btnRemoteControlPulseTimeRead_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte[] frame = _protocol.BuildRemoteControlPulseTimeReadFrame(stationAddress);
            await SendVirtualModuleFrameAsync(frame, "遥控状态脉冲时间读取命令");
        }

        private async void btnMeterControlFeedbackSignalSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte signalType = meterSignalTypeComboBox?.Text.Trim() == "反馈信号" ? (byte)0x02 : (byte)0x01;
            byte operation = meterSignalOperationComboBox?.Text.Trim() == "读取状态" ? (byte)0x00 : (byte)0x01;
            byte value = meterSignalValueComboBox?.Text.Trim() == "开" ? (byte)0x01 : (byte)0x00;

            if (signalType == 0x01 && operation == 0x00)
            {
                PublishLog("控制信号只能设置状态，不能读取状态。");
                return;
            }

            byte[] frame = _protocol.BuildMeterControlFeedbackSignalFrame(stationAddress, signalType, operation, value);
            await SendVirtualModuleFrameAsync(frame, $"电能表控制与反馈信号控制命令：类型={signalType:X2} 操作={operation:X2} 值={value:X2}");
        }

        private async void btnAlarmStatusRead_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte[] frame = _protocol.BuildAlarmStatusReadFrame(stationAddress);
            await SendVirtualModuleFrameAsync(frame, "告警状态量读取命令");
        }

        private async void btnIndicatorLightControl_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseUInt16(ledBlinkTimeTextBox, "闪烁时间", out ushort blinkTime))
            {
                return;
            }

            byte color = GetSelectedIndexByte(ledColorComboBox, 1);
            byte ledType = GetSelectedIndexByte(ledTypeComboBox, 1);
            byte ledMask = BuildCheckBoxMask(ledChannelCheckBoxes);
            byte mode = ledModeComboBox?.Text.Trim() switch
            {
                "常亮" => 0x01,
                "闪烁" => 0x02,
                _ => 0x00
            };

            byte[] frame = _protocol.BuildIndicatorLightControlFrame(stationAddress, color, ledType, ledMask, mode, blinkTime);
            await SendVirtualModuleFrameAsync(frame, $"指示灯控制命令：颜色={color:X2} 类型={ledType:X2} LED={ledMask:X2} 模式={mode:X2}");
        }

        private async void btnTerminalTypeSet_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte terminalType = (byte)(terminalTypeComboBox?.SelectedIndex ?? 0);
            byte[] frame = _protocol.BuildTerminalTypeSetFrame(stationAddress, terminalType);
            await SendVirtualModuleFrameAsync(frame, $"设置终端类型命令：类型={terminalType:X2}({terminalTypeComboBox?.Text})");
        }

        private async void btnAuxPowerLoadSet_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte channelMask = BuildCheckBoxMask(auxPowerLoadCheckBoxes);
            bool enabled = auxPowerLoadStateComboBox?.Text.Trim() == "带载";
            byte[] frame = _protocol.BuildAuxiliaryPowerLoadFrame(stationAddress, channelMask, enabled);
            await SendVirtualModuleFrameAsync(frame, $"终端辅助电源带载命令：通道={channelMask:X2} 状态={(enabled ? "带载" : "不带载")}");
        }

        private async void btnAuxPowerVoltageSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseUInt16(auxPowerVoltageCalibrationTextBox, "辅助电源校准电压", out ushort calibrationMv))
            {
                return;
            }

            byte channel = GetSelectedIndexByte(auxPowerVoltageChannelComboBox, 1);
            byte operation = auxPowerVoltageOperationComboBox?.Text.Trim() == "校准" ? (byte)0x0A : (byte)0x01;
            byte[] frame = _protocol.BuildAuxiliaryPowerVoltageFrame(stationAddress, channel, operation, calibrationMv);
            await SendVirtualModuleFrameAsync(frame, $"读取终端辅助电源电压命令：通道={channel:X2} 操作={operation:X2}");
        }

        private async void btnSmaControl_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte state = (byte)(smaStateComboBox?.SelectedIndex ?? 0);
            byte[] frame = _protocol.BuildSmaInterfaceControlFrame(stationAddress, state);
            await SendVirtualModuleFrameAsync(frame, $"SMA接口控制命令：状态={state:X2}({smaStateComboBox?.Text})");
        }

        private async void btnSourcePowerSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte target = GetSelectedIndexByte(sourcePowerTargetComboBox, 1);
            bool powerOn = sourcePowerStateComboBox?.Text.Trim() == "上电";
            byte[] frame = _protocol.BuildSourceMeterAmplifierPowerFrame(stationAddress, target, powerOn);
            await SendVirtualModuleFrameAsync(frame, $"源表功放交流上电命令：对象={target:X2} 状态={(powerOn ? "上电" : "下电")}");
        }

        private async void btnSourceSwitchSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            bool electricSource = sourceSwitchComboBox?.Text.Trim() == "电工源";
            byte[] frame = _protocol.BuildSourceSwitchFrame(stationAddress, electricSource);
            await SendVirtualModuleFrameAsync(frame, $"标准源与电工源切换命令：{(electricSource ? "电工源" : "标准源")}");
        }

        private async void btnSinglePhaseAccessSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte accessState = (byte)(singlePhaseAccessComboBox?.SelectedIndex ?? 0);
            byte[] frame = _protocol.BuildSinglePhaseMeterSourceAccessFrame(stationAddress, accessState);
            await SendVirtualModuleFrameAsync(frame, $"电表柜电工源接入单相表命令：状态={accessState:X2}({singlePhaseAccessComboBox?.Text})");
        }

        private async void btnCurrentTimeSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            bool setTime = timeOperationComboBox?.Text.Trim() == "设置当前时间";
            byte[] frame = _protocol.BuildCurrentTimeFrame(stationAddress, setTime, DateTime.Now);
            await SendVirtualModuleFrameAsync(frame, $"设置/读取当前时间命令：{(setTime ? "设置当前时间" : "读取当前时间")}");
        }

        private async void btnSamplingVoltageSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            if (!TryParseUInt16(samplingVoltageCalibrationTextBox, "采样电压校准值", out ushort calibrationMv))
            {
                return;
            }

            byte voltageType = GetSelectedIndexByte(samplingVoltageTypeComboBox, 1);
            byte operation = samplingVoltageOperationComboBox?.Text.Trim() == "校准" ? (byte)0x80 : (byte)0x01;
            byte[] frame = _protocol.BuildSamplingVoltageFrame(stationAddress, voltageType, operation, calibrationMv);
            await SendVirtualModuleFrameAsync(frame, $"读取检测板采样电压命令：类型={voltageType:X2} 操作={operation:X2}");
        }

        private async void btnPanelRemovalSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            bool clear = panelRemovalOperationComboBox?.Text.Trim() == "清除";
            byte[] frame = _protocol.BuildPanelRemovalInfoFrame(stationAddress, clear);
            await SendVirtualModuleFrameAsync(frame, $"读取面板拆卸信息命令：{(clear ? "清除" : "读取")}");
        }

        private async void btnGroundFaultSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte phase = (byte)(groundFaultComboBox?.SelectedIndex ?? 0);
            byte[] frame = _protocol.BuildGroundFaultTestFrame(stationAddress, phase);
            await SendVirtualModuleFrameAsync(frame, $"接地故障测试命令：状态={phase:X2}({groundFaultComboBox?.Text})");
        }

        private async void btnPtResetSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte[] frame = _protocol.BuildPtResetFrame(stationAddress);
            await SendVirtualModuleFrameAsync(frame, "PT复位命令");
        }

        private async void btnLoopSelfCheckSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte operation = loopSelfCheckComboBox?.Text.Trim() switch
            {
                "3相4开始自检" => 0x02,
                "读取自检结果" => 0x03,
                "复位自检结果" => 0x10,
                _ => 0x01
            };
            byte[] frame = _protocol.BuildLoopSelfCheckFrame(stationAddress, operation);
            await SendVirtualModuleFrameAsync(frame, $"电压回路电流回路自检命令：操作={operation:X2}");
        }

        private async void btnImpedanceBoxSend_Click(object? sender, EventArgs e)
        {
            if (!TryGetRemoteControlStationAddress(out byte stationAddress))
            {
                return;
            }

            byte voltageMask = BuildCheckBoxMask(impedanceVoltageCheckBoxes);
            byte itemCode = (byte)(impedanceItemComboBox?.SelectedIndex ?? 0);
            byte functionCode = GetImpedanceFunctionCode();
            byte[] frame = _protocol.BuildImpedanceBoxControlFrame(stationAddress, voltageMask, itemCode, functionCode);
            await SendVirtualModuleFrameAsync(frame, $"阻抗箱控制命令：电压={voltageMask:X2} 项={itemCode:X2} 功能={functionCode:X2}");
        }

        private async void btnMeterMotorPress_Click(object sender, EventArgs e)
        {
            await SendMotorPressCommandAsync(motorType: 0x01, motorName: "表位电机", pressed: true);
        }

        private async void btnMeterMotorRelease_Click(object sender, EventArgs e)
        {
            await SendMotorPressCommandAsync(motorType: 0x01, motorName: "表位电机", pressed: false);
        }

        private async void btnMagnetMotorPress_Click(object sender, EventArgs e)
        {
            await SendMotorPressCommandAsync(motorType: 0x02, motorName: "永磁铁电机", pressed: true);
        }

        private async void btnMagnetMotorRelease_Click(object sender, EventArgs e)
        {
            await SendMotorPressCommandAsync(motorType: 0x02, motorName: "永磁铁电机", pressed: false);
        }

        private void chkCurrentIc_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCurrentIc.Checked)
            {
                chkCurrentIn.Checked = false;
            }
        }

        private void chkCurrentIn_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCurrentIn.Checked)
            {
                chkCurrentIc.Checked = false;
            }
        }

        private void virtualPinTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadVirtualPinSequenceItems();
        }

        private async Task SendModuleModeSwitchCommandAsync(byte moduleMode, string modeName)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte[] frame = _protocol.BuildModuleModeSwitchFrame(stationAddress, moduleMode);
            PublishLog($"发送切换{modeName}命令：{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? $"切换{modeName}命令发送成功。" : $"切换{modeName}命令发送失败。");
        }

        private async Task SendVirtualModuleFrameAsync(byte[] frame, string commandDescription)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            PublishLog($"发送{commandDescription}，{DetectionBoardProtocolV2.ToHexString(frame)}");
            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? $"{commandDescription}发送成功。" : $"{commandDescription}发送失败。");
        }

        private void BindErrorInstrument()
        {
            ultrSimpleDisplayV2.ProtocolVersion = ErrorInstrumentProtocolVersion.V2;
            ultrSimpleDisplayV2.TerminalAddressProvider = () => stationTextBox.Text.Trim();
            ultrSimpleDisplayV2.LogRequested += PublishLog;
            ultrSimpleDisplayV2.SendCommandRequested += SendErrorInstrumentCommandAsync;
        }

        private async Task SendErrorInstrumentCommandAsync(string messageHex)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            byte[] frame = ModelTool.HexStringToByteArray(messageHex);
            PublishLog($"发送误差实验命令：{DetectionBoardProtocolV2.ToHexString(frame)}");
            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? "误差实验命令发送成功。" : "误差实验命令发送失败。");
        }

        private byte GetVirtualUsbState()
        {
            return virtualUsbStateComboBox.Text.Trim() switch
            {
                "恢复连接" => 0x01,
                "USB重启" => 0x02,
                _ => 0x00
            };
        }

        private byte GetVirtualVoltageReadMode()
        {
            return virtualVoltageModeComboBox.Text.Trim() switch
            {
                "单次读取" => 0x01,
                "连续读取" => 0x02,
                _ => 0x00
            };
        }

        private byte GetVirtualPinType()
        {
            return virtualPinTypeComboBox.Text.Trim() == "RST" ? (byte)0x02 : (byte)0x01;
        }

        private void LoadVirtualPinSequenceItems()
        {
            if (virtualPinSequenceComboBox == null)
            {
                return;
            }

            int count = GetVirtualPinType() == 0x02 ? 2 : 3;
            virtualPinSequenceComboBox.Items.Clear();
            for (int i = 1; i <= count; i++)
            {
                virtualPinSequenceComboBox.Items.Add(i.ToString());
            }

            virtualPinSequenceComboBox.SelectedIndex = 0;
        }

        private async Task SendVoltageControlAsync(bool powerOn)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte mask = BuildPhaseMask(chkVoltageUa.Checked, chkVoltageUb.Checked, chkVoltageUc.Checked);
            byte[] frame = _protocol.BuildAcVoltageControlFrame(stationAddress, powerOn ? mask : (byte)0x00);
            string action = powerOn ? "上电压" : "下电压";
            await SendVirtualModuleFrameAsync(frame, $"交流电压控制命令：{action} 掩码={mask:X2}");
        }

        private async Task SendCurrentControlAsync(bool powerOn)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte mask = BuildPhaseMask(chkCurrentIa.Checked, chkCurrentIb.Checked, chkCurrentIc.Checked);
            byte[] frame = _protocol.BuildAcCurrentControlFrame(stationAddress, powerOn ? mask : (byte)0x00);
            string action = powerOn ? "上电流" : "下电流";
            await SendVirtualModuleFrameAsync(frame, $"交流电流控制命令：{action} 掩码={mask:X2}");
        }

        private async Task SendMotorPressCommandAsync(byte motorType, string motorName, bool pressed)
        {
            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            byte[] frame = _protocol.BuildMotorPressFrame(stationAddress, motorType, pressed);
            string action = pressed ? "压接" : "断开";
            await SendVirtualModuleFrameAsync(frame, $"电机压接命令：{motorName}{action}");
        }

        private bool TryGetRemoteControlStationAddress(out byte stationAddress)
        {
            stationAddress = 0;
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return false;
            }

            return TryGetStationAddress(out stationAddress, out _);
        }

        private ushort BuildRemoteSignalStateMask()
        {
            return BuildCheckBoxMask16(remoteSignalStateCheckBoxes);
        }

        private static byte BuildCheckBoxMask(IReadOnlyList<CheckBox> checkBoxes)
        {
            byte mask = 0;
            for (int i = 0; i < checkBoxes.Count && i < 8; i++)
            {
                if (checkBoxes[i].Checked)
                {
                    mask |= (byte)(1 << i);
                }
            }

            return mask;
        }

        private static ushort BuildCheckBoxMask16(IReadOnlyList<CheckBox> checkBoxes, int maxBits = 16)
        {
            ushort mask = 0;
            for (int i = 0; i < checkBoxes.Count && i < maxBits; i++)
            {
                if (checkBoxes[i].Checked)
                {
                    mask |= (ushort)(1 << i);
                }
            }

            return mask;
        }

        private bool TryParseUInt16(TextBox? textBox, string name, out ushort value)
        {
            value = 0;
            if (ushort.TryParse(textBox?.Text.Trim(), out value))
            {
                return true;
            }

            PublishLog($"{name}格式不正确。");
            return false;
        }

        private bool TryParseUInt32(TextBox? textBox, string name, out uint value)
        {
            value = 0;
            if (uint.TryParse(textBox?.Text.Trim(), out value))
            {
                return true;
            }

            PublishLog($"{name}格式不正确。");
            return false;
        }

        private bool TryParseByte(TextBox? textBox, string name, out byte value)
        {
            value = 0;
            if (byte.TryParse(textBox?.Text.Trim(), out value))
            {
                return true;
            }

            PublishLog($"{name}格式不正确。");
            return false;
        }

        private static byte GetSelectedIndexByte(ComboBox? comboBox, int offset)
        {
            int selectedIndex = comboBox?.SelectedIndex ?? 0;
            return (byte)(selectedIndex + offset);
        }

        private byte GetImpedanceFunctionCode()
        {
            int selectedIndex = impedanceFunctionComboBox?.SelectedIndex ?? 0;
            return (byte)Math.Clamp(selectedIndex, 0, 12);
        }

        private static byte BuildPhaseMask(bool phaseA, bool phaseB, bool phaseC)
        {
            byte mask = 0;
            if (phaseA) mask |= 0x01;
            if (phaseB) mask |= 0x02;
            if (phaseC) mask |= 0x04;
            return mask;
        }

        private byte GetVirtualModuleMode()
        {
            return virtualModuleModeComboBox.Text.Trim() == "APP" ? (byte)0x02 : (byte)0x01;
        }

        private bool TryGetVirtualModuleTypeCode(out byte typeCode)
        {
            typeCode = 0;

            if (virtualModuleTypeComboBox.SelectedItem is not VirtualModuleTypeItem item)
            {
                PublishLog("请选择虚拟模组类型。");
                return false;
            }

            typeCode = item.Code;
            return true;
        }

        private void LoadVirtualModuleTypeItems(byte mode)
        {
            virtualModuleTypeComboBox.Items.Clear();

            VirtualModuleTypeItem[] items = mode == 0x01
                ? [
                    new(0x01, "远程通信模块（3个ACM通道）"),
                    new(0x02, "本地通信模块（2个ACM通道）"),
                    new(0x03, "RS485/CAN/MBUS通信模块(4路-5个ACM通道)"),
                    new(0x04, "脉冲/遥信/遥控/模拟量采集通信模块（2个ACM通道）"),
                    new(0x05, "RS485/CAN/MBUS通信模块(2路3个ACM通道)"),
                    new(0x06, "RS485/遥信通信模块（4路）4ACM")
                ]
                : [
                    new(0x01, "远程通信模块（3个ACM通道）"),
                    new(0x02, "本地通信模块（2个ACM通道）"),
                    new(0x03, "设置2路RS485"),
                    new(0x04, "设置4路RS485"),
                    new(0x05, "设置2路MBUS"),
                    new(0x06, "设置4路MBUS"),
                    new(0x07, "设置1路CAN"),
                    new(0x08, "设置2路CAN"),
                    new(0x09, "设置4路CAN"),
                    new(0x0A, "设置2路脉冲/遥信"),
                    new(0x0B, "设置4路脉冲/遥信"),
                    new(0x0C, "设置2路遥控"),
                    new(0x0D, "设置模拟量采集模块"),
                    new(0x0E, "设置回路状态巡检模块")
                ];

            virtualModuleTypeComboBox.Items.AddRange(items);
            virtualModuleTypeComboBox.SelectedIndex = 0;
        }

        private async Task SendCarrierPinLevelSetCommandAsync(byte pinType, string pinName, bool highLevel)
        {
            if (_tcpClient?.IsConnected != true)
            {
                PublishLog("请先连接终端。");
                return;
            }

            if (!TryGetStationAddress(out byte stationAddress, out _))
            {
                return;
            }

            if (!TryGetModuleNumber(out byte moduleNumber))
            {
                return;
            }

            byte[] frame = _protocol.BuildCarrierModulePinLevelSetFrame(stationAddress, moduleNumber, pinType, highLevel);
            string levelName = highLevel ? "高电平" : "低电平";
            PublishLog($"发送设置载波模组{pinName}引脚{levelName}命令：模块={moduleNumber}，{DetectionBoardProtocolV2.ToHexString(frame)}");

            bool sent = await _tcpClient.SendBytesAsync(frame);
            PublishLog(sent ? $"设置载波模组{pinName}引脚{levelName}命令发送成功。" : $"设置载波模组{pinName}引脚{levelName}命令发送失败。");
        }

        private void EnsureTcpClient()
        {
            if (_tcpClient != null)
            {
                return;
            }

            _tcpClient = new EnhancedTcpClient("TerminalV2");
            _tcpClient.EnableAutoReconnect = false;
            _tcpClient.EnableHeartbeat = false;
            _tcpClient.EnableInactivityProbe = false;
            _tcpClient.MessageReceived += TcpClient_MessageReceived;
            _tcpClient.ConnectionStatusChanged += TcpClient_ConnectionStatusChanged;
            _tcpClient.ErrorOccurred += TcpClient_ErrorOccurred;
        }

        private void DisposeTcpClient()
        {
            _isDisposing = true;

            if (_tcpClient != null)
            {
                _tcpClient.MessageReceived -= TcpClient_MessageReceived;
                _tcpClient.ConnectionStatusChanged -= TcpClient_ConnectionStatusChanged;
                _tcpClient.ErrorOccurred -= TcpClient_ErrorOccurred;
                _tcpClient.Disconnect();
                _tcpClient = null;
            }
        }

        private void DisconnectTcpClient()
        {
            string endpoint = _tcpClient?.ServerEndpoint ?? $"{ipTextBox.Text.Trim()}:{portTextBox.Text.Trim()}";
            if (_tcpClient != null)
            {
                _tcpClient.MessageReceived -= TcpClient_MessageReceived;
                _tcpClient.ConnectionStatusChanged -= TcpClient_ConnectionStatusChanged;
                _tcpClient.ErrorOccurred -= TcpClient_ErrorOccurred;
            }

            _tcpClient?.Disconnect();
            _tcpClient = null;
            SetConnectionUiState("未连接", false);
            PublishLog($"终端测试单元V2已断开：{endpoint}");
        }

        private void TcpClient_MessageReceived(object sender, TcpClientMessageEventArgs e)
        {
            UpdateUI(() =>
            {
                if (_protocol.TryParseFrame(e.RawData, out DetectionBoardProtocolV2Frame? frame, out string error))
                {
                    ultrSimpleDisplayV2.HandleReceivedMessage(BitConverter.ToString(e.RawData).Replace("-", string.Empty));

                    if (frame!.CommandCode == 0x07 && _protocol.TryParseCarrierModuleDcMeasureData(frame.Data, out CarrierModuleDcMeasureData? measureData, out string measureError))
                    {
                        UpdateMeasureDisplay(measureData!);
                    }

                    if (frame!.CommandCode == 0x24 && _protocol.TryParseMeterVoltageLoopPowerData(frame.Data, out MeterVoltageLoopPowerData? powerData, out string powerError))
                    {
                        UpdateMeterVoltageLoopPowerDisplay(powerData!);
                    }

                    PublishLog(
                        $"终端测试单元V2收到协议帧：方向={frame!.Direction:X2} 地址={frame.Address:X2} 类型={frame.ProtocolType:X2} 命令={frame.CommandCode:X2} 数据={DetectionBoardProtocolV2.ToHexString(frame.Data)}");
                    return;
                }

                PublishLog($"终端测试单元V2收到非完整协议帧：{DetectionBoardProtocolV2.ToHexString(e.RawData)}，解析结果：{error}");
            });
        }

        private void TcpClient_ConnectionStatusChanged(object sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                string status = e.IsConnected ? $"已连接：{_tcpClient?.ServerEndpoint}" : e.Status;
                SetConnectionUiState(status, e.IsConnected);
                PublishLog($"终端测试单元V2状态：{e.Status}");
            });
        }

        private void TcpClient_ErrorOccurred(object sender, string errorMessage)
        {
            UpdateUI(() => PublishLog($"终端测试单元V2错误：{errorMessage}"));
        }

        private void SetConnectionUiState(string status, bool connected)
        {
            lblStatus.Text = $"状态：{status}";
            connectButton.Text = connected ? "断开" : "连接";
            ipTextBox.Enabled = !connected;
            portTextBox.Enabled = !connected;
        }

        private bool TryGetStationHexText(out string stationHex)
        {
            if (TryGetStationAddress(out _, out stationHex))
            {
                return true;
            }

            stationHex = string.Empty;
            return false;
        }

        private bool TryGetStationAddress(out byte stationAddress, out string stationHex)
        {
            stationAddress = 0;
            stationHex = string.Empty;

            try
            {
                stationAddress = _protocol.ConvertStationDecimalToByte(stationTextBox.Text);
                stationHex = stationAddress.ToString("X2");
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
            {
                PublishLog(ex.Message);
                return false;
            }
        }

        private bool TryGetModuleNumber(out byte moduleNumber)
        {
            moduleNumber = 0;

            if (!byte.TryParse(moduleNumberTextBox.Text.Trim(), out byte value) || value < 1 || value > 5)
            {
                PublishLog("模块号必须是 1~5。");
                return false;
            }

            moduleNumber = value;
            return true;
        }

        private void PublishLog(string message)
        {
            if (_isDisposing)
            {
                return;
            }

            OnUpdateRequestedTerminalV2Log?.Invoke(message);
        }

        private bool TryGetMeasureModuleType(out byte moduleType)
        {
            moduleType = 0;

            string text = measureModuleTypeComboBox.Text.Trim();
            if (text == "真实模组")
            {
                moduleType = 0x01;
                return true;
            }

            if (text == "sta模组" || text == "STA模组")
            {
                moduleType = 0x02;
                return true;
            }

            PublishLog("请选择正确的模组类型。");
            return false;
        }

        private bool TryGetMeasureReadItem(out byte readItem)
        {
            readItem = 0;

            string text = measureReadItemComboBox.Text.Trim();
            if (text == "读取电压")
            {
                readItem = 0x01;
                return true;
            }

            if (text == "读取电流")
            {
                readItem = 0x02;
                return true;
            }

            if (text == "读取电压电流")
            {
                readItem = 0x03;
                return true;
            }

            if (text == "读取功耗")
            {
                readItem = 0x04;
                return true;
            }

            PublishLog("请选择正确的读取项。");
            return false;
        }

        private bool TryGetMeasureModeOrRate(out byte modeOrRate)
        {
            modeOrRate = 0;

            if (measureModeComboBox.Text.Trim() == "连续读取")
            {
                modeOrRate = 0x01;
                return true;
            }

            if (measureModeComboBox.Text.Trim() == "停止连续")
            {
                modeOrRate = 0x00;
                return true;
            }

            if (measureModeComboBox.Text.Trim() == "单次读取")
            {
                modeOrRate = 0x02;
                return true;
            }

            PublishLog("请选择正确的读取方式。");
            return false;
        }

        private byte GetMeterVoltageLoopPowerOperation()
        {
            return meterVoltageLoopPowerComboBox.Text.Trim() switch
            {
                "A相功率" => 0x01,
                "B相功率" => 0x02,
                "C相功率" => 0x03,
                "合相功率" => 0x04,
                "校准/保存" => 0xAA,
                "复位" => 0xBB,
                _ => 0x01
            };
        }

        private void UpdateMeasureDisplay(CarrierModuleDcMeasureData measureData)
        {
            measureResultTextBox.Clear();
            measureResultTextBox.AppendText($"模组类型: {GetMeasureModuleTypeName(measureData.ModuleType)}{Environment.NewLine}");
            measureResultTextBox.AppendText($"模块号: {measureData.ModuleNumber}{Environment.NewLine}");
            measureResultTextBox.AppendText($"读取项: {GetMeasureReadItemName(measureData.ReadItem)}{Environment.NewLine}");
            measureResultTextBox.AppendText($"控制字节: {measureData.ReadModeOrRate:X2}{Environment.NewLine}");
            measureResultTextBox.AppendText($"原始数据: {DetectionBoardProtocolV2.ToHexString(measureData.Payload)}{Environment.NewLine}");
            measureResultTextBox.AppendText($"解析数据: {FormatMeasurePayload(measureData)}{Environment.NewLine}");
        }

        private void UpdateMeterVoltageLoopPowerDisplay(MeterVoltageLoopPowerData powerData)
        {
            measureResultTextBox.Clear();
            measureResultTextBox.AppendText($"读取项: 表位电压回路功率{Environment.NewLine}");
            measureResultTextBox.AppendText($"相位: {GetMeterVoltageLoopPowerPhaseName(powerData.Phase)}{Environment.NewLine}");
            measureResultTextBox.AppendText($"有功功率: {powerData.ActivePower:F3}{Environment.NewLine}");
            measureResultTextBox.AppendText($"无功功率: {powerData.ReactivePower:F3}{Environment.NewLine}");
            measureResultTextBox.AppendText($"视在功率: {powerData.ApparentPower:F3}{Environment.NewLine}");
        }

        private static string FormatMeasurePayload(CarrierModuleDcMeasureData measureData)
        {
            if (measureData.Payload.Length == 0)
            {
                return "无";
            }

            if (measureData.ReadItem == 0x03)
            {
                if (measureData.Payload.Length % 4 != 0)
                {
                    return "数据长度不是4的倍数，无法按电压/电流解析。";
                }

                List<string> values = [];
                for (int i = 0; i < measureData.Payload.Length; i += 4)
                {
                    ushort voltage = ReadUInt16LittleEndian(measureData.Payload, i);
                    ushort current = ReadUInt16LittleEndian(measureData.Payload, i + 2);
                    values.Add($"电压={voltage}mV 电流={current}mA");
                }

                return string.Join("；", values);
            }

            if (measureData.Payload.Length % 2 != 0)
            {
                return "数据长度不是2的倍数，无法按u16解析。";
            }

            string unit = measureData.ReadItem switch
            {
                0x01 => "mV",
                0x02 => "mA",
                0x04 => "mW",
                _ => string.Empty
            };

            List<string> items = [];
            for (int i = 0; i < measureData.Payload.Length; i += 2)
            {
                items.Add($"{ReadUInt16LittleEndian(measureData.Payload, i)}{unit}");
            }

            return string.Join("；", items);
        }

        private static ushort ReadUInt16LittleEndian(byte[] data, int startIndex)
        {
            return (ushort)(data[startIndex] | (data[startIndex + 1] << 8));
        }

        private static string GetMeasureModuleTypeName(byte moduleType)
        {
            return moduleType switch
            {
                0x01 => "真实模组",
                0x02 => "sta模组",
                _ => $"未知({moduleType:X2})"
            };
        }

        private static string GetMeasureReadItemName(byte readItem)
        {
            return readItem switch
            {
                0x01 => "读取电压",
                0x02 => "读取电流",
                0x03 => "读取电压电流",
                0x04 => "读取功耗",
                _ => $"未知({readItem:X2})"
            };
        }

        private static string GetMeterVoltageLoopPowerPhaseName(byte phase)
        {
            return phase switch
            {
                0x01 => "A相",
                0x02 => "B相",
                0x03 => "C相",
                0x04 => "合相",
                _ => $"未知({phase:X2})"
            };
        }

        private void UpdateUI(Action action)
        {
            if (IsDisposed || _isDisposing)
            {
                return;
            }

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(action);
                return;
            }

            action();
        }

        private sealed record VirtualModuleTypeItem(byte Code, string Name)
        {
            public override string ToString() => Name;
        }

    }
}
