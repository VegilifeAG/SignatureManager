namespace Signatur_Verwaltung
{
    partial class CheckForUpdates
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            HeaderLabel = new Label();
            progressBar1 = new ProgressBar();
            NoUpdatesAvailableLabel = new Label();
            UpdatesAvailableLabel = new Label();
            timer1 = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // HeaderLabel
            // 
            HeaderLabel.AutoSize = true;
            HeaderLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            HeaderLabel.Location = new Point(157, 23);
            HeaderLabel.Name = "HeaderLabel";
            HeaderLabel.Size = new Size(165, 15);
            HeaderLabel.TabIndex = 3;
            HeaderLabel.Text = "Auf Softwareupdates prüfen...";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(18, 55);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(442, 23);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 2;
            // 
            // NoUpdatesAvailableLabel
            // 
            NoUpdatesAvailableLabel.AutoSize = true;
            NoUpdatesAvailableLabel.Location = new Point(46, 44);
            NoUpdatesAvailableLabel.Name = "NoUpdatesAvailableLabel";
            NoUpdatesAvailableLabel.Size = new Size(387, 15);
            NoUpdatesAvailableLabel.TabIndex = 4;
            NoUpdatesAvailableLabel.Text = "Es stehen keine Updates von Signatur Verwaltung zum installieren bereit.";
            NoUpdatesAvailableLabel.TextAlign = ContentAlignment.TopCenter;
            NoUpdatesAvailableLabel.Visible = false;
            // 
            // UpdatesAvailableLabel
            // 
            UpdatesAvailableLabel.AutoSize = true;
            UpdatesAvailableLabel.Font = new Font("Segoe UI", 9F);
            UpdatesAvailableLabel.Location = new Point(46, 44);
            UpdatesAvailableLabel.Name = "UpdatesAvailableLabel";
            UpdatesAvailableLabel.Size = new Size(385, 30);
            UpdatesAvailableLabel.TabIndex = 5;
            UpdatesAvailableLabel.Text = "Es stehen neue Updates von Signatur Verwaltung zum installieren bereit.\r\nBitte starten Sie die App neu um diese zu installieren.";
            UpdatesAvailableLabel.TextAlign = ContentAlignment.TopCenter;
            UpdatesAvailableLabel.Visible = false;
            // 
            // timer1
            // 
            timer1.Tick += timer1_Tick;
            // 
            // CheckForUpdates
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(479, 102);
            Controls.Add(UpdatesAvailableLabel);
            Controls.Add(NoUpdatesAvailableLabel);
            Controls.Add(HeaderLabel);
            Controls.Add(progressBar1);
            MaximizeBox = false;
            MaximumSize = new Size(495, 141);
            MinimizeBox = false;
            MinimumSize = new Size(495, 141);
            Name = "CheckForUpdates";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Einstellungen - Signatur Verwaltung";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label HeaderLabel;
        private ProgressBar progressBar1;
        private Label NoUpdatesAvailableLabel;
        private Label UpdatesAvailableLabel;
        private System.Windows.Forms.Timer timer1;
    }
}