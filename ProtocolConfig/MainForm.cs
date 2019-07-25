using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DatabaseLib;
using Log;
namespace ProtocolConfig
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {

        }

        private void ConnectTest_Click(object sender, EventArgs e)
        {
            if (DataHelper.Instance.ConnectionTest())
            {
                MessageBox.Show("数据库连接成功！");
                Log4Net.AddLog("数据库连接成功！", InfoLevel.INFO);
            }
            else
            {
                MessageBox.Show("数据库连接失败！");
                Log4Net.AddLog("数据库连接失败！",InfoLevel.FATAL);
            }
        }
    }
}
