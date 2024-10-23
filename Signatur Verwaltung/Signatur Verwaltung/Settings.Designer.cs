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
            checkConnectionButton = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(16, 12);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(344, 59);
            label1.TabIndex = 0;
            label1.Text = "API Verbindung";
            // 
            // clientIDBox
            // 
            clientIDBox.Location = new Point(16, 88);
            clientIDBox.Margin = new Padding(4);
            clientIDBox.Name = "clientIDBox";
            clientIDBox.PlaceholderText = "Client ID";
            clientIDBox.Size = new Size(400, 39);
            clientIDBox.TabIndex = 1;
            // 
            // tenantIDBox
            // 
            tenantIDBox.Location = new Point(16, 154);
            tenantIDBox.Margin = new Padding(4);
            tenantIDBox.Name = "tenantIDBox";
            tenantIDBox.PlaceholderText = "Tenant ID";
            tenantIDBox.Size = new Size(400, 39);
            tenantIDBox.TabIndex = 2;
            // 
            // clientSecretBox
            // 
            clientSecretBox.Location = new Point(16, 221);
            clientSecretBox.Margin = new Padding(4);
            clientSecretBox.Name = "clientSecretBox";
            clientSecretBox.PasswordChar = '*';
            clientSecretBox.PlaceholderText = "Client Secret";
            clientSecretBox.Size = new Size(400, 39);
            clientSecretBox.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(461, 12);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(395, 59);
            label2.TabIndex = 4;
            label2.Text = "Signature Channel";
            // 
            // signatureChannelComboBox
            // 
            signatureChannelComboBox.FormattingEnabled = true;
            signatureChannelComboBox.Items.AddRange(new object[] { "Marcel Bourquin on VEGILIFE-GRAPH-OCM", "Debora Staub on VEGILIFE-GRAPH-OCM", "Yannick Wiss on VEGILIFE-GRAPH-OCM" });
            signatureChannelComboBox.Location = new Point(461, 88);
            signatureChannelComboBox.Margin = new Padding(4);
            signatureChannelComboBox.Name = "signatureChannelComboBox";
            signatureChannelComboBox.Size = new Size(476, 40);
            signatureChannelComboBox.TabIndex = 5;
            // 
            // button1
            // 
            button1.Location = new Point(793, 221);
            button1.Margin = new Padding(4);
            button1.Name = "button1";
            button1.Size = new Size(146, 44);
            button1.TabIndex = 6;
            button1.Text = "Speichern";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ForeColor = SystemColors.AppWorkspace;
            label3.Location = new Point(16, 417);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(518, 32);
            label3.TabIndex = 7;
            label3.Text = "© 2024 Marc Büttner - Alle rechte vorbehalten.";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(461, 198);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(0, 32);
            label4.TabIndex = 8;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.ForeColor = SystemColors.AppWorkspace;
            label5.Location = new Point(16, 372);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(268, 32);
            label5.TabIndex = 9;
            label5.Text = "Lizensiert für VegilifeAG";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.ForeColor = SystemColors.AppWorkspace;
            label6.Location = new Point(771, 417);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(166, 32);
            label6.TabIndex = 10;
            label6.Text = "Version 1.0.0.0";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 8F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label7.ForeColor = SystemColors.AppWorkspace;
            label7.Location = new Point(461, 145);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(478, 30);
            label7.TabIndex = 11;
            label7.Text = "Powered by Microsoft 365 Graph and SharePoint";
            // 
            // resetButton
            // 
            resetButton.Location = new Point(621, 221);
            resetButton.Margin = new Padding(4);
            resetButton.Name = "resetButton";
            resetButton.Size = new Size(164, 44);
            resetButton.TabIndex = 12;
            resetButton.Text = "Zurücksetzen";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Click += resetButton_Click;
            // 
            // checkConnectionButton
            // 
            checkConnectionButton.Location = new Point(16, 283);
            checkConnectionButton.Margin = new Padding(4);
            checkConnectionButton.Name = "checkConnectionButton";
            checkConnectionButton.Size = new Size(402, 44);
            checkConnectionButton.TabIndex = 13;
            checkConnectionButton.Text = "Verbindung Testen";
            checkConnectionButton.UseVisualStyleBackColor = true;
            checkConnectionButton.Click += checkConnectionButton_Click;
            // 
            // Settings
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(958, 468);
            Controls.Add(checkConnectionButton);
            Controls.Add(resetButton);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(button1);
            Controls.Add(signatureChannelComboBox);
            Controls.Add(label2);
            Controls.Add(clientSecretBox);
            Controls.Add(tenantIDBox);
            Controls.Add(clientIDBox);
            Controls.Add(label1);
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Settings";
            ShowIcon = false;
            Text = "Einstellungen - Signatur Verwaltung";
            Load += Settings_Load;
            ResumeLayout(false);
            PerformLayout();
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
        private Button checkConnectionButton;
    }
}