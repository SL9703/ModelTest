using ModelTest.Tools;

namespace ModelTest.CustomControl
{
    public partial class SGCCTestUserControl : UserControl
    {
        private const string SGCC645BroadcastMessage = "FEFEFEFE68AAAAAAAAAAAA681300DF16";
        private const string CSG698BroadcastMessage = "6810001000684AFFFFFFFFFFFF010A710000210100E0C216";
        private const string KZHLStatusMessage = "6817004345AAAAAAAAAAAA10da5f05013DFF140200006c6816";
        private const string KZHLIdMessage = "6817004345AAAAAAAAAAAA10DA5F050127F10002000027D316";

        public event Func<string, Task>? SendMessageRequested;
        public event Action<string>? LogRequested;

        public SGCCTestUserControl()
        {
            InitializeComponent();
            BackColor = Color.FromArgb(88, 149, 127);
            cbxSgccOadCategory.DataSource = SGCCOadConfig.OadCategories.ToList();
            cbxSgccOadCategory.SelectedItem = SGCCOadConfig.EnergyCategory;
            BindSgccOadItems();
            ModelTool.BindMutexCheckBoxes(cbxSGCC_Meter, cbxSGCC_Terminal);
        }

        private async void SGCC645FF_Click(object sender, EventArgs e)
        {
            await SendAsync(SGCC645BroadcastMessage);
        }

        private async void CSG698FF_Click(object sender, EventArgs e)
        {
            await SendAsync(CSG698BroadcastMessage);
        }

        private async void buttonKZHLStatus_Click(object sender, EventArgs e)
        {
            await SendAsync(KZHLStatusMessage);
        }

        private async void buttonKZHLID_Click(object sender, EventArgs e)
        {
            await SendAsync(KZHLIdMessage);
        }

        private async void btnReadMSG_Click(object sender, EventArgs e)
        {
            const string _68H = "68";
            const string _16H = "16";
            const string Ctrl = "43";
            const string SASgin = "05";
            const int RequiredAddressLength = 12;

            string serverAddress = tbxMeterTerminalAddr.Text.Trim();
            if (serverAddress.Length != RequiredAddressLength)
            {
                WriteLog("698报文服务器地址长度不正确，应为12位");
                return;
            }

            string caAddress = cbxSGCC_Meter.Checked ? "A0" : "10";
            string reverseServerAddress = ModelTool.ReverseHexString(serverAddress);
            if (!SGCCTools.TryGetOadApdu(cbxSgccOAD.Text, out string apdu))
            {
                WriteLog("请选择要读取的国网698 OAD项目");
                return;
            }

            if (cbxSgccOAD.Text == "广播读取终端或电表地址")
            {
                reverseServerAddress = "AAAAAAAAAAAA";
            }

            string sgccMessage = SGCCTools.BytesToSGCCMessage(_68H, Ctrl, SASgin, reverseServerAddress, caAddress, apdu, _16H);
            WriteLog("国网698 APDU类型：" + SGCCTools.GetApduServiceTypeDescription(apdu));
            WriteLog("国网698 GET-Request类型：" + SGCCTools.GetApduChoiceDescription(apdu));
            WriteLog("国网698 请求PID：" + SGCCTools.GetApduPiidDescription(apdu));
            WriteLog("国网698 功能OAD：" + SGCCTools.GetApduOadDescription(apdu));
            await SendAsync(sgccMessage);
        }

        private void cbxSgccOadCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindSgccOadItems();
        }

        private void BindSgccOadItems()
        {
            string category = cbxSgccOadCategory.Text;
            cbxSgccOAD.DataSource = SGCCOadConfig.GetServiceNamesByCategory(category).ToList();
            cbxSgccOAD.SelectedIndex = -1;
        }

        private async Task SendAsync(string message)
        {
            if (SendMessageRequested == null)
            {
                WriteLog("国网测试发送事件未绑定");
                return;
            }

            await SendMessageRequested.Invoke(message);
        }

        private void WriteLog(string message)
        {
            LogRequested?.Invoke(message);
        }
    }
}
