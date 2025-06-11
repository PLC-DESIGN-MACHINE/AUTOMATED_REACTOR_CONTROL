using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    partial class Main_Form1
    {
        /// <summary>
        /// ตัวแปร designer จำเป็น
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Panel หลักสำหรับวาง UserControl
        /// </summary>
        private Panel panelMain;

        /// <summary>
        /// เคลียร์ทรัพยากร
        /// </summary>
        /// <param name="disposing">true ถ้าต้องการล้าง managed resources</param>
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
        /// ฟังก์ชันที่สร้าง Component ของ Form
        /// </summary>
        private void InitializeComponent()
        {
            this.panelMain = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(1486, 961);
            this.panelMain.TabIndex = 0;
            // 
            // Main_Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1486, 961);
            this.Controls.Add(this.panelMain);
            this.Name = "Main_Form1";
            this.Text = "Automated Reactor Control - Control Set 1";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
