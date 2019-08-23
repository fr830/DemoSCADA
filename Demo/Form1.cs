using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DemoDriver;
using DataService;
using Log;

namespace Demo
{
    public partial class MainFrm : Form
    {
        public MainFrm()
        {
            InitializeComponent();
        }

        DAService da;

        private void Btn_Test_Click(object sender, EventArgs e)
        {
           da = new DAService();
        }

        private void Btn_Send_Click(object sender, EventArgs e)
        {
            byte[] temp = {65,66};
            da.SendBytes(temp);
        }

        private void Btn_TestTag_Click(object sender, EventArgs e)
        {
            //浮点数 是把后收到的字节 作为高位，其他都是把 先收到的字节 作为高位
            MessageBox.Show(da["test1"].Value.Byte.ToString());
            MessageBox.Show(da["test2"].Value.Boolean.ToString());
            MessageBox.Show(da["test3"].Value.Int16.ToString());
            MessageBox.Show(da["test4"].Value.Single.ToString());
            MessageBox.Show(da["test5"].Value.Int32.ToString());

        }

        private void Btn_CreatFile_Click(object sender, EventArgs e)
        {
            Log.LogBytes.WriteBytesToFile(new byte[] { 0x66, 0x67,0x1,0x2,0x3},5);
        }
    }
}
