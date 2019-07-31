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
using System.Data.SqlClient;
using System.IO;


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
            AddTag();
        }

        private void AddTag()
        {
            TagData tag = new TagData((short)(list.Count == 0 ? 1 : list.Max(x => x.ID) + 1), 0, "", "", 1, 1, true, false, false, false, null, "", 0, 0, 0);
            bindSourceProtocol.Add(tag);
            int index = list.BinarySearch(tag);
            if (index < 0) index = ~index;
            list.Insert(index, tag);
            dGVAccess.FirstDisplayedScrollingRowIndex = bindSourceProtocol.Count - 1;
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
            List<TagData> data = new List<TagData>();
            list.Clear();
            string sql = "SELECT Id,GroupID,TagName,Address,DataType,DataSize,IsActive,Archive,DefaultValue,Description,Maximum,Minimum,Cycle from Protocol where DataType<12";

            using (var reader = DataHelper.Instance.ExecuteReader(sql))
            {                
                while (reader.Read())
                {
                    TagData tag = new TagData(reader.GetInt16(0), reader.GetInt16(1), reader.GetString(2), reader.GetString(3), reader.GetByte(4),
                     (ushort)reader.GetInt16(5), reader.GetBoolean(6), false, false, reader.GetBoolean(7),
                     reader.GetValue(8), reader.GetNullableString(9), reader.GetFloat(10), reader.GetFloat(11), reader.GetInt32(12));
                    list.Add(tag);
                    data.Add(tag);
                }
            }

            list.Sort();
            bindSourceProtocol.DataSource = new SortableBindingList<TagData>(data);
            tagCount.Text += list.Count.ToString();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            /*********为了让最后编辑的单元格数据可以保存到数据库中*****/
            toolStripCollect.Focus();
            dGVAccess.CurrentCell = null;
            /*********************************************************/

            if (Save())
            {
                MessageBox.Show("保存成功!");
            }
            else
            {
                MessageBox.Show("保存失败!");
            }
        }

        private bool Save()
        {
            bool result = true;

            TagDataReader reader = new TagDataReader(list);

            result &= DataHelper.Instance.BulkCopy(reader, "Protocol", "DELETE FROM Protocol", SqlBulkCopyOptions.KeepIdentity);

            return result;
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            TagData tag = bindSourceProtocol.Current as TagData;
            bindSourceProtocol.Remove(tag);
            list.Remove(tag);
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("将清除所有的标签，是否确定？", "警告", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bindSourceProtocol.Clear();
                list.Clear();
            }
        }

        private void BtnQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (MessageBox.Show("退出之前是否需要保存？", "警告", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Save();
            }
        }

        private void LoadFromCsv()
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                List<TagData> dataList = new List<TagData>();
                string data = Clipboard.GetText(TextDataFormat.Text);
                if (string.IsNullOrEmpty(data)) return;
                list.Clear();
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                {
                    using (var mysr = new StreamReader(stream))
                    {
                        string strline = "";
                        while ((strline = mysr.ReadLine()) != null)
                        {
                            string[] aryline = strline.Split('\t');
                            try
                            {
                                var id = Convert.ToInt16(aryline[0]);
                                var groupid = Convert.ToInt16(aryline[1]);
                                var name = aryline[2];
                                var address = aryline[3];
                                var type = Convert.ToByte(aryline[4]);
                                var size = Convert.ToUInt16(aryline[5]);
                                var active = Convert.ToBoolean(aryline[6]);
                                var desp = aryline[7];
                                TagData tag = new TagData(id, groupid, name, address, type, size, active, false, false, false, null, desp, 0, 0, 0);
                                list.Add(tag);
                                dataList.Add(tag);
                            }
                            catch (Exception err)
                            {
                                Log4Net.AddLog("LoadFromCsv() " + err.Message, InfoLevel.FATAL);
                                continue;
                            }
                        }
                    }
                }
                list.Sort();

                bindSourceProtocol.DataSource = new SortableBindingList<TagData>(dataList);
                tagCount.Text += dataList.Count.ToString();
            }
        }

        private void BtnPasteTags_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "xml文件 (*.xml)|*.xml|csv文件 (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string file = saveFileDialog1.FileName;
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                        SaveToCsv(file);
                        break;
                    case 2:
                        SaveToCsv(file);
                        break;
                }
            }
        }

        private void SaveToCsv(string file)
        {
            using (StreamWriter objWriter = new StreamWriter(file, false))
            {
                foreach (TagData tag in list)
                {
                    objWriter.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8}", tag.ID, tag.GroupID, tag.Name, tag.Address, tag.DataType, tag.Size, tag.Active, tag.DefaultValue, tag.Description);
                }
            }
        }

        private void DGVAccess_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            SolidBrush b = new SolidBrush(this.dGVAccess.RowHeadersDefaultCellStyle.ForeColor);
            e.Graphics.DrawString((e.RowIndex + 1).ToString(System.Globalization.CultureInfo.CurrentUICulture), this.dGVAccess.DefaultCellStyle.Font, b, e.RowBounds.Location.X + 20, e.RowBounds.Location.Y + 4);
        }

        private void ContextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "粘贴CSV":
                    LoadFromCsv();
                    break;
            }
        }
    }
}
