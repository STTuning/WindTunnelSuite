using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace UnoLedControl
{
    public class FansPage : UserControl
    {
        private readonly SerialPort _serial;
        private readonly TelemetryStore _telemetry;
        private readonly Action<string> _logTx;
        private readonly Timer _timer = new Timer();
        private bool _connected;

        private TrackBar tbAll, tb1, tb2, tb3, tb4;
        private Label vAll, v1, v2, v3, v4;
        private Label rpm1, rpm2, rpm3, rpm4;

        // Avoid flicker by only updating when changed
        private readonly Dictionary<Label, string> _last = new Dictionary<Label, string>();

        public FansPage(SerialPort serial, TelemetryStore telemetry, Action<string> logTx)
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

            _timer.Interval = 200;
            _timer.Tick += (s, e) => RefreshTelemetry();
            _timer.Start();

            RefreshTelemetry();
        }

        public void SetConnected(bool connected) { _connected = connected; }

        private void BuildUI()
        {
            Controls.Clear();

            var card = new GlassPanel
            {
                FillColor = Color.FromArgb(255, 10, 10, 14), // opaque (important)
                CornerRadius = 18,
                Dock = DockStyle.Fill,
                Padding = new Padding(18)
            };
            Controls.Add(card);

            // Root layout: header + content (so it resizes with console)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
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
                Text = "Fans",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(title, 0, 0);

            var hint = new Label
            {
                Text = "Set speed (%)",
                ForeColor = Color.Gainsboro,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            header.Controls.Add(hint, 1, 0);

            var btnStop = new Button
            {
                Text = "STOP",
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 12, 0, 12)
            };
            StyleButton(btnStop);
            btnStop.Click += (s, e) => StopAll();
            header.Controls.Add(btnStop, 2, 0);

            // CONTENT: grid for rows (no absolute X positions)
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,                 // IMPORTANT: Top, not Fill
                AutoSize = true,                      // grow only as needed
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 520)); // fixed slider width
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

            root.Controls.Add(content, 0, 1);

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            scroll.Controls.Add(content);
            root.Controls.Add(scroll, 0, 1);

            int row = 0;
            AddFanRow(content, row++, "FAN ALL", out tbAll, out vAll, out _);
            AddFanRow(content, row++, "FAN 1", out tb1, out v1, out rpm1);
            AddFanRow(content, row++, "FAN 2", out tb2, out v2, out rpm2);
            AddFanRow(content, row++, "FAN 3", out tb3, out v3, out rpm3);
            AddFanRow(content, row++, "FAN 4", out tb4, out v4, out rpm4);

            tbAll.Scroll += (s, e) => SetAll(tbAll.Value);
            tb1.Scroll += (s, e) => Send("FAN 1 " + tb1.Value);
            tb2.Scroll += (s, e) => Send("FAN 2 " + tb2.Value);
            tb3.Scroll += (s, e) => Send("FAN 3 " + tb3.Value);
            tb4.Scroll += (s, e) => Send("FAN 4 " + tb4.Value);

            // cache init
            _last.Clear();
            _last[vAll] = null; _last[v1] = null; _last[v2] = null; _last[v3] = null; _last[v4] = null;
            _last[rpm1] = null; _last[rpm2] = null; _last[rpm3] = null; _last[rpm4] = null;
        }

        private void AddFanRow(TableLayoutPanel t, int rowIndex, string name, out TrackBar tb, out Label pct, out Label rpm)
        {
            // Ensure correct row count
            if (t.RowCount <= rowIndex)
                t.RowCount = rowIndex + 1;

            // Force fixed row height (prevents last row stretching)
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

            var k = new Label
            {
                Text = name,
                ForeColor = Color.Gainsboro,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 0, 8)
            };
            t.Controls.Add(k, 0, rowIndex);

            pct = new Label
            {
                Text = "0%",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 8, 0, 8)
            };
            t.Controls.Add(pct, 1, rowIndex);

            tb = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                LargeChange = 5,
                SmallChange = 1,
                Anchor = AnchorStyles.Left | AnchorStyles.Right, // not Dock=Fill
                Height = 45,
                Margin = new Padding(6, 14, 6, 14)
            };
            t.Controls.Add(tb, 2, rowIndex);

            if (name == "FAN ALL")
            {
                rpm = null;
                var placeholder = new Label { Dock = DockStyle.Fill, BackColor = Color.Transparent };
                t.Controls.Add(placeholder, 3, rowIndex);
                return;
            }

            rpm = new Label
            {
                Text = "RPM: 0",
                ForeColor = Color.Lime,
                BackColor = Color.FromArgb(10, 10, 12),
                Font = new Font("Consolas", 14f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 12, 0, 12)
            };
            t.Controls.Add(rpm, 3, rowIndex);
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

        private void SetAll(int v)
        {
            if (tb1.Value != v) tb1.Value = v;
            if (tb2.Value != v) tb2.Value = v;
            if (tb3.Value != v) tb3.Value = v;
            if (tb4.Value != v) tb4.Value = v;

            Send("FAN ALL " + v);
        }

        private void StopAll()
        {
            if (tbAll.Value != 0) tbAll.Value = 0;
            if (tb1.Value != 0) tb1.Value = 0;
            if (tb2.Value != 0) tb2.Value = 0;
            if (tb3.Value != 0) tb3.Value = 0;
            if (tb4.Value != 0) tb4.Value = 0;

            Send("FAN STOP");
        }

        private void SetIfChanged(Label label, string value)
        {
            if (label == null) return;
            if (!_last.TryGetValue(label, out var lastVal) || lastVal != value)
            {
                _last[label] = value;
                label.Text = value;
            }
        }

        private void RefreshTelemetry()
        {
            // Percent labels
            SetIfChanged(vAll, tbAll.Value + "%");
            SetIfChanged(v1, tb1.Value + "%");
            SetIfChanged(v2, tb2.Value + "%");
            SetIfChanged(v3, tb3.Value + "%");
            SetIfChanged(v4, tb4.Value + "%");

            // RPM labels from telemetry
            if (rpm1 != null) SetIfChanged(rpm1, "RPM: " + _telemetry.Get("rpm1", "0"));
            if (rpm2 != null) SetIfChanged(rpm2, "RPM: " + _telemetry.Get("rpm2", "0"));
            if (rpm3 != null) SetIfChanged(rpm3, "RPM: " + _telemetry.Get("rpm3", "0"));
            if (rpm4 != null) SetIfChanged(rpm4, "RPM: " + _telemetry.Get("rpm4", "0"));
        }
    }
}
