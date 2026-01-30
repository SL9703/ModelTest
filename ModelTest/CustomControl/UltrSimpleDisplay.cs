using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelTest.CustomControl
{
    public partial class UltrSimpleDisplay : UserControl
    {
        private double _displaydouble { get; set; }
        private int digitCount = 5;//显示位数
        private int digitSpcing = 10;//数码管间距
        private Color onColor = Color.Green;//点亮颜色
        private Color offColor = Color.FromArgb(40, 20, 20);//熄灭颜色
        public UltrSimpleDisplay()
        {
            InitializeComponent();
            this.Paint += simpleDisplay;//绘制
        }
        /// <summary>
        /// 数码管绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void simpleDisplay(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);
            //格式化显示值
            string dispalyStr = FormatForDispaly(_displaydouble);
            //计算数码管尺寸和位置
            int digitwidth = 50;
            int digitheight = 80;
            int totalwidth = dispalyStr.Length * (digitwidth + digitSpcing) - digitSpcing;
            int startX = (this.ClientSize.Width - digitwidth) / 2 - 300;
            int startY = (this.ClientSize.Height - digitwidth) / 2 -10;

            //绘制每个数码管
            for (int i = 0; i < dispalyStr.Length; i++)
            {
                char c = dispalyStr[i];
                int x = startX + i * (digitwidth + digitSpcing);

                DrawDigit(g, c, x, startY, digitwidth, digitheight);
                //显示原始值和相关信息
                using (Font infoFont = new Font("Arial", 10))
                {
                    g.DrawString($"显示值:{_displaydouble}", infoFont, Brushes.White, 10, 10);
                    g.DrawString($"显示内容:{dispalyStr}", infoFont, Brushes.White, 250, 10);
                    g.DrawString($"模拟显示", infoFont, Brushes.Gray, 500, 10);
                }
            }
        }

        private void DrawDigit(Graphics g, char c, int x, int y, int width, int height)
        {
            // 7段数码管段定义（按常见顺序：A-G）
            bool[] segments = GetSegmentsForChar(c);

            // 段的位置和尺寸
            int segmentThickness = width / 8;
            int horizontalLength = width - 2 * segmentThickness;
            int verticalLength = (height - 3 * segmentThickness) / 2;

            // 计算各段的坐标
            Rectangle[] segmentRects = new Rectangle[7];

            // 段A（上横）
            segmentRects[0] = new Rectangle(x + segmentThickness, y, horizontalLength, segmentThickness);

            // 段B（右上竖）
            segmentRects[1] = new Rectangle(x + width - segmentThickness, y + segmentThickness, segmentThickness, verticalLength);

            // 段C（右下竖）
            segmentRects[2] = new Rectangle(x + width - segmentThickness, y + segmentThickness + verticalLength + segmentThickness, segmentThickness, verticalLength);

            // 段D（下横）
            segmentRects[3] = new Rectangle(x + segmentThickness, y + height - segmentThickness, horizontalLength, segmentThickness);

            // 段E（左下竖）
            segmentRects[4] = new Rectangle(x, y + segmentThickness + verticalLength + segmentThickness, segmentThickness, verticalLength);

            // 段F（左上竖）
            segmentRects[5] = new Rectangle(x, y + segmentThickness, segmentThickness, verticalLength);

            // 段G（中横）
            segmentRects[6] = new Rectangle(x + segmentThickness, y + segmentThickness + verticalLength, horizontalLength, segmentThickness);

            // 绘制所有段
            for (int i = 0; i < 7; i++)
            {
                DrawSegment(g, segmentRects[i], segments[i]);
            }

            // 绘制小数点
            if (c == '.')
            {
                Rectangle dpRect = new Rectangle(
                    x + width,
                    y + height - segmentThickness * 2,
                    segmentThickness,
                    segmentThickness);
                DrawSegment(g, dpRect, true);
            }

            // 绘制数码管外框
            g.DrawRectangle(Pens.Gray, x, y, width, height);

            // 在底部显示字符
            using (Font charFont = new Font("Arial", 12))
            {
                g.DrawString(c.ToString(), charFont, Brushes.White, x + width / 2 - 5, y + height + 5);
            }
        }

        public string FormatForDispaly(double value)
        {
            //
            string str = value.ToString("F4");//固定4位小数
            if (str.Length > digitCount)
            {
                str = value.ToString("E4");//如果太长，使用科学计数法
            }
            return str;
        }
        private void DrawSegment(Graphics g, Rectangle rect, bool isOn)
        {
            Color fillColor = isOn ? onColor : offColor;
            Color glowColor = isOn ? Color.FromArgb(100, onColor) : Color.Transparent;

            // 绘制发光效果
            if (isOn)
            {
                using (SolidBrush glowBrush = new SolidBrush(glowColor))
                {
                    Rectangle glowRect = new Rectangle(
                        rect.X - 2, rect.Y - 2,
                        rect.Width + 4, rect.Height + 4);
                    g.FillRectangle(glowBrush, glowRect);
                }
            }

            // 绘制段主体
            using (SolidBrush brush = new SolidBrush(fillColor))
            {
                g.FillRectangle(brush, rect);
            }

            // 绘制段边框
            g.DrawRectangle(Pens.DarkGray, rect);

            // 如果是点亮状态，添加高光效果
            if (isOn)
            {
                using (Pen highlightPen = new Pen(Color.FromArgb(150, Color.White), 1))
                {
                    g.DrawLine(highlightPen, rect.Left, rect.Top, rect.Right, rect.Top);
                    g.DrawLine(highlightPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                }
            }
        }
        private bool[] GetSegmentsForChar(char c)
        {
            bool[] segments = new bool[7]; // A, B, C, D, E, F, G

            switch (c)
            {
                case '0':
                    segments = new bool[] { true, true, true, true, true, true, false };
                    break;
                case '1':
                    segments = new bool[] { false, true, true, false, false, false, false };
                    break;
                case '2':
                    segments = new bool[] { true, true, false, true, true, false, true };
                    break;
                case '3':
                    segments = new bool[] { true, true, true, true, false, false, true };
                    break;
                case '4':
                    segments = new bool[] { false, true, true, false, false, true, true };
                    break;
                case '5':
                    segments = new bool[] { true, false, true, true, false, true, true };
                    break;
                case '6':
                    segments = new bool[] { true, false, true, true, true, true, true };
                    break;
                case '7':
                    segments = new bool[] { true, true, true, false, false, false, false };
                    break;
                case '8':
                    segments = new bool[] { true, true, true, true, true, true, true };
                    break;
                case '9':
                    segments = new bool[] { true, true, true, true, false, true, true };
                    break;
                case '-':
                    segments = new bool[] { false, false, false, false, false, false, true };
                    break;
                case '.':
                    // 小数点特殊处理，只显示小数点
                    segments = new bool[] { false, false, false, false, false, false, false };
                    break;
                case 'E':
                case 'e':
                    segments = new bool[] { true, false, false, true, true, true, true };
                    break;
                default:
                    // 默认全灭
                    segments = new bool[] { false, false, false, false, false, false, false };
                    break;
            }

            return segments;
        }
    }
}
