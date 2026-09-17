// MÃ DEV TẠM (G-UX-JOURNEY) — thao tác cần hệ điều hành: menu gốc (NSMenu chặn luồng chính), hộp modal, giữ/trả focus.
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;

namespace UxJourney
{
    public static class UxOs
    {
        /// <summary>
        /// Lên lịch ở LUỒNG NỀN: sau <paramref name="delayMilliseconds"/> chụp mọi cửa sổ của Unity này (menu gốc là cửa sổ layer 101)
        /// rồi gửi chuỗi phím (osascript) khi app đứng trước là Unity này. Gọi NGAY TRƯỚC thao tác mở menu/modal, vì thao tác đó chặn
        /// luồng chính cho tới khi menu đóng.
        /// </summary>
        public static void ScheduleKeys(UxRunner runner, string stem, int delayMilliseconds, string[] keyScripts, string note)
        {
            string logPath = Path.Combine(runner.Options.OutputDirectory, "os-automation.log");
            Thread thread = new Thread(() =>
            {
                try
                {
                    Thread.Sleep(delayMilliseconds);
                    string capture = UxCapture.CaptureProcessWindows(runner, stem, null);
                    string line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " " + note + " chụp: " + capture;
                    int front;
                    int.TryParse(runner.Helper("front").Trim(), out front);
                    if (keyScripts.Length > 0 && front != runner.ProcessId)
                    {
                        line += " | app trước pid " + front + " khác Unity này → kích hoạt " + runner.Helper("activate " + runner.ProcessId).Trim();
                        Thread.Sleep(800);
                        int.TryParse(runner.Helper("front").Trim(), out front);
                    }
                    foreach (string key in keyScripts)
                    {
                        if (front != runner.ProcessId)
                        {
                            line += " | KHÔNG gửi phím '" + key + "' (app trước pid " + front + ")";
                            break;
                        }
                        string result = UxRunner.RunProcess("/usr/bin/osascript", "-e 'tell application \"System Events\" to " + key + "'", 10000);
                        line += " | " + key + " → " + result.Trim();
                        Thread.Sleep(350);
                        int.TryParse(runner.Helper("front").Trim(), out front);
                    }
                    File.AppendAllText(logPath, line + "\n");
                }
                catch (Exception exception)
                {
                    File.AppendAllText(logPath, "ScheduleKeys ném " + exception.Message + "\n");
                }
            }) { IsBackground = true, Name = "UxOsKeys" };
            thread.Start();
        }

        /// <summary>Kích hoạt Unity này (menu gốc/modal cần app đứng trước), nhớ app trước để trả lại.</summary>
        public static IEnumerator Activate(UxRunner runner)
        {
            int front = runner.FrontPid();
            if (front != runner.ProcessId)
            {
                if (runner.StolenFromPid == 0) runner.StolenFromPid = front;
                runner.Log("đưa Unity này lên trước (app trước pid " + front + "): " + runner.SetFrontmost(runner.ProcessId));
            }
            runner.FocusNeeded++;
            yield return new WaitSeconds(0.8);
        }

        public static void Release(UxRunner runner)
        {
            runner.FocusNeeded = Math.Max(0, runner.FocusNeeded - 1);
        }
    }
}
