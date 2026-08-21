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

        /// <summary>
        /// 在 x86/x64 电脑上（小端序 Little-Endian），`GetBytes` 返回的数组是**低字节在前**：
        ///     假设 MAC 地址是 `1A:2B:3C:4D:5E:6F`
        ///     内存布局（小端序，低字节在前）：ulong addr 的值 = 0x00001A2B3C4D5E6F（高16位为0，低48位是MAC）
        ///     小端序 GetBytes 后：
        ///     byte[0] = 0x6F   ← MAC最后一个字节
        ///     byte[1] = 0x5E
        ///     byte[2] = 0x4D
        ///     byte[3] = 0x3C
        ///     byte[4] = 0x2B
        ///     byte[5] = 0x1A   ← MAC第一个字节
        ///     byte[6] = 0x00
        ///     byte[7] = 0x00
        ///     Take(6) 后：
        ///         [0x6F, 0x5E, 0x4D, 0x3C, 0x2B, 0x1A]
        ///     Reverse() 后：
        ///         [0x1A, 0x2B, 0x3C, 0x4D, 0x5E, 0x6F]  ✓ 正确的MAC顺序！
        /// </summary>
        /// <param name="addr">args.BluetoothAddress	ulong（64 位整数）	0x00001A2B3C4D5E6F</param>
        /// <returns>将字节数组转换成**十六进制字符串**，默认用**连字符 `-`** 分隔每个字节</returns>
        private static string MacFromAddress(ulong addr)
        {
            /*
                将字节数组转换成**十六进制字符串**，默认用**连字符 `-`** 分隔每个字节。
                输入 `[0x1A, 0x2B, 0x3C, 0x4D, 0x5E, 0x6F]`
                输出 `"1A-2B-3C-4D-5E-6F"`
             */
            return BitConverter.ToString(
                            BitConverter.GetBytes(addr)   // ① ulong → byte[8]
                                .Take(6)                   // ② 取前6个字节
                                .Reverse()                 // ③ 反转字节顺序
                                .ToArray()                 // ④ 转回数组
            );
        }

        /// <summary>
        /// 去掉冒号/横线/空格 并统一大小写, 让 "aabbccddeeff" 和 "AA:BB:CC:DD:EE:FF" 能匹配上
        /// 注意: SN 里的 '-' 也会被去掉, 所以 "G7N-A4CSDV00136" 归一化成 "G7NA4CSDV00136"
        /// </summary>
        /// <param name="s">设备号字符串</param>
        /// <returns>归一化设备号</returns>
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
 
        /// <summary>
        /// SN 匹配: 归一化后支持三种情况
        /// 1) 完全相等 (条码 SN == 广播 SN)
        /// 2) 条码是完整 SN, 广播只放尾部 15 字节 (a.EndsWith(b))
        /// 3) 条码只印了尾部几位, 广播更完整 (b.EndsWith(a))
        /// </summary>
        /// <param name="targetNorm"></param>
        /// <param name="advSn"></param>
        /// <returns></returns>
        private static bool SnMatches(string targetNorm, string advSn)
        {
            return SnMatchesNorm(targetNorm, Normalize(advSn));
        }

        /// <summary>SN 匹配(输入均已归一化, 供批量分发复用)</summary>
        private static bool SnMatchesNorm(string targetNorm, string snNorm)
        {
            if (targetNorm.Length == 0 || snNorm.Length == 0) return false;
            return targetNorm == snNorm || targetNorm.EndsWith(snNorm) || snNorm.EndsWith(targetNorm);
        }

        /// <summary>
        /// 匹配规则: 设备名 == target 或 MAC == target 或 广播SN匹配 target
        /// </summary>
        /// <param name="args">广播扫描到的参数</param>
        /// <param name="target">设备名称</param>
        /// <param name="targetNorm">归一化设备名称</param>
        /// <returns></returns>
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

        /// <summary>
        ///  去掉最大最小值后取均值, 抗RSSI瞬时尖脉冲
        ///  样本不足 3 个时退化为普通平均, 避免 Take(-1)/空集合 Average 抛异常
        /// </summary>
        /// <param name="values">RSSI</param>
        /// <returns>RSSI平均值</returns>
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
        /// onTrace 是采样级详细追踪(锁定MAC/逐包RSSI/结束统计), 只写后台日志, 不刷界面。
        /// </summary>
        public static async Task<List<int>> SampleRssiAsync(
            string target, int sampleCount, int timeoutSeconds,
            CancellationToken ct, Action<int> onSample = null, Action<string> onTrace = null)
        {
            void Trace(string msg) => onTrace?.Invoke(string.Format("[{0}] {1}", target, msg));
            var samples = new List<int>();
            /**
                创建了一个"未完成的 Task"，通过 done.Task 可以拿到它
                给了你三个"遥控器"来结束它：
                        SetResult(值)	宣布成功完成，done.Task 的结果就是这个值
                        SetException(ex)	宣布失败，await 它会抛出这个异常
                        SetCanceled()	宣布取消，await 它会抛 TaskCanceledException
                <bool> 只是说这个 Task 完成时携带一个 bool 类型的结果。
                这里其实不关心结果值，用 bool 只是随便选个类型占位（也有人用 TaskCompletionSource<object> 或新版的无泛型 TaskCompletionSource）。
                执行流程是这样的：[这个模式叫把"事件/回调"桥接成"async/await"，官方名称是 TAP 模式的手动实现，非常常用。]
                        主流程创建一个"还没完成"的 Task，然后 await 它（配合 WhenAny）
                        主流程挂起，不阻塞线程
                        蓝牙后台线程每收到一个广播包就触发回调，某次回调发现样本采够了，调用 done.TrySetResult(true)
                        这一瞬间，第 2 步里被等待的 Task 变成"已完成"，await 恢复，主流程继续往下走
                        可以把它想象成取餐叫号：先拿到一个号码牌（Task），你先去干别的（await 挂起），后厨做好菜（回调触发）按铃（SetResult），你听到铃声去取餐。
                代码里用的是 TrySetResult(true) 而不是 SetResult(true)：
                        SetResult：如果这个 Task 已经被完成过（比如两个回调几乎同时都发现采够了），第二次调用会抛异常
                        TrySetResult：如果已完成，就安静地返回 false，不抛异常
                        由于 Received 回调可能在多个线程并发触发，用 TrySetResult 保证"只有第一个到达的线程真正完成它，其余的无害忽略"，这也是这段代码线程安全设计的一部分。
             */
            var done = new TaskCompletionSource<bool>(); // TaskCompletionSource-① 先造一个"占位任务"
            string targetNorm = Normalize(target);
            /*
                        WinRT 提供的 BLE 广播监听器，能被动接收周围所有 BLE 设备的广播包。
                        ScanningMode = Active 表示主动扫描模式：
                            除了监听广播包，还会请求并接收设备的 Scan Response（补充数据包），
                            拿到的信息更全，代价是更耗电。对应的 Passive 模式只收广播包、不主动发请求。
                        周围任何 BLE 设备发广播都会触发这个事件，所以第一步先用 Matches 过滤，只认目标设备的包（匹配规则见方法注释）。
                        lock (samples) 是关键：Received 事件在后台线程触发，高频广播下可能并发进入，加锁保证样本计数和添加不会错乱。
                        逻辑是：还没采够就添加一个样本（RawSignalStrengthInDBm 就是 RSSI，单位 dBm，一般是 -30 ~ -100 之间的负值，越接近 0 信号越强），
                        并通知上层；采够了就用 done.TrySetResult(true) 发完成信号。
                        用 TrySetResult 而不是 SetResult，是因为它幂等且线程安全——多次触发也不会抛异常。
             */
            BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

            // MAC 锁: 第一次匹配成功后锁定该设备的蓝牙地址, 后续只收它发的包。
            // 防的场景: 产线上多台待测设备同时广播, 若两台广播了相同 SN(如固件未烧录用默认值),
            // 不锁定的话两台的 RSSI 会混进同一次采样, 均值失真。
            ulong lockedMac = 0; // 0 = 尚未锁定

            watcher.Received += (s, args) =>
            {
                if (!Matches(args, target, targetNorm)) return;
                lock (samples)
                {
                    if (lockedMac == 0)
                    {
                        lockedMac = args.BluetoothAddress; // 首个匹配的设备 → 锁定
                        Trace("匹配锁定 MAC " + MacFromAddress(lockedMac) +
                            " (广播名: " + (args.Advertisement.LocalName ?? "(空)") + ")");
                    }
                    if (args.BluetoothAddress != lockedMac) return;       // 其他设备(即使SN相同) → 丢弃
                    if (samples.Count < sampleCount)
                    {
                        samples.Add(args.RawSignalStrengthInDBm);
                        Trace(string.Format("采样 RSSI={0}dBm ({1}/{2})",
                            args.RawSignalStrengthInDBm, samples.Count, sampleCount));
                        if (onSample != null) onSample(args.RawSignalStrengthInDBm);
                    }
                    if (samples.Count >= sampleCount)
                        done.TrySetResult(true); // TaskCompletionSource-③ 后台回调采够了 → 宣布完成
                }
            };

            watcher.Start();
            try
            {
                try
                {
                    // 若 ct 已提前取消, Task.Delay 会同步抛 TaskCanceledException, 这里吞掉按超时处理
                    // Task.WhenAny 同时等两件事：采够样本（done.Task）或超时（Task.Delay），谁先到就结束等待。这是"限时等待"的标准写法
                    await Task.WhenAny(done.Task, Task.Delay(timeoutSeconds * 1000, ct)); // TaskCompletionSource-② 主流程在这里等它
                }
                catch (TaskCanceledException) 
                {
                    // 内层负责吞取消异常
                }
            }
            finally
            {
                if (watcher != null) watcher.Stop(); // 每设备用完即停, 下个设备重开
            }
            Trace(string.Format("采样结束: 采到 {0}/{1} 个样本" + (samples.Count == 0 ? " (未发现设备或超时)" : ""),
                samples.Count, sampleCount));
            return samples;
        }

        #region 并行批量采样: 单 watcher + 分发表

        /// <summary>
        /// 批量并行采样的内部状态: 每个目标设备一份, 各自持有样本/MAC锁/完成信号,
        /// 互不干扰 → 多台设备同时采样, 相同 SN 也不会串台(MAC锁按目标独立生效)。
        /// </summary>
        private sealed class TargetState
        {
            public string Target;                                    // 原始输入(SN/名称/MAC)
            public string TargetNorm;                                // 归一化输入
            public readonly List<int> Samples = new List<int>();     // 已采样本
            public ulong LockedMac;                                  // 0 = 尚未锁定
            public readonly TaskCompletionSource<bool> Done = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// 并行批量采样(单 watcher + 分发): 一个 watcher 收所有广播包, 每包只解析一次,
        /// 按 SN/名称/MAC 路由到各目标的采样队列 → 所有设备同时采样, 总耗时 ≈ 最慢那台,
        /// 而不是旧版串行的"各台之和"。实测基准: 设备广播约 1.1 包/秒, 采 10 个样本约 9 秒,
        /// 无论几台设备, 整批都只要 ~10 秒。
        ///
        /// 为什么比"每台一个 watcher"好:
        ///   1. 没有 watcher 反复 Start/Stop 的平台启动延迟(旧版偶发"未发现"的元凶)
        ///   2. 每个包只做一次厂商数据解析, 开销随设备数线性而不是平方级增长
        ///   3. 首包等待时间和 Python(bleak 持续监听) 同级, 不会再出现等 1 分多钟的情况
        /// </summary>
        /// <param name="targets">目标列表(SN/设备名/MAC 任意混合, 自动去重)</param>
        /// <param name="sampleCount">每台采样个数</param>
        /// <param name="timeoutSeconds">整批超时(秒), 到点没采够的设备按实际样本数返回</param>
        /// <param name="ct">取消令牌</param>
        /// <param name="onSample">每采到一包回调(target, rssi), watcher 后台线程触发</param>
        /// <param name="onTrace">采样级追踪日志, watcher 后台线程触发</param>
        /// <param name="onTargetDone">某台采够样本立即回调(target, 样本快照), 不必等整批结束</param>
        /// <returns>target → 样本列表(顺序与 targets 一致, 0 个样本=未发现)</returns>
        public static async Task<Dictionary<string, List<int>>> SampleRssiBatchAsync(
            List<string> targets, int sampleCount, int timeoutSeconds,
            CancellationToken ct, Action<string, int> onSample = null,
            Action<string> onTrace = null, Action<string, List<int>> onTargetDone = null)
        {
            var results = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            if (targets == null) return results;

            // 去重 + 建分发表(每台设备独立状态)
            var states = targets
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(t => new TargetState { Target = t, TargetNorm = Normalize(t) })
                .ToList();
            if (states.Count == 0) return results;

            var gate = new object(); // 分发表总锁(与单台版 lock(samples) 同思路)

            BluetoothLEAdvertisementWatcher watcher =
                new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

            watcher.Received += (s, args) =>
            {
                // ★ 每个广播包只解析一次, 所有目标共享 —— 这是批量版的核心优化:
                //   N 台设备不再是每台各自解析一遍厂商数据, 而是每包 1 次
                string name = args.Advertisement.LocalName;
                string nameNorm = string.IsNullOrEmpty(name) ? null : Normalize(name);
                string mac = MacFromAddress(args.BluetoothAddress);
                string macNorm = Normalize(mac);
                string snNorm = null;
                if (TryParseYiyunSn(args, out string sn, out _)) snNorm = Normalize(sn);

                foreach (TargetState st in states)
                {
                    // 匹配规则与单台版完全一致: 名称/MAC/SN 三选一
                    bool match =
                        (nameNorm != null && (name == st.Target || nameNorm == st.TargetNorm)) ||
                        mac == st.Target || macNorm == st.TargetNorm ||
                        (snNorm != null && SnMatchesNorm(st.TargetNorm, snNorm));
                    if (!match) continue;

                    lock (gate)
                    {
                        if (st.LockedMac == 0)
                        {
                            st.LockedMac = args.BluetoothAddress;
                            onTrace?.Invoke(string.Format("[{0}] 匹配锁定 MAC {1} (广播名: {2})",
                                st.Target, mac, name ?? "(空)"));
                        }
                        if (args.BluetoothAddress != st.LockedMac) continue; // 同SN其他设备 → 丢弃

                        if (st.Samples.Count < sampleCount)
                        {
                            st.Samples.Add(args.RawSignalStrengthInDBm);
                            onTrace?.Invoke(string.Format("[{0}] 采样 RSSI={1}dBm ({2}/{3})",
                                st.Target, args.RawSignalStrengthInDBm, st.Samples.Count, sampleCount));
                            onSample?.Invoke(st.Target, args.RawSignalStrengthInDBm);
                        }
                        if (st.Samples.Count >= sampleCount && !st.Done.Task.IsCompleted)
                        {
                            st.Done.TrySetResult(true);
                            onTrace?.Invoke(string.Format("[{0}] 采样结束: 采到 {1}/{2}",
                                st.Target, st.Samples.Count, sampleCount));
                            // 传快照, 避免上层遍历时后台线程还在改列表
                            onTargetDone?.Invoke(st.Target, st.Samples.ToList());
                        }
                    }
                }
            };

            watcher.Start();
            try
            {
                // 整批完成 = 所有设备各自采够; 有任何一台没采够就等整批超时/取消
                Task allDone = Task.WhenAll(states.Select(x => x.Done.Task).ToArray());
                try
                {
                    await Task.WhenAny(allDone, Task.Delay(timeoutSeconds * 1000, ct));
                }
                catch (TaskCanceledException) { } // 取消按超时处理: 已采多少算多少
            }
            finally
            {
                watcher.Stop(); // 整批只用一个 watcher, 没有反复启停
            }

            foreach (TargetState st in states)
            {
                results[st.Target] = st.Samples;
                if (st.Samples.Count == 0)
                    onTrace?.Invoke(string.Format("[{0}] 采样结束: 0/{1} (未发现设备或超时)",
                        st.Target, sampleCount));
            }
            return results;
        }

        #endregion
    }
}
