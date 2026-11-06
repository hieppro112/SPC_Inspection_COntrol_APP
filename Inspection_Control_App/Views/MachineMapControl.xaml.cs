using Inspection_Control_App.Model;
using Inspection_Control_App.SQL;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Sơ đồ các máy (tablet). Máy nào đang giữ PO cùng type với PO đang check thì sáng lên.
    /// </summary>
    public partial class MachineMapControl : UserControl
    {
        private List<MachineSlot> _slots = new();
        private bool _isEditable;

        /// <summary>Bấm vào ô máy (chỉ phát ra khi IsEditable = true).</summary>
        public event Action<MachineSlot>? MachineClicked;

        /// <summary>Cho phép bấm vào ô máy để sửa mã (màn Manage, sau khi xác nhận MSNV).</summary>
        public bool IsEditable
        {
            get => _isEditable;
            set
            {
                _isEditable = value;
                TxtEditHint.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                foreach (var tile in MapGrid.Children.OfType<ContentControl>())
                    tile.Cursor = value ? Cursors.Hand : null;
            }
        }

        public MachineMapControl()
        {
            InitializeComponent();
        }

        /// <summary>Đọc sơ đồ máy từ DB (hoặc sơ đồ mặc định) và vẽ lưới.</summary>
        public async Task LoadLayoutAsync(MySQL sql)
        {
            _slots = await sql.GetMachineLayout();
            BuildGrid();
        }

        private void BuildGrid()
        {
            MapGrid.Children.Clear();
            MapGrid.RowDefinitions.Clear();
            MapGrid.ColumnDefinitions.Clear();
            if (_slots.Count == 0) return;

            int maxRow = _slots.Max(s => s.RowNo);
            int maxCol = _slots.Max(s => s.ColNo);
            for (int r = 0; r < maxRow; r++)
                MapGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < maxCol; c++)
                MapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var template = (DataTemplate)Resources["MachineTileTemplate"];
            foreach (var slot in _slots)
            {
                var tile = new ContentControl
                {
                    Content = slot,
                    ContentTemplate = template,
                    Cursor = _isEditable ? Cursors.Hand : null,
                };
                tile.MouseLeftButtonUp += (_, _) =>
                {
                    if (_isEditable) MachineClicked?.Invoke(slot);
                };
                Grid.SetRow(tile, slot.RowNo - 1);
                Grid.SetColumn(tile, slot.ColNo - 1);
                MapGrid.Children.Add(tile);
            }
        }

        /// <summary>
        /// Làm sáng các máy có trong danh sách PO cùng type (kết quả GetPOByPSTX).
        /// </summary>
        public void Highlight(IEnumerable<CheckStatusModel>? rows, string currentDevice)
        {
            var byMachine = (rows ?? Enumerable.Empty<CheckStatusModel>())
                .Where(r => !string.IsNullOrWhiteSpace(r.MachineName))
                .GroupBy(r => r.MachineName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            int litCount = 0;
            foreach (var slot in _slots)
            {
                slot.IsCurrent = string.Equals(slot.MachineName, currentDevice?.Trim(), StringComparison.OrdinalIgnoreCase);

                if (byMachine.TryGetValue(slot.MachineName, out var pos))
                {
                    slot.IsMatched = true;
                    slot.PoCount = pos.Select(p => p.PONumber).Distinct().Count();
                    slot.MatchInfo = string.Join(Environment.NewLine,
                        pos.Select(p => $"{p.PONumber}  ·  {p.Roname}  ·  {p.Status}"));
                    if (string.IsNullOrWhiteSpace(slot.IndexTerminal))
                        slot.IndexTerminal = pos.Select(p => p.Index).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i)) ?? "";
                    litCount++;
                }
                else
                {
                    slot.IsMatched = false;
                    slot.PoCount = 0;
                    slot.MatchInfo = null;
                }
            }

            // Máy giữ PO cùng type nhưng không có trên sơ đồ
            var outside = byMachine.Keys
                .Where(k => !_slots.Any(s => string.Equals(s.MachineName, k, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (byMachine.Count == 0)
                TxtSummary.Text = "Không có máy nào giữ PO cùng type.";
            else
                TxtSummary.Text = $"{litCount} máy đang giữ PO cùng type."
                    + (outside.Count > 0 ? $"  Máy ngoài sơ đồ: {string.Join(", ", outside)}" : "");
        }
    }
}
