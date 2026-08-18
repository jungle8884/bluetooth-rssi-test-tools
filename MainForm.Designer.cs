namespace bluetooth_rssi_test_tools
{
    partial class MainForm
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
            this.panel_main = new System.Windows.Forms.Panel();
            this.splitContainer_top = new System.Windows.Forms.SplitContainer();
            this.gb_control = new System.Windows.Forms.GroupBox();
            this.tb_sample = new System.Windows.Forms.TextBox();
            this.tb_high = new System.Windows.Forms.TextBox();
            this.tb_low = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btn_ex_result = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.btn_Start = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tb_device_name = new System.Windows.Forms.TextBox();
            this.splitContainer_down = new System.Windows.Forms.SplitContainer();
            this.dgv_testItems = new System.Windows.Forms.DataGridView();
            this.tb_Text_Result = new System.Windows.Forms.TextBox();
            this.btn_clear_logs = new System.Windows.Forms.Button();
            this.btn_ex_logs = new System.Windows.Forms.Button();
            this.tb_logs = new System.Windows.Forms.TextBox();
            this.panel_main.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_top)).BeginInit();
            this.splitContainer_top.Panel1.SuspendLayout();
            this.splitContainer_top.Panel2.SuspendLayout();
            this.splitContainer_top.SuspendLayout();
            this.gb_control.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_down)).BeginInit();
            this.splitContainer_down.Panel1.SuspendLayout();
            this.splitContainer_down.Panel2.SuspendLayout();
            this.splitContainer_down.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_testItems)).BeginInit();
            this.SuspendLayout();
            // 
            // panel_main
            // 
            this.panel_main.Controls.Add(this.splitContainer_top);
            this.panel_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_main.Location = new System.Drawing.Point(0, 0);
            this.panel_main.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel_main.Name = "panel_main";
            this.panel_main.Size = new System.Drawing.Size(1469, 961);
            this.panel_main.TabIndex = 0;
            // 
            // splitContainer_top
            // 
            this.splitContainer_top.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer_top.Location = new System.Drawing.Point(0, 0);
            this.splitContainer_top.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainer_top.Name = "splitContainer_top";
            this.splitContainer_top.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer_top.Panel1
            // 
            this.splitContainer_top.Panel1.Controls.Add(this.gb_control);
            // 
            // splitContainer_top.Panel2
            // 
            this.splitContainer_top.Panel2.Controls.Add(this.splitContainer_down);
            this.splitContainer_top.Size = new System.Drawing.Size(1469, 961);
            this.splitContainer_top.SplitterDistance = 242;
            this.splitContainer_top.TabIndex = 0;
            // 
            // gb_control
            // 
            this.gb_control.Controls.Add(this.tb_sample);
            this.gb_control.Controls.Add(this.tb_high);
            this.gb_control.Controls.Add(this.tb_low);
            this.gb_control.Controls.Add(this.label3);
            this.gb_control.Controls.Add(this.btn_ex_result);
            this.gb_control.Controls.Add(this.btn_cancel);
            this.gb_control.Controls.Add(this.btn_Start);
            this.gb_control.Controls.Add(this.label2);
            this.gb_control.Controls.Add(this.label1);
            this.gb_control.Controls.Add(this.tb_device_name);
            this.gb_control.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gb_control.Location = new System.Drawing.Point(0, 0);
            this.gb_control.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gb_control.Name = "gb_control";
            this.gb_control.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gb_control.Size = new System.Drawing.Size(1469, 242);
            this.gb_control.TabIndex = 0;
            this.gb_control.TabStop = false;
            this.gb_control.Text = "未开始";
            // 
            // tb_sample
            // 
            this.tb_sample.Location = new System.Drawing.Point(689, 117);
            this.tb_sample.Name = "tb_sample";
            this.tb_sample.Size = new System.Drawing.Size(207, 25);
            this.tb_sample.TabIndex = 10;
            // 
            // tb_high
            // 
            this.tb_high.Location = new System.Drawing.Point(211, 82);
            this.tb_high.Name = "tb_high";
            this.tb_high.Size = new System.Drawing.Size(100, 25);
            this.tb_high.TabIndex = 9;
            // 
            // tb_low
            // 
            this.tb_low.Location = new System.Drawing.Point(211, 149);
            this.tb_low.Name = "tb_low";
            this.tb_low.Size = new System.Drawing.Size(100, 25);
            this.tb_low.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(435, 120);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(187, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "设备采样数据（取平均值）";
            // 
            // btn_ex_result
            // 
            this.btn_ex_result.Location = new System.Drawing.Point(1320, 13);
            this.btn_ex_result.Margin = new System.Windows.Forms.Padding(4);
            this.btn_ex_result.Name = "btn_ex_result";
            this.btn_ex_result.Size = new System.Drawing.Size(128, 49);
            this.btn_ex_result.TabIndex = 5;
            this.btn_ex_result.Text = "导出结果";
            this.btn_ex_result.UseVisualStyleBackColor = true;
            this.btn_ex_result.Click += new System.EventHandler(this.btn_ex_result_Click);
            // 
            // btn_cancel
            // 
            this.btn_cancel.Location = new System.Drawing.Point(1147, 103);
            this.btn_cancel.Margin = new System.Windows.Forms.Padding(4);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(128, 49);
            this.btn_cancel.TabIndex = 4;
            this.btn_cancel.Text = "停止测试";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            // 
            // btn_Start
            // 
            this.btn_Start.Location = new System.Drawing.Point(979, 103);
            this.btn_Start.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(128, 49);
            this.btn_Start.TabIndex = 3;
            this.btn_Start.Text = "启动测试";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(134, 152);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "下限";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(136, 85);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "上限";
            // 
            // tb_device_name
            // 
            this.tb_device_name.Location = new System.Drawing.Point(137, 22);
            this.tb_device_name.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tb_device_name.Name = "tb_device_name";
            this.tb_device_name.Size = new System.Drawing.Size(1145, 25);
            this.tb_device_name.TabIndex = 0;
            this.tb_device_name.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tb_device_name.TextChanged += new System.EventHandler(this.tb_device_name_TextChanged);
            this.tb_device_name.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tb_device_name_KeyDown);
            // 
            // splitContainer_down
            // 
            this.splitContainer_down.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer_down.Location = new System.Drawing.Point(0, 0);
            this.splitContainer_down.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitContainer_down.Name = "splitContainer_down";
            this.splitContainer_down.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer_down.Panel1
            // 
            this.splitContainer_down.Panel1.Controls.Add(this.dgv_testItems);
            // 
            // splitContainer_down.Panel2
            // 
            this.splitContainer_down.Panel2.Controls.Add(this.tb_Text_Result);
            this.splitContainer_down.Panel2.Controls.Add(this.btn_clear_logs);
            this.splitContainer_down.Panel2.Controls.Add(this.btn_ex_logs);
            this.splitContainer_down.Panel2.Controls.Add(this.tb_logs);
            this.splitContainer_down.Size = new System.Drawing.Size(1469, 715);
            this.splitContainer_down.SplitterDistance = 427;
            this.splitContainer_down.TabIndex = 0;
            // 
            // dgv_testItems
            // 
            this.dgv_testItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_testItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_testItems.Location = new System.Drawing.Point(0, 0);
            this.dgv_testItems.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dgv_testItems.Name = "dgv_testItems";
            this.dgv_testItems.RowHeadersWidth = 51;
            this.dgv_testItems.RowTemplate.Height = 27;
            this.dgv_testItems.Size = new System.Drawing.Size(1469, 427);
            this.dgv_testItems.TabIndex = 0;
            // 
            // tb_Text_Result
            // 
            this.tb_Text_Result.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tb_Text_Result.Location = new System.Drawing.Point(1163, 102);
            this.tb_Text_Result.Multiline = true;
            this.tb_Text_Result.Name = "tb_Text_Result";
            this.tb_Text_Result.ReadOnly = true;
            this.tb_Text_Result.Size = new System.Drawing.Size(285, 59);
            this.tb_Text_Result.TabIndex = 3;
            this.tb_Text_Result.Text = "通过次数/总次数";
            this.tb_Text_Result.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btn_clear_logs
            // 
            this.btn_clear_logs.Location = new System.Drawing.Point(1320, 201);
            this.btn_clear_logs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_clear_logs.Name = "btn_clear_logs";
            this.btn_clear_logs.Size = new System.Drawing.Size(137, 44);
            this.btn_clear_logs.TabIndex = 2;
            this.btn_clear_logs.Text = "清除日志";
            this.btn_clear_logs.UseVisualStyleBackColor = true;
            this.btn_clear_logs.Click += new System.EventHandler(this.btn_clear_logs_Click);
            // 
            // btn_ex_logs
            // 
            this.btn_ex_logs.Location = new System.Drawing.Point(1147, 201);
            this.btn_ex_logs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_ex_logs.Name = "btn_ex_logs";
            this.btn_ex_logs.Size = new System.Drawing.Size(137, 44);
            this.btn_ex_logs.TabIndex = 1;
            this.btn_ex_logs.Text = "导出日志";
            this.btn_ex_logs.UseVisualStyleBackColor = true;
            this.btn_ex_logs.Click += new System.EventHandler(this.btn_ex_logs_Click);
            // 
            // tb_logs
            // 
            this.tb_logs.Dock = System.Windows.Forms.DockStyle.Left;
            this.tb_logs.Location = new System.Drawing.Point(0, 0);
            this.tb_logs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tb_logs.Multiline = true;
            this.tb_logs.Name = "tb_logs";
            this.tb_logs.Size = new System.Drawing.Size(1107, 284);
            this.tb_logs.TabIndex = 0;
            this.tb_logs.Text = "日志显示处";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1469, 961);
            this.Controls.Add(this.panel_main);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "蓝牙信号强度测试工具";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panel_main.ResumeLayout(false);
            this.splitContainer_top.Panel1.ResumeLayout(false);
            this.splitContainer_top.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_top)).EndInit();
            this.splitContainer_top.ResumeLayout(false);
            this.gb_control.ResumeLayout(false);
            this.gb_control.PerformLayout();
            this.splitContainer_down.Panel1.ResumeLayout(false);
            this.splitContainer_down.Panel2.ResumeLayout(false);
            this.splitContainer_down.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_down)).EndInit();
            this.splitContainer_down.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_testItems)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel_main;
        private System.Windows.Forms.SplitContainer splitContainer_top;
        private System.Windows.Forms.SplitContainer splitContainer_down;
        private System.Windows.Forms.GroupBox gb_control;
        private System.Windows.Forms.DataGridView dgv_testItems;
        private System.Windows.Forms.TextBox tb_logs;
        private System.Windows.Forms.Button btn_clear_logs;
        private System.Windows.Forms.Button btn_ex_logs;
        private System.Windows.Forms.TextBox tb_device_name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_cancel;
        private System.Windows.Forms.Button btn_Start;
        private System.Windows.Forms.Button btn_ex_result;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tb_low;
        private System.Windows.Forms.TextBox tb_high;
        private System.Windows.Forms.TextBox tb_sample;
        private System.Windows.Forms.TextBox tb_Text_Result;
    }
}

