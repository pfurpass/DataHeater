using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DataHeater.Helper
{
    internal static class Updater
    {
        // ── Konfiguration ─────────────────────────────────────────────────
        private const string UpdateXmlUrl = "https://dataheater.phillips-network.work/update.xml";
        // ─────────────────────────────────────────────────────────────────

        private static readonly string _skipFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DataHeater", "skip_version.txt");

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        static Updater()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"DataHeater/{(v != null ? $"{v.Major}.{v.Minor}" : "1.0")}");
        }

        public static async void CheckAsync(bool isEnglish = false,
            params Control[] disableDuringDownload)
        {
            try
            {
                // XML laden
                string xml = await _http.GetStringAsync(UpdateXmlUrl);
                var doc = XDocument.Parse(xml);

                string latestStr = doc.Root?.Element("version")?.Value?.Trim() ?? "";
                string downloadUrl = doc.Root?.Element("url")?.Value?.Trim() ?? "";
                string notes = doc.Root?.Element("notes")?.Value?.Trim() ?? "";

                if (string.IsNullOrEmpty(latestStr) || string.IsNullOrEmpty(downloadUrl))
                    return;

                if (!Version.TryParse(latestStr, out var latest)) return;

                var current = Assembly.GetExecutingAssembly().GetName().Version
                              ?? new Version(1, 0, 0);

                if (latest <= current) return;

                // Schon angeboten?
                if (ReadSkipVersion() == latestStr) return;

                string latestClean = $"v{latest.Major}.{latest.Minor}.{latest.Build}";
                string currentClean = $"v{current.Major}.{current.Minor}.{current.Build}";

                string title = isEnglish ? "Update available" : "Update verfügbar";
                string msg = isEnglish
                    ? $"Version {latestClean} is available (you have {currentClean})."
                      + (notes.Length > 0 ? $"\n\n{notes}" : "")
                      + "\n\nDownload and open now?"
                    : $"Version {latestClean} ist verfügbar (aktuell: {currentClean})."
                      + (notes.Length > 0 ? $"\n\n{notes}" : "")
                      + "\n\nJetzt herunterladen und öffnen?";

                SaveSkipVersion(latestStr); // nicht nochmal fragen, egal was der User wählt

                var result = MessageBox.Show(msg, title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                    await DownloadAsync(downloadUrl, latestClean, isEnglish, disableDuringDownload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Updater] {ex.Message}");
#if DEBUG
                MessageBox.Show($"Update-Check fehlgeschlagen:\n{ex.Message}",
                    "Updater Debug", MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
            }
        }

        // ── Download ──────────────────────────────────────────────────────
        private static async Task DownloadAsync(string url, string version,
            bool isEnglish, Control[] toDisable)
        {
            foreach (var c in toDisable) SetEnabled(c, false);

            string ext = Path.GetExtension(new Uri(url).AbsolutePath);
            string dest = Path.Combine(Path.GetTempPath(), $"DataHeater-{version}{ext}");

            using var progress = new Form
            {
                Text = "Update",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new System.Drawing.Size(360, 80),
                ControlBox = false
            };
            var lbl = new Label
            {
                Text = isEnglish ? "Downloading update…" : "Update wird heruntergeladen…",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            progress.Controls.Add(lbl);
            progress.Show();
            Application.DoEvents();

            try
            {
                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(dest, bytes);
            }
            finally
            {
                progress.Close();
                foreach (var c in toDisable) SetEnabled(c, true);
            }

            // Im Explorer markiert öffnen
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{dest}\"")
            { UseShellExecute = true });

            MessageBox.Show(
                isEnglish
                    ? $"Download complete!\n\nFile saved to:\n{dest}\n\nClose DataHeater and replace the old exe with the new one."
                    : $"Download abgeschlossen!\n\nGespeichert unter:\n{dest}\n\nDataHeater schließen und die alte exe durch die neue ersetzen.",
                isEnglish ? "Update ready" : "Update bereit",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────
        private static void SetEnabled(Control c, bool enabled)
        {
            if (c.InvokeRequired) c.Invoke(() => c.Enabled = enabled);
            else c.Enabled = enabled;
        }

        private static string ReadSkipVersion()
        {
            try { if (File.Exists(_skipFile)) return File.ReadAllText(_skipFile).Trim(); }
            catch { }
            return "";
        }

        private static void SaveSkipVersion(string version)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_skipFile)!);
                File.WriteAllText(_skipFile, version);
            }
            catch { }
        }
    }
}