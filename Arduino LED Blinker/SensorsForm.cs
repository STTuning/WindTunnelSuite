using System;
using System.Drawing;
using System.Windows.Forms;

namespace UnoLedControl
{
    public partial class SensorsForm : Form
    {
        private readonly TelemetryStore _telemetry;
        private readonly Timer _timer;

        public SensorsForm(TelemetryStore telemetry)
        {
            InitializeComponent();

            _telemetry = telemetry;

            // Background image from file (same approach you use elsewhere)
            try
            {
                this.BackgroundImage = Image.FromFile("background.png");
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch { }

            this.DoubleBuffered = true;

            ApplyLabelTheme();

            _timer = new Timer();
            _timer.Interval = 200;
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Timer_Tick(null, EventArgs.Empty);
        }

        private void ApplyLabelTheme()
        {
            // Key labels
            StyleKey(lblLastRxK);
            StyleKey(lblFanAllK);
            StyleKey(lblFan1K);
            StyleKey(lblFan2K);
            StyleKey(lblFan3K);
            StyleKey(lblFan4K);
            StyleKey(lblRpm1K);
            StyleKey(lblRpm2K);
            StyleKey(lblRpm3K);
            StyleKey(lblRpm4K);
            StyleKey(lblMpxAdcK);
            StyleKey(lblBmpPK);
            StyleKey(lblBmpTK);
            StyleKey(lblAs5600K);

            // Value labels
            StyleValue(lblLastRxV);
            StyleValue(lblFanAllV);
            StyleValue(lblFan1V);
            StyleValue(lblFan2V);
            StyleValue(lblFan3V);
            StyleValue(lblFan4V);
            StyleValue(lblRpm1V);
            StyleValue(lblRpm2V);
            StyleValue(lblRpm3V);
            StyleValue(lblRpm4V);
            StyleValue(lblMpxAdcV);
            StyleValue(lblBmpPV);
            StyleValue(lblBmpTV);
            StyleValue(lblAs5600V);
        }

        private void StyleKey(Label l)
        {
            l.BackColor = Color.Transparent;
            l.ForeColor = Color.Gainsboro;
            l.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        private void StyleValue(Label l)
        {
            l.BackColor = Color.Transparent;
            l.ForeColor = Color.White;
            l.Font = new Font("Consolas", 10f, FontStyle.Bold);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lblLastRxV.Text = _telemetry.Get("last_rx");

            lblFanAllV.Text = _telemetry.Get("fanAll");
            lblFan1V.Text = _telemetry.Get("fan1");
            lblFan2V.Text = _telemetry.Get("fan2");
            lblFan3V.Text = _telemetry.Get("fan3");
            lblFan4V.Text = _telemetry.Get("fan4");

            lblRpm1V.Text = _telemetry.Get("rpm1");
            lblRpm2V.Text = _telemetry.Get("rpm2");
            lblRpm3V.Text = _telemetry.Get("rpm3");
            lblRpm4V.Text = _telemetry.Get("rpm4");

            lblMpxAdcV.Text = _telemetry.Get("mpx_adc");
            lblBmpPV.Text = _telemetry.Get("bmp_p");
            lblBmpTV.Text = _telemetry.Get("bmp_t");
            lblAs5600V.Text = _telemetry.Get("as5600");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _timer.Stop(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
