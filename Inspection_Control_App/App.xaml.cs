using Inspection_Control_App.Helper;
using System.Windows;
using System.Windows.Threading;

namespace Inspection_Control_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Lỗi trên luồng giao diện: ghi log và giữ app chạy tiếp
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            // Lỗi ở luồng nền: ghi log (app vẫn có thể bị tắt nhưng có dấu vết)
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                AppLog.Error($"Lỗi nghiêm trọng (IsTerminating={args.IsTerminating})", args.ExceptionObject as Exception);
            // Task lỗi mà không ai await
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                AppLog.Error("Lỗi Task không được xử lý", args.Exception);
                args.SetObserved();
            };

            AppLog.Info("===== Khởi động app =====");
            AppLog.Memory("Startup");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLog.Memory($"Thoát app (ExitCode={e.ApplicationExitCode})");
            base.OnExit(e);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLog.Error("Lỗi không được xử lý trên giao diện", e.Exception);
            AppLog.Memory("Lúc xảy ra lỗi");

            // Hết bộ nhớ thì không cố chạy tiếp
            if (e.Exception is OutOfMemoryException) return;

            e.Handled = true;
        }
    }
}
