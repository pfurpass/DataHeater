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

        private class TableEntry
        {
            public string TableName { get; set; }
            public int SourceIndex { get; set; }
            private readonly List<DbTarget> _src;
            public TableEntry(List<DbTarget> src) => _src = src;
            public override string ToString() => $"[{TableName}]  ←  {_src[SourceIndex]}";
        }

        public Form1()
        {
            InitializeComponent();

            // Explizite Defaults (nicht vom Designer abhängig)
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
        }

        // ── Typ-Auswahl ────────────────────────────────────────────────────
        private void cmbSrcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = cmbSrcType.SelectedItem?.ToString() ?? "";
            pnlSrcSqlite.Visible = t == "SQLite";
            pnlSrcDb.Visible = t != "SQLite";
            if (t != "SQLite") txtSrcPort.Text = DefaultPort(t);
        }

        private void cmbTgtType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string t = cmbTgtType.SelectedItem?.ToString() ?? "";
            pnlTgtSqlite.Visible = t == "SQLite";
            pnlTgtDb.Visible = t != "SQLite";
            if (t != "SQLite") txtTgtPort.Text = DefaultPort(t);
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
            using var d = new OpenFileDialog
            { Filter = "SQLite (*.db;*.sqlite)|*.db;*.sqlite|Alle (*.*)|*.*" };
            if (d.ShowDialog() == DialogResult.OK) txtSrcPath.Text = d.FileName;
        }

        private void btnTgtBrowse_Click(object sender, EventArgs e)
        {
            using var d = new OpenFileDialog
            { Filter = "SQLite (*.db;*.sqlite)|*.db;*.sqlite|Alle (*.*)|*.*" };
            if (d.ShowDialog() == DialogResult.OK) txtTgtPath.Text = d.FileName;
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
                SetStatus("✏️ Quelle aktualisiert – bitte neu verbinden.", Color.DarkOrange);
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
            { MessageBox.Show("Bitte Eintrag auswählen!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int idx = chkSources.SelectedIndex;
            LoadToPanel(_sources[idx], cmbSrcType, txtSrcPath, txtSrcHost, txtSrcPort,
                        txtSrcDatabase, txtSrcUsername, txtSrcPassword);
            _editSrcIdx = idx;
            btnAddSource.Text = "💾 Speichern";
            btnEditSource.Text = "❌ Abbrechen";
            btnEditSource.Click -= btnEditSource_Click;
            btnEditSource.Click += btnCancelEditSource_Click;
        }

        private void btnCancelEditSource_Click(object sender, EventArgs e) => EndEditSource();
        private void EndEditSource()
        {
            _editSrcIdx = -1;
            btnAddSource.Text = "➕ Hinzufügen";
            btnEditSource.Text = "✏️ Bearbeiten";
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
                SetStatus("✏️ Ziel aktualisiert – bitte neu verbinden.", Color.DarkOrange);
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
            { MessageBox.Show("Bitte Eintrag auswählen!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int idx = chkTargets.SelectedIndex;
            LoadToPanel(_targets[idx], cmbTgtType, txtTgtPath, txtTgtHost, txtTgtPort,
                        txtTgtDatabase, txtTgtUsername, txtTgtPassword);
            _editTgtIdx = idx;
            btnAddTarget.Text = "💾 Speichern";
            btnEditTarget.Text = "❌ Abbrechen";
            btnEditTarget.Click -= btnEditTarget_Click;
            btnEditTarget.Click += btnCancelEditTarget_Click;
        }

        private void btnCancelEditTarget_Click(object sender, EventArgs e) => EndEditTarget();
        private void EndEditTarget()
        {
            _editTgtIdx = -1;
            btnAddTarget.Text = "➕ Hinzufügen";
            btnEditTarget.Text = "✏️ Bearbeiten";
            btnEditTarget.Click -= btnCancelEditTarget_Click;
            btnEditTarget.Click += btnEditTarget_Click;
        }

        // ── Panel ↔ DbTarget ───────────────────────────────────────────────
        private static void LoadToPanel(DbTarget t,
            ComboBox type, TextBox path,
            TextBox host, TextBox port, TextBox db, TextBox user, TextBox pwd)
        {
            type.SelectedItem = t.Type switch
            {
                DbType.PostgreSQL => "PostgreSQL",
                DbType.Oracle => "Oracle",
                DbType.MariaDB => "MariaDB",
                _ => "SQLite"
            };
            if (t.Type == DbType.SQLite) { path.Text = t.Database; return; }
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

            if (type == "SQLite")
            {
                if (isSource)
                {
                    if (string.IsNullOrWhiteSpace(txtPath.Text))
                    { MessageBox.Show("Bitte SQLite Datei auswählen!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return null; }
                    return new DbTarget { Type = DbType.SQLite, Database = txtPath.Text };
                }
                using var d = new SaveFileDialog
                { Filter = "SQLite (*.db)|*.db", FileName = "export.db" };
                if (d.ShowDialog() != DialogResult.OK) return null;
                return new DbTarget { Type = DbType.SQLite, Database = d.FileName };
            }

            if (string.IsNullOrWhiteSpace(txtHost.Text) || string.IsNullOrWhiteSpace(txtDb.Text))
            { MessageBox.Show("Bitte Host und Datenbank eingeben!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return null; }

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
            SetStatus("⇄ Getauscht – bitte neu verbinden.", Color.DarkOrange);
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
            _ => new MariaDbDatabase(
                t.ConnectionString, t.ConnectionStringWithoutDb, t.Database, chkCreateDb.Checked)
        };

        // ── Verbinden ──────────────────────────────────────────────────────
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (chkSources.CheckedItems.Count == 0)
            { MessageBox.Show("Bitte mindestens eine Quelle anhaken!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                SetStatus("Verbinde…", Color.Gray);
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
                SetStatus($"✅ Verbunden! {listTables.Items.Count} Tabellen.", Color.Green);
            }
            catch (Exception ex)
            {
                SetStatus("❌ " + ex.Message, Color.Red);
                MessageBox.Show(ex.Message, "Verbindungsfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Migrieren ──────────────────────────────────────────────────────
        private async void btnMigrate_Click(object sender, EventArgs e)
        {
            if (listTables.CheckedItems.Count == 0)
            { MessageBox.Show("Bitte mindestens eine Tabelle anhaken!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (chkTargets.CheckedItems.Count == 0)
            { MessageBox.Show("Bitte mindestens ein Ziel anhaken!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var entries = listTables.CheckedItems.Cast<TableEntry>().ToList();
            var duplicates = entries.GroupBy(x => x.TableName)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Count > 0 && !chkRenameDuplicates.Checked)
            {
                MessageBox.Show(
                    "Folgende Tabellennamen sind mehrfach vorhanden:\n\n" +
                    string.Join("\n", duplicates) +
                    "\n\nBitte 'Duplikate umbenennen' aktivieren oder doppelte Einträge abhaken.",
                    "Duplikate!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        SetStatus($"⏳ '{entry.TableName}' → '{tgtName}' @ {_targets[i]} ({n}/{total})",
                            Color.DarkBlue);
                        Application.DoEvents();
                        await db.CreateTableIfNotExistsAsync(data, tgtName);
                        if (rbReplace.Checked) await db.TruncateTableAsync(tgtName);
                        await db.InsertDataAsync(tgtName, data);
                    }
                }

                SetStatus($"✅ Migration abgeschlossen! {n} Tabelle(n). Prüfe NULLs…", Color.Green);
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
                            { nullReport.AppendLine($"⚠️  {tName}: LEER (0 Zeilen)!"); continue; }
                            foreach (System.Data.DataColumn col in td.Columns)
                            {
                                bool allNull = td.Rows.Cast<System.Data.DataRow>()
                                    .All(r => r[col] == System.DBNull.Value || r[col]?.ToString() == "");
                                if (allNull)
                                    nullReport.AppendLine($"⚠️  {tName}.{col.ColumnName}: komplett NULL");
                            }
                        }
                        catch { }
                    }
                }

                string report = nullReport.Length > 0
                    ? "⚠️ Folgende Spalten sind komplett NULL:" + nullReport
                    : "✅ Datenkontrolle OK – keine komplett-NULL Spalten.";

                SetStatus($"✅ Migration abgeschlossen! {n} Tabelle(n).", Color.Green);
                MessageBox.Show("Migration abgeschlossen!" + report, "Fertig",
                    MessageBoxButtons.OK,
                    nullReport.Length > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("❌ " + ex.Message, Color.Red);
                MessageBox.Show(ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
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