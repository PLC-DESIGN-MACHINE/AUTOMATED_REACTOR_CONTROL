using System;
using System.Drawing;
using System.Windows.Forms;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    partial class UC_CONTROL_SET_1
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UC_CONTROL_SET_1));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel14 = new System.Windows.Forms.Panel();
            this.panel_CONTROL1_SET_1 = new System.Windows.Forms.Panel();
            this.But_CONTROL1_SET_1 = new System.Windows.Forms.Button();
            this.panel_CONTROL1_SET_2 = new System.Windows.Forms.Panel();
            this.But_CONTROL1_SET_2 = new System.Windows.Forms.Button();
            this.panel_Ext_Temp1 = new System.Windows.Forms.Panel();
            this.label_Ext1 = new System.Windows.Forms.Label();
            this.pictureBox7 = new System.Windows.Forms.PictureBox();
            this.panel_RPM1 = new System.Windows.Forms.Panel();
            this.label_RPM1 = new System.Windows.Forms.Label();
            this.pictureBox6 = new System.Windows.Forms.PictureBox();
            this.panel_Dosing1 = new System.Windows.Forms.Panel();
            this.label_Dosing1 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.panel_TJ1 = new System.Windows.Forms.Panel();
            this.label_TJ1 = new System.Windows.Forms.Label();
            this.pictureBox5 = new System.Windows.Forms.PictureBox();
            this.panel_TR_TJ1 = new System.Windows.Forms.Panel();
            this.label_TR_TJ1 = new System.Windows.Forms.Label();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.panel_TR1 = new System.Windows.Forms.Panel();
            this.label_TR1 = new System.Windows.Forms.Label();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.panel_Graph1_Data = new System.Windows.Forms.Panel();
            this.But_Graph_Data1 = new System.Windows.Forms.Button();
            this.panel_Program1_Sequence1 = new System.Windows.Forms.Panel();
            this.But_Program_Sequence1 = new System.Windows.Forms.Button();
            this.panel_Motor_Stirrer1 = new System.Windows.Forms.Panel();
            this.text1_Motor_Stirrer_Set1 = new System.Windows.Forms.TextBox();
            this.pict_Motor_Stirrer1 = new System.Windows.Forms.PictureBox();
            this.panel_Thermostat1 = new System.Windows.Forms.Panel();
            this.text1_Thermo_Set1 = new System.Windows.Forms.TextBox();
            this.pict_Thermostat1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.but_Setting1 = new System.Windows.Forms.Button();
            this.mainContainer = new System.Windows.Forms.Panel();
            this.sidebarPanel = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.picStatusIndicator = new AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Lamp_Connect();
            this.switch_Connect = new AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Switch_Target1();
            this.lblSidebarTitle = new System.Windows.Forms.Label();
            this.cmbModeSelector = new System.Windows.Forms.ComboBox();
            this.toggleSidebarButton = new AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Switch_Target1();
            this.sidebarAnimationTimer = new System.Windows.Forms.Timer(this.components);
            this.switch_Target1_Set1 = new AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Switch_Target1();
            this.switch_A_M1 = new AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Switch_A_M();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel14.SuspendLayout();
            this.panel_CONTROL1_SET_1.SuspendLayout();
            this.panel_CONTROL1_SET_2.SuspendLayout();
            this.panel_Ext_Temp1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox7)).BeginInit();
            this.panel_RPM1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).BeginInit();
            this.panel_Dosing1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panel_TJ1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).BeginInit();
            this.panel_TR_TJ1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).BeginInit();
            this.panel_TR1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.panel_Graph1_Data.SuspendLayout();
            this.panel_Program1_Sequence1.SuspendLayout();
            this.panel_Motor_Stirrer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pict_Motor_Stirrer1)).BeginInit();
            this.panel_Thermostat1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pict_Thermostat1)).BeginInit();
            this.panel1.SuspendLayout();
            this.mainContainer.SuspendLayout();
            this.sidebarPanel.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox1.Location = new System.Drawing.Point(-2, -7);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(455, 105);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 17;
            this.pictureBox1.TabStop = false;
            // 
            // panel14
            // 
            this.panel14.Controls.Add(this.pictureBox1);
            this.panel14.Location = new System.Drawing.Point(516, 26);
            this.panel14.Name = "panel14";
            this.panel14.Size = new System.Drawing.Size(445, 90);
            this.panel14.TabIndex = 18;
            // 
            // panel_CONTROL1_SET_1
            // 
            this.panel_CONTROL1_SET_1.Controls.Add(this.But_CONTROL1_SET_1);
            this.panel_CONTROL1_SET_1.Location = new System.Drawing.Point(0, 133);
            this.panel_CONTROL1_SET_1.Name = "panel_CONTROL1_SET_1";
            this.panel_CONTROL1_SET_1.Size = new System.Drawing.Size(749, 85);
            this.panel_CONTROL1_SET_1.TabIndex = 19;
            // 
            // But_CONTROL1_SET_1
            // 
            this.But_CONTROL1_SET_1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("But_CONTROL1_SET_1.BackgroundImage")));
            this.But_CONTROL1_SET_1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.But_CONTROL1_SET_1.Location = new System.Drawing.Point(-5, -4);
            this.But_CONTROL1_SET_1.Name = "But_CONTROL1_SET_1";
            this.But_CONTROL1_SET_1.Size = new System.Drawing.Size(752, 95);
            this.But_CONTROL1_SET_1.TabIndex = 1;
            this.But_CONTROL1_SET_1.UseVisualStyleBackColor = true;
            // 
            // panel_CONTROL1_SET_2
            // 
            this.panel_CONTROL1_SET_2.Controls.Add(this.But_CONTROL1_SET_2);
            this.panel_CONTROL1_SET_2.Location = new System.Drawing.Point(742, 133);
            this.panel_CONTROL1_SET_2.Name = "panel_CONTROL1_SET_2";
            this.panel_CONTROL1_SET_2.Size = new System.Drawing.Size(749, 85);
            this.panel_CONTROL1_SET_2.TabIndex = 20;
            // 
            // But_CONTROL1_SET_2
            // 
            this.But_CONTROL1_SET_2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("But_CONTROL1_SET_2.BackgroundImage")));
            this.But_CONTROL1_SET_2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.But_CONTROL1_SET_2.Location = new System.Drawing.Point(-14, -4);
            this.But_CONTROL1_SET_2.Name = "But_CONTROL1_SET_2";
            this.But_CONTROL1_SET_2.Size = new System.Drawing.Size(769, 95);
            this.But_CONTROL1_SET_2.TabIndex = 4;
            this.But_CONTROL1_SET_2.UseVisualStyleBackColor = true;
            // 
            // panel_Ext_Temp1
            // 
            this.panel_Ext_Temp1.Controls.Add(this.label_Ext1);
            this.panel_Ext_Temp1.Controls.Add(this.pictureBox7);
            this.panel_Ext_Temp1.Location = new System.Drawing.Point(1232, 437);
            this.panel_Ext_Temp1.Name = "panel_Ext_Temp1";
            this.panel_Ext_Temp1.Size = new System.Drawing.Size(190, 90);
            this.panel_Ext_Temp1.TabIndex = 30;
            // 
            // label_Ext1
            // 
            this.label_Ext1.AutoSize = true;
            this.label_Ext1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label_Ext1.Location = new System.Drawing.Point(46, 46);
            this.label_Ext1.Name = "label_Ext1";
            this.label_Ext1.Size = new System.Drawing.Size(32, 37);
            this.label_Ext1.TabIndex = 18;
            this.label_Ext1.Text = "0";
            // 
            // pictureBox7
            // 
            this.pictureBox7.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox7.BackgroundImage")));
            this.pictureBox7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox7.Location = new System.Drawing.Point(-11, -10);
            this.pictureBox7.Name = "pictureBox7";
            this.pictureBox7.Size = new System.Drawing.Size(207, 110);
            this.pictureBox7.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox7.TabIndex = 34;
            this.pictureBox7.TabStop = false;
            // 
            // panel_RPM1
            // 
            this.panel_RPM1.Controls.Add(this.label_RPM1);
            this.panel_RPM1.Controls.Add(this.pictureBox6);
            this.panel_RPM1.Location = new System.Drawing.Point(1154, 260);
            this.panel_RPM1.Name = "panel_RPM1";
            this.panel_RPM1.Size = new System.Drawing.Size(160, 90);
            this.panel_RPM1.TabIndex = 29;
            // 
            // label_RPM1
            // 
            this.label_RPM1.AutoSize = true;
            this.label_RPM1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label_RPM1.Location = new System.Drawing.Point(41, 50);
            this.label_RPM1.Name = "label_RPM1";
            this.label_RPM1.Size = new System.Drawing.Size(32, 37);
            this.label_RPM1.TabIndex = 18;
            this.label_RPM1.Text = "0";
            // 
            // pictureBox6
            // 
            this.pictureBox6.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox6.BackgroundImage")));
            this.pictureBox6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox6.Location = new System.Drawing.Point(-15, -4);
            this.pictureBox6.Name = "pictureBox6";
            this.pictureBox6.Size = new System.Drawing.Size(190, 110);
            this.pictureBox6.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox6.TabIndex = 33;
            this.pictureBox6.TabStop = false;
            // 
            // panel_Dosing1
            // 
            this.panel_Dosing1.Controls.Add(this.label_Dosing1);
            this.panel_Dosing1.Controls.Add(this.pictureBox2);
            this.panel_Dosing1.Location = new System.Drawing.Point(794, 228);
            this.panel_Dosing1.Name = "panel_Dosing1";
            this.panel_Dosing1.Size = new System.Drawing.Size(160, 90);
            this.panel_Dosing1.TabIndex = 28;
            // 
            // label_Dosing1
            // 
            this.label_Dosing1.AutoSize = true;
            this.label_Dosing1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label_Dosing1.Location = new System.Drawing.Point(38, 47);
            this.label_Dosing1.Name = "label_Dosing1";
            this.label_Dosing1.Size = new System.Drawing.Size(32, 37);
            this.label_Dosing1.TabIndex = 18;
            this.label_Dosing1.Text = "0";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox2.BackgroundImage")));
            this.pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox2.Location = new System.Drawing.Point(-15, -5);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(190, 110);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox2.TabIndex = 32;
            this.pictureBox2.TabStop = false;
            // 
            // panel_TJ1
            // 
            this.panel_TJ1.Controls.Add(this.label_TJ1);
            this.panel_TJ1.Controls.Add(this.pictureBox5);
            this.panel_TJ1.Location = new System.Drawing.Point(568, 555);
            this.panel_TJ1.Name = "panel_TJ1";
            this.panel_TJ1.Size = new System.Drawing.Size(160, 90);
            this.panel_TJ1.TabIndex = 27;
            // 
            // label_TJ1
            // 
            this.label_TJ1.AutoSize = true;
            this.label_TJ1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label_TJ1.Location = new System.Drawing.Point(35, 46);
            this.label_TJ1.Name = "label_TJ1";
            this.label_TJ1.Size = new System.Drawing.Size(32, 37);
            this.label_TJ1.TabIndex = 18;
            this.label_TJ1.Text = "0";
            // 
            // pictureBox5
            // 
            this.pictureBox5.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox5.BackgroundImage")));
            this.pictureBox5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox5.Location = new System.Drawing.Point(-18, -10);
            this.pictureBox5.Name = "pictureBox5";
            this.pictureBox5.Size = new System.Drawing.Size(190, 110);
            this.pictureBox5.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox5.TabIndex = 17;
            this.pictureBox5.TabStop = false;
            // 
            // panel_TR_TJ1
            // 
            this.panel_TR_TJ1.Controls.Add(this.label_TR_TJ1);
            this.panel_TR_TJ1.Controls.Add(this.pictureBox4);
            this.panel_TR_TJ1.Location = new System.Drawing.Point(568, 437);
            this.panel_TR_TJ1.Name = "panel_TR_TJ1";
            this.panel_TR_TJ1.Size = new System.Drawing.Size(160, 90);
            this.panel_TR_TJ1.TabIndex = 26;
            // 
            // label_TR_TJ1
            // 
            this.label_TR_TJ1.AutoSize = true;
            this.label_TR_TJ1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label_TR_TJ1.Location = new System.Drawing.Point(35, 46);
            this.label_TR_TJ1.Name = "label_TR_TJ1";
            this.label_TR_TJ1.Size = new System.Drawing.Size(32, 37);
            this.label_TR_TJ1.TabIndex = 19;
            this.label_TR_TJ1.Text = "0";
            // 
            // pictureBox4
            // 
            this.pictureBox4.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox4.BackgroundImage")));
            this.pictureBox4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox4.Location = new System.Drawing.Point(-17, -10);
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.Size = new System.Drawing.Size(190, 110);
            this.pictureBox4.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox4.TabIndex = 17;
            this.pictureBox4.TabStop = false;
            // 
            // panel_TR1
            // 
            this.panel_TR1.Controls.Add(this.label_TR1);
            this.panel_TR1.Controls.Add(this.pictureBox3);
            this.panel_TR1.Location = new System.Drawing.Point(568, 320);
            this.panel_TR1.Name = "panel_TR1";
            this.panel_TR1.Size = new System.Drawing.Size(160, 90);
            this.panel_TR1.TabIndex = 25;
            // 
            // label_TR1
            // 
            this.label_TR1.AutoSize = true;
            this.label_TR1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label_TR1.Location = new System.Drawing.Point(35, 47);
            this.label_TR1.Name = "label_TR1";
            this.label_TR1.Size = new System.Drawing.Size(32, 37);
            this.label_TR1.TabIndex = 17;
            this.label_TR1.Text = "0";
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox3.BackgroundImage")));
            this.pictureBox3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox3.Location = new System.Drawing.Point(-17, -9);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(190, 110);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox3.TabIndex = 18;
            this.pictureBox3.TabStop = false;
            // 
            // panel_Graph1_Data
            // 
            this.panel_Graph1_Data.Controls.Add(this.But_Graph_Data1);
            this.panel_Graph1_Data.Location = new System.Drawing.Point(19, 618);
            this.panel_Graph1_Data.Name = "panel_Graph1_Data";
            this.panel_Graph1_Data.Size = new System.Drawing.Size(415, 100);
            this.panel_Graph1_Data.TabIndex = 24;
            // 
            // But_Graph_Data1
            // 
            this.But_Graph_Data1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("But_Graph_Data1.BackgroundImage")));
            this.But_Graph_Data1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.But_Graph_Data1.Location = new System.Drawing.Point(-17, -5);
            this.But_Graph_Data1.Name = "But_Graph_Data1";
            this.But_Graph_Data1.Size = new System.Drawing.Size(445, 110);
            this.But_Graph_Data1.TabIndex = 1;
            this.But_Graph_Data1.UseVisualStyleBackColor = true;
            // 
            // panel_Program1_Sequence1
            // 
            this.panel_Program1_Sequence1.Controls.Add(this.But_Program_Sequence1);
            this.panel_Program1_Sequence1.Location = new System.Drawing.Point(19, 505);
            this.panel_Program1_Sequence1.Name = "panel_Program1_Sequence1";
            this.panel_Program1_Sequence1.Size = new System.Drawing.Size(415, 100);
            this.panel_Program1_Sequence1.TabIndex = 23;
            // 
            // But_Program_Sequence1
            // 
            this.But_Program_Sequence1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("But_Program_Sequence1.BackgroundImage")));
            this.But_Program_Sequence1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.But_Program_Sequence1.Location = new System.Drawing.Point(-17, -7);
            this.But_Program_Sequence1.Name = "But_Program_Sequence1";
            this.But_Program_Sequence1.Size = new System.Drawing.Size(445, 110);
            this.But_Program_Sequence1.TabIndex = 1;
            this.But_Program_Sequence1.UseVisualStyleBackColor = true;
            // 
            // panel_Motor_Stirrer1
            // 
            this.panel_Motor_Stirrer1.Controls.Add(this.text1_Motor_Stirrer_Set1);
            this.panel_Motor_Stirrer1.Controls.Add(this.pict_Motor_Stirrer1);
            this.panel_Motor_Stirrer1.Location = new System.Drawing.Point(19, 386);
            this.panel_Motor_Stirrer1.Name = "panel_Motor_Stirrer1";
            this.panel_Motor_Stirrer1.Size = new System.Drawing.Size(415, 100);
            this.panel_Motor_Stirrer1.TabIndex = 22;
            // 
            // text1_Motor_Stirrer_Set1
            // 
            this.text1_Motor_Stirrer_Set1.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.text1_Motor_Stirrer_Set1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.text1_Motor_Stirrer_Set1.BackColor = System.Drawing.SystemColors.HighlightText;
            this.text1_Motor_Stirrer_Set1.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.text1_Motor_Stirrer_Set1.Location = new System.Drawing.Point(27, 41);
            this.text1_Motor_Stirrer_Set1.Name = "text1_Motor_Stirrer_Set1";
            this.text1_Motor_Stirrer_Set1.Size = new System.Drawing.Size(112, 40);
            this.text1_Motor_Stirrer_Set1.TabIndex = 77;
            this.text1_Motor_Stirrer_Set1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // pict_Motor_Stirrer1
            // 
            this.pict_Motor_Stirrer1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pict_Motor_Stirrer1.BackgroundImage")));
            this.pict_Motor_Stirrer1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pict_Motor_Stirrer1.Location = new System.Drawing.Point(-16, -5);
            this.pict_Motor_Stirrer1.Name = "pict_Motor_Stirrer1";
            this.pict_Motor_Stirrer1.Size = new System.Drawing.Size(445, 110);
            this.pict_Motor_Stirrer1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pict_Motor_Stirrer1.TabIndex = 33;
            this.pict_Motor_Stirrer1.TabStop = false;
            // 
            // panel_Thermostat1
            // 
            this.panel_Thermostat1.Controls.Add(this.text1_Thermo_Set1);
            this.panel_Thermostat1.Controls.Add(this.pict_Thermostat1);
            this.panel_Thermostat1.Location = new System.Drawing.Point(19, 269);
            this.panel_Thermostat1.Name = "panel_Thermostat1";
            this.panel_Thermostat1.Size = new System.Drawing.Size(415, 100);
            this.panel_Thermostat1.TabIndex = 21;
            // 
            // text1_Thermo_Set1
            // 
            this.text1_Thermo_Set1.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.text1_Thermo_Set1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.text1_Thermo_Set1.BackColor = System.Drawing.SystemColors.HighlightText;
            this.text1_Thermo_Set1.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.text1_Thermo_Set1.Location = new System.Drawing.Point(27, 38);
            this.text1_Thermo_Set1.Name = "text1_Thermo_Set1";
            this.text1_Thermo_Set1.Size = new System.Drawing.Size(112, 40);
            this.text1_Thermo_Set1.TabIndex = 76;
            this.text1_Thermo_Set1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // pict_Thermostat1
            // 
            this.pict_Thermostat1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pict_Thermostat1.BackgroundImage")));
            this.pict_Thermostat1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pict_Thermostat1.Location = new System.Drawing.Point(-16, -9);
            this.pict_Thermostat1.Name = "pict_Thermostat1";
            this.pict_Thermostat1.Size = new System.Drawing.Size(445, 110);
            this.pict_Thermostat1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pict_Thermostat1.TabIndex = 32;
            this.pict_Thermostat1.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.but_Setting1);
            this.panel1.Location = new System.Drawing.Point(19, 736);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(415, 100);
            this.panel1.TabIndex = 31;
            // 
            // but_Setting1
            // 
            this.but_Setting1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("but_Setting1.BackgroundImage")));
            this.but_Setting1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.but_Setting1.Location = new System.Drawing.Point(-17, -5);
            this.but_Setting1.Name = "but_Setting1";
            this.but_Setting1.Size = new System.Drawing.Size(445, 110);
            this.but_Setting1.TabIndex = 1;
            this.but_Setting1.UseVisualStyleBackColor = true;
            // 
            // mainContainer
            // 
            this.mainContainer.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.mainContainer.Controls.Add(this.sidebarPanel);
            this.mainContainer.Controls.Add(this.toggleSidebarButton);
            this.mainContainer.Location = new System.Drawing.Point(18, 15);
            this.mainContainer.Name = "mainContainer";
            this.mainContainer.Size = new System.Drawing.Size(492, 108);
            this.mainContainer.TabIndex = 77;
            // 
            // sidebarPanel
            // 
            this.sidebarPanel.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.sidebarPanel.Controls.Add(this.panel2);
            this.sidebarPanel.Controls.Add(this.switch_Connect);
            this.sidebarPanel.Controls.Add(this.lblSidebarTitle);
            this.sidebarPanel.Controls.Add(this.cmbModeSelector);
            this.sidebarPanel.Location = new System.Drawing.Point(105, 5);
            this.sidebarPanel.Name = "sidebarPanel";
            this.sidebarPanel.Size = new System.Drawing.Size(380, 96);
            this.sidebarPanel.TabIndex = 83;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.picStatusIndicator);
            this.panel2.Location = new System.Drawing.Point(311, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(60, 57);
            this.panel2.TabIndex = 82;
            // 
            // picStatusIndicator
            // 
            this.picStatusIndicator.ImageConnected = ((System.Drawing.Image)(resources.GetObject("picStatusIndicator.ImageConnected")));
            this.picStatusIndicator.ImageConnecting = ((System.Drawing.Image)(resources.GetObject("picStatusIndicator.ImageConnecting")));
            this.picStatusIndicator.ImageDisconnected = ((System.Drawing.Image)(resources.GetObject("picStatusIndicator.ImageDisconnected")));
            this.picStatusIndicator.Location = new System.Drawing.Point(0, 0);
            this.picStatusIndicator.Mode = "Disconnected";
            this.picStatusIndicator.Name = "picStatusIndicator";
            this.picStatusIndicator.Size = new System.Drawing.Size(61, 57);
            this.picStatusIndicator.TabIndex = 83;
            // 
            // switch_Connect
            // 
            this.switch_Connect.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("switch_Connect.BackgroundImage")));
            this.switch_Connect.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.switch_Connect.IsOn = false;
            this.switch_Connect.Location = new System.Drawing.Point(158, 3);
            this.switch_Connect.Name = "switch_Connect";
            this.switch_Connect.OffImage = ((System.Drawing.Image)(resources.GetObject("switch_Connect.OffImage")));
            this.switch_Connect.OnImage = ((System.Drawing.Image)(resources.GetObject("switch_Connect.OnImage")));
            this.switch_Connect.Size = new System.Drawing.Size(76, 57);
            this.switch_Connect.TabIndex = 78;
            // 
            // lblSidebarTitle
            // 
            this.lblSidebarTitle.AutoSize = true;
            this.lblSidebarTitle.Font = new System.Drawing.Font("MS Reference Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSidebarTitle.Location = new System.Drawing.Point(14, 59);
            this.lblSidebarTitle.Name = "lblSidebarTitle";
            this.lblSidebarTitle.Size = new System.Drawing.Size(74, 24);
            this.lblSidebarTitle.TabIndex = 0;
            this.lblSidebarTitle.Text = "label1";
            // 
            // cmbModeSelector
            // 
            this.cmbModeSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModeSelector.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbModeSelector.FormattingEnabled = true;
            this.cmbModeSelector.Location = new System.Drawing.Point(14, 7);
            this.cmbModeSelector.Name = "cmbModeSelector";
            this.cmbModeSelector.Size = new System.Drawing.Size(127, 41);
            this.cmbModeSelector.TabIndex = 76;
            // 
            // toggleSidebarButton
            // 
            this.toggleSidebarButton.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("toggleSidebarButton.BackgroundImage")));
            this.toggleSidebarButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.toggleSidebarButton.IsOn = false;
            this.toggleSidebarButton.Location = new System.Drawing.Point(9, 8);
            this.toggleSidebarButton.Name = "toggleSidebarButton";
            this.toggleSidebarButton.OffImage = ((System.Drawing.Image)(resources.GetObject("toggleSidebarButton.OffImage")));
            this.toggleSidebarButton.OnImage = ((System.Drawing.Image)(resources.GetObject("toggleSidebarButton.OnImage")));
            this.toggleSidebarButton.Size = new System.Drawing.Size(71, 61);
            this.toggleSidebarButton.TabIndex = 78;
            // 
            // switch_Target1_Set1
            // 
            this.switch_Target1_Set1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("switch_Target1_Set1.BackgroundImage")));
            this.switch_Target1_Set1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.switch_Target1_Set1.IsOn = false;
            this.switch_Target1_Set1.Location = new System.Drawing.Point(502, 212);
            this.switch_Target1_Set1.Name = "switch_Target1_Set1";
            this.switch_Target1_Set1.OffImage = ((System.Drawing.Image)(resources.GetObject("switch_Target1_Set1.OffImage")));
            this.switch_Target1_Set1.OnImage = ((System.Drawing.Image)(resources.GetObject("switch_Target1_Set1.OnImage")));
            this.switch_Target1_Set1.Size = new System.Drawing.Size(108, 72);
            this.switch_Target1_Set1.TabIndex = 75;
            // 
            // switch_A_M1
            // 
            this.switch_A_M1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("switch_A_M1.BackgroundImage")));
            this.switch_A_M1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.switch_A_M1.IsOn = false;
            this.switch_A_M1.Location = new System.Drawing.Point(341, 223);
            this.switch_A_M1.Name = "switch_A_M1";
            this.switch_A_M1.OffImage = ((System.Drawing.Image)(resources.GetObject("switch_A_M1.OffImage")));
            this.switch_A_M1.OnImage = ((System.Drawing.Image)(resources.GetObject("switch_A_M1.OnImage")));
            this.switch_A_M1.Size = new System.Drawing.Size(119, 61);
            this.switch_A_M1.TabIndex = 74;
            // 
            // UC_CONTROL_SET_1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.HighlightText;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Controls.Add(this.mainContainer);
            this.Controls.Add(this.switch_Target1_Set1);
            this.Controls.Add(this.switch_A_M1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel_Ext_Temp1);
            this.Controls.Add(this.panel_RPM1);
            this.Controls.Add(this.panel_Dosing1);
            this.Controls.Add(this.panel_TJ1);
            this.Controls.Add(this.panel_TR_TJ1);
            this.Controls.Add(this.panel_TR1);
            this.Controls.Add(this.panel_Graph1_Data);
            this.Controls.Add(this.panel_Program1_Sequence1);
            this.Controls.Add(this.panel_Motor_Stirrer1);
            this.Controls.Add(this.panel_Thermostat1);
            this.Controls.Add(this.panel_CONTROL1_SET_2);
            this.Controls.Add(this.panel_CONTROL1_SET_1);
            this.Controls.Add(this.panel14);
            this.Name = "UC_CONTROL_SET_1";
            this.Size = new System.Drawing.Size(1488, 1000);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel14.ResumeLayout(false);
            this.panel_CONTROL1_SET_1.ResumeLayout(false);
            this.panel_CONTROL1_SET_2.ResumeLayout(false);
            this.panel_Ext_Temp1.ResumeLayout(false);
            this.panel_Ext_Temp1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox7)).EndInit();
            this.panel_RPM1.ResumeLayout(false);
            this.panel_RPM1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox6)).EndInit();
            this.panel_Dosing1.ResumeLayout(false);
            this.panel_Dosing1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panel_TJ1.ResumeLayout(false);
            this.panel_TJ1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox5)).EndInit();
            this.panel_TR_TJ1.ResumeLayout(false);
            this.panel_TR_TJ1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox4)).EndInit();
            this.panel_TR1.ResumeLayout(false);
            this.panel_TR1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.panel_Graph1_Data.ResumeLayout(false);
            this.panel_Program1_Sequence1.ResumeLayout(false);
            this.panel_Motor_Stirrer1.ResumeLayout(false);
            this.panel_Motor_Stirrer1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pict_Motor_Stirrer1)).EndInit();
            this.panel_Thermostat1.ResumeLayout(false);
            this.panel_Thermostat1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pict_Thermostat1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.mainContainer.ResumeLayout(false);
            this.sidebarPanel.ResumeLayout(false);
            this.sidebarPanel.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private PictureBox pictureBox1;
        private Panel panel14;
        private Panel panel_CONTROL1_SET_1;
        private Button But_CONTROL1_SET_1;
        private Panel panel_CONTROL1_SET_2;
        private Button But_CONTROL1_SET_2;
        private Panel panel_Ext_Temp1;
        private Label label_Ext1;
        private Panel panel_RPM1;
        private Label label_RPM1;
        private Panel panel_Dosing1;
        private Label label_Dosing1;
        private Panel panel_TJ1;
        private Label label_TJ1;
        private PictureBox pictureBox5;
        private Panel panel_TR_TJ1;
        private Label label_TR_TJ1;
        private PictureBox pictureBox4;
        private Panel panel_TR1;
        private Label label_TR1;
        private PictureBox pictureBox3;
        private Panel panel_Graph1_Data;
        private Button But_Graph_Data1;
        private Panel panel_Program1_Sequence1;
        private Button But_Program_Sequence1;
        private Panel panel_Motor_Stirrer1;
        private Panel panel_Thermostat1;
        private Panel panel1;
        private Button but_Setting1;
        private PictureBox pictureBox7;
        private PictureBox pictureBox6;
        private PictureBox pictureBox2;
        private PictureBox pict_Thermostat1;
        private PictureBox pict_Motor_Stirrer1;
        private Switch_A_M switch_A_M1;
        private TextBox text1_Motor_Stirrer_Set1;
        protected TextBox text1_Thermo_Set1;
        private Switch_Target1 switch_Target1_Set1;
        private Panel mainContainer;
        private Switch_Target1 switch_Connect;
        private Label lblSidebarTitle;
        private Timer sidebarAnimationTimer;
        private Panel sidebarPanel;
        private ComboBox cmbModeSelector;
        private Switch_Target1 toggleSidebarButton;
        private Panel panel2;
        private Lamp_Connect picStatusIndicator;
        private Timer timer1;

        public EventHandler UC_CONTROL_SET_1_Load { get; private set; }
    }
}
