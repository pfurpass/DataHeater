using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DataHeater.Helper;
using System.Threading.Tasks;

namespace DataHeater
{
    public partial class Form1 : Form
    {
        private readonly List<DbTarget> _sources = new();
        private readonly List<DbTarget> _targets = new();
        private int _editingSourceIndex = -1;
        private int _editingTargetIndex = -1;

        private class TableEntry
        {
            public string TableName { get; set; }
            public int SourceIndex { get; set; }
            private readonly List<DbTarget> _src;
            public TableEntry(List<DbTarget> src) { _src = src; }
            public override string ToString() => $"[{TableName}]  ←  {_src[SourceIndex]}";
        }

        public Form1()
        {
            InitializeComponent();

            listTables.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Space)
                {
                    bool allChecked = listTables.SelectedIndices.Cast<int>()
                        .All(i => listTables.GetItemChecked(i));
                    foreach (int i in listTables.SelectedIndices)
                        listTables.SetItemChecked(i, !allChecked);
                    e.Handled = true;
                }
            };
        }

        private void cmbSrcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isSqlite = cmbSrcType.SelectedItem.ToString() == "SQLite";
            pnlSrcSqlite.Visible = isSqlite;
            pnlSrcDb.Visible = !isSqlite;
            if (!isSqlite)
                txtSrcPort.Text = cmbSrcType.SelectedItem.ToString() == "PostgreSQL" ? "5432" : "3306";
        }

        private void cmbTgtType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isSqlite = cmbTgtType.SelectedItem.ToString() == "SQLite";
            pnlTgtSqlite.Visible = isSqlite;
            pnlTgtDb.Visible = !isSqlite;
            if (!isSqlite)
                txtTgtPort.Text = cmbTgtType.SelectedItem.ToString() == "PostgreSQL" ? "5432" : "3306";
        }

        private void btnSrcBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "SQLite Datenbank (*.db;*.sqlite)|*.db;*.sqlite|Alle Dateien (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
                txtSrcPath.Text = dlg.FileName;
        }

        private void btnTgtBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "SQLite Datenbank (*.db;*.sqlite)|*.db;*.sqlite|Alle Dateien (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
                txtTgtPath.Text = dlg.FileName;
        }

        private void btnAddSource_Click(object sender, EventArgs e)
        {
            var target = BuildTargetFromPanel(cmbSrcType, txtSrcPath,
                txtSrcHost, txtSrcPort, txtSrcDatabase, txtSrcUsername, txtSrcPassword, isSource: true);
            if (target == null) return;

            if (_editingSourceIndex >= 0)
            {
                bool wasChecked = chkSources.GetItemChecked(_editingSourceIndex);
                _sources[_editingSourceIndex] = target;
                chkSources.Items[_editingSourceIndex] = target;
                chkSources.SetItemChecked(_editingSourceIndex, wasChecked);
                _editingSourceIndex = -1;
                btnAddSource.Text = "➕ Hinzufügen";
                btnEditSource.Text = "✏️ Bearbeiten";
                btnEditSource.Click -= btnCancelEditSource_Click;
                btnEditSource.Click += btnEditSource_Click;
                listTables.Items.Clear();
                lblStatus.ForeColor = Color.DarkOrange;
                lblStatus.Text = "✏️ Eintrag aktualisiert – bitte neu verbinden.";
            }
            else
            {
                _sources.Add(target);
                int idx = chkSources.Items.Add(target);
                chkSources.SetItemChecked(idx, true);
            }
        }

        private void btnRemoveSource_Click(object sender, EventArgs e)
        {
            if (chkSources.SelectedIndex < 0) return;
            int idx = chkSources.SelectedIndex;
            _sources.RemoveAt(idx);
            chkSources.Items.RemoveAt(idx);
        }

        private void btnEditSource_Click(object sender, EventArgs e)
        {
            if (chkSources.SelectedIndex < 0)
            {
                MessageBox.Show("Bitte einen Eintrag auswählen!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int idx = chkSources.SelectedIndex;
            LoadIntoPanel(_sources[idx], cmbSrcType, txtSrcPath,
                txtSrcHost, txtSrcPort, txtSrcDatabase, txtSrcUsername, txtSrcPassword);
            _editingSourceIndex = idx;
            btnAddSource.Text = "💾 Speichern";
            btnEditSource.Text = "❌ Abbrechen";
            btnEditSource.Click -= btnEditSource_Click;
            btnEditSource.Click += btnCancelEditSource_Click;
        }

        private void btnCancelEditSource_Click(object sender, EventArgs e)
        {
            _editingSourceIndex = -1;
            btnAddSource.Text = "➕ Hinzufügen";
            btnEditSource.Text = "✏️ Bearbeiten";
            btnEditSource.Click -= btnCancelEditSource_Click;
            btnEditSource.Click += btnEditSource_Click;
        }

        private void btnAddTarget_Click(object sender, EventArgs e)
        {
            var target = BuildTargetFromPanel(cmbTgtType, txtTgtPath,
                txtTgtHost, txtTgtPort, txtTgtDatabase, txtTgtUsername, txtTgtPassword, isSource: false);
            if (target == null) return;

            if (_editingTargetIndex >= 0)
            {
                bool wasChecked = chkTargets.GetItemChecked(_editingTargetIndex);
                _targets[_editingTargetIndex] = target;
                chkTargets.Items[_editingTargetIndex] = target;
                chkTargets.SetItemChecked(_editingTargetIndex, wasChecked);
                _editingTargetIndex = -1;
                btnAddTarget.Text = "➕ Hinzufügen";
                btnEditTarget.Text = "✏️ Bearbeiten";
                btnEditTarget.Click -= btnCancelEditTarget_Click;
                btnEditTarget.Click += btnEditTarget_Click;
                lblStatus.ForeColor = Color.DarkOrange;
                lblStatus.Text = "✏️ Eintrag aktualisiert – bitte neu verbinden.";
            }
            else
            {
                _targets.Add(target);
                int idx = chkTargets.Items.Add(target);
                chkTargets.SetItemChecked(idx, true);
            }
        }

        private void btnRemoveTarget_Click(object sender, EventArgs e)
        {
            if (chkTargets.SelectedIndex < 0) return;
            int idx = chkTargets.SelectedIndex;
            _targets.RemoveAt(idx);
            chkTargets.Items.RemoveAt(idx);
        }

        private void btnEditTarget_Click(object sender, EventArgs e)
        {
            if (chkTargets.SelectedIndex < 0)
            {
                MessageBox.Show("Bitte einen Eintrag auswählen!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int idx = chkTargets.SelectedIndex;
            LoadIntoPanel(_targets[idx], cmbTgtType, txtTgtPath,
                txtTgtHost, txtTgtPort, txtTgtDatabase, txtTgtUsername, txtTgtPassword);
            _editingTargetIndex = idx;
            btnAddTarget.Text = "💾 Speichern";
            btnEditTarget.Text = "❌ Abbrechen";
            btnEditTarget.Click -= btnEditTarget_Click;
            btnEditTarget.Click += btnCancelEditTarget_Click;
        }

        private void btnCancelEditTarget_Click(object sender, EventArgs e)
        {
            _editingTargetIndex = -1;
            btnAddTarget.Text = "➕ Hinzufügen";
            btnEditTarget.Text = "✏️ Bearbeiten";
            btnEditTarget.Click -= btnCancelEditTarget_Click;
            btnEditTarget.Click += btnEditTarget_Click;
        }

        private void LoadIntoPanel(DbTarget t,
            ComboBox cmbType, TextBox txtPath,
            TextBox txtHost, TextBox txtPort,
            TextBox txtDb, TextBox txtUser, TextBox txtPwd)
        {
            cmbType.SelectedItem = t.Type switch
            {
                DbType.PostgreSQL => "PostgreSQL",
                DbType.MariaDB => "MariaDB",
                _ => "SQLite"
            };

            if (t.Type == DbType.SQLite)
                txtPath.Text = t.Database;
            else
            {
                txtHost.Text = t.Host;
                txtPort.Text = t.Port;
                txtDb.Text = t.Database;
                txtUser.Text = t.Username;
                txtPwd.Text = t.Password;
            }
        }

        private DbTarget BuildTargetFromPanel(
            ComboBox cmbType, TextBox txtPath,
            TextBox txtHost, TextBox txtPort,
            TextBox txtDb, TextBox txtUser, TextBox txtPwd,
            bool isSource)
        {
            string type = cmbType.SelectedItem.ToString();

            if (type == "SQLite")
            {
                if (isSource)
                {
                    if (string.IsNullOrWhiteSpace(txtPath.Text))
                    {
                        MessageBox.Show("Bitte SQLite Datei auswählen!", "Hinweis",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    return new DbTarget { Type = DbType.SQLite, Database = txtPath.Text };
                }
                else
                {
                    using var dlg = new SaveFileDialog();
                    dlg.Filter = "SQLite Datenbank (*.db)|*.db";
                    dlg.FileName = "export.db";
                    if (dlg.ShowDialog() != DialogResult.OK) return null;
                    return new DbTarget { Type = DbType.SQLite, Database = dlg.FileName };
                }
            }

            if (string.IsNullOrWhiteSpace(txtHost.Text) || string.IsNullOrWhiteSpace(txtDb.Text))
            {
                MessageBox.Show("Bitte Host und Datenbank eingeben!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return new DbTarget
            {
                Type = type == "PostgreSQL" ? DbType.PostgreSQL : DbType.MariaDB,
                Host = txtHost.Text,
                Port = txtPort.Text,
                Database = txtDb.Text,
                Username = txtUser.Text,
                Password = txtPwd.Text
            };
        }

        private void btnDirection_Click(object sender, EventArgs e)
        {
            var tmpTargets = new List<DbTarget>(_targets);
            var tmpSources = new List<DbTarget>(_sources);

            _targets.Clear(); chkTargets.Items.Clear();
            _sources.Clear(); chkSources.Items.Clear();

            foreach (var t in tmpSources)
            {
                _targets.Add(t);
                int idx = chkTargets.Items.Add(t);
                chkTargets.SetItemChecked(idx, true);
            }
            foreach (var t in tmpTargets)
            {
                _sources.Add(t);
                int idx = chkSources.Items.Add(t);
                chkSources.SetItemChecked(idx, true);
            }

            listTables.Items.Clear();
            lblStatus.ForeColor = Color.DarkOrange;
            lblStatus.Text = "⇄ Getauscht – bitte neu verbinden.";
        }

        private void btnCheckAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listTables.Items.Count; i++)
                listTables.SetItemChecked(i, true);
        }

        private void btnCheckNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listTables.Items.Count; i++)
                listTables.SetItemChecked(i, false);
        }

        private ITargetDatabase BuildDb(DbTarget target) => target.Type switch
        {
            DbType.PostgreSQL => new PostgresDatabase(
                target.ConnectionString,
                target.ConnectionStringWithoutDb,
                target.Database,
                chkCreateDb.Checked),
            DbType.SQLite => new SqliteDatabase(target.ConnectionString),
            _ => new MariaDbDatabase(
                target.ConnectionString,
                target.ConnectionStringWithoutDb,
                target.Database,
                chkCreateDb.Checked)
        };

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (chkSources.CheckedItems.Count == 0)
            {
                MessageBox.Show("Bitte mindestens eine Quelle hinzufügen und anhaken!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblStatus.ForeColor = Color.Gray;
                lblStatus.Text = "Verbinde...";
                listTables.Items.Clear();

                for (int i = 0; i < chkSources.Items.Count; i++)
                {
                    if (!chkSources.GetItemChecked(i)) continue;
                    var sourceDb = BuildDb(_sources[i]);
                    var tables = await sourceDb.GetTablesAsync();
                    foreach (var t in tables)
                    {
                        int idx = listTables.Items.Add(new TableEntry(_sources)
                        {
                            TableName = t,
                            SourceIndex = i
                        });
                        listTables.SetItemChecked(idx, true);
                    }
                }

                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = $"✅ Verbunden! {listTables.Items.Count} Tabellen gefunden.";
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "❌ Fehler: " + ex.Message;
                MessageBox.Show(ex.Message, "Verbindungsfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnMigrate_Click(object sender, EventArgs e)
        {
            if (listTables.CheckedItems.Count == 0)
            {
                MessageBox.Show("Bitte mindestens eine Tabelle anhaken!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (chkTargets.CheckedItems.Count == 0)
            {
                MessageBox.Show("Bitte mindestens ein Ziel anhaken!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var checkedEntries = listTables.CheckedItems.Cast<TableEntry>().ToList();
            var duplicates = checkedEntries
                .GroupBy(e => e.TableName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0 && !chkRenameDuplicates.Checked)
            {
                MessageBox.Show(
                    $"Folgende Tabellennamen kommen in mehreren Quellen vor:\n\n" +
                    string.Join("\n", duplicates) +
                    "\n\nBitte 'Duplikate umbenennen' aktivieren oder die doppelten Tabellen abhaken.",
                    "Duplikate gefunden!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                btnMigrate.Enabled = false;
                btnConnect.Enabled = false;
                int total = checkedEntries.Count;
                int count = 0;

                foreach (var entry in checkedEntries)
                {
                    count++;
                    string targetName = entry.TableName;
                    if (chkRenameDuplicates.Checked && duplicates.Contains(entry.TableName))
                    {
                        string suffix = _sources[entry.SourceIndex].Database
                            .Split('/', '\\')[^1]
                            .Replace(".db", "").Replace(".sqlite", "");
                        targetName = $"{entry.TableName}_from_{suffix}";
                    }

                    var sourceDb = BuildDb(_sources[entry.SourceIndex]);
                    var data = await sourceDb.GetTableDataAsync(entry.TableName);

                    for (int i = 0; i < chkTargets.Items.Count; i++)
                    {
                        if (!chkTargets.GetItemChecked(i)) continue;
                        var target = _targets[i];
                        var db = BuildDb(target);

                        lblStatus.ForeColor = Color.DarkBlue;
                        lblStatus.Text = $"⏳ '{entry.TableName}' → '{targetName}' @ {target} ({count}/{total})";
                        Application.DoEvents();

                        await db.CreateTableIfNotExistsAsync(data, targetName);
                        if (rbReplace.Checked) await db.TruncateTableAsync(targetName);
                        await db.InsertDataAsync(targetName, data);
                    }
                }

                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = $"✅ Migration abgeschlossen! {count} Tabelle(n) migriert.";
                MessageBox.Show("Migration abgeschlossen!", "Fertig",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "❌ Fehler: " + ex.Message;
                MessageBox.Show(ex.Message, "Migrationsfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnMigrate.Enabled = true;
                btnConnect.Enabled = true;
            }
        }
    }
}