using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelTest.CustomControl
{
    public partial class ErrorTestControl : UserControl
    {
        private ComboBox comboBox1;
        private TextBox tbxBZB;
        private TextBox tbxDNB;
        private TextBox quanshu;
        public ErrorTestControl()
        {
            InitializeComponent();
            ChangeDataSelect();
        }

        private void ChangeDataSelect()
        {
            Panel controlPanl = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 80,
            };
            Label label1 = new Label
            {
                Location = new Point(0, 0),
                Text = "实验类型",
            };

            comboBox1 = new ComboBox
            {
                Location = new Point(0, 30),
                Size = new Size(100, 40),
                Items = { "有功", "无功", "日计时" },
                SelectedIndex = 0,
            };
            Label label2 = new Label
            {
                Location = new Point(120, 0),
                Text = "实验方式",
            };
            ComboBox comboBox2 = new ComboBox
            {
                Location = new Point(120, 30),
                Size = new Size(100, 40),
                Items = { "电脉冲", "光脉冲", "蓝牙脉冲" },
                SelectedIndex = 0,
            };
            Label label3 = new Label
            {
                Location = new Point(220, 0),
                Text = "标准表常数",
            };
            Label label4 = new Label
            {
                Location = new Point(220, 30),
                Text = "电能表常数",
            };
            tbxBZB = new TextBox
            {
                Location = new Point(330, 0),
                Size = new Size(130, 20)
            };
            tbxDNB = new TextBox
            {
                Location = new Point(330, 30),
                Size = new Size(130, 20)
            };
            Label label5 = new Label
            {
                Location = new Point(460, 0),
                Text = "圈数",
                Size = new Size(60, 20)
            };
            quanshu = new TextBox
            {
                Location = new Point(520, 0),
                Size = new Size(60, 20)
            };
            Button StartError = new Button
            {
                Text = "启动误差",
                Location = new Point(580, 0),
                Size = new Size(120, 30),
                BackColor = Color.Green,
                ForeColor = Color.White,
            };
            StartError.Click += StartErrorClick;
            Button StoptError = new Button
            {
                Text = "停止误差",
                Location = new Point(700, 0),
                Size = new Size(120, 30),
                BackColor = Color.Red,
                ForeColor = Color.White,
            };
            StoptError.Click += StoptError_Click;
            Button GetError = new Button
            {
                Text = "获取误差",
                Location = new Point(460, 30),
                Size = new Size(120, 30),
                BackColor = Color.Blue,
                ForeColor = Color.White,
            };
            GetError.Click += GetError_Click;
            controlPanl.Controls.AddRange(new Control[] {
                label1,comboBox1, tbxBZB,tbxDNB,label2,
           comboBox2,label3,label4,label5,quanshu,StartError,StoptError,GetError});
            this.Controls.Add(controlPanl);
        }
        /// <summary>
        /// 获取误差结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void GetError_Click(object? sender, EventArgs e)
        {

        }

        /// <summary>
        /// 停止误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void StoptError_Click(object? sender, EventArgs e)
        {

        }

        /// <summary>
        /// 启动误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private double BZBYG;
        private double DNBYG;
        private double BZBWG;
        private double DNBWG;
        private double RJSHZ;
        private void StartErrorClick(object? sender, EventArgs e)
        {
            constantSet();//常数频率相关从参数获取

        }
        /// <summary>
        /// 常数设置
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void constantSet()
        {
            //55 07 00 01 00 32 字节1 ，字节2 AA
            //设置标准表常数,分有功无功 01 标准表有功脉冲常数 02标准表无功脉冲常数
            bool BZBConstant = double.TryParse(tbxBZB.Text,out double _bzbc);//标准表常数 字节2
            bool DNBBConstant = double.TryParse(tbxDNB.Text,out double _dnbc);//电能表常数 字节2
            bool  RJSHZConstant = double.TryParse(quanshu.Text,out double _rjsc);//日计时圈数
                                         //设置电能表常数，分有功无功 03 电能表有功脉冲常数 04电能表有功脉冲常数
            ModelMain modelMain = new();
            //日计时实验需要设置频率
            switch (comboBox1.Text)
            {
                case "有功":
                    BZBYG = 01;
                    DNBYG = 03;
                    modelMain.SetErrorConstant(BZBYG, _bzbc, DNBYG, _dnbc, 1);
                    break;
                case "无功":
                    BZBWG = 02;
                    DNBWG = 04;
                    modelMain.SetErrorConstant(BZBWG, _bzbc, DNBWG, _dnbc, 2);
                    break;
                case "日计时":
                    RJSHZ = 05;
                    modelMain.SetErrorConstant(RJSHZ, _rjsc, 0, 0, 3);
                    break;
                default:
                    break;
            }
        }
    }
}
