using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace DataHeater.Helper
{
    /// <summary>
    /// Excel-Implementierung von ITargetDatabase.
    /// Eine .xlsx-Datei = alle Tabellen als separate Sheets (Tabs).
    /// </summary>
    internal class ExcelDatabase : ITargetDatabase
    {
        private readonly string _path;

        public ExcelDatabase(string path) => _path = path;

        /// <summary>Excel sheet names: max 31 chars, no forbidden chars.</summary>
        private static string SafeSheetName(string name)
        {
            // Excel forbids: \ / ? * [ ] :
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[\/?*\[\]:]", "_");
            return name.Length > 31 ? name[..31] : name;
        }

        // ── GetTablesAsync ─────────────────────────────────────────────────
        public Task<List<string>> GetTablesAsync()
        {
            var list = new List<string>();
            if (!File.Exists(_path)) return Task.FromResult(list);
            using var wb = new XLWorkbook(_path);
            foreach (var ws in wb.Worksheets)
                list.Add(ws.Name);
            return Task.FromResult(list);
        }

        // ── GetTableDataAsync ──────────────────────────────────────────────
        public Task<DataTable> GetTableDataAsync(string tableName)
        {
            var table = new DataTable(tableName);
            if (!File.Exists(_path)) return Task.FromResult(table);

            using var wb = new XLWorkbook(_path);
            if (!wb.TryGetWorksheet(SafeSheetName(tableName), out var ws))
                return Task.FromResult(table);

            var used = ws.RangeUsed();
            if (used == null) return Task.FromResult(table);

            var rows = used.RowsUsed().ToList();
            if (rows.Count == 0) return Task.FromResult(table);

            // Erste Zeile = Header
            foreach (var cell in rows[0].CellsUsed())
            {
                string colName = cell.GetString().Trim();
                if (string.IsNullOrEmpty(colName))
                    colName = $"col_{cell.Address.ColumnNumber}";

                var col = new DataColumn(colName, typeof(string));
                col.ExtendedProperties["ColumnInfo"] = new ColumnInfo
                {
                    Name = colName,
                    DotNetType = typeof(string),
                    OriginalDbTypeName = "TEXT"
                };
                table.Columns.Add(col);
            }

            // Datenzeilen
            for (int i = 1; i < rows.Count; i++)
            {
                var row = table.NewRow();
                int col = 0;
                foreach (var cell in rows[i].Cells(1, table.Columns.Count))
                {
                    if (col >= table.Columns.Count) break;
                    string val = cell.GetString();
                    row[col] = string.IsNullOrEmpty(val) ? (object)DBNull.Value : val;
                    col++;
                }
                table.Rows.Add(row);
            }

            return Task.FromResult(table);
        }

        // ── CreateTableIfNotExistsAsync ────────────────────────────────────
        public Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            string dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            return Task.CompletedTask;
        }

        // ── TruncateTableAsync ─────────────────────────────────────────────
        public Task TruncateTableAsync(string tableName)
        {
            if (!File.Exists(_path)) return Task.CompletedTask;
            using var wb = new XLWorkbook(_path);
            if (wb.TryGetWorksheet(SafeSheetName(tableName), out var ws))
            {
                ws.Clear();
                wb.Save();
            }
            return Task.CompletedTask;
        }

        // ── InsertDataAsync ────────────────────────────────────────────────
        public Task InsertDataAsync(string tableName, DataTable data)
        {
            XLWorkbook wb = File.Exists(_path)
                ? new XLWorkbook(_path)
                : new XLWorkbook();

            IXLWorksheet ws;
            bool hasHeader = false;

            if (wb.TryGetWorksheet(SafeSheetName(tableName), out var existing))
            {
                ws = existing;
                hasHeader = ws.LastRowUsed() != null;
            }
            else
            {
                ws = wb.Worksheets.Add(SafeSheetName(tableName));
            }

            if (!hasHeader)
                WriteHeader(ws, data, 1);

            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            int rowIdx = lastRow + 1;

            foreach (DataRow row in data.Rows)
            {
                for (int c = 0; c < data.Columns.Count; c++)
                {
                    object val = row[c];
                    ws.Cell(rowIdx, c + 1).SetValue(
                        val == DBNull.Value || val == null ? "" : val.ToString());
                }
                rowIdx++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(_path);
            wb.Dispose();

            return Task.CompletedTask;
        }

        // ── Header ────────────────────────────────────────────────────────
        private static void WriteHeader(IXLWorksheet ws, DataTable data, int row)
        {
            for (int c = 0; c < data.Columns.Count; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.SetValue(data.Columns[c].ColumnName);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.SteelBlue;
                cell.Style.Font.FontColor = XLColor.White;
            }
        }
    }
}