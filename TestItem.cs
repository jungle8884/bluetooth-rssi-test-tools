using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bluetooth_rssi_test_tools
{
    /// <summary>
    /// 测试项数据模型。
    /// 实现 INotifyPropertyChanged: 属性被赋值时主动通知绑定引擎,
    /// DataGridView 上对应的单元格会立即刷新(增删行由 BindingList 负责,
    /// 行内属性变化必须靠本接口, 否则代码里改值表格不更新)。
    /// </summary>
    public class TestItem : INotifyPropertyChanged
    {
        private bool _testCheck;
        private string _device;
        private string _rssi;
        private string _avg;
        private string _result;

        // 是否测试
        public bool TestCheck
        {
            get { return _testCheck; }
            set { _testCheck = value; OnPropertyChanged("TestCheck"); }
        }

        // 设备名称
        public string Device
        {
            get { return _device; }
            set { _device = value; OnPropertyChanged("Device"); }
        }

        // 信号强度
        public string RSSI
        {
            get { return _rssi; }
            set { _rssi = value; OnPropertyChanged("RSSI"); }
        }

        // 采样平均值
        public string AVG
        {
            get { return _avg; }
            set { _avg = value; OnPropertyChanged("AVG"); }
        }

        // 测试结果
        public string Result
        {
            get { return _result; }
            set { _result = value; OnPropertyChanged("Result"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
