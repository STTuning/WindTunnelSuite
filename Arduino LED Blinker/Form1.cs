using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UnoLedControl
{
    public partial class Form1 : Form
    {
        private readonly SerialPort serial = new SerialPort();
        private readonly TelemetryStore telemetry = new TelemetryStore();

        private Panel nav;
        private Panel content;
        private Panel consolePanel;
        private TextBox txtConsole;

        private Label status;
        private ComboBox comboPorts;
        private Button btnConnect;
        private Button btnFans;
        private Button btnSensors;
        private Button btnConsole;

        private FansPage fansPage;
        private SensorsPage sensorsPage;

        private Button btnAngle;
        private AirfoilAnglePage anglePage;

        private bool consoleVisible = true;

        public Form1()
        {
            InitializeComponent();
            BuildShell();
            SetupSerial();

            RefreshPorts();

            // Pages
            // FansPage MUST have ctor: (SerialPort, TelemetryStore, Action<string>)
            // SensorsPage MUST have ctor: (SerialPort, TelemetryStore, Action<string>)
            fansPage = new FansPage(serial, telemetry, LogTx);
            sensorsPage = new SensorsPage(serial, telemetry, LogTx);
            anglePage = new AirfoilAnglePage(serial, telemetry, LogTx);


            ShowPage(fansPage);
        }

        private void SetupSerial()
        {
            serial.BaudRate = 115200;
            serial.NewLine = "\n";
            serial.Encoding = Encoding.ASCII;
            serial.DataReceived += Serial_DataReceived;
        }

        private void BuildShell()
        {
            // Background image
            try
            {
                BackgroundImage = Image.FromFile("background.png");
                BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch { }

            DoubleBuffered = true;
            Text = "Wind Tunnel Control (PC UI)";
            Size = new Size(1250, 520);
            MinimumSize = new Size(1100, 480);

            // IMPORTANT: we are building UI in code, so we remove any designer controls.
            Controls.Clear();

            // 1) LEFT NAV (add first)
            nav = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(200, 10, 10, 14)
            };
            Controls.Add(nav);

            // 2) RIGHT CONSOLE (add second)
            consolePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                BackColor = Color.FromArgb(200, 10, 10, 14),
                Visible = true
            };
            Controls.Add(consolePanel);

            txtConsole = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BackColor = Color.FromArgb(20, 20, 28), // solid, no alpha
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.FixedSingle
            };
            consolePanel.Controls.Add(txtConsole);

            // 3) CENTER CONTENT (add last so Dock=Fill uses remaining space)
            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(18),
                Margin = new Padding(0)
            };
            Controls.Add(content);

            // Ensure dock order is correct
            nav.BringToFront();
            consolePanel.BringToFront();
            content.BringToFront();

            // NAV UI
            var title = new Label
            {
                Text = "Wind Tunnel",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Location = new Point(16, 16),
                Size = new Size(200, 30)
            };
            nav.Controls.Add(title);

            comboPorts = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(16, 60),
                Size = new Size(188, 24),
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.White
            };
            nav.Controls.Add(comboPorts);

            btnConnect = MakeNavButton("Connect", 100);
            btnConnect.Click += (s, e) => ToggleConnect();
            nav.Controls.Add(btnConnect);

            status = new Label
            {
                Text = "Disconnected",
                ForeColor = Color.Gainsboro,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 144),
                Size = new Size(188, 20)
            };
            nav.Controls.Add(status);

            btnFans = MakeNavButton("Fans", 190);
            btnFans.Click += (s, e) => ShowPage(fansPage);
            nav.Controls.Add(btnFans);

            btnAngle = MakeNavButton("Airfoil Angle", 274);
            btnAngle.Click += (s, e) => ShowPage(anglePage);
            nav.Controls.Add(btnAngle);

            btnSensors = MakeNavButton("Sensors", 232);
            btnSensors.Click += (s, e) => ShowPage(sensorsPage);
            nav.Controls.Add(btnSensors);

            btnConsole = MakeNavButton("Console", 316);
            btnConsole.Click += (s, e) => ToggleConsole();
            nav.Controls.Add(btnConsole);
        }

        private Button MakeNavButton(string text, int y)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(16, y),
                Size = new Size(188, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 255, 255, 255);
            return b;
        }

        private void ShowPage(Control page)
        {
            if (page == null) return;

            content.SuspendLayout();
            content.Controls.Clear();

            page.Dock = DockStyle.Fill;
            content.Controls.Add(page);

            page.BringToFront();
            content.ResumeLayout(true);
        }

        private void ToggleConsole()
        {
            consoleVisible = !consoleVisible;
            consolePanel.Visible = consoleVisible;
        }

        private void RefreshPorts()
        {
            comboPorts.Items.Clear();
            string[] ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            comboPorts.Items.AddRange(ports);
            if (comboPorts.Items.Count > 0)
                comboPorts.SelectedIndex = 0;
        }

        private void ToggleConnect()
        {
            try
            {
                if (serial.IsOpen)
                {
                    serial.Close();
                    btnConnect.Text = "Connect";
                    status.Text = "Disconnected";
                    fansPage?.SetConnected(false);
                    sensorsPage?.SetConnected(false);
                    anglePage?.SetConnected(false);
                    LogRx("Disconnected");
                    return;
                }

                if (comboPorts.SelectedItem == null)
                {
                    MessageBox.Show("Select a COM port first.");
                    return;
                }

                serial.PortName = comboPorts.SelectedItem.ToString();
                serial.Open();

                btnConnect.Text = "Disconnect";
                status.Text = "Connected: " + serial.PortName;

                fansPage?.SetConnected(true);
                anglePage?.SetConnected(true);
                sensorsPage?.SetConnected(true);

                LogRx("Connected to " + serial.PortName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Serial error");
            }
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serial.ReadLine();
                if (line == null) return;

                line = line.Trim();

                telemetry.UpdateFromTelemetryLine(line);

                LogRx(line);
            }
            catch { }
        }

        private void LogRx(string line)
        {
            if (txtConsole == null) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(LogRx), line);
                return;
            }

            txtConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] RX: {line}{Environment.NewLine}");
        }

        private void LogTx(string line)
        {
            if (txtConsole == null) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(LogTx), line);
                return;
            }

            txtConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] TX: {line}{Environment.NewLine}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { if (serial.IsOpen) serial.Close(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
