﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.Config;
using System.Security.Policy;
using System.IO;
using Spine;
using Newtonsoft.Json.Linq;

namespace WzComparerR2
{
    public partial class FrmOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOptions()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif

            cmbWzEncoding.Items.AddRange(new[]
            {
                new ComboItem("Default"){ Value = 0 },
                new ComboItem("Shift-JIS (JMS)"){ Value = 932 },
                new ComboItem("GB 2312 (CMS)"){ Value = 936 },
                new ComboItem("EUC-KR (KMS)"){ Value = 949 },
                new ComboItem("Big5 (TMS)"){ Value = 950 },
                new ComboItem("ISO 8859-1 (GMS)"){ Value = 1252 },
                new ComboItem("ASCII"){ Value = -1 },
            });

            cmbWzVersionVerifyMode.Items.AddRange(new[]
            {
                new ComboItem("Fast"){ Value = WzLib.WzVersionVerifyMode.Fast },
                new ComboItem("Legacy"){ Value = WzLib.WzVersionVerifyMode.Default },
            });

            cmbDesiredLanguage.Items.AddRange(new[]
            {
                new ComboItem("Cantonese (HKMS)"){ Value = "yue" },
                new ComboItem("Chinese Simplified (CMS)"){ Value = "zh-CN" },
                new ComboItem("Chinese Traditional (TMS)"){ Value = "zh-TW" },
                new ComboItem("English (GMS/MSEA)"){ Value = "en" },
                new ComboItem("Japanese (JMS)"){ Value = "ja" },
                new ComboItem("Korean (KMS)"){ Value = "ko" },
            });

            cmbMozhiBackend.Items.AddRange(new[]
            {
                new ComboItem("mozhi.aryak.me"){ Value = "https://mozhi.aryak.me" },
                new ComboItem("translate.bus-hit.me"){ Value = "https://translate.bus-hit.me" },
                new ComboItem("nyc1.mz.ggtyler.dev"){ Value = "https://nyc1.mz.ggtyler.dev" },
                new ComboItem("translate.projectsegfau.lt"){ Value = "https://translate.projectsegfau.lt" },
                new ComboItem("translate.nerdvpn.de"){ Value = "https://translate.nerdvpn.de" },
                new ComboItem("mozhi.ducks.party"){ Value = "https://mozhi.ducks.party" },
                new ComboItem("mozhi.frontendfriendly.xyz"){ Value = "https://mozhi.frontendfriendly.xyz" },
                new ComboItem("mozhi.pussthecat.org"){ Value = "https://mozhi.pussthecat.org" },
                new ComboItem("mo.zorby.top"){ Value = "https://mo.zorby.top" },
                new ComboItem("mozhi.adminforge.de"){ Value = "https://mozhi.adminforge.de" },
                new ComboItem("translate.privacyredirect.com"){ Value = "https://translate.privacyredirect.com" },
                new ComboItem("mozhi.canine.tools"){ Value = "https://mozhi.canine.tools" },
                new ComboItem("mozhi.gitro.xyz"){ Value = "https://mozhi.gitro.xyz" },
                new ComboItem("api.hikaricalyx.com"){ Value = "https://api.hikaricalyx.com/mozhi" },
            });

            cmbPreferredTranslateEngine.Items.AddRange(new[]
            {
                new ComboItem("Google (Non-Mozhi)"){ Value = 0 },
                new ComboItem("Google"){ Value = 1 },
                new ComboItem("DeepL"){ Value = 2 },
                new ComboItem("DuckDuckGo / Bing"){ Value = 3 },
                new ComboItem("MyMemory"){ Value = 4 },
                new ComboItem("Yandex"){ Value = 5 },
                new ComboItem("Naver Papago (Non-Mozhi)"){ Value = 6 },
                new ComboItem("OpenAI Compatible"){ Value = 9 },
            });

            cmbPreferredLayout.Items.AddRange(new[]
            {
                new ComboItem("Do not translate"){ Value = 0 },
                new ComboItem("Show translated text first"){ Value = 1 },
                new ComboItem("Show original text first"){ Value = 2 },
                new ComboItem("Show translated text only"){ Value = 3 },
            });

            cmbDetectCurrency.Items.AddRange(new[]
            {
                new ComboItem("Auto Detect"){ Value = "auto" },
                new ComboItem("Chinese Yuan (CNY)"){ Value = "cny" },
                new ComboItem("Japanese Yen (JPY)"){ Value = "jpy" },
                new ComboItem("Korean Won (KRW)"){ Value = "krw" },
                new ComboItem("New Taiwan Dollar (NTD)"){ Value = "twd" },
                new ComboItem("Singapore Dollar (SGD)"){ Value = "sgd" },
                new ComboItem("US Dollar (USD)"){ Value = "usd" },
            });

            cmbDesiredCurrency.Items.AddRange(new[]
            {
                new ComboItem("Do not convert"){ Value = "none" },
                new ComboItem("Australian Dollar (AUD)"){ Value = "aud" },
                new ComboItem("Canadian Dollar (CAD)"){ Value = "cad" },
                new ComboItem("Chinese Yuan (CNY)"){ Value = "cny" },
                new ComboItem("Euro (EUR)"){ Value = "eur" },
                new ComboItem("Hong Kong Dollar (HKD)"){ Value = "hkd" },
                new ComboItem("Japanese Yen (JPY)"){ Value = "jpy" },
                new ComboItem("Korean Won (KRW)"){ Value = "krw" },
                new ComboItem("Macau Pataca (MOP)"){ Value = "mop" },
                new ComboItem("Malaysian Ringgit (MYR)"){ Value = "myr" },
                new ComboItem("New Taiwan Dollar (NTD)"){ Value = "twd" },
                new ComboItem("Singapore Dollar (SGD)"){ Value = "sgd" },
                new ComboItem("US Dollar (USD)"){ Value = "usd" },
            });

            cmbLanguageModel.Items.AddRange(new[]
            {
                new ComboItem("Keep Current"){ Value = "none" },
            });
        }

        public bool SortWzOnOpened
        {
            get { return chkWzAutoSort.Checked; }
            set { chkWzAutoSort.Checked = value; }
        }

        public bool SortWzByImgID
        {
            get { return chkWzSortByImgID.Checked; }
            set { chkWzSortByImgID.Checked = value; }
        }

        public int DefaultWzCodePage
        {
            get
            {
                return ((cmbWzEncoding.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbWzEncoding.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbWzEncoding.SelectedItem = item;
            }
        }

        public bool AutoDetectExtFiles
        {
            get { return chkAutoCheckExtFiles.Checked; }
            set { chkAutoCheckExtFiles.Checked = value; }
        }

        public bool ImgCheckDisabled
        {
            get { return chkImgCheckDisabled.Checked; }
            set { chkImgCheckDisabled.Checked = value; }
        }

        public string OpenAIBackend
        {
            get { return txtOpenAIBackend.Text; }
            set { txtOpenAIBackend.Text = value; }
        }

        public string OpenAISystemMessage
        {
            get { return txtOpenAISystemMessage.Text; }
            set { txtOpenAISystemMessage.Text = value; }
        }

        public string NxSecretKey
        {
            get { return txtSecretkey.Text; }
            set { txtSecretkey.Text = value;}
        }

        public int PreferredLayout
        {
            get
            {
                return ((cmbPreferredLayout.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbPreferredLayout.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbPreferredLayout.SelectedItem = item;
            }
        }

        public string MozhiBackend
        {
            get
            {
                return ((cmbMozhiBackend.SelectedItem as ComboItem)?.Value as string) ?? "https://mozhi.aryak.me";
            }
            set
            {
                var items = cmbMozhiBackend.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbMozhiBackend.SelectedItem = item;
            }
        }

        public string LanguageModel
        {
            get
            {
                return ((cmbLanguageModel.SelectedItem as ComboItem)?.Value as string) ?? "";
            }
            set
            {
                var items = cmbLanguageModel.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbLanguageModel.SelectedItem = item;
            }
        }
        public bool OpenAIExtraOption
        {
            get { return chkOpenAIExtraOption.Checked; }
            set { chkOpenAIExtraOption.Checked = value; }
        }

        public double LMTemperature
        {
            get
            {
                return Double.Parse(txtLMTemperature.Text);
            }
            set
            {
                txtLMTemperature.Text = value.ToString();
            }
        }
        public int MaximumToken
        {
            get
            {
                return int.Parse(txtMaximumToken.Text);
            }
            set
            {
                txtMaximumToken.Text = value.ToString();
            }
        }

        public string DetectCurrency
        {
            get
            {
                return ((cmbDetectCurrency.SelectedItem as ComboItem)?.Value as string) ?? "auto";
            }
            set
            {
                var items = cmbDetectCurrency.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbDetectCurrency.SelectedItem = item;
            }
        }

        public string DesiredCurrency
        {
            get
            {
                return ((cmbDesiredCurrency.SelectedItem as ComboItem)?.Value as string) ?? "jpy";
            }
            set
            {
                var items = cmbDesiredCurrency.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbDesiredCurrency.SelectedItem = item;
            }
        }

        public int PreferredTranslateEngine
        {
            get
            {
                return ((cmbPreferredTranslateEngine.SelectedItem as ComboItem)?.Value as int?) ?? 0;
            }
            set
            {
                var items = cmbPreferredTranslateEngine.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as int? == value)
                    ?? items.Last();
                item.Value = value;
                cmbPreferredTranslateEngine.SelectedItem = item;
            }
        }

        public string DesiredLanguage
        {
            get
            {
                return ((cmbDesiredLanguage.SelectedItem as ComboItem)?.Value as string) ?? "ja";
            }
            set
            {
                var items = cmbDesiredLanguage.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as string == value)
                    ?? items.Last();
                item.Value = value;
                cmbDesiredLanguage.SelectedItem = item;
            }
        }

        private void buttonXCheck2_Click(object sender, EventArgs e)
        {
            ComboItem selectedItem = (ComboItem)cmbPreferredTranslateEngine.SelectedItem;
            string respText;
            HttpWebRequest req;
            string backendAddress;
            if (string.IsNullOrEmpty(OpenAIBackend))
            {
                backendAddress = txtOpenAIBackend.WatermarkText;
            }
            else
            {
                backendAddress = OpenAIBackend;
            }
            switch ((int)selectedItem.Value)
            {
                case 8:
                case 9:
                    req = WebRequest.Create(backendAddress + "/models") as HttpWebRequest;
                    req.Timeout = 15000;
                    if (!string.IsNullOrEmpty(NxSecretKey))
                    {
                        JObject reqHeaders = JObject.Parse(NxSecretKey);
                        foreach (var property in reqHeaders.Properties()) req.Headers.Add(property.Name, property.Value.ToString());
                    }
                    try
                    {
                        string respJson = new StreamReader(req.GetResponse().GetResponseStream(), Encoding.UTF8).ReadToEnd();
                        JObject jsonResp = JObject.Parse(respJson);
                        JArray dataArray = (JArray)jsonResp["data"];
                        StringBuilder sb = new StringBuilder();
                        cmbLanguageModel.Items.Clear();
                        foreach (JObject dataItem in dataArray)
                        {
                            sb.AppendLine(dataItem["id"].ToString());
                            cmbLanguageModel.Items.Add(new ComboItem(dataItem["id"].ToString()) { Value = dataItem["id"].ToString() });
                        }
                        cmbLanguageModel.SelectedIndex = 0;
                        respText = sb.ToString();
                    }
                    catch
                    {
                        respText = "This API is invalid.";
                    }
                    break;
                default:
                    req = WebRequest.Create((cmbMozhiBackend.SelectedItem as ComboItem)?.Value + "/api/engines") as HttpWebRequest;
                    req.Timeout = 15000;
                    try
                    {
                        string respJson = new StreamReader(req.GetResponse().GetResponseStream(), Encoding.UTF8).ReadToEnd();
                        if (respJson.Contains("All Engines"))
                        {
                            respText = "This Mozhi Server is valid.";
                        }
                        else
                        {
                            respText = "This Mozhi Server is invalid.";
                        }
                    }
                    catch (WebException ex)
                    {
                        respText = "This Mozhi Server is invalid.";
                    }
                    catch (Exception ex)
                    {
                        respText = "An unknown error has occurred: " + ex;
                    }
                    break;
            }
            MessageBoxEx.Show(respText);
        }

        private void buttonXCheck3_Click(object sender, EventArgs e)
        {
            string respText;
            JObject testJObject;
            try
            {
                testJObject = JObject.Parse(txtSecretkey.Text);
                respText = "The JSON is valid. ";
            }
            catch
            {
                respText = "The JSON is invalid. ";
            }
            MessageBoxEx.Show(respText);
        }

        private void cmbPreferredTranslateEngine_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboItem selectedItem = (ComboItem)cmbPreferredTranslateEngine.SelectedItem;
            switch ((int)selectedItem.Value)
            {
                case 0:
                case 6:
                    labelX5.Visible = true;
                    labelX5.Enabled = false;
                    labelX11.Visible = false;
                    labelX12.Enabled = false;
                    labelX13.Enabled = false;
                    labelX14.Enabled = false;
                    labelX15.Enabled = false;
                    txtOpenAIBackend.Enabled = false;
                    txtOpenAISystemMessage.Enabled = false;
                    txtLMTemperature.Enabled = false;
                    txtMaximumToken.Enabled = false;
                    cmbMozhiBackend.Visible = true;
                    cmbMozhiBackend.Enabled = false;
                    cmbLanguageModel.Visible = false;
                    chkOpenAIExtraOption.Enabled = false;
                    buttonXCheck2.Enabled = false;
                    break;
                case 8:
                case 9:
                    labelX5.Visible = false;
                    labelX11.Visible = true;
                    labelX12.Enabled = true;
                    labelX13.Enabled = chkOpenAIExtraOption.Checked;
                    labelX14.Enabled = chkOpenAIExtraOption.Checked;
                    labelX15.Enabled = true;
                    txtOpenAIBackend.Enabled = true;
                    txtOpenAISystemMessage.Enabled = true;
                    txtLMTemperature.Enabled = chkOpenAIExtraOption.Checked;
                    txtMaximumToken.Enabled = chkOpenAIExtraOption.Checked;
                    cmbMozhiBackend.Visible = false;
                    cmbLanguageModel.Visible = true;
                    chkOpenAIExtraOption.Enabled = true;
                    buttonXCheck2.Enabled = true;
                    break;
                default:
                    labelX5.Visible = true;
                    labelX5.Enabled = true;
                    labelX11.Visible = false;
                    labelX12.Enabled = false;
                    labelX13.Enabled = false;
                    labelX14.Enabled = false;
                    labelX15.Enabled = false;
                    txtOpenAIBackend.Enabled = false;
                    txtOpenAISystemMessage.Enabled = false;
                    txtLMTemperature.Enabled = false;
                    txtMaximumToken.Enabled = false;
                    cmbMozhiBackend.Visible = true;
                    cmbMozhiBackend.Enabled = true;
                    cmbLanguageModel.Visible = false;
                    chkOpenAIExtraOption.Enabled = false;
                    buttonXCheck2.Enabled = true;
                    break;
            }
        }
        private void chkOpenAIExtraOption_CheckedChanged(object sender, EventArgs e)
        {
            txtLMTemperature.Enabled = chkOpenAIExtraOption.Checked && chkOpenAIExtraOption.Enabled;
            txtMaximumToken.Enabled = chkOpenAIExtraOption.Checked && chkOpenAIExtraOption.Enabled;
            labelX13.Enabled = chkOpenAIExtraOption.Checked && chkOpenAIExtraOption.Enabled;
            labelX14.Enabled = chkOpenAIExtraOption.Checked && chkOpenAIExtraOption.Enabled;
        }

        private void buttonX3_Click(object sender, EventArgs e)
        {
            string GlossaryTablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TranslationCache", "Glossary.csv");
            if (!File.Exists(GlossaryTablePath))
            {
                using (StreamWriter writer = new StreamWriter(GlossaryTablePath, false, new UTF8Encoding(true)))
                {
                    writer.Write(
                        "identifier,ko,ja,zh-CN,zh-TW,en\r\n" +
                        "<glos_0000000001>,메소,メル,金币,楓幣,mesos\r\n" +
                        "<glos_0000000002>,메소,メル,金币,楓幣,meso\r\n");
                }
            }
#if NET6_0_OR_GREATER
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = GlossaryTablePath
            });
#else
            Process.Start(GlossaryTablePath);
#endif
        }

        public WzLib.WzVersionVerifyMode WzVersionVerifyMode
        {
            get { return ((cmbWzVersionVerifyMode.SelectedItem as ComboItem)?.Value as WzLib.WzVersionVerifyMode?) ?? default; }
            set
            {
                var items = cmbWzVersionVerifyMode.Items.Cast<ComboItem>();
                var item = items.FirstOrDefault(_item => _item.Value as WzLib.WzVersionVerifyMode? == value)
                    ?? items.First();
                cmbWzVersionVerifyMode.SelectedItem = item;
            }
        }

        public void Load(WcR2Config config)
        {
            this.SortWzOnOpened = config.SortWzOnOpened;
            this.SortWzByImgID = config.SortWzByImgID;
            this.DefaultWzCodePage = config.WzEncoding;
            this.AutoDetectExtFiles = config.AutoDetectExtFiles;
            this.ImgCheckDisabled = config.ImgCheckDisabled;
            this.WzVersionVerifyMode = config.WzVersionVerifyMode;
            this.NxSecretKey = config.NxSecretKey;
            this.MozhiBackend = config.MozhiBackend;
            this.LanguageModel = config.LanguageModel;
            this.OpenAIBackend = config.OpenAIBackend;
            this.OpenAIExtraOption = config.OpenAIExtraOption;
            this.OpenAISystemMessage = config.OpenAISystemMessage;
            this.LMTemperature = config.LMTemperature;
            this.MaximumToken = config.MaximumToken;
            this.PreferredTranslateEngine = config.PreferredTranslateEngine;
            this.DesiredLanguage = config.DesiredLanguage;
            this.PreferredLayout = config.PreferredLayout;
            this.DetectCurrency = config.DetectCurrency;
            this.DesiredCurrency = config.DesiredCurrency;
        }

        public void Save(WcR2Config config)
        {
            config.SortWzOnOpened = this.SortWzOnOpened;
            config.SortWzByImgID = this.SortWzByImgID;
            config.WzEncoding = this.DefaultWzCodePage;
            config.AutoDetectExtFiles = this.AutoDetectExtFiles;
            config.ImgCheckDisabled = this.ImgCheckDisabled;
            config.WzVersionVerifyMode = this.WzVersionVerifyMode;
            config.NxSecretKey = this.NxSecretKey;
            config.MozhiBackend = this.MozhiBackend;
            if (this.LanguageModel != "none") config.LanguageModel = this.LanguageModel;
            config.OpenAIBackend = this.OpenAIBackend;
            config.OpenAIExtraOption = this.OpenAIExtraOption;
            config.OpenAISystemMessage = this.OpenAISystemMessage;
            config.LMTemperature = this.LMTemperature;
            config.MaximumToken = this.MaximumToken;
            config.PreferredTranslateEngine = this.PreferredTranslateEngine;
            config.DesiredLanguage = this.DesiredLanguage;
            config.PreferredLayout = this.PreferredLayout;
            config.DetectCurrency = this.DetectCurrency;
            config.DesiredCurrency = this.DesiredCurrency;
        }
    }
}