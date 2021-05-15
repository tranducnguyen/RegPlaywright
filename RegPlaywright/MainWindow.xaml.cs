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
        int numThread;
        int checkChrome;
        int numSuccess;
        string pathBrowser = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        // @"C:\Program Files\Google\Chrome\Application\chrome.exe"
        public MainWindow()
        {
            InitializeComponent();
        }
        void CreatAcMulti()
        {

            List<Info> listInfo = new List<Info>();
            InfoController InfoController = new InfoController();

            numSuccess = 0;
            List<Thread> listThread = new List<Thread>();
            for (int k = 0; k < 5000; k++)
            {
                listInfo = InfoController.Create(numThread);
                Dispatcher.Invoke(new Action(() =>
                {
                    lsvData.ItemsSource = listInfo;
                    this.btnReg.Content = "Clicked";
                }));
                checkChrome = numThread;
                for (int i = 0; i < numThread; i++)
                {
                    //int x = 100 + ((i % 4) * 250);
                    //int y = (i / 4) * 300;
                    int x = 100;
                    int y = 0;
                    int index = i;

                    Thread t = new Thread(() =>
                    {
                        CreateAccAsync(listInfo[index], x, y, "", index);

                    })
                    {
                        IsBackground = true
                    };
                    t.Start();

                    listThread.Add(t);
                }

                foreach (Thread item in listThread)
                {
                    item.Join();
                }

                DbAction db = new DbAction();
                foreach (Info infoItem in listInfo)
                {
                    if (infoItem.Uid != null)
                    {
                        if (infoItem.Uid.Length > 0)
                        {
                            File.AppendAllText("ketqua.txt", infoItem.Uid + "|" + infoItem.Pass + "|" + infoItem.Cookie + "\n");
                        }
                    }

                    if (infoItem.Status.Contains("Success") && infoItem.Status != null)
                    {
                        db.AddPhone(new PhoneList { Phone = infoItem.Sdt, Active = "Success" });
                        db.AddUA(new UAList { CheckPoint = "0", Success = "1", UA = infoItem.Ua });
                        db.AddIP(new IPInfo { CheckPoint = "0", Success = "1", IP = infoItem.Ip });

                    }
                    else
                    {
                        db.AddPhone(new PhoneList { Phone = infoItem.Sdt, Active = "checkpoint" });
                        db.AddUA(new UAList { CheckPoint = "1", Success = "0", UA = infoItem.Ua });
                        db.AddIP(new IPInfo { CheckPoint = "1", Success = "0", IP = infoItem.Ip });
                    }
                }

                updateListView();
                updateShow("Success: " + this.numSuccess);
                if (!changeIP())
                {
                    updateShow("Không change đươc IP");
                    return;
                }
                updateShow("Change xong IP");
            }

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
                updateShow("Bắt đầu Success: " + this.numSuccess);
                checkChrome = numThread;
                List<ChroniumReg> listChrome = new List<ChroniumReg>();
                List<Info> listInfo = new List<Info>();
                listInfo = InfoController.Create(numThread);
                using var ctsAll = new CancellationTokenSource();
                ctsAll.CancelAfter(TimeSpan.FromMinutes(5));
                foreach(Info item in listInfo)
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

                await listChrome.ParallelForEachAsync(async (item) =>
                {
                    int vitri = index;
                    int x = 100 + ((vitri % 4) * 250);
                    int y = (vitri / 4) * 300;
                    index++;
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(150));
                    await Task.Delay(100);
                    await CreateBrowseAsync(item, cts.Token, x, y, "", vitri).ConfigureAwait(false);

                },
                maxDegreeOfParallelism: 4,
                ctsAll.Token);

                Debug.Print("Tổng số " + listChrome.Count.ToString());

                await listChrome.ParallelForEachAsync(async (item) =>
                {
                    await RegBrowseAsync(item).ConfigureAwait(false);

                }, maxDegreeOfParallelism: this.numThread);

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
                        db.AddPhone(new PhoneList { Phone = item.Info.Sdt, Active = "Success" ,DateCount = dateCount.ToString()});
                        db.AddUA(new UAList { CheckPoint = "0", Success = "1", UA = item.Info.Ua });
                        db.AddIP(new IPInfo { CheckPoint = "0", Success = "1", IP = item.Info.Ip });

                    }
                    else
                    {
                        db.AddPhone(new PhoneList { Phone = item.Info.Sdt, Active = "checkpoint", DateCount = dateCount.ToString()});
                        db.AddUA(new UAList { CheckPoint = "1", Success = "0", UA = item.Info.Ua });
                        db.AddIP(new IPInfo { CheckPoint = "1", Success = "0", IP = item.Info.Ip });
                    }
                }

                Debug.Print("Update list");
                updateListView();
                updateShow("Success: " + this.numSuccess);
                Debug.Print("Change IP");
                if (!changeIP())
                {
                    updateShow("Không change đươc IP");
                    return;
                }
                updateShow("Change xong IP");
                Debug.Print("Change xong IP");
            }
        }

        async Task<ChroniumReg> RegBrowseAsync(ChroniumReg chrome)
        {
            Debug.Print("Chạy hàm RegBrowseAsync " + chrome.Info.Ho.ToString());
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
                    var cookieadd = new List<SetNetworkCookieParam>
                                    {
                                        new SetNetworkCookieParam {Url="https://p.facebook.com/", Name = "datr", Value = chrome.Info.Cookiedatr},
                                        new SetNetworkCookieParam {Url="https://www.facebook.com/", Name = "datr", Value = chrome.Info.Cookiedatr},
                                    };
                    await chrome.Browser.AddCookiesAsync(cookieadd).ConfigureAwait(false);
                }
                catch
                {
                    chrome.Info.Status = "Error reg";
                    await chrome.Browser.CloseAsync().ConfigureAwait(false);
                    await chrome.Browser.DisposeAsync().ConfigureAwait(false);
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
                        await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;
                    }
                    try
                    {
                        await Task.Delay(1000);
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
                        await Page.ClickAsync("//*/button[@type='submit']");
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
                        while (checkChrome > 0 && count_limit > 0)
                        {
                            try
                            {
                                bool signup = await Page.IsVisibleAsync("//*/button[@value='Đăng ký']", 100);

                                if (signup && check_v2)
                                {
                                    check_v2 = false;
                                    checkChrome--;
                                }
                            }
                            catch { Debug.Print("Out dòng 315"); }
                            await Task.Delay(100);
                            count_limit--;
                            if (count_limit == 0)
                                checkChrome = 0;
                        }
                        await Page.ClickAsync("//*/button[@value='Đăng ký']");
                    }
                    catch
                    {
                        chrome.Info.Status = "Error input";
                        await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;
                    }
                    count = 30;
                    bool error = false;
                    bool checkpoint = false;
                    bool done = false;
                    while (count > 0 & !error & !checkpoint & !done)
                    {
                        error = Page.Url.Contains("error");
                        checkpoint = Page.Url.Contains("checkpoint");
                        done = Page.Url.Contains("save-device");
                        count--;
                        await Task.Delay(1000);
                    }

                    if (count <= 0)
                    {
                        chrome.Info.Status = "Out Time";
                        await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        return chrome;

                    }

                    if (error)
                    {
                        chrome.Info.Status = "Error";
                        await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        return chrome;
                    }

                    if (checkpoint)
                    {

                        chrome.Info.Status = "CheckPoint";

                        await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
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
                        chrome.Info.Datecreate = DateTime.UtcNow;

                        await chrome.Browser.CloseAsync().ConfigureAwait(false);
                        await chrome.Browser.DisposeAsync().ConfigureAwait(false);
                        chrome.Dispose();
                        numSuccess++;
                        return chrome;
                        //return infoItem;
                    }
                }
                chrome.Info.Status = "Error reg";

                await chrome.Browser.CloseAsync().ConfigureAwait(false);
                await chrome.Browser.DisposeAsync().ConfigureAwait(false);
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
                    airPlanMod(deviceList[0]);
                    //changeEmulator(deviceList[0]);
                    return true;
                }
                updateShow("Không tìm thấy thiết bị - thoát sau " + (30 - i).ToString() + "s");
                sleep(1);
            }
            return false;
        }
        void CreateAccAsync(Info infoItem, int x = 0, int y = 0, string proxy = null, int indexvalue = 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(150));

            RegByChrome(infoItem, x, y, "", indexvalue).Wait(cts.Token);


        }
        void changeEmulator(string deviceChose)
        {
            NguyenHelper.ExecuteCMD(string.Format("adb -s {0} shell monkey -p com.device.emulator.pro -c android.intent.category.LAUNCHER 1", deviceChose));
            sleep(1);
            NguyenHelper.clickElementXpath(deviceChose, "//node[@resource-id='com.device.emulator.pro:id/action_randomall']");

        }

        async Task<ChroniumReg> CreateBrowseAsync(ChroniumReg chromeItem, CancellationToken token, int x = 0, int y = 0, string v = null, int indexvalue = 0)
        {
            
            Debug.Print(indexvalue + " - vi tri " + x);
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
                IgnoreAllDefaultArgs = false
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
                    await chromeItem.Browser.CloseAsync().ConfigureAwait(false);
                    await chromeItem.Browser.DisposeAsync().ConfigureAwait(false);
                   

                    chromeItem.Browser = null;
                }
                await Task.Delay(100);
                count--;
            } while (chromeItem.Browser == null && count > 0);
            return chromeItem;
        }
        async Task<Info> RegByChrome(Info infoItem, int x = 0, int y = 0, string proxy = null, int indexvalue = 0)
        {
            Random rand = new Random();
            int sex = rand.Next(1, 2);

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
                UserAgent = infoItem.Ua,
                IgnoreAllDefaultArgs = false
            };

            using IPlaywright playwright = Playwright.CreateAsync().Result;
            {
                IChromiumBrowserContext browser = null;
                do
                {
                    try
                    {
                        browser = await playwright.Chromium.LaunchPersistentContextAsync("", options);
                    }
                    catch
                    {
                        await browser.CloseAsync();
                        await browser.DisposeAsync();
                        browser = null;
                    }
                    await Task.Delay(500);
                } while (browser == null);

                await browser.ClearCookiesAsync();
                await browser.ClearPermissionsAsync();

                var Page = browser.Pages[0];
                await Page.RouteAsync("**", (router, e) =>
                {
                    if (e.ResourceType == ResourceType.Image || e.ResourceType == ResourceType.Images || e.ResourceType == ResourceType.StyleSheet || e.ResourceType == ResourceType.Font || e.ResourceType == ResourceType.Media)
                        router.AbortAsync();
                    else
                        router.ContinueAsync();
                });

                try
                {
                    var cookieadd = new List<SetNetworkCookieParam>
                                    {
                                        new SetNetworkCookieParam {Url="https://p.facebook.com/", Name = "datr", Value = infoItem.Cookiedatr},
                                        new SetNetworkCookieParam {Url="https://www.facebook.com/", Name = "datr", Value = infoItem.Cookiedatr},
                                    };
                    await browser.AddCookiesAsync(cookieadd).ConfigureAwait(false);
                }
                catch
                {
                    infoItem.Status = "Error reg";
                    await browser.CloseAsync().ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                    //GC.Collect();
                    //GC.WaitForPendingFinalizers();
                    return infoItem;
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
                        infoItem.Status = "Error net";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        return infoItem;
                    }
                    try
                    {
                        await Task.Delay(1000);
                        await Page.TypeAsync("//*[@name='lastname']", infoItem.Ho, delay: new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.TypeAsync("//*[@name='firstname']", infoItem.Ten, delay: new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#day", infoItem.Birth_day.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#month", infoItem.Birth_month.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#year", infoItem.Birth_year.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']");
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.TypeAsync("//*[@name='reg_email__']", infoItem.Sdt, delay: new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        if (sex == 1)
                        {
                            await Page.CheckAsync("#Nam").ConfigureAwait(false);
                        }
                        else
                        {
                            await Page.CheckAsync("#Nữ").ConfigureAwait(false);
                        }
                        //await Page.CheckAsync("//input[@id='sex' and @value='"+sex+"']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        if (await Page.IsVisibleAsync("//*/button[@value='Tiếp']"))
                        {
                            infoItem.Status = "Error input";
                            await browser.CloseAsync();
                            await browser.DisposeAsync();
                            checkChrome--;
                            return infoItem;
                        }
                        await Page.TypeAsync("#password_step_input", infoItem.Pass, new Random().Next(150, 300)).ConfigureAwait(false);

                        await Task.Delay(7000);
                        int count_limit = 300;
                        bool check_v2 = true;
                        while (checkChrome > 0 && count_limit > 0)
                        {
                            try
                            {
                                bool signup = await Page.IsVisibleAsync("//*/button[@value='Đăng ký']", 100);

                                if (signup && check_v2)
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
                        await Page.ClickAsync("//*/button[@value='Đăng ký']");
                    }
                    catch
                    {
                        infoItem.Status = "Error input";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        return infoItem;
                    }
                    count = 30;
                    bool error = false;
                    bool checkpoint = false;
                    bool done = false;
                    while (count > 0 & !error & !checkpoint & !done)
                    {
                        error = Page.Url.Contains("error");
                        checkpoint = Page.Url.Contains("checkpoint");
                        done = Page.Url.Contains("save-device");
                        count--;
                        await Task.Delay(1000);
                    }

                    if (count <= 0)
                    {
                        infoItem.Status = "Out Time";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        return infoItem;

                    }

                    if (error)
                    {
                        infoItem.Status = "Error";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        return infoItem;
                    }

                    if (checkpoint)
                    {

                        infoItem.Status = "CheckPoint";

                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        return infoItem;

                    }

                    if (done)
                    {

                        infoItem.Status = "Success";
                        var ak = (await browser.GetCookiesAsync("https://facebook.com/").ConfigureAwait(false)).ToList();
                        List<string> values = new List<string>();
                        ak.ForEach(item =>
                        {
                            if (item.Name.Contains("datr") || item.Name.Contains("sb") || item.Name.Contains("c_user") || item.Name.Contains("xs"))
                                values.Add($"{item.Name}={item.Value}");
                        });
                        string result = string.Join(";", values);
                        string pattern = @"c_user=(\d+)";
                        string id = Regex.Match(result, pattern).Groups[1].Value.ToString();
                        infoItem.Uid = id;
                        infoItem.Cookie = result;
                        infoItem.Datecreate = DateTime.UtcNow;

                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        numSuccess++;
                        return infoItem;
                        //return infoItem;
                    }
                }
                infoItem.Status = "Error reg";
                await browser.CloseAsync().ConfigureAwait(false);
                await browser.DisposeAsync().ConfigureAwait(false);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                return infoItem;
            }
        }
        void test()
        {
            string deviceID = "xxx";
            string app = "test1";
            string link = "test2";
            string x = ("adb -s " + deviceID + $" shell am start -a android.intent.action.VIEW -d {app}://{link}");
            MessageBox.Show(x);
        }

        async Task<Info> RegByFirefox(Info infoItem, int x = 0, int y = 0, string proxy = null, int indexvalue = 0)
        {
            Random rand = new Random();
            int sex = rand.Next(1, 2);

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
                "--disable-gpu",
                "media.peerconnection.enabled=false"},
                ExecutablePath = pathBrowser,
                IgnoreDefaultArgs = new string[] {
                    "--enable-automation",
                    "--disable-infobars",
                },
                UserAgent = infoItem.Ua,
                IgnoreAllDefaultArgs = false
            };
            ;
            using IPlaywright playwright = Playwright.CreateAsync().Result;
            {
                IBrowserContext browser = null;
                do
                {
                    try
                    {
                        browser = await playwright.Firefox.LaunchPersistentContextAsync("", options);
                    }
                    catch
                    {
                        await browser.CloseAsync();
                        await browser.DisposeAsync();
                        browser = null;
                    }
                    await Task.Delay(500);
                } while (browser == null);
                await browser.ClearCookiesAsync();
                await browser.ClearPermissionsAsync();
                try
                {
                    var cookieadd = new List<SetNetworkCookieParam>
                {
                    new SetNetworkCookieParam {Url="https://p.facebook.com/", Name = "datr", Value = infoItem.Cookiedatr},
                    new SetNetworkCookieParam {Url="https://p.facebook.com/", Name = "fr", Value = infoItem.CookieFr}
                };
                    //                    new SetNetworkCookieParam {Domain=".facebook.com",Path="/", Name = "datr", Value = infoItem.Cookiedatr},
                    //new SetNetworkCookieParam { Domain = ".facebook.com", Path = "/", Name = "fr", Value = infoItem.CookieFr }
                    await browser.AddCookiesAsync(cookieadd);
                }
                catch
                {
                    infoItem.Status = "Error reg";
                    await browser.CloseAsync().ConfigureAwait(false);
                    await browser.DisposeAsync().ConfigureAwait(false);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    return infoItem;
                }
                var Page = browser.Pages[0];
                await Page.AddInitScriptAsync("<script>" +
                    "function initFingerprintJS() {" +
                    "FingerprintJS.load({ token: 'EFzvvALtV68gUDWLuzGg'})" +
                    ".then(fp => fp.get())" +
                    ".then(result => console.log(result.visitorId));" +
                    "}" +
                    " async " +
                    "src=\"https://cdn.jsdelivr.net/npm/@fingerprintjs/fingerprintjs-pro@3/dist/fp.min.js\"" +
                    " onload=\"initFingerprintJS()\"" +
                    " </script>");

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
                        infoItem.Status = "Error net";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        return infoItem;
                    }
                    try
                    {
                        await Task.Delay(1000);
                        await Page.TypeAsync("//*[@name='lastname']", infoItem.Ho, delay: new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.TypeAsync("//*[@name='firstname']", infoItem.Ten, delay: new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#day", infoItem.Birth_day.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#month", infoItem.Birth_month.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.SelectOptionAsync("#year", infoItem.Birth_year.ToString()).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']");
                        await Task.Delay(new Random().Next(1500, 2000));
                        await Page.TypeAsync("//*[@name='reg_email__']", infoItem.Sdt, delay: new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        if (sex == 1)
                        {
                            await Page.CheckAsync("#Nam").ConfigureAwait(false);
                        }
                        else
                        {
                            await Page.CheckAsync("#Nữ").ConfigureAwait(false);
                        }
                        //await Page.CheckAsync("//input[@id='sex' and @value='"+sex+"']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(3000, 4000));
                        await Page.ClickAsync("//*/button[@type='submit']").ConfigureAwait(false);
                        await Task.Delay(new Random().Next(1500, 2000));
                        if (await Page.IsVisibleAsync("//*/button[@value='Tiếp']"))
                        {
                            infoItem.Status = "Error input";
                            await browser.CloseAsync();
                            await browser.DisposeAsync();
                            checkChrome--;
                            return infoItem;
                        }
                        await Page.TypeAsync("#password_step_input", infoItem.Pass, new Random().Next(150, 300)).ConfigureAwait(false);
                        await Task.Delay(7000);
                        int count_limit = 300;
                        bool check_v2 = true;
                        while (checkChrome > 0 && count_limit > 0)
                        {
                            try
                            {
                                bool signup = await Page.IsVisibleAsync("//*/button[@value='Đăng ký']", 100);

                                if (signup && check_v2)
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
                        await Page.ClickAsync("//*/button[@value='Đăng ký']");
                    }
                    catch
                    {
                        infoItem.Status = "Error input";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        return infoItem;
                    }
                    count = 30;
                    bool error = false;
                    bool checkpoint = false;
                    bool done = false;
                    while (count > 0 & !error & !checkpoint & !done)
                    {
                        error = Page.Url.Contains("error");
                        checkpoint = Page.Url.Contains("checkpoint");
                        done = Page.Url.Contains("save-device");
                        count--;
                        await Task.Delay(1000);
                    }

                    if (count <= 0)
                    {
                        infoItem.Status = "Out Time";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        return infoItem;

                    }

                    if (error)
                    {
                        infoItem.Status = "Error";
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        return infoItem;
                    }

                    if (checkpoint)
                    {

                        infoItem.Status = "CheckPoint";

                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        return infoItem;

                    }

                    if (done)
                    {

                        infoItem.Status = "Success";
                        var ak = (await browser.GetCookiesAsync("https://facebook.com/").ConfigureAwait(false)).ToList();
                        List<string> values = new List<string>();
                        ak.ForEach(item =>
                        {
                            if (item.Name.Contains("datr") || item.Name.Contains("sb") || item.Name.Contains("c_user") || item.Name.Contains("xs"))
                                values.Add($"{item.Name}={item.Value}");
                        });
                        string result = string.Join(";", values);
                        string pattern = @"c_user=(\d+)";
                        string id = Regex.Match(result, pattern).Groups[1].Value.ToString();
                        infoItem.Uid = id;
                        infoItem.Cookie = result;
                        infoItem.Datecreate = DateTime.UtcNow;
                        await browser.CloseAsync().ConfigureAwait(false);
                        await browser.DisposeAsync().ConfigureAwait(false);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        numSuccess++;
                        return infoItem;
                        //return infoItem;
                    }
                }
                infoItem.Status = "Error reg";
                await browser.CloseAsync().ConfigureAwait(false);
                await browser.DisposeAsync().ConfigureAwait(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return infoItem;

            }
        }

        void UpdateShow(string status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this.labShow.Content = status;
            }));
        }
        void getCPUsage()
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
        private void fakeBySSH()
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
        void airPlanMod(string deviceChose)
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
                sleep(3);
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
        void updateShow(string value)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                labShow.Content = value;
            }));
        }
        void updateListView()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                lsvData.Items.Refresh();
            }));
        }

        void sleep(int timeSleep)
        {
            Thread.Sleep(TimeSpan.FromSeconds(timeSleep));
        }
        private void txbSoLuong_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                this.numThread = int.Parse(txbSoLuong.Text);
            }
            catch { this.numThread = 1; }

        }
        public void changeIPDcom(string mang, bool Dcom = false)
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
        private void btnReg_Click(object sender, RoutedEventArgs e)
        {

            Thread ts = new Thread(CreatAcMulti_v2)
            {
                IsBackground = true
            };
            ts.Start();
        }
    }
}
