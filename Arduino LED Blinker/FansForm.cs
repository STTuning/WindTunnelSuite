using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace UnoLedControl
{
    public partial class FansForm : Form
    {
        private readonly SerialPort _serial;
        private readonly TelemetryStore _telemetry;
        private readonly Timer _timer = new Timer();

        public FansForm(SerialPort sharedSerial, TelemetryStore telemetry)
        {
            InitializeComponent();

            _serial = sharedSerial;
            _telemetry = telemetry;

            try
            {
                this.BackgroundImage = Image.FromFile("background.png");
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch { }

            this.DoubleBuffered = true;

            // Put everything on a glass panel for readability
            BuildGlassLayout();

            // Refresh values from telemetry store
            _timer.Interval = 200;
            _timer.Tick += (s, e) => UpdateRpmLabels();
            _timer.Start();

            UpdateLabels();
            UpdateRpmLabels();
        }

        private GlassPanel glass;
        private Label lblR1, lblR2, lblR3, lblR4;

        private void BuildGlassLayout()
        {
            glass = new GlassPanel();
            glass.Location = new Point(12, 12);
            glass.Size = new Size(496, 336);
            glass.FillColor = Color.FromArgb(190, 10, 10, 14);
            this.Controls.Add(glass);

            // Re-parent existing controls into glass
            foreach (Control c in this.Controls)
            {
                // we just added glass; skip moving it
            }

            // Move existing controls into glass (must do carefully)
            Control[] toMove = new Control[this.Controls.Count];
            this.Controls.CopyTo(toMove, 0);

            for (int i = 0; i < toMove.Length; i++)
            {
                Control c = toMove[i];
                if (c == glass) continue;
                this.Controls.Remove(c);
                glass.Controls.Add(c);
            }

            // Make labels readable
            MakeLabelReadable(lblAll);
            MakeLabelReadable(lblF1);
            MakeLabelReadable(lblF2);
            MakeLabelReadable(lblF3);
            MakeLabelReadable(lblF4);

            // Add RPM labels
            lblR1 = MakeRpmLabel(260, 75, "RPM1: --");
            lblR2 = MakeRpmLabel(260, 135, "RPM2: --");
            lblR3 = MakeRpmLabel(260, 195, "RPM3: --");
            lblR4 = MakeRpmLabel(260, 255, "RPM4: --");

            glass.Controls.Add(lblR1);
            glass.Controls.Add(lblR2);
            glass.Controls.Add(lblR3);
            glass.Controls.Add(lblR4);

            StyleButton(btnStop);
        }

        private Label MakeRpmLabel(int x, int y, string text)
        {
            Label l = new Label();
            l.Location = new Point(x, y);
            l.Size = new Size(220, 20);
            l.Text = text;
            l.BackColor = Color.Transparent;
            l.ForeColor = Color.Gainsboro;
            l.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            return l;
        }

        private void MakeLabelReadable(Label l)
        {
            l.BackColor = Color.Transparent;
            l.ForeColor = Color.White;
            l.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        private void StyleButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(90, 255, 255, 255);
            b.BackColor = Color.FromArgb(210, 25, 25, 35);
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void Send(string cmd)
        {
            if (_serial == null || !_serial.IsOpen) return;
            try { _serial.WriteLine(cmd); }
            catch { }
        }

        private void UpdateLabels()
        {
            lblAll.Text = "FAN ALL: " + tbAll.Value + "%";
            lblF1.Text = "FAN 1: " + tbF1.Value + "%";
            lblF2.Text = "FAN 2: " + tbF2.Value + "%";
            lblF3.Text = "FAN 3: " + tbF3.Value + "%";
            lblF4.Text = "FAN 4: " + tbF4.Value + "%";
        }

        private void UpdateRpmLabels()
        {
            lblR1.Text = "RPM1: " + _telemetry.Get("rpm1");
            lblR2.Text = "RPM2: " + _telemetry.Get("rpm2");
            lblR3.Text = "RPM3: " + _telemetry.Get("rpm3");
            lblR4.Text = "RPM4: " + _telemetry.Get("rpm4");
        }

        private void tbAll_Scroll(object sender, EventArgs e)
        {
            tbF1.Value = tbAll.Value;
            tbF2.Value = tbAll.Value;
            tbF3.Value = tbAll.Value;
            tbF4.Value = tbAll.Value;

            UpdateLabels();
            Send("FAN ALL " + tbAll.Value);
        }

        private void tbF1_Scroll(object sender, EventArgs e) { UpdateLabels(); Send("FAN 1 " + tbF1.Value); }
        private void tbF2_Scroll(object sender, EventArgs e) { UpdateLabels(); Send("FAN 2 " + tbF2.Value); }
        private void tbF3_Scroll(object sender, EventArgs e) { UpdateLabels(); Send("FAN 3 " + tbF3.Value); }
        private void tbF4_Scroll(object sender, EventArgs e) { UpdateLabels(); Send("FAN 4 " + tbF4.Value); }

        private void btnStop_Click(object sender, EventArgs e)
        {
            tbAll.Value = 0;
            tbF1.Value = 0;
            tbF2.Value = 0;
            tbF3.Value = 0;
            tbF4.Value = 0;

            UpdateLabels();
            Send("FAN STOP");
        }
    }
}
