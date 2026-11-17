using System.Diagnostics;
using System.IO;

namespace Inspection_Control_App.Helper
{
    /// <summary>
    /// Ghi log ra file C:\Inspection_Config\logs\app_yyyyMMdd.log (giữ 14 ngày gần nhất).
    /// Không bao giờ ném lỗi ra ngoài.
    /// </summary>
    public static class AppLog
    {
        private static readonly string LogDir = @"C:\Inspection_Config\logs";
        private static readonly object Gate = new();
        private const int KeepDays = 14;
        private static bool _cleaned;

        public static void Info(string message) => Write("INFO", message, null);

        public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        /// <summary>Ghi mức dùng bộ nhớ hiện tại (để theo dõi khi chạy lâu).</summary>
        public static void Memory(string context)
        {
            try
            {
                using var p = Process.GetCurrentProcess();
                Info($"{context} | WorkingSet={p.WorkingSet64 / 1024 / 1024} MB, " +
                     $"Private={p.PrivateMemorySize64 / 1024 / 1024} MB, " +
                     $"GC={GC.GetTotalMemory(false) / 1024 / 1024} MB, Handles={p.HandleCount}");
            }
            catch { /* bỏ qua */ }
        }

        private static void Write(string level, string message, Exception? ex)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{Environment.MachineName}] {message}";
            if (ex != null) line += Environment.NewLine + ex;
            Debug.WriteLine(line);

            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(LogDir);
                    File.AppendAllText(Path.Combine(LogDir, $"app_{DateTime.Now:yyyyMMdd}.log"), line + Environment.NewLine);
                    if (!_cleaned) { _cleaned = true; CleanOldLogs(); }
                }
            }
            catch { /* không để lỗi ghi log làm sập app */ }
        }

        private static void CleanOldLogs()
        {
            foreach (var file in Directory.GetFiles(LogDir, "app_*.log"))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-KeepDays))
                    File.Delete(file);
            }
        }
    }
}
