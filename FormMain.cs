using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ToolTray
{
    public partial class FormMain : Form
    {
        private struct ToolItemFolder
        {
            public string Text;
            public string Path;
            public bool IsRecurse;
            public string[] Filter;
            public string[] Admin;
        }
        private IniParser iniFile;
        private String iniFilePath;
        private const string ADMIN_TAG = "RUN_AS_ADMIN";
        public FormMain()
        {
            InitializeComponent();
            iniFilePath = Path.Combine( Path.GetDirectoryName(Application.ExecutablePath),"ToolTray.ini");
            if (!File.Exists(iniFilePath))
            {
                File.WriteAllText(iniFilePath, ";Please see https://github.com/ecator/tooltray");
            }
            
            InitMenu();

            var assemblyName = Assembly.GetEntryAssembly().GetName();
            this.Text = assemblyName.Name + " " + assemblyName.Version + (Debugger.IsAttached ? " debug mode" : "");
            this.Icon = Properties.Resources.icon;
            notifyIcon.Text = this.Text;
            notifyIcon.Icon = this.Icon;
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (!Debugger.IsAttached)
                {
                    this.Hide();
                }
                this.Opacity = 1;
                this.ShowInTaskbar = true;
            }));
        }

        private void buttonCnf_Click(object sender, EventArgs e)
        {
            var process = new ProcessStartInfo();
            process.FileName = "notepad.exe";
            process.Arguments = iniFilePath;
            Process.Start(process);
        }
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            InitMenu();
        }
        private void notifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
        }


        private void FormMain_Resize(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && MessageBox.Show("Close ToolTray?", "Attention", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void ToolStripItemClicked(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            //Debug.WriteLine("text:" + item.Text + " tag:" + item.Tag);
            var target = item.Tag.ToString();
            var ext = Path.GetExtension(target);
            var process = new ProcessStartInfo();
            process.UseShellExecute = true;
            if(item.Image != null  && item.Image.Tag.ToString() == ADMIN_TAG)
            {
                process.Verb = "runas";
            }
            
            switch (ext.ToLower())
            {
                case ".ps1":
                    process.FileName = "powershell.exe";
                    if (ExistInPath("pwsh.exe"))
                    {
                        process.FileName = "pwsh.exe";
                    }
                    process.Arguments = "\"" + target + "\"";
                    break;
                default:
                    process.FileName = target;
                    break;
            }
            RefreshStatus("run " + target);
            Process.Start(process);
        }
        
        private bool ExistInPath(string fileName)
        {
            var paths = Environment.GetEnvironmentVariable("PATH").Split(";");
            return paths.Any(path => File.Exists(Path.Combine(path, fileName)));
        }

        private string ReplaceWithEnvironmentVariable(string str)
        {
            string pattern = @"%(\S+?)%";
            foreach(Match match in Regex.Matches(str, pattern, RegexOptions.IgnoreCase))
            {
                str = str.Replace(match.Value, Environment.GetEnvironmentVariable(match.Groups[1].Value));
            }
            return str;
        }

        private void InitMenu()
        {
            iniFile = new IniParser(iniFilePath);

            decimal add = 0;
            decimal skip = 0;
            contextMenuStrip.Items.Clear();
            foreach(var section in iniFile.GetAllSections())
            {
                ToolItemFolder toolItemFolder;
                toolItemFolder.Text = section;
                toolItemFolder.Path = ReplaceWithEnvironmentVariable(iniFile.GetSetting(section,"PATH"));
                toolItemFolder.IsRecurse = iniFile.GetSetting(section, "RECURSE", "0") == "1" ? true : false;
                toolItemFolder.Filter = iniFile.GetSetting(section, "FILTER", "*").Split(",");
                toolItemFolder.Admin = iniFile.GetSetting(section, "ADMIN", "").Split(",");
                if (!Directory.Exists(toolItemFolder.Path))
                {
                    skip++;
                    continue;
                }
                var items = GetToolStripItems(toolItemFolder);
                if (items.Count > 0)
                {
                    var item = new ToolStripMenuItem();
                    item.Text = toolItemFolder.Text;
                    item.DropDownItems.AddRange(items.ToArray());
                    contextMenuStrip.Items.Add(item);
                    add++;
                }
                else
                {
                    skip++;
                }
            }
            RefreshStatus( add.ToString() + " added, " + skip.ToString() + " skipped");
        }

        private void RefreshStatus(string statusText)
        {
            var now = DateTime.Now;
            if (statusText.Length > 40)
            {
                statusText = statusText.Substring(0, 20) + "..."+ statusText.Substring(statusText.Length - 20);

            }
            toolStripStatusLabel.Text = now.ToString("yyyy/MM/dd HH:mm:ss") + ">" + statusText;
        }
        private static string WildcardToRegex(string pattern)
        {
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            return regexPattern;
        }
        private List<ToolStripItem> GetToolStripItems(ToolItemFolder toolItemFolder)
        {
            var items = new List<ToolStripItem>();

            var files = Directory.GetFiles(toolItemFolder.Path);
            IEnumerable<string> matchedFiles = files.Where(file => toolItemFolder.Filter.Any(pattern => Regex.IsMatch(Path.GetFileName(file),WildcardToRegex(pattern),RegexOptions.IgnoreCase)));
            foreach (var file in matchedFiles)
            {
                //Debug.WriteLine(file);
                RefreshStatus("add " + file);
                var item = new ToolStripMenuItem();
                item.Text = Path.GetFileName(file);
                item.Tag = file;
                item.Click += ToolStripItemClicked;
                if (toolItemFolder.Admin.Any(pattern => Regex.IsMatch(Path.GetFileName(file), WildcardToRegex(pattern), RegexOptions.IgnoreCase)))
                {
                    item.Image = Properties.Resources.adminIcon;
                    item.Image.Tag = ADMIN_TAG;
                }
                items.Add(item);
            }
            if (toolItemFolder.IsRecurse)
            {
                var dirs = Directory.GetDirectories(toolItemFolder.Path);
                foreach (var dir in dirs)
                {
                    ToolItemFolder nextToolItemFolder;
                    nextToolItemFolder.Text = Path.GetFileName(dir);
                    nextToolItemFolder.Path = dir;
                    nextToolItemFolder.IsRecurse = toolItemFolder.IsRecurse;
                    nextToolItemFolder.Filter = toolItemFolder.Filter;
                    nextToolItemFolder.Admin = toolItemFolder.Admin;
                    var nextItems = GetToolStripItems(nextToolItemFolder);
                    if (nextItems.Count > 0)
                    {
                        var item = new ToolStripMenuItem();
                        item.Text = nextToolItemFolder.Text;
                        item.DropDownItems.AddRange(nextItems.ToArray());
                        items.Add(item);
                    }
                }
            }

            return items;
        }
    }
}