using System.Drawing;
using System.Windows.Forms;

namespace DataHeater
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            Icon = (Icon)resources.GetObject("$this.Icon");
            grpSource = new GroupBox();
            lblSrcType = new Label();
            cmbSrcType = new ComboBox();
            pnlSrcSqlite = new Panel();
            txtSrcPath = new TextBox();
            btnSrcBrowse = new Button();
            pnlSrcDb = new Panel();
            lblSrcHost = new Label(); txtSrcHost = new TextBox();
            lblSrcPort = new Label(); txtSrcPort = new TextBox();
            lblSrcDatabase = new Label(); txtSrcDatabase = new TextBox();
            lblSrcUsername = new Label(); txtSrcUsername = new TextBox();
            lblSrcPassword = new Label(); txtSrcPassword = new TextBox();
            btnAddSource = new Button();
            btnRemoveSource = new Button();
            btnEditSource = new Button();
            chkSources = new CheckedListBox();

            btnDirection = new Button();

            grpTargets = new GroupBox();
            lblTgtType = new Label();
            cmbTgtType = new ComboBox();
            pnlTgtSqlite = new Panel();
            txtTgtPath = new TextBox();
            btnTgtBrowse = new Button();
            pnlTgtDb = new Panel();
            lblTgtHost = new Label(); txtTgtHost = new TextBox();
            lblTgtPort = new Label(); txtTgtPort = new TextBox();
            lblTgtDatabase = new Label(); txtTgtDatabase = new TextBox();
            lblTgtUsername = new Label(); txtTgtUsername = new TextBox();
            lblTgtPassword = new Label(); txtTgtPassword = new TextBox();
            btnAddTarget = new Button();
            btnRemoveTarget = new Button();
            btnEditTarget = new Button();
            chkTargets = new CheckedListBox();

            grpTables = new GroupBox();
            listTables = new CheckedListBox();
            btnCheckAll = new Button();
            btnCheckNone = new Button();
            chkRenameDuplicates = new CheckBox();
            chkCreateDb = new CheckBox();

            grpMode = new GroupBox();
            rbInsert = new RadioButton();
            rbReplace = new RadioButton();
            btnConnect = new Button();
            btnMigrate = new Button();
            lblStatus = new Label();

            // ================================================================
            //  grpSource
            // ================================================================
            grpSource.Text = "Quelle";
            grpSource.Location = new Point(10, 10);
            grpSource.Size = new Size(400, 278);
            grpSource.TabStop = false;
            grpSource.Controls.AddRange(new System.Windows.Forms.Control[]
            { lblSrcType, cmbSrcType, pnlSrcSqlite, pnlSrcDb,
              btnAddSource, btnRemoveSource, btnEditSource, chkSources });

            lblSrcType.AutoSize = true;
            lblSrcType.Location = new Point(10, 28);
            lblSrcType.Text = "Typ:";

            cmbSrcType.Location = new Point(48, 25);
            cmbSrcType.Size = new Size(130, 23);
            cmbSrcType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSrcType.Items.AddRange(new object[] { "SQLite", "MariaDB", "PostgreSQL", "Oracle" });
            cmbSrcType.SelectedIndex = 0;
            cmbSrcType.SelectedIndexChanged += cmbSrcType_SelectedIndexChanged;

            // pnlSrcSqlite
            pnlSrcSqlite.Location = new Point(10, 56);
            pnlSrcSqlite.Size = new Size(378, 28);
            txtSrcPath.Location = new Point(0, 2); txtSrcPath.Size = new Size(286, 23);
            txtSrcPath.PlaceholderText = "Dateipfad …";
            btnSrcBrowse.Location = new Point(293, 1); btnSrcBrowse.Size = new Size(80, 25);
            btnSrcBrowse.Text = "📂 ..."; btnSrcBrowse.UseVisualStyleBackColor = true;
            btnSrcBrowse.Click += btnSrcBrowse_Click;
            pnlSrcSqlite.Controls.AddRange(new System.Windows.Forms.Control[] { txtSrcPath, btnSrcBrowse });

            // pnlSrcDb
            pnlSrcDb.Location = new Point(10, 56);
            pnlSrcDb.Size = new Size(378, 125);
            LayoutDbPanel(pnlSrcDb,
                lblSrcHost, txtSrcHost, lblSrcPort, txtSrcPort,
                lblSrcDatabase, txtSrcDatabase, lblSrcUsername, txtSrcUsername,
                lblSrcPassword, txtSrcPassword);

            Btn(btnAddSource, 10, 192, 110, "➕ Hinzufügen", btnAddSource_Click);
            Btn(btnRemoveSource, 128, 192, 110, "➖ Entfernen", btnRemoveSource_Click);
            Btn(btnEditSource, 246, 192, 110, "✏️ Bearbeiten", btnEditSource_Click);

            chkSources.FormattingEnabled = true;
            chkSources.CheckOnClick = true;
            chkSources.Location = new Point(10, 227);
            chkSources.Size = new Size(378, 42);

            // ================================================================
            //  btnDirection
            // ================================================================
            btnDirection.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnDirection.Location = new Point(418, 120);
            btnDirection.Size = new Size(54, 40);
            btnDirection.Text = "⇄";
            btnDirection.UseVisualStyleBackColor = true;
            btnDirection.Click += btnDirection_Click;

            // ================================================================
            //  grpTargets
            // ================================================================
            grpTargets.Text = "Ziele";
            grpTargets.Location = new Point(480, 10);
            grpTargets.Size = new Size(410, 278);
            grpTargets.TabStop = false;
            grpTargets.Controls.AddRange(new System.Windows.Forms.Control[]
            { lblTgtType, cmbTgtType, pnlTgtSqlite, pnlTgtDb,
              btnAddTarget, btnRemoveTarget, btnEditTarget, chkTargets });

            lblTgtType.AutoSize = true;
            lblTgtType.Location = new Point(10, 28);
            lblTgtType.Text = "Typ:";

            cmbTgtType.Location = new Point(48, 25);
            cmbTgtType.Size = new Size(130, 23);
            cmbTgtType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTgtType.Items.AddRange(new object[] { "SQLite", "MariaDB", "PostgreSQL", "Oracle" });
            cmbTgtType.SelectedIndex = 1;   // MariaDB default
            cmbTgtType.SelectedIndexChanged += cmbTgtType_SelectedIndexChanged;

            // pnlTgtSqlite
            pnlTgtSqlite.Location = new Point(10, 56);
            pnlTgtSqlite.Size = new Size(388, 28);
            txtTgtPath.Location = new Point(0, 2); txtTgtPath.Size = new Size(296, 23);
            txtTgtPath.PlaceholderText = "Dateipfad …";
            btnTgtBrowse.Location = new Point(303, 1); btnTgtBrowse.Size = new Size(80, 25);
            btnTgtBrowse.Text = "📂 ..."; btnTgtBrowse.UseVisualStyleBackColor = true;
            btnTgtBrowse.Click += btnTgtBrowse_Click;
            pnlTgtSqlite.Controls.AddRange(new System.Windows.Forms.Control[] { txtTgtPath, btnTgtBrowse });

            // pnlTgtDb
            pnlTgtDb.Location = new Point(10, 56);
            pnlTgtDb.Size = new Size(388, 125);
            LayoutDbPanel(pnlTgtDb,
                lblTgtHost, txtTgtHost, lblTgtPort, txtTgtPort,
                lblTgtDatabase, txtTgtDatabase, lblTgtUsername, txtTgtUsername,
                lblTgtPassword, txtTgtPassword);

            Btn(btnAddTarget, 10, 192, 110, "➕ Hinzufügen", btnAddTarget_Click);
            Btn(btnRemoveTarget, 128, 192, 110, "➖ Entfernen", btnRemoveTarget_Click);
            Btn(btnEditTarget, 246, 192, 110, "✏️ Bearbeiten", btnEditTarget_Click);

            chkTargets.FormattingEnabled = true;
            chkTargets.CheckOnClick = true;
            chkTargets.Location = new Point(10, 227);
            chkTargets.Size = new Size(388, 42);

            // ================================================================
            //  grpTables
            // ================================================================
            grpTables.Text = "Tabellen";
            grpTables.Location = new Point(10, 298);
            grpTables.Size = new Size(880, 202);
            grpTables.TabStop = false;
            grpTables.Controls.AddRange(new System.Windows.Forms.Control[]
            { listTables, btnCheckAll, btnCheckNone, chkRenameDuplicates, chkCreateDb });

            listTables.FormattingEnabled = true;
            listTables.CheckOnClick = false;
            listTables.Location = new Point(10, 22);
            listTables.Size = new Size(858, 108);

            Btn(btnCheckAll, 10, 138, 100, "☑ Alle", btnCheckAll_Click);
            Btn(btnCheckNone, 118, 138, 100, "☐ Keine", btnCheckNone_Click);

            chkRenameDuplicates.AutoSize = true;
            chkRenameDuplicates.Location = new Point(230, 141);
            chkRenameDuplicates.Text = "⚠️ Duplikate automatisch umbenennen";
            chkRenameDuplicates.Font = new Font("Segoe UI", 8.5F);
            chkRenameDuplicates.ForeColor = Color.DarkOrange;

            chkCreateDb.AutoSize = true;
            chkCreateDb.Checked = true;
            chkCreateDb.Location = new Point(10, 167);
            chkCreateDb.Text = "🗄️ Datenbank automatisch erstellen falls nicht vorhanden";
            chkCreateDb.Font = new Font("Segoe UI", 8.5F);

            // ================================================================
            //  grpMode + Buttons + Status
            // ================================================================
            grpMode.Text = "Migrationsmodus";
            grpMode.Location = new Point(10, 510);
            grpMode.Size = new Size(310, 46);
            grpMode.TabStop = false;
            grpMode.Controls.AddRange(new System.Windows.Forms.Control[] { rbInsert, rbReplace });

            rbInsert.AutoSize = true; rbInsert.Checked = true;
            rbInsert.Location = new Point(10, 20); rbInsert.Text = "Nur einfügen (INSERT)";
            rbReplace.AutoSize = true;
            rbReplace.Location = new Point(175, 20); rbReplace.Text = "Löschen + neu";

            btnConnect.Location = new Point(330, 515);
            btnConnect.Size = new Size(130, 36);
            btnConnect.Text = "🔌 Verbinden";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;

            btnMigrate.BackColor = Color.SteelBlue;
            btnMigrate.ForeColor = Color.White;
            btnMigrate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMigrate.Location = new Point(470, 515);
            btnMigrate.Size = new Size(130, 36);
            btnMigrate.Text = "Migrieren →";
            btnMigrate.UseVisualStyleBackColor = false;
            btnMigrate.Click += btnMigrate_Click;

            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(615, 525);
            lblStatus.Size = new Size(275, 20);
            lblStatus.Text = "Bereit.";
            lblStatus.ForeColor = Color.Gray;

            // ================================================================
            //  Form
            // ================================================================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 565);
            Text = "DataHeater – Universal DB Migration";
            Controls.AddRange(new System.Windows.Forms.Control[]
            {
                grpSource, btnDirection, grpTargets,
                grpTables, grpMode, btnConnect, btnMigrate, lblStatus
            });
        }

        // ── Hilfsmethoden ──────────────────────────────────────────────────
        private static void LayoutDbPanel(Panel pnl,
            Label lHost, TextBox tHost, Label lPort, TextBox tPort,
            Label lDb, TextBox tDb, Label lUser, TextBox tUser,
            Label lPwd, TextBox tPwd)
        {
            L(lHost, 0, 5, "Host:"); T(tHost, 75, 3, 150, "localhost");
            L(lPort, 235, 5, "Port:"); T(tPort, 268, 3, 60, "3306");
            L(lDb, 0, 35, "Datenbank:"); T(tDb, 75, 33, pnl.Width - 80, "datenbankname");
            L(lUser, 0, 65, "Benutzer:"); T(tUser, 75, 63, pnl.Width - 80, "root");
            L(lPwd, 0, 95, "Passwort:"); T(tPwd, 75, 93, pnl.Width - 80, "");
            tPwd.PasswordChar = '●';
            tDb.PlaceholderText = "datenbankname";
            pnl.Controls.AddRange(new System.Windows.Forms.Control[]
                { lHost, tHost, lPort, tPort, lDb, tDb, lUser, tUser, lPwd, tPwd });

            static void L(Label l, int x, int y, string txt)
            { l.AutoSize = true; l.Location = new Point(x, y); l.Text = txt; }
            static void T(TextBox t, int x, int y, int w, string def)
            { t.Location = new Point(x, y); t.Size = new Size(w, 23); if (!string.IsNullOrEmpty(def)) t.Text = def; }
        }

        private static void Btn(Button b, int x, int y, int w, string text,
            System.EventHandler handler)
        {
            b.Location = new Point(x, y); b.Size = new Size(w, 27);
            b.Text = text;
            b.UseVisualStyleBackColor = true;
            b.Click += handler;
        }

        // ── Felder ─────────────────────────────────────────────────────────
        private GroupBox grpSource;
        private Label lblSrcType;
        private ComboBox cmbSrcType;
        private Panel pnlSrcSqlite;
        private TextBox txtSrcPath;
        private Button btnSrcBrowse;
        private Panel pnlSrcDb;
        private Label lblSrcHost, lblSrcPort, lblSrcDatabase, lblSrcUsername, lblSrcPassword;
        private TextBox txtSrcHost, txtSrcPort, txtSrcDatabase, txtSrcUsername, txtSrcPassword;
        private Button btnAddSource, btnRemoveSource, btnEditSource;
        private CheckedListBox chkSources;
        private Button btnDirection;
        private GroupBox grpTargets;
        private Label lblTgtType;
        private ComboBox cmbTgtType;
        private Panel pnlTgtSqlite;
        private TextBox txtTgtPath;
        private Button btnTgtBrowse;
        private Panel pnlTgtDb;
        private Label lblTgtHost, lblTgtPort, lblTgtDatabase, lblTgtUsername, lblTgtPassword;
        private TextBox txtTgtHost, txtTgtPort, txtTgtDatabase, txtTgtUsername, txtTgtPassword;
        private Button btnAddTarget, btnRemoveTarget, btnEditTarget;
        private CheckedListBox chkTargets;
        private GroupBox grpTables;
        private CheckedListBox listTables;
        private Button btnCheckAll, btnCheckNone;
        private CheckBox chkRenameDuplicates;
        private CheckBox chkCreateDb;
        private GroupBox grpMode;
        private RadioButton rbInsert, rbReplace;
        private Button btnConnect, btnMigrate;
        private Label lblStatus;
    }
}