using YGFCT.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;
using YGFCT.Printer;
using System.Printing;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Windows.Devices.Bluetooth.Advertisement;

namespace YGFCT
{
    public partial class Main : Form
    {
        BluetoothLEAdvertisementWatcher watcher = new BluetoothLEAdvertisementWatcher();
        private string customMessageBoxTitle;
        string printerName;
        public AyarForm AyarFrm;
        public KurulumForm KurulumFrm;
        public Sifre SifreFrm;
        public ProgAyarForm ProgAyarFrm;
        private IntPtr ShellHwnd;
        private DateTime lastDateTime = DateTime.Now;

        int failCount = 0;
        bool loopEnd = true;
        int stepStateMax = 0;
        int[] stepJob = new int[41];
        string[] isim = new string[41];
        byte[,] feedback = new byte[41, 10];
        byte[,] veri = new byte[41, 10];
        string[] feedbackCare = new string[41];
        string[] veriCare = new string[41];
        int[] fbyteLenght = new int[41];
        int[] byteLenght = new int[41];
        int[] COM = new int[41];
        string[] kontrol = new string[41];
        string[] gorev = new string[41];
        string[] cozunurluk = new string[2];

        int stepState = 0;
        int serialTx1TimerCounter = 0;
        int serialTx2TimerCounter = 0;
        int serialTx3TimerCounter = 0;
        int adminTimerCounter = 0;
        int timeoutTimerCounter = 0;
        int saniyeTimerCounter = 0;

        byte[] arrayRx = new byte[256];
        int counterRxByte = 0;

        int totalCard = 0;
        int errorCard = 0;
        int fctSaniye = 0;
        public int yetki;

        string[] batchFileFeedback = new string[4];
        private bool isProgrammingStarted = false;
        int isProgrammingState = 0;
        bool againFct = true;

        int versions_number = 0;
        string[] versionsBarcodName = new string[21];
        string[] versionsSlaveName = new string[21];
        int[] stepProgJob = new int[21];
        string computerBatchFileAdress;
        string logDosyaPath = "";

        string companyNo; //Barkoddan Karşılatırılan
        string SAPNo;  //Barkoddan Karşılatırılan
        string productionDate;  //Barkoddan Alınan
        string indexNo; //Barkoddan Alınan
        string productionNo; //Barkoddan Alınan
        string cardNo;  //Barkoddan Karşılatırılan
        string gerberVer;  //Sabit
        string BOMVer;  //Sabit
        string ICTRev;  //Sabit
        string FCTRev;  //Sabit
        string softwareVer;  //Sabit
        string softwareRev;  //Sabit
        string UniqId; //Kartdan Alınan
        string HSMId; //Kartdan Alınan
        string BLEName;
        bool BLEScanState = false;
        int ErrorBleCounter = 0;
        bool lastFailState1 = true;
        bool lastFailState2 = true;
        bool lastFailState3 = true;
        private static readonly string BaseUrl = "https://cdu.arcelik.com/CardDataApi/"; //PRODUCTION
        string barcode72 = "";

        public Main()
        {
            this.AyarFrm = new AyarForm();
            this.AyarFrm.MainFrm = this;
            this.KurulumFrm = new KurulumForm();
            this.KurulumFrm.MainFrm = this;
            this.SifreFrm = new Sifre();
            this.SifreFrm.MainFrm = this;
            this.ProgAyarFrm = new ProgAyarForm();
            this.ProgAyarFrm.MainFrm = this;
            InitializeComponent();
        }

        public class INIKaydet
        {
            [DllImport("kernel32")]
            private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

            [DllImport("kernel32")]
            private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

            public INIKaydet(string dosyaYolu)
            {
                DOSYAYOLU = dosyaYolu;
            }
            private string DOSYAYOLU = String.Empty;
            public string Varsayilan { get; set; }
            public string Oku(string bolum, string ayaradi)
            {
                Varsayilan = Varsayilan ?? string.Empty;
                StringBuilder StrBuild = new StringBuilder(256);
                GetPrivateProfileString(bolum, ayaradi, Varsayilan, StrBuild, 255, DOSYAYOLU);
                return StrBuild.ToString();
            }
            public long Yaz(string bolum, string ayaradi, string deger)
            {
                return WritePrivateProfileString(bolum, ayaradi, deger, DOSYAYOLU);
            }
        }

        [DllImport("user32.dll")]
        public static extern byte ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string ClassName, string WindowName);

        private void Main_Load(object sender, EventArgs e)
        {
            this.ShellHwnd = Main.FindWindow("Shell TrayWnd", (string)null);
            IntPtr shellHwnd = this.ShellHwnd;
            int num1 = (int)Main.ShowWindow(this.ShellHwnd, 0);

            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            Control.CheckForIllegalCrossThreadCalls = false;

            this.customMessageBoxTitle = Ayarlar.Default.projectName;
            this.printerName = Ayarlar.Default.printerName;
            this.projectNameTxt.Text = customMessageBoxTitle;
            this.Text = customMessageBoxTitle;

            this.cardPicture.ImageLocation = Ayarlar.Default.PNGdosyayolu;

            foreach (string portName in SerialPort.GetPortNames())
            {
                this.AyarFrm.SerialPort1Com.Items.Add((object)portName);
                this.AyarFrm.SerialPort3Com.Items.Add((object)portName);
                this.AyarFrm.SerialPort2Com.Items.Add((object)portName);
            }
            if (Kurulum.Default.chBoxProgramlama)
            {
               // btnStartProgramming.Enabled = false;
                btnFCTInit.Enabled = false;
            }

            this.logDosyaPath = Ayarlar.Default.txtLogDosya;
            this.serialPort1.PortName = Ayarlar.Default.SerialPort1Com;
            this.serialPort1.BaudRate = Ayarlar.Default.SerialPort1Baud;
            this.serialPort1.DataBits = Ayarlar.Default.SerialPort1dataBits;
            this.serialPort1.StopBits = Ayarlar.Default.SerialPort1stopBit;
            this.serialPort1.Parity = Ayarlar.Default.SerialPort1Parity;
            this.serialPort1.ReceivedBytesThreshold = 1;
            this.serialPort2.PortName = Ayarlar.Default.SerialPort2Com;
            this.serialPort2.BaudRate = Ayarlar.Default.SerialPort2Baud;
            this.serialPort2.DataBits = Ayarlar.Default.SerialPort2dataBits;
            this.serialPort2.StopBits = Ayarlar.Default.SerialPort2stopBit;
            this.serialPort2.Parity = Ayarlar.Default.SerialPort2Parity;
            this.serialPort2.ReceivedBytesThreshold = 1;
            this.serialPort3.PortName = Ayarlar.Default.SerialPort3Com;
            this.serialPort3.BaudRate = Ayarlar.Default.SerialPort3Baud;
            this.serialPort3.DataBits = Ayarlar.Default.SerialPort3dataBits;
            this.serialPort3.StopBits = Ayarlar.Default.SerialPort3stopBits;
            this.serialPort3.Parity = Ayarlar.Default.SerialPort3Parity;
            this.serialPort3.ReceivedBytesThreshold = 1;

            this.serialTx1timer.Interval = Ayarlar.Default.SerialTx1Timer;
            this.serialTx2timer.Interval = Ayarlar.Default.SerialTx2Timer;
            this.serialTx3timer.Interval = Ayarlar.Default.SerialTx3Timer;
            this.timerAdmin.Interval = Ayarlar.Default.timerAdmin;
            this.serialRxTimeout.Interval = Ayarlar.Default.serialRxTimeout;

            this.companyNo = Prog_Ayarlar.Default.companyNo;
          //  this.SAPNo = Prog_Ayarlar.Default.SAPNo;
            this.cardNo = Prog_Ayarlar.Default.cardNo;
            this.gerberVer= Prog_Ayarlar.Default.gerberVer;
            this.BOMVer = Prog_Ayarlar.Default.BOMVer;
            this.ICTRev = Prog_Ayarlar.Default.ICTRev;
            this.FCTRev = Prog_Ayarlar.Default.FCTRev;
            this.softwareVer = Prog_Ayarlar.Default.softwareVer;
            this.softwareRev = Prog_Ayarlar.Default.softwareRev;

            this.versions_number = Convert.ToInt16(Prog_Ayarlar.Default.barcodeNum);
            this.stepProgJob[1] = Convert.ToInt16(Prog_Ayarlar.Default.step1Job);
            this.stepProgJob[2] = Convert.ToInt16(Prog_Ayarlar.Default.step2Job);
            this.stepProgJob[3] = Convert.ToInt16(Prog_Ayarlar.Default.step3Job);
            this.stepProgJob[4] = Convert.ToInt16(Prog_Ayarlar.Default.step4Job);
            this.stepProgJob[5] = Convert.ToInt16(Prog_Ayarlar.Default.step5Job);
            this.stepProgJob[6] = Convert.ToInt16(Prog_Ayarlar.Default.step6Job);
            this.stepProgJob[7] = Convert.ToInt16(Prog_Ayarlar.Default.step7Job);
            this.stepProgJob[8] = Convert.ToInt16(Prog_Ayarlar.Default.step8Job);
            this.stepProgJob[9] = Convert.ToInt16(Prog_Ayarlar.Default.step9Job);
            this.stepProgJob[10] = Convert.ToInt16(Prog_Ayarlar.Default.step10Job);
            this.stepProgJob[11] = Convert.ToInt16(Prog_Ayarlar.Default.step11Job);
            this.stepProgJob[12] = Convert.ToInt16(Prog_Ayarlar.Default.step12Job);
            this.stepProgJob[13] = Convert.ToInt16(Prog_Ayarlar.Default.step13Job);
            this.stepProgJob[14] = Convert.ToInt16(Prog_Ayarlar.Default.step14Job);
            this.stepProgJob[15] = Convert.ToInt16(Prog_Ayarlar.Default.step15Job);
            this.stepProgJob[16] = Convert.ToInt16(Prog_Ayarlar.Default.step16Job);
            this.stepProgJob[17] = Convert.ToInt16(Prog_Ayarlar.Default.step17Job);
            this.stepProgJob[18] = Convert.ToInt16(Prog_Ayarlar.Default.step18Job);
            this.stepProgJob[19] = Convert.ToInt16(Prog_Ayarlar.Default.step19Job);
            this.stepProgJob[20] = Convert.ToInt16(Prog_Ayarlar.Default.step20Job);
            this.versionsBarcodName[1] = Prog_Ayarlar.Default.barcode1;
            this.versionsBarcodName[2] = Prog_Ayarlar.Default.barcode2;
            this.versionsBarcodName[3] = Prog_Ayarlar.Default.barcode3;
            this.versionsBarcodName[4] = Prog_Ayarlar.Default.barcode4;
            this.versionsBarcodName[5] = Prog_Ayarlar.Default.barcode5;
            this.versionsBarcodName[6] = Prog_Ayarlar.Default.barcode6;
            this.versionsBarcodName[7] = Prog_Ayarlar.Default.barcode7;
            this.versionsBarcodName[8] = Prog_Ayarlar.Default.barcode8;
            this.versionsBarcodName[9] = Prog_Ayarlar.Default.barcode9;
            this.versionsBarcodName[10] = Prog_Ayarlar.Default.barcode10;
            this.versionsBarcodName[11] = Prog_Ayarlar.Default.barcode11;
            this.versionsBarcodName[12] = Prog_Ayarlar.Default.barcode12;
            this.versionsBarcodName[13] = Prog_Ayarlar.Default.barcode13;
            this.versionsBarcodName[14] = Prog_Ayarlar.Default.barcode14;
            this.versionsBarcodName[15] = Prog_Ayarlar.Default.barcode15;
            this.versionsBarcodName[16] = Prog_Ayarlar.Default.barcode16;
            this.versionsBarcodName[17] = Prog_Ayarlar.Default.barcode17;
            this.versionsBarcodName[18] = Prog_Ayarlar.Default.barcode18;
            this.versionsBarcodName[19] = Prog_Ayarlar.Default.barcode19;
            this.versionsBarcodName[20] = Prog_Ayarlar.Default.barcode20;
            this.versionsSlaveName[1] = Prog_Ayarlar.Default.Sbarcode1;
            this.versionsSlaveName[2] = Prog_Ayarlar.Default.Sbarcode2;
            this.versionsSlaveName[3] = Prog_Ayarlar.Default.Sbarcode3;
            this.versionsSlaveName[4] = Prog_Ayarlar.Default.Sbarcode4;
            this.versionsSlaveName[5] = Prog_Ayarlar.Default.Sbarcode5;
            this.versionsSlaveName[6] = Prog_Ayarlar.Default.Sbarcode6;
            this.versionsSlaveName[7] = Prog_Ayarlar.Default.Sbarcode7;
            this.versionsSlaveName[8] = Prog_Ayarlar.Default.Sbarcode8;
            this.versionsSlaveName[9] = Prog_Ayarlar.Default.Sbarcode9;
            this.versionsSlaveName[10] = Prog_Ayarlar.Default.Sbarcode10;
            this.versionsSlaveName[11] = Prog_Ayarlar.Default.Sbarcode11;
            this.versionsSlaveName[12] = Prog_Ayarlar.Default.Sbarcode12;
            this.versionsSlaveName[13] = Prog_Ayarlar.Default.Sbarcode13;
            this.versionsSlaveName[14] = Prog_Ayarlar.Default.Sbarcode14;
            this.versionsSlaveName[15] = Prog_Ayarlar.Default.Sbarcode15;
            this.versionsSlaveName[16] = Prog_Ayarlar.Default.Sbarcode16;
            this.versionsSlaveName[17] = Prog_Ayarlar.Default.Sbarcode17;
            this.versionsSlaveName[18] = Prog_Ayarlar.Default.Sbarcode18;
            this.versionsSlaveName[19] = Prog_Ayarlar.Default.Sbarcode19;
            this.versionsSlaveName[20] = Prog_Ayarlar.Default.Sbarcode20;
            batchFileFeedback[0] = Prog_Ayarlar.Default.successBatch;
            batchFileFeedback[1] = Prog_Ayarlar.Default.error1Batch;
            batchFileFeedback[2] = Prog_Ayarlar.Default.error2Batch;
            batchFileFeedback[3] = Prog_Ayarlar.Default.error3Batch;

            this.yetki = 0;
            this.yetkidegistir();
            this.stepStateMax = Convert.ToInt16(Kurulum.Default.stepNum);

            for (int i = 1; i <= stepStateMax; i++)
            {
                if (i == 1)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step1Job);
                    this.isim[i] = Kurulum.Default.isim1;
                    this.feedbackCare = Kurulum.Default.feedback1.Split(',');
                    this.veriCare = Kurulum.Default.veri1.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM1);
                    this.kontrol[i] = Kurulum.Default.kontrol1;
                    this.gorev[i] = Kurulum.Default.gorev1;
                }
                else if (i == 2)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step2Job);
                    this.isim[i] = Kurulum.Default.isim2;
                    this.feedbackCare = Kurulum.Default.feedback2.Split(',');
                    this.veriCare = Kurulum.Default.veri2.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM2);
                    this.kontrol[i] = Kurulum.Default.kontrol2;
                    this.gorev[i] = Kurulum.Default.gorev2;
                }
                else if (i == 3)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step3Job);
                    this.isim[i] = Kurulum.Default.isim3;
                    this.feedbackCare = Kurulum.Default.feedback3.Split(',');
                    this.veriCare = Kurulum.Default.veri3.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM3);
                    this.kontrol[i] = Kurulum.Default.kontrol3;
                    this.gorev[i] = Kurulum.Default.gorev3;
                }
                else if (i == 4)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step4Job);
                    this.isim[i] = Kurulum.Default.isim4;
                    this.feedbackCare = Kurulum.Default.feedback4.Split(',');
                    this.veriCare = Kurulum.Default.veri4.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM4);
                    this.kontrol[i] = Kurulum.Default.kontrol4;
                    this.gorev[i] = Kurulum.Default.gorev4;
                }
                else if (i == 5)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step5Job);
                    this.isim[i] = Kurulum.Default.isim5;
                    this.feedbackCare = Kurulum.Default.feedback5.Split(',');
                    this.veriCare = Kurulum.Default.veri5.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM5);
                    this.kontrol[i] = Kurulum.Default.kontrol5;
                    this.gorev[i] = Kurulum.Default.gorev5;
                }
                else if (i == 6)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step6Job);
                    this.isim[i] = Kurulum.Default.isim6;
                    this.feedbackCare = Kurulum.Default.feedback6.Split(',');
                    this.veriCare = Kurulum.Default.veri6.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM6);
                    this.kontrol[i] = Kurulum.Default.kontrol6;
                    this.gorev[i] = Kurulum.Default.gorev6;
                }
                else if (i == 7)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step7Job);
                    this.isim[i] = Kurulum.Default.isim7;
                    this.feedbackCare = Kurulum.Default.feedback7.Split(',');
                    this.veriCare = Kurulum.Default.veri7.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM7);
                    this.kontrol[i] = Kurulum.Default.kontrol7;
                    this.gorev[i] = Kurulum.Default.gorev7;
                }
                else if (i == 8)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step8Job);
                    this.isim[i] = Kurulum.Default.isim8;
                    this.feedbackCare = Kurulum.Default.feedback8.Split(',');
                    this.veriCare = Kurulum.Default.veri8.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM8);
                    this.kontrol[i] = Kurulum.Default.kontrol8;
                    this.gorev[i] = Kurulum.Default.gorev8;
                }
                else if (i == 9)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step9Job);
                    this.isim[i] = Kurulum.Default.isim9;
                    this.feedbackCare = Kurulum.Default.feedback9.Split(',');
                    this.veriCare = Kurulum.Default.veri9.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM9);
                    this.kontrol[i] = Kurulum.Default.kontrol9;
                    this.gorev[i] = Kurulum.Default.gorev9;
                }
                else if (i == 10)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step10Job);
                    this.isim[i] = Kurulum.Default.isim10;
                    this.feedbackCare = Kurulum.Default.feedback10.Split(',');
                    this.veriCare = Kurulum.Default.veri10.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM10);
                    this.kontrol[i] = Kurulum.Default.kontrol10;
                    this.gorev[i] = Kurulum.Default.gorev10;
                }
                else if (i == 11)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step11Job);
                    this.isim[i] = Kurulum.Default.isim11;
                    this.feedbackCare = Kurulum.Default.feedback11.Split(',');
                    this.veriCare = Kurulum.Default.veri11.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM11);
                    this.kontrol[i] = Kurulum.Default.kontrol11;
                    this.gorev[i] = Kurulum.Default.gorev11;
                }
                else if (i == 12)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step12Job);
                    this.isim[i] = Kurulum.Default.isim12;
                    this.feedbackCare = Kurulum.Default.feedback12.Split(',');
                    this.veriCare = Kurulum.Default.veri12.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM12);
                    this.kontrol[i] = Kurulum.Default.kontrol12;
                    this.gorev[i] = Kurulum.Default.gorev12;
                }
                else if (i == 13)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step13Job);
                    this.isim[i] = Kurulum.Default.isim13;
                    this.feedbackCare = Kurulum.Default.feedback13.Split(',');
                    this.veriCare = Kurulum.Default.veri13.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM13);
                    this.kontrol[i] = Kurulum.Default.kontrol13;
                    this.gorev[i] = Kurulum.Default.gorev13;
                }
                else if (i == 14)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step14Job);
                    this.isim[i] = Kurulum.Default.isim14;
                    this.feedbackCare = Kurulum.Default.feedback14.Split(',');
                    this.veriCare = Kurulum.Default.veri14.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM14);
                    this.kontrol[i] = Kurulum.Default.kontrol14;
                    this.gorev[i] = Kurulum.Default.gorev14;
                }
                else if (i == 15)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step15Job);
                    this.isim[i] = Kurulum.Default.isim15;
                    this.feedbackCare = Kurulum.Default.feedback15.Split(',');
                    this.veriCare = Kurulum.Default.veri15.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM15);
                    this.kontrol[i] = Kurulum.Default.kontrol15;
                    this.gorev[i] = Kurulum.Default.gorev15;
                }
                else if (i == 16)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step16Job);
                    this.isim[i] = Kurulum.Default.isim16;
                    this.feedbackCare = Kurulum.Default.feedback16.Split(',');
                    this.veriCare = Kurulum.Default.veri16.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM16);
                    this.kontrol[i] = Kurulum.Default.kontrol16;
                    this.gorev[i] = Kurulum.Default.gorev16;
                }
                else if (i == 17)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step17Job);
                    this.isim[i] = Kurulum.Default.isim17;
                    this.feedbackCare = Kurulum.Default.feedback17.Split(',');
                    this.veriCare = Kurulum.Default.veri17.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM17);
                    this.kontrol[i] = Kurulum.Default.kontrol17;
                    this.gorev[i] = Kurulum.Default.gorev17;
                }
                else if (i == 18)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step18Job);
                    this.isim[i] = Kurulum.Default.isim18;
                    this.feedbackCare = Kurulum.Default.feedback18.Split(',');
                    this.veriCare = Kurulum.Default.veri18.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM18);
                    this.kontrol[i] = Kurulum.Default.kontrol18;
                    this.gorev[i] = Kurulum.Default.gorev18;
                }
                else if (i == 19)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step19Job);
                    this.isim[i] = Kurulum.Default.isim19;
                    this.feedbackCare = Kurulum.Default.feedback19.Split(',');
                    this.veriCare = Kurulum.Default.veri19.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM19);
                    this.kontrol[i] = Kurulum.Default.kontrol19;
                    this.gorev[i] = Kurulum.Default.gorev19;
                }
                else if (i == 20)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step20Job);
                    this.isim[i] = Kurulum.Default.isim20;
                    this.feedbackCare = Kurulum.Default.feedback20.Split(',');
                    this.veriCare = Kurulum.Default.veri20.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM20);
                    this.kontrol[i] = Kurulum.Default.kontrol20;
                    this.gorev[i] = Kurulum.Default.gorev20;
                }
                else if (i == 21)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step21Job);
                    this.isim[i] = Kurulum.Default.isim21;
                    this.feedbackCare = Kurulum.Default.feedback21.Split(',');
                    this.veriCare = Kurulum.Default.veri21.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM21);
                    this.kontrol[i] = Kurulum.Default.kontrol21;
                    this.gorev[i] = Kurulum.Default.gorev21;
                }
                else if (i == 22)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step22Job);
                    this.isim[i] = Kurulum.Default.isim22;
                    this.feedbackCare = Kurulum.Default.feedback22.Split(',');
                    this.veriCare = Kurulum.Default.veri22.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM22);
                    this.kontrol[i] = Kurulum.Default.kontrol22;
                    this.gorev[i] = Kurulum.Default.gorev22;
                }
                else if (i == 23)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step23Job);
                    this.isim[i] = Kurulum.Default.isim23;
                    this.feedbackCare = Kurulum.Default.feedback23.Split(',');
                    this.veriCare = Kurulum.Default.veri23.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM23);
                    this.kontrol[i] = Kurulum.Default.kontrol23;
                    this.gorev[i] = Kurulum.Default.gorev23;
                }
                else if (i == 24)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step24Job);
                    this.isim[i] = Kurulum.Default.isim24;
                    this.feedbackCare = Kurulum.Default.feedback24.Split(',');
                    this.veriCare = Kurulum.Default.veri24.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM24);
                    this.kontrol[i] = Kurulum.Default.kontrol24;
                    this.gorev[i] = Kurulum.Default.gorev24;
                }
                else if (i == 25)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step25Job);
                    this.isim[i] = Kurulum.Default.isim25;
                    this.feedbackCare = Kurulum.Default.feedback25.Split(',');
                    this.veriCare = Kurulum.Default.veri25.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM25);
                    this.kontrol[i] = Kurulum.Default.kontrol25;
                    this.gorev[i] = Kurulum.Default.gorev25;
                }
                else if (i == 26)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step26Job);
                    this.isim[i] = Kurulum.Default.isim26;
                    this.feedbackCare = Kurulum.Default.feedback26.Split(',');
                    this.veriCare = Kurulum.Default.veri26.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM26);
                    this.kontrol[i] = Kurulum.Default.kontrol26;
                    this.gorev[i] = Kurulum.Default.gorev26;
                }
                else if (i == 27)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step27Job);
                    this.isim[i] = Kurulum.Default.isim27;
                    this.feedbackCare = Kurulum.Default.feedback27.Split(',');
                    this.veriCare = Kurulum.Default.veri27.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM27);
                    this.kontrol[i] = Kurulum.Default.kontrol27;
                    this.gorev[i] = Kurulum.Default.gorev27;
                }
                else if (i == 28)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step28Job);
                    this.isim[i] = Kurulum.Default.isim28;
                    this.feedbackCare = Kurulum.Default.feedback28.Split(',');
                    this.veriCare = Kurulum.Default.veri28.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM28);
                    this.kontrol[i] = Kurulum.Default.kontrol28;
                    this.gorev[i] = Kurulum.Default.gorev28;
                }
                else if (i == 29)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step29Job);
                    this.isim[i] = Kurulum.Default.isim29;
                    this.feedbackCare = Kurulum.Default.feedback29.Split(',');
                    this.veriCare = Kurulum.Default.veri29.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM29);
                    this.kontrol[i] = Kurulum.Default.kontrol29;
                    this.gorev[i] = Kurulum.Default.gorev29;
                }
                else if (i == 30)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step30Job);
                    this.isim[i] = Kurulum.Default.isim30;
                    this.feedbackCare = Kurulum.Default.feedback30.Split(',');
                    this.veriCare = Kurulum.Default.veri30.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM30);
                    this.kontrol[i] = Kurulum.Default.kontrol30;
                    this.gorev[i] = Kurulum.Default.gorev30;
                }
                else if (i == 31)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step31Job);
                    this.isim[i] = Kurulum.Default.isim31;
                    this.feedbackCare = Kurulum.Default.feedback31.Split(',');
                    this.veriCare = Kurulum.Default.veri31.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM31);
                    this.kontrol[i] = Kurulum.Default.kontrol31;
                    this.gorev[i] = Kurulum.Default.gorev31;
                }
                else if (i == 32)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step32Job);
                    this.isim[i] = Kurulum.Default.isim32;
                    this.feedbackCare = Kurulum.Default.feedback32.Split(',');
                    this.veriCare = Kurulum.Default.veri32.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM32);
                    this.kontrol[i] = Kurulum.Default.kontrol32;
                    this.gorev[i] = Kurulum.Default.gorev32;
                }
                else if (i == 33)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step33Job);
                    this.isim[i] = Kurulum.Default.isim33;
                    this.feedbackCare = Kurulum.Default.feedback33.Split(',');
                    this.veriCare = Kurulum.Default.veri33.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM33);
                    this.kontrol[i] = Kurulum.Default.kontrol33;
                    this.gorev[i] = Kurulum.Default.gorev33;
                }
                else if (i == 34)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step34Job);
                    this.isim[i] = Kurulum.Default.isim34;
                    this.feedbackCare = Kurulum.Default.feedback34.Split(',');
                    this.veriCare = Kurulum.Default.veri34.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM34);
                    this.kontrol[i] = Kurulum.Default.kontrol34;
                    this.gorev[i] = Kurulum.Default.gorev34;
                }
                else if (i == 35)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step35Job);
                    this.isim[i] = Kurulum.Default.isim35;
                    this.feedbackCare = Kurulum.Default.feedback35.Split(',');
                    this.veriCare = Kurulum.Default.veri35.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM35);
                    this.kontrol[i] = Kurulum.Default.kontrol35;
                    this.gorev[i] = Kurulum.Default.gorev35;
                }
                else if (i == 36)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step36Job);
                    this.isim[i] = Kurulum.Default.isim36;
                    this.feedbackCare = Kurulum.Default.feedback36.Split(',');
                    this.veriCare = Kurulum.Default.veri36.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM36);
                    this.kontrol[i] = Kurulum.Default.kontrol36;
                    this.gorev[i] = Kurulum.Default.gorev36;
                }
                else if (i == 37)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step37Job);
                    this.isim[i] = Kurulum.Default.isim37;
                    this.feedbackCare = Kurulum.Default.feedback37.Split(',');
                    this.veriCare = Kurulum.Default.veri37.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM37);
                    this.kontrol[i] = Kurulum.Default.kontrol37;
                    this.gorev[i] = Kurulum.Default.gorev37;
                }
                else if (i == 38)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step38Job);
                    this.isim[i] = Kurulum.Default.isim38;
                    this.feedbackCare = Kurulum.Default.feedback38.Split(',');
                    this.veriCare = Kurulum.Default.veri38.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM38);
                    this.kontrol[i] = Kurulum.Default.kontrol38;
                    this.gorev[i] = Kurulum.Default.gorev38;
                }
                else if (i == 39)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step39Job);
                    this.isim[i] = Kurulum.Default.isim39;
                    this.feedbackCare = Kurulum.Default.feedback39.Split(',');
                    this.veriCare = Kurulum.Default.veri39.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM39);
                    this.kontrol[i] = Kurulum.Default.kontrol39;
                    this.gorev[i] = Kurulum.Default.gorev39;
                }
                else if (i == 40)
                {
                    this.stepJob[i] = Convert.ToInt16(Kurulum.Default.step40Job);
                    this.isim[i] = Kurulum.Default.isim40;
                    this.feedbackCare = Kurulum.Default.feedback40.Split(',');
                    this.veriCare = Kurulum.Default.veri40.Split(',');
                    this.COM[i] = Convert.ToInt16(Kurulum.Default.COM40);
                    this.kontrol[i] = Kurulum.Default.kontrol40;
                    this.gorev[i] = Kurulum.Default.gorev40;
                }


                this.byteLenght[i] = Convert.ToInt32(veriCare[0]);
                for (int j = 1; j <= Convert.ToInt32(byteLenght[i]); j++)
                {
                    this.veri[i, j] = Convert.ToByte(veriCare[j]);
                }

                this.fbyteLenght[i] = Convert.ToInt32(feedbackCare[0]);
                for (int j = 1; j <= Convert.ToInt32(fbyteLenght[i]); j++)
                {
                    this.feedback[i, j] = Convert.ToByte(feedbackCare[j]);
                }

                this.Text = customMessageBoxTitle;
            }
            if (Ayarlar.Default.chBoxSerial1)
            {
                try
                {
                    this.serialPort1.DtrEnable = true;
                    this.serialPort1.Open();
                    lblStatusCom1.Text = "ON";
                    lblStatusCom1.BackColor = Color.Green;
                }
                catch (Exception ex)
                {
                    int num2 = (int)MessageBox.Show("Com1 Port Hatası: " + ex.ToString());
                    lblStatusCom1.Text = "OFF";
                    lblStatusCom1.BackColor = Color.Red;
                }
            }
            if (Ayarlar.Default.chBoxSerial2)
            {
                try
                {
                    this.serialPort2.Open();
                    lblStatusCom2.Text = "ON";
                    lblStatusCom2.BackColor = Color.Green;
                }
                catch (Exception ex)
                {
                    int num2 = (int)MessageBox.Show("Com2 Port Hatası: " + ex.ToString());
                    lblStatusCom2.Text = "OFF";
                    lblStatusCom2.BackColor = Color.Red;
                }
            }
            if (Ayarlar.Default.chBoxSerial3)
            {
                try
                {
                    this.serialPort3.Open();
                    lblStatusCom3.Text = "ON";
                    lblStatusCom3.BackColor = Color.Green;
                }
                catch (Exception ex)
                {
                    int num2 = (int)MessageBox.Show("Com3 Port Hatası: " + ex.ToString());
                    lblStatusCom3.Text = "OFF";
                    lblStatusCom3.BackColor = Color.Red;
                }
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (againFct)
            {
                while (serialPort1.BytesToRead > 0)
                {
                    arrayRx[counterRxByte] = Convert.ToByte(serialPort1.ReadByte());
                    counterRxByte++;
                    Thread.Sleep(100);
                }
                if (stepState == 77)
                {
                    if (counterRxByte > 30)
                    {
                        serialWriteBLE();
                    }
                }
                else if (stepState == 79)
                {
                    serialBufferClear();
                }
                else
                {
                    this.Invoke(new EventHandler(ShowData1));
                }
            }
            else
            {
               // serialPort1.DiscardInBuffer();
              //  serialPort1.DiscardOutBuffer();
            }
        }

        private void ShowData1(object sender, EventArgs e)
        {
            for (int i = 0; i < counterRxByte; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(arrayRx[i]) + "' ", Color.Green);
            }
            ConsoleAppendLine("COM1'den geldi.", Color.Green);
            ConsoleNewLine();
            if (stepState == 88)
            {
                serialBufferClear();
            }
            else
            {
                justFeedbackCheck();
            }
        }

        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (serialPort2.BytesToRead > 0)
            {
                arrayRx[counterRxByte] = Convert.ToByte(serialPort2.ReadByte());
                counterRxByte++;
                Thread.Sleep(100);
            }
            this.Invoke(new EventHandler(ShowData2));
        }

        private void ShowData2(object sender, EventArgs e)
        {
            for (int i = 0; i < counterRxByte; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(arrayRx[i]) + "' ", Color.Green);
            }
            ConsoleAppendLine("COM2'den geldi.", Color.Green);
            ConsoleNewLine();

            if (arrayRx[0] == 83)
            {
               // ProcessFailed();
            }
            else
            {
                if (Kurulum.Default.chBoxProgramlama && stepState == 0)
                {
                    lastFailState1 = true;
                    lastFailState2 = true;
                    lastFailState3 = true;
                    //lblStep1.Text = isim[1];
                    againFct = true;
                    this.Invoke(new EventHandler(btnStartProgramming_Click));
                }
                justFeedbackCheck();
            }
        }

        private void serialPort3_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (serialPort3.BytesToRead > 0)
            {
                arrayRx[counterRxByte] = Convert.ToByte(serialPort3.ReadByte());
                counterRxByte++;
            }
            this.Invoke(new EventHandler(ShowData3));
        }

        private void ShowData3(object sender, EventArgs e)
        {
            for (int i = 0; i < counterRxByte; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(arrayRx[i]) + "' ", Color.Green);
            }
            ConsoleAppendLine("COM3'den geldi.", Color.Green);
            ConsoleNewLine();
            justFeedbackCheck();
        }

        public void btnFCTInit_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)  //Birşeyler Eklenebilir.
            {
                stepState++;
                ProcessFCT();
                saniyeTimer.Start();
            }
        }

        private void ProcessStart()
        {
            ConsoleAppendLine(isim[stepState], Color.Black);
            ConsoleNewLine();
            double value = stepState;
            progressBarFCT.Value = Convert.ToInt32(value / stepStateMax * 100);
            btnFCTInit.Text = System.Convert.ToString(stepState);

            switch (stepState)
            {
                case 1:
                    lblStep1.Text = isim[stepState];
                    break;

                case 2:
                    lblStep2.Text = isim[stepState];
                    break;

                case 3:
                    lblStep3.Text = isim[stepState];
                    break;

                case 4:
                    lblStep4.Text = isim[stepState];
                    break;

                case 5:
                    lblStep5.Text = isim[stepState];
                    break;

                case 6:
                    lblStep6.Text = isim[stepState];
                    break;

                case 7:
                    lblStep7.Text = isim[stepState];
                    break;

                case 8:
                    lblStep8.Text = isim[stepState];
                    break;

                case 9:
                    lblStep9.Text = isim[stepState];
                    break;

                case 10:
                    lblStep10.Text = isim[stepState];
                    break;

                case 11:
                    lblStep11.Text = isim[stepState];
                    break;

                case 12:
                    lblStep12.Text = isim[stepState];
                    break;

                case 13:
                    lblStep13.Text = isim[stepState];
                    break;

                case 14:
                    lblStep14.Text = isim[stepState];
                    break;

                case 15:
                    lblStep15.Text = isim[stepState];
                    break;

                case 16:
                    lblStep16.Text = isim[stepState];
                    break;

                case 17:
                    lblStep17.Text = isim[stepState];
                    break;

                case 18:
                    lblStep18.Text = isim[stepState];
                    break;

                case 19:
                    lblStep19.Text = isim[stepState];
                    break;

                case 20:
                    lblStep20.Text = isim[stepState];
                    break;

                case 21:
                    lblStep21.Text = isim[stepState];
                    break;

                case 22:
                    lblStep22.Text = isim[stepState];
                    break;

                case 23:
                    lblStep23.Text = isim[stepState];
                    break;

                case 24:
                    lblStep24.Text = isim[stepState];
                    break;

                case 25:
                    lblStep25.Text = isim[stepState];
                    break;

                case 26:
                    lblStep26.Text = isim[stepState];
                    break;

                case 27:
                    lblStep27.Text = isim[stepState];
                    break;

                case 28:
                    lblStep28.Text = isim[stepState];
                    break;

                case 29:
                    lblStep29.Text = isim[stepState];
                    break;

                case 30:
                    lblStep30.Text = isim[stepState];
                    break;

                case 31:
                    lblStep31.Text = isim[stepState];
                    break;

                case 32:
                    lblStep32.Text = isim[stepState];
                    break;

                case 33:
                    lblStep33.Text = isim[stepState];
                    break;

                case 34:
                    lblStep34.Text = isim[stepState];
                    break;

                case 35:
                    lblStep35.Text = isim[stepState];
                    break;

                case 36:
                    lblStep36.Text = isim[stepState];
                    break;

                case 37:
                    lblStep37.Text = isim[stepState];
                    break;

                case 38:
                    lblStep38.Text = isim[stepState];
                    break;

                case 39:
                    lblStep39.Text = isim[stepState];
                    break;

                case 40:
                    lblStep40.Text = isim[stepState];
                    break;
            }
            btnFCTInit.Enabled = false;
        }

        private void serialWriteByte1()
        {
            byte[] byteArray = new byte[8];

            for (int i = 0; i < byteLenght[stepState]; i++)
            {
                byteArray[i] = veri[stepState, i + 1];
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM1'den gitti.", Color.Blue);
            serialPort1.Write(byteArray, 0, byteLenght[stepState]);
        }

        private void serialWriteByte2()
        {
            byte[] byteArray = new byte[8];

            for (int i = 0; i < byteLenght[stepState]; i++)
            {
                byteArray[i] = veri[stepState, i + 1];
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM2'den gitti.", Color.Blue);
            serialPort2.Write(byteArray, 0, byteLenght[stepState]);
        }

        private void serialWriteByte3()
        {
            byte[] byteArray = new byte[8];

            for (int i = 0; i < byteLenght[stepState]; i++)
            {
                byteArray[i] = veri[stepState, i + 1];
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM3'den gitti.", Color.Blue);
            serialPort3.Write(byteArray, 0, byteLenght[stepState]);
        }

        private void ProcessSuccess()
        {
            switch (stepState)
            {
                case 1:
                    lblStep1.BackColor = Color.Lime;
                    tBoxPassed1.BackColor = Color.Lime;
                    tBoxPassed1.Text = "PASSED";
                    break;

                case 2:
                    lblStep2.BackColor = Color.Lime;
                    tBoxPassed2.BackColor = Color.Lime;
                    tBoxPassed2.Text = "PASSED";
                    break;

                case 3:
                    lblStep3.BackColor = Color.Lime;
                    tBoxPassed3.BackColor = Color.Lime;
                    tBoxPassed3.Text = "PASSED";
                    break;

                case 4:
                    lblStep4.BackColor = Color.Lime;
                    tBoxPassed4.BackColor = Color.Lime;
                    tBoxPassed4.Text = "PASSED";
                    break;

                case 5:
                    lblStep5.BackColor = Color.Lime;
                    tBoxPassed5.BackColor = Color.Lime;
                    tBoxPassed5.Text = "PASSED";
                    break;

                case 6:
                    lblStep6.BackColor = Color.Lime;
                    tBoxPassed6.BackColor = Color.Lime;
                    tBoxPassed6.Text = "PASSED";
                    break;

                case 7:
                    lblStep7.BackColor = Color.Lime;
                    tBoxPassed7.BackColor = Color.Lime;
                    tBoxPassed7.Text = "PASSED";
                    break;

                case 8:
                    lblStep8.BackColor = Color.Lime;
                    tBoxPassed8.BackColor = Color.Lime;
                    tBoxPassed8.Text = "PASSED";
                    break;

                case 9:
                    lblStep9.BackColor = Color.Lime;
                    tBoxPassed9.BackColor = Color.Lime;
                    tBoxPassed9.Text = "PASSED";
                    break;

                case 10:
                    lblStep10.BackColor = Color.Lime;
                    tBoxPassed10.BackColor = Color.Lime;
                    tBoxPassed10.Text = "PASSED";
                    break;

                case 11:
                    lblStep11.BackColor = Color.Lime;
                    tBoxPassed11.BackColor = Color.Lime;
                    tBoxPassed11.Text = "PASSED";
                    break;

                case 12:
                    lblStep12.BackColor = Color.Lime;
                    tBoxPassed12.BackColor = Color.Lime;
                    tBoxPassed12.Text = "PASSED";
                    break;

                case 13:
                    lblStep13.BackColor = Color.Lime;
                    tBoxPassed13.BackColor = Color.Lime;
                    tBoxPassed13.Text = "PASSED";
                    break;

                case 14:
                    lblStep14.BackColor = Color.Lime;
                    tBoxPassed14.BackColor = Color.Lime;
                    tBoxPassed14.Text = "PASSED";
                    break;

                case 15:
                    lblStep15.BackColor = Color.Lime;
                    tBoxPassed15.BackColor = Color.Lime;
                    tBoxPassed15.Text = "PASSED";
                    break;

                case 16:
                    lblStep16.BackColor = Color.Lime;
                    tBoxPassed16.BackColor = Color.Lime;
                    tBoxPassed16.Text = "PASSED";
                    break;

                case 17:
                    lblStep17.BackColor = Color.Lime;
                    tBoxPassed17.BackColor = Color.Lime;
                    tBoxPassed17.Text = "PASSED";
                    break;

                case 18:
                    lblStep18.BackColor = Color.Lime;
                    tBoxPassed18.BackColor = Color.Lime;
                    tBoxPassed18.Text = "PASSED";
                    break;

                case 19:
                    lblStep19.BackColor = Color.Lime;
                    tBoxPassed19.BackColor = Color.Lime;
                    tBoxPassed19.Text = "PASSED";
                    break;

                case 20:
                    lblStep20.BackColor = Color.Lime;
                    tBoxPassed20.BackColor = Color.Lime;
                    tBoxPassed20.Text = "PASSED";
                    break;

                case 21:
                    lblStep21.BackColor = Color.Lime;
                    tBoxPassed21.BackColor = Color.Lime;
                    tBoxPassed21.Text = "PASSED";
                    break;

                case 22:
                    lblStep22.BackColor = Color.Lime;
                    tBoxPassed22.BackColor = Color.Lime;
                    tBoxPassed22.Text = "PASSED";
                    break;

                case 23:
                    lblStep23.BackColor = Color.Lime;
                    tBoxPassed23.BackColor = Color.Lime;
                    tBoxPassed23.Text = "PASSED";
                    break;

                case 24:
                    lblStep24.BackColor = Color.Lime;
                    tBoxPassed24.BackColor = Color.Lime;
                    tBoxPassed24.Text = "PASSED";
                    break;

                case 25:
                    lblStep25.BackColor = Color.Lime;
                    tBoxPassed25.BackColor = Color.Lime;
                    tBoxPassed25.Text = "PASSED";
                    break;

                case 26:
                    lblStep26.BackColor = Color.Lime;
                    tBoxPassed26.BackColor = Color.Lime;
                    tBoxPassed26.Text = "PASSED";
                    break;

                case 27:
                    lblStep27.BackColor = Color.Lime;
                    tBoxPassed27.BackColor = Color.Lime;
                    tBoxPassed27.Text = "PASSED";
                    break;

                case 28:
                    lblStep28.BackColor = Color.Lime;
                    tBoxPassed28.BackColor = Color.Lime;
                    tBoxPassed28.Text = "PASSED";
                    break;

                case 29:
                    lblStep29.BackColor = Color.Lime;
                    tBoxPassed29.BackColor = Color.Lime;
                    tBoxPassed29.Text = "PASSED";
                    break;

                case 30:
                    lblStep30.BackColor = Color.Lime;
                    tBoxPassed30.BackColor = Color.Lime;
                    tBoxPassed30.Text = "PASSED";
                    break;

                case 31:
                    lblStep31.BackColor = Color.Lime;
                    tBoxPassed31.BackColor = Color.Lime;
                    tBoxPassed31.Text = "PASSED";
                    break;

                case 32:
                    lblStep32.BackColor = Color.Lime;
                    tBoxPassed32.BackColor = Color.Lime;
                    tBoxPassed32.Text = "PASSED";
                    break;

                case 33:
                    lblStep33.BackColor = Color.Lime;
                    tBoxPassed33.BackColor = Color.Lime;
                    tBoxPassed33.Text = "PASSED";
                    break;

                case 34:
                    lblStep34.BackColor = Color.Lime;
                    tBoxPassed34.BackColor = Color.Lime;
                    tBoxPassed34.Text = "PASSED";
                    break;

                case 35:
                    lblStep35.BackColor = Color.Lime;
                    tBoxPassed35.BackColor = Color.Lime;
                    tBoxPassed35.Text = "PASSED";
                    break;

                case 36:
                    lblStep36.BackColor = Color.Lime;
                    tBoxPassed36.BackColor = Color.Lime;
                    tBoxPassed36.Text = "PASSED";
                    break;

                case 37:
                    lblStep37.BackColor = Color.Lime;
                    tBoxPassed37.BackColor = Color.Lime;
                    tBoxPassed37.Text = "PASSED";
                    break;

                case 38:
                    lblStep38.BackColor = Color.Lime;
                    tBoxPassed38.BackColor = Color.Lime;
                    tBoxPassed38.Text = "PASSED";
                    break;

                case 39:
                    lblStep39.BackColor = Color.Lime;
                    tBoxPassed39.BackColor = Color.Lime;
                    tBoxPassed39.Text = "PASSED";
                    break;

                case 40:
                    lblStep40.BackColor = Color.Lime;
                    tBoxPassed40.BackColor = Color.Lime;
                    tBoxPassed40.Text = "PASSED";
                    break;
            }
        }

        private void ProcessFailed()
        {
            switch (stepState)
            {
                case 1:
                    lblStep1.BackColor = Color.Red;
                    tBoxPassed1.BackColor = Color.Red;
                    tBoxPassed1.Text = "FAILED";
                    break;

                case 2:
                    lblStep2.BackColor = Color.Red;
                    tBoxPassed2.BackColor = Color.Red;
                    tBoxPassed2.Text = "FAILED";
                    break;

                case 3:
                    lblStep3.BackColor = Color.Red;
                    tBoxPassed3.BackColor = Color.Red;
                    tBoxPassed3.Text = "FAILED";
                    break;

                case 4:
                    lblStep4.BackColor = Color.Red;
                    tBoxPassed4.BackColor = Color.Red;
                    tBoxPassed4.Text = "FAILED";
                    break;

                case 5:
                    lblStep5.BackColor = Color.Red;
                    tBoxPassed5.BackColor = Color.Red;
                    tBoxPassed5.Text = "FAILED";
                    break;

                case 6:
                    lblStep6.BackColor = Color.Red;
                    tBoxPassed6.BackColor = Color.Red;
                    tBoxPassed6.Text = "FAILED";
                    break;

                case 7:
                    lblStep7.BackColor = Color.Red;
                    tBoxPassed7.BackColor = Color.Red;
                    tBoxPassed7.Text = "FAILED";
                    break;

                case 8:
                    lblStep8.BackColor = Color.Red;
                    tBoxPassed8.BackColor = Color.Red;
                    tBoxPassed8.Text = "FAILED";
                    break;

                case 9:
                    lblStep9.BackColor = Color.Red;
                    tBoxPassed9.BackColor = Color.Red;
                    tBoxPassed9.Text = "FAILED";
                    break;

                case 10:
                    lblStep10.BackColor = Color.Red;
                    tBoxPassed10.BackColor = Color.Red;
                    tBoxPassed10.Text = "FAILED";
                    break;

                case 11:
                    lblStep11.BackColor = Color.Red;
                    tBoxPassed11.BackColor = Color.Red;
                    tBoxPassed11.Text = "FAILED";
                    break;

                case 12:
                    lblStep12.BackColor = Color.Red;
                    tBoxPassed12.BackColor = Color.Red;
                    tBoxPassed12.Text = "FAILED";
                    break;

                case 13:
                    lblStep13.BackColor = Color.Red;
                    tBoxPassed13.BackColor = Color.Red;
                    tBoxPassed13.Text = "FAILED";
                    break;

                case 14:
                    lblStep14.BackColor = Color.Red;
                    tBoxPassed14.BackColor = Color.Red;
                    tBoxPassed14.Text = "FAILED";
                    break;

                case 15:
                    lblStep15.BackColor = Color.Red;
                    tBoxPassed15.BackColor = Color.Red;
                    tBoxPassed15.Text = "FAILED";
                    break;

                case 16:
                    lblStep16.BackColor = Color.Red;
                    tBoxPassed16.BackColor = Color.Red;
                    tBoxPassed16.Text = "FAILED";
                    break;

                case 17:
                    lblStep17.BackColor = Color.Red;
                    tBoxPassed17.BackColor = Color.Red;
                    tBoxPassed17.Text = "FAILED";
                    break;

                case 18:
                    lblStep18.BackColor = Color.Red;
                    tBoxPassed18.BackColor = Color.Red;
                    tBoxPassed18.Text = "FAILED";
                    break;

                case 19:
                    lblStep19.BackColor = Color.Red;
                    tBoxPassed19.BackColor = Color.Red;
                    tBoxPassed19.Text = "FAILED";
                    break;

                case 20:
                    lblStep20.BackColor = Color.Red;
                    tBoxPassed20.BackColor = Color.Red;
                    tBoxPassed20.Text = "FAILED";
                    break;

                case 21:
                    lblStep21.BackColor = Color.Red;
                    tBoxPassed21.BackColor = Color.Red;
                    tBoxPassed21.Text = "FAILED";
                    break;

                case 22:
                    lblStep22.BackColor = Color.Red;
                    tBoxPassed22.BackColor = Color.Red;
                    tBoxPassed22.Text = "FAILED";
                    break;

                case 23:
                    lblStep23.BackColor = Color.Red;
                    tBoxPassed23.BackColor = Color.Red;
                    tBoxPassed23.Text = "FAILED";
                    break;

                case 24:
                    lblStep24.BackColor = Color.Red;
                    tBoxPassed24.BackColor = Color.Red;
                    tBoxPassed24.Text = "FAILED";
                    break;

                case 25:
                    lblStep25.BackColor = Color.Red;
                    tBoxPassed25.BackColor = Color.Red;
                    tBoxPassed25.Text = "FAILED";
                    break;

                case 26:
                    lblStep26.BackColor = Color.Red;
                    tBoxPassed26.BackColor = Color.Red;
                    tBoxPassed26.Text = "FAILED";
                    break;

                case 27:
                    lblStep27.BackColor = Color.Red;
                    tBoxPassed27.BackColor = Color.Red;
                    tBoxPassed27.Text = "FAILED";
                    break;

                case 28:
                    lblStep28.BackColor = Color.Red;
                    tBoxPassed28.BackColor = Color.Red;
                    tBoxPassed28.Text = "FAILED";
                    break;

                case 29:
                    lblStep29.BackColor = Color.Red;
                    tBoxPassed29.BackColor = Color.Red;
                    tBoxPassed29.Text = "FAILED";
                    break;

                case 30:
                    lblStep30.BackColor = Color.Red;
                    tBoxPassed30.BackColor = Color.Red;
                    tBoxPassed30.Text = "FAILED";
                    break;

                case 31:
                    lblStep31.BackColor = Color.Red;
                    tBoxPassed31.BackColor = Color.Red;
                    tBoxPassed31.Text = "FAILED";
                    break;

                case 32:
                    lblStep32.BackColor = Color.Red;
                    tBoxPassed32.BackColor = Color.Red;
                    tBoxPassed32.Text = "FAILED";
                    break;

                case 33:
                    lblStep33.BackColor = Color.Red;
                    tBoxPassed33.BackColor = Color.Red;
                    tBoxPassed33.Text = "FAILED";
                    break;

                case 34:
                    lblStep34.BackColor = Color.Red;
                    tBoxPassed34.BackColor = Color.Red;
                    tBoxPassed34.Text = "FAILED";
                    break;

                case 35:
                    lblStep35.BackColor = Color.Red;
                    tBoxPassed35.BackColor = Color.Red;
                    tBoxPassed35.Text = "FAILED";
                    break;

                case 36:
                    lblStep36.BackColor = Color.Red;
                    tBoxPassed36.BackColor = Color.Red;
                    tBoxPassed36.Text = "FAILED";
                    break;

                case 37:
                    lblStep37.BackColor = Color.Red;
                    tBoxPassed37.BackColor = Color.Red;
                    tBoxPassed37.Text = "FAILED";
                    break;

                case 38:
                    lblStep38.BackColor = Color.Red;
                    tBoxPassed38.BackColor = Color.Red;
                    tBoxPassed38.Text = "FAILED";
                    break;

                case 39:
                    lblStep39.BackColor = Color.Red;
                    tBoxPassed39.BackColor = Color.Red;
                    tBoxPassed39.Text = "FAILED";
                    break;

                case 40:
                    lblStep40.BackColor = Color.Red;
                    tBoxPassed40.BackColor = Color.Red;
                    tBoxPassed40.Text = "FAILED";
                    break;
            }
            if (stepState <= 39)
            {
                CustomMessageBox.ShowMessage("Kart Hatalı.Lütfen Kutuya Ayırınız!", customMessageBoxTitle, MessageBoxButtons.OK, CustomMessageBoxIcon.Error, Color.Red);
            }
            else
            {
                if (lastFailState1)
                {
                    lastFailState1 = false;
                    CustomMessageBox.ShowMessage("Kart Daha Önce Cloud'a Eklenmeştir. Lütfen Ayrı Kutuya Ayırınız", customMessageBoxTitle, MessageBoxButtons.OK, CustomMessageBoxIcon.Error, Color.Red);
                }
            }
            All_FCT_Fail();
            FCT_Finish();
        }
         
        private void All_FCT_Fail()
        {
            byte[] byteArray = new byte[8];
            byteArray[0] = 48;
            ConsoleAppendLine("COM2'den TÜM FCT BAŞARISIZ GİTTİ.", Color.Blue);
            serialPort2.Write(byteArray, 0, 1);
            logTut(0);
            if (lastFailState2)
            {
                lastFailState2 = false;
                errorCardTxt.Text = Convert.ToString(++errorCard);
            }
        } 

        private void FCT_Finish()
        {
            FCT_Clear();
            againFct = false;
            if (lastFailState3)
            {
                lastFailState3 = false;
                Verim();
            }
            saniyeTimer.Stop();
            fctSaniye = 0;
            watcher.Stop();
            serialRxTimeout.Stop();
            serialRxTimeout.Enabled = false;
            loopEnd = false;
            ErrorBleCounter = 0;
        }

        private void serialBufferClear()
        {
            for (int i = 0; i <= counterRxByte; i++)
            {
                arrayRx[i] = 0x0;
            }
            counterRxByte = 0;
            serialPort1.DiscardInBuffer();
            serialPort1.DiscardOutBuffer();
            serialPort2.DiscardInBuffer();
            serialPort2.DiscardOutBuffer();
        }

        private void All_FCT_Success()
        {
            byte[] byteArray = new byte[8];
            byteArray[0] = 49;
            ConsoleAppendLine("COM2'DEN TÜM FCT BAŞARILI GİTTİ.", Color.Blue);
            serialPort2.Write(byteArray, 0, 1);
            logTut(1);
        }

        private void ProcessCountinue()
        {
            stepState++;
            ProcessFCT();
          //  btnFCTInit.Enabled = true;
        }

        private void ProcessFCT()
        {
            if (againFct)
            {
                if (stepState > stepStateMax)
                {
                    FCT_Finish();
                    CustomMessageBox.ShowMessage("FCT Testi Sonlandı. Lütfen Tekrar Başlayın!", customMessageBoxTitle, MessageBoxButtons.OK, CustomMessageBoxIcon.Information, Color.Green);
                    All_FCT_Success();
                }
                else
                {
                    ProcessStart();
                    if (stepJob[stepState] != 5 && stepJob[stepState] != 4)
                    {
                        if (COM[stepState] == 1)
                        {
                            serialTx1timer.Start();// serialWriteByte1();
                        }
                        else if (COM[stepState] == 2)
                        {
                            if (stepState != 40)
                            {
                                serialTx2timer.Start();// serialWriteByte2();
                            }
                        }
                        else if (COM[stepState] == 3)
                        {
                            serialTx3timer.Start();// serialWriteByte3();
                        }
                    }

                    if (stepJob[stepState] == 4)
                    {
                        if (CustomMessageBox.ShowMessage(gorev[stepState], customMessageBoxTitle, MessageBoxButtons.OK, CustomMessageBoxIcon.Warning, Color.Yellow) == DialogResult.OK)
                        {
                            if (COM[stepState] == 1)
                            {
                                serialTx1timer.Start();// serialWriteByte1();
                            }
                        }
                    }

                    if (stepJob[stepState] == 2)
                    {
                        if (CustomMessageBox.ShowMessage(kontrol[stepState], customMessageBoxTitle, MessageBoxButtons.YesNo, CustomMessageBoxIcon.Question, Color.Yellow) == DialogResult.Yes)
                        {
                            /*
                            if (stepState == 40)
                            {
                                cloudAction(); //Önce buluta Yaz  Başarılı olursa Yazıcıya Yaz
                            }
                            */
                            ProcessSuccess();
                            ProcessCountinue();
                        }
                        else
                        {
                            ProcessFailed();
                        }
                    }
                    else if (stepJob[stepState] == 6)
                    {
                        ProcessSuccess();
                        ProcessCountinue();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        serialRxTimeout.Enabled = true;
                        serialRxTimeout.Start();
                    }
                }
            }
            else
            {
                againFct = true;
                FCT_Clear();
            }
        }

        private void justFeedbackCheck()
        {
            int trueRX = 0;
            serialRxTimeout.Stop();
            serialRxTimeout.Enabled = false;

            if (stepJob[stepState] == 1 || stepJob[stepState] == 5)
            {
                for (int i = 0; i < counterRxByte; i++)
                {
                    if (arrayRx[i] == feedback[stepState, i + 1])
                    {
                        trueRX++;
                    }
                    else
                    {
                        ProcessFailed();
                        break;
                    }
                }
                if (trueRX == counterRxByte)
                {
                    ProcessSuccess();
                    if (stepState == 39)
                    {
                        stepState = 77;
                        serialWriteHSM();
                    }
                    else if (stepState == 8)
                    {
                        stepState = 88;
                        serialWriteRelayOff();
                    }
                    else
                    {
                        ProcessCountinue();
                    }
                }
                else
                {
                  //  ProcessFailed();
                }
            }
            else if (stepJob[stepState] == 3)
            {
                for (int i = 0; i < counterRxByte; i++)
                {
                    if (arrayRx[i] == feedback[stepState, i + 1])
                    {
                        trueRX++;
                    }
                    else
                    {
                        ProcessFailed();
                        break;
                    }
                }
                if (trueRX == counterRxByte)
                {
                    if (CustomMessageBox.ShowMessage(kontrol[stepState], customMessageBoxTitle, MessageBoxButtons.YesNo, CustomMessageBoxIcon.Question, Color.Yellow) == DialogResult.Yes)
                    {
                        ProcessSuccess();
                        ProcessCountinue();
                    }
                    else
                    {
                        ProcessFailed();
                    }
                }
                else
                {
                  //  ProcessFailed();
                }
            }
            else if (stepJob[stepState] == 4)
            {
                for (int i = 0; i < counterRxByte; i++)
                {
                    if (arrayRx[i] == feedback[stepState, i + 1])
                    {
                        trueRX++;
                    }
                    else
                    {
                       ProcessFailed();
                       break;
                    }
                }
                if (trueRX == counterRxByte)
                {
                    ProcessSuccess();
                    ProcessCountinue();
                }
                else
                {
                  // ProcessFailed();
                }
            }

            serialBufferClear();  //Yeni
        }

        private void Verim()
        {
            totalCardTxt.Text = Convert.ToString(++totalCard);
            verimTxt.Text = Convert.ToString(100 - ((float)((float)errorCard / totalCard)) * 100);
        }

        private void FCT_Clear()
        {
          //  btnFCTInit.Enabled = true;
            stepState = 0;
            btnFCTInit.Text = "BUTONLARA BASARAK FCT TESTİNİ BAŞLAT";
            progressBarFCT.Value = 0;
            failCount = 0;

            if (Kurulum.Default.chBoxProgramlama)
            {
                btnStartProgramming.Enabled = false;
            }

            lblStep1.BackColor = Color.White;
            lblStep2.BackColor = Color.White;
            lblStep3.BackColor = Color.White;
            lblStep4.BackColor = Color.White;
            lblStep5.BackColor = Color.White;
            lblStep6.BackColor = Color.White;
            lblStep7.BackColor = Color.White;
            lblStep8.BackColor = Color.White;
            lblStep9.BackColor = Color.White;
            lblStep10.BackColor = Color.White;
            lblStep11.BackColor = Color.White;
            lblStep12.BackColor = Color.White;
            lblStep13.BackColor = Color.White;
            lblStep14.BackColor = Color.White;
            lblStep15.BackColor = Color.White;
            lblStep16.BackColor = Color.White;
            lblStep17.BackColor = Color.White;
            lblStep18.BackColor = Color.White;
            lblStep19.BackColor = Color.White;
            lblStep20.BackColor = Color.White;
            lblStep21.BackColor = Color.White;
            lblStep22.BackColor = Color.White;
            lblStep23.BackColor = Color.White;
            lblStep24.BackColor = Color.White;
            lblStep25.BackColor = Color.White;
            lblStep26.BackColor = Color.White;
            lblStep27.BackColor = Color.White;
            lblStep28.BackColor = Color.White;
            lblStep29.BackColor = Color.White;
            lblStep30.BackColor = Color.White;
            lblStep31.BackColor = Color.White;
            lblStep32.BackColor = Color.White;
            lblStep33.BackColor = Color.White;
            lblStep34.BackColor = Color.White;
            lblStep35.BackColor = Color.White;
            lblStep36.BackColor = Color.White;
            lblStep37.BackColor = Color.White;
            lblStep38.BackColor = Color.White;
            lblStep39.BackColor = Color.White;
            lblStep40.BackColor = Color.White;

            lblStep1.Text = "";
            lblStep2.Text = "";
            lblStep3.Text = "";
            lblStep4.Text = "";
            lblStep5.Text = "";
            lblStep6.Text = "";
            lblStep7.Text = "";
            lblStep8.Text = "";
            lblStep9.Text = "";
            lblStep10.Text = "";
            lblStep11.Text = "";
            lblStep12.Text = "";
            lblStep13.Text = "";
            lblStep14.Text = "";
            lblStep15.Text = "";
            lblStep16.Text = "";
            lblStep17.Text = "";
            lblStep18.Text = "";
            lblStep19.Text = "";
            lblStep20.Text = "";
            lblStep21.Text = "";
            lblStep22.Text = "";
            lblStep23.Text = "";
            lblStep24.Text = "";
            lblStep25.Text = "";
            lblStep26.Text = "";
            lblStep27.Text = "";
            lblStep28.Text = "";
            lblStep29.Text = "";
            lblStep30.Text = "";
            lblStep31.Text = "";
            lblStep32.Text = "";
            lblStep33.Text = "";
            lblStep34.Text = "";
            lblStep35.Text = "";
            lblStep36.Text = "";
            lblStep37.Text = "";
            lblStep38.Text = "";
            lblStep39.Text = "";
            lblStep40.Text = "";

            tBoxPassed1.BackColor = Color.White;
            tBoxPassed2.BackColor = Color.White;
            tBoxPassed3.BackColor = Color.White;
            tBoxPassed4.BackColor = Color.White;
            tBoxPassed5.BackColor = Color.White;
            tBoxPassed6.BackColor = Color.White;
            tBoxPassed7.BackColor = Color.White;
            tBoxPassed8.BackColor = Color.White;
            tBoxPassed9.BackColor = Color.White;
            tBoxPassed10.BackColor = Color.White;
            tBoxPassed11.BackColor = Color.White;
            tBoxPassed12.BackColor = Color.White;
            tBoxPassed13.BackColor = Color.White;
            tBoxPassed14.BackColor = Color.White;
            tBoxPassed15.BackColor = Color.White;
            tBoxPassed16.BackColor = Color.White;
            tBoxPassed17.BackColor = Color.White;
            tBoxPassed18.BackColor = Color.White;
            tBoxPassed19.BackColor = Color.White;
            tBoxPassed20.BackColor = Color.White;
            tBoxPassed21.BackColor = Color.White;
            tBoxPassed22.BackColor = Color.White;
            tBoxPassed23.BackColor = Color.White;
            tBoxPassed24.BackColor = Color.White;
            tBoxPassed25.BackColor = Color.White;
            tBoxPassed26.BackColor = Color.White;
            tBoxPassed27.BackColor = Color.White;
            tBoxPassed28.BackColor = Color.White;
            tBoxPassed29.BackColor = Color.White;
            tBoxPassed30.BackColor = Color.White;
            tBoxPassed31.BackColor = Color.White;
            tBoxPassed32.BackColor = Color.White;
            tBoxPassed33.BackColor = Color.White;
            tBoxPassed34.BackColor = Color.White;
            tBoxPassed35.BackColor = Color.White;
            tBoxPassed36.BackColor = Color.White;
            tBoxPassed37.BackColor = Color.White;
            tBoxPassed38.BackColor = Color.White;
            tBoxPassed39.BackColor = Color.White;
            tBoxPassed40.BackColor = Color.White;

            tBoxPassed1.Text = "";
            tBoxPassed2.Text = "";
            tBoxPassed3.Text = "";
            tBoxPassed4.Text = "";
            tBoxPassed5.Text = "";
            tBoxPassed6.Text = "";
            tBoxPassed7.Text = "";
            tBoxPassed8.Text = "";
            tBoxPassed9.Text = "";
            tBoxPassed10.Text = "";
            tBoxPassed11.Text = "";
            tBoxPassed12.Text = "";
            tBoxPassed13.Text = "";
            tBoxPassed14.Text = "";
            tBoxPassed15.Text = "";
            tBoxPassed16.Text = "";
            tBoxPassed17.Text = "";
            tBoxPassed18.Text = "";
            tBoxPassed19.Text = "";
            tBoxPassed20.Text = "";
            tBoxPassed21.Text = "";
            tBoxPassed22.Text = "";
            tBoxPassed23.Text = "";
            tBoxPassed24.Text = "";
            tBoxPassed25.Text = "";
            tBoxPassed26.Text = "";
            tBoxPassed27.Text = "";
            tBoxPassed28.Text = "";
            tBoxPassed29.Text = "";
            tBoxPassed30.Text = "";
            tBoxPassed31.Text = "";
            tBoxPassed32.Text = "";
            tBoxPassed33.Text = "";
            tBoxPassed34.Text = "";
            tBoxPassed35.Text = "";
            tBoxPassed36.Text = "";
            tBoxPassed37.Text = "";
            tBoxPassed38.Text = "";
            tBoxPassed39.Text = "";
            tBoxPassed40.Text = "";

            tbState.Text = "";
            tbState.BackColor = Color.Ivory;

            serialBufferClear();
            ConsoleClean();
            //Application.Restart();
        }

        private void logTut(int state)
        {
            if (logDosyaPath != "")
            {
                INIKaydet ini = new INIKaydet(logDosyaPath);  // @"\Ayarlar.ini"
                if (state == 1)
                {
                    ini.Yaz(tbBarcodeLast.Text + "   " + DateTime.Now.ToString(), "", "PASSED");
                }
                else if (state == 0)
                {
                    ini.Yaz(tbBarcodeLast.Text + "   " + DateTime.Now.ToString(), "", "FAILED");
                }

              //  CustomMessageBox.ShowMessage("Log Kaydedildi.", Ayarlar.Default.projectName, MessageBoxButtons.OK, CustomMessageBoxIcon.Information, Color.Yellow);
            }
            else
            {
                CustomMessageBox.ShowMessage("Log için Dosya Yolu Boş Kalamaz", Ayarlar.Default.projectName, MessageBoxButtons.OK, CustomMessageBoxIcon.Error, Color.Red);
            }
        }

        private void rtbConsole_TextChanged(object sender, EventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            rtb.SelectionStart = rtb.Text.Length;
            rtb.ScrollToCaret();
        }
        
        /*Kullanıcı Arayüzüne Yazı Yazılır*/
        private void ConsoleAppendLine(string text, Color color)
        {
            if (rtbConsole.InvokeRequired)
            {
                rtbConsole.Invoke(new Action(delegate ()
                {
                    rtbConsole.Select(rtbConsole.TextLength, 0);
                    rtbConsole.SelectionColor = color;
                    rtbConsole.AppendText(text + Environment.NewLine);
                    rtbConsole.Select(rtbConsole.TextLength, 0);
                    rtbConsole.SelectionColor = Color.White;
                }));
            }
            else
            {
                rtbConsole.Select(rtbConsole.TextLength, 0);
                rtbConsole.SelectionColor = color;
                rtbConsole.AppendText(text + Environment.NewLine);
                rtbConsole.Select(rtbConsole.TextLength, 0);
                rtbConsole.SelectionColor = Color.White;
            }
        }

        /*Kullanıcı Arayüzünde Bir Satır Boşluk Bırakılır*/
        private void ConsoleNewLine()
        {
            if (rtbConsole.InvokeRequired)
            {
                rtbConsole.Invoke(new Action(delegate ()
                {
                    rtbConsole.AppendText(Environment.NewLine);
                }));
            }
            else
            {
                rtbConsole.AppendText(Environment.NewLine);
            }
        }

        private void ConsoleClean()
        {
            if (rtbConsole.InvokeRequired)
            {
                rtbConsole.Invoke(new Action(delegate ()
                {
                    rtbConsole.Text = "";
                    rtbConsole.Select(rtbConsole.TextLength, 0);
                    rtbConsole.SelectionColor = Color.White;
                }));
            }
            else
            {
                rtbConsole.Text = "";
                rtbConsole.Select(rtbConsole.TextLength, 0);
                rtbConsole.SelectionColor = Color.White;
            }
        }

        private void btnCikis_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAyar_Click(object sender, EventArgs e)
        {
            int num = (int)this.AyarFrm.ShowDialog();
        }

        private void btnKurulum_Click(object sender, EventArgs e)
        {
            int num = (int)this.KurulumFrm.ShowDialog();
        }

        public void yetkidegistir()
        {
            if (this.yetki == 0)
            {
                this.btnCikis.Enabled = false;
                this.btnAyar.Enabled = false;
                this.btnProgAyar.Enabled = false;
                this.btnKurulum.Enabled = false;
                this.btnCikis.BackColor = Color.Beige;
                this.btnAyar.BackColor = Color.Beige;
                this.btnProgAyar.BackColor = Color.Beige;
                this.btnKurulum.BackColor = Color.Beige;
            }
            if (this.yetki == 1)
            {
                this.btnCikis.Enabled = true;
                this.btnAyar.Enabled = true;
                this.btnProgAyar.Enabled = true;
                this.btnKurulum.Enabled = true;
                this.btnCikis.BackColor = Color.Red;
                this.btnAyar.BackColor = Color.Red;
                this.btnProgAyar.BackColor = Color.Red;
                this.btnKurulum.BackColor = Color.Red;
                timerAdmin.Start();
            }
            if (this.yetki == 2)
            {
                this.btnCikis.Enabled = true;
                this.btnCikis.BackColor = Color.Red;
                this.btnAyar.BackColor = Color.Beige;
                this.btnKurulum.BackColor = Color.Beige;
                this.btnProgAyar.BackColor = Color.Beige;
                timerAdmin.Start();
            }
        }

        private void tbUserLogin_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyData != Keys.L)
                return;
            if (this.yetki != 0)
            {
                timerAdmin.Stop();
                this.yetki = 0;
                this.yetkidegistir();
            }
            else
            {
                int num = (int)this.SifreFrm.ShowDialog();
                tbUserLogin.Clear();
            }
        }

        private void serialTx1timer_Tick(object sender, EventArgs e)
        {
            serialTx1TimerCounter++;
            if (serialTx1TimerCounter == 1)
            {
                serialTx1TimerCounter = 0;
                serialTx1timer.Stop();
                serialWriteByte1();
            }
        }

        private void serialTx2timer_Tick(object sender, EventArgs e)
        {
            serialTx2TimerCounter++;
            if (serialTx2TimerCounter == 1)
            {
                serialTx2TimerCounter = 0;
                serialTx2timer.Stop();
                serialWriteByte2();
            }
        }

        private void serialTx3timer_Tick(object sender, EventArgs e)
        {
            serialTx3TimerCounter++;
            if (serialTx3TimerCounter == 1)
            {
                serialTx3TimerCounter = 0;
                serialTx3timer.Stop();
                serialWriteByte3();
            }
        }

        private void timerAdmin_Tick_1(object sender, EventArgs e)
        {
            adminTimerCounter++;
            if (adminTimerCounter == 1)
            {
                adminTimerCounter = 0;
                timerAdmin.Stop();
                this.yetki = 0;
                this.yetkidegistir();
            }
        }

        private void serialRxTimeout_Tick(object sender, EventArgs e)
        {
            timeoutTimerCounter++;
            if (timeoutTimerCounter == 1)
            {
                timeoutTimerCounter = 0;
                serialRxTimeout.Stop();
                serialRxTimeout.Enabled = false;
                ProcessFailed();
            }
        }

        private void saniyeTimer_Tick(object sender, EventArgs e)
        {
            saniyeTimerCounter++;
            if (saniyeTimerCounter == 1)
            {
                saniyeTimerCounter = 0;
                fctTimerTxt.Text = Convert.ToString(++fctSaniye);
            }
        }
        /***********************************************************************************************************************/
        private void btnStartProgramming_Click(object sender, EventArgs e)
        {
            string barcode = tbBarcodeCurrent.Text;
            Thread programThread = new Thread(ProgramThreadFunction);
            programThread.Start(barcode);
        }

        private void tbBarcodeCurrent_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
              //  btnStartProgramming_Click(sender, e);
            }
        }

        /* Tüm Ana İşlemlerin Yönlendirilmesi*/
        private void ProgramThreadFunction(object data)
        {
            bool result = false;

            // Clean console
            ConsoleClean();

            string barcode = (string)data;
            string company_no = string.Empty;
            string SAP_no = string.Empty;
            string production_date = string.Empty;
            string index_no = string.Empty;
            string production_no = string.Empty;
            string card_no = string.Empty;

            // if barcode is true
            if (BarcodeCheck(barcode))
            {
                // Show message box
                DialogResult dialog_result = DialogResult.None;
                if (Kurulum.Default.chBoxProgramlama)
                {
                    dialog_result = DialogResult.OK;
                }
                else
                {
                    this.Invoke(new Action(delegate ()
                    {
                        dialog_result = CustomMessageBox.ShowMessage("Programlama kablolarını doğru şekilde karta takınız. Sonra Tamam'a tıklayınız!", customMessageBoxTitle, MessageBoxButtons.OKCancel, CustomMessageBoxIcon.Warning, Color.Yellow);
                    }));
                }
                if (dialog_result == DialogResult.OK)
                {
                    // set ProgrammingStarted flag
                    isProgrammingStarted = true;

                    // Disable Programming Button
                    btnStartProgramming.Invoke(new Action(delegate () { btnStartProgramming.Enabled = false; }));

                    ConsoleAppendLine("Barkod: " + barcode, Color.Black);
                    ConsoleNewLine();
                    ConsoleAppendLine("Barkod kriterlere uygun.", Color.Green);
                    ConsoleNewLine();

                    company_no = GetStringBetweenTwoSubStrings(barcode, "$01", "$02");
                    SAP_no = (GetStringBetweenTwoSubStrings(barcode, "$02", "$03").Substring(0, 10));
                    production_date = GetStringBetweenTwoSubStrings(barcode, "$03", "$04");
                    productionDate = production_date;
                    index_no = GetStringBetweenTwoSubStrings(barcode, "$04", "$05");
                    indexNo = index_no;
                    production_no = GetStringBetweenTwoSubStrings(barcode, "$05", "$06");
                    productionNo = production_no;
                    card_no = GetStringBetweenTwoSubStrings(barcode, "$06", "$07");

                    ConsoleAppendLine("Company No: " + company_no, Color.Black);
                    ConsoleAppendLine("SAP No: " + SAP_no, Color.Black);
                    ConsoleAppendLine("Production Date: " + production_date, Color.Black);
                    ConsoleAppendLine("Index No: " + index_no, Color.Black);
                    ConsoleAppendLine("Product No: " + production_no, Color.Black);
                    ConsoleAppendLine("Card No: " + card_no, Color.Black);
                    result = true;
                }
                else
                {
                    tbBarcodeCurrent.Invoke(new Action(delegate ()
                    {
                        // Focus Barcode textbox
                        tbBarcodeCurrent.Focus();
                        // Select all text in textbox
                        tbBarcodeCurrent.SelectionStart = 0;
                        tbBarcodeCurrent.SelectionLength = tbBarcodeCurrent.Text.Length;
                    }));

                    return;
                }
            }
            else
            {
                if (barcode.Equals(""))
                {
                    isProgrammingState = 1;
                    ConsoleAppendLine("Barkod: " + barcode, Color.Black);
                    ConsoleNewLine();
                    ConsoleAppendLine("Yanlış barkod! Barkod kriterlere uygun değil! Programlama yapılamadı!", Color.Red);
                    ConsoleNewLine();
                    result = false;
                }
                else
                {
                    isProgrammingState = 1;
                    ConsoleAppendLine("Barkod: " + barcode, Color.Black);
                    ConsoleNewLine();
                    ConsoleAppendLine("Yanlış barkod! Barkod kriterlere uygun değil! Programlama yapılamadı!", Color.Red);
                    ConsoleNewLine();
                    result = false;
                }
            }

            // Update status bar
            if (result)
            {
                tbState.Invoke(new Action(delegate ()
                {
                    tbState.BackColor = Color.Green;
                    tbState.Text = "PROGRAMLAMA BAŞARILI";
                    stepState = 0;
                    ProcessCountinue();
                    this.Invoke(new EventHandler(btnFCTInit_Click));
                }));
            }
            else
            {
                tbState.Invoke(new Action(delegate ()
                {
                    isProgrammingState = 1;
                    tbState.BackColor = Color.Red;
                    tbState.Text = "PROGRAMLAMA BAŞARISIZ";
                }));
                ProcessFailed();
            }

            // Assign Last Barcode with Current
            tbBarcodeLast.Invoke(new Action(delegate () { tbBarcodeLast.Text = barcode; }));

            tbBarcodeCurrent.Invoke(new Action(delegate ()
            {
                // Clean Current Barcode
                tbBarcodeCurrent.Text = "";
                // Focus Barcode textbox
                tbBarcodeCurrent.Focus();
                // Select all text in textbox
                tbBarcodeCurrent.SelectionStart = 0;
                tbBarcodeCurrent.SelectionLength = tbBarcodeCurrent.Text.Length;
            }));

            // Enable Programming Button
            btnStartProgramming.Invoke(new Action(delegate () { btnStartProgramming.Enabled = true; }));

            // set ProgrammingStarted flag
            isProgrammingStarted = false;
        }

        /* Gelen Barcode Kontrol Edilir*/
        private bool BarcodeCheck(String barcode)
        {
            // Barcode length should be between 26-150.
            if (barcode.Length < 26 || barcode.Length > 150) return false;

            // Simple starting contains ending substring check.
            if (!(barcode.StartsWith("$01") && barcode.Contains("$02") && barcode.Contains("$03") && barcode.Contains("$04") 
                && barcode.Contains("$05") && barcode.Contains("$06") && barcode.Contains("$07")
                && barcode.EndsWith("#")))
            {
                return false;
            }

            // Find the index of the substrings in Barcode. Value is -1, if substring is not exist.
            int[] index = new int[8];
            int count = 150;
            if (barcode.Length < 150) count = barcode.Length;
            index[0] = barcode.IndexOf("$01", 0, count);
            index[1] = barcode.IndexOf("$02", 0, count);
            index[2] = barcode.IndexOf("$03", 0, count);
            index[3] = barcode.IndexOf("$04", 0, count);
            index[4] = barcode.IndexOf("$05", 0, count);
            index[5] = barcode.IndexOf("$06", 0, count);
            index[6] = barcode.IndexOf("$07", 0, count);
            index[7] = barcode.IndexOf("#", 0, count);

            for (int i = 0; i < index.Length; ++i)
            {
                // if substring is not exist
                if (index[i] == -1)
                {
                    return false;
                }

                // if substring position is not correct
                for (int j = i + 1; j < index.Length; ++j)
                {
                    if (index[i] > index[j])
                    {
                        return false;
                    }
                }
            }
            //Karşılaştırma Kısmı
            string company_no = GetStringBetweenTwoSubStrings(barcode, "$01", "$02");
            string SAP_no = (GetStringBetweenTwoSubStrings(barcode, "$02", "$03").Substring(0, 10));
            string card_no = GetStringBetweenTwoSubStrings(barcode, "$06", "$07");;
            // check company no
            if (!company_no.Equals(companyNo))
            {
                return false;
            }

            if (radioButton1.Checked)   //100
            {
                SAPNo = radioButton1.Text;
                computerBatchFileAdress = Prog_Ayarlar.Default.Logdosyayolu1;
            }
            else if (radioButton2.Checked)  //200
            {
                SAPNo = radioButton2.Text;
                computerBatchFileAdress = Prog_Ayarlar.Default.Logdosyayolu2;
            }
            else if (radioButton3.Checked)  //300
            {
                SAPNo = radioButton3.Text;
                computerBatchFileAdress = Prog_Ayarlar.Default.Logdosyayolu3;
            }
            else if (radioButton4.Checked)  //400
            {
                SAPNo = radioButton4.Text;
                computerBatchFileAdress = Prog_Ayarlar.Default.Logdosyayolu4;
            }
            else if (radioButton5.Checked)  //500
            {
                SAPNo = radioButton5.Text;
                computerBatchFileAdress = Prog_Ayarlar.Default.Logdosyayolu5;
            }

            // check SAP no
            if (!SAP_no.Equals(SAPNo))
            {
                return false;
            }

            // check card no
            if (!card_no.Equals(cardNo))
            {
                return false;
            }

            // There is no problem
            return true;
        }

        /*Gelen Barkodu Ayıklanır*/
        private String GetStringBetweenTwoSubStrings(String source, String sub1, String sub2)
        {
            int pFrom = source.IndexOf(sub1) + sub1.Length;
            int pTo = source.LastIndexOf(sub2);
            String result = source.Substring(pFrom, pTo - pFrom);
            return result;
        }

        private bool ProgramProduct(string product_no)
        {
            bool result = false;
            String batchPath = String.Empty;

            for (int i = 1; i <= versions_number; i++)
            {

                if (versionsBarcodName[i] == product_no)
                {
                    if (stepProgJob[versions_number] == 1)
                    {
                        batchPath = computerBatchFileAdress + product_no + ".bat";    // C:\Users\serkan.baki\Desktop\MIND-BATCH-FILES\
                        result = RunBatch(batchPath, customMessageBoxTitle);
                        if (result == false) return result;
                    }
                    else if (stepProgJob[versions_number] == 2)
                    {
                        batchPath = computerBatchFileAdress + product_no + ".bat";    // C:\Users\serkan.baki\Desktop\MIND-BATCH-FILES\
                        result = RunBatch(batchPath, customMessageBoxTitle);
                        if (result == false) return result;
                        if (CustomMessageBox.ShowMessage("Slave Yüklemesi Yapılması İçin Önce Programlama Kablosunu Takınız ve Evet'e Tıklayınız", customMessageBoxTitle, MessageBoxButtons.YesNo, CustomMessageBoxIcon.Question, Color.Yellow) == DialogResult.Yes)
                        {
                            batchPath = computerBatchFileAdress + versionsSlaveName[i] + ".bat";    // C:\Users\serkan.baki\Desktop\MIND-BATCH-FILES\
                            result = RunBatch(batchPath, customMessageBoxTitle);
                            if (result == false) return result;
                        }
                    }
                    break;
                }
            }
            return result;
        }

        /*Seçilen .bat Çalıştırılır- Kontrol Edilir ve Kapatılır*/
        private bool RunBatch(string batch_path, string batch_name)
        {
            bool result = false;

            Process processBatch = new Process();
            processBatch.StartInfo.UseShellExecute = false;
            processBatch.StartInfo.RedirectStandardOutput = true;
            processBatch.StartInfo.CreateNoWindow = true;
            processBatch.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processBatch.StartInfo.FileName = batch_path;
            //processBatch.StartInfo.Arguments = string.Format("");
            processBatch.Start();

            StreamReader strmReader = processBatch.StandardOutput;
            string batchTempRow = string.Empty;
            // get all lines of batch
            while ((batchTempRow = strmReader.ReadLine()) != null)
            {
                // Write batch operation to the console
                ConsoleAppendLine(batchTempRow, Color.White);

                // check programming is successful.
                // if succesfully finished.
                if (Prog_Ayarlar.Default.chBoxSuccess && ((batchTempRow.IndexOf("pause", StringComparison.OrdinalIgnoreCase) >= 0) || (batchTempRow.IndexOf(batchFileFeedback[0], StringComparison.OrdinalIgnoreCase) >= 0)))  // color ae
                {
                    isProgrammingState = 2;
                    ConsoleNewLine();
                    ConsoleAppendLine(batch_name + " Programlama İşlemi Başarıyla Tamamlanmıştır!", Color.Green);
                    result = true;
                    break;
                }
                else if (Prog_Ayarlar.Default.chBoxError1 && ((batchTempRow.IndexOf("pause", StringComparison.OrdinalIgnoreCase) >= 0) || (batchTempRow.IndexOf(batchFileFeedback[1], StringComparison.OrdinalIgnoreCase) >= 0))) //Could not start CPU core.
                {
                    isProgrammingState = 1;
                    ConsoleNewLine();
                    ConsoleAppendLine(batch_name + " Programlama İşlemi Başarısız1.", Color.Red);  // Programlama Soketi Düzgün Takılı Değil!
                    All_FCT_Fail();
                    result = false;
                    break;
                }
                else if (Prog_Ayarlar.Default.chBoxError2 && ((batchTempRow.IndexOf("pause", StringComparison.OrdinalIgnoreCase) >= 0) || (batchTempRow.IndexOf(batchFileFeedback[2], StringComparison.OrdinalIgnoreCase) >= 0)))  // Cannot connect to target.
                {
                    isProgrammingState = 1;
                    ConsoleNewLine();
                    ConsoleAppendLine(batch_name + " Programlama İşlemi Başarısız2.", Color.Red); // Programlama Soketi Takılı Değil!
                    All_FCT_Fail();
                    result = false;
                    break;
                }
                else if (Prog_Ayarlar.Default.chBoxError3 && ((batchTempRow.IndexOf("pause", StringComparison.OrdinalIgnoreCase) >= 0) || (batchTempRow.IndexOf(batchFileFeedback[3], StringComparison.OrdinalIgnoreCase) >= 0))) //FAILED
                {
                    isProgrammingState = 1;
                    ConsoleNewLine();
                    ConsoleAppendLine(batch_name + " Programlama İşlemi Başarısız3.", Color.Red);  //  USB Takılı Değil!
                    All_FCT_Fail();
                    result = false;
                    break;
                }
            }

            // if batch didn't closed kill it.
            if (!processBatch.HasExited)
            {
                processBatch.Kill();
            }

            return result;
        }

        private void btnProgAyar_Click(object sender, EventArgs e)
        {
            int num = (int)this.ProgAyarFrm.ShowDialog();
        }

        private void serialWriteHSM()
        {
            byte[] byteArray = new byte[8];
            byteArray[0] = 4;
            byteArray[1] = 0;
            byteArray[2] = 0;
            byteArray[3] = 0;
            byteArray[4] = 251;
            for (int i = 0; i < 5; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM1'den gitti.", Color.Blue);
            serialPort1.Write(byteArray, 0, 5);
        }

        private void serialWriteRelayOff()
        {
            byte[] byteArray = new byte[8];
            byteArray[0] = 3;
            byteArray[1] = 0;
            byteArray[2] = 1;
            byteArray[3] = 14;
            byteArray[4] = 237;
            for (int i = 0; i < 5; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM1'den gitti.", Color.Blue);
            serialPort1.Write(byteArray, 0, 5);
            Thread.Sleep(100);

            byteArray[0] = 3;
            byteArray[1] = 0;
            byteArray[2] = 1;
            byteArray[3] = 18;
            byteArray[4] = 233;
            for (int i = 0; i < 5; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM1'den gitti.", Color.Blue);
            serialPort1.Write(byteArray, 0, 5);
            Thread.Sleep(100);

            byteArray[0] = 3;
            byteArray[1] = 0;
            byteArray[2] = 1;
            byteArray[3] = 10;
            byteArray[4] = 241;
            for (int i = 0; i < 5; i++)
            {
                ConsoleAppendLine("' " + Convert.ToByte(byteArray[i]) + " '", Color.Blue);
            }
            ConsoleAppendLine("COM1'den gitti.", Color.Blue);
            serialPort1.Write(byteArray, 0, 5);
            Thread.Sleep(100);

            waitTimer.Start();
        }

        private void serialWriteBLE()
        {
            stepState = 79;
            string uniq_id = "";
            for (int i = 0; i < 9; i++)
            {
                int number = int.Parse(Convert.ToString(arrayRx[20 + i]));
                if (number < 16)
                {
                    uniq_id = uniq_id + "0";
                }
                uniq_id = uniq_id + Convert.ToString(number, 16);
                ConsoleAppendLine("' " + Convert.ToByte(arrayRx[20 + i]) + " '", Color.Blue);
            }
            HSMId = uniq_id;
            UniqId =  uniq_id + "0000";
            BLEName = "HwZ_" + uniq_id;
            ConsoleAppendLine(BLEName, Color.Blue);

            serialBufferClear();  //Yeni
            // stepState = 39;
            // ProcessCountinue(); //40. Adıma Gitsin Diye
            BLEScan();
        }

        public void BLEScan()
        {
            try
            {
                BLEScanState = true;
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                watcher.SignalStrengthFilter.InRangeThresholdInDBm = -80;
                watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -90;
                watcher.Received += OnAdvertisementReceived;
                watcher.Start();
                serialRxTimeout.Start();
                serialRxTimeout.Enabled = true;
              //  geciciLoop();
            }
            catch (Exception ex)
            {
                ConsoleAppendLine("Error BLE2", Color.Red);
                ErrorBleCounter++;
                if (ErrorBleCounter == 100)
                {
                    ErrorBleCounter = 0;
                    ProcessFailed();
                }
            }
        }

        public void geciciLoop()
        {
            while(loopEnd)
            {

            }
        }

        public void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            try
            {
                if (Convert.ToString(eventArgs.Advertisement.LocalName).Substring(0,4) == "HwZ_" && BLEScanState == true)
                {
                    BLEScanState = false;
                    ConsoleAppendLine(Convert.ToString(eventArgs.Advertisement.LocalName), Color.Green);
                    watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                    watcher.Stop();
                    loopEnd = false;
                    cloudAction();
                }
            }
            catch (Exception ex)
            {
                ConsoleAppendLine("Error BLE2", Color.Red);
                ErrorBleCounter++;
                if(ErrorBleCounter == 100)
                {
                    ErrorBleCounter = 0;
                    ProcessFailed();
                }
            }
        }

         void cloudAction()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            //  var token = HttpClientExtentions.GetToken("ALPPLASTEST", "ALPPLASTEST*"); //TEST
            var token = HttpClientExtentions.GetToken("ALPPLAS", "AlpPlas1*"); //PRODUCTION
            barcode72 = companyNo + SAPNo + productionDate + indexNo + productionNo + cardNo + gerberVer + BOMVer + ICTRev + FCTRev + softwareVer + softwareRev + UniqId;
            ConsoleAppendLine("Barkod:" + barcode72, Color.Orange);
            InsertConnectedCard(token);
        }

        public void InsertConnectedCard(string token) 
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(BaseUrl);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                 
                var model = new CreateProductCardDataModel()
                {
                    FullBarcode = barcode72, // 72 characters card information
                    CertificateId = "", //If exist  Formülü bekliyorum
                    HsmId = HSMId, //If hsm exist  //18 Hane
                    CertificatePublicData = "", //If hsm exist
                    MacId = "" //If barcode doesn't have macid
                };

                var json = JsonConvert.SerializeObject(model);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync("api/CardData/AddOrUpdateWifiCardData", httpContent).Result.Content.ReadAsStringAsync().Result;

                ReturnModel<string> result = JsonConvert.DeserializeObject<ReturnModel<string>>(response);
                if (result.Status == ReturnTypeStatus.Success)
                {
                    ConsoleAppendLine("Card Added to Arcelik successfully", Color.Green);
                    printAction();
                }
                else if (result.Status == ReturnTypeStatus.Error)
                {
                    ConsoleAppendLine("Cloud ERROR:" + result.Message, Color.Green);
                    ProcessFailed();
                }
              //  printAction();
                Console.ReadLine();
            }
        }

        void printAction()
        {
            // ^XA: Start Format
            // ^XZ: End Format
            // ^LHx,y: Label Home Position, x = x-axis position (in dots), y = y-axis position (in dots), Values: 0 to 32000
            // ^FOx,y: Field Origin, x = x-axis position (in dots), y = y-axis position (in dots), Values: 0 to 32000
            // ^Afo,h,w: Scalable/Bitmapped Font, f = font name, o = field orientation, h = Character Height (in dots), w = width (in dots), Values: 10 to 32000
            // ^FD: Field Data
            // ^FS: Field Separator
            string s1 = companyNo + indexNo.Substring(0, 2);
            string s2 = indexNo.Substring(2, 4);
            string s3 = productionNo.Substring(0, 4);
            string s4 = productionNo.Substring(4, 4);
            string s5 = productionNo.Substring(8, 4);
            string s6 = productionNo.Substring(12, 2) + cardNo;
            string start = "^XA" + "^LH10,25";
            string qr = "^BQN,2,2" + "^FDQA," + barcode72 + "^FS";
            string veri1 = "^FO70,5" + "^A0,12,12" + "^FD" + "P/N: " + SAPNo + "^FS";   //İlki Pozisyon //İkincisi Boy-En
            string veri2 = "^FO70,25" + "^A0,12,12" + "^FD" + "S/N: " + s1 + "-" + s2 + "-" + s3 + "^FS";   //İlki Pozisyon //İkincisi Boy-En
            string veri3 = "^FO70,45" + "^A0,12,12" + "^FD" + "       " + s4 + "-" + s5 + "-" + s6 + "^FS";   //İlki Pozisyon //İkincisi Boy-En
            string veri4 = "^FO70,65" + "^A0,12,12" + "^FD" + "VER: " + softwareVer + "." + softwareRev + " G:" + gerberVer + " B:" + BOMVer + " T:" + productionDate + "^FS";   //İlki Pozisyon //İkincisi Boy-En
            string end = "^XZ";
            string test = start + qr + veri1 + veri2 + veri3 + veri4 + end;

            //Get local print server
            var server = new LocalPrintServer();

            //Load queue for correct printer
            PrintQueue pq = server.GetPrintQueue(printerName, new string[0] { });
            PrintJobInfoCollection jobs = pq.GetPrintJobInfoCollection();
            foreach (PrintSystemJobInfo job in jobs)
            {
                job.Cancel();
            }

            if (!RawPrinterHelper.SendStringToPrinter(printerName, test))
            {
                // Show message box
                if (CustomMessageBox.ShowMessage("Printer Error: (" + printerName + ")", customMessageBoxTitle, MessageBoxButtons.OK, CustomMessageBoxIcon.Error, Color.WhiteSmoke) == DialogResult.OK)
                {
                }
            }
            stepState = 39;
            ProcessCountinue(); //40. Adıma Gitsin Diye
        }

        #region class declarations
        public class CreateProductCardDataModel
        {
            public string FullBarcode { get; set; }
            public string HsmId { get; set; }
            public string CertificateId { get; set; }
            public string CertificatePublicData { get; set; }
            public string MacId { get; set; }
        }

        public static class HttpClientExtentions
        {
            public static string GetToken(string userName, string password)
            {
                var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };

                var response =
                  client.PostAsync("Token",
                    new StringContent(string.Format("grant_type=password&username={0}&password={1}",
                      HttpUtility.UrlEncode(userName),
                      HttpUtility.UrlEncode(password)), Encoding.UTF8,
                      "application/x-www-form-urlencoded")).Result;

                var resultJson = response.Content.ReadAsStringAsync().Result;

                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultJson);

                return dict["access_token"].ToString();
            }
        }

        public class ReturnModel<T>
        {
            public T Data { get; set; }
            public ReturnTypeStatus Status { get; set; }
            public string Message { get; set; }
        }

        public enum ReturnTypeStatus
        {
            Success = 1,
            Error = 2,
            NotFound = 3,
            Working = 4
        }
        #endregion

        private void waitTimer_Tick(object sender, EventArgs e)
        {
            if (stepState == 88)
            {
                stepState = 8;
                ProcessCountinue(); //9. Adıma Gitsin Diye
            }

            waitTimer.Stop();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox2.Checked == true)
            {
                tbUserLogin.Enabled = true;
            }
            else
            {
                tbUserLogin.Enabled = false;
            }
        }
    }
}

