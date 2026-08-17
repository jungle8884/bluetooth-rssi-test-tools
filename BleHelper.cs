using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace bluetooth_rssi_test_tools
{
    /// <summary>
    /// BLE 扫描封装（WinRT BluetoothLEAdvertisementWatcher，Win10+ 可用）
    /// </summary>
    public static class BleHelper
    {
        public sealed class BleDeviceInfo
        {
            public string Name;
            public string Mac;
            public int Rssi;
        }

        // ulong 地址 转换为 "AA:BB:CC:DD:EE:FF" (注意字节序, 不Reverse会倒序)
        private static string MacFromAddress(ulong addr)
        {
            return BitConverter.ToString(BitConverter.GetBytes(addr).Take(6).Reverse().ToArray());
        }

        // 去掉冒号/横线/空格 并统一大小写, 让 "aabbccddeeff" 和 "AA:BB:CC:DD:EE:FF" 能匹配上
        private static string Normalize (string s)
        {
            var sb = new StringBuilder();
            foreach (char c in (s ?? ""))
            {
                if (c != ':' && c != '-' && c != ' ') sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        // 去掉最大最小值后取均值, 抗RSSI瞬时尖脉冲
        private static double TrimMean(List<int> values)
        {
            var s = values.OrderBy(x => x).ToList();
            return s.Skip(1).Take(s.Count - 2).Average();
        }

        public static async Task<double?> SampleRssiAsync(
            string target, int sampleCount, int timeoutSeconds, 
            Action<int> onSample, CancellationToken ct)
        {
            var samples = new List<int>();
            var done = new TaskCompletionSource<bool>();
            BluetoothLEAdvertisementWatcher watcher = null;

            try
            {
                watcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active // Active 才能拿到广播名
                };

                watcher.Received += (s, args) =>
                {
                    string name = args.Advertisement.LocalName ?? "";
                    string mac = MacFromAddress(args.BluetoothAddress);
                    // 宽松匹配: 按名称或按MAC(去冒号横线后比较)
                    bool hit = string.Equals(name, target, StringComparison.OrdinalIgnoreCase)
                        || Normalize(mac) == Normalize(target);
                    if (!hit) return;

                    samples.Add(args.RawSignalStrengthInDBm);
                    if (onSample != null) onSample(args.RawSignalStrengthInDBm);
                    if (samples.Count >= sampleCount) done.TrySetResult(true);
                };

                watcher.Start();

                // 竞速: "采够了" 和 "超时" 谁先到谁赢 --- 产线绝不等一个不存在的设备
                Task finished = await Task.WhenAny(done.Task, Task.Delay(timeoutSeconds * 1000, ct));
                if (finished != done.Task || samples.Count == 0) return null;

                return TrimMean(samples);
            }
            finally
            {
                if (watcher != null) watcher.Stop(); // 每设备用完即停, 下个设备重开
            }
        }
    }
}
