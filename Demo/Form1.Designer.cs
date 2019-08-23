namespace Demo
{
    partial class MainFrm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_Test = new System.Windows.Forms.Button();
            this.btn_Send = new System.Windows.Forms.Button();
            this.btn_TestTag = new System.Windows.Forms.Button();
            this.btn_CreatFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Test
            // 
            this.btn_Test.Location = new System.Drawing.Point(9, 12);
            this.btn_Test.Name = "btn_Test";
            this.btn_Test.Size = new System.Drawing.Size(75, 23);
            this.btn_Test.TabIndex = 0;
            this.btn_Test.Text = "测试驱动";
            this.btn_Test.UseVisualStyleBackColor = true;
            this.btn_Test.Click += new System.EventHandler(this.Btn_Test_Click);
            // 
            // btn_Send
            // 
            this.btn_Send.Location = new System.Drawing.Point(107, 12);
            this.btn_Send.Name = "btn_Send";
            this.btn_Send.Size = new System.Drawing.Size(75, 23);
            this.btn_Send.TabIndex = 1;
            this.btn_Send.Text = "发送数据";
            this.btn_Send.UseVisualStyleBackColor = true;
            this.btn_Send.Click += new System.EventHandler(this.Btn_Send_Click);
            // 
            // btn_TestTag
            // 
            this.btn_TestTag.Location = new System.Drawing.Point(205, 12);
            this.btn_TestTag.Name = "btn_TestTag";
            this.btn_TestTag.Size = new System.Drawing.Size(75, 23);
            this.btn_TestTag.TabIndex = 2;
            this.btn_TestTag.Text = "测试ITag";
            this.btn_TestTag.UseVisualStyleBackColor = true;
            this.btn_TestTag.Click += new System.EventHandler(this.Btn_TestTag_Click);
            // 
            // btn_CreatFile
            // 
            this.btn_CreatFile.Location = new System.Drawing.Point(303, 12);
            this.btn_CreatFile.Name = "btn_CreatFile";
            this.btn_CreatFile.Size = new System.Drawing.Size(75, 23);
            this.btn_CreatFile.TabIndex = 3;
            this.btn_CreatFile.Text = "文件测试";
            this.btn_CreatFile.UseVisualStyleBackColor = true;
            this.btn_CreatFile.Click += new System.EventHandler(this.Btn_CreatFile_Click);
            // 
            // MainFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btn_CreatFile);
            this.Controls.Add(this.btn_TestTag);
            this.Controls.Add(this.btn_Send);
            this.Controls.Add(this.btn_Test);
            this.Name = "MainFrm";
            this.Text = "Demo";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Test;
        private System.Windows.Forms.Button btn_Send;
        private System.Windows.Forms.Button btn_TestTag;
        private System.Windows.Forms.Button btn_CreatFile;
    }
}

