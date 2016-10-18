﻿using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Virtion.WeChat.Server;
using Virtion.WeChat.Util;

namespace Virtion.WeChat.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        private int tip;//state 
        private string uuid;
        private string redirectUrl;
        private BackgroundWorker backgroundWorker;


        private bool isEnabled;
        public bool IsEnabled
        {
            get { return this.isEnabled; }
            set
            {
                this.isEnabled = value;
                base.RaisePropertyChanged("IsEnabled");
            }
        }

        private string loginInfo;
        public String LoginInfo
        {
            get { return this.loginInfo; }
            set
            {
                this.loginInfo = value;
                base.RaisePropertyChanged("LoginInfo");
            }
        }

        private ImageSource source;
        public ImageSource Source
        {
            get { return this.source; }
            set
            {
                this.source = value;
                base.RaisePropertyChanged("Source");
            }
        }


        public RelayCommand StartCommand
        {
            get;
            private set;
        }


        public LoginViewModel()
        {
            this.isEnabled = true;
            StartCommand = new RelayCommand(Window_Loaded);
        }

        private void Window_Loaded()
        {
            LoginInfo = "正在加载二维码";

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += Login_DoWork;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.ProgressChanged += Login_StateChange;
            backgroundWorker.RunWorkerCompleted += Login_Completed;
            backgroundWorker.RunWorkerAsync();
        }


        private void Login_DoWork(object sender, DoWorkEventArgs e)
        {
            GetQrCode();
            backgroundWorker.ReportProgress(0);

            //等待登录
            while (!backgroundWorker.CancellationPending)
            {
                string url = WxApi.LoginUrl +
                    "?tip=" + tip +
                    "&uuid=" + uuid +
                    "&_=" + Time.Now();

                string ret = HttpRequest.GetSync(url);

                Console.WriteLine("等待登录");
                Console.WriteLine(ret);

                string[] rets = ret.Split(new char[] { '=', ';' });
                string code = rets[1];
                switch (rets[1])
                {
                    case "408"://超时
                        break;
                    case "201"://已扫描
                        tip = 0;
                        //状态报告(1);
                        //状态报告(2);
                        backgroundWorker.ReportProgress(201);
                        break;
                    case "200"://已登录
                        //状态报告(3);
                        backgroundWorker.ReportProgress(201);
                        redirectUrl = ret.Split('"')[1];
                        backgroundWorker.CancelAsync();
                        break;
                    default://400,500
                        GetQrCode();
                        backgroundWorker.ReportProgress(0);
                        break;
                }
            }
        }

        private void Login_StateChange(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 0:
                    LoginInfo = "请使用微信扫描二维码以登录";
                    break;
                case 201:
                    LoginInfo = "成功扫描,请在手机上点击确认以登录";
                    break;
                case 200:
                    LoginInfo = "正在登录...";
                    break;
            }
        }

        private void Login_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (string.IsNullOrEmpty(redirectUrl) == true)
                return;

            if (this.redirectUrl.IndexOf("wx2") > -1)
            {
                WxApi.Server = "wx2";
            }
            App.MainWindow.RedirectUrl = this.redirectUrl;

            this.IsEnabled = false;
        }

        private void GetQrCodeUuid()
        {
            string ret = HttpRequest.GetSync(WxApi.QrCodeUuidUrl);
            this.uuid = ret.Split('"')[1];
            this.tip = 1;
        }

        private void GetQrCode()
        {
            GetQrCodeUuid();
            Uri uri = new Uri(WxApi.QrCodeImageUrl + uuid + "?t=webwx&_=" + Time.Now(), UriKind.Absolute);
 
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                this.Source = new BitmapImage(uri);
            });
        }


    }
}
