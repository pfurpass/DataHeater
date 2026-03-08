using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataHeater.Helper
{
    /// <summary>
    /// CSV-Implementierung von ITargetDatabase.
    /// Quelle : eine einzelne .csv-Datei  → eine Tabelle
    /// Ziel   : ein Ordner               → je Tabelle eine [name].csv
    ///
    /// Format : RFC 4180
    ///   - erste Zeile = Spaltennamen (Header)
    ///   - jede weitere Zeile = ein Datensatz
    ///   - Trennzeichen: Komma
    ///   - Felder mit Komma / Zeilenumbruch / Anführungszeichen werden in "" eingeschlossen
    ///   - Anführungszeichen innerhalb werden verdoppelt: "He said ""hi"""
    /// </summary>
    internal class CsvDatabase : ITargetDatabase
    {
        private readonly string _path;
        private readonly bool _isFolder;

        public CsvDatabase(string path)
        {
            _path = path;
            _isFolder = !path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        }

        // ── GetTablesAsync ─────────────────────────────────────────────────
        public Task<List<string>> GetTablesAsync()
        {
            var list = new List<string>();

            if (_isFolder)
            {
                if (Directory.Exists(_path))
                    foreach (string f in Directory.GetFiles(_path, "*.csv"))
                        list.Add(Path.GetFileNameWithoutExtension(f));
            }
            else
            {
                if (File.Exists(_path))
                    list.Add(Path.GetFileNameWithoutExtension(_path));
            }

            return Task.FromResult(list);
        }

        // ── GetTableDataAsync ──────────────────────────────────────────────
        public async Task<DataTable> GetTableDataAsync(string tableName)
        {
            string file = ResolveFile(tableName);
            var table = new DataTable(tableName);

            if (!File.Exists(file)) return table;

            using var reader = new StreamReader(file, Encoding.UTF8);

            // --- Header (Zeile 1) -----------------------------------------
            string headerLine = await reader.ReadLineAsync();
            if (headerLine == null) return table;

            foreach (string colName in ParseLine(headerLine))
            {
                string safe = string.IsNullOrWhiteSpace(colName)
                    ? $"col_{table.Columns.Count + 1}"
                    : colName.Trim();

                var col = new DataColumn(safe, typeof(string));
                col.ExtendedProperties["ColumnInfo"] = new ColumnInfo
                {
                    Name = safe,
                    DotNetType = typeof(string),
                    OriginalDbTypeName = "TEXT"
                };
                table.Columns.Add(col);
            }

            // --- Daten (ab Zeile 2) ----------------------------------------
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = ParseLine(line);
                var row = table.NewRow();

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    string val = i < fields.Count ? fields[i] : null;
                    row[i] = val == null ? (object)DBNull.Value : val;
                }

                table.Rows.Add(row);
            }

            return table;
        }

        // ── CreateTableIfNotExistsAsync ────────────────────────────────────
        public Task CreateTableIfNotExistsAsync(DataTable schema, string tableName)
        {
            if (_isFolder)
                Directory.CreateDirectory(_path);
            return Task.CompletedTask;
        }

        // ── TruncateTableAsync ─────────────────────────────────────────────
        public Task TruncateTableAsync(string tableName)
        {
            string file = ResolveFile(tableName);
            if (File.Exists(file)) File.Delete(file);
            return Task.CompletedTask;
        }

        // ── InsertDataAsync ────────────────────────────────────────────────
        public async Task InsertDataAsync(string tableName, DataTable data)
        {
            if (_isFolder)
                Directory.CreateDirectory(_path);

            string file = ResolveFile(tableName);
            bool writeHeader = !File.Exists(file);

            // StreamWriter mit append=true → mehrere Insert-Aufrufe funktionieren korrekt
            await using var writer = new StreamWriter(file,
                append: true, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Header nur einmal schreiben (wenn Datei neu)
            if (writeHeader)
            {
                var headerFields = new List<string>(data.Columns.Count);
                foreach (DataColumn col in data.Columns)
                    headerFields.Add(Escape(col.ColumnName));

                await writer.WriteLineAsync(string.Join(",", headerFields));
            }

            // Eine Zeile pro DataRow
            foreach (DataRow row in data.Rows)
            {
                var fields = new List<string>(data.Columns.Count);

                foreach (DataColumn col in data.Columns)
                {
                    object val = row[col];
                    fields.Add(val == DBNull.Value || val == null
                        ? ""                          // NULL → leeres Feld
                        : Escape(val.ToString()));
                }

                await writer.WriteLineAsync(string.Join(",", fields));
            }
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────

        private string ResolveFile(string tableName) => _isFolder
            ? Path.Combine(_path, tableName + ".csv")
            : _path;

        /// <summary>RFC 4180: Feld in Anführungszeichen wenn nötig.</summary>
        private static string Escape(string value)
        {
            if (value == null) return "";

            bool needsQuotes = value.Contains(',')
                            || value.Contains('"')
                            || value.Contains('\n')
                            || value.Contains('\r');

            if (!needsQuotes) return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Parst eine CSV-Zeile nach RFC 4180.
        /// Behandelt korrekt: quoted fields, doppelte Anführungszeichen, Kommas in Feldern.
        /// </summary>
        private static List<string> ParseLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            int pos = 0;

            while (pos <= line.Length)
            {
                // Letztes leeres Feld nach abschließendem Komma
                if (pos == line.Length)
                {
                    // Komma war das letzte Zeichen → leeres Feld anhängen
                    // (wird durch die Schleifenbedingung korrekt behandelt)
                    break;
                }

                if (line[pos] == '"')
                {
                    // Quoted field
                    pos++; // öffnendes " überspringen
                    var sb = new StringBuilder();

                    while (pos < line.Length)
                    {
                        char c = line[pos];
                        if (c == '"')
                        {
                            pos++;
                            if (pos < line.Length && line[pos] == '"')
                            {
                                // Escaped quote ""
                                sb.Append('"');
                                pos++;
                            }
                            else
                            {
                                // Schließendes "
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(c);
                            pos++;
                        }
                    }

                    result.Add(sb.ToString());

                    // Komma nach dem schließenden " überspringen
                    if (pos < line.Length && line[pos] == ',') pos++;
                }
                else
                {
                    // Unquoted field → bis zum nächsten Komma
                    int start = pos;
                    while (pos < line.Length && line[pos] != ',') pos++;
                    result.Add(line.Substring(start, pos - start));

                    // Komma überspringen
                    if (pos < line.Length) pos++;
                    else if (line.Length > 0 && line[line.Length - 1] == ',')
                    {
                        // Zeile endet mit Komma → letztes leeres Feld
                        result.Add("");
                        break;
                    }
                }
            }

            return result;
        }
    }
}