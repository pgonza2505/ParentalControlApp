using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ParentalControlApp
{
    public class BlockedSite
    {
        public string Keyword { get; set; }
        public string Priority { get; set; } // low, mid, high
        public override string ToString() => $"{Keyword} ({Priority})";
    }

    public class MainForm : Form
    {
        private TextBox txtNewSite;
        private ComboBox cmbPriority;
        private Button btnAddSite, btnRemoveSite, btnClearLogs, btnStart, btnStop, btnExit;
        private ListBox lstBlockedSites, lstLogs;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private readonly string blockedSitesPath = "blocked_sites.txt";
        private readonly string logPath = "block_log.txt";

        private CancellationTokenSource monitoringTokenSource;
        private List<BlockedSite> blockedSites = new();
        private HashSet<string> recentlyLoggedLowPriority = new();

        public MainForm()
        {
            Text = "Parental Controls";
            Size = new System.Drawing.Size(500, 600);

            string appName = "ParentalControlApp";
            string appPath = Application.ExecutablePath;
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (key.GetValue(appName) == null)
                key.SetValue(appName, appPath);

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Open", null, (s, e) => ShowMainWindow());
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Shield,
                ContextMenuStrip = trayMenu,
                Text = "Parental Controls Running",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => ShowMainWindow();

            Label lblSites = new Label { Text = "Blocked Sites", Top = 10, Left = 10, Width = 150 };
            txtNewSite = new TextBox { Top = 35, Left = 10, Width = 150 };
            cmbPriority = new ComboBox { Top = 35, Left = 170, Width = 80 };
            cmbPriority.Items.AddRange(new[] { "low", "mid", "high" });
            cmbPriority.SelectedIndex = 2;
            btnAddSite = new Button { Text = "Add", Top = 35, Left = 260, Width = 75 };
            btnRemoveSite = new Button { Text = "Remove Selected", Top = 35, Left = 340, Width = 120 };

            lstBlockedSites = new ListBox { Top = 70, Left = 10, Width = 445, Height = 150 };

            Label lblLogs = new Label { Text = "Logs", Top = 230, Left = 10, Width = 150 };
            lstLogs = new ListBox { Top = 255, Left = 10, Width = 445, Height = 200 };
            btnClearLogs = new Button { Text = "Clear Logs", Top = 460, Left = 10, Width = 100 };

            btnStart = new Button { Text = "Start Monitoring", Top = 500, Left = 10, Width = 140 };
            btnStop = new Button { Text = "Stop Monitoring", Top = 500, Left = 160, Width = 140, Enabled = false };
            btnExit = new Button { Text = "Exit", Top = 500, Left = 310, Width = 100 };

            btnAddSite.Click += (s, e) => AddSite();
            btnRemoveSite.Click += (s, e) => RemoveSelectedSite();
            btnClearLogs.Click += (s, e) => ClearLogs();
            btnExit.Click += (s, e) => HideToTray();
            btnStart.Click += (s, e) => StartMonitoring();
            btnStop.Click += (s, e) => StopMonitoring();
            txtNewSite.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) AddSite(); };

            FormClosing += (s, e) => { e.Cancel = true; HideToTray(); };

            Controls.AddRange(new Control[] {
                lblSites, txtNewSite, cmbPriority, btnAddSite, btnRemoveSite, lstBlockedSites,
                lblLogs, lstLogs, btnClearLogs,
                btnStart, btnStop, btnExit
            });

            LoadBlockedSites();
            LoadLogs();
        }

        private void ShowMainWindow() => Invoke((MethodInvoker)(() => { Show(); WindowState = FormWindowState.Normal; BringToFront(); }));
        private void HideToTray() => Hide();

        private void AddSite()
        {
            string site = txtNewSite.Text.Trim().ToLower();
            string priority = cmbPriority.SelectedItem?.ToString() ?? "high";
            if (!string.IsNullOrWhiteSpace(site) && !blockedSites.Any(b => b.Keyword == site))
            {
                var newSite = new BlockedSite { Keyword = site, Priority = priority };
                blockedSites.Add(newSite);
                lstBlockedSites.Items.Add(newSite);
                File.AppendAllLines(blockedSitesPath, new[] { $"{site},{priority}" });
                txtNewSite.Clear();
            }
        }

        private void RemoveSelectedSite()
        {
            if (lstBlockedSites.SelectedItem is BlockedSite selected)
            {
                blockedSites.RemoveAll(b => b.Keyword == selected.Keyword);
                lstBlockedSites.Items.Remove(selected);
                File.WriteAllLines(blockedSitesPath, blockedSites.Select(b => $"{b.Keyword},{b.Priority}"));
                recentlyLoggedLowPriority.Remove(selected.Keyword);
            }
        }

        private void LoadBlockedSites()
        {
            blockedSites.Clear();
            lstBlockedSites.Items.Clear();
            if (File.Exists(blockedSitesPath))
            {
                foreach (var line in File.ReadAllLines(blockedSitesPath))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        var site = new BlockedSite { Keyword = parts[0], Priority = parts[1] };
                        blockedSites.Add(site);
                        lstBlockedSites.Items.Add(site);
                    }
                }
            }
        }

        private void LoadLogs()
        {
            if (File.Exists(logPath))
            {
                var lines = File.ReadAllLines(logPath);
                lstLogs.Items.AddRange(lines);
            }
        }

        private void ClearLogs()
        {
            lstLogs.Items.Clear();
            File.WriteAllText(logPath, string.Empty);
            recentlyLoggedLowPriority.Clear();
        }

        private void StartMonitoring()
        {
            if (monitoringTokenSource != null) return;

            btnStart.Enabled = false;
            btnStop.Enabled = true;

            monitoringTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorBrowserTitles(monitoringTokenSource.Token));
        }

        private void StopMonitoring()
        {
            monitoringTokenSource?.Cancel();
            monitoringTokenSource = null;

            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void MonitorBrowserTitles(CancellationToken token)
        {
            string[] browsers = { "chrome", "msedge", "firefox", "brave", "opera" };

            while (!token.IsCancellationRequested)
            {
                foreach (string browser in browsers)
                {
                    foreach (Process proc in Process.GetProcessesByName(browser))
                    {
                        try
                        {
                            string title = proc.MainWindowTitle.ToLower();
                            if (string.IsNullOrWhiteSpace(title)) continue;

                            foreach (var blocked in blockedSites)
                            {
                                if (title.Contains(blocked.Keyword))
                                {
                                    string logEntry = $"[{DateTime.Now}] {blocked.Priority.ToUpper()}: {blocked.Keyword}";
                                    Invoke((MethodInvoker)(() =>
                                    {
                                        lstLogs.Items.Add(logEntry);
                                        File.AppendAllText(logPath, logEntry + "\n");
                                    }));

                                    if (blocked.Priority == "low")
                                    {
                                        recentlyLoggedLowPriority.Add(blocked.Keyword);
                                        continue;
                                    }

                                    if (blocked.Priority == "high")
                                    {
                                        System.Media.SystemSounds.Hand.Play();
                                        try { proc.Kill(); } catch { }
                                    }

                                    Invoke((MethodInvoker)(() => ShowBlockingPopup()));
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
                Thread.Sleep(2000);
            }
        }

        private void ShowBlockingPopup()
        {
            MessageBox.Show(
                "This website has been blocked.\nYour activity has been logged.\nPlease contact the administrator if you believe this is a mistake.",
                "Website Blocked",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}