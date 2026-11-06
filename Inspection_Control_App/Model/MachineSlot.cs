using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Inspection_Control_App.Model
{
    /// <summary>
    /// Một ô máy (tablet) trên sơ đồ. Vị trí lấy từ bảng [KVH].[dbo].[MachineLayout].
    /// </summary>
    public class MachineSlot : INotifyPropertyChanged
    {
        /// <summary>Id trong bảng MachineLayout. 0 = sơ đồ mặc định (chưa có bảng trên DB).</summary>
        public int Id { get; set; }
        public string MachineName { get; set; } = "";
        public int RowNo { get; set; }
        public int ColNo { get; set; }

        private string _indexTerminal = "";
        public string IndexTerminal
        {
            get => _indexTerminal;
            set { _indexTerminal = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(SubText)); }
        }

        private bool _isMatched;
        /// <summary>Máy đang giữ PO cùng type với PO đang check → sáng lên.</summary>
        public bool IsMatched
        {
            get => _isMatched;
            set { _isMatched = value; OnPropertyChanged(); }
        }

        private bool _isCurrent;
        /// <summary>Đây là máy đang chạy app.</summary>
        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        private int _poCount;
        public int PoCount
        {
            get => _poCount;
            set { _poCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubText)); }
        }

        private string? _matchInfo;
        /// <summary>Danh sách PO trùng type (hiện ở tooltip).</summary>
        public string? MatchInfo
        {
            get => _matchInfo;
            set { _matchInfo = value; OnPropertyChanged(); }
        }

        public string SubText
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(IndexTerminal)) parts.Add($"Bàn {IndexTerminal}");
                if (PoCount > 0) parts.Add($"{PoCount} PO");
                return string.Join(" · ", parts);
            }
        }

        /// <summary>
        /// Sơ đồ mặc định, dùng khi chưa tạo bảng MachineLayout trên DB
        /// (giống dữ liệu mẫu trong SQL/Scripts/MachineLayout.sql).
        /// </summary>
        public static List<MachineSlot> Defaults() =>
        [
            new() { MachineName = "F2Tab22010165", RowNo = 1, ColNo = 1 },
            new() { MachineName = "SPC-F2JP005",   RowNo = 2, ColNo = 1 },
            new() { MachineName = "F2TAB22010293", RowNo = 3, ColNo = 1 },
            new() { MachineName = "F2Tab22010187", RowNo = 4, ColNo = 1 },
            new() { MachineName = "F2-TABLET999",  RowNo = 5, ColNo = 1 }, // mã tạm, cần sửa
            new() { MachineName = "SPC-F2PC22024", RowNo = 1, ColNo = 2 },
            new() { MachineName = "F2Tab22040014", RowNo = 2, ColNo = 2 },
            new() { MachineName = "INS2018",       RowNo = 3, ColNo = 2 },
            new() { MachineName = "F2-TABLET115",  RowNo = 4, ColNo = 2 },
            new() { MachineName = "F2TAB18010172", RowNo = 5, ColNo = 2 },
            new() { MachineName = "F2-TABLET83",   RowNo = 4, ColNo = 3 },
            new() { MachineName = "F2_TAB_PR001",  RowNo = 5, ColNo = 3 },
        ];

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
