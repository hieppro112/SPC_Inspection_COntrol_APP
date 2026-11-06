-- =============================================================
-- Bảng cấu hình sơ đồ máy (tablet) cho Inspection Control App
-- Chạy 1 lần trên SSMS. Script chạy lại nhiều lần vẫn an toàn.
--   RowNo : hàng trên sơ đồ (1 = trên cùng)
--   ColNo : cột / cụm trên sơ đồ (1 = trái nhất)
--   MachineName phải trùng với tên máy (Environment.MachineName)
--   mà app ghi vào [KVH].[dbo].[POWip].nameMachine
-- =============================================================
USE [KVH];
GO

IF OBJECT_ID(N'dbo.MachineLayout', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MachineLayout
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MachineLayout PRIMARY KEY,
        MachineName NVARCHAR(100)     NOT NULL,
        RowNo       INT               NOT NULL,
        ColNo       INT               NOT NULL,
        IsActive    BIT               NOT NULL CONSTRAINT DF_MachineLayout_IsActive  DEFAULT (1),
        UpdatedAt   DATETIME          NOT NULL CONSTRAINT DF_MachineLayout_UpdatedAt DEFAULT (GETDATE()),
        UpdatedBy   NVARCHAR(50)      NULL,  -- MSNV quản lý sửa gần nhất (ghi từ màn Manage)
        CONSTRAINT UQ_MachineLayout_MachineName UNIQUE (MachineName),
        CONSTRAINT UQ_MachineLayout_Position    UNIQUE (RowNo, ColNo)
    );
END
GO

-- Bảng đã tạo từ bản script cũ → bổ sung cột UpdatedBy
IF COL_LENGTH(N'dbo.MachineLayout', N'UpdatedBy') IS NULL
    ALTER TABLE dbo.MachineLayout ADD UpdatedBy NVARCHAR(50) NULL;
GO

-- Dữ liệu mẫu 12 máy theo sơ đồ (chỉ thêm khi bảng còn trống)
-- LƯU Ý: 'F2-TABLET999' là mã tạm (ảnh gốc bị trùng F2-TABLET115) -> sửa lại mã đúng.
IF NOT EXISTS (SELECT 1 FROM dbo.MachineLayout)
BEGIN
    INSERT INTO dbo.MachineLayout (MachineName, RowNo, ColNo) VALUES
    -- Cụm 1 (cột trái)
    (N'F2Tab22010165', 1, 1),
    (N'SPC-F2JP005',   2, 1),
    (N'F2TAB22010293', 3, 1),
    (N'F2Tab22010187', 4, 1),
    (N'F2-TABLET999',  5, 1),
    -- Cụm 2 (cột giữa)
    (N'SPC-F2PC22024', 1, 2),
    (N'F2Tab22040014', 2, 2),
    (N'INS2018',       3, 2),
    (N'F2-TABLET115',  4, 2),
    (N'F2TAB18010172', 5, 2),
    -- Cụm 3 (cột phải)
    (N'F2-TABLET83',   4, 3),
    (N'F2_TAB_PR001',  5, 3);
END
GO

-- Có thể đổi mã máy ngay trên app: màn Manage → xác nhận MSNV quản lý → nhấn vào máy.
-- Hoặc sửa tay:
-- UPDATE dbo.MachineLayout SET MachineName = N'<MÃ ĐÚNG>', UpdatedAt = GETDATE()
-- WHERE MachineName = N'F2-TABLET999';
