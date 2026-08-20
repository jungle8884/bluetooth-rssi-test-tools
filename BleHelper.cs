using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace bluetooth_rssi_test_tools
{
    /// <summary>
    /// BLE 扫描封装（WinRT BluetoothLEAdvertisementWatcher，Win10+ 可用）
    /// </summary>
    public static class BleHelper
    {
        // 艺云 AI 充电宝: 厂商数据 CompanyID (蓝牙 SIG 保留给厂商私用)
        private const ushort YiyunCompanyId = 0xFFFF;

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
        // 注意: SN 里的 '-' 也会被去掉, 所以 "G7N-A4CSDV00136" 归一化成 "G7NA4CSDV00136"
        private static string Normalize (string s)
        {
            var sb = new StringBuilder();
            foreach (char c in (s ?? ""))
            {
                if (c != ':' && c != '-' && c != ' ') sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解析艺云 AI 充电宝广播包 SN。
        /// 协议(实测确认, 2026-08-20): AD Type 0xFF, CompanyID = 0xFFFF,
        /// WinRT 的 ManufacturerData.Data 不含 CompanyID 那 2 字节,
        /// 负载结构 = [Type 1B][SN 15B], 共 16 字节, SN 为纯 ASCII。
        /// Type: 0x00=配对模式, 0x01=回连模式。
        /// 例: 47 37 4E 2D 41 34 43 53 44 56 30 30 31 33 36 → "G7N-A4CSDV00136"
        /// </summary>
        /// <param name="args">Watcher.Received 事件参数</param>
        /// <param name="sn">输出的广播 SN(失败为 null)</param>
        /// <param name="broadcastType">输出的广播类型字节(失败为 0)</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParseYiyunSn(BluetoothLEAdvertisementReceivedEventArgs args,
            out string sn, out byte broadcastType)
        {
            sn = null;
            broadcastType = 0;
            if (args == null) return false;

            // 1. 取厂商数据段 (Data 不含 CompanyID 2 字节)
            // ManufacturerData 属性是 Win10 初版就有, 兼容性最好
            var mf = args.Advertisement.ManufacturerData
                .FirstOrDefault(x => x.CompanyId == YiyunCompanyId);
            if (mf == null) return false;

            // 2. IBuffer -> byte[]
            byte[] data;
            using (var reader = DataReader.FromBuffer(mf.Data))
            {
                data = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(data);
            }

            // 3. 长度校验: Type(1) + SN(15) = 16
            if (data.Length < 16) return false;

            // 4. 解析
            broadcastType = data[0];
            byte[] snBytes = data.Skip(1).Take(15).ToArray();
            sn = Encoding.ASCII.GetString(snBytes); // ASCII 不会抛, 但保险起见不走 try
            return true;
        }

        // SN 匹配: 归一化后支持三种情况
        //   1) 完全相等 (条码 SN == 广播 SN)
        //   2) 条码是完整 SN, 广播只放尾部 15 字节 (a.EndsWith(b))
        //   3) 条码只印了尾部几位, 广播更完整 (b.EndsWith(a))
        private static bool SnMatches(string targetNorm, string advSn)
        {
            string b = Normalize(advSn);
            if (targetNorm.Length == 0 || b.Length == 0) return false;
            return targetNorm == b || targetNorm.EndsWith(b) || b.EndsWith(targetNorm);
        }

        // 匹配规则: 设备名 == target 或 MAC == target 或 广播SN匹配 target
        private static bool Matches(BluetoothLEAdvertisementReceivedEventArgs args,
            string target, string targetNorm)
        {
            if (target.Length == 0) return false;
            string name = args.Advertisement.LocalName;
            string mac = MacFromAddress(args.BluetoothAddress);
            if (!string.IsNullOrEmpty(name) && (name == target || Normalize(name) == targetNorm)) return true;
            if (!string.IsNullOrEmpty(mac) && (mac == target || Normalize(mac) == targetNorm)) return true;
            return TryParseYiyunSn(args, out string advSn, out _) && SnMatches(targetNorm, advSn);
        }

        // 去掉最大最小值后取均值, 抗RSSI瞬时尖脉冲
        // 样本不足 3 个时退化为普通平均, 避免 Take(-1)/空集合 Average 抛异常
        public static double TrimMean(List<int> values)
        {
            if (values == null || values.Count == 0) return double.NaN;
            if (values.Count <= 2) return values.Average();
            var s = values.OrderBy(x => x).ToList();
            return s.Skip(1).Take(s.Count - 2).Average();
        }

        /// <summary>
        /// 采集目标设备 sampleCount 个 RSSI 样本（去抖/抗尖脉冲在上层做 trim_mean）。
        /// 匹配规则: 设备名 == target 或 MAC == target 或 艺云广播SN匹配 target(忽略大小写/冒号/横线)。
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
                if (!Matches(args, target, targetNorm)) return;
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
                try
                {
                    // 若 ct 已提前取消, Task.Delay 会同步抛 TaskCanceledException, 这里吞掉按超时处理
                    await Task.WhenAny(done.Task, Task.Delay(timeoutSeconds * 1000, ct));
                }
                catch (TaskCanceledException) { }
            }
            finally
            {
                if (watcher != null) watcher.Stop(); // 每设备用完即停, 下个设备重开
            }
            return samples;
        }
    }
}
