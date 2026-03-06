namespace DataHeater
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            grpSource = new GroupBox();
            lblSrcType = new Label();
            cmbSrcType = new ComboBox();
            pnlSrcSqlite = new Panel();
            txtSrcPath = new TextBox();
            btnSrcBrowse = new Button();
            pnlSrcDb = new Panel();
            lblSrcHost = new Label();
            txtSrcHost = new TextBox();
            lblSrcPort = new Label();
            txtSrcPort = new TextBox();
            lblSrcDatabase = new Label();
            txtSrcDatabase = new TextBox();
            lblSrcUsername = new Label();
            txtSrcUsername = new TextBox();
            lblSrcPassword = new Label();
            txtSrcPassword = new TextBox();
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
            lblTgtHost = new Label();
            txtTgtHost = new TextBox();
            lblTgtPort = new Label();
            txtTgtPort = new TextBox();
            lblTgtDatabase = new Label();
            txtTgtDatabase = new TextBox();
            lblTgtUsername = new Label();
            txtTgtUsername = new TextBox();
            lblTgtPassword = new Label();
            txtTgtPassword = new TextBox();
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

            grpSource.SuspendLayout();
            pnlSrcSqlite.SuspendLayout();
            pnlSrcDb.SuspendLayout();
            grpTargets.SuspendLayout();
            pnlTgtSqlite.SuspendLayout();
            pnlTgtDb.SuspendLayout();
            grpTables.SuspendLayout();
            grpMode.SuspendLayout();
            SuspendLayout();

            // grpSource
            grpSource.Controls.Add(lblSrcType); grpSource.Controls.Add(cmbSrcType);
            grpSource.Controls.Add(pnlSrcSqlite); grpSource.Controls.Add(pnlSrcDb);
            grpSource.Controls.Add(btnAddSource); grpSource.Controls.Add(btnRemoveSource);
            grpSource.Controls.Add(btnEditSource);
            grpSource.Controls.Add(chkSources);
            grpSource.Location = new Point(10, 10); grpSource.Size = new Size(400, 275);
            grpSource.TabStop = false; grpSource.Text = "Quelle";

            lblSrcType.AutoSize = true; lblSrcType.Location = new Point(10, 28); lblSrcType.Text = "Typ:";
            cmbSrcType.Location = new Point(48, 25); cmbSrcType.Size = new Size(130, 23);
            cmbSrcType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSrcType.Items.AddRange(new object[] { "SQLite", "MariaDB", "PostgreSQL" });
            cmbSrcType.SelectedIndex = 0;
            cmbSrcType.SelectedIndexChanged += cmbSrcType_SelectedIndexChanged;

            pnlSrcSqlite.Controls.Add(txtSrcPath); pnlSrcSqlite.Controls.Add(btnSrcBrowse);
            pnlSrcSqlite.Location = new Point(10, 55); pnlSrcSqlite.Size = new Size(378, 30); pnlSrcSqlite.Visible = true;
            txtSrcPath.Location = new Point(0, 3); txtSrcPath.Size = new Size(285, 23); txtSrcPath.PlaceholderText = "Dateipfad...";
            btnSrcBrowse.Location = new Point(293, 2); btnSrcBrowse.Size = new Size(80, 25); btnSrcBrowse.Text = "📂 ...";
            btnSrcBrowse.UseVisualStyleBackColor = true; btnSrcBrowse.Click += btnSrcBrowse_Click;

            pnlSrcDb.Controls.Add(lblSrcHost); pnlSrcDb.Controls.Add(txtSrcHost);
            pnlSrcDb.Controls.Add(lblSrcPort); pnlSrcDb.Controls.Add(txtSrcPort);
            pnlSrcDb.Controls.Add(lblSrcDatabase); pnlSrcDb.Controls.Add(txtSrcDatabase);
            pnlSrcDb.Controls.Add(lblSrcUsername); pnlSrcDb.Controls.Add(txtSrcUsername);
            pnlSrcDb.Controls.Add(lblSrcPassword); pnlSrcDb.Controls.Add(txtSrcPassword);
            pnlSrcDb.Location = new Point(10, 55); pnlSrcDb.Size = new Size(378, 125); pnlSrcDb.Visible = false;

            lblSrcHost.AutoSize = true; lblSrcHost.Location = new Point(0, 5); lblSrcHost.Text = "Host:";
            txtSrcHost.Location = new Point(75, 3); txtSrcHost.Size = new Size(150, 23); txtSrcHost.Text = "localhost";
            lblSrcPort.AutoSize = true; lblSrcPort.Location = new Point(235, 5); lblSrcPort.Text = "Port:";
            txtSrcPort.Location = new Point(268, 3); txtSrcPort.Size = new Size(60, 23); txtSrcPort.Text = "3306";
            lblSrcDatabase.AutoSize = true; lblSrcDatabase.Location = new Point(0, 35); lblSrcDatabase.Text = "Datenbank:";
            txtSrcDatabase.Location = new Point(75, 33); txtSrcDatabase.Size = new Size(295, 23); txtSrcDatabase.PlaceholderText = "datenbankname";
            lblSrcUsername.AutoSize = true; lblSrcUsername.Location = new Point(0, 65); lblSrcUsername.Text = "Benutzer:";
            txtSrcUsername.Location = new Point(75, 63); txtSrcUsername.Size = new Size(295, 23); txtSrcUsername.Text = "root";
            lblSrcPassword.AutoSize = true; lblSrcPassword.Location = new Point(0, 95); lblSrcPassword.Text = "Passwort:";
            txtSrcPassword.Location = new Point(75, 93); txtSrcPassword.Size = new Size(295, 23); txtSrcPassword.PasswordChar = '●';

            btnAddSource.Location = new Point(10, 190); btnAddSource.Size = new Size(110, 28); btnAddSource.Text = "➕ Hinzufügen";
            btnAddSource.UseVisualStyleBackColor = true; btnAddSource.Click += btnAddSource_Click;
            btnRemoveSource.Location = new Point(128, 190); btnRemoveSource.Size = new Size(110, 28); btnRemoveSource.Text = "➖ Entfernen";
            btnRemoveSource.UseVisualStyleBackColor = true; btnRemoveSource.Click += btnRemoveSource_Click;
            btnEditSource.Location = new Point(246, 190); btnEditSource.Size = new Size(110, 28); btnEditSource.Text = "✏️ Bearbeiten";
            btnEditSource.UseVisualStyleBackColor = true; btnEditSource.Click += btnEditSource_Click;

            chkSources.FormattingEnabled = true; chkSources.Location = new Point(10, 225);
            chkSources.Size = new Size(378, 42); chkSources.CheckOnClick = true;

            // btnDirection
            btnDirection.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnDirection.Location = new Point(418, 120); btnDirection.Size = new Size(54, 40); btnDirection.Text = "⇄";
            btnDirection.UseVisualStyleBackColor = true; btnDirection.Click += btnDirection_Click;

            // grpTargets
            grpTargets.Controls.Add(lblTgtType); grpTargets.Controls.Add(cmbTgtType);
            grpTargets.Controls.Add(pnlTgtSqlite); grpTargets.Controls.Add(pnlTgtDb);
            grpTargets.Controls.Add(btnAddTarget); grpTargets.Controls.Add(btnRemoveTarget);
            grpTargets.Controls.Add(btnEditTarget);
            grpTargets.Controls.Add(chkTargets);
            grpTargets.Location = new Point(480, 10); grpTargets.Size = new Size(410, 275);
            grpTargets.TabStop = false; grpTargets.Text = "Ziele";

            lblTgtType.AutoSize = true; lblTgtType.Location = new Point(10, 28); lblTgtType.Text = "Typ:";
            cmbTgtType.Location = new Point(48, 25); cmbTgtType.Size = new Size(130, 23);
            cmbTgtType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTgtType.Items.AddRange(new object[] { "SQLite", "MariaDB", "PostgreSQL" });
            cmbTgtType.SelectedIndex = 1;
            cmbTgtType.SelectedIndexChanged += cmbTgtType_SelectedIndexChanged;

            pnlTgtSqlite.Controls.Add(txtTgtPath); pnlTgtSqlite.Controls.Add(btnTgtBrowse);
            pnlTgtSqlite.Location = new Point(10, 55); pnlTgtSqlite.Size = new Size(388, 30); pnlTgtSqlite.Visible = false;
            txtTgtPath.Location = new Point(0, 3); txtTgtPath.Size = new Size(295, 23); txtTgtPath.PlaceholderText = "Dateipfad...";
            btnTgtBrowse.Location = new Point(303, 2); btnTgtBrowse.Size = new Size(80, 25); btnTgtBrowse.Text = "📂 ...";
            btnTgtBrowse.UseVisualStyleBackColor = true; btnTgtBrowse.Click += btnTgtBrowse_Click;

            pnlTgtDb.Controls.Add(lblTgtHost); pnlTgtDb.Controls.Add(txtTgtHost);
            pnlTgtDb.Controls.Add(lblTgtPort); pnlTgtDb.Controls.Add(txtTgtPort);
            pnlTgtDb.Controls.Add(lblTgtDatabase); pnlTgtDb.Controls.Add(txtTgtDatabase);
            pnlTgtDb.Controls.Add(lblTgtUsername); pnlTgtDb.Controls.Add(txtTgtUsername);
            pnlTgtDb.Controls.Add(lblTgtPassword); pnlTgtDb.Controls.Add(txtTgtPassword);
            pnlTgtDb.Location = new Point(10, 55); pnlTgtDb.Size = new Size(388, 125); pnlTgtDb.Visible = true;

            lblTgtHost.AutoSize = true; lblTgtHost.Location = new Point(0, 5); lblTgtHost.Text = "Host:";
            txtTgtHost.Location = new Point(75, 3); txtTgtHost.Size = new Size(150, 23); txtTgtHost.Text = "localhost";
            lblTgtPort.AutoSize = true; lblTgtPort.Location = new Point(235, 5); lblTgtPort.Text = "Port:";
            txtTgtPort.Location = new Point(268, 3); txtTgtPort.Size = new Size(60, 23); txtTgtPort.Text = "3306";
            lblTgtDatabase.AutoSize = true; lblTgtDatabase.Location = new Point(0, 35); lblTgtDatabase.Text = "Datenbank:";
            txtTgtDatabase.Location = new Point(75, 33); txtTgtDatabase.Size = new Size(305, 23); txtTgtDatabase.PlaceholderText = "datenbankname";
            lblTgtUsername.AutoSize = true; lblTgtUsername.Location = new Point(0, 65); lblTgtUsername.Text = "Benutzer:";
            txtTgtUsername.Location = new Point(75, 63); txtTgtUsername.Size = new Size(305, 23); txtTgtUsername.Text = "root";
            lblTgtPassword.AutoSize = true; lblTgtPassword.Location = new Point(0, 95); lblTgtPassword.Text = "Passwort:";
            txtTgtPassword.Location = new Point(75, 93); txtTgtPassword.Size = new Size(305, 23); txtTgtPassword.PasswordChar = '●';

            btnAddTarget.Location = new Point(10, 190); btnAddTarget.Size = new Size(110, 28); btnAddTarget.Text = "➕ Hinzufügen";
            btnAddTarget.UseVisualStyleBackColor = true; btnAddTarget.Click += btnAddTarget_Click;
            btnRemoveTarget.Location = new Point(128, 190); btnRemoveTarget.Size = new Size(110, 28); btnRemoveTarget.Text = "➖ Entfernen";
            btnRemoveTarget.UseVisualStyleBackColor = true; btnRemoveTarget.Click += btnRemoveTarget_Click;
            btnEditTarget.Location = new Point(246, 190); btnEditTarget.Size = new Size(110, 28); btnEditTarget.Text = "✏️ Bearbeiten";
            btnEditTarget.UseVisualStyleBackColor = true; btnEditTarget.Click += btnEditTarget_Click;

            chkTargets.FormattingEnabled = true; chkTargets.Location = new Point(10, 225);
            chkTargets.Size = new Size(388, 42); chkTargets.CheckOnClick = true;

            // grpTables
            grpTables.Controls.Add(listTables);
            grpTables.Controls.Add(btnCheckAll);
            grpTables.Controls.Add(btnCheckNone);
            grpTables.Controls.Add(chkRenameDuplicates);
            grpTables.Controls.Add(chkCreateDb);
            grpTables.Location = new Point(10, 295);
            grpTables.Size = new Size(880, 205);
            grpTables.TabStop = false;
            grpTables.Text = "Tabellen";

            listTables.FormattingEnabled = true;
            listTables.Location = new Point(10, 22);
            listTables.Size = new Size(858, 110);
            listTables.CheckOnClick = false;

            btnCheckAll.Location = new Point(10, 138); btnCheckAll.Size = new Size(100, 26); btnCheckAll.Text = "☑ Alle";
            btnCheckAll.UseVisualStyleBackColor = true; btnCheckAll.Click += btnCheckAll_Click;
            btnCheckNone.Location = new Point(118, 138); btnCheckNone.Size = new Size(100, 26); btnCheckNone.Text = "☐ Keine";
            btnCheckNone.UseVisualStyleBackColor = true; btnCheckNone.Click += btnCheckNone_Click;

            chkRenameDuplicates.AutoSize = true;
            chkRenameDuplicates.Location = new Point(230, 142);
            chkRenameDuplicates.Text = "⚠️ Duplikate automatisch umbenennen – sonst Fehler bei Duplikaten";
            chkRenameDuplicates.Font = new Font("Segoe UI", 8.5F);
            chkRenameDuplicates.ForeColor = Color.DarkOrange;

            chkCreateDb.AutoSize = true;
            chkCreateDb.Checked = true;
            chkCreateDb.Location = new Point(10, 170);
            chkCreateDb.Text = "🗄️ Datenbank automatisch erstellen falls nicht vorhanden";
            chkCreateDb.Font = new Font("Segoe UI", 8.5F);

            // grpMode
            grpMode.Controls.Add(rbInsert); grpMode.Controls.Add(rbReplace);
            grpMode.Location = new Point(10, 510); grpMode.Size = new Size(310, 50);
            grpMode.TabStop = false; grpMode.Text = "Migrationsmodus";

            rbInsert.AutoSize = true; rbInsert.Checked = true; rbInsert.Location = new Point(10, 22); rbInsert.Text = "Nur einfügen (INSERT)";
            rbReplace.AutoSize = true; rbReplace.Location = new Point(175, 22); rbReplace.Text = "Löschen + neu";

            btnConnect.Location = new Point(330, 518); btnConnect.Size = new Size(130, 35); btnConnect.Text = "🔌 Verbinden";
            btnConnect.UseVisualStyleBackColor = true; btnConnect.Click += btnConnect_Click;

            btnMigrate.BackColor = Color.SteelBlue; btnMigrate.ForeColor = Color.White;
            btnMigrate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMigrate.Location = new Point(470, 518); btnMigrate.Size = new Size(130, 35); btnMigrate.Text = "Migrieren →";
            btnMigrate.UseVisualStyleBackColor = false; btnMigrate.Click += btnMigrate_Click;

            lblStatus.AutoSize = false; lblStatus.Location = new Point(615, 528);
            lblStatus.Size = new Size(275, 20); lblStatus.Text = "Bereit."; lblStatus.ForeColor = Color.Gray;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 565);
            Controls.Add(grpSource); Controls.Add(btnDirection); Controls.Add(grpTargets);
            Controls.Add(grpTables); Controls.Add(grpMode);
            Controls.Add(btnConnect); Controls.Add(btnMigrate); Controls.Add(lblStatus);
            Text = "DataHeater – Universal DB Migration";
            grpSource.ResumeLayout(false); pnlSrcSqlite.ResumeLayout(false); pnlSrcDb.ResumeLayout(false);
            grpTargets.ResumeLayout(false); pnlTgtSqlite.ResumeLayout(false); pnlTgtDb.ResumeLayout(false);
            grpTables.ResumeLayout(false); grpMode.ResumeLayout(false);
            ResumeLayout(false);
        }

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