namespace Alphastorms.Downloader.Win
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblInitialMessage = new System.Windows.Forms.Label();
            this.pbDownloader = new System.Windows.Forms.PictureBox();
            this.tbServerMessage = new System.Windows.Forms.TextBox();
            this.pgDownload = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.btnPlay = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbDownloader)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblInitialMessage);
            this.panel1.Controls.Add(this.pbDownloader);
            this.panel1.Controls.Add(this.tbServerMessage);
            this.panel1.Location = new System.Drawing.Point(6, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(790, 253);
            this.panel1.TabIndex = 0;
            // 
            // lblInitialMessage
            // 
            this.lblInitialMessage.AutoSize = true;
            this.lblInitialMessage.Location = new System.Drawing.Point(254, 10);
            this.lblInitialMessage.Name = "lblInitialMessage";
            this.lblInitialMessage.Size = new System.Drawing.Size(134, 15);
            this.lblInitialMessage.TabIndex = 2;
            this.lblInitialMessage.Text = "Welcome Message Here";
            // 
            // pbDownloader
            // 
            this.pbDownloader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbDownloader.Location = new System.Drawing.Point(6, 5);
            this.pbDownloader.Name = "pbDownloader";
            this.pbDownloader.Size = new System.Drawing.Size(242, 245);
            this.pbDownloader.TabIndex = 1;
            this.pbDownloader.TabStop = false;
            // 
            // tbServerMessage
            // 
            this.tbServerMessage.Location = new System.Drawing.Point(251, 46);
            this.tbServerMessage.Multiline = true;
            this.tbServerMessage.Name = "tbServerMessage";
            this.tbServerMessage.Size = new System.Drawing.Size(536, 204);
            this.tbServerMessage.TabIndex = 0;
            this.tbServerMessage.Text = "Connecting...";
            // 
            // pgDownload
            // 
            this.pgDownload.Location = new System.Drawing.Point(12, 280);
            this.pgDownload.Name = "pgDownload";
            this.pgDownload.Size = new System.Drawing.Size(790, 23);
            this.pgDownload.TabIndex = 1;
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(12, 262);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(75, 15);
            this.lblProgress.TabIndex = 2;
            this.lblProgress.Text = "Connecting..";
            // 
            // btnPlay
            // 
            this.btnPlay.BackColor = System.Drawing.Color.LightGreen;
            this.btnPlay.Enabled = false;
            this.btnPlay.Font = new System.Drawing.Font("Segoe UI", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnPlay.Location = new System.Drawing.Point(212, 329);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(368, 72);
            this.btnPlay.TabIndex = 3;
            this.btnPlay.Text = "Play";
            this.btnPlay.UseVisualStyleBackColor = false;
            this.btnPlay.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 428);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.pgDownload);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "Alphastorms Downloader";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbDownloader)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Panel panel1;
        private Label lblInitialMessage;
        private PictureBox pbDownloader;
        private TextBox tbServerMessage;
        private ProgressBar pgDownload;
        private Label lblProgress;
        private Button btnPlay;
    }
}