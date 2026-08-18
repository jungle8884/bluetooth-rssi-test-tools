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

        private static bool Matches(string name, string mac, string target, string targetNorm)
        {
            if (target.Length == 0) return false;
            if (!string.IsNullOrEmpty(name) && (name == target || Normalize(name) == targetNorm)) return true;
            if (!string.IsNullOrEmpty(mac) && (mac == target || Normalize(mac) == targetNorm)) return true;
            return false;
        }

        // 去掉最大最小值后取均值, 抗RSSI瞬时尖脉冲
        public static double TrimMean(List<int> values)
        {
            var s = values.OrderBy(x => x).ToList();
            return s.Skip(1).Take(s.Count - 2).Average();
        }

        /// <summary>
        /// 采集目标设备 sampleCount 个 RSSI 样本（去抖/抗尖脉冲在上层做 trim_mean）。
        /// 匹配规则: 设备名 == target 或 MAC == target（忽略冒号横线）。
        /// onSample 在 watcher 后台线程回调，UI 更新请自行 Invoke。
        /// </summary>
        public static async Task<List<int>> SampleRssiAsync(
            string target, int sampleCount, int timeoutSeconds, 
            CancellationToken ct, Action<int> onSample = null)
        {
            var samples = new List<int>();
            var done = new TaskCompletionSource<bool>();
            string targetNorm = Normalize(target);
            BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

            watcher.Received += (s, args) =>
            {
                string name = args.Advertisement.LocalName;
                string mac = MacFromAddress(args.BluetoothAddress);
                if (!Matches(name, mac, target, targetNorm)) return;
                lock (samples)
                {
                    if (samples.Count < sampleCount)
                    {
                        samples.Add(args.RawSignalStrengthInDBm);
                        if (onSample != null) onSample(args.RawSignalStrengthInDBm);
                    }
                    if (samples.Count >= sampleCount) done.TrySetResult(true);
                }
            };

            watcher.Start();
            try
            {
                await Task.WhenAny(done.Task, Task.Delay(timeoutSeconds * 1000, ct));
            }
            finally
            {
                if (watcher != null) watcher.Stop(); // 每设备用完即停, 下个设备重开
            }
            return samples;
        }
    }
}
