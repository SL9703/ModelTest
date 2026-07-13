using System.Text;
using ModelTest.Tools;

namespace ModelTest.CustomControl
{
    /// <summary>
    /// 国家电网加密机用户控件。
    /// 负责连接管理、接口筛选、默认参数回填、加密调用以及日志回传。
    /// </summary>
    public partial class SGCCEncryptionServiceUserControl : UserControl
    {
        public delegate void UpdateMainFormDelegate(string message, Color? color = null);

        private const string WinSocketConfigSection = "WinSocketServer";
        private const string WinSocketIpKey = "ip";
        private const string WinSocketPortKey = "port";
        private const string DefaultWinSocketIp = "22.58.244.70";
        private const string DefaultWinSocketPort = "8001";
        private const int ServerImpLazyPageSize = 80;
        private const string ServerImpPlaceholderText = "输入关键字搜索接口";
        private static readonly string ConfigPath = Path.Combine(Application.StartupPath, "XCKJcomfig.ini");

        private readonly WinSocketServer _winSocketServer = new();
        private readonly WinSocketUiContext _winSocketUiContext;
        private readonly System.Windows.Forms.Timer _heartbeatTimer = new();
        private readonly Color _segmentCheckedBackColor = Color.FromArgb(242, 196, 55);
        private readonly Color _segmentCheckedHoverBackColor = Color.FromArgb(250, 206, 78);
        private readonly Color _segmentUncheckedBackColor = Color.FromArgb(104, 156, 137);
        private readonly Color _segmentUncheckedHoverBackColor = Color.FromArgb(118, 170, 150);
        private readonly Color _segmentCheckedForeColor = Color.FromArgb(37, 47, 41);
        private readonly Color _segmentUncheckedForeColor = Color.FromArgb(233, 240, 236);

        // 用于避免重复登录、重复心跳和控件联动时的重入。
        private bool _heartbeatRunning;
        private bool _loginInProgress;
        private bool _serverImpUpdating;
        private bool _keyModeUpdating;

        // 当前默认参数的来源 key。
        // 菜单项和接口名可能不是一一对应，因此这里优先保留菜单项文本。
        private string? _currentDefaultParameterKey;

        // 右键菜单展示名称到真实接口名的映射。
        private readonly Dictionary<string, string> _terminalEncryptionFunctionMap = new()
        {
            ["（698）终端对称密钥更新"] = "Obj_Terminal_Formal_GetTrmKeyData",
            ["（698）终端任务数据"] = "Obj_Terminal_Formal_GetTerminalSetData",
            ["（698）终端任务数据-密钥更新"] = "Obj_Terminal_Formal_GetTerminalSetData",
            ["（698）验证终端返回数据"] = "Obj_Terminal_Formal_VerifyTerminalData",
            ["（698）主站会话协商数据"] = "Obj_Terminal_Formal_InitSession",
            ["（698）主站会话协商验证"] = "Obj_Terminal_Formal_VerifySession",
            ["（698）主站会话协商数据-IFT"] = "Obj_Terminal_Formal_InitSession_RH",
            ["（698）主站会话协商验证-IFT"] = "Obj_Terminal_Formal_VerifySession_RH",
            ["（698）安全传输加密-IFT"] = "Obj_Terminal_Formal_GetSessionData_RH",
            ["（698）终端对称密钥更新-IFT"] = "Obj_Terminal_Formal_GetTrmKeyData_RH",
            ["（698）终端对称密钥恢复-IFT"] = "Obj_Terminal_Formal_GetTrmKeyData_RH",
            ["（配电）终端对称密钥更新"] = "RH_Terminal_Formal_GetTrmKeyData",
            ["（配电）配电主站签名"] = "RH_InternalSign",
            ["（配电）主站验证签名"] = "RH_VerifySig",
            ["（配电）安全传输加密"] = "RH_EncData",
            ["（配电）获取签名主站证书"] = "RH_CertificateDataUpdate_PD",
        };

        // 电表加密菜单展示名称到真实接口名的映射。
        // 645/698 的菜单语义不同，但当前底层可调用接口是同一组电表函数，因此统一映射到现有实现。
        private readonly Dictionary<string, string> _meterEncryptionFunctionMap = new()
        {
            ["（698）身份认证函数"] = "Obj_Meter_Formal_GetSessionData",
            ["（698）远程充值函数"] = "Obj_Meter_Formal_GetPurseData",
            ["（698）电表对称密钥更新"] = "Obj_Meter_Formal_GetTrmKeyData",
            ["（698）电表对称密钥恢复"] = "Obj_Meter_Formal_GetTrmKeyData",
            ["（645）身份认证函数"] = "Meter_Formal_IdentityAuthentication",
            ["（645）远程控制函数"] = "Meter_Formal_UserControl",
            ["（645）远程充值函数"] = "Meter_Formal_InCreasePurse",
            ["（645）电表对称密钥更新"] = "Meter_Formal_KeyUpdateV2",
            ["（645）电表对称密钥恢复"] = "Meter_Formal_KeyUpdateV2",
        };

        public event UpdateMainFormDelegate? OnUpdateRequestedEncryptionLog;

        public SGCCEncryptionServiceUserControl()
        {
            _winSocketUiContext = new WinSocketUiContext(_winSocketServer, message => PublishEncryptionLog(message));
            InitializeComponent();
            BackColor = Color.FromArgb(88, 149, 127);

            _heartbeatTimer.Interval = 30_000;
            _heartbeatTimer.Tick += HeartbeatTimer_Tick;

            LoadWinSocketConfig();
            InitializeServerImp();
            InitializeTerminalEncryptionMenu();
            InitializeMeterEncryptionMenu();
            UpdateKeyModeVisualState();
        }

        /// <summary>
        /// 初始化接口下拉框的行为。
        /// 这里把搜索、占位提示和 hover 样式都挂到同一个入口里，避免 Designer 事件分散。
        /// </summary>
        private void InitializeServerImp()
        {
            _winSocketUiContext.ResetCatalog();

            cbxServerImp.DropDownStyle = ComboBoxStyle.DropDown;
            cbxServerImp.Text = ServerImpPlaceholderText;
            cbxServerImp.SelectedIndex = -1;

            cbxServerImp.DropDown -= CbxServerImp_DropDown;
            cbxServerImp.TextUpdate -= CbxServerImp_TextUpdate;
            cbxServerImp.Enter -= CbxServerImp_Enter;
            cbxServerImp.Leave -= CbxServerImp_Leave;
            cbxServerImp.DropDown += CbxServerImp_DropDown;
            cbxServerImp.TextUpdate += CbxServerImp_TextUpdate;
            cbxServerImp.Enter += CbxServerImp_Enter;
            cbxServerImp.Leave += CbxServerImp_Leave;
            cbxPublicKey.MouseEnter += SegmentCheckBox_MouseEnter;
            cbxPublicKey.MouseLeave += SegmentCheckBox_MouseLeave;
            cbxPrivateKey.MouseEnter += SegmentCheckBox_MouseEnter;
            cbxPrivateKey.MouseLeave += SegmentCheckBox_MouseLeave;
        }

        /// <summary>
        /// 初始化“终端加密函数调用”菜单。
        /// 菜单项展示业务名称，Tag 保存实际接口名。
        /// </summary>
        private void InitializeTerminalEncryptionMenu()
        {
            cmsTerminalEncryptionFunctions.Items.Clear();

            foreach (var entry in _terminalEncryptionFunctionMap)
            {
                if (entry.Key == "（698）主站会话协商数据-IFT")
                {
                    cmsTerminalEncryptionFunctions.Items.Add(new ToolStripSeparator());
                }

                if (entry.Key == "（配电）终端对称密钥更新")
                {
                    cmsTerminalEncryptionFunctions.Items.Add(new ToolStripSeparator());
                }

                var item = new ToolStripMenuItem(entry.Key)
                {
                    Tag = entry.Value
                };
                item.Click += TerminalEncryptionMenuItem_Click;
                cmsTerminalEncryptionFunctions.Items.Add(item);
            }
        }

        /// <summary>
        /// 初始化“电表加密函数调用”菜单。
        /// 菜单项展示业务名称，Tag 保存实际接口名。
        /// </summary>
        private void InitializeMeterEncryptionMenu()
        {
            cmsMeterEncryptionFunctions.Items.Clear();

            foreach (var entry in _meterEncryptionFunctionMap)
            {
                if (entry.Key == "（645）身份认证函数")
                {
                    cmsMeterEncryptionFunctions.Items.Add(new ToolStripSeparator());
                }

                var item = new ToolStripMenuItem(entry.Key)
                {
                    Tag = entry.Value
                };
                item.Click += MeterEncryptionMenuItem_Click;
                cmsMeterEncryptionFunctions.Items.Add(item);
            }
        }

        private bool UsePrivateKey => cbxPrivateKey.Checked;

        /// <summary>
        /// 从 ini 读取加密机 IP/Port；首次启动没有配置时写入默认值。
        /// </summary>
        private void LoadWinSocketConfig()
        {
            string ip = Confighelper.ReadIni(WinSocketConfigSection, WinSocketIpKey, "", 255, ConfigPath).Trim();
            string port = Confighelper.ReadIni(WinSocketConfigSection, WinSocketPortKey, "", 255, ConfigPath).Trim();

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = DefaultWinSocketIp;
                Confighelper.WriteIni(WinSocketConfigSection, WinSocketIpKey, ip, ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(port))
            {
                port = DefaultWinSocketPort;
                Confighelper.WriteIni(WinSocketConfigSection, WinSocketPortKey, port, ConfigPath);
            }

            tbxServerIp.Text = ip;
            tbxServerPort.Text = port;
        }

        /// <summary>
        /// 登录成功后把当前现场配置持久化，便于下次直接复用。
        /// </summary>
        private void SaveWinSocketConfig(string ip, string port)
        {
            Confighelper.WriteIni(WinSocketConfigSection, WinSocketIpKey, ip, ConfigPath);
            Confighelper.WriteIni(WinSocketConfigSection, WinSocketPortKey, port, ConfigPath);
        }

        /// <summary>
        /// 登录按钮既承担“连接”也承担“断开”。
        /// 真实的登录/断开判断在 WinSocketLoginService 中完成。
        /// </summary>
        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (_loginInProgress)
            {
                return;
            }

            _loginInProgress = true;
            SetLoginButtonState(false, "连接中...");

            try
            {
                string serverIp = tbxServerIp.Text.Trim();
                string serverPort = tbxServerPort.Text.Trim();
                tbxServerIp.Text = serverIp;
                tbxServerPort.Text = serverPort;

                WinSocketLoginOperationResult result = await _winSocketUiContext.ExecuteLoginAsync(serverIp, serverPort);
                ApplyLoginResult(result);
            }
            finally
            {
                _loginInProgress = false;
                SetLoginButtonState(true, _winSocketUiContext.IsConnected ? "断开服务器" : "登录加密机");
            }
        }

        /// <summary>
        /// 心跳只在登录成功后启动，用于维持会话并观察加密机链路状态。
        /// </summary>
        private async void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            if (_heartbeatRunning)
            {
                return;
            }

            const int flag = 0;
            const string putDiv = "0000000000000001";
            PublishEncryptionLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 发送心跳身份认证数据:flag={flag};putDiv={putDiv}");

            _heartbeatRunning = true;
            try
            {
                WinSocketServiceInvoker.IdentityHeartbeatResult result = await Task.Run(() => _winSocketUiContext.SendHeartbeat(flag, putDiv));
                PublishEncryptionLog($"数据连接测试, 接收心跳身份认证数据:{DateTime.Now:yyyy-MM-dd HH:mm:ss}result={result.Code};outRand={result.OutRand};outEndata={result.OutEndata}");
            }
            catch (Exception ex)
            {
                PublishEncryptionLog($"数据连接测试, 接收心跳身份认证数据:{DateTime.Now:yyyy-MM-dd HH:mm:ss}result=异常;outRand=;outEndata=;error={ex.Message}");
            }
            finally
            {
                _heartbeatRunning = false;
            }
        }

        /// <summary>
        /// 将登录结果统一回写到界面状态。
        /// 包括按钮文字、输入框只读状态、心跳启停以及日志输出。
        /// </summary>
        private void ApplyLoginResult(WinSocketLoginOperationResult result)
        {
            if (result.ShouldStopHeartbeat)
            {
                StopHeartbeat();
            }

            lblStatusValue.Text = result.StatusText;
            tbxServerIp.ReadOnly = result.IsConnected;
            tbxServerPort.ReadOnly = result.IsConnected;
            PublishEncryptionLog(result.Message);

            if (result.ShouldStartHeartbeat)
            {
                StartHeartbeat();
            }

            tbxServerIp.Text = result.ServerIp;
            tbxServerPort.Text = result.ServerPort;

            if (result.IsConnected)
            {
                SaveWinSocketConfig(result.ServerIp, result.ServerPort);
            }
        }

        /// <summary>
        /// 启动周期心跳。
        /// </summary>
        private void StartHeartbeat()
        {
            if (!_heartbeatTimer.Enabled)
            {
                _heartbeatTimer.Start();
            }
        }

        /// <summary>
        /// 停止周期心跳。
        /// </summary>
        private void StopHeartbeat()
        {
            _heartbeatTimer.Stop();
            _heartbeatRunning = false;
        }

        /// <summary>
        /// 登录按钮在异步期间切到不可点击态，避免重复发送登录/断开。
        /// </summary>
        private void SetLoginButtonState(bool enabled, string text)
        {
            btnLogin.Enabled = enabled;
            btnLogin.Text = text;
        }

        /// <summary>
        /// 执行当前选中接口，并把返回内容仅展示到控件内部输出框。
        /// 全局日志仍通过 WinSocketUiContext 回传给主界面。
        /// </summary>
        private void BtnEncrypt_Click(object? sender, EventArgs e)
        {
            ExecuteCurrentService();
        }

        /// <summary>
        /// 在按钮下方弹出终端加密函数菜单。
        /// </summary>
        private void BtnTerminalEncryptionMenu_Click(object? sender, EventArgs e)
        {
            cmsTerminalEncryptionFunctions.Show(btnTerminalEncryptionMenu, 0, btnTerminalEncryptionMenu.Height);
        }

        /// <summary>
        /// 在按钮下方弹出电表加密函数菜单。
        /// </summary>
        private void BtnMeterEncryptionMenu_Click(object? sender, EventArgs e)
        {
            cmsMeterEncryptionFunctions.Show(btnMeterEncryptionMenu, 0, btnMeterEncryptionMenu.Height);
        }

        /// <summary>
        /// 通过下拉框直接选中接口时，默认参数按“接口名”刷新。
        /// 这是与右键菜单选择的唯一区别。
        /// </summary>
        private void CbxServerImp_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_serverImpUpdating)
            {
                return;
            }

            string? serviceName = cbxServerImp.SelectedItem as string;
            _currentDefaultParameterKey = serviceName;
            ApplyDefaultParameters(_currentDefaultParameterKey, serviceName);

            string description = _winSocketUiContext.GetParameterDescription(serviceName);
            if (!string.IsNullOrWhiteSpace(description))
            {
                ShowParameterDescription(description);
            }
        }

        /// <summary>
        /// 菜单项选择时，同时记录菜单文本和真实接口名。
        /// 后续默认参数优先按菜单文本回填，这样同一接口可区分不同业务入口。
        /// </summary>
        private void TerminalEncryptionMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string serviceName)
            {
                return;
            }

            SelectService(item.Text ?? serviceName, serviceName, executeImmediately: true);
        }

        /// <summary>
        /// 电表加密菜单项选择时，直接切换接口并立即执行。
        /// </summary>
        private void MeterEncryptionMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string serviceName)
            {
                return;
            }

            SelectService(item.Text ?? serviceName, serviceName, executeImmediately: true);
        }

        /// <summary>
        /// 选中接口并刷新说明/默认参数。
        /// 如果接口在下拉框中存在，则同步选中；否则只写文本。
        /// </summary>
        private void SelectService(string parameterKey, string serviceName, bool executeImmediately = false)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return;
            }

            _currentDefaultParameterKey = parameterKey;
            int itemIndex = cbxServerImp.FindStringExact(serviceName);
            if (itemIndex >= 0)
            {
                _serverImpUpdating = true;
                try
                {
                    cbxServerImp.SelectedIndex = itemIndex;
                }
                finally
                {
                    _serverImpUpdating = false;
                }
            }
            else
            {
                cbxServerImp.Text = serviceName;
            }

            ApplyDefaultParameters(_currentDefaultParameterKey, serviceName);

            string description = _winSocketUiContext.GetParameterDescription(serviceName);
            if (!string.IsNullOrWhiteSpace(description))
            {
                ShowParameterDescription(description);
            }

            if (executeImmediately)
            {
                ExecuteCurrentService();
            }
        }

        /// <summary>
        /// 下拉时按当前输入关键字做一次懒加载过滤。
        /// </summary>
        private void CbxServerImp_DropDown(object? sender, EventArgs e)
        {
            string filter = string.Equals(cbxServerImp.Text, ServerImpPlaceholderText, StringComparison.Ordinal)
                ? string.Empty
                : cbxServerImp.Text;
            RenderServerImpItems(filter, keepText: false);
        }

        /// <summary>
        /// 输入框获得焦点时清空占位文字，便于直接输入搜索关键字。
        /// </summary>
        private void CbxServerImp_Enter(object? sender, EventArgs e)
        {
            if (string.Equals(cbxServerImp.Text, ServerImpPlaceholderText, StringComparison.Ordinal))
            {
                cbxServerImp.Text = string.Empty;
            }
        }

        /// <summary>
        /// 失焦后如果没有输入内容，则恢复占位提示。
        /// </summary>
        private void CbxServerImp_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbxServerImp.Text))
            {
                cbxServerImp.Text = ServerImpPlaceholderText;
                cbxServerImp.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// 公钥/私钥做成互斥分段按钮。
        /// 这里只处理“选中公钥”这个方向，另一侧由 UpdateKeyMode 统一回写。
        /// </summary>
        private void CbxPublicKey_CheckedChanged(object? sender, EventArgs e)
        {
            if (_keyModeUpdating || !cbxPublicKey.Checked)
            {
                UpdateKeyModeVisualState();
                return;
            }

            UpdateKeyMode(isPrivateKey: false);
        }

        /// <summary>
        /// 私钥分段按钮的互斥处理。
        /// </summary>
        private void CbxPrivateKey_CheckedChanged(object? sender, EventArgs e)
        {
            if (_keyModeUpdating || !cbxPrivateKey.Checked)
            {
                UpdateKeyModeVisualState();
                return;
            }

            UpdateKeyMode(isPrivateKey: true);
        }

        /// <summary>
        /// 统一切换公钥/私钥状态，并按当前选中接口重新回填一套默认参数。
        /// </summary>
        private void UpdateKeyMode(bool isPrivateKey)
        {
            _keyModeUpdating = true;
            try
            {
                cbxPrivateKey.Checked = isPrivateKey;
                cbxPublicKey.Checked = !isPrivateKey;
            }
            finally
            {
                _keyModeUpdating = false;
            }

            UpdateKeyModeVisualState();
            RefreshDefaultParametersForCurrentService();
        }

        /// <summary>
        /// 公私钥切换后刷新当前接口默认参数。
        /// 菜单选择优先用菜单 key；普通下拉选择则退化为接口名。
        /// </summary>
        private void RefreshDefaultParametersForCurrentService()
        {
            string? serviceName = cbxServerImp.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = cbxServerImp.Text;
            }

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return;
            }

            ApplyDefaultParameters(_currentDefaultParameterKey ?? serviceName, serviceName);
        }

        /// <summary>
        /// 根据“菜单项 key + 接口名 + 公私钥”三元组合回填参数。
        /// </summary>
        private void ApplyDefaultParameters(string? selectionKey, string? serviceName)
        {
            string defaultParameters = _winSocketUiContext.GetDefaultParameters(selectionKey, serviceName, UsePrivateKey);
            if (!string.IsNullOrWhiteSpace(defaultParameters))
            {
                tbxParameters.Text = defaultParameters;
            }
        }

        /// <summary>
        /// 刷新分段按钮的选中/未选中外观。
        /// </summary>
        private void UpdateKeyModeVisualState()
        {
            ApplySegmentedCheckBoxStyle(cbxPublicKey, cbxPublicKey.Checked, isHovering: false);
            ApplySegmentedCheckBoxStyle(cbxPrivateKey, cbxPrivateKey.Checked, isHovering: false);
        }

        /// <summary>
        /// 统一应用 segmented button 的配色。
        /// </summary>
        private void ApplySegmentedCheckBoxStyle(CheckBox checkBox, bool isChecked, bool isHovering)
        {
            checkBox.BackColor = isChecked
                ? (isHovering ? _segmentCheckedHoverBackColor : _segmentCheckedBackColor)
                : (isHovering ? _segmentUncheckedHoverBackColor : _segmentUncheckedBackColor);
            checkBox.ForeColor = isChecked ? _segmentCheckedForeColor : _segmentUncheckedForeColor;
        }

        /// <summary>
        /// hover 时提高颜色对比度，增强按钮反馈。
        /// </summary>
        private void SegmentCheckBox_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ApplySegmentedCheckBoxStyle(checkBox, checkBox.Checked, isHovering: true);
            }
        }

        /// <summary>
        /// 离开 hover 时恢复常规状态颜色。
        /// </summary>
        private void SegmentCheckBox_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ApplySegmentedCheckBoxStyle(checkBox, checkBox.Checked, isHovering: false);
            }
        }

        /// <summary>
        /// 接口名称实时搜索。
        /// 输入内容时只刷新候选项，不主动覆盖用户当前文本。
        /// </summary>
        private void CbxServerImp_TextUpdate(object? sender, EventArgs e)
        {
            if (_serverImpUpdating)
            {
                return;
            }

            RenderServerImpItems(cbxServerImp.Text, keepText: true);
            cbxServerImp.DroppedDown = true;
            cbxServerImp.SelectionStart = cbxServerImp.Text.Length;
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// 根据关键字重建下拉项。
        /// 使用 _serverImpUpdating 防止 Items 重建时触发 SelectedIndexChanged 干扰当前状态。
        /// </summary>
        private void RenderServerImpItems(string filter, bool keepText)
        {
            string currentText = keepText ? cbxServerImp.Text : string.Empty;
            int selectionStart = cbxServerImp.SelectionStart;
            string[] items = _winSocketUiContext.GetFilteredServiceNames(filter, ServerImpLazyPageSize);

            _serverImpUpdating = true;
            cbxServerImp.SelectedIndexChanged -= CbxServerImp_SelectedIndexChanged;
            cbxServerImp.BeginUpdate();
            try
            {
                cbxServerImp.Items.Clear();
                cbxServerImp.Items.AddRange(items);
                cbxServerImp.SelectedIndex = -1;

                if (keepText)
                {
                    cbxServerImp.Text = currentText;
                    cbxServerImp.SelectionStart = Math.Min(selectionStart, cbxServerImp.Text.Length);
                    cbxServerImp.SelectionLength = 0;
                }
            }
            finally
            {
                cbxServerImp.EndUpdate();
                cbxServerImp.SelectedIndexChanged += CbxServerImp_SelectedIndexChanged;
                _serverImpUpdating = false;
            }
        }

        /// <summary>
        /// 端口输入仅允许数字和退格。
        /// </summary>
        private void TbxServerPort_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar >= '0' && e.KeyChar <= '9') || e.KeyChar == 8)
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        /// <summary>
        /// 右下输出区只展示当前接口的参数说明。
        /// </summary>
        private void ShowParameterDescription(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(ShowParameterDescription), message);
                return;
            }

            rtbxOutput.Clear();
            rtbxOutput.AppendText(message);
        }

        /// <summary>
        /// 右下输出区只展示本次执行结果，不重复承接主界面日志。
        /// </summary>
        private void AppendExecutionResult(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendExecutionResult), message);
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (rtbxOutput.TextLength > 0)
            {
                rtbxOutput.AppendText("\r\n\r\n");
            }

            rtbxOutput.AppendText($"接口返回：{message}");
        }

        /// <summary>
        /// 统一执行当前接口。
        /// 菜单项和按钮最终都走这里，避免两套执行逻辑分叉。
        /// </summary>
        private void ExecuteCurrentService()
        {
            WinSocketServiceInvoker.ExecutionResult result = _winSocketUiContext.ExecuteService(cbxServerImp.Text, tbxParameters.Text);
            AppendExecutionResult(result.DisplayMessage);
        }

        /// <summary>
        /// 把加密机运行日志回传给主界面。
        /// </summary>
        private void PublishEncryptionLog(string message, Color? color = null)
        {
            OnUpdateRequestedEncryptionLog?.Invoke(message, color);
        }
    }
}
