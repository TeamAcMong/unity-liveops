// MÃ DEV TẠM (G-UX-JOURNEY) — driver hành trình người dùng cho LiveOps Hub. Không thuộc package, không commit vào nhánh feature.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UxJourney
{
    public sealed class WaitFrames
    {
        public readonly int Count;
        public WaitFrames(int count) { Count = count; }
    }

    public sealed class WaitSeconds
    {
        public readonly double Seconds;
        public WaitSeconds(double seconds) { Seconds = seconds; }
    }

    public sealed class WaitUntilOrTimeout
    {
        public readonly Func<bool> Condition;
        public readonly double TimeoutSeconds;
        public WaitUntilOrTimeout(Func<bool> condition, double timeoutSeconds) { Condition = condition; TimeoutSeconds = timeoutSeconds; }
    }

    public sealed class UxOptions
    {
        public string OutputDirectory;
        public string Label;
        public List<string> Journeys = new List<string>();
        public bool ExitWhenDone = true;
        public int RestoreFrontPid;
        public string ToolsDirectory = "/Users/datho/.cache/unity-liveops/hub-p1-plan/plan/uiux/journey/_tools";
    }

    /// <summary>Điểm vào -executeMethod UxJourney.UxJourneyDriver.Run.</summary>
    public static class UxJourneyDriver
    {
        internal static UxRunner Active;

        public static void Run()
        {
            UxOptions options = new UxOptions();
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (args[index] == "-uxOut") options.OutputDirectory = args[index + 1];
                if (args[index] == "-uxLabel") options.Label = args[index + 1];
                if (args[index] == "-uxJourneys")
                {
                    foreach (string part in args[index + 1].Split(','))
                    {
                        if (part.Trim().Length > 0) options.Journeys.Add(part.Trim());
                    }
                }
                if (args[index] == "-uxNoExit" && args[index + 1] == "1") options.ExitWhenDone = false;
                if (args[index] == "-uxRestoreFrontPid") int.TryParse(args[index + 1], out options.RestoreFrontPid);
            }
            if (string.IsNullOrEmpty(options.OutputDirectory) || options.Journeys.Count == 0)
            {
                Debug.LogError("UXJ: thiếu -uxOut hoặc -uxJourneys");
                EditorApplication.Exit(2);
                return;
            }
            if (string.IsNullOrEmpty(options.Label)) options.Label = Application.unityVersion.StartsWith("2022") ? "2022.3" : "6000.6";
            Directory.CreateDirectory(options.OutputDirectory);
            Active = new UxRunner(options);
            // Chờ vài giây cho Editor ổn định sau khi mở project (layout, import) rồi mới chạy.
            EditorApplication.delayCall += () => Active.Start();
        }
    }

    /// <summary>Tắt App Nap cho chính tiến trình Unity này (NSProcessInfo beginActivity) để Editor chạy tiếp khi bị che/không focus.</summary>
    public static class UxNative
    {
        private const string ObjC = "/usr/lib/libobjc.A.dylib";
        [System.Runtime.InteropServices.DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
        [System.Runtime.InteropServices.DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
        [System.Runtime.InteropServices.DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr selector);
        [System.Runtime.InteropServices.DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr SendString(IntPtr receiver, IntPtr selector, string value);
        [System.Runtime.InteropServices.DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr SendActivity(IntPtr receiver, IntPtr selector, ulong options, IntPtr reason);
        private static IntPtr _token;

        public static string DisableAppNap()
        {
            try
            {
                IntPtr processInfo = Send(objc_getClass("NSProcessInfo"), sel_registerName("processInfo"));
                IntPtr reason = SendString(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), "UxJourney driver");
                // NSActivityUserInitiated (0x00FFFFFF) | NSActivityLatencyCritical (0xFF00000000)
                _token = SendActivity(processInfo, sel_registerName("beginActivityWithOptions:reason:"), 0x00FFFFFFUL | 0xFF00000000UL, reason);
                if (_token != IntPtr.Zero) Send(_token, sel_registerName("retain"));
                return _token != IntPtr.Zero ? "ok" : "token null";
            }
            catch (Exception exception)
            {
                return "lỗi " + exception.Message;
            }
        }
    }

    public sealed class UxRunner
    {
        private sealed class Frame
        {
            public IEnumerator Enumerator;
            public string JourneyId;
        }

        public readonly UxOptions Options;
        private readonly List<Frame> _stack = new List<Frame>();
        private int _waitFrames;
        private double _waitUntilTime;
        private WaitUntilOrTimeout _waitCondition;
        private double _waitConditionStart;
        private bool _inPump;
        private StreamWriter _log;
        private readonly object _logLock = new object();
        private readonly ConcurrentQueue<string> _consoleQueue = new ConcurrentQueue<string>();
        private readonly List<string> _consoleSinceMark = new List<string>();
        public long TickCount;
        private Thread _watchdog;
        private volatile bool _finished;
        public readonly List<string> Failures = new List<string>();
        public string CurrentJourney = "";
        public string CurrentStep = "";
        public readonly int ProcessId = Process.GetCurrentProcess().Id;

        /// <summary>Việc chạy trong vòng update lồng (vd vòng modal của ShowModalUtility) — khi luồng chính đang kẹt trong SendEvent.</summary>
        public Action NestedTick;

        public UxRunner(UxOptions options)
        {
            Options = options;
        }

        public void Start()
        {
            _log = new StreamWriter(Path.Combine(Options.OutputDirectory, "journey.log"), true, new UTF8Encoding(false)) { AutoFlush = true };
            Log("=== bắt đầu, Unity " + Application.unityVersion + ", pid " + ProcessId + ", journeys " + string.Join(",", Options.Journeys));
            Application.logMessageReceivedThreaded += OnLog;
            Log("tắt App Nap cho tiến trình: " + UxNative.DisableAppNap());
            if (Options.RestoreFrontPid > 0)
            {
                Log("trả focus cho pid " + Options.RestoreFrontPid + ": " + Helper("activate " + Options.RestoreFrontPid).Trim());
            }
            _stack.Add(new Frame { Enumerator = RunAll(), JourneyId = null });
            EditorApplication.update += Tick;
            _watchdog = new Thread(WatchdogLoop) { IsBackground = true, Name = "UxJourneyWatchdog" };
            _watchdog.Start();
        }

        public void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [" + CurrentJourney + "/" + CurrentStep + "] " + message;
            lock (_logLock)
            {
                if (_log != null) _log.WriteLine(line);
            }
            Debug.Log("UXJ: " + line);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition != null && condition.StartsWith("UXJ: ", StringComparison.Ordinal)) return;
            if (type == LogType.Log) return;
            string first = stackTrace ?? string.Empty;
            int newline = first.IndexOf('\n');
            if (newline >= 0) first = first.Substring(0, newline);
            _consoleQueue.Enqueue(type + ": " + condition + (first.Length > 0 ? " @ " + first : string.Empty));
        }

        /// <summary>Lấy các dòng Console (Warning/Error/Exception) từ lần gọi trước.</summary>
        public List<string> DrainConsole()
        {
            List<string> result = new List<string>();
            while (_consoleQueue.TryDequeue(out string line)) result.Add(line);
            return result;
        }

        private IEnumerator RunAll()
        {
            yield return new WaitSeconds(3);
            foreach (string journey in Options.Journeys)
            {
                Func<UxRunner, IEnumerator> factory = UxJourneys.Find(journey);
                if (factory == null)
                {
                    Log("journey lạ: " + journey);
                    Failures.Add(journey + ": không có");
                    continue;
                }
                CurrentJourney = journey;
                CurrentStep = "start";
                Log(">>> journey " + journey);
                DrainConsole();
                _stack.Add(new Frame { Enumerator = factory(this), JourneyId = journey });
                yield return null; // frame journey chạy trên đỉnh stack tới khi xong
                UxJourneys.CleanupAfterJourney(this);
                yield return new WaitFrames(5);
            }
            Finish();
        }

        private double _nextHeartbeat;
        private long _heartbeatTicks;
        public int StolenFromPid;
        /// <summary>Kế hoạch phím cho hộp modal sắp mở (ShowModalUtility chặn update): watchdog thực hiện khi luồng chính kẹt.</summary>
        public readonly ConcurrentQueue<string[]> ModalPlans = new ConcurrentQueue<string[]>();
        public readonly ConcurrentQueue<string> ModalStems = new ConcurrentQueue<string>();

        public void PlanModal(string stepId, params string[] actions)
        {
            ModalStems.Enqueue(stepId);
            ModalPlans.Enqueue(actions);
        }
        public int FocusNeeded;

        private void Tick()
        {
            Interlocked.Increment(ref TickCount);
            if (_finished) return;
            double now = EditorApplication.timeSinceStartup;
            if (now >= _nextHeartbeat)
            {
                if (_nextHeartbeat > 0)
                {
                    Log("nhịp: " + (TickCount - _heartbeatTicks) + " tick/10s");
                }
                _heartbeatTicks = TickCount;
                _nextHeartbeat = now + 10;
            }
            if (StolenFromPid > 0 && FocusNeeded == 0 && !_inPump)
            {
                int pid = StolenFromPid;
                StolenFromPid = 0;
                Log("trả focus cho pid " + pid + ": " + SetFrontmost(pid));
            }
            if (_inPump)
            {
                try
                {
                    NestedTick?.Invoke();
                }
                catch (Exception exception)
                {
                    Log("NestedTick ném " + exception);
                }
                return;
            }
            _inPump = true;
            try
            {
                // Giữ vòng update của Editor quay khi Unity ở nền: không có gì cần vẽ thì Editor ngủ và coroutine đứng hàng phút.
                if (UxJourneys.Hub != null) UxJourneys.Hub.Repaint();
                Pump();
            }
            finally
            {
                _inPump = false;
            }
        }

        private void Pump()
        {
            try
            {
                NestedTick?.Invoke();
            }
            catch (Exception exception)
            {
                Log("NestedTick (chính) ném " + exception);
            }

            if (_waitFrames > 0)
            {
                _waitFrames--;
                return;
            }
            if (_waitUntilTime > 0)
            {
                if (EditorApplication.timeSinceStartup < _waitUntilTime) return;
                _waitUntilTime = 0;
            }
            if (_waitCondition != null)
            {
                bool done;
                try
                {
                    done = _waitCondition.Condition();
                }
                catch (Exception exception)
                {
                    Log("điều kiện chờ ném " + exception.Message);
                    done = true;
                }
                if (!done && EditorApplication.timeSinceStartup - _waitConditionStart < _waitCondition.TimeoutSeconds) return;
                if (!done) Log("hết hạn chờ điều kiện (" + _waitCondition.TimeoutSeconds + "s)");
                _waitCondition = null;
            }

            // Tối đa vài bước trong một tick để frame con không tốn thêm tick khi chỉ push.
            for (int guard = 0; guard < 8; guard++)
            {
                if (_stack.Count == 0) return;
                Frame top = _stack[_stack.Count - 1];
                bool moved;
                try
                {
                    moved = top.Enumerator.MoveNext();
                }
                catch (Exception exception)
                {
                    HandleException(exception);
                    return;
                }
                if (!moved)
                {
                    _stack.RemoveAt(_stack.Count - 1);
                    if (top.JourneyId != null) Log("<<< xong journey " + top.JourneyId);
                    continue;
                }
                object current = top.Enumerator.Current;
                if (current == null) return;
                if (current is IEnumerator nested)
                {
                    _stack.Add(new Frame { Enumerator = nested });
                    continue;
                }
                if (current is WaitFrames frames)
                {
                    _waitFrames = Math.Max(0, frames.Count - 1);
                    return;
                }
                if (current is WaitSeconds seconds)
                {
                    _waitUntilTime = EditorApplication.timeSinceStartup + seconds.Seconds;
                    return;
                }
                if (current is WaitUntilOrTimeout condition)
                {
                    _waitCondition = condition;
                    _waitConditionStart = EditorApplication.timeSinceStartup;
                    return;
                }
                return;
            }
        }

        private void HandleException(Exception exception)
        {
            string journey = null;
            // Bỏ các frame tới gốc journey.
            while (_stack.Count > 0)
            {
                Frame frame = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                if (frame.JourneyId != null)
                {
                    journey = frame.JourneyId;
                    break;
                }
                if (_stack.Count == 0) break;
            }
            string message = "journey " + (journey ?? CurrentJourney) + " bước " + CurrentStep + " ném " + exception;
            Failures.Add((journey ?? CurrentJourney) + "/" + CurrentStep + ": " + exception.GetType().Name + ": " + exception.Message);
            Log("LỖI " + message);
            if (_stack.Count == 0)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (_finished) return;
            _finished = true;
            Log("=== kết thúc, lỗi: " + Failures.Count + (Failures.Count > 0 ? " — " + string.Join(" | ", Failures) : string.Empty));
            File.WriteAllText(Path.Combine(Options.OutputDirectory, "failures.txt"), string.Join("\n", Failures), new UTF8Encoding(false));
            EditorApplication.update -= Tick;
            Application.logMessageReceivedThreaded -= OnLog;
            if (Options.ExitWhenDone)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(Failures.Count == 0 ? 0 : 1);
            }
        }

        // ------------------------------------------------------------------------------------------------ watchdog

        /// <summary>
        /// Luồng chính kẹt (menu hệ điều hành, hộp modal không tick update): chụp màn hình OS và thử Esc qua osascript khi app
        /// đứng trước là CHÍNH Unity này. Mọi ghi chép ra file riêng vì không được đụng API Unity từ luồng nền.
        /// </summary>
        private void WatchdogLoop()
        {
            string watchdogLog = Path.Combine(Options.OutputDirectory, "watchdog.log");
            long lastTick = Interlocked.Read(ref TickCount);
            DateTime lastChange = DateTime.UtcNow;
            DateTime lastRealChange = DateTime.UtcNow;
            int stuckEvents = 0;
            while (!_finished)
            {
                Thread.Sleep(500);
                long tick = Interlocked.Read(ref TickCount);
                if (tick != lastTick)
                {
                    lastTick = tick;
                    lastChange = DateTime.UtcNow;
                    lastRealChange = lastChange;
                    continue;
                }
                double stuckSeconds = (DateTime.UtcNow - lastChange).TotalSeconds;
                if (stuckSeconds >= 2 && stuckSeconds < 2.6)
                {
                    // đánh thức vòng sự kiện của Unity ở nền bằng một phím Shift gửi thẳng vào tiến trình (vô hại)
                    try { Helper("key " + ProcessId + " 56"); } catch (Exception) { }
                }
                double totalStuck = (DateTime.UtcNow - lastRealChange).TotalSeconds;
                if (totalStuck > 120)
                {
                    File.AppendAllText(watchdogLog, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " kẹt quá 120s ở " + CurrentJourney + "/" + CurrentStep + " — tự dừng Unity\n");
                    try { UxCapture.CaptureProcessWindows(this, Path.Combine(Options.OutputDirectory, "watchdog-final"), null); } catch (Exception) { }
                    Process.GetCurrentProcess().Kill();
                }
                if (stuckSeconds < 4) continue;
                stuckEvents++;
                string step = CurrentJourney + "/" + CurrentStep;
                try
                {
                    string[] plan;
                    string stem;
                    if (!ModalPlans.TryDequeue(out plan))
                    {
                        plan = new[] { "key:53" };
                        stem = Path.Combine(Options.OutputDirectory, "watchdog-" + stuckEvents.ToString("00", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        stem = Path.Combine(Options.OutputDirectory, ModalStems.TryDequeue(out string planned) ? planned : "modal-" + stuckEvents);
                    }
                    string note = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " kẹt " + stuckSeconds.ToString("0") + "s ở " + step;
                    note += " | chụp " + UxCapture.CaptureProcessWindows(this, stem, null);
                    int front;
                    int.TryParse(Helper("front").Trim(), out front);
                    if (front != ProcessId)
                    {
                        if (StolenFromPid == 0) StolenFromPid = front;
                        note += " | app trước pid " + front + " → đưa Unity này lên trước " + SetFrontmost(ProcessId);
                        Thread.Sleep(1200);
                        note += " | chụp lại " + UxCapture.CaptureProcessWindows(this, stem + "-front", null);
                    }
                    int index = 0;
                    foreach (string action in plan)
                    {
                        index++;
                        if (action.StartsWith("key:", StringComparison.Ordinal))
                        {
                            string[] parts = action.Split(':');
                            note += " | " + action + " → " + Helper("key " + ProcessId + " " + parts[1] + (parts.Length > 2 ? " " + parts[2] : string.Empty)).Trim();
                            Thread.Sleep(450);
                        }
                        else if (action.StartsWith("type:", StringComparison.Ordinal))
                        {
                            note += " | " + action + " → " + Helper("type " + ProcessId + " '" + action.Substring(5) + "'").Trim();
                            Thread.Sleep(450);
                        }
                        else if (action == "capture")
                        {
                            note += " | chụp " + UxCapture.CaptureProcessWindows(this, stem + "-k" + index, null);
                        }
                        if (Interlocked.Read(ref TickCount) != tick) note += " (luồng chính chạy lại)";
                    }
                    File.AppendAllText(watchdogLog, note + "\n");
                }
                catch (Exception exception)
                {
                    File.AppendAllText(watchdogLog, "watchdog ném " + exception.Message + "\n");
                }
                lastChange = DateTime.UtcNow; // chờ thêm trước lần can thiệp sau
            }
        }

        public string Helper(string arguments)
        {
            return RunProcess(Path.Combine(Options.ToolsDirectory, "uxwin"), arguments, 10000);
        }

        /// <summary>Đưa app có pid lên trước bằng System Events (NSRunningApplication.activate bị macOS 15 từ chối khi người dùng đang thao tác).</summary>
        public string SetFrontmost(int pid)
        {
            return RunProcess("/usr/bin/osascript", "-e 'tell application \"System Events\" to set frontmost of (first process whose unix id is " + pid + ") to true'", 10000).Trim();
        }

        public int FrontPid()
        {
            int.TryParse(Helper("front").Trim(), out int pid);
            return pid;
        }

        public static string RunProcess(string file, string arguments, int timeoutMilliseconds)
        {
            ProcessStartInfo info = new ProcessStartInfo("/bin/bash", "-c \"" + (file + " " + arguments).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (Process process = Process.Start(info))
            {
                if (process == null) return string.Empty;
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    try { process.Kill(); } catch (Exception) { }
                }
                return output + (error.Length > 0 ? " [stderr] " + error : string.Empty);
            }
        }
    }
}
