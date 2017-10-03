namespace PrepoznavanjeOblika
{
    partial class CameraUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            //CV.CloseWebcam();
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.tb_cannyMax = new System.Windows.Forms.TextBox();
            this.trackBar_cannyMax = new System.Windows.Forms.TrackBar();
            this.tb_cannyMin = new System.Windows.Forms.TextBox();
            this.trackBar_CannyMin = new System.Windows.Forms.TrackBar();
            this.rtb_test = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.b_startCamera = new System.Windows.Forms.Button();
            this.pb_processed = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_cannyMax)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_CannyMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_processed)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tb_cannyMax);
            this.panel1.Controls.Add(this.trackBar_cannyMax);
            this.panel1.Controls.Add(this.tb_cannyMin);
            this.panel1.Controls.Add(this.trackBar_CannyMin);
            this.panel1.Controls.Add(this.rtb_test);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.b_startCamera);
            this.panel1.Controls.Add(this.pb_processed);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1252, 654);
            this.panel1.TabIndex = 37;
            // 
            // tb_cannyMax
            // 
            this.tb_cannyMax.Enabled = false;
            this.tb_cannyMax.Location = new System.Drawing.Point(304, 39);
            this.tb_cannyMax.Name = "tb_cannyMax";
            this.tb_cannyMax.Size = new System.Drawing.Size(28, 20);
            this.tb_cannyMax.TabIndex = 65;
            // 
            // trackBar_cannyMax
            // 
            this.trackBar_cannyMax.LargeChange = 2;
            this.trackBar_cannyMax.Location = new System.Drawing.Point(193, 37);
            this.trackBar_cannyMax.Maximum = 30;
            this.trackBar_cannyMax.Minimum = 1;
            this.trackBar_cannyMax.Name = "trackBar_cannyMax";
            this.trackBar_cannyMax.Size = new System.Drawing.Size(104, 45);
            this.trackBar_cannyMax.TabIndex = 64;
            this.trackBar_cannyMax.Value = 10;
            this.trackBar_cannyMax.Scroll += new System.EventHandler(this.trackBar_cannyMax_Scroll);
            // 
            // tb_cannyMin
            // 
            this.tb_cannyMin.Enabled = false;
            this.tb_cannyMin.Location = new System.Drawing.Point(304, 5);
            this.tb_cannyMin.Name = "tb_cannyMin";
            this.tb_cannyMin.Size = new System.Drawing.Size(28, 20);
            this.tb_cannyMin.TabIndex = 63;
            // 
            // trackBar_CannyMin
            // 
            this.trackBar_CannyMin.LargeChange = 20;
            this.trackBar_CannyMin.Location = new System.Drawing.Point(193, 3);
            this.trackBar_CannyMin.Maximum = 200;
            this.trackBar_CannyMin.Minimum = 2;
            this.trackBar_CannyMin.Name = "trackBar_CannyMin";
            this.trackBar_CannyMin.Size = new System.Drawing.Size(104, 45);
            this.trackBar_CannyMin.SmallChange = 10;
            this.trackBar_CannyMin.TabIndex = 62;
            this.trackBar_CannyMin.TickFrequency = 10;
            this.trackBar_CannyMin.Value = 100;
            this.trackBar_CannyMin.Scroll += new System.EventHandler(this.trackBar_CannyMin_Scroll);
            // 
            // rtb_test
            // 
            this.rtb_test.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.rtb_test.Location = new System.Drawing.Point(338, 5);
            this.rtb_test.Name = "rtb_test";
            this.rtb_test.Size = new System.Drawing.Size(216, 117);
            this.rtb_test.TabIndex = 61;
            this.rtb_test.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(112, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 59;
            this.button1.Text = "Calibrate";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_2);
            // 
            // b_startCamera
            // 
            this.b_startCamera.Location = new System.Drawing.Point(3, 3);
            this.b_startCamera.Name = "b_startCamera";
            this.b_startCamera.Size = new System.Drawing.Size(103, 23);
            this.b_startCamera.TabIndex = 38;
            this.b_startCamera.Text = "Start Camera";
            this.b_startCamera.UseVisualStyleBackColor = true;
            this.b_startCamera.Click += new System.EventHandler(this.button2_Click);
            // 
            // pb_processed
            // 
            this.pb_processed.BackColor = System.Drawing.Color.Transparent;
            this.pb_processed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pb_processed.Location = new System.Drawing.Point(0, 0);
            this.pb_processed.Name = "pb_processed";
            this.pb_processed.Size = new System.Drawing.Size(1252, 654);
            this.pb_processed.TabIndex = 34;
            this.pb_processed.TabStop = false;
            this.pb_processed.Click += new System.EventHandler(this.pb_processed_Click);
            // 
            // CameraUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.panel1);
            this.Name = "CameraUI";
            this.Size = new System.Drawing.Size(1252, 654);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_cannyMax)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_CannyMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_processed)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.PictureBox pb_processed;
        private System.Windows.Forms.Button b_startCamera;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RichTextBox rtb_test;
        private System.Windows.Forms.TrackBar trackBar_CannyMin;
        private System.Windows.Forms.TextBox tb_cannyMin;
        private System.Windows.Forms.TextBox tb_cannyMax;
        private System.Windows.Forms.TrackBar trackBar_cannyMax;
    }
}
