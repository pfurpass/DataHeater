# 🔥 DataHeater

**DataHeater** is a simple Windows desktop tool to migrate data between **SQLite** and **MariaDB/MySQL** databases — in both directions.

---

## ✨ Features

- 📂 **Browse** for your SQLite file with a file picker
- 🔌 **Connect** to MariaDB/MySQL with individual fields (Host, Port, Database, User, Password)
- ↔️ **Switch migration direction** — SQLite → MariaDB or MariaDB → SQLite
- ☑️ **Multi-table selection** — migrate one or multiple tables at once
- 🔁 **Migration modes:**
  - `INSERT only` — append data, keep existing rows
  - `DELETE + INSERT` — wipe the table first, then insert fresh
- 💾 **SQLite export** — when migrating to SQLite, a Save dialog lets you choose where to save the output file
- 📁 **Open folder** after export — jump straight to the output file

---

## 🖥️ Requirements

- Windows 10 / 11
- No .NET installation needed (self-contained EXE)
- A running MariaDB or MySQL server (local or remote)

---

## 🚀 Installation

1. Download the latest `DataHeater.exe` from [Releases](../../releases)
2. Double-click — no installation required

---

## 📖 Usage

1. **Select your SQLite file** using the `📂` button
2. **Fill in MariaDB connection details** (Host, Port, Database, User, Password)
3. Click **🔌 Verbinden** to connect and load the table list
4. **Select one or more tables** (Ctrl+Click for multi-select)
5. Choose your **migration mode** (Insert only / Delete + Insert)
6. Click **Migrieren** — done!

To flip the direction, click the **→** arrow button between the two panels.

---

## 🛠️ Built With

- [.NET 8 / Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
- [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite)
- [MySql.Data](https://www.nuget.org/packages/MySql.Data)

---

## 📦 Build from Source

```bash
git clone https://github.com/YOURNAME/DataHeater.git
cd DataHeater
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output EXE will be in:
```
bin\Release\net8.0-windows\win-x64\publish\
```

---

## 📄 License

MIT — free to use, modify and distribute.
