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
using DataService;

namespace ProtocolConfig
{
    public partial class MainForm : Form
    {
        List<TagData> list = new List<TagData>();

        public static readonly List<DataTypeSource> DataDict = new List<DataTypeSource>
        {
           new DataTypeSource (1,"开关型"),new DataTypeSource (3,"字节"), new DataTypeSource (4,"短整型"),
           new DataTypeSource (5,"单字型"),new DataTypeSource (6,"双字型"),new DataTypeSource (7,"长整型"),
           new DataTypeSource (8,"浮点型"),new DataTypeSource (9,"系统型"),new DataTypeSource (10,"ASCII字符串"),
           new DataTypeSource(0,"")
        };

        public MainForm()
        {
            InitializeComponent();

            DataGridViewComboBoxColumn col = dGVAccess.Columns["Column3"] as DataGridViewComboBoxColumn;
            col.DataSource = DataDict;
            col.DisplayMember = "Name";
            col.ValueMember = "DataType";
        }



        private void BtnAdd_Click(object sender, EventArgs e)
        {
            Storage va = new Storage();
            va.Single = 3.2f;
            List<HistoryData> _hda = new List<HistoryData>() {
                new HistoryData(1,QUALITIES.QUALITY_GOOD, va,DateTime.Now),
                new HistoryData(1, QUALITIES.QUALITY_GOOD, va, DateTime.Now),
                new HistoryData(1, QUALITIES.QUALITY_GOOD, va, DateTime.Now) };

            DataHelper.Instance.BulkCopy(new HDAAccessReader(_hda), "Log");
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadFromDatabase();
        }

        private void LoadFromDatabase()
        {
            list.Clear();
            string sql = "SELECT ID,GroupID,TagName,Address,DataType,DataSize,IsActive,IsArchive,DefaultValue,Description,Maximum,Minimum,Cycle from Protocol where DataType<12";

            using (var reader = DataHelper.Instance.ExecuteReader(sql))
            {

                reader.Read();

                short i = reader.GetInt16(0);

                //while (reader.Read())
                //{
                //    TagData tag = new TagData(reader.GetInt16(0), reader.GetInt16(1), reader.GetString(2), reader.GetString(3), reader.GetByte(4),
                //        (ushort)reader.GetInt16(5), reader.GetBoolean(6),  false, false, reader.GetBoolean(7),
                //        reader.GetValue(8), reader.GetNullableString(9), reader.GetFloat(10), reader.GetFloat(11), reader.GetInt32(12));
                //    list.Add(tag);
                //}
            }
            list.Sort();
            bindSourceProtocol.DataSource = new SortableBindingList<TagData>(list);
            tagCount.Text = list.Count.ToString();
        }
    }
}
