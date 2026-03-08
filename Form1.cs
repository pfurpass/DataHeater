using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataHeater.Helper;

namespace DataHeater
{
    public partial class Form1 : Form
    {
        private readonly List<DbTarget> _sources = new();
        private readonly List<DbTarget> _targets = new();
        private int _editSrcIdx = -1;
        private int _editTgtIdx = -1;
        private bool _isEnglish = true;

        private class TableEntry
        {
            public string TableName { get; set; }
            public int SourceIndex { get; set; }
            private readonly List<DbTarget> _src;
            public TableEntry(List<DbTarget> src) => _src = src;
            public override string ToString() => $"[{TableName}]  <-  {_src[SourceIndex]}";
        }

        public Form1()
        {
            InitializeComponent();

            pnlSrcSqlite.Visible = true;
            pnlSrcDb.Visible = false;
            pnlTgtSqlite.Visible = false;
            pnlTgtDb.Visible = true;
            txtTgtPort.Text = "3306";

            listTables.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Space) return;
                bool allChecked = listTables.SelectedIndices.Cast<int>()
                    .All(i => listTables.GetItemChecked(i));
                foreach (int i in listTables.SelectedIndices)
                    listTables.SetItemChecked(i, !allChecked);
                e.Handled = true;
            };

            ApplyLanguage();
        }

        // ── Sprache ────────────────────────────────────────────────────────
        private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            _isEnglish = cmbLanguage.SelectedIndex == 1;
            ApplyLanguage();
        }

        private string T(string de, string en) => _isEnglish ? en : de;

        private void ApplyLanguage()
        {
            Text = "DataHeater \u2013 Universal DB Migration";

            grpSource.Text = T("Quelle", "Source");
            grpTargets.Text = T("Ziele", "Targets");
            grpTables.Text = T("Tabellen", "Tables");
            grpMode.Text = T("Migrationsmodus", "Migration Mode");

            lblSrcType.Text = T("Typ:", "Type:");
            lblTgtType.Text = T("Typ:", "Type:");

            lblSrcHost.Text = "Host:";
            lblSrcPort.Text = "Port:";
            lblSrcDatabase.Text = T("Datenbank:", "Database:");
            lblSrcUsername.Text = T("Benutzer:", "User:");
            lblSrcPassword.Text = T("Passwort:", "Password:");

            lblTgtHost.Text = "Host:";
            lblTgtPort.Text = "Port:";
            lblTgtDatabase.Text = T("Datenbank:", "Database:");
            lblTgtUsername.Text = T("Benutzer:", "User:");
            lblTgtPassword.Text = T("Passwort:", "Password:");

            UpdateSrcPlaceholder(cmbSrcType.SelectedItem?.ToString() ?? "");
            UpdateTgtPlaceholder(cmbTgtType.SelectedItem?.ToString() ?? "");

            btnSrcBrowse.Text = "\U0001F4C2 ...";
            btnTgtBrowse.Text = "\U0001F4C2 ...";

            if (_editSrcIdx < 0)
            {
                btnAddSource.Text = T("\u2795 Hinzuf\u00fcgen", "\u2795 Add");
                btnEditSource.Text = T("\u270f\ufe0f Bearbeiten", "\u270f\ufe0f Edit");
            }
            btnRemoveSource.Text = T("\u2796 Entfernen", "\u2796 Remove");

            if (_editTgtIdx < 0)
            {
                btnAddTarget.Text = T("\u2795 Hinzuf\u00fcgen", "\u2795 Add");
                btnEditTarget.Text = T("\u270f\ufe0f Bearbeiten", "\u270f\ufe0f Edit");
            }
            btnRemoveTarget.Text = T("\u2796 Entfernen", "\u2796 Remove");

            btnDirection.Text = "\u21c4";

            btnCheckAll.Text = T("\u2611 Alle", "\u2611 All");
            btnCheckNone.Text = T("\u2610 Keine", "\u2610 None");

            chkRenameDuplicates.Text = T(
                "\u26a0\ufe0f Duplikate automatisch umbenennen",
                "\u26a0\ufe0f Auto-rename duplicates");
            chkCreateDb.Text = T(
                "\U0001f5c4\ufe0f Datenbank automatisch erstellen falls nicht vorhanden",
                "\U0001f5c4\ufe0f Auto-create database if not exists");

            rbInsert.Text = T("Nur einf\u00fcgen (INSERT)", "Insert only (INSERT)");
            rbReplace.Text = T("L\u00f6schen + neu", "Delete + re-insert");

            btnConnect.Text = T("\U0001f50c Verbinden", "\U0001f50c Connect");
            btnMigrate.Text = T("Migrieren \u2192", "Migrate \u2192");

            lblLang.Text = T("Spr.:", "Lang:");
            if (lblStatus.Text == "Bereit." || lblStatus.Text == "Ready.")
                SetStatus(T("Bereit.", "Ready."), Color.Gray);
        }

        private void UpdateSrcPlaceholder(string t) =>
            txtSrcPath.PlaceholderText = t switch
            {
                "CSV" => T("Ordner mit CSV-Dateien \u2026", "Folder with CSV files \u2026"),
                "Excel" => T("Excel-Datei (.xlsx) \u2026", "Excel file (.xlsx) \u2026"),
                _ => T("Dateipfad \u2026", "File path \u2026")
            };

        private void UpdateTgtPlaceholder(string t) =>
            txtTgtPath.PlaceholderText = t switch
            {
                "CSV" => T("Zielordner \u2026", "Target folder \u2026"),
                "Excel" => T("Excel-Datei (.xlsx) \u2026", "Excel file (.xlsx) \u2026"),
                _ => T("Dateipfad \u2026", "File path \u2026")
            };

        // ── Typ-Auswahl ────────────────────────────────────────────────────
        private static bool IsFileBased(string t) =>
            t == "SQLite" || t == "CSV" || t == "Excel";

        private void cmbSrcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = cmbSrcType.SelectedItem?.ToString() ?? "";
            pnlSrcSqlite.Visible = IsFileBased(t);
            pnlSrcDb.Visible = !IsFileBased(t);
            if (!IsFileBased(t)) txtSrcPort.Text = DefaultPort(t);
            UpdateSrcPlaceholder(t);
        }

        private void cmbTgtType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = cmbTgtType.SelectedItem?.ToString() ?? "";
            pnlTgtSqlite.Visible = IsFileBased(t);
            pnlTgtDb.Visible = !IsFileBased(t);
            if (!IsFileBased(t)) txtTgtPort.Text = DefaultPort(t);
            UpdateTgtPlaceholder(t);
        }

        private static string DefaultPort(string type) => type switch
        {
            "PostgreSQL" => "5432",
            "Oracle" => "1521",
            _ => "3306"
        };

        // ── Browse ─────────────────────────────────────────────────────────
        private void btnSrcBrowse_Click(object sender, EventArgs e)
        {
            string t = cmbSrcType.SelectedItem?.ToString() ?? "";
            if (t == "CSV")
            {
                using var d = new FolderBrowserDialog
                { Description = T("Ordner mit CSV-Dateien w\u00e4hlen", "Select folder with CSV files") };
                if (d.ShowDialog() == DialogResult.OK) txtSrcPath.Text = d.SelectedPath;
            }
            else if (t == "Excel")
            {
                using var d = new OpenFileDialog
                { Filter = "Excel (*.xlsx)|*.xlsx", Title = T("Excel-Datei \u00f6ffnen", "Open Excel file") };
                if (d.ShowDialog() == DialogResult.OK) txtSrcPath.Text = d.FileName;
            }
            else
            {
                using var d = new OpenFileDialog
                { Filter = $"SQLite (*.db;*.sqlite)|*.db;*.sqlite|{T("Alle", "All")} (*.*)|*.*" };
                if (d.ShowDialog() == DialogResult.OK) txtSrcPath.Text = d.FileName;
            }
        }

        private void btnTgtBrowse_Click(object sender, EventArgs e)
        {
            string t = cmbTgtType.SelectedItem?.ToString() ?? "";
            if (t == "CSV")
            {
                using var d = new FolderBrowserDialog
                { Description = T("Zielordner f\u00fcr CSV-Dateien w\u00e4hlen", "Select target folder for CSV files") };
                if (d.ShowDialog() == DialogResult.OK) txtTgtPath.Text = d.SelectedPath;
            }
            else if (t == "Excel")
            {
                using var d = new SaveFileDialog
                { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "export.xlsx", Title = T("Excel-Zieldatei w\u00e4hlen", "Choose target Excel file") };
                if (d.ShowDialog() == DialogResult.OK) txtTgtPath.Text = d.FileName;
            }
            else
            {
                using var d = new OpenFileDialog
                { Filter = $"SQLite (*.db;*.sqlite)|*.db;*.sqlite|{T("Alle", "All")} (*.*)|*.*" };
                if (d.ShowDialog() == DialogResult.OK) txtTgtPath.Text = d.FileName;
            }
        }

        // ── Quelle ─────────────────────────────────────────────────────────
        private void btnAddSource_Click(object sender, EventArgs e)
        {
            var t = BuildTarget(cmbSrcType, txtSrcPath, txtSrcHost, txtSrcPort,
                                txtSrcDatabase, txtSrcUsername, txtSrcPassword, isSource: true);
            if (t == null) return;

            if (_editSrcIdx >= 0)
            {
                bool was = chkSources.GetItemChecked(_editSrcIdx);
                _sources[_editSrcIdx] = t;
                chkSources.Items[_editSrcIdx] = t;
                chkSources.SetItemChecked(_editSrcIdx, was);
                EndEditSource();
                SetStatus(T("\u270f\ufe0f Quelle aktualisiert \u2013 bitte neu verbinden.",
                            "\u270f\ufe0f Source updated \u2013 please reconnect."), Color.DarkOrange);
                listTables.Items.Clear();
            }
            else
            {
                _sources.Add(t);
                chkSources.SetItemChecked(chkSources.Items.Add(t), true);
            }
        }

        private void btnRemoveSource_Click(object sender, EventArgs e)
        {
            if (chkSources.SelectedIndex < 0) return;
            int i = chkSources.SelectedIndex;
            _sources.RemoveAt(i); chkSources.Items.RemoveAt(i);
        }

        private void btnEditSource_Click(object sender, EventArgs e)
        {
            if (chkSources.SelectedIndex < 0)
            {
                MessageBox.Show(T("Bitte Eintrag ausw\u00e4hlen!", "Please select an entry!"),
                    T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int idx = chkSources.SelectedIndex;
            LoadToPanel(_sources[idx], cmbSrcType, txtSrcPath, txtSrcHost, txtSrcPort,
                        txtSrcDatabase, txtSrcUsername, txtSrcPassword);
            _editSrcIdx = idx;
            btnAddSource.Text = T("\U0001f4be Speichern", "\U0001f4be Save");
            btnEditSource.Text = T("\u274c Abbrechen", "\u274c Cancel");
            btnEditSource.Click -= btnEditSource_Click;
            btnEditSource.Click += btnCancelEditSource_Click;
        }

        private void btnCancelEditSource_Click(object sender, EventArgs e) => EndEditSource();
        private void EndEditSource()
        {
            _editSrcIdx = -1;
            btnAddSource.Text = T("\u2795 Hinzuf\u00fcgen", "\u2795 Add");
            btnEditSource.Text = T("\u270f\ufe0f Bearbeiten", "\u270f\ufe0f Edit");
            btnEditSource.Click -= btnCancelEditSource_Click;
            btnEditSource.Click += btnEditSource_Click;
        }

        // ── Ziel ───────────────────────────────────────────────────────────
        private void btnAddTarget_Click(object sender, EventArgs e)
        {
            var t = BuildTarget(cmbTgtType, txtTgtPath, txtTgtHost, txtTgtPort,
                                txtTgtDatabase, txtTgtUsername, txtTgtPassword, isSource: false);
            if (t == null) return;

            if (_editTgtIdx >= 0)
            {
                bool was = chkTargets.GetItemChecked(_editTgtIdx);
                _targets[_editTgtIdx] = t;
                chkTargets.Items[_editTgtIdx] = t;
                chkTargets.SetItemChecked(_editTgtIdx, was);
                EndEditTarget();
                SetStatus(T("\u270f\ufe0f Ziel aktualisiert \u2013 bitte neu verbinden.",
                            "\u270f\ufe0f Target updated \u2013 please reconnect."), Color.DarkOrange);
            }
            else
            {
                _targets.Add(t);
                chkTargets.SetItemChecked(chkTargets.Items.Add(t), true);
            }
        }

        private void btnRemoveTarget_Click(object sender, EventArgs e)
        {
            if (chkTargets.SelectedIndex < 0) return;
            int i = chkTargets.SelectedIndex;
            _targets.RemoveAt(i); chkTargets.Items.RemoveAt(i);
        }

        private void btnEditTarget_Click(object sender, EventArgs e)
        {
            if (chkTargets.SelectedIndex < 0)
            {
                MessageBox.Show(T("Bitte Eintrag ausw\u00e4hlen!", "Please select an entry!"),
                    T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int idx = chkTargets.SelectedIndex;
            LoadToPanel(_targets[idx], cmbTgtType, txtTgtPath, txtTgtHost, txtTgtPort,
                        txtTgtDatabase, txtTgtUsername, txtTgtPassword);
            _editTgtIdx = idx;
            btnAddTarget.Text = T("\U0001f4be Speichern", "\U0001f4be Save");
            btnEditTarget.Text = T("\u274c Abbrechen", "\u274c Cancel");
            btnEditTarget.Click -= btnEditTarget_Click;
            btnEditTarget.Click += btnCancelEditTarget_Click;
        }

        private void btnCancelEditTarget_Click(object sender, EventArgs e) => EndEditTarget();
        private void EndEditTarget()
        {
            _editTgtIdx = -1;
            btnAddTarget.Text = T("\u2795 Hinzuf\u00fcgen", "\u2795 Add");
            btnEditTarget.Text = T("\u270f\ufe0f Bearbeiten", "\u270f\ufe0f Edit");
            btnEditTarget.Click -= btnCancelEditTarget_Click;
            btnEditTarget.Click += btnEditTarget_Click;
        }

        // ── Panel <-> DbTarget ─────────────────────────────────────────────
        private static void LoadToPanel(DbTarget t,
            ComboBox type, TextBox path,
            TextBox host, TextBox port, TextBox db, TextBox user, TextBox pwd)
        {
            type.SelectedItem = t.Type switch
            {
                DbType.PostgreSQL => "PostgreSQL",
                DbType.Oracle => "Oracle",
                DbType.MariaDB => "MariaDB",
                DbType.CSV => "CSV",
                DbType.Excel => "Excel",
                _ => "SQLite"
            };
            if (t.Type == DbType.SQLite || t.Type == DbType.CSV || t.Type == DbType.Excel)
            { path.Text = t.Database; return; }
            host.Text = t.Host; port.Text = t.Port;
            db.Text = t.Database; user.Text = t.Username; pwd.Text = t.Password;
        }

        private DbTarget BuildTarget(
            ComboBox cmbType, TextBox txtPath,
            TextBox txtHost, TextBox txtPort,
            TextBox txtDb, TextBox txtUser, TextBox txtPwd,
            bool isSource)
        {
            string type = cmbType.SelectedItem?.ToString() ?? "MariaDB";

            // ── SQLite ─────────────────────────────────────────────────────
            if (type == "SQLite")
            {
                if (isSource)
                {
                    if (string.IsNullOrWhiteSpace(txtPath.Text))
                    {
                        MessageBox.Show(
                            T("Bitte SQLite-Datei ausw\u00e4hlen!", "Please select a SQLite file!"),
                            T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    return new DbTarget { Type = DbType.SQLite, Database = txtPath.Text };
                }
                using var ds = new SaveFileDialog
                { Filter = "SQLite (*.db)|*.db", FileName = "export.db" };
                if (ds.ShowDialog() != DialogResult.OK) return null;
                return new DbTarget { Type = DbType.SQLite, Database = ds.FileName };
            }

            // ── CSV ────────────────────────────────────────────────────────
            if (type == "CSV")
            {
                if (string.IsNullOrWhiteSpace(txtPath.Text))
                {
                    using var fd = new FolderBrowserDialog
                    {
                        Description = isSource
                            ? T("Ordner mit CSV-Dateien w\u00e4hlen", "Select folder with CSV files")
                            : T("Zielordner f\u00fcr CSV-Dateien w\u00e4hlen", "Select target folder for CSV files")
                    };
                    if (fd.ShowDialog() != DialogResult.OK) return null;
                    txtPath.Text = fd.SelectedPath;
                }
                return new DbTarget { Type = DbType.CSV, Database = txtPath.Text };
            }

            // ── Excel ──────────────────────────────────────────────────────
            if (type == "Excel")
            {
                if (isSource)
                {
                    if (string.IsNullOrWhiteSpace(txtPath.Text))
                    {
                        using var od = new OpenFileDialog
                        { Filter = "Excel (*.xlsx)|*.xlsx", Title = T("Excel-Datei \u00f6ffnen", "Open Excel file") };
                        if (od.ShowDialog() != DialogResult.OK) return null;
                        txtPath.Text = od.FileName;
                    }
                    return new DbTarget { Type = DbType.Excel, Database = txtPath.Text };
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtPath.Text))
                    {
                        using var sd = new SaveFileDialog
                        { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "export.xlsx" };
                        if (sd.ShowDialog() != DialogResult.OK) return null;
                        txtPath.Text = sd.FileName;
                    }
                    return new DbTarget { Type = DbType.Excel, Database = txtPath.Text };
                }
            }

            // ── DB-Typen ───────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtHost.Text) || string.IsNullOrWhiteSpace(txtDb.Text))
            {
                MessageBox.Show(
                    T("Bitte Host und Datenbank eingeben!", "Please enter host and database!"),
                    T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return new DbTarget
            {
                Type = type switch
                {
                    "PostgreSQL" => DbType.PostgreSQL,
                    "Oracle" => DbType.Oracle,
                    _ => DbType.MariaDB
                },
                Host = txtHost.Text,
                Port = txtPort.Text,
                Database = txtDb.Text,
                Username = txtUser.Text,
                Password = txtPwd.Text
            };
        }

        // ── Richtung tauschen ──────────────────────────────────────────────
        private void btnDirection_Click(object sender, EventArgs e)
        {
            var ts = new List<DbTarget>(_sources);
            var tt = new List<DbTarget>(_targets);
            _sources.Clear(); chkSources.Items.Clear();
            _targets.Clear(); chkTargets.Items.Clear();
            foreach (var t in tt) { _sources.Add(t); chkSources.SetItemChecked(chkSources.Items.Add(t), true); }
            foreach (var t in ts) { _targets.Add(t); chkTargets.SetItemChecked(chkTargets.Items.Add(t), true); }
            listTables.Items.Clear();
            SetStatus(T("\u21c4 Getauscht \u2013 bitte neu verbinden.",
                        "\u21c4 Swapped \u2013 please reconnect."), Color.DarkOrange);
        }

        // ── Alle / Keine ───────────────────────────────────────────────────
        private void btnCheckAll_Click(object sender, EventArgs e)
        { for (int i = 0; i < listTables.Items.Count; i++) listTables.SetItemChecked(i, true); }

        private void btnCheckNone_Click(object sender, EventArgs e)
        { for (int i = 0; i < listTables.Items.Count; i++) listTables.SetItemChecked(i, false); }

        // ── DB-Instanz ─────────────────────────────────────────────────────
        private ITargetDatabase BuildDb(DbTarget t) => t.Type switch
        {
            DbType.PostgreSQL => new PostgresDatabase(
                t.ConnectionString, t.ConnectionStringWithoutDb, t.Database, chkCreateDb.Checked),
            DbType.Oracle => new OracleDatabase(
                t.ConnectionString, t.ConnectionStringWithoutDb, t.Database, chkCreateDb.Checked),
            DbType.SQLite => new SqliteDatabase(t.ConnectionString),
            DbType.CSV => new CsvDatabase(t.ConnectionString),
            DbType.Excel => new ExcelDatabase(t.ConnectionString),
            _ => new MariaDbDatabase(
                t.ConnectionString, t.ConnectionStringWithoutDb, t.Database, chkCreateDb.Checked)
        };

        // ── Verbinden ──────────────────────────────────────────────────────
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (chkSources.CheckedItems.Count == 0)
            {
                MessageBox.Show(
                    T("Bitte mindestens eine Quelle anhaken!", "Please check at least one source!"),
                    T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                SetStatus(T("Verbinde\u2026", "Connecting\u2026"), Color.Gray);
                listTables.Items.Clear();
                for (int i = 0; i < chkSources.Items.Count; i++)
                {
                    if (!chkSources.GetItemChecked(i)) continue;
                    var tables = await BuildDb(_sources[i]).GetTablesAsync();
                    foreach (var t in tables)
                    {
                        int idx = listTables.Items.Add(
                            new TableEntry(_sources) { TableName = t, SourceIndex = i });
                        listTables.SetItemChecked(idx, true);
                    }
                }
                SetStatus(
                    T($"\u2705 Verbunden! {listTables.Items.Count} Tabellen.",
                      $"\u2705 Connected! {listTables.Items.Count} tables."),
                    Color.Green);
            }
            catch (Exception ex)
            {
                SetStatus("\u274c " + ex.Message, Color.Red);
                MessageBox.Show(ex.Message,
                    T("Verbindungsfehler", "Connection Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Migrieren ──────────────────────────────────────────────────────
        private async void btnMigrate_Click(object sender, EventArgs e)
        {
            if (listTables.CheckedItems.Count == 0)
            {
                MessageBox.Show(
                    T("Bitte mindestens eine Tabelle anhaken!", "Please check at least one table!"),
                    T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (chkTargets.CheckedItems.Count == 0)
            {
                MessageBox.Show(
                    T("Bitte mindestens ein Ziel anhaken!", "Please check at least one target!"),
                    T("Hinweis", "Note"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var entries = listTables.CheckedItems.Cast<TableEntry>().ToList();
            var duplicates = entries.GroupBy(x => x.TableName)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Count > 0 && !chkRenameDuplicates.Checked)
            {
                MessageBox.Show(
                    T("Folgende Tabellennamen sind mehrfach vorhanden:\n\n",
                      "The following table names appear multiple times:\n\n") +
                    string.Join("\n", duplicates) +
                    T("\n\nBitte 'Duplikate umbenennen' aktivieren oder doppelte Eintr\u00e4ge abhaken.",
                      "\n\nPlease enable 'Auto-rename duplicates' or uncheck duplicate entries."),
                    T("Duplikate!", "Duplicates!"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                btnMigrate.Enabled = btnConnect.Enabled = false;
                int total = entries.Count, n = 0;

                foreach (var entry in entries)
                {
                    n++;
                    string tgtName = entry.TableName;
                    if (chkRenameDuplicates.Checked && duplicates.Contains(entry.TableName))
                    {
                        string suffix = System.IO.Path.GetFileNameWithoutExtension(
                            _sources[entry.SourceIndex].Database ?? "src");
                        tgtName = $"{entry.TableName}_from_{suffix}";
                    }

                    var data = await BuildDb(_sources[entry.SourceIndex])
                        .GetTableDataAsync(entry.TableName);

                    for (int i = 0; i < chkTargets.Items.Count; i++)
                    {
                        if (!chkTargets.GetItemChecked(i)) continue;
                        var db = BuildDb(_targets[i]);
                        SetStatus(
                            $"\u23f3 '{entry.TableName}' \u2192 '{tgtName}' @ {_targets[i]} ({n}/{total})",
                            Color.DarkBlue);
                        Application.DoEvents();
                        await db.CreateTableIfNotExistsAsync(data, tgtName);
                        if (rbReplace.Checked) await db.TruncateTableAsync(tgtName);
                        await db.InsertDataAsync(tgtName, data);
                    }
                }

                SetStatus(
                    T($"\u2705 Migration abgeschlossen! {n} Tabelle(n). Pr\u00fcfe NULLs\u2026",
                      $"\u2705 Migration complete! {n} table(s). Checking NULLs\u2026"),
                    Color.Green);
                Application.DoEvents();

                // NULL-Prüfung
                var nullReport = new System.Text.StringBuilder();
                foreach (var entry2 in entries)
                {
                    string tName = entry2.TableName;
                    if (chkRenameDuplicates.Checked && duplicates.Contains(entry2.TableName))
                    {
                        string sfx = System.IO.Path.GetFileNameWithoutExtension(
                            _sources[entry2.SourceIndex].Database ?? "src");
                        tName = $"{entry2.TableName}_from_{sfx}";
                    }
                    for (int ti = 0; ti < chkTargets.Items.Count; ti++)
                    {
                        if (!chkTargets.GetItemChecked(ti)) continue;
                        try
                        {
                            var td = await BuildDb(_targets[ti]).GetTableDataAsync(tName);
                            if (td.Rows.Count == 0)
                            {
                                nullReport.AppendLine(
                                    T($"\u26a0\ufe0f  {tName}: LEER (0 Zeilen)!",
                                      $"\u26a0\ufe0f  {tName}: EMPTY (0 rows)!"));
                                continue;
                            }
                            foreach (System.Data.DataColumn col in td.Columns)
                            {
                                bool allNull = td.Rows.Cast<System.Data.DataRow>()
                                    .All(r => r[col] == System.DBNull.Value || r[col]?.ToString() == "");
                                if (allNull)
                                    nullReport.AppendLine(
                                        T($"\u26a0\ufe0f  {tName}.{col.ColumnName}: komplett NULL",
                                          $"\u26a0\ufe0f  {tName}.{col.ColumnName}: all NULL"));
                            }
                        }
                        catch { }
                    }
                }

                string report = nullReport.Length > 0
                    ? T("\u26a0\ufe0f Folgende Spalten sind komplett NULL:\n\n",
                        "\u26a0\ufe0f The following columns are all NULL:\n\n") + nullReport
                    : T("\u2705 Datenkontrolle OK \u2013 keine komplett-NULL Spalten.",
                        "\u2705 Data check OK \u2013 no all-NULL columns.");

                SetStatus(
                    T($"\u2705 Migration abgeschlossen! {n} Tabelle(n).",
                      $"\u2705 Migration complete! {n} table(s)."),
                    Color.Green);

                MessageBox.Show(
                    T("Migration abgeschlossen!\n\n", "Migration complete!\n\n") + report,
                    T("Fertig", "Done"),
                    MessageBoxButtons.OK,
                    nullReport.Length > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("\u274c " + ex.Message, Color.Red);
                MessageBox.Show(ex.Message,
                    T("Fehler", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnMigrate.Enabled = btnConnect.Enabled = true;
            }
        }

        private void SetStatus(string msg, Color color)
        { lblStatus.ForeColor = color; lblStatus.Text = msg; }
    }
}