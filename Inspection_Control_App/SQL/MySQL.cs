using Inspection_Control_App.Model;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

namespace Inspection_Control_App.SQL
{
    public class MySQL
    {
        private static string connectionString { get; } = "Data Source=192.168.122.2;Initial Catalog=MANUFASPCPD;User ID=admin;Password=Buitanphat0201@";
        //dl nhan vien
        private static string connection_Employee { get; } = @"Data Source=192.168.0.11;Initial Catalog=SGPrecision;User ID=hrmsadmin;Password=adminhrms;Max Pool Size=50;Application Name=Molybden_DL;";
        private static string query_id_get_name { get; } = "select [Position(E)] from DataSPC where [Code] = @idNV";

        //private static string query_insertPO { get; } = "Insert MANUFA_F2_PO_WIP(PO_Number,NumInspec,nameMachine) values (@PO_Add,@Num,@machine)";
        private static string query_insertPO { get; } = "IF NOT EXISTS (\r\n    SELECT 1 \r\n    FROM MANUFA_F2_PO_WIP \r\n    WHERE PO_Number = @PO_Add AND nameMachine = @machine\r\n)\r\nBEGIN\r\n    INSERT INTO MANUFA_F2_PO_WIP (PO_Number, NumInspec, Saved_At, nameMachine)\r\n    VALUES (@PO_Add, @Num, GETDATE(), @machine);\r\nEND";
        private static string query_data_table_WIP { get; } = "SELECT \r\n    hed.AUFNR AS POREQNO,\r\n    hed.PHTX AS ITEMCD,\r\n    hed.PHCD AS PITEMID,\r\n    hed.KDAUF,\r\n    hed.GAMNG,\r\n    prd.FERTH,\r\n    dtl.EDATU AS EXPORTD,\r\n    dtl.KWMENG AS TTL_LOT,\r\n    dtl.RRONYU1 as SHIPTO,\r\n    f.TRANSPORT as SHIPBY,\r\n    hed.PSTX,\r\n\r\n    -- 1. LẤY THÊM CỘT THỜI GIAN VÀO ĐÂY\r\n    POWIP.Saved_At AS SAVED_TIME,\r\n\r\n    CASE \r\n        WHEN dtl.RRONYU1 IN ('KJS', 'KJK', 'KJS_TK', 'ERC', 'WRC', 'CRC', 'FJS')\r\n             AND SUBSTRING(dtl.ZGLOBAL_CODE, 7, 1) = '-'\r\n        THEN SUBSTRING(dtl.ZGLOBAL_CODE, 8, LEN(dtl.ZGLOBAL_CODE))\r\n        ELSE dtl.ZGLOBAL_CODE\r\n    END AS CUSTNO,\r\n\r\n    CASE \r\n        WHEN f.product_code <> '' THEN f.product_code\r\n        WHEN LEFT(dtl.RONAME, 6) = '[SBS]_' THEN SUBSTRING(dtl.RONAME, 7, LEN(dtl.RONAME))\r\n        WHEN LEFT(dtl.RONAME, 5) = '[HF]_' THEN SUBSTRING(dtl.RONAME, 6, LEN(dtl.RONAME))\r\n        WHEN LEFT(hed.PHTX, 6) = '[SBS]_' THEN SUBSTRING(hed.PHTX, 7, LEN(hed.PHTX))\r\n        WHEN LEFT(hed.PHTX, 5) = '[HF]_' THEN SUBSTRING(hed.PHTX, 6, LEN(hed.PHTX))\r\n        ELSE ISNULL(dtl.RONAME, hed.PHTX)\r\n    END AS RONAME\r\n\r\nFROM MANUFA_F_PD_DT_REQ_HED hed\r\n\r\nLEFT JOIN MANUFA_F_PD_GRB_PRODUCT prd \r\n    ON hed.PHCD = prd.MATNR\r\n\r\nLEFT JOIN MANUFA_F_PD_DT_ORDER_DTL dtl \r\n    ON dtl.VBELN = hed.KDAUF\r\n\r\n-- 2. SỬA ĐIỀU KIỆN JOIN THÀNH GHÉP THEO MÃ PO (HOẶC KDAUF/AUFNR)\r\nLEFT JOIN MANUFA_F2_PO_WIP POWIP\r\n    ON hed.AUFNR = POWIP.PO_Number   -- Hoặc dtl.VBELN / hed.KDAUF tùy thuộc cột nào lưu Mã PO trong POWIP\r\n\r\nOUTER APPLY\r\n(\r\n    SELECT TOP 1\r\n        w.product_code, TRANSPORT\r\n    FROM MANUFA_F_PD_W_ORDER w\r\n    WHERE w.mpo = dtl.ZGLOBAL_CODE\r\n    ORDER BY w.ID DESC\r\n) f\r\n\r\nWHERE hed.AUFNR = @PO_num\r\n  AND (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL)\r\n  AND (dtl.ABGRU IS NULL OR dtl.ABGRU NOT LIKE 'Z1');";
        //private static string query_Get_ALL_table_WIP { get; } = "SELECT \r\n    hed.AUFNR AS POREQNO,\r\n    hed.PHTX AS ITEMCD,\r\n    hed.PHCD AS PITEMID,\r\n    hed.KDAUF,\r\n    hed.GAMNG,\r\n    prd.FERTH,\r\n    dtl.EDATU AS EXPORTD,\r\n    dtl.KWMENG AS TTL_LOT,\r\n    dtl.RRONYU1 AS SHIPTO,\r\n    f.TRANSPORT AS SHIPBY,\r\n    hed.PSTX,\r\n\r\n    POWIP.Saved_At AS SAVED_TIME,\r\n\r\n    CASE \r\n        WHEN dtl.RRONYU1 IN ('KJS', 'KJK', 'KJS_TK', 'ERC', 'WRC', 'CRC', 'FJS')\r\n             AND SUBSTRING(dtl.ZGLOBAL_CODE, 7, 1) = '-'\r\n        THEN SUBSTRING(dtl.ZGLOBAL_CODE, 8, LEN(dtl.ZGLOBAL_CODE))\r\n        ELSE dtl.ZGLOBAL_CODE\r\n    END AS CUSTNO,\r\n\r\n    CASE \r\n        WHEN f.product_code <> '' THEN f.product_code\r\n        WHEN LEFT(dtl.RONAME, 6) = '[SBS]_' THEN SUBSTRING(dtl.RONAME, 7, LEN(dtl.RONAME))\r\n        WHEN LEFT(dtl.RONAME, 5) = '[HF]_' THEN SUBSTRING(dtl.RONAME, 6, LEN(dtl.RONAME))\r\n        WHEN LEFT(hed.PHTX, 6) = '[SBS]_' THEN SUBSTRING(hed.PHTX, 7, LEN(hed.PHTX))\r\n        WHEN LEFT(hed.PHTX, 5) = '[HF]_' THEN SUBSTRING(hed.PHTX, 6, LEN(hed.PHTX))\r\n        ELSE ISNULL(dtl.RONAME, hed.PHTX)\r\n    END AS RONAME\r\n\r\nFROM MANUFA_F_PD_DT_REQ_HED hed\r\n\r\nLEFT JOIN MANUFA_F_PD_GRB_PRODUCT prd \r\n    ON hed.PHCD = prd.MATNR\r\n\r\nLEFT JOIN MANUFA_F_PD_DT_ORDER_DTL dtl \r\n    ON dtl.VBELN = hed.KDAUF\r\n\r\n-- CHUYỂN THÀNH INNER JOIN\r\nINNER JOIN MANUFA_F2_PO_WIP POWIP\r\n    ON hed.AUFNR = POWIP.PO_Number\r\n\r\nOUTER APPLY\r\n(\r\n    SELECT TOP 1\r\n        w.product_code, TRANSPORT\r\n    FROM MANUFA_F_PD_W_ORDER w\r\n    WHERE w.mpo = dtl.ZGLOBAL_CODE\r\n    ORDER BY w.ID DESC\r\n) f\r\n\r\nWHERE (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL)\r\n  AND (dtl.ABGRU IS NULL OR dtl.ABGRU NOT LIKE 'Z1');";
        private static string query_Get_ALL_table_WIP2 { get; } = @"
SELECT 
    hed.AUFNR AS POREQNO,
    hed.PHTX AS ITEMCD,
    hed.PHCD AS PITEMID,
    hed.KDAUF,
    hed.GAMNG,
    prd.FERTH,
    dtl.EDATU AS EXPORTD,
    dtl.KWMENG AS TTL_LOT,
    dtl.RRONYU1 AS SHIPTO,
    f.TRANSPORT AS SHIPBY,
    hed.PSTX,

    POWIP.Saved_At AS SAVED_TIME,

    CASE 
        WHEN dtl.RRONYU1 IN ('KJS', 'KJK', 'KJS_TK', 'ERC', 'WRC', 'CRC', 'FJS')
             AND SUBSTRING(dtl.ZGLOBAL_CODE, 7, 1) = '-'
        THEN SUBSTRING(dtl.ZGLOBAL_CODE, 8, LEN(dtl.ZGLOBAL_CODE))
        ELSE dtl.ZGLOBAL_CODE
    END AS CUSTNO,

    CASE 
        WHEN f.product_code <> '' THEN f.product_code
        WHEN LEFT(dtl.RONAME, 6) = '[SBS]_' THEN SUBSTRING(dtl.RONAME, 7, LEN(dtl.RONAME))
        WHEN LEFT(dtl.RONAME, 5) = '[HF]_' THEN SUBSTRING(dtl.RONAME, 6, LEN(dtl.RONAME))
        WHEN LEFT(hed.PHTX, 6) = '[SBS]_' THEN SUBSTRING(hed.PHTX, 7, LEN(hed.PHTX))
        WHEN LEFT(hed.PHTX, 5) = '[HF]_' THEN SUBSTRING(hed.PHTX, 6, LEN(hed.PHTX))
        ELSE ISNULL(dtl.RONAME, hed.PHTX)
    END AS RONAME

FROM MANUFA_F_PD_DT_REQ_HED hed

LEFT JOIN MANUFA_F_PD_GRB_PRODUCT prd 
    ON hed.PHCD = prd.MATNR

LEFT JOIN MANUFA_F_PD_DT_ORDER_DTL dtl 
    ON dtl.VBELN = hed.KDAUF

-- CHUYỂN THÀNH INNER JOIN
INNER JOIN MANUFA_F2_PO_WIP POWIP
    ON hed.AUFNR = POWIP.PO_Number

OUTER APPLY
(
    SELECT TOP 1
        w.product_code, TRANSPORT
    FROM MANUFA_F_PD_W_ORDER w
    WHERE w.mpo = dtl.ZGLOBAL_CODE
    ORDER BY w.ID DESC
) f

WHERE (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL)
  AND (dtl.ABGRU IS NULL OR dtl.ABGRU NOT LIKE 'Z1')

-- SẮP XẾP THỜI GIAN LƯU TỪ MỚI NHẤT ĐẾN CŨ NHẤT
ORDER BY POWIP.Saved_At DESC;";
        //        private static string query_Get_ALL_table_WIP { get; } = @"
        //SELECT 
        //    hed.AUFNR AS POREQNO,
        //    hed.PHTX AS ITEMCD,
        //    hed.PHCD AS PITEMID,
        //    hed.KDAUF,
        //    hed.GAMNG,
        //    prd.FERTH,
        //    dtl.EDATU AS EXPORTD,
        //    dtl.KWMENG AS TTL_LOT,
        //    dtl.RRONYU1 AS SHIPTO,
        //    f.TRANSPORT AS SHIPBY,
        //    hed.PSTX,

        //    POWIP.Saved_At AS SAVED_TIME,

        //    CASE 
        //        WHEN dtl.RRONYU1 IN ('KJS', 'KJK', 'KJS_TK', 'ERC', 'WRC', 'CRC', 'FJS')
        //             AND SUBSTRING(dtl.ZGLOBAL_CODE, 7, 1) = '-'
        //        THEN SUBSTRING(dtl.ZGLOBAL_CODE, 8, LEN(dtl.ZGLOBAL_CODE))
        //        ELSE dtl.ZGLOBAL_CODE
        //    END AS CUSTNO,

        //    CASE 
        //        WHEN f.product_code <> '' THEN f.product_code
        //        WHEN LEFT(dtl.RONAME, 6) = '[SBS]_' THEN SUBSTRING(dtl.RONAME, 7, LEN(dtl.RONAME))
        //        WHEN LEFT(dtl.RONAME, 5) = '[HF]_' THEN SUBSTRING(dtl.RONAME, 6, LEN(dtl.RONAME))
        //        WHEN LEFT(hed.PHTX, 6) = '[SBS]_' THEN SUBSTRING(hed.PHTX, 7, LEN(hed.PHTX))
        //        WHEN LEFT(hed.PHTX, 5) = '[HF]_' THEN SUBSTRING(hed.PHTX, 6, LEN(hed.PHTX))
        //        ELSE ISNULL(dtl.RONAME, hed.PHTX)
        //    END AS RONAME

        //FROM MANUFA_F_PD_DT_REQ_HED hed

        //LEFT JOIN MANUFA_F_PD_GRB_PRODUCT prd 
        //    ON hed.PHCD = prd.MATNR

        //LEFT JOIN MANUFA_F_PD_DT_ORDER_DTL dtl 
        //    ON dtl.VBELN = hed.KDAUF

        //-- CHUYỂN THÀNH INNER JOIN
        //INNER JOIN MANUFA_F2_PO_WIP POWIP
        //    ON hed.AUFNR = POWIP.PO_Number

        //OUTER APPLY
        //(
        //    SELECT TOP 1
        //        w.product_code, TRANSPORT
        //    FROM MANUFA_F_PD_W_ORDER w
        //    WHERE w.mpo = dtl.ZGLOBAL_CODE
        //    ORDER BY w.ID DESC
        //) f

        //WHERE (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL)
        //  AND (dtl.ABGRU IS NULL OR dtl.ABGRU NOT LIKE 'Z1')
        //  -- ĐIỀU KIỆN LỌC THEO TÊN THIẾT BỊ / MÁY
        //  AND POWIP.nameMachine = @machine

        //-- SẮP XẾP TỪ MỚI ĐẾN CŨ
        //ORDER BY POWIP.Saved_At DESC;";
        private static string query_Get_ALL_table_WIP { get; } = @"
SELECT 
    hed.AUFNR AS POREQNO,
    hed.PHTX AS ITEMCD,
    hed.PHCD AS PITEMID,
    hed.KDAUF,
    hed.GAMNG,
    prd.FERTH,
    dtl.EDATU AS EXPORTD,
    dtl.KWMENG AS TTL_LOT,
    dtl.RRONYU1 AS SHIPTO,
    f.TRANSPORT AS SHIPBY,
    hed.PSTX,

    POWIP.Saved_At AS SAVED_TIME,

    CASE 
        WHEN dtl.RRONYU1 IN ('KJS', 'KJK', 'KJS_TK', 'ERC', 'WRC', 'CRC', 'FJS')
             AND SUBSTRING(dtl.ZGLOBAL_CODE, 7, 1) = '-'
        THEN SUBSTRING(dtl.ZGLOBAL_CODE, 8, LEN(dtl.ZGLOBAL_CODE))
        ELSE dtl.ZGLOBAL_CODE
    END AS CUSTNO,

    CASE 
        WHEN f.product_code <> '' THEN f.product_code
        WHEN LEFT(dtl.RONAME, 6) = '[SBS]_' THEN SUBSTRING(dtl.RONAME, 7, LEN(dtl.RONAME))
        WHEN LEFT(dtl.RONAME, 5) = '[HF]_' THEN SUBSTRING(dtl.RONAME, 6, LEN(dtl.RONAME))
        WHEN LEFT(hed.PHTX, 6) = '[SBS]_' THEN SUBSTRING(hed.PHTX, 7, LEN(hed.PHTX))
        WHEN LEFT(hed.PHTX, 5) = '[HF]_' THEN SUBSTRING(hed.PHTX, 6, LEN(hed.PHTX))
        ELSE ISNULL(dtl.RONAME, hed.PHTX)
    END AS RONAME

FROM MANUFA_F_PD_DT_REQ_HED hed

LEFT JOIN MANUFA_F_PD_GRB_PRODUCT prd 
    ON hed.PHCD = prd.MATNR

LEFT JOIN MANUFA_F_PD_DT_ORDER_DTL dtl 
    ON dtl.VBELN = hed.KDAUF

-- CHUYỂN THÀNH INNER JOIN
INNER JOIN MANUFA_F2_PO_WIP POWIP
    ON hed.AUFNR = POWIP.PO_Number

OUTER APPLY
(
    SELECT TOP 1
        w.product_code, TRANSPORT
    FROM MANUFA_F_PD_W_ORDER w
    WHERE w.mpo = dtl.ZGLOBAL_CODE
    ORDER BY w.ID DESC
) f

WHERE (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL)
  AND (dtl.ABGRU IS NULL OR dtl.ABGRU NOT LIKE 'Z1')
  -- ĐIỀU KIỆN LỌC THEO TÊN THIẾT BỊ / MÁY
  AND POWIP.nameMachine = @machine
  And POWIP.Saved_At >= @date

-- SẮP XẾP TỪ MỚI ĐẾN CŨ
ORDER BY POWIP.Saved_At DESC;";
        //lay thheo type va tim so ban 
        //private static string qury_get_PO_type { get; } = "SELECT DISTINCT\r\npw.PO_Number,\r\npw.NumInspec,\r\nhed.PSTX,\r\npw.Saved_At,\r\nLC.index_terminal\r\nFROM MANUFA_F2_PO_WIP pw\r\nINNER JOIN MANUFA_F_PD_DT_REQ_HED hed \r\nON pw.PO_Number = hed.AUFNR\r\nINNER JOIN [F2Database].[dbo].[F2_Inspection_H_location] LC \r\nON pw.nameMachine = LC.Machine\r\nWHERE hed.PSTX = (\r\n-- Lấy mã PSTX của PO truyền vào (@PO_num)\r\nSELECT TOP 1 PSTX \r\nFROM MANUFA_F_PD_DT_REQ_HED\r\nWHERE AUFNR = @PO_search\r\n)\r\nAND (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL);\r\n";
        //private static string qury_get_PO_type { get; } = "" +
        //    "SELECT DISTINCT\r\npw.PO_Number,\r\npw.NumInspec,\r\nhed.PSTX,\r\nhed.PHTX,\r\npw.Saved_At,\r\nLC.index_terminal\r\nFROM MANUFA_F2_PO_WIP pw\r\nINNER JOIN MANUFA_F_PD_DT_REQ_HED hed \r\nON pw.PO_Number = hed.AUFNR\r\nINNER JOIN [F2Database].[dbo].[F2_Inspection_H_location] LC \r\nON pw.nameMachine = LC.Machine\r\nWHERE hed.PHTX = (\r\nSELECT TOP 1 PHTX \r\nFROM MANUFA_F_PD_DT_REQ_HED\r\nWHERE AUFNR = @PO_search\r\n)\r\nAND (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL);";
        private static string qury_get_PO_type { get; } = "" +
    "SELECT DISTINCT\r\npw.PO_Number,\r\npw.NumInspec,\r\nhed.PSTX,\r\nhed.PHTX,\r\npw.Saved_At,\r\nLC.index_terminal\r\nFROM MANUFA_F2_PO_WIP pw\r\nINNER JOIN MANUFA_F_PD_DT_REQ_HED hed \r\nON pw.PO_Number = hed.AUFNR\r\nINNER JOIN [F2Database].[dbo].[F2_Inspection_H_location] LC \r\nON pw.nameMachine = LC.Machine\r\nWHERE hed.PHTX = (\r\nSELECT TOP 1 PHTX \r\nFROM MANUFA_F_PD_DT_REQ_HED\r\nWHERE AUFNR = @PO_search\r\n)\r\nAND pw.Saved_At >= @date\r\nAND (hed.LOEKZ <> 'X' OR hed.LOEKZ IS NULL);";
        private static string query_check_PO { get; } = "select [PO_Check] FROM [MANUFASPCPD].[dbo].[MANUFA_F2_Users]";
        private static string query_Get_ins { get; } = "select top(1) * from [MANUFASPCPD].[dbo].[MANUFA_F2_Users] where nameMachine = @machine order by dateCreated desc ";


        public async Task<bool> InsertPOWIP(string po, string ins, string machine)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query_insertPO, connection);


                    command.Parameters.AddWithValue("@PO_Add", po);
                    command.Parameters.AddWithValue("@Num", ins);
                    command.Parameters.AddWithValue("@machine", machine);

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }

                catch (Exception ex)
                {
                    Debug.WriteLine("loi khi them du lieu: " + ex);
                    return false;
                }
            }

        }

        public POModel GetPOModel(string po)
        {
            using (SqlConnection connect = new SqlConnection(connectionString))
            {
                try
                {
                    connect.Open();
                    SqlCommand cmd = new SqlCommand(query_data_table_WIP, connect);
                    cmd.Parameters.AddWithValue("PO_num", po.ToString().Trim());
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            float.TryParse(reader["GAMNG"].ToString(), out float sluong);
                            DateTime.TryParse(reader["SAVED_TIME"].ToString(), out DateTime date);
                            int.TryParse(reader["TTL_LOT"].ToString(), out int NumLot);
                            int.TryParse(reader["GAMNG"].ToString(), out int Qty);

                            return new POModel
                            {
                                Time = date,
                                PONumber = reader["POREQNO"].ToString(),
                                Custno = reader["CUSTNO"].ToString(),
                                ShipTo = reader["SHIPTO"].ToString(),
                                ShipBy = reader["SHIPBY"].ToString(),
                                Roname = reader["RONAME"].ToString(),
                                Qty = Qty,
                                ExportDate = reader["EXPORTD"].ToString()
                            };
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Xay ra loi trong qua trinh get data: " + ex.Message);
                    return null;
                }
            }
        }
        public async Task<MyUserControl> GetPOModel_CHECK(string machine)
        {
            using (SqlConnection connect = new SqlConnection(connectionString))
            {
                try
                {
                    await connect.OpenAsync();
                    SqlCommand cmd = new SqlCommand(query_Get_ins, connect);
                    cmd.Parameters.AddWithValue("@machine", machine.ToString().Trim());
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            DateTime.TryParse(reader["dateCreated"].ToString() ?? "", out DateTime date);

                            return new MyUserControl
                            {
                                Ins_Key = reader["Ins_Key"].ToString()??"",
                                PO_Check = reader["PO_Check"].ToString() ?? "",
                                dateCreated = date,
                                nameMachine = reader["nameMachine"].ToString() ?? "",
                            };
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Xay ra loi trong qua trinh get data: " + ex.Message);
                    return null;
                }
            }
        }
        public List<POModel> GetListPOModel(string po)
        {
            List<POModel> listPO = new List<POModel>();

            using (SqlConnection connect = new SqlConnection(connectionString))
            {
                try
                {
                    connect.Open();
                    SqlCommand cmd = new SqlCommand(query_data_table_WIP, connect);

                    // Nếu câu truy vấn của bạn có tham số @PO_num
                    // Trường hợp po null hoặc rỗng thì truyền DBNull.Value (để lấy tất cả nếu query hỗ trợ)
                    if (!string.IsNullOrEmpty(po))
                    {
                        cmd.Parameters.AddWithValue("@PO_num", po.Trim());
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PO_num", DBNull.Value);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Dùng vòng lặp WHILE để đọc tất cả bản ghi trả về
                        while (reader.Read())
                        {
                            DateTime.TryParse(reader["SAVED_TIME"]?.ToString(), out DateTime date);
                            int.TryParse(reader["TTL_LOT"]?.ToString(), out int numLot);
                            int.TryParse(reader["GAMNG"]?.ToString(), out int qty);

                            POModel item = new POModel
                            {
                                Time = date,
                                PONumber = reader["POREQNO"]?.ToString() ?? "",
                                Custno = reader["CUSTNO"].ToString() ?? "",
                                ShipTo = reader["SHIPTO"]?.ToString() ?? "",
                                ShipBy = reader["SHIPBY"]?.ToString() ?? "",
                                Roname = reader["RONAME"]?.ToString() ?? "",
                                Qty = qty,
                                ExportDate = reader["EXPORTD"]?.ToString() ?? ""
                            };

                            listPO.Add(item); // Thêm từng item vào List
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Xay ra loi trong qua trinh get list data: " + ex.Message);
                }
            }

            return listPO; // Trả về danh sách (nếu lỗi hoặc không có dữ liệu sẽ trả về List rỗng)
        }

        public async Task<List<POModel>> GetAllPOList(string machine, DateTime dateSearch)
        {
            List<POModel> listPO = new List<POModel>();
            using (SqlConnection connect = new SqlConnection(connectionString))
            {
                try
                {
                    connect.Open();
                    // Không cần thêm cmd.Parameters nữa
                    SqlCommand cmd = new SqlCommand(query_Get_ALL_table_WIP, connect);
                    cmd.Parameters.AddWithValue("@machine", machine);
                    cmd.Parameters.AddWithValue("@date", dateSearch);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            DateTime.TryParse(reader["SAVED_TIME"]?.ToString(), out DateTime date);
                            int.TryParse(reader["TTL_LOT"]?.ToString(), out int numLot);
                            int.TryParse(reader["GAMNG"]?.ToString(), out int qty);

                            listPO.Add(new POModel
                            {
                                Time = date,
                                PONumber = reader["POREQNO"]?.ToString() ?? "",
                                Custno = reader["CUSTNO"]?.ToString() ?? "",
                                ShipTo = reader["SHIPTO"]?.ToString() ?? "",
                                ShipBy = reader["SHIPBY"]?.ToString() ?? "",
                                Roname = reader["RONAME"]?.ToString() ?? "",
                                Qty = qty,
                                ExportDate = reader["EXPORTD"]?.ToString() ?? ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi khi lấy danh sách PO: " + ex.Message);
                }
            }

            return listPO;
        }

        public async Task<List<CheckStatusModel>> GetPOByPSTX(string poNum,DateTime date)
        {
            List<CheckStatusModel> result = new List<CheckStatusModel>();

            using (SqlConnection connect = new SqlConnection(connectionString))
            {
                try
                {
                    await connect.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(qury_get_PO_type, connect))
                    {
                        cmd.Parameters.AddWithValue("@PO_search", poNum.Trim());
                        cmd.Parameters.AddWithValue("@date", date);


                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DateTime.TryParse(reader["Saved_At"]?.ToString(), out DateTime savedDate);
                                string valuePO = reader["PO_Number"]?.ToString() ?? "";
                                result.Add(new CheckStatusModel
                                {
                                    PONumber = valuePO,
                                    NumInspec = reader["NumInspec"]?.ToString() ?? "",
                                    Typename = reader["PSTX"]?.ToString() ?? "",
                                    Status = await CheckPO(valuePO)?StatusCheck.Checking:StatusCheck.Normal,
                                    SavedAt = savedDate,
                                    Roname = reader["PHTX"]?.ToString() ?? "",
                                    Index = reader["index_terminal"]?.ToString() ?? "",
                                });
                            }
                        }
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi get data WIP: " + ex.Message);
                    return [];
                }
            }
        }

        public async Task<List<string>> Get_list_checking()
        {
            List<string> listPO = new List<string>();
            using (SqlConnection connect = new SqlConnection(connectionString))
            {
                try
                {
                    await connect.OpenAsync();
                    // Không cần thêm cmd.Parameters nữa
                    using (SqlCommand cmd = new SqlCommand(query_check_PO, connect))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            listPO.Add(reader["PO_Check"]?.ToString() ?? "");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi khi lấy danh sách PO: " + ex.Message);
                }
            }

            return listPO;
        }

        //check stt PO 
        public async Task<bool> CheckPO(string po)
        {
            var list = await Get_list_checking();
            foreach (var item in list)
            {
                if (item == po)
                {
                    return true;
                }
            }
            return false;
        }


        public async Task<bool> GetEmployee(string id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connection_Employee))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query_id_get_name, conn))
                    {
                        cmd.Parameters.AddWithValue("@idNV", id);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var check = reader["Position(E)"].ToString() ?? "";
                                return !string.IsNullOrWhiteSpace(check) && check.ToString().Trim() != ("Worker");
                            }
                        }
                    }
                }

                return false;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

    }
}
