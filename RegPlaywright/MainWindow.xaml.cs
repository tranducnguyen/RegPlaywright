using PlaywrightSharp;
using PlaywrightSharp.Chromium;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RegPlaywright.Controller;
using RegPlaywright.Model;
using System.Management;
using Dasync.Collections;

namespace RegPlaywright
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Ver 1.0
        const string version = "1.0.0";
        int numThread;
        int checkChrome;
        int numTabSameTime;
        string deviceChose;
        int numSuccess;
        bool isCCmoiD = true;
        string pathBrowser = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

        public MainWindow()
        {
           
            InitializeComponent();
            this.Title = "Registration Facebook by Kyo ver " + version;
        }
        void CreatAcMulti_v2()
        {
            _ = CreatAcMulti_v2Async();
        }
        async Task CreatAcMulti_v2Async()
        {
            InfoController InfoController = new InfoController();
            numSuccess = 0;

            Dispatcher.Invoke(new Action(() =>
            {
                this.btnReg.Content = "Clicked";
            }));
            DateTime dateNow = DateTime.Now;
            DateTime dateCheck = new DateTime(2021, 5, 11);
            int dateCount = dateNow.Subtract(dateCheck).Days;
            DbAction db = new DbAction();
            for (int k = 0; k < 5000; k++)
            {
                UpdateShow("Bắt đầu Success: " + this.numSuccess);
                checkChrome = numThread;
                List<ChroniumReg> listChrome = new List<ChroniumReg>();
                List<Info> listInfo = new List<Info>();
                listInfo = InfoController.Create(numThread);
                foreach (Info item in listInfo)
                {
                    ChroniumReg chromeReg = new ChroniumReg
                    {
                        Info = item
                    };
                    listChrome.Add(chromeReg);
                }
                Dispatcher.Invoke(new Action(() =>
                {
                    lsvData.ItemsSource = listInfo;
                }));
                int index = 0;

                using var ctsAll = new CancellationTokenSource();
                ctsAll.CancelAfter(TimeSpan.FromMinutes(5));
                await listChrome.ParallelForEachAsync(async (item) =>
                {
                    int vitri = index;
                    int x = 100 + ((vitri % 4) * 250);
                    int y = (vitri / 4) * 150;
                    index++;
                    await Task.Delay(100);
                    await CreateBrowseAsync(item, x, y, "", vitri).ConfigureAwait(false);

                },
                maxDegreeOfParallelism: this.numTabSameTime,
                ctsAll.Token);

                await listChrome.ParallelForEachAsync(async (item) =>
                {
                    await RegBrowseAsync(item).ConfigureAwait(false);

                }, maxDegreeOfParallelism: this.numThread, ctsAll.Token);

                Debug.Print("Nạp file vào db");

                foreach (ChroniumReg item in listChrome)
                {
                    if (item.Info.Uid != null)
                    {
                        if (item.Info.Uid.Length > 0)
                        {
                            File.AppendAllText("ketqua.txt", item.Info.Uid + "|" + item.Info.Pass + "|" + item.Info.Cookie + "\n");
                        }
                    }

                    if (item.Info.Status.Contains("Success") && item.Info.Status != null)
                    {
                        db.AddPhone(new PhoneList { Phone = item.Info.Sdt, Active = "Success", DateCount = dateCount.ToString() });
                        db.AddUA(new UAList { CheckPoint = "0", Success = "1", UA = item.Info.Ua, Notice = item.IsNote.ToString() });
                        db.AddIP(new IPInfo { CheckPoint = "0", Success = "1", IP = item.Info.Ip });

                    }
                    else
                    {
                        if (item.Info.Status != null)
                        {
                            db.AddPhone(new PhoneList { Phone = item.Info.Sdt, Active = item.Info.Status, DateCount = dateCount.ToString() });
                            db.AddUA(new UAList { CheckPoint = "1", Success = "0", UA = item.Info.Ua, Notice = item.IsNote.ToString() });
                            db.AddIP(new IPInfo { CheckPoint = "1", Success = "0", IP = item.Info.Ip });
                        }
                        else
                        {
                            db.AddPhone(new PhoneList { Phone = item.Info.Sdt, Active = "Không xác định", DateCount = dateCount.ToString() });
                            db.AddUA(new UAList { CheckPoint = "1", Success = "0", UA = item.Info.Ua, Notice = item.IsNote.ToString() });
                            db.AddIP(new IPInfo { CheckPoint = "1", Success = "0", IP = item.Info.Ip });
                        }

                    }
                }
                Debug.Print("Update list");
                UpdateListView();
                UpdateShow("Success: " + this.numSuccess);
                Debug.Print("Change IP");
                if (!changeIP())
                {
                    UpdateShow("Không change đươc IP");
                    return;
                }
                UpdateShow("Change xong IP");
                Debug.Print("Change xong IP");
            }
        }

        async Task<ChroniumReg> RegBrowseAsync(ChroniumReg chrome)
        {
            if (chrome.Browser != null)
            {
                await chrome.Browser.ClearCookiesAsync();
                await chrome.Browser.ClearPermissionsAsync();
                var Page = chrome.Browser.Pages[0];
                await Page.RouteAsync("**", (router, e) =>
                {
                    if (e.ResourceType == ResourceType.Image || e.ResourceType == ResourceType.Images || e.ResourceType == ResourceType.StyleSheet || e.ResourceType == ResourceType.Font || e.ResourceType == ResourceType.Media)
                        router.AbortAsync();
                    else
                        router.ContinueAsync();
                });

                try
                {
                    if (isCCmoiD)
                    {
                        var cookieadd = new List<SetNetworkCookieParam>
                                    {
                                        new SetNetworkCookieParam {Url="https://p.facebook.com/", Name = "datr", Value = chrome.Info.Cookiedatr},
                                        new SetNetworkCookieParam {Url="https://www.facebook.com/", Name = "datr", Value = chrome.Info.Cookiedatr},
                                    };
                        await chrome.Browser.AddCookiesAsync(cookieadd).ConfigureAwait(false);
                    }
                    else
                    {
                        var cookieadd = new List<SetNetworkCookieParam>
                                    {
                                        new SetNetworkCookieParam {Url="https://p.facebook.com/", Name = "fr", Value = chrome.Info.CookieFr},
                                        new SetNetworkCookieParam {Url="https://www.facebook.com/", Name = "fr", Value = chrome.Info.CookieFr},
                                    };
                        await chrome.Browser.AddCookiesAsync(cookieadd).ConfigureAwait(false);
                    }
                    
                }
                catch
                {
                    chrome.Info.Status = "Error set cc";
                    //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                    //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                    chrome.Dispose();
                    return chrome;
                }

                int count = 5;
                bool creation = false;
                while (count > 0 && !creation)
                {
                    try
                    {
                        creation = await Page.IsVisibleAsync("#signup-button", 1000).ConfigureAwait(false);
                    }
                    catch { }
                    count--;
                }


                if (count > 0)
                {
                    await Task.Delay(2000);
                    try
                    {
                        await Page.ClickAsync("#signup-button").ConfigureAwait(false);
                    }
                    catch
                    {
                        chrome.Info.Status = "Error net";
                        //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;
                    }
                    try
                    {
                        await Task.Delay(2000);
                        await Page.TypeAsync("//*[@name='lastname']", chrome.Info.Ho, delay: 150).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.TypeAsync("//*[@name='firstname']", chrome.Info.Ten, delay: 150).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#day", chrome.Info.Birth_day.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#month", chrome.Info.Birth_month.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#year", chrome.Info.Birth_year.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.TypeAsync("//*[@name='reg_email__']", chrome.Info.Sdt, delay: 150).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));

                        await Page.CheckAsync("#Nam").ConfigureAwait(false);

                        //await Page.CheckAsync("//input[@id='sex' and @value='"+sex+"']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));

                        await Page.TypeAsync("#password_step_input", chrome.Info.Pass, 150).ConfigureAwait(false);

                        await Task.Delay(7000);
                        int count_limit = 300;
                        bool check_v2 = true;
                        bool signup1 = false;
                        while (checkChrome > 0 && count_limit > 0)
                        {
                            try
                            {
                                signup1 = await Page.IsVisibleAsync("//*/button[@value='Đăng ký']", 100).ConfigureAwait(false);

                                if (signup1 && check_v2)
                                {
                                    check_v2 = false;
                                    checkChrome--;
                                }
                            }
                            catch { }
                            await Task.Delay(100);
                            count_limit--;
                            if (count_limit == 0)
                                checkChrome = 0;
                        }

                        //try
                        //{
                        //    await Page.ClickAsync("//*[@id='signup_button']", timeout: 1000);
                        //    await Page.WaitForLoadStateAsync(state: LifecycleEvent.Networkidle, 60000).ConfigureAwait(false);
                           
                        //}
                        //catch
                        //{
                        //    await Page.DblClickAsync("//button[@type='submit' and( @data-sigil='touchable multi_step_submit' or @value='Sign Up')]", timeout: 2000).ConfigureAwait(false);
                        //    await Page.WaitForLoadStateAsync(state: LifecycleEvent.Networkidle, 60000).ConfigureAwait(false);
                        //}
                        await Page.ClickAsync("//*/button[@value='Đăng ký']").ConfigureAwait(false);
                    }
                    catch
                    {
                        chrome.Info.Status = "Error input";
                        //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;
                    }
                    count = 30;
                    bool error = false;
                    bool checkpoint = false;
                    bool done = false;

                    while (count > 0 & !error & !checkpoint & !done)
                    {
                        string fullContent = await Page.GetContentAsync().ConfigureAwait(false);

                        string urlPage = Page.Url;

                        error = urlPage.Contains("error")||fullContent.Contains("Chúng tôi cần thêm thông tin");
                        checkpoint = urlPage.Contains("checkpoint");
                        done = urlPage.Contains("save-device")||fullContent.Contains("Đăng nhập bằng");
                        count--;
                        await Task.Delay(1000).ConfigureAwait(false);
                    }

                    if (count <= 0)
                    {
                        chrome.Info.Status = "Out Time";
                        //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;

                    }

                    if (error)
                    {
                        chrome.Info.Status = "Error";
                        //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;
                    }

                    if (checkpoint)
                    {

                        chrome.Info.Status = "CheckPoint";

                        //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;

                    }

                    if (done)
                    {

                        chrome.Info.Status = "Success";
                        var ak = (await chrome.Browser.GetCookiesAsync("https://facebook.com/").ConfigureAwait(false)).ToList();
                        List<string> values = new List<string>();
                        ak.ForEach(item =>
                        {
                            if (item.Name.Contains("datr") || item.Name.Contains("sb") || item.Name.Contains("c_user") || item.Name.Contains("xs"))
                                values.Add($"{item.Name}={item.Value}");
                        });
                        string result = string.Join(";", values);
                        string pattern = @"c_user=(\d+)";
                        string id = Regex.Match(result, pattern).Groups[1].Value.ToString();
                        chrome.Info.Uid = id;
                        chrome.Info.Cookie = result;

                        //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        numSuccess++;
                        return chrome;
                    }
                }
                chrome.Info.Status = "Error reg";
                //await chrome.Browser.CloseAsync().ConfigureAwait(false);
                //await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                chrome.Dispose();
                return chrome;
            }
            else
            {
                Debug.Print("Broser bị null trong hàm RegBrowseAsync " + chrome.Info.Ho.ToString());
            }
            chrome.Dispose();
            Debug.Print("Thoát hàm RegBrowseAsync " + chrome.Info.Ho.ToString());
            return chrome;
        }

        
        bool changeIP()
        {
            List<string> deviceList;
            for (int i = 0; i < 30; i++)
            {
                deviceList = NguyenHelper.GetDevices();
                if (deviceList.Count > 0)
                {
                    if (string.IsNullOrEmpty(this.deviceChose))
                    {
                        AirPlanMod(deviceList[0]);
                    }
                    else
                    {
                        AirPlanMod(this.deviceChose);
                    }

                    //changeEmulator(deviceList[0]);
                    return true;
                }
                UpdateShow("Không tìm thấy thiết bị - thoát sau " + (30 - i).ToString() + "s");
                Sleep(1);
            }
            return false;
        }
        void changeEmulator(string deviceChose)
        {
            NguyenHelper.ExecuteCMD(string.Format("adb -s {0} shell monkey -p com.device.emulator.pro -c android.intent.category.LAUNCHER 1", deviceChose));
            Sleep(1);
            NguyenHelper.clickElementXpath(deviceChose, "//node[@resource-id='com.device.emulator.pro:id/action_randomall']");

        }

        async Task<ChroniumReg> CreateBrowseAsync(ChroniumReg chromeItem, int x = 0, int y = 0, string v = null, int indexvalue = 0)
        {
            LaunchPersistentOptions options = new LaunchPersistentOptions
            {
                Headless = false,
                Args = new string[] {
                "--window-position="+x+","+y,
                "--app=https://p.facebook.com/",
                "--disable-notifications",
                "--blink-settings=imagesEnable=false",
                "--window-size=250,300",
                "--disable-extensions",
                "--disable-translate",
                "--disable-gpu"},
                ExecutablePath = pathBrowser,
                IgnoreDefaultArgs = new string[] {
                    "--enable-automation",
                    "--disable-infobars",
                },
                UserAgent = chromeItem.Info.Ua,
                IgnoreAllDefaultArgs = false,
                Timeout = 180000
            };

            chromeItem.Browser = null;
            chromeItem.Playwright = await Playwright.CreateAsync();

            int count = 5;
            do
            {
                try
                {
                    chromeItem.Browser = await chromeItem.Playwright.Chromium.LaunchPersistentContextAsync("", options);
                }
                catch
                {
                    if (chromeItem.Browser != null)
                    {
                        await chromeItem.Browser.CloseAsync().ConfigureAwait(false);
                        await chromeItem.Browser.DisposeAsync().ConfigureAwait(false);
                        chromeItem.DisposeBrowser();
                    }
                    chromeItem.Browser = null;
                }
                await Task.Delay(100);
                count--;
            } while (chromeItem.Browser == null && count > 0);
            return chromeItem;
        }
        void UpdateShow(string status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this.labShow.Content = status;
            }));
        }
        void GetCPUsage()
        {
            try
            {

                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process");


                foreach (ManagementObject queryObj in searcher.Get())
                {
                    Console.WriteLine("ProcessID: {0}", queryObj["IDProcess"]);
                    Console.WriteLine("Handles: {0}", queryObj["HandleCount"]);
                    Console.WriteLine("Threads: {0}", queryObj["ThreadCount"]);
                    Console.WriteLine("Memory: {0}", queryObj["WorkingSetPrivate"]);
                    Console.WriteLine("CPU%: {0}", queryObj["PercentProcessorTime"]);
                    Console.Read();
                }


            }
            catch (ManagementException e)
            {
                MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
            }

        }
        private void FakeBySSH()
        {
            try
            {
                using (var client = new SshClient("127.0.0.1", "root", "alpine"))
                {
                    client.Connect();
                    client.RunCommand("shortcuts://run-shortcut?name=uu");
                    client.RunCommand("uiopen shortcuts://run-shortcut?name=uu");
                    Thread.Sleep(2000);
                    Console.WriteLine(client.RunCommand("ifconfig"));
                    Console.WriteLine(client.RunCommand("dpkg –list"));
                    Console.WriteLine(client.RunCommand("cat /private/var/mobile/Library/Caches/com.apple.mobile.installation.plist"));
                    client.Disconnect();
                }
            }
            catch
            {
                // MessageBox.Show("3G error. Vui lòng bật ssh");
            }
        }
        void AirPlanMod(string deviceChose)
        {

            try
            {
                //showNotice(deviceChose, "Open fake IMEI app");
                NguyenHelper.ExecuteCMD(string.Format("adb -s {0} shell am start -a android.settings.AIRPLANE_MODE_SETTINGS", deviceChose));
            tatAir:
                var element = NguyenHelper.findElementByXpath(deviceChose, "//node[@resource-id='com.android.settings:id/switch_widget']");

                //Kiểm tra, nếu đã tắt thì bật airplane mod
                if (element == null)
                {
                    goto tatAir;
                }

                if (element.Attribute("text").ToString().Contains("TẮT"))
                {
                    var x = Regex.Matches(element.Attribute("bounds").Value, @"\[(.*?)\]", RegexOptions.Singleline);
                    Double[] vitri = new double[4];
                    if (x.Count > 0)
                    {
                        string[] y = x[0].Value.Split(',');
                        vitri[0] = double.Parse(y[0].Replace("[", ""));
                        vitri[1] = double.Parse(y[1].Replace("]", ""));
                        y = x[1].Value.Split(',');
                        vitri[2] = double.Parse(y[0].Replace("[", ""));
                        vitri[3] = double.Parse(y[1].Replace("]", ""));
                        Point vitritrungtam = new Point
                        {
                            X = Convert.ToInt32((vitri[0] + vitri[2]) / 2),
                            Y = Convert.ToInt32((vitri[1] + vitri[3]) / 2)
                        };
                        NguyenHelper.Tap(deviceChose, (int)vitritrungtam.X, (int)vitritrungtam.Y);
                    }
                }
                Sleep(3);
            batAir:
                element = NguyenHelper.findElementByXpath(deviceChose, "//node[@resource-id='com.android.settings:id/switch_widget']");
                //Kiểm tra, nếu đã bật thì tắt airplane mod
                // Đảm bảo sau khi thực hiện, đều có mạng
                if (element == null)
                {
                    goto batAir;
                }

                if (element.Attribute("text").ToString().Contains("Bật"))
                {
                    var x = Regex.Matches(element.Attribute("bounds").Value, @"\[(.*?)\]", RegexOptions.Singleline);
                    Double[] vitri = new double[4];
                    if (x.Count > 0)
                    {
                        string[] y = x[0].Value.Split(',');
                        vitri[0] = double.Parse(y[0].Replace("[", ""));
                        vitri[1] = double.Parse(y[1].Replace("]", ""));
                        y = x[1].Value.Split(',');
                        vitri[2] = double.Parse(y[0].Replace("[", ""));
                        vitri[3] = double.Parse(y[1].Replace("]", ""));
                        Point vitritrungtam = new Point
                        {
                            X = Convert.ToInt32((vitri[0] + vitri[2]) / 2),
                            Y = Convert.ToInt32((vitri[1] + vitri[3]) / 2)
                        };
                        NguyenHelper.Tap(deviceChose, (int)vitritrungtam.X, (int)vitritrungtam.Y);
                    }

                }

            checklai:
                element = NguyenHelper.findElementByXpath(deviceChose, "//node[@resource-id='com.android.settings:id/switch_widget']");
                //Kiểm tra, nếu đã bật thì tắt airplane mod
                // Đảm bảo sau khi thực hiện, đều có mạng
                if (element == null)
                {
                    goto checklai;
                }
                if (element.Attribute("text").ToString().Contains("Bật"))
                {

                    goto batAir;
                }
                //ADBHelper.ExecuteCMD("adb -s " + deviceChose + " shell input keyevent KEYCODE_HOME");
                return;
            }
            catch { }

        }
        void UpdateListView()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                lsvData.Items.Refresh();
            }));
        }

        void Sleep(int timeSleep)
        {
            Thread.Sleep(TimeSpan.FromSeconds(timeSleep));
        }
        private void TxbSoLuong_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                this.numThread = int.Parse(txbSoLuong.Text);
            }
            catch { this.numThread = 1; }

        }
        public void ChangeIPDcom(string mang, bool Dcom = false)
        {
            try
            {
                Process process = new Process();
                if (Dcom)
                {
                    process.StartInfo.FileName = "rasdial.exe";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.Arguments = "\"" + mang + "\"";
                    process.Start();
                    process.WaitForExit();
                }
                bool flag = !Dcom;
                if (flag)
                {
                    process.StartInfo.FileName = "rasdial.exe";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.Arguments = "\"" + mang + "\" /disconnect";
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch
            {
            }

        }
        private void BtnReg_Click(object sender, RoutedEventArgs e)
        {

            Thread ts = new Thread(CreatAcMulti_v2)
            {
                IsBackground = true
            };
            ts.Start();
        }

        private void TxbSoLuong_Copy_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                this.numTabSameTime = int.Parse(txbSoLuong_Copy.Text);
            }
            catch { this.numTabSameTime = 4; }
        }

        private void TxbSoLuong_Copy1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                this.deviceChose = txbSoLuong_Copy1.Text;
            }
            catch { }
        }

        private void Txb_pathCCmoi_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPathHo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPathCC_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPathTen_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ChbMoiD_Click(object sender, RoutedEventArgs e)
        {
            if (chbMoiD.IsChecked.Value)
            {
                this.isCCmoiD = true;
                chbMoiF.IsChecked = false;
            }  
        }

        private void ChbMoiF_Click(object sender, RoutedEventArgs e)
        {
            if (chbMoiF.IsChecked.Value)
            {
                this.isCCmoiD = false;
                chbMoiD.IsChecked = false;
            }
        }

        private void ChbMoiDF_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnPathBrowser_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
