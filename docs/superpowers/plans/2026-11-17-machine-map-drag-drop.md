# Sơ đồ máy kéo thả — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Khung 6×4 ô; ở màn Manage (sau khi xác nhận MSNV quản lý) kéo thả / thêm / xóa / đổi mã máy trên bản nháp, bấm Lưu mới ghi DB; ô máy làm đẹp hơn.

**Architecture:** Logic bản nháp thuần (không WPF) nằm trong `LayoutDraft` để test được. `MachineMapControl` vẽ khung, tự xử lý kéo thả bằng mouse capture + Adorner (Cách 2) và gọi `LayoutDraft`. `MySQL.SaveMachineLayout` ghi bản nháp trong một transaction.

**Tech Stack:** WPF .NET 8 (`net8.0-windows`), `System.Data.SqlClient`, xUnit cho test.

**Spec:** `docs/superpowers/specs/2026-11-17-machine-map-drag-drop-design.md`

## Global Constraints

- Khung cố định **6 hàng × 4 cột**; `RowNo`/`ColNo` trong DB bắt đầu từ 1.
- Bảng `[KVH].[dbo].[MachineLayout]`: UNIQUE `(RowNo, ColNo)`, UNIQUE `MachineName`; cột `UpdatedBy`, `UpdatedAt`.
- So sánh mã máy **không phân biệt hoa thường**, có `Trim()`.
- Sơ đồ mặc định (khi chưa có bảng; mọi `Id == 0`) → không cho vào chế độ chỉnh sửa.
- Không thêm thư viện cho app (test project được dùng xUnit).
- Ngưỡng bắt đầu kéo: **6px**.

## Review Focus

1. Đổi chỗ 2 máy rồi Lưu → UNIQUE `(RowNo, ColNo)` không được nổ (dời tạm vị trí âm trong transaction) — test thủ công với DB (Task 2).
2. Xóa máy A rồi thêm lại mã A trong cùng phiên → phải lưu được (DELETE trước INSERT) — `Add_NameOfDeletedMachine_Allowed` (Task 1).
3. Đổi mã thành mã máy khác nhưng khác hoa thường → bị từ chối — `Rename_DuplicateIgnoringCase_Rejected` (Task 1).
4. Thả ra ngoài khung / Esc / mất capture → máy về chỗ cũ, không có thay đổi — thủ công (Task 3).
5. Máy trong DB nằm ngoài 6×4 → giữ nguyên, không bị xóa, không tính là thay đổi — `OutOfFrame_KeptAndNotCounted` (Task 1).

---

### Task 1: `LayoutDraft` + test project

**Files:**
- Create: `Inspection_Control_App/Model/LayoutDraft.cs`
- Modify: `Inspection_Control_App/Model/MachineSlot.cs` (thêm `Clone()`)
- Create: `Inspection_Control_App.Tests/Inspection_Control_App.Tests.csproj` (xUnit, `net8.0-windows`, ProjectReference tới app)
- Create: `Inspection_Control_App.Tests/LayoutDraftTests.cs`

**Interfaces — Produces:**
- `MachineSlot MachineSlot.Clone()` — copy `Id, MachineName, RowNo, ColNo, IndexTerminal`.
- `class LayoutDraft`
  - `const int Rows = 6, Cols = 4`; `static bool InFrame(int row, int col)`
  - `LayoutDraft(IEnumerable<MachineSlot> original)` — giữ bản sao, ảnh chụp gốc theo Id.
  - `IReadOnlyList<MachineSlot> Slots` (gồm cả máy ngoài khung), `IReadOnlyList<int> DeletedIds`
  - `MachineSlot? At(int row, int col)`
  - `bool Move(MachineSlot slot, int row, int col)` — ô trống: chuyển; có máy: đổi chỗ; ngoài khung hoặc cùng ô: `false`.
  - `string? Add(string name, int row, int col)` — `null` = OK, ngược lại là thông báo lỗi tiếng Việt.
  - `void Remove(MachineSlot slot)` — Id > 0 thì ghi vào `DeletedIds`.
  - `string? Rename(MachineSlot slot, string newName)`
  - `int ChangeCount` (xóa + thêm + máy cũ khác vị trí/mã so với gốc), `bool HasChanges`.

- [ ] **Step 1: Viết test** `LayoutDraftTests`:
  - `Move_ToEmptyCell_Moves` (máy (1,1) → (6,4): `At(6,4)` là máy đó, `At(1,1)` null, `ChangeCount == 1`)
  - `Move_ToOccupiedCell_Swaps` (A(1,1), B(2,1) → A ở (2,1), B ở (1,1), `ChangeCount == 2`)
  - `Move_OutsideFrame_ReturnsFalse` (`Move(a, 7, 1) == false`, không đổi)
  - `Move_BackToOriginal_NoChanges` (`HasChanges == false`)
  - `Add_EmptyName_Rejected`, `Add_OccupiedCell_Rejected`, `Add_DuplicateIgnoringCase_Rejected` ("f2tab22010165" khi đã có "F2Tab22010165")
  - `Add_NameOfDeletedMachine_Allowed`
  - `Remove_ExistingMachine_RecordsDeletedId` (`DeletedIds == [id]`, `ChangeCount == 1`); `Remove_NewMachine_NoDeletedId`
  - `Rename_DuplicateIgnoringCase_Rejected`; `Rename_SameNameDifferentCase_Allowed` (chính nó)
  - `OutOfFrame_KeptAndNotCounted` (máy Row 9 vẫn trong `Slots`, `HasChanges == false`)
- [ ] **Step 2:** `dotnet test` → FAIL (chưa có `LayoutDraft`).
- [ ] **Step 3:** Viết `LayoutDraft`, `MachineSlot.Clone()`.
- [ ] **Step 4:** `dotnet test` → tất cả PASS.

### Task 2: Lưu DB + hộp thoại không ghi DB

**Files:**
- Modify: `Inspection_Control_App/SQL/MySQL.cs` — thêm `SaveMachineLayout`, bỏ `UpdateMachineName` và query của nó.
- Modify: `Inspection_Control_App/Views/EditMachineDialog.xaml(.cs)`

**Interfaces:**
- Consumes: `LayoutDraft` (Task 1).
- Produces:
  - `Task<string?> MySQL.SaveMachineLayout(LayoutDraft draft, string updatedBy)` — `null` = OK.
    Một `SqlTransaction`: (1) `DELETE` theo `DeletedIds`; (2) các máy cũ thay đổi: `UPDATE ... SET RowNo = -Id, ColNo = -Id`; (3) `UPDATE` vị trí + mã cuối, `UpdatedBy`, `UpdatedAt = GETDATE()`; (4) `INSERT` máy `Id == 0`. Lỗi → rollback; SqlException 2627/2601 → "Có mã máy hoặc vị trí bị trùng trên DB."
  - `EditMachineDialog(string title, string positionText, string currentName, Func<string, string?> validate)`; `public string NewName { get; }`. Bấm Lưu: `validate(name)` khác null → hiện lỗi; ngược lại `DialogResult = true`.
- [ ] **Step 1:** Viết code; `dotnet build` → 0 lỗi.
- [ ] **Step 2:** (Thủ công, có DB) đổi chỗ 2 máy + thêm + xóa → Lưu → kiểm tra bảng đúng.

### Task 3: `MachineMapControl` — khung 6×4, ô đẹp, chỉnh sửa, kéo thả

**Files:**
- Modify: `Inspection_Control_App/Views/MachineMapControl.xaml(.cs)`
- Create: `Inspection_Control_App/Views/DragGhostAdorner.cs`

**Interfaces:**
- Consumes: `LayoutDraft`, `EditMachineDialog`, `MySQL.SaveMachineLayout`.
- Produces (cho Task 4):
  - `Task LoadLayoutAsync(MySQL sql)` (giữ tên cũ; thoát chế độ sửa)
  - `void Highlight(IEnumerable<CheckStatusModel>? rows, string currentDevice)` (giữ tên cũ; lưu lại để áp dụng lại sau khi vẽ lại)
  - `bool IsEditing`, `bool HasChanges`, `int ChangeCount`, `bool CanEdit` (= có slot và mọi `Id > 0`)
  - `bool BeginEdit()`; `void CancelEdit()`; `Task<string?> SaveAsync(MySQL sql, string updatedBy)` (thành công → tải lại, thoát sửa)
  - `event Action? EditStateChanged` (vào/thoát sửa, số thay đổi đổi)
- [ ] **Step 1:** Khung 6×4 luôn đủ 24 ô; ô trống viền nét đứt (`+` khi đang sửa); ô máy dạng thẻ theo spec (icon, tên đậm, "Bàn X · n PO", trạng thái sáng gradient vàng→cam + glow, "MÁY NÀY" + viền `#007ACC`, ✕ khi sửa); máy ngoài khung → dòng tóm tắt.
- [ ] **Step 2:** Kéo thả: `PreviewMouseLeftButtonDown` ghi điểm; `MouseMove` vượt 6px → `CaptureMouse`, ô gốc `Opacity 0.3`, `DragGhostAdorner` (VisualBrush của ô, Opacity 0.85, bóng đổ) trên AdornerLayer của khung, viền xanh ô đích; `MouseLeftButtonUp` → `draft.Move` nếu trong khung; `Esc` / `LostMouseCapture` / thả ngoài → hủy. Nhả không kéo → hộp thoại đổi mã. Bấm ô trống → hộp thoại thêm. ✕ → xác nhận → `Remove`.
- [ ] **Step 3:** `dotnet build` → 0 lỗi; `dotnet test` vẫn PASS.

### Task 4: Màn Manage + màn chính

**Files:**
- Modify: `Inspection_Control_App/Views/ManageScreen.xaml(.cs)`, `Inspection_Control_App/Views/MainWindow.xaml.cs`

- [ ] **Step 1:** Manage: nút "✏ Chỉnh sửa sơ đồ" (hiện khi MSNV đúng và `CanEdit`), thanh "💾 Lưu · ↺ Hủy · Có n thay đổi chưa lưu" khi `IsEditing`; MSNV sai / đổi MSNV → hỏi nếu có thay đổi rồi `CancelEdit`; `Closing` có thay đổi → Có (lưu) / Không (bỏ) / Hủy (ở lại).
- [ ] **Step 2:** Màn chính: gọi `LoadLayoutAsync` mỗi vòng làm mới 20s trước `Highlight`.
- [ ] **Step 3:** `dotnet build` → 0 lỗi; `dotnet test` PASS.
- [ ] **Step 4:** Commit khi người dùng đồng ý.
