using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataHeater.Helper
{
    /// <summary>
    /// Prüft beim Start ob auf GitHub eine neuere Version verfügbar ist.
    /// Setzt voraus:
    ///   - GitHub Releases mit Tags wie "v1.2.3"
    ///   - Das Release-Asset heißt "DataHeater-Setup.exe" (oder .zip)
    ///   - Die App-Version steht in <Version> in der .csproj
    /// </summary>
    internal static class Updater
    {
        // ── Konfiguration ─────────────────────────────────────────────────
        private const string GitHubOwner = "pfurpass";   // ← anpassen
        private const string GitHubRepo = "DataHeater";          // ← anpassen
        private const string AssetName = "DataHeater.zip";// ← anpassen (oder .zip)
        // ─────────────────────────────────────────────────────────────────

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        static Updater()
        {
            // GitHub API verlangt einen User-Agent
            // Nur Major.Minor – ProductInfoHeaderValue mag keine 4-teiligen Versionen ("3.0.0.0")
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            string vStr = v != null ? $"{v.Major}.{v.Minor}" : "1.0";
            _http.DefaultRequestHeaders.UserAgent.ParseAdd($"DataHeater/{vStr}");
        }

        /// <summary>
        /// Prüft GitHub auf neue Version. Zeigt Dialog wenn Update verfügbar.
        /// Muss nach Form.Show() aufgerufen werden (fire-and-forget via async void).
        /// </summary>
        public static async void CheckAsync(bool isEnglish = false)
        {
            try
            {
                var (latestTag, downloadUrl) = await FetchLatestReleaseAsync();
                if (downloadUrl == null) return;

                var current = Assembly.GetExecutingAssembly().GetName().Version
                              ?? new Version(1, 0, 0, 0);
                var latest = ParseVersion(latestTag);

                if (latest <= current) return; // kein Update nötig

                string title = isEnglish ? "Update available" : "Update verfügbar";
                string msg = isEnglish
                    ? $"Version {latestTag} is available (you have v{current.Major}.{current.Minor}.{current.Build}).\n\nDownload and install now?"
                    : $"Version {latestTag} ist verfügbar (aktuell: v{current.Major}.{current.Minor}.{current.Build}).\n\nJetzt herunterladen und installieren?";

                var result = MessageBox.Show(msg, title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                    await DownloadAndRunAsync(downloadUrl, latestTag, isEnglish);
            }
            catch (Exception ex)
            {
                // Im Debug-Modus sichtbar machen – im Release still ignorieren
                System.Diagnostics.Debug.WriteLine($"[Updater] Fehler: {ex.Message}");
#if DEBUG
                MessageBox.Show($"Update-Check fehlgeschlagen:{ ex.Message}",
                    "Updater Debug", MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
            }
        }

        // ── GitHub API ────────────────────────────────────────────────────
        private static async Task<(string tag, string url)> FetchLatestReleaseAsync()
        {
            string apiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            var resp = await _http.GetStringAsync(apiUrl);

            using var doc = JsonDocument.Parse(resp);
            var root = doc.RootElement;

            string tag = root.GetProperty("tag_name").GetString() ?? "";

            // Asset-Download-URL suchen
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.Equals(AssetName, StringComparison.OrdinalIgnoreCase))
                    {
                        string url = asset.GetProperty("browser_download_url").GetString() ?? "";
                        return (tag, url);
                    }
                }
            }

            // Kein passendes Asset → direkt zur Release-Seite leiten
            string htmlUrl = root.TryGetProperty("html_url", out var hu) ? hu.GetString() ?? "" : "";
            return (tag, htmlUrl.Length > 0 ? null : null); // kein Asset = kein Auto-Download
        }

        // ── Download & Start ──────────────────────────────────────────────
        private static async Task DownloadAndRunAsync(string url, string tag, bool isEnglish)
        {
            string tmp = Path.Combine(Path.GetTempPath(),
                $"DataHeater-{tag}{Path.GetExtension(AssetName)}");

            // Fortschrittsdialog (simpel)
            string dlMsg = isEnglish ? "Downloading update…" : "Update wird heruntergeladen…";
            using var progress = new Form
            {
                Text = isEnglish ? "Update" : "Update",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new System.Drawing.Size(340, 80),
                ControlBox = false
            };
            var lbl = new Label
            {
                Text = dlMsg,
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
                await File.WriteAllBytesAsync(tmp, bytes);
            }
            finally
            {
                progress.Close();
            }

            // Installer / zip starten
            Process.Start(new ProcessStartInfo(tmp) { UseShellExecute = true });

            // App beenden damit der Installer die Dateien ersetzen kann
            Application.Exit();
        }

        // ── Versionsparsing ───────────────────────────────────────────────
        private static Version ParseVersion(string tag)
        {
            // Unterstützt: "v1.2.3", "1.2.3", "DataHeater-pub-4.0.0", beliebige Prefixe
            // Sucht einfach die erste Zahl-Punkt-Zahl Sequenz im Tag
            var match = System.Text.RegularExpressions.Regex.Match(
                tag, @"(\d+\.\d+[\.\d]*)");
            if (match.Success && Version.TryParse(match.Value, out var v))
                return v;
            return new Version(0, 0, 0);
        }
    }
}