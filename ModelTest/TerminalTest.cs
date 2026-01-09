using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelTest
{
    public partial class TerminalTest : Form
    {
        public Form OwnerForm { get; set; }
        public TerminalTest()
        {
            InitializeComponent();
            // 订阅关闭事件
            this.FormClosed += (s, e) =>
            {
                // 关闭时显示主窗体
                OwnerForm?.Show();
            };
            //设置背景颜色58957f
            this.BackColor = Color.FromArgb(88, 149, 127);
        }
    }
}

