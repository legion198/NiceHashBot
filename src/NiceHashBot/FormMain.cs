﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NiceHashBotLib;

namespace NiceHashBot
{
    public partial class FormMain : Form
    {
        public static FormPool FormPoolInstance;
        public static FormNewOrder FormNewOrderInstance;

        private Timer TimerRefresh;
        private Timer BalanceRefresh;

        public FormMain()
        {
            InitializeComponent();
            this.Text = Application.ProductName + " [" + Application.ProductVersion + "]";

            FormPoolInstance = null;
            FormNewOrderInstance = null;

            TimerRefresh = new Timer();
            TimerRefresh.Interval = 500;
            TimerRefresh.Tick += new EventHandler(TimerRefresh_Tick);
            TimerRefresh.Start();

            BalanceRefresh = new Timer();
            BalanceRefresh.Interval = 30 * 1000;
            BalanceRefresh.Tick += new EventHandler(BalanceRefresh_Tick);
            BalanceRefresh.Start();
        }

        private void BalanceRefresh_Tick(object sender, EventArgs e)
        {
            if (!APIWrapper.ValidAuthorization) return;

            APIBalance Balance = APIWrapper.GetBalance();
            if (Balance == null)
            {
                toolStripLabel2.Text = "";
            }
            else
            {
                toolStripLabel2.Text = Balance.Confirmed.ToString("F8") + " BTC";
            }
        }

        private void TimerRefresh_Tick(object sender, EventArgs e)
        {
            if (!APIWrapper.ValidAuthorization) return;

            OrderContainer[] Orders = OrderContainer.GetAll();
            int Selected = -1;
            if (listView1.SelectedIndices.Count > 0)
                Selected = listView1.SelectedIndices[0];
            listView1.Items.Clear();
            for (int i = 0; i < Orders.Length; i++)
            {
                ListViewItem LVI = new ListViewItem(APIWrapper.SERVICE_NAME[Orders[i].ServiceLocation]);
                LVI.SubItems.Add(APIWrapper.ALGORITHM_NAME[Orders[i].Algorithm]);
                if (Orders[i].OrderStats != null)
                {
                    LVI.SubItems.Add("#" + Orders[i].OrderStats.ID.ToString());
                    string PriceText = Orders[i].OrderStats.Price.ToString("F4") + " (" + Orders[i].MaxPrice.ToString("F4") + ")";
                    if (Orders[i].Algorithm == 1)
                        PriceText += " BTC/TH/Day";
                    else
                        PriceText += " BTC/GH/Day";
                    LVI.SubItems.Add(PriceText);
                    LVI.SubItems.Add(Orders[i].OrderStats.BTCAvailable.ToString("F8"));
                    LVI.SubItems.Add(Orders[i].OrderStats.Workers.ToString());
                    string SpeedText = "";
                    if (Orders[i].Algorithm == 1)
                        SpeedText = (Orders[i].OrderStats.Speed * 0.001).ToString("F4") + " (" + Orders[i].Limit.ToString("F2") + ") TH/s";
                    else
                        SpeedText = Orders[i].OrderStats.Speed.ToString("F4") + " (" + Orders[i].Limit.ToString("F2") + ") GH/s";
                    LVI.SubItems.Add(SpeedText);
                    if (!Orders[i].OrderStats.Alive)
                        LVI.BackColor = Color.PaleVioletRed;
                    else
                        LVI.BackColor = Color.LightGreen;
                    LVI.SubItems.Add("View competing orders");
                }

                if (Selected >= 0 && Selected == i)
                    LVI.Selected = true;

                listView1.Items.Add(LVI);
            }
        }

        private void createNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FormNewOrderInstance == null)
            {
                FormNewOrderInstance = new FormNewOrder();
                FormNewOrderInstance.Show();
            }
        }

        private void poolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormPools();
        }


        public static void ShowFormPools()
        {
            if (FormPoolInstance == null)
            {
                FormPoolInstance = new FormPool();
                FormPoolInstance.Show();
            }
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.nicehash.com/index.jsp?p=wallet");
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            OrderContainer.Remove(listView1.SelectedIndices[0]);
            TimerRefresh_Tick(sender, e);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings FS = new FormSettings(SettingsContainer.Settings.APIID, SettingsContainer.Settings.APIKey, SettingsContainer.Settings.TwoFactorSecret);
            if (FS.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SettingsContainer.Settings.APIID = FS.ID;
                SettingsContainer.Settings.APIKey = FS.Key;
                SettingsContainer.Settings.TwoFactorSecret = FS.TwoFASecret;
                SettingsContainer.Commit();

                if (SettingsContainer.Settings.TwoFactorSecret.Length > 0)
                    APIWrapper.TwoFASecret = SettingsContainer.Settings.TwoFactorSecret;
                else
                    APIWrapper.TwoFASecret = null;

                if (APIWrapper.Initialize(SettingsContainer.Settings.APIID.ToString(), SettingsContainer.Settings.APIKey))
                {
                    BalanceRefresh_Tick(sender, e);
                }
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            settingsToolStripMenuItem_Click(sender, e);
        }

        private void setToUnlimitedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            OrderContainer.SetLimit(listView1.SelectedIndices[0], 0);
        }

        private void setToToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            FormNumberInput FNI = new FormNumberInput("Set new limit", 0, 1000, OrderContainer.GetLimit(listView1.SelectedIndices[0]), 2);
            if (FNI.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OrderContainer.SetLimit(listView1.SelectedIndices[0], FNI.Value);
            }
        }

        private void setMaximalPriceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            FormNumberInput FNI = new FormNumberInput("Set new price", 0.0001, 100, OrderContainer.GetMaxPrice(listView1.SelectedIndices[0]), 4);
            if (FNI.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OrderContainer.SetMaxPrice(listView1.SelectedIndices[0], FNI.Value);
            }
        }
    }
}
