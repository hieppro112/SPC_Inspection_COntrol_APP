namespace Inspection_Control_App.Model
{
    /// <summary>
    /// Bản nháp sơ đồ máy khi đang chỉnh sửa: kéo thả, thêm, xóa, đổi mã chỉ thay đổi ở đây,
    /// bấm Lưu mới ghi xuống DB (MySQL.SaveMachineLayout). Không phụ thuộc WPF để test được.
    /// </summary>
    public class LayoutDraft
    {
        public const int Rows = 6;
        public const int Cols = 4;

        private readonly List<MachineSlot> _slots;
        private readonly List<int> _deletedIds = new();
        private readonly Dictionary<int, (string Name, int Row, int Col)> _original;

        public LayoutDraft(IEnumerable<MachineSlot> original)
        {
            _slots = original.Select(s => s.Clone()).ToList();
            _original = _slots.Where(s => s.Id > 0)
                              .ToDictionary(s => s.Id, s => (s.MachineName, s.RowNo, s.ColNo));
        }

        /// <summary>Tất cả máy trong bản nháp (kể cả máy nằm ngoài khung).</summary>
        public IReadOnlyList<MachineSlot> Slots => _slots;

        /// <summary>Id các máy đã có trên DB bị xóa.</summary>
        public IReadOnlyList<int> DeletedIds => _deletedIds;

        public static bool InFrame(int row, int col) => row >= 1 && row <= Rows && col >= 1 && col <= Cols;

        public MachineSlot? At(int row, int col) =>
            _slots.FirstOrDefault(s => s.RowNo == row && s.ColNo == col);

        /// <summary>Chuyển máy sang ô (row, col); ô có máy thì đổi chỗ.</summary>
        public bool Move(MachineSlot slot, int row, int col)
        {
            if (!InFrame(row, col) || !_slots.Contains(slot)) return false;
            if (slot.RowNo == row && slot.ColNo == col) return false;

            var other = At(row, col);
            if (other != null)
            {
                other.RowNo = slot.RowNo;
                other.ColNo = slot.ColNo;
            }
            slot.RowNo = row;
            slot.ColNo = col;
            return true;
        }

        /// <summary>Thêm máy mới. Trả về null nếu thành công, ngược lại là thông báo lỗi.</summary>
        public string? Add(string name, int row, int col)
        {
            name = (name ?? "").Trim();
            if (!InFrame(row, col)) return "Vị trí nằm ngoài khung.";
            if (At(row, col) != null) return "Ô này đã có máy.";
            var error = ValidateName(name, except: null);
            if (error != null) return error;

            _slots.Add(new MachineSlot { Id = 0, MachineName = name, RowNo = row, ColNo = col });
            return null;
        }

        public void Remove(MachineSlot slot)
        {
            if (!_slots.Remove(slot)) return;
            if (slot.Id > 0) _deletedIds.Add(slot.Id);
        }

        /// <summary>Đổi mã máy. Trả về null nếu thành công, ngược lại là thông báo lỗi.</summary>
        public string? Rename(MachineSlot slot, string newName)
        {
            newName = (newName ?? "").Trim();
            var error = ValidateName(newName, except: slot);
            if (error != null) return error;
            slot.MachineName = newName;
            return null;
        }

        /// <summary>Kiểm tra mã máy: không rỗng, không quá 100 ký tự, không trùng máy khác (không phân biệt hoa thường).</summary>
        public string? ValidateName(string name, MachineSlot? except)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return "Vui lòng nhập mã máy.";
            if (name.Length > 100) return "Mã máy tối đa 100 ký tự.";
            if (_slots.Any(s => !ReferenceEquals(s, except)
                                && string.Equals(s.MachineName, name, StringComparison.OrdinalIgnoreCase)))
                return $"Mã máy '{name}' đã có trên sơ đồ.";
            return null;
        }

        /// <summary>Máy cũ (Id &gt; 0) có vị trí hoặc mã khác bản gốc.</summary>
        public IEnumerable<MachineSlot> ChangedExisting =>
            _slots.Where(s => s.Id > 0 && _original.TryGetValue(s.Id, out var o)
                              && (o.Row != s.RowNo || o.Col != s.ColNo
                                  || !string.Equals(o.Name, s.MachineName, StringComparison.Ordinal)));

        public IEnumerable<MachineSlot> Added => _slots.Where(s => s.Id <= 0);

        public int ChangeCount => _deletedIds.Count + Added.Count() + ChangedExisting.Count();

        public bool HasChanges => ChangeCount > 0;
    }
}
