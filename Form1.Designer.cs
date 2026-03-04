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
            grpSqlite = new GroupBox();
            txtSqlitePath = new TextBox();
            btnBrowse = new Button();
            btnDirection = new Button();
            grpMariaDb = new GroupBox();
            lblHost = new Label();
            txtHost = new TextBox();
            lblPort = new Label();
            txtPort = new TextBox();
            lblDatabase = new Label();
            txtDatabase = new TextBox();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblPassword = new Label();
            txtPassword = new TextBox();
            grpTables = new GroupBox();
            listTables = new ListBox();
            btnConnect = new Button();
            btnMigrate = new Button();
            lblStatus = new Label();
            grpMode = new GroupBox();
            rbInsert = new RadioButton();
            rbReplace = new RadioButton();
            grpSqlite.SuspendLayout();
            grpMariaDb.SuspendLayout();
            grpTables.SuspendLayout();
            grpMode.SuspendLayout();
            SuspendLayout();

            // grpSqlite
            grpSqlite.Controls.Add(txtSqlitePath);
            grpSqlite.Controls.Add(btnBrowse);
            grpSqlite.Location = new Point(10, 10);
            grpSqlite.Name = "grpSqlite";
            grpSqlite.Size = new Size(320, 65);
            grpSqlite.TabIndex = 0;
            grpSqlite.TabStop = false;
            grpSqlite.Text = "SQLite (Quelle)";

            // txtSqlitePath
            txtSqlitePath.Location = new Point(10, 28);
            txtSqlitePath.Name = "txtSqlitePath";
            txtSqlitePath.Size = new Size(230, 23);
            txtSqlitePath.TabIndex = 0;
            txtSqlitePath.PlaceholderText = "Dateipfad...";

            // btnBrowse
            btnBrowse.Location = new Point(248, 27);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(60, 25);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "📂 ...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;

            // btnDirection
            btnDirection.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnDirection.Location = new Point(340, 25);
            btnDirection.Name = "btnDirection";
            btnDirection.Size = new Size(60, 40);
            btnDirection.TabIndex = 1;
            btnDirection.Text = "→";
            btnDirection.UseVisualStyleBackColor = true;
            btnDirection.Click += btnDirection_Click;

            // grpMariaDb
            grpMariaDb.Controls.Add(lblHost);
            grpMariaDb.Controls.Add(txtHost);
            grpMariaDb.Controls.Add(lblPort);
            grpMariaDb.Controls.Add(txtPort);
            grpMariaDb.Controls.Add(lblDatabase);
            grpMariaDb.Controls.Add(txtDatabase);
            grpMariaDb.Controls.Add(lblUsername);
            grpMariaDb.Controls.Add(txtUsername);
            grpMariaDb.Controls.Add(lblPassword);
            grpMariaDb.Controls.Add(txtPassword);
            grpMariaDb.Location = new Point(410, 10);
            grpMariaDb.Name = "grpMariaDb";
            grpMariaDb.Size = new Size(460, 155);
            grpMariaDb.TabIndex = 2;
            grpMariaDb.TabStop = false;
            grpMariaDb.Text = "MariaDB (Ziel)";

            // lblHost
            lblHost.AutoSize = true;
            lblHost.Location = new Point(10, 28);
            lblHost.Text = "Host:";

            // txtHost
            txtHost.Location = new Point(65, 25);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(140, 23);
            txtHost.Text = "localhost";

            // lblPort
            lblPort.AutoSize = true;
            lblPort.Location = new Point(215, 28);
            lblPort.Text = "Port:";

            // txtPort
            txtPort.Location = new Point(255, 25);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(60, 23);
            txtPort.Text = "3306";

            // lblDatabase
            lblDatabase.AutoSize = true;
            lblDatabase.Location = new Point(10, 60);
            lblDatabase.Text = "Datenbank:";

            // txtDatabase
            txtDatabase.Location = new Point(100, 57);
            txtDatabase.Name = "txtDatabase";
            txtDatabase.Size = new Size(200, 23);
            txtDatabase.PlaceholderText = "datenbankname";

            // lblUsername
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(10, 93);
            lblUsername.Text = "Benutzer:";

            // txtUsername
            txtUsername.Location = new Point(100, 90);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(200, 23);
            txtUsername.Text = "root";

            // lblPassword
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(10, 126);
            lblPassword.Text = "Passwort:";

            // txtPassword
            txtPassword.Location = new Point(100, 123);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(200, 23);
            txtPassword.PasswordChar = '●';

            // grpTables
            grpTables.Controls.Add(listTables);
            grpTables.Location = new Point(10, 175);
            grpTables.Name = "grpTables";
            grpTables.Size = new Size(860, 200);
            grpTables.TabIndex = 3;
            grpTables.TabStop = false;
            grpTables.Text = "Tabellen (Mehrfachauswahl mit Strg+Klick)";

            // listTables
            listTables.FormattingEnabled = true;
            listTables.Location = new Point(10, 22);
            listTables.Name = "listTables";
            listTables.SelectionMode = SelectionMode.MultiExtended;
            listTables.Size = new Size(838, 168);
            listTables.TabIndex = 0;

            // grpMode
            grpMode.Controls.Add(rbInsert);
            grpMode.Controls.Add(rbReplace);
            grpMode.Location = new Point(10, 385);
            grpMode.Name = "grpMode";
            grpMode.Size = new Size(310, 50);
            grpMode.TabIndex = 6;
            grpMode.TabStop = false;
            grpMode.Text = "Migrationsmodus";

            // rbInsert
            rbInsert.AutoSize = true;
            rbInsert.Checked = true;
            rbInsert.Location = new Point(10, 22);
            rbInsert.Name = "rbInsert";
            rbInsert.TabIndex = 0;
            rbInsert.Text = "Nur einfügen (INSERT)";

            // rbReplace
            rbReplace.AutoSize = true;
            rbReplace.Location = new Point(165, 22);
            rbReplace.Name = "rbReplace";
            rbReplace.TabIndex = 1;
            rbReplace.Text = "Löschen + neu einfügen";

            // btnConnect
            btnConnect.Location = new Point(330, 395);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(130, 35);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "🔌 Verbinden";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;

            // btnMigrate
            btnMigrate.BackColor = Color.SteelBlue;
            btnMigrate.ForeColor = Color.White;
            btnMigrate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnMigrate.Location = new Point(470, 395);
            btnMigrate.Name = "btnMigrate";
            btnMigrate.Size = new Size(130, 35);
            btnMigrate.TabIndex = 5;
            btnMigrate.Text = "Migrieren →";
            btnMigrate.UseVisualStyleBackColor = false;
            btnMigrate.Click += btnMigrate_Click;

            // lblStatus
            lblStatus.AutoSize = false;
            lblStatus.Location = new Point(615, 405);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(255, 20);
            lblStatus.Text = "Bereit.";
            lblStatus.ForeColor = Color.Gray;

            // Form1
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(880, 445);
            Controls.Add(grpSqlite);
            Controls.Add(btnDirection);
            Controls.Add(grpMariaDb);
            Controls.Add(grpTables);
            Controls.Add(grpMode);
            Controls.Add(btnConnect);
            Controls.Add(btnMigrate);
            Controls.Add(lblStatus);
            Name = "Form1";
            Text = "DataHeater – SQLite ↔ MariaDB Migration";
            grpSqlite.ResumeLayout(false);
            grpMariaDb.ResumeLayout(false);
            grpTables.ResumeLayout(false);
            grpMode.ResumeLayout(false);
            ResumeLayout(false);
        }

        private GroupBox grpSqlite;
        private TextBox txtSqlitePath;
        private Button btnBrowse;
        private Button btnDirection;
        private GroupBox grpMariaDb;
        private Label lblHost, lblPort, lblDatabase, lblUsername, lblPassword;
        private TextBox txtHost, txtPort, txtDatabase, txtUsername, txtPassword;
        private GroupBox grpTables;
        private ListBox listTables;
        private Button btnConnect;
        private Button btnMigrate;
        private Label lblStatus;
        private GroupBox grpMode;
        private RadioButton rbInsert;
        private RadioButton rbReplace;
    }
}