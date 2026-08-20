using System;
using System.IO;
using System.Text;

namespace bluetooth_rssi_test_tools
{
    /// <summary>
    /// 后台文件日志: 自动写入 exe 目录下 Logs\, 按"天"分文件, 线程安全。
    /// 文件名: BLE检测日志_20260820.log (跨天自动切换新文件, 时间精确到毫秒)。
    /// 任何写入异常都被吞掉——日志失败绝不能影响测试主流程。
    /// </summary>
    public static class FileLogger
    {
        private static readonly object _lock = new object();
        private static string _currentDate = string.Empty;
        private static string _currentFile = string.Empty;

        /// <summary>
        /// 追加一条日志(自动带时间戳)。可在任意线程调用。
        /// </summary>
        public static void Write(string msg)
        {
            try
            {
                lock (_lock)
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    // 按天切换文件: 产线连续运行跨天后自动开新文件, 单文件不会无限膨胀
                    string today = DateTime.Now.ToString("yyyyMMdd");
                    if (today != _currentDate)
                    {
                        _currentDate = today;
                        _currentFile = Path.Combine(dir,
                            string.Format("BLE检测日志_{0}.log", today));
                        File.AppendAllText(_currentFile,
                            string.Format("\r\n==== {0:yyyy-MM-dd dddd} 开始记录 ====\r\n", DateTime.Now),
                            new UTF8Encoding(true));
                    }

                    File.AppendAllText(_currentFile,
                        string.Format("[{0:HH:mm:ss.fff}] {1}\r\n", DateTime.Now, msg),
                        new UTF8Encoding(true));
                }
            }
            catch
            {
                // 磁盘满/文件被占用等: 静默放弃, 不影响测试
            }
        }
    }
}
