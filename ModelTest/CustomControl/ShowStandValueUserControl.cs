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
    public partial class ShowStandValueUserControl : UserControl
    {
        public ShowStandValueUserControl()
        {
            InitializeComponent();
        }
        private string _UA;
        private string _UB;
        private string _UC;

        private string _IA;
        private string _IB;
        private string _IC;

        private string _PA;
        private string _PB;
        private string _PC;

        private string _QA;
        private string _QB;
        private string _QC;

        private string _SA;
        private string _SB;
        private string _SC;

        private string _Pfa;
        private string _Pfb;
        private string _pfc;

        private string _Xa;
        private string _Xb;
        private string _Xc;

        private string _Freq;//频率

        private string _Alarm;//报警

        private string _Contans;//常数

        private string _Uab;
        private string _Uca;

        private string _XX;

        private string _VLC;
        private string _ALC;


        public string UA { get => _UA; set { _UA = value; tb_UA.Text = value; } }
        public string UB { get => _UB; set { _UB = value; tb_UB.Text = value; } }
        public string UC { get => _UC; set { _UC = value; tb_UC.Text = value; } }
        public string IA { get => _IA; set { _IA = value; tb_IA.Text = value; } }
        public string IB { get => _IB; set { _IB = value; tb_IB.Text = value; } }
        public string IC { get => _IC; set { _IC = value; tb_IC.Text = value; } }
        public string PA { get => _PA; set { _PA = value; tb_PA.Text = value; } }
        public string PB { get => _PB; set { _PB = value; tb_PB.Text = value; } }
        public string PC { get => _PC; set { _PC = value; tb_PC.Text = value; } }
        public string QA { get => _QA; set { _QA = value; tb_QA.Text = value; } }   
        public string QB { get => _QB; set { _QB = value; tb_QB.Text = value; } }
        public string QC { get => _QC; set { _QC = value; tb_QC.Text = value; } }
        public string SA { get => _SA; set { _SA = value; tb_SA.Text = value; } }   
        public string SC { get => _SC; set { _SC = value; tb_SC.Text = value; } }
        public string SB { get => _SB; set { _SB = value; tb_SB.Text = value; } }
        public string Pfa { get => _Pfa; set { _Pfa = value; tb_PFA.Text = value; } }
        public string Pfb { get => _Pfb; set { _Pfb = value; tb_PFB.Text = value; } }
        public string Pfc { get => _pfc; set { _pfc = value; tb_PFC.Text = value; } }
        public string Xa { get => _Xa; set { _Xa = value; tb_XA.Text = value; } }
        public string Xb { get => _Xb; set { _Xb = value; tb_XB.Text = value; } }
        public string Xc { get => _Xc; set { _Xc = value; tb_XC.Text = value; } }
        public string Freq { get => _Freq; set { _Freq = value; tb_HZ.Text = value; } }
        public string Alarm { get => _Alarm; set { _Alarm = value; tb_Alarm.Text = value; } }
        public string Contans { get => _Contans; set { _Contans = value; tb_contans.Text = value; } }
        public string Uab { get => _Uab; set { _Uab = value; tb_Uba.Text = value; } }
        public string Uca { get => _Uca; set { _Uca = value; tb_Uca.Text = value; } }

        public string XX { get => _XX; set { _XX = value; tb_xx.Text = value; } }
        public string VLC { get => _VLC; set { _VLC = value; tb_V_LC.Text = value; } }
        public string ALC { get => _ALC; set { _ALC = value; tb_A_LC.Text = value; } }
    }
}
