# Sơ đồ máy: kéo thả, thêm, xóa — thiết kế

Ngày: 2026-11-17 · Nhánh: `feature/machine-map`

## Mục đích

Người quản lý tự sắp xếp sơ đồ máy (tablet) ngay trên app thay vì sửa bảng
`[KVH].[dbo].[MachineLayout]` trên DB: kéo thả máy sang vị trí khác, thêm máy,
xóa máy, đổi mã máy. Màn chính vẫn chỉ xem + sáng đèn khi máy giữ PO cùng type.

## Mục tiêu / Không làm / Thất bại nếu

- **Mục tiêu:** khung 6×4 ô; sau khi xác nhận MSNV quản lý ở màn Manage thì
  kéo thả, thêm, xóa, đổi mã được; ô máy đẹp hơn.
- **Không làm:** đổi kích thước khung trên app; chỉnh sơ đồ ở màn chính;
  phân quyền chi tiết hơn "MSNV không phải Worker" (giữ `GetEmployee` hiện có).
- **Thất bại nếu:** kéo nhầm mà không quay lại được; lưu dở dang làm hai máy
  trùng ô / mất máy; màn chính không sáng đèn đúng như trước.

## Ràng buộc

- WPF .NET 8, `System.Data.SqlClient`, không thêm thư viện.
- Bảng `MachineLayout` có UNIQUE `(RowNo, ColNo)` và UNIQUE `MachineName`.
- Chạy trên tablet Windows: phải dùng được bằng ngón tay (WPF chuyển touch → mouse).
- Khi chưa có bảng trên DB, app dùng sơ đồ mặc định → **không cho chỉnh sửa**.

## Cách làm (đã chọn: Cách 2)

| Cách | Mô tả | Đánh đổi |
|---|---|---|
| 1. DragDrop có sẵn của WPF | `DragDrop.DoDragDrop`, ô đích sáng khi rê qua | Ít code, ổn định; không có hình máy bay theo tay |
| **2. Tự viết kéo thả + hình bóng (chọn)** | Bắt chuột, vẽ hình mờ của ô máy chạy theo con trỏ bằng Adorner | Đẹp, trực quan nhất; code nhiều hơn, phải tự xử lý hủy kéo / mất capture |
| 3. Chọn rồi bấm | Bấm máy, bấm ô đích | Đơn giản, hợp touch; không phải kéo thả |

Người dùng chọn **Cách 2**.

## Thiết kế

### Khung
- Cố định **6 hàng × 4 cột** (hằng số trong `MachineMapControl`). Luôn vẽ đủ 24 ô.
- Ô trống: viền nét đứt mờ. Ở chế độ chỉnh sửa ô trống hiện dấu **＋**.
- Máy trong DB có vị trí ngoài 6×4: không vẽ, liệt kê ở dòng tóm tắt
  ("Máy ngoài khung: …") để quản lý biết mà sửa.

### Ô máy (làm đẹp)
```
┌──────────────────────────┐        ┌──────────────────────────┐
│ ▣  F2Tab22010165       ✕ │  sáng  │ ▣  F2-TABLET115     ● 2  │  ← nền vàng/cam gradient,
│    Bàn 3 · 2 PO          │  ───▶  │    Bàn 7 · 2 PO          │    phát sáng nhẹ
└──────────────────────────┘        └──────────────────────────┘
```
- Thẻ bo góc 8px, nền tối có gradient nhẹ, icon tablet bên trái, tên máy đậm,
  dòng phụ "Bàn X · n PO".
- **Sáng (giữ cùng type):** gradient vàng → cam, chữ tối, glow; badge số PO.
- **Máy này:** viền xanh `#007ACC` 2px + nhãn nhỏ "MÁY NÀY".
- **Chế độ chỉnh sửa:** nút ✕ ở góc phải trên, con trỏ hình bàn tay.

### Chế độ chỉnh sửa (màn Manage)
- Xác nhận MSNV quản lý đúng → hiện nút **✏ Chỉnh sửa sơ đồ**.
- Bấm vào → thanh công cụ: **💾 Lưu** · **↺ Hủy** · nhãn "Có n thay đổi chưa lưu".
- Mọi thao tác chỉ đổi trên **bản nháp trong bộ nhớ**; chỉ ghi DB khi bấm Lưu.
- Hủy → tải lại từ DB, thoát chế độ chỉnh sửa.
- Đóng cửa sổ khi còn thay đổi chưa lưu → hỏi "Lưu / Không lưu / Ở lại".
- Đổi MSNV hoặc MSNV xác nhận sai → thoát chế độ chỉnh sửa (hỏi nếu có thay đổi).

### Kéo thả (Cách 2)
- Nhấn giữ trên ô máy, di quá ~6px mới bắt đầu kéo (để còn phân biệt với bấm).
- Khi kéo: ô gốc mờ 30%, hình bóng ô máy (bán trong suốt, hơi nghiêng/đổ bóng)
  chạy theo con trỏ; ô đích dưới con trỏ viền xanh sáng.
- Thả vào ô trống → máy chuyển sang. Thả vào ô có máy → **đổi chỗ** hai máy.
- Thả ra ngoài khung, nhấn **Esc**, hoặc mất capture chuột → hủy, máy về chỗ cũ.
- Nhấn rồi nhả không di chuyển → mở hộp thoại **đổi mã máy**.

### Thêm / xóa / đổi mã
- **Thêm:** bấm ô trống (＋) → hộp thoại nhập mã máy (có nút "Dùng máy này").
- **Xóa:** bấm ✕ → hỏi xác nhận → bỏ khỏi bản nháp.
- **Đổi mã:** như trên. Hộp thoại chỉ kiểm tra rỗng / trùng mã trong bản nháp,
  **không ghi DB nữa** (sửa `EditMachineDialog` thành trả về mã mới).

### Lưu vào DB
`MySQL.SaveMachineLayout(slots, deletedIds, updatedBy)` — **một transaction**:
1. `DELETE` các Id đã xóa.
2. Các máy cũ đổi vị trí/mã: dời tạm sang vị trí âm (`RowNo = -Id`) để tránh
   đụng UNIQUE khi đổi chỗ, rồi `UPDATE` vị trí + mã cuối cùng, `UpdatedBy`, `UpdatedAt`.
3. `INSERT` máy mới.
Lỗi → rollback toàn bộ, báo lỗi (trùng mã → thông báo dễ hiểu), giữ nguyên bản nháp.

### Màn chính
- Chỉ xem. Tải lại sơ đồ trong vòng làm mới 20 giây hiện có, để thay đổi từ
  màn Manage hiện ra mà không cần mở lại app.

## Để người làm tự quyết
- Màu/độ glow cụ thể, kích thước icon, animation khi đổi chỗ.
- Cách tính ô dưới con trỏ (theo kích thước hàng/cột của Grid).
- Đặt Adorner ở AdornerLayer của khung hay của cửa sổ.
