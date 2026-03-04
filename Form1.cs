using System;
using System.Windows.Forms;
using DataHeater.Helper;
using System.Threading.Tasks;

namespace DataHeater
{
    public partial class Form1 : Form
    {
        private SqliteDatabase sqliteDb;
        private MariaDbDatabase mariaDb;
        private bool _sqliteToMariaDb = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateDirectionUI()
        {
            if (_sqliteToMariaDb)
            {
                btnDirection.Text = "→";
                btnMigrate.Text = "Migrieren →";
                grpSqlite.Text = "SQLite (Quelle)";
                grpMariaDb.Text = "MariaDB (Ziel)";
            }
            else
            {
                btnDirection.Text = "←";
                btnMigrate.Text = "← Migrieren";
                grpSqlite.Text = "SQLite (Ziel)";
                grpMariaDb.Text = "MariaDB (Quelle)";
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog();
            dlg.Filter = "SQLite Datenbank (*.db;*.sqlite)|*.db;*.sqlite|Alle Dateien (*.*)|*.*";
            dlg.Title = "SQLite Datei auswählen";
            if (dlg.ShowDialog() == DialogResult.OK)
                txtSqlitePath.Text = dlg.FileName;
        }

        private void btnDirection_Click(object sender, EventArgs e)
        {
            _sqliteToMariaDb = !_sqliteToMariaDb;
            UpdateDirectionUI();
            listTables.Items.Clear();
            lblStatus.Text = "Richtung geändert – bitte neu verbinden.";
            lblStatus.ForeColor = Color.DarkOrange;
        }

        private string BuildMariaDbConnectionString() =>
            $"Server={txtHost.Text};Port={txtPort.Text};Database={txtDatabase.Text};Uid={txtUsername.Text};Pwd={txtPassword.Text};";

        private string BuildSqliteConnectionString() =>
            $"Data Source={txtSqlitePath.Text};";

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                lblStatus.ForeColor = Color.Gray;
                lblStatus.Text = "Verbinde...";
                sqliteDb = new SqliteDatabase(BuildSqliteConnectionString());
                mariaDb = new MariaDbDatabase(BuildMariaDbConnectionString());
                listTables.Items.Clear();

                var tables = _sqliteToMariaDb
                    ? await sqliteDb.GetTablesAsync()
                    : await mariaDb.GetTablesAsync();

                foreach (var t in tables)
                    listTables.Items.Add(t);

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
            if (listTables.SelectedItems.Count == 0)
            {
                MessageBox.Show("Bitte mindestens eine Tabelle auswählen!", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sqlitePath = txtSqlitePath.Text;

            if (!_sqliteToMariaDb)
            {
                using var dlg = new SaveFileDialog();
                dlg.Filter = "SQLite Datenbank (*.db)|*.db";
                dlg.Title = "SQLite Zieldatei wählen";
                dlg.FileName = "export.db";
                if (dlg.ShowDialog() != DialogResult.OK) return;
                sqlitePath = dlg.FileName;
                sqliteDb = new SqliteDatabase($"Data Source={sqlitePath};");
            }

            try
            {
                btnMigrate.Enabled = false;
                btnConnect.Enabled = false;
                int total = listTables.SelectedItems.Count;
                int count = 0;

                foreach (var selected in listTables.SelectedItems)
                {
                    string tableName = selected.ToString();
                    lblStatus.ForeColor = Color.DarkBlue;
                    lblStatus.Text = $"⏳ Migriere '{tableName}'... ({count + 1}/{total})";
                    Application.DoEvents();

                    if (_sqliteToMariaDb)
                    {
                        var data = await sqliteDb.GetTableDataAsync(tableName);
                        await mariaDb.CreateTableIfNotExistsAsync(data, tableName);
                        if (rbReplace.Checked)
                        {
                            lblStatus.Text = $"🗑️ Lösche '{tableName}'... ({count + 1}/{total})";
                            Application.DoEvents();
                            await mariaDb.TruncateTableAsync(tableName);
                        }
                        await mariaDb.InsertDataAsync(tableName, data);
                    }
                    else
                    {
                        var data = await mariaDb.GetTableDataAsync(tableName);
                        await sqliteDb.CreateTableIfNotExistsAsync(data, tableName);
                        if (rbReplace.Checked)
                        {
                            lblStatus.Text = $"🗑️ Lösche '{tableName}'... ({count + 1}/{total})";
                            Application.DoEvents();
                            await sqliteDb.TruncateTableAsync(tableName);
                        }
                        await sqliteDb.InsertDataAsync(tableName, data);
                    }
                    count++;
                }

                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = $"✅ Fertig! {count} Tabelle(n) migriert.";

                if (!_sqliteToMariaDb)
                {
                    var result = MessageBox.Show(
                        $"Migration abgeschlossen!\nGespeichert unter:\n{sqlitePath}\n\nOrdner öffnen?",
                        "Fertig", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (result == DialogResult.Yes)
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{sqlitePath}\"");
                }
                else
                {
                    MessageBox.Show("Migration abgeschlossen!", "Fertig",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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