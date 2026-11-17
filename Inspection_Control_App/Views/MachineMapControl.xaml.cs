using Inspection_Control_App.Model;
using Inspection_Control_App.SQL;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Sơ đồ máy trong khung 6×4. Máy giữ PO cùng type với PO đang check thì sáng lên.
    /// Ở chế độ chỉnh sửa: kéo thả đổi vị trí, thêm, xóa, đổi mã trên bản nháp (LayoutDraft);
    /// bấm Lưu (SaveAsync) mới ghi xuống DB.
    /// </summary>
    public partial class MachineMapControl : UserControl
    {
        private const double RowHeight = 56;
        private const double DragThreshold = 6;

        private List<MachineSlot> _slots = new();   // dữ liệu từ DB
        private LayoutDraft? _draft;                // bản nháp khi đang chỉnh sửa

        // highlight gần nhất (áp dụng lại sau mỗi lần vẽ lại)
        private List<CheckStatusModel> _lastRows = new();
        private string _lastDevice = Environment.MachineName;

        // kéo thả
        private MachineSlot? _pressSlot;
        private FrameworkElement? _pressTile;
        private Point _pressPoint;
        private Vector _grabOffset;
        private bool _dragging;
        private DragGhostAdorner? _ghost;
        private AdornerLayer? _adornerLayer;
        private Border? _dropIndicator;

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(MachineMapControl),
                new PropertyMetadata(false));

        /// <summary>Đang ở chế độ chỉnh sửa sơ đồ.</summary>
        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            private set => SetValue(IsEditingProperty, value);
        }

        /// <summary>Vào/thoát chế độ chỉnh sửa hoặc số thay đổi chưa lưu thay đổi.</summary>
        public event Action? EditStateChanged;

        public bool HasChanges => _draft?.HasChanges ?? false;
        public int ChangeCount => _draft?.ChangeCount ?? 0;

        /// <summary>Có thể chỉnh sửa: sơ đồ lấy từ DB (không phải sơ đồ mặc định).</summary>
        public bool CanEdit => _slots.Count > 0 && _slots.All(s => s.Id > 0);

        private IEnumerable<MachineSlot> DisplayedSlots => (IEnumerable<MachineSlot>?)_draft?.Slots ?? _slots;

        public MachineMapControl()
        {
            InitializeComponent();
            for (int r = 0; r < LayoutDraft.Rows; r++)
                MapGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
            for (int c = 0; c < LayoutDraft.Cols; c++)
                MapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            LostMouseCapture += (_, _) => CancelDrag();
            PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && _pressSlot != null) { CancelDrag(); e.Handled = true; } };

            BuildGrid();
        }

        // ================= Tải / lưu =================

        /// <summary>Đọc sơ đồ máy từ DB (hoặc sơ đồ mặc định), thoát chế độ chỉnh sửa và vẽ lại.</summary>
        public async Task LoadLayoutAsync(MySQL sql)
        {
            var slots = await sql.GetMachineLayout();
            if (_pressSlot != null) return; // đang kéo → bỏ qua lần tải này
            _slots = slots;
            ExitEdit();
        }

        /// <summary>Vào chế độ chỉnh sửa. Trả về false nếu không thể (đang dùng sơ đồ mặc định).</summary>
        public bool BeginEdit()
        {
            if (!CanEdit) return false;
            if (IsEditing) return true;
            _draft = new LayoutDraft(_slots);
            IsEditing = true;
            Refresh();
            return true;
        }

        /// <summary>Bỏ mọi thay đổi chưa lưu và thoát chế độ chỉnh sửa.</summary>
        public void CancelEdit() => ExitEdit();

        /// <summary>Ghi bản nháp xuống DB. Thành công → tải lại và thoát chỉnh sửa; trả về null. Lỗi → giữ bản nháp, trả về thông báo.</summary>
        public async Task<string?> SaveAsync(MySQL sql, string updatedBy)
        {
            if (_draft == null) return null;
            if (!_draft.HasChanges) { ExitEdit(); return null; }

            string? error = await sql.SaveMachineLayout(_draft, updatedBy);
            if (error != null) return error;

            await LoadLayoutAsync(sql);
            return null;
        }

        private void ExitEdit()
        {
            CancelDrag();
            _draft = null;
            IsEditing = false;
            Refresh();
        }

        private void Refresh()
        {
            BuildGrid();
            ApplyHighlight();
            TxtEditHint.Visibility = IsEditing ? Visibility.Visible : Visibility.Collapsed;
            Frame.BorderBrush = IsEditing ? new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)) : new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
            TxtHeader.Text = IsEditing ? "✏ CHỈNH SỬA SƠ ĐỒ MÁY" : "🗺 SƠ ĐỒ MÁY (CÙNG TYPE)";
            EditStateChanged?.Invoke();
        }

        // ================= Vẽ khung =================

        private void BuildGrid()
        {
            MapGrid.Children.Clear();

            // 24 ô trống (nền)
            for (int r = 1; r <= LayoutDraft.Rows; r++)
                for (int c = 1; c <= LayoutDraft.Cols; c++)
                    MapGrid.Children.Add(CreateEmptyCell(r, c));

            // các máy trong khung
            var template = (DataTemplate)Resources["MachineTileTemplate"];
            foreach (var slot in DisplayedSlots.Where(s => LayoutDraft.InFrame(s.RowNo, s.ColNo)))
            {
                var tile = new ContentControl
                {
                    Content = slot,
                    ContentTemplate = template,
                    Focusable = false,
                    Cursor = IsEditing ? Cursors.SizeAll : null,
                };
                tile.PreviewMouseLeftButtonDown += (s, e) => OnTilePressed(slot, tile, e);
                Grid.SetRow(tile, slot.RowNo - 1);
                Grid.SetColumn(tile, slot.ColNo - 1);
                Panel.SetZIndex(tile, 1);
                MapGrid.Children.Add(tile);
            }

            // chỉ báo ô đích khi kéo
            _dropIndicator = new Border
            {
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(9),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0x4F, 0xC3, 0xF7)),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
            };
            Panel.SetZIndex(_dropIndicator, 2);
            MapGrid.Children.Add(_dropIndicator);
        }

        private FrameworkElement CreateEmptyCell(int row, int col)
        {
            var rect = new Rectangle
            {
                Margin = new Thickness(3),
                RadiusX = 8,
                RadiusY = 8,
                Stroke = new SolidColorBrush(Color.FromRgb(0x3A, 0x3D, 0x44)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Fill = Brushes.Transparent,
            };
            var plus = new TextBlock
            {
                Text = "＋",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x60, 0x70)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Visibility = IsEditing ? Visibility.Visible : Visibility.Collapsed,
            };
            var cell = new Grid { Background = Brushes.Transparent };
            cell.Children.Add(rect);
            cell.Children.Add(plus);

            if (IsEditing)
            {
                cell.Cursor = Cursors.Hand;
                cell.ToolTip = "Thêm máy vào ô này";
                cell.MouseEnter += (_, _) => { if (_pressSlot == null) rect.Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)); };
                cell.MouseLeave += (_, _) => rect.Stroke = new SolidColorBrush(Color.FromRgb(0x3A, 0x3D, 0x44));
                cell.MouseLeftButtonUp += (_, e) =>
                {
                    if (_pressSlot != null || _draft == null || _draft.At(row, col) != null) return;
                    e.Handled = true;
                    AddMachineAt(row, col);
                };
            }

            Grid.SetRow(cell, row - 1);
            Grid.SetColumn(cell, col - 1);
            return cell;
        }

        // ================= Thêm / xóa / đổi mã =================

        private Window? OwnerWindow => Window.GetWindow(this);

        private static string PositionText(int row, int col) => $"Vị trí: hàng {row}, cột {col}";

        private void AddMachineAt(int row, int col)
        {
            if (_draft == null) return;
            var draft = _draft;
            var dialog = new EditMachineDialog("THÊM MÁY", PositionText(row, col), "",
                name => draft.ValidateName(name, null)) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true && draft.Add(dialog.NewName, row, col) == null)
                Refresh();
        }

        private void RenameMachine(MachineSlot slot)
        {
            if (_draft == null) return;
            var draft = _draft;
            string pos = PositionText(slot.RowNo, slot.ColNo)
                + (string.IsNullOrWhiteSpace(slot.IndexTerminal) ? "" : $"  ·  Bàn {slot.IndexTerminal}");
            var dialog = new EditMachineDialog("ĐỔI MÃ MÁY", pos, slot.MachineName,
                name => draft.ValidateName(name, slot)) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true && draft.Rename(slot, dialog.NewName) == null)
                Refresh();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_draft == null || (sender as FrameworkElement)?.DataContext is not MachineSlot slot) return;

            var answer = MessageBox.Show(OwnerWindow!,
                $"Xóa máy '{slot.MachineName}' khỏi sơ đồ?\n(Chỉ thực sự xóa khi bấm Lưu.)",
                "Xóa máy", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            _draft.Remove(slot);
            Refresh();
        }

        // ================= Kéo thả (tự viết, có hình bóng) =================

        private void OnTilePressed(MachineSlot slot, FrameworkElement tile, MouseButtonEventArgs e)
        {
            if (!IsEditing || _pressSlot != null) return;
            if (IsInsideButton(e.OriginalSource as DependencyObject, tile)) return; // bấm nút ✕

            _pressSlot = slot;
            _pressTile = tile;
            _pressPoint = e.GetPosition(MapGrid);
            _grabOffset = _pressPoint - tile.TranslatePoint(new Point(0, 0), MapGrid);
            _dragging = false;

            Focus();          // để nhận phím Esc
            CaptureMouse();   // nhận MouseMove/Up kể cả khi ra ngoài khung
            e.Handled = true;
        }

        private static bool IsInsideButton(DependencyObject? source, DependencyObject stopAt)
        {
            while (source != null && source != stopAt)
            {
                if (source is ButtonBase) return true;
                source = source is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }
            return false;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_pressSlot == null || _pressTile == null) return;
            var pos = e.GetPosition(MapGrid);

            if (!_dragging)
            {
                if ((pos - _pressPoint).Length < DragThreshold) return;
                StartDrag();
            }

            _ghost?.MoveTo(pos - _grabOffset);

            var cell = HitCell(pos);
            if (cell is (int r, int c) && _dropIndicator != null)
            {
                Grid.SetRow(_dropIndicator, r - 1);
                Grid.SetColumn(_dropIndicator, c - 1);
                _dropIndicator.Visibility = Visibility.Visible;
            }
            else if (_dropIndicator != null)
            {
                _dropIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void StartDrag()
        {
            if (_pressTile == null) return;
            _dragging = true;

            _adornerLayer = AdornerLayer.GetAdornerLayer(MapGrid);
            if (_adornerLayer != null)
            {
                _ghost = new DragGhostAdorner(MapGrid, _pressTile);
                _adornerLayer.Add(_ghost);
            }
            _pressTile.Opacity = 0.3;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_pressSlot == null) return;
            var slot = _pressSlot;
            bool wasDragging = _dragging;
            var drop = HitCell(e.GetPosition(MapGrid));

            CancelDrag();
            e.Handled = true;

            if (!wasDragging)
            {
                RenameMachine(slot); // bấm rồi nhả không kéo → đổi mã
                return;
            }
            if (_draft != null && drop is (int r, int c) && _draft.Move(slot, r, c))
                Refresh();
        }

        /// <summary>Kết thúc kéo, đưa giao diện về như cũ (không thay đổi bản nháp).</summary>
        private void CancelDrag()
        {
            if (_pressSlot == null) return;
            _pressSlot = null; // đặt trước để LostMouseCapture không gọi lại

            if (_ghost != null) _adornerLayer?.Remove(_ghost);
            _ghost = null;
            _adornerLayer = null;
            if (_pressTile != null) _pressTile.Opacity = 1;
            _pressTile = null;
            _dragging = false;
            if (_dropIndicator != null) _dropIndicator.Visibility = Visibility.Collapsed;

            if (IsMouseCaptured) ReleaseMouseCapture();
        }

        /// <summary>Ô (hàng, cột) dưới điểm p (tọa độ theo MapGrid); null nếu ngoài khung.</summary>
        private (int Row, int Col)? HitCell(Point p)
        {
            double w = MapGrid.ActualWidth, h = MapGrid.ActualHeight;
            if (w <= 0 || h <= 0 || p.X < 0 || p.Y < 0 || p.X >= w || p.Y >= h) return null;
            int col = (int)(p.X / (w / LayoutDraft.Cols)) + 1;
            int row = (int)(p.Y / (h / LayoutDraft.Rows)) + 1;
            return LayoutDraft.InFrame(row, col) ? (row, col) : null;
        }

        // ================= Sáng đèn =================

        /// <summary>
        /// Làm sáng các máy có trong danh sách PO cùng type (kết quả GetPOByPSTX).
        /// </summary>
        public void Highlight(IEnumerable<CheckStatusModel>? rows, string currentDevice)
        {
            _lastRows = rows?.ToList() ?? new List<CheckStatusModel>();
            _lastDevice = currentDevice ?? "";
            ApplyHighlight();
        }

        private void ApplyHighlight()
        {
            var byMachine = _lastRows
                .Where(r => !string.IsNullOrWhiteSpace(r.MachineName))
                .GroupBy(r => r.MachineName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var slots = DisplayedSlots.ToList();
            int litCount = 0;
            foreach (var slot in slots)
            {
                slot.IsCurrent = string.Equals(slot.MachineName, _lastDevice.Trim(), StringComparison.OrdinalIgnoreCase);

                if (byMachine.TryGetValue(slot.MachineName, out var pos))
                {
                    slot.IsMatched = true;
                    slot.PoCount = pos.Select(p => p.PONumber).Distinct().Count();
                    slot.MatchInfo = string.Join(Environment.NewLine,
                        pos.Select(p => $"{p.PONumber}  ·  {p.Roname}  ·  {p.Status}"));
                    if (string.IsNullOrWhiteSpace(slot.IndexTerminal))
                        slot.IndexTerminal = pos.Select(p => p.Index).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i)) ?? "";
                    if (LayoutDraft.InFrame(slot.RowNo, slot.ColNo)) litCount++;
                }
                else
                {
                    slot.IsMatched = false;
                    slot.PoCount = 0;
                    slot.MatchInfo = null;
                }
            }

            var parts = new List<string>();
            if (byMachine.Count == 0)
                parts.Add("Không có máy nào giữ PO cùng type.");
            else
                parts.Add($"{litCount} máy đang giữ PO cùng type.");

            var notOnMap = byMachine.Keys
                .Where(k => !slots.Any(s => LayoutDraft.InFrame(s.RowNo, s.ColNo)
                                            && string.Equals(s.MachineName, k, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (notOnMap.Count > 0)
                parts.Add($"Máy giữ cùng type nhưng không có trên sơ đồ: {string.Join(", ", notOnMap)}.");

            var outOfFrame = slots.Where(s => !LayoutDraft.InFrame(s.RowNo, s.ColNo)).Select(s => s.MachineName).ToList();
            if (outOfFrame.Count > 0)
                parts.Add($"Máy ngoài khung {LayoutDraft.Rows}×{LayoutDraft.Cols}: {string.Join(", ", outOfFrame)}.");

            TxtSummary.Text = string.Join("  ", parts);
        }
    }
}
