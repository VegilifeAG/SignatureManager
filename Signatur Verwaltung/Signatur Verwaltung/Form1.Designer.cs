namespace Signatur_Verwaltung
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            statusLabel = new Label();
            progressBar1 = new ProgressBar();
            pictureBox1 = new PictureBox();
            contextMenuStrip1 = new ContextMenuStrip(components);
            signaturenAktualisierenToolStripMenuItem = new ToolStripMenuItem();
            einstellungenToolStripMenuItem = new ToolStripMenuItem();
            beendenToolStripMenuItem = new ToolStripMenuItem();
            notifyIcon1 = new NotifyIcon(components);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            statusLabel.Location = new Point(187, 12);
            statusLabel.Margin = new Padding(4, 0, 4, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(380, 32);
            statusLabel.TabIndex = 0;
            statusLabel.Text = "Signaturen werden aktualisiert..";
            statusLabel.UseWaitCursor = true;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(187, 68);
            progressBar1.Margin = new Padding(4);
            progressBar1.MarqueeAnimationSpeed = 1;
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(693, 44);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 1;
            progressBar1.UseWaitCursor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources._530543c0_644d_4369_9314_97c5eb197d37_cover;
            pictureBox1.Location = new Point(-18, -8);
            pictureBox1.Margin = new Padding(4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(198, 152);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            pictureBox1.UseWaitCursor = true;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(32, 32);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { signaturenAktualisierenToolStripMenuItem, einstellungenToolStripMenuItem, beendenToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(346, 118);
            // 
            // signaturenAktualisierenToolStripMenuItem
            // 
            signaturenAktualisierenToolStripMenuItem.Name = "signaturenAktualisierenToolStripMenuItem";
            signaturenAktualisierenToolStripMenuItem.Size = new Size(345, 38);
            signaturenAktualisierenToolStripMenuItem.Text = "Signaturen aktualisieren";
            signaturenAktualisierenToolStripMenuItem.Click += signaturenAktualisierenToolStripMenuItem_Click;
            // 
            // einstellungenToolStripMenuItem
            // 
            einstellungenToolStripMenuItem.Name = "einstellungenToolStripMenuItem";
            einstellungenToolStripMenuItem.Size = new Size(345, 38);
            einstellungenToolStripMenuItem.Text = "Einstellungen";
            einstellungenToolStripMenuItem.Click += einstellungenToolStripMenuItem_Click_1;
            // 
            // beendenToolStripMenuItem
            // 
            beendenToolStripMenuItem.Name = "beendenToolStripMenuItem";
            beendenToolStripMenuItem.Size = new Size(345, 38);
            beendenToolStripMenuItem.Text = "Beenden";
            beendenToolStripMenuItem.Click += beendenToolStripMenuItem_Click;
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "Signatur Verwaltung: Auf dem neusten Stand!";
            notifyIcon1.Visible = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(896, 127);
            ControlBox = false;
            Controls.Add(pictureBox1);
            Controls.Add(progressBar1);
            Controls.Add(statusLabel);
            FormBorderStyle = FormBorderStyle.None;
            HelpButton = true;
            Margin = new Padding(4);
            MaximizeBox = false;
            Name = "Form1";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = " ";
            UseWaitCursor = true;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label statusLabel;
        private ProgressBar progressBar1;
        private PictureBox pictureBox1;
        private ContextMenuStrip contextMenuStrip1;
        private NotifyIcon notifyIcon1;
        private ToolStripMenuItem signaturenAktualisierenToolStripMenuItem;
        private ToolStripMenuItem einstellungenToolStripMenuItem;
        private ToolStripMenuItem beendenToolStripMenuItem;
    }
}
