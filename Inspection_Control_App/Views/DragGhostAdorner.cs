using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Hình bóng của ô máy chạy theo con trỏ khi kéo thả (vẽ trên AdornerLayer của khung).
    /// </summary>
    public class DragGhostAdorner : Adorner
    {
        private readonly ImageSource _snapshot;
        private readonly Size _size;
        private Point _topLeft;

        /// <param name="adornedElement">Khung chứa (tọa độ vẽ tính theo phần tử này).</param>
        /// <param name="source">Ô máy cần chụp lại làm hình bóng.</param>
        public DragGhostAdorner(UIElement adornedElement, FrameworkElement source) : base(adornedElement)
        {
            IsHitTestVisible = false;
            _size = new Size(Math.Max(1, source.ActualWidth), Math.Max(1, source.ActualHeight));

            var dpi = VisualTreeHelper.GetDpi(source);
            var bmp = new RenderTargetBitmap(
                (int)Math.Ceiling(_size.Width * dpi.DpiScaleX),
                (int)Math.Ceiling(_size.Height * dpi.DpiScaleY),
                dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bmp.Render(source);
            bmp.Freeze();
            _snapshot = bmp;
        }

        /// <summary>Đặt góc trên-trái của hình bóng (tọa độ theo khung).</summary>
        public void MoveTo(Point topLeft)
        {
            _topLeft = topLeft;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var rect = new Rect(_size);
            dc.PushTransform(new TranslateTransform(_topLeft.X, _topLeft.Y));
            dc.PushTransform(new RotateTransform(-3, _size.Width / 2, _size.Height / 2));

            // bóng đổ
            var shadow = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0));
            dc.DrawRoundedRectangle(shadow, null, new Rect(6, 8, _size.Width - 6, _size.Height - 6), 8, 8);

            dc.PushOpacity(0.92);
            dc.DrawImage(_snapshot, rect);
            dc.Pop();

            // viền sáng cho dễ nhìn
            dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)), 2),
                new Rect(3, 3, _size.Width - 6, _size.Height - 6), 8, 8);

            dc.Pop();
            dc.Pop();
        }
    }
}
