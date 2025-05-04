namespace Signatur_Verwaltung
{
    partial class Settings
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
            label1 = new Label();
            clientIDBox = new TextBox();
            tenantIDBox = new TextBox();
            clientSecretBox = new TextBox();
            label2 = new Label();
            signatureChannelComboBox = new ComboBox();
            button1 = new Button();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            resetButton = new Button();
            label8 = new Label();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            label9 = new Label();
            checkBox1 = new CheckBox();
            Seperator = new Label();
            tabPage2 = new TabPage();
            label11 = new Label();
            tabPage3 = new TabPage();
            button2 = new Button();
            label10 = new Label();
            pictureBox1 = new PictureBox();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(5, 13);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(372, 37);
            label1.TabIndex = 0;
            label1.Text = "Microsoft 365 - Verbindung";
            // 
            // clientIDBox
            // 
            clientIDBox.Location = new Point(11, 54);
            clientIDBox.Margin = new Padding(2);
            clientIDBox.Name = "clientIDBox";
            clientIDBox.PlaceholderText = "Client ID";
            clientIDBox.Size = new Size(398, 23);
            clientIDBox.TabIndex = 1;
            // 
            // tenantIDBox
            // 
            tenantIDBox.Location = new Point(11, 81);
            tenantIDBox.Margin = new Padding(2);
            tenantIDBox.Name = "tenantIDBox";
            tenantIDBox.PlaceholderText = "Tenant ID";
            tenantIDBox.Size = new Size(398, 23);
            tenantIDBox.TabIndex = 2;
            // 
            // clientSecretBox
            // 
            clientSecretBox.Location = new Point(11, 108);
            clientSecretBox.Margin = new Padding(2);
            clientSecretBox.Name = "clientSecretBox";
            clientSecretBox.PasswordChar = '*';
            clientSecretBox.PlaceholderText = "Client Secret";
            clientSecretBox.Size = new Size(398, 23);
            clientSecretBox.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(5, 14);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(70, 30);
            label2.TabIndex = 4;
            label2.Text = "Kanal";
            // 
            // signatureChannelComboBox
            // 
            signatureChannelComboBox.FormattingEnabled = true;
            signatureChannelComboBox.Items.AddRange(new object[] { "Marcel Bourquin on VEGILIFE-GRAPH-OCM", "Debora Staub on VEGILIFE-GRAPH-OCM", "Yannick Wiss on VEGILIFE-GRAPH-OCM" });
            signatureChannelComboBox.Location = new Point(5, 55);
            signatureChannelComboBox.Margin = new Padding(2);
            signatureChannelComboBox.Name = "signatureChannelComboBox";
            signatureChannelComboBox.Size = new Size(454, 23);
            signatureChannelComboBox.TabIndex = 5;
            // 
            // button1
            // 
            button1.Location = new Point(381, 497);
            button1.Margin = new Padding(2);
            button1.Name = "button1";
            button1.Size = new Size(79, 21);
            button1.TabIndex = 6;
            button1.Text = "Speichern";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9.75F);
            label3.ForeColor = SystemColors.AppWorkspace;
            label3.Location = new Point(92, 430);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(281, 17);
            label3.TabIndex = 7;
            label3.Text = "© 2025 Marc Büttner - Alle rechte vorbehalten.";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(5, 90);
            label4.Margin = new Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new Size(0, 15);
            label4.TabIndex = 8;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9.75F);
            label5.ForeColor = SystemColors.AppWorkspace;
            label5.Location = new Point(160, 415);
            label5.Margin = new Padding(2, 0, 2, 0);
            label5.Name = "label5";
            label5.Size = new Size(145, 17);
            label5.TabIndex = 9;
            label5.Text = "Lizensiert für VegilifeAG";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = SystemColors.AppWorkspace;
            label6.Location = new Point(166, 305);
            label6.Margin = new Padding(2, 0, 2, 0);
            label6.Name = "label6";
            label6.Size = new Size(132, 25);
            label6.TabIndex = 10;
            label6.Text = "Version 1.0.0.0";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 8F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label7.ForeColor = SystemColors.AppWorkspace;
            label7.Location = new Point(5, 65);
            label7.Margin = new Padding(2, 0, 2, 0);
            label7.Name = "label7";
            label7.Size = new Size(0, 13);
            label7.TabIndex = 11;
            // 
            // resetButton
            // 
            resetButton.Location = new Point(288, 497);
            resetButton.Margin = new Padding(2);
            resetButton.Name = "resetButton";
            resetButton.Size = new Size(88, 21);
            resetButton.TabIndex = 12;
            resetButton.Text = "Zurücksetzen";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Visible = false;
            resetButton.Click += resetButton_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.ForeColor = SystemColors.AppWorkspace;
            label8.Location = new Point(11, 137);
            label8.Name = "label8";
            label8.Size = new Size(382, 15);
            label8.TabIndex = 15;
            label8.Text = "*API-Verbindungseinstellungen werden via Servereinrichtung verwaltet.";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Location = new Point(-4, 1);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(472, 491);
            tabControl1.TabIndex = 16;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(label9);
            tabPage1.Controls.Add(checkBox1);
            tabPage1.Controls.Add(Seperator);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(label8);
            tabPage1.Controls.Add(clientIDBox);
            tabPage1.Controls.Add(tenantIDBox);
            tabPage1.Controls.Add(clientSecretBox);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(464, 463);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Allgemein";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label9.Location = new Point(5, 185);
            label9.Margin = new Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new Size(166, 37);
            label9.TabIndex = 18;
            label9.Text = "Darstellung";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Enabled = false;
            checkBox1.Location = new Point(11, 234);
            checkBox1.Margin = new Padding(2, 1, 2, 1);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(275, 19);
            checkBox1.TabIndex = 17;
            checkBox1.Text = "Detaillierte Update-Benachrichtigung anzeigen.";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // Seperator
            // 
            Seperator.AutoSize = true;
            Seperator.ForeColor = SystemColors.ControlDark;
            Seperator.Location = new Point(5, 165);
            Seperator.Name = "Seperator";
            Seperator.Size = new Size(451, 15);
            Seperator.TabIndex = 16;
            Seperator.Text = "―――――――――――――――――――――――――――――――――――――";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(label11);
            tabPage2.Controls.Add(label2);
            tabPage2.Controls.Add(signatureChannelComboBox);
            tabPage2.Controls.Add(label7);
            tabPage2.Controls.Add(label4);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(464, 463);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Signaturen";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.ForeColor = SystemColors.AppWorkspace;
            label11.Location = new Point(4, 82);
            label11.Name = "label11";
            label11.Size = new Size(285, 15);
            label11.TabIndex = 16;
            label11.Text = "*Update-Kanal wird von ihrer Organisation verwaltet.";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(button2);
            tabPage3.Controls.Add(label10);
            tabPage3.Controls.Add(pictureBox1);
            tabPage3.Controls.Add(label5);
            tabPage3.Controls.Add(label6);
            tabPage3.Controls.Add(label3);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(464, 463);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Info";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(145, 359);
            button2.Name = "button2";
            button2.Size = new Size(175, 23);
            button2.TabIndex = 13;
            button2.Text = "Auf Softwareupdates prüfen";
            button2.UseVisualStyleBackColor = true;
            button2.Visible = false;
            button2.Click += button2_Click;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label10.Location = new Point(68, 260);
            label10.Name = "label10";
            label10.Size = new Size(328, 45);
            label10.TabIndex = 12;
            label10.Text = "Signatur Verwaltung";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources._530543c0_644d_4369_9314_97c5eb197d37_cover;
            pictureBox1.Location = new Point(132, 53);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(201, 204);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 11;
            pictureBox1.TabStop = false;
            // 
            // Settings
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(466, 526);
            Controls.Add(tabControl1);
            Controls.Add(resetButton);
            Controls.Add(button1);
            Margin = new Padding(2);
            MaximizeBox = false;
            MaximumSize = new Size(482, 565);
            MinimumSize = new Size(482, 565);
            Name = "Settings";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Einstellungen - Signatur Verwaltung";
            Load += Settings_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private TextBox clientIDBox;
        private TextBox tenantIDBox;
        private TextBox clientSecretBox;
        private Label label2;
        private ComboBox signatureChannelComboBox;
        private Button button1;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Button resetButton;
        private Label label8;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TabPage tabPage3;
        private PictureBox pictureBox1;
        private Label Seperator;
        private Label label9;
        private CheckBox checkBox1;
        private Label label10;
        private Button button2;
        private Label label11;
    }
}