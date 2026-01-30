using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace UnoLedControl
{
    public class SensorsPage : UserControl
    {
        private readonly SerialPort _serial;
        private readonly TelemetryStore _telemetry;
        private readonly Action<string> _logTx;
        private readonly Timer _timer = new Timer();
        private bool _connected;

        private TrackBar tbAll;
        private Button btnStop;

        // Value labels
        private Label vLast, vR1, vR2, vR3, vR4, vMpx, vBmpP, vBmpT, vAs;

        // Cache last displayed values to avoid repaint flicker
        private readonly Dictionary<Label, string> _last = new Dictionary<Label, string>();

        public SensorsPage(SerialPort serial, TelemetryStore telemetry, Action<string> logTx)
        {
            _serial = serial;
            _telemetry = telemetry;
            _logTx = logTx;

            Dock = DockStyle.Fill;
            BackColor = Color.Transparent;

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            UpdateStyles();

            BuildUI();

            _timer.Interval = 250;
            _timer.Tick += (s, e) => RefreshValues();
            _timer.Start();

            RefreshValues();
        }

        public void SetConnected(bool connected) { _connected = connected; }

        private void BuildUI()
        {
            Controls.Clear();

            var card = new GlassPanel
            {
                FillColor = Color.FromArgb(255, 10, 10, 14),
                CornerRadius = 18,
                Dock = DockStyle.Fill,
                Padding = new Padding(18)
            };
            Controls.Add(card);

            // ROOT layout: header + table
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));   // taller header (fix STOP cut)
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(root);

            // HEADER
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = Color.Transparent
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            root.Controls.Add(header, 0, 0);

            var title = new Label
            {
                Text = "Sensors (Live)",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(title, 0, 0);

            // Fan strip (layout-based, no absolute positioning)
            var fanStrip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };
            fanStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            fanStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.Controls.Add(fanStrip, 1, 0);

            var lblFan = new Label
            {
                Text = "Fan ALL",
                ForeColor = Color.Gainsboro,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            fanStrip.Controls.Add(lblFan, 0, 0);

            tbAll = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                LargeChange = 5,
                SmallChange = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 12, 0, 12)
            };
            tbAll.Scroll += (s, e) => Send("FAN ALL " + tbAll.Value);
            fanStrip.Controls.Add(tbAll, 1, 0);

            // STOP button (fixed size + margin, no Dock=Fill => no clipping)
            btnStop = new Button
            {
                Text = "STOP",
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 18, 0, 18)
            };
            StyleButton(btnStop);
            btnStop.Click += (s, e) => { tbAll.Value = 0; Send("FAN STOP"); };
            header.Controls.Add(btnStop, 2, 0);

            // TABLE
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(255, 10, 10, 14)
            };
            root.Controls.Add(scroll, 0, 1);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Color.FromArgb(255, 10, 10, 14),
                Padding = new Padding(0, 6, 0, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            scroll.Controls.Add(table);

            // rows
            vLast = AddRow(table, "Last packet received (time)", "--", chip: false);

            AddSeparator(table, "Fans feedback");
            vR1 = AddRow(table, "Fan 1 speed (RPM)", "--", chip: true);
            vR2 = AddRow(table, "Fan 2 speed (RPM)", "--", chip: true);
            vR3 = AddRow(table, "Fan 3 speed (RPM)", "--", chip: true);
            vR4 = AddRow(table, "Fan 4 speed (RPM)", "--", chip: true);

            AddSeparator(table, "Air / pressure sensors");
            vMpx = AddRow(table, "MPXV7002 differential pressure (raw ADC)", "--", chip: true);
            vBmpP = AddRow(table, "BMP280 barometric pressure (hPa)", "--", chip: true);
            vBmpT = AddRow(table, "BMP280 air temperature (°C)", "--", chip: true);

            AddSeparator(table, "Position");
            vAs = AddRow(table, "AS5600 shaft angle (deg)", "--", chip: true);

            // cache init
            _last.Clear();
            _last[vLast] = null;
            _last[vR1] = null; _last[vR2] = null; _last[vR3] = null; _last[vR4] = null;
            _last[vMpx] = null; _last[vBmpP] = null; _last[vBmpT] = null; _last[vAs] = null;
        }

        private void AddSeparator(TableLayoutPanel table, string text)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            var sep = new Label
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(25, 25, 35),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Margin = new Padding(0, 10, 0, 2)
            };

            table.Controls.Add(sep, 0, row);
            table.SetColumnSpan(sep, 2);
        }

        private Label AddRow(TableLayoutPanel table, string name, string initial, bool chip)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            var k = new Label
            {
                Text = name,
                ForeColor = Color.Gainsboro,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 2, 0, 2)
            };

            Label v;
            if (chip)
            {
                v = new Label
                {
                    Text = initial,
                    ForeColor = Color.Lime,
                    BackColor = Color.FromArgb(10, 10, 12),
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 14f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(8, 2, 0, 2)
                };
            }
            else
            {
                v = new Label
                {
                    Text = initial,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 12f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(8, 2, 0, 2)
                };
            }

            table.Controls.Add(k, 0, row);
            table.Controls.Add(v, 1, row);
            return v;
        }

        private void StyleButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 255, 255, 255);
            b.BackColor = Color.FromArgb(25, 25, 35);
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void Send(string cmd)
        {
            if (!_connected || _serial == null || !_serial.IsOpen) return;
            try
            {
                _serial.WriteLine(cmd);
                _logTx?.Invoke(cmd);
            }
            catch { }
        }

        private void SetIfChanged(Label label, string value)
        {
            if (label == null) return;
            if (!_last.TryGetValue(label, out string lastVal) || lastVal != value)
            {
                _last[label] = value;
                label.Text = value;
            }
        }

        private void RefreshValues()
        {
            SetIfChanged(vLast, _telemetry.Get("last_rx", "--"));

            SetIfChanged(vR1, _telemetry.Get("rpm1", "0"));
            SetIfChanged(vR2, _telemetry.Get("rpm2", "0"));
            SetIfChanged(vR3, _telemetry.Get("rpm3", "0"));
            SetIfChanged(vR4, _telemetry.Get("rpm4", "0"));

            SetIfChanged(vMpx, _telemetry.Get("mpx_adc", "--"));
            SetIfChanged(vBmpP, _telemetry.Get("bmp_p", "--"));
            SetIfChanged(vBmpT, _telemetry.Get("bmp_t", "--"));
            SetIfChanged(vAs, _telemetry.Get("as5600", "--"));
        }
    }
}
