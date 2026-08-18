using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace bluetooth_rssi_test_tools
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer _debounceTimer; // 扫码防抖
        private CancellationTokenSource _cts; // 测试取消令牌
        private bool _suppressBoundChange = false;
        private BindingList<TestItem> _items;
        private BindingSource _itemsSource;
        public MainForm()
        {
            InitializeComponent();
            InitDgvTestItems();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitUi();
        }

        private void InitUi()
        {
            tb_high.Text = "-10";
            tb_low.Text = "-90";
            tb_sample.Text = "10";

            _items = new BindingList<TestItem>();
            _itemsSource = new BindingSource { DataSource = _items };
            // 关键绑定: BindingList → BindingSource → DataGridView
            // 没有这一行, _items.Add() 后表格永远空白
            dgv_testItems.DataSource = _itemsSource;

            // 回车立即添加 + 300ms 无输入自动添加(兼容不带回车的扫码枪)
            _debounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                AddDeviceFromInput();
            };

            // 注册阈值联动验证
            tb_low.TextChanged += TbBound_TextChanged;
            tb_high.TextChanged += TbBound_TextChanged;
        }

        // 统一处理 low/high 的文本变化并保持下限 <= 上限
        private async void TbBound_TextChanged(object sender, EventArgs e) 
        {
            if (_suppressBoundChange) return;
            _suppressBoundChange = true;
            try 
            {
                bool lowOK = TryParseRssi(tb_low.Text, out int low);
                bool highOK = TryParseRssi(tb_high.Text, out int high);

                if (!lowOK || !highOK) 
                {
                    btn_Start.Enabled = false;
                    Log("上下限输入不合法");
                    return;
                }

                var edited = sender as TextBox;
                if (low > high)
                {
                    Log("上下限输入不满足规则：[下限 <= 上限]，请重新输入!");
                    if (edited == tb_low) 
                    {
                        tb_low.Clear();
                        await Task.Delay(3000);
                    } else
                    {
                        tb_high.Clear();
                        await Task.Delay(3000);
                    }
                }
            }
            finally { _suppressBoundChange = false; }
        }

        // 安全解析 RSSI 文本为 int（支持负数），返回是否成功
        private bool TryParseRssi(string s, out int value)
        {
            s = s?.Trim() ?? string.Empty;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private void InitDgvTestItems()
        {
            dgv_testItems.AutoGenerateColumns = false;
            dgv_testItems.AllowUserToAddRows = false;
            dgv_testItems.ReadOnly = false;
            dgv_testItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv_testItems.RowHeadersVisible = false;
            dgv_testItems.BackgroundColor = System.Drawing.Color.White;

            var columns = new List<(string Header, string DataProp, int Width, DataGridViewContentAlignment Align)>
            {
                ("是否测试", nameof(TestItem.TestCheck), 200, DataGridViewContentAlignment.MiddleCenter),
                ("设备名称", nameof(TestItem.Device), 200, DataGridViewContentAlignment.MiddleCenter),
                ("信号强度", nameof(TestItem.RSSI), 200, DataGridViewContentAlignment.MiddleCenter),
                ("平均值", nameof(TestItem.AVG), 200, DataGridViewContentAlignment.MiddleCenter),
                ("测试结果", nameof(TestItem.Result), 200, DataGridViewContentAlignment.MiddleCenter)
            };

            foreach (var col in columns)
            {
                var dgvc = new DataGridViewTextBoxColumn
                {
                    HeaderText = col.Header,
                    DataPropertyName = col.DataProp,
                    Width = col.Width,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = col.Align}
                };

                // 特殊处理: 把bool类型的TestCheck显示为复选框样式
                if (col.DataProp == nameof(TestItem.TestCheck))
                {
                    var chkCol = new DataGridViewCheckBoxColumn 
                    { 
                        HeaderText = col.Header,
                        DataPropertyName =col.DataProp,
                        Width =col.Width,
                        DefaultCellStyle = new DataGridViewCellStyle 
                        {
                            Alignment = DataGridViewContentAlignment.MiddleCenter
                        },
                        ReadOnly = false // 允许用户点击
                    };
                    chkCol.TrueValue = true;
                    chkCol.FalseValue = false;
                    dgv_testItems.Columns.Add(chkCol);
                    continue;
                }

                dgv_testItems.Columns.Add(dgvc);
            }
        }

        private async void btn_Start_Click(object sender, EventArgs e)
        {
            // 参数校验
            if (!TryParseRssi(tb_low.Text, out int low)) { Log("下限不合法"); return; }
            if (!TryParseRssi(tb_high.Text, out int high)) { Log("上限不合法"); return; }
            if (low > high) { Log("下限不能大于上限"); return; }
            if (!int.TryParse(tb_sample.Text, out int sampleCount) || sampleCount < 3 || sampleCount > 50) 
            {
                Log("样本数须在 3~50 之间");
                return;
            }

            // 快照勾选设备: 防止测试中途用户勾选/取消行, 导致遍历的集合被修改
            List<TestItem> targets = _items.Where(x => x.TestCheck).ToList();
            if (targets.Count == 0) { Log("没有勾选任何设备"); return; }

            // 进入测试状态: 禁按钮+建取消令牌
            _cts = new CancellationTokenSource();
            btn_Start.Enabled = false;

            try
            {
                int done = 0;
                foreach (TestItem item in targets) // 串行测试
                {
                    if (_cts.IsCancellationRequested) { Log("测试已取消"); break; }
                    done++;
                    Text = string.Format("测试中 ({0}/{1}:{2})", done, targets.Count, item.Device);
                    Log("开始测试: " + item.Device);

                    item.RSSI = string.Empty;
                    item.AVG = string.Empty;
                    item.Result = "测试中";

                    List<int> samples = await BleHelper.SampleRssiAsync(
                        item.Device, sampleCount, 12, _cts.Token,
                        rssi => BeginInvoke(new Action(() => { item.RSSI = rssi.ToString(); })));

                    if (_cts.IsCancellationRequested) break;
                    if (samples.Count == 0)
                    {
                        item.Result = "未发现设备";
                        Log($"未发现 [{item.Device}] 设备");
                        continue;
                    }

                    double avg = BleHelper.TrimMean(samples);
                    bool ok = avg >= low && avg <= high;
                    item.AVG = avg.ToString("F1");
                    item.Result = ok ? "PASS" : "FAIL";
                    Log(string.Format("[{0}] {1} → {2}",
                        item.Device,
                        item.AVG == string.Empty ? "-" : item.AVG,
                        item.Result));

                    // 将结果统一为大写并根据值设置颜色
                    var resultText = (item.Result ?? string.Empty).ToUpperInvariant();
                    tb_Text_Result.Text = resultText;

                    if (resultText == "PASS")
                    {
                        tb_Text_Result.ForeColor = Color.Green;
                    }
                    else if (resultText == "FAIL")
                    {
                        tb_Text_Result.ForeColor = Color.Red;
                    }
                    else
                    {
                        tb_Text_Result.ForeColor = SystemColors.WindowText; // 恢复默认颜色
                    }
                }
                Log("==== 全部测试完成 ====");
            }
            catch (Exception ex)
            {
                Log("测试异常: " + ex.Message);
            }
            finally 
            {
                if (_cts != null) 
                { 
                    _cts.Dispose(); 
                    _cts = null;
                }
                btn_Start.Enabled = true;
                Text = "BLE RSSI 检测结束";
            }

        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            if (_cts != null) _cts.Cancel();
            btn_Start.Enabled = true;
            Log("操作人员主动取消测试");
        }

        // 字段转义: 含逗号/引号/换行时加引号包裹, 内部引号翻倍
        private string CsvCell(object value)
        {
            string s = value == null ? string.Empty : value.ToString();
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\r") || s.Contains("\n"))
            {
                s = "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        private void btn_ex_result_Click(object sender, EventArgs e)
        {
            if (_items == null || _items.Count == 0) 
            { 
                Log("[提示] 无内容可保存"); 
                return; 
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*";
                dlg.FileName = string.Format("BLE检测结果_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                dlg.Title = "保存测试结果";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("是否测试,设备名称,信号强度,平均值,测试结果");
                foreach (TestItem item in _items)
                {
                    sb.AppendLine(string.Join(",",
                        item.TestCheck ? "是" : "否",
                        CsvCell(item.Device),
                        CsvCell(item.RSSI),
                        CsvCell(item.AVG),
                        CsvCell(item.Result)));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), new  UTF8Encoding(true));
                Log("[保存] " + dlg.FileName);
            }
        }

        private void btn_ex_logs_Click(object sender, EventArgs e)
        {
            // 导出日志到文本
            if (string.IsNullOrEmpty(tb_logs.Text)) { Log("日志为空, 无需导出"); return; }
            using (var dlg = new SaveFileDialog()) 
            {
                dlg.Filter = "文本文件(*.txt)|*.txt";
                dlg.FileName = string.Format("BLE检测日志_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                File.WriteAllText(dlg.FileName, tb_logs.Text, new UTF8Encoding(true));
                Log("[导出日志] " + dlg.FileName);
            }
        }

        private void btn_clear_logs_Click(object sender, EventArgs e)
        {
            tb_logs.Clear();
        }

        // 录入设备名称开始启动timer
        private void tb_device_name_TextChanged(object sender, EventArgs e)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        // 录入完成停止timer
        private void tb_device_name_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // 屏蔽 "叮" 声
                _debounceTimer.Stop();
                AddDeviceFromInput();
            }
        }

        private void AddDeviceFromInput()
        {
            string text = tb_device_name.Text.Trim();
            if (text.Length == 0) return;

            // 列表查重: 同一设备只允许一行(连续或非连续重复都挡住)
            foreach (TestItem it in _items)
            {
                if (string.Equals(it.Device, text, StringComparison.OrdinalIgnoreCase))
                {
                    Log("[跳过] 设备已存在: " + text);
                    tb_device_name.Clear();
                    return;
                }
            }

            var item = new TestItem
            {
                TestCheck = true,
                Device = text,
                RSSI = string.Empty,
                AVG = string.Empty,
                Result = string.Empty
            };
            _items.Add(item);   // BindingList 自动通知表格新增一行

            tb_device_name.Clear();
            Log("[添加]" + text);
        }

        /// <summary>
        /// 将一条日志消息追加到日志文本框(tb_logs), 自动带时间戳; 线程安全。
        /// </summary>
        private void Log(string msg)
        {
            if (IsDisposed || tb_logs == null) return;
            if (tb_logs.InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(msg)));
                return;
            }
            tb_logs.AppendText(string.Format("[{0:HH:mm:ss}] {1}\r\n", DateTime.Now, msg));

            // 日志上限保护: 超过 maxChars 时只保留尾部 keepChars, 防止无限膨胀
            const int maxChars = 200_000;
            const int keepChars = 100_000;
            if (tb_logs.TextLength > maxChars)
            {
                tb_logs.Text = tb_logs.Text.Substring(tb_logs.TextLength - keepChars);
                tb_logs.SelectionStart = tb_logs.TextLength;
                tb_logs.ScrollToCaret();
            }
        }
        
    }
}
