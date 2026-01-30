using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelTest.CustomControl
{
    public partial class ErrorTestControl : UserControl
    {
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

            ComboBox comboBox1 = new ComboBox
            {
                Location = new Point(0, 30),
                Size = new Size(100, 40),
                Items = { "有功", "无功", "日计时", "启动实验", "启动实验", "潜动实验", "常数实验" },
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
            TextBox tbxBZB = new TextBox
            {
                Location = new Point(330, 0),
                Size = new Size(130, 20)
            };
            TextBox tbxDNB = new TextBox
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
            TextBox quanshu = new TextBox
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
        private void StartErrorClick(object? sender, EventArgs e)
        {
            
        }
    }
}
