namespace UnoLedControl
{
    partial class FansForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TrackBar tbAll;
        private System.Windows.Forms.TrackBar tbF1;
        private System.Windows.Forms.TrackBar tbF2;
        private System.Windows.Forms.TrackBar tbF3;
        private System.Windows.Forms.TrackBar tbF4;

        private System.Windows.Forms.Label lblAll;
        private System.Windows.Forms.Label lblF1;
        private System.Windows.Forms.Label lblF2;
        private System.Windows.Forms.Label lblF3;
        private System.Windows.Forms.Label lblF4;

        private System.Windows.Forms.Button btnStop;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tbAll = new System.Windows.Forms.TrackBar();
            this.tbF1 = new System.Windows.Forms.TrackBar();
            this.tbF2 = new System.Windows.Forms.TrackBar();
            this.tbF3 = new System.Windows.Forms.TrackBar();
            this.tbF4 = new System.Windows.Forms.TrackBar();

            this.lblAll = new System.Windows.Forms.Label();
            this.lblF1 = new System.Windows.Forms.Label();
            this.lblF2 = new System.Windows.Forms.Label();
            this.lblF3 = new System.Windows.Forms.Label();
            this.lblF4 = new System.Windows.Forms.Label();

            this.btnStop = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.tbAll)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF4)).BeginInit();

            this.SuspendLayout();

            // Form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 360);
            this.Name = "FansForm";
            this.Text = "Fans Control";

            // Labels (white text over background)
            SetupLabel(this.lblAll, 12, 15, "FAN ALL: 0%");
            SetupLabel(this.lblF1, 12, 75, "FAN 1: 0%");
            SetupLabel(this.lblF2, 12, 135, "FAN 2: 0%");
            SetupLabel(this.lblF3, 12, 195, "FAN 3: 0%");
            SetupLabel(this.lblF4, 12, 255, "FAN 4: 0%");

            // Trackbars
            ConfigureTrackBar(this.tbAll, 12, 35);
            ConfigureTrackBar(this.tbF1, 12, 95);
            ConfigureTrackBar(this.tbF2, 12, 155);
            ConfigureTrackBar(this.tbF3, 12, 215);
            ConfigureTrackBar(this.tbF4, 12, 275);

            this.tbAll.Scroll += new System.EventHandler(this.tbAll_Scroll);
            this.tbF1.Scroll += new System.EventHandler(this.tbF1_Scroll);
            this.tbF2.Scroll += new System.EventHandler(this.tbF2_Scroll);
            this.tbF3.Scroll += new System.EventHandler(this.tbF3_Scroll);
            this.tbF4.Scroll += new System.EventHandler(this.tbF4_Scroll);

            // Stop button
            this.btnStop.Location = new System.Drawing.Point(400, 12);
            this.btnStop.Size = new System.Drawing.Size(100, 30);
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);

            // Add controls
            this.Controls.Add(this.lblAll);
            this.Controls.Add(this.lblF1);
            this.Controls.Add(this.lblF2);
            this.Controls.Add(this.lblF3);
            this.Controls.Add(this.lblF4);

            this.Controls.Add(this.tbAll);
            this.Controls.Add(this.tbF1);
            this.Controls.Add(this.tbF2);
            this.Controls.Add(this.tbF3);
            this.Controls.Add(this.tbF4);

            this.Controls.Add(this.btnStop);

            ((System.ComponentModel.ISupportInitialize)(this.tbAll)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbF4)).EndInit();

            this.ResumeLayout(false);
        }

        private void ConfigureTrackBar(System.Windows.Forms.TrackBar tb, int x, int y)
        {
            tb.Location = new System.Drawing.Point(x, y);
            tb.Size = new System.Drawing.Size(480, 45);
            tb.Minimum = 0;
            tb.Maximum = 100;
            tb.TickFrequency = 10;
            tb.Value = 0;
        }

        private void SetupLabel(System.Windows.Forms.Label lbl, int x, int y, string text)
        {
            lbl.Location = new System.Drawing.Point(x, y);
            lbl.Size = new System.Drawing.Size(220, 20);
            lbl.Text = text;
            lbl.BackColor = System.Drawing.Color.Transparent;
            lbl.ForeColor = System.Drawing.Color.White;
        }
    }
}
