using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Point = System.Drawing.Point;

namespace RegPlaywright.Controller
{
    static class NguyenHelper
    {
        private static string TAP_DEVICES = "adb -s {0} shell input tap {1} {2}";
        private static string ADB_FOLDER_PATH = "";
        private static string[] thongbao = new string[] { "//node[@text='Cho phép']", "//node[@text='CHO PHÉP']", "//node[@text='LÚC KHÁC']", "//node[@text='THỬ LẠI']", "//node[@resource-id='android:id/aerr_close']", "//node[@text='Không, tạo tài khoản mới']", "//node[@content-desc='Bỏ qua']", "//node[@text='BỎ QUA']" };
        private static string[] sLoi = new string[] { "//node[@text='Gần đây, số điện thoại bạn đang cố gắng xác minh đã được sử dụng để xác minh một tài khoản khác. Vui lòng thử số khác.']" };
        public static void uidump(string deviceChose)
        {
            string strCmd = string.Format("adb -s {0} shell uiautomator dump /data/local/tmp/uidump.xml", deviceChose);
            NguyenHelper.ExecuteCMD(strCmd);
            strCmd = string.Format("adb -s {0} pull /data/local/tmp/uidump.xml {1}", deviceChose, Directory.GetCurrentDirectory() + "\\" + deviceChose.Replace(".", "-").Replace(":", "-") + "dump.xml");
            NguyenHelper.ExecuteCMD(strCmd);
            return;
        }
        public static XDocument refeshDump(string deviceChose)
        {
            uidump(deviceChose);
            return loadDump(deviceChose);
        }
        public static XDocument loadDump(string deviceChose)
        {
            return XDocument.Load(Directory.GetCurrentDirectory() + "\\" + deviceChose.Replace(".", "-").Replace(":", "-") + "dump.xml");
        }
        public static void ChangeWMIC()
        {
            try
            {
                char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
                byte[] dataz = new byte[10];
                using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
                {
                    crypto.GetBytes(dataz);
                }
                StringBuilder resultp = new StringBuilder(10);
                foreach (byte b in dataz)
                {
                    resultp.Append(chars[b % (chars.Length)]);
                }
                ProcessStartInfo process = new ProcessStartInfo
                {
                    FileName = "WMIC.exe",
                    Arguments = "computersystem where caption='" + System.Environment.MachineName + "'rename " + resultp.ToString()
                };
                using (Process proc = Process.Start(process))
                {
                    proc.WaitForExit();
                    Console.WriteLine("Exit code = " + proc.ExitCode);
                }
            }
            catch { }
        }
        public static List<string> GetDevices()
        {
            List<string> list = new List<string>();
            string input = NguyenHelper.ExecuteCMD("adb devices");
            string pattern = "(?<=List of devices attached)([^\\n]*\\n+)+";
            MatchCollection matchCollection = Regex.Matches(input, pattern, RegexOptions.Singleline);
            bool flag = matchCollection.Count > 0;
            if (flag)
            {
                string value = matchCollection[0].Groups[0].Value;
                string[] array = Regex.Split(value, "\r\n");
                string[] array2 = array;
                int i = 0;
                while (i < array2.Length)
                {
                    string text = array2[i];
                    bool flag2 = !string.IsNullOrEmpty(text) && text != " ";
                    if (flag2)
                    {
                        string[] array3 = text.Trim().Split(new char[]
                        {
                    '\t'
                        });
                        string text2 = array3[0];
                        try
                        {
                            string a = array3[1];
                            bool flag3 = a != "device";
                            if (flag3)
                            {
                                goto IL_EA;
                            }
                        }
                        catch
                        {
                        }
                        list.Add(text2.Trim());
                    }
                IL_EA:
                    i++;
                    continue;
                }
            }
            return list;
        }
        static bool checkImageAndClickXpath(string deviceChose, string Xpathelement, int timeLoop = 5)
        {
            while (timeLoop > 0)
            {
                var pointAnh = vitriElementByxPath(deviceChose, Xpathelement);
                if (pointAnh != null)
                {
                    NguyenHelper.Tap(deviceChose, (int)pointAnh.Value.X, (int)pointAnh.Value.Y);
                    return true;
                }
                timeLoop--;
            }
            return false;
        }

        public static Point? vitriElementByxPath(string deviceChose, string Xpathelement, bool isAllowThongBao = true, XDocument xmlDump = null)
        {
            if (xmlDump == null)
            {
                xmlDump = refeshDump(deviceChose);
            }

            if (isAllowThongBao)
            {
                allow_thongbao(deviceChose, 2, xmlDump);
            }

            Point? ketqua;
            try
            {
                var nodes = xmlDump.XPathSelectElement(Xpathelement);
                if (nodes != null)
                {
                    var x = Regex.Matches(nodes.Attribute("bounds").Value, @"\[(.*?)\]", RegexOptions.Singleline);
                    Double[] vitri = new double[4];
                    if (x.Count > 0)
                    {
                        string[] y = x[0].Value.Split(',');
                        vitri[0] = double.Parse(y[0].Replace("[", ""));
                        vitri[1] = double.Parse(y[1].Replace("]", ""));
                        y = x[1].Value.Split(',');
                        vitri[2] = double.Parse(y[0].Replace("[", ""));
                        vitri[3] = double.Parse(y[1].Replace("]", ""));
                        Point vitritrungtam = new Point();
                        vitritrungtam.X = Convert.ToInt32((vitri[0] + vitri[2]) / 2);
                        vitritrungtam.Y = Convert.ToInt32((vitri[1] + vitri[3]) / 2);
                        ketqua = new Point?(vitritrungtam);

                    }
                    else
                    {
                        ketqua = null;
                    }
                }
                else
                {
                    ketqua = null;
                }

            }
            catch (Exception)
            {

                ketqua = null;
            }

            return ketqua;
        }

        public static XElement findElementByXpath(string deviceChose, string Xpathelement, int solanLap = 5, bool isAlowThongBao = true, XDocument xmlDoc = null)
        {
            XElement nodes;
            while (solanLap > 0)
            {
                if (xmlDoc == null)
                {
                    xmlDoc = refeshDump(deviceChose);
                }


                if (isAlowThongBao)
                {
                    allow_thongbao(deviceChose, 2, xmlDoc);
                }


                try
                {
                    nodes = xmlDoc.XPathSelectElement(Xpathelement);
                    if (nodes != null)
                    {
                        return nodes;
                    }

                }
                catch (Exception)
                {
                }
                solanLap--;

            }
            return null;
        }

        public static XElement findElementByXpaths(string deviceChose, string[] Xpathelement, int solanLap = 5, bool isAlowThongBao = true, XDocument xmlDoc = null)
        {
            XElement nodes;
            while (solanLap > 0)
            {
                if (xmlDoc == null)
                {
                    xmlDoc = refeshDump(deviceChose);
                }


                if (isAlowThongBao)
                {
                    allow_thongbao(deviceChose, 2, xmlDoc);
                }


                try
                {
                    foreach (string item in Xpathelement)
                    {
                        nodes = xmlDoc.XPathSelectElement(item);
                        if (nodes != null)
                        {
                            return nodes;
                        }
                    }
                }
                catch (Exception)
                {
                }
                solanLap--;

            }
            return null;
        }

        public static void allow_thongbao(string deviceChose, int solanLap = 5, XDocument xmlDoc = null)
        {
            if (xmlDoc == null)
            {
                xmlDoc = refeshDump(deviceChose);
            }
            while (solanLap > 0)
            {

                foreach (string itemXpath in thongbao)
                {
                    clickElementXpath(deviceChose, itemXpath, xmlDoc);
                }


                //Hiện thông báo của google
                var node_allow = xmlDoc.XPathSelectElement("//node[@text='Nhớ số điện thoại và mật khẩu của bạn']");
                if (node_allow != null)
                {
                    TapByPercent(deviceChose, 49.8, 67.3);
                    return;
                }
                node_allow = xmlDoc.XPathSelectElement("//node[@text='Chọn tên của bạn']");
                if (node_allow != null)
                {
                    bool isClick = clickElementXpath(deviceChose, "//node[@class='android.widget.RadioButton'][1]", xmlDoc);
                    if (isClick)
                    {
                        clickElementXpath(deviceChose, "//node[@text='Tiếp']", xmlDoc);
                    }
                }
                solanLap--;
            }
            return;
        }

        public static bool not_allow_thongbao(string deviceChose, int solanLap = 5, XDocument xmlDoc = null)
        {
            if (xmlDoc == null)
            {
                xmlDoc = refeshDump(deviceChose);
            }
            XElement nodes;
            while (solanLap > 0)
            {
                foreach (string itemXpath in sLoi)
                {
                    try
                    {
                        nodes = xmlDoc.XPathSelectElement(itemXpath);
                        if (nodes != null)
                        {
                            return true;
                        }

                    }
                    catch (Exception)
                    {
                    }
                }
                solanLap--;
            }
            return false;
        }
        public static bool clickElementXpaths(string deviceChose, string[] xPathElement, XDocument xml = null, int solanLap = 5)
        {
            while (solanLap > 0)
            {
                if (xml == null)
                {
                    xml = refeshDump(deviceChose);
                }
                foreach (string item in xPathElement)
                {
                    var node_allow1 = xml.XPathSelectElement(item);
                    if (node_allow1 != null)
                    {
                        var x = Regex.Matches(node_allow1.Attribute("bounds").Value, @"\[(.*?)\]", RegexOptions.Singleline);
                        Double[] vitri = new double[4];
                        if (x.Count > 0)
                        {
                            string[] y = x[0].Value.Split(',');
                            vitri[0] = double.Parse(y[0].Replace("[", ""));
                            vitri[1] = double.Parse(y[1].Replace("]", ""));
                            y = x[1].Value.Split(',');
                            vitri[2] = double.Parse(y[0].Replace("[", ""));
                            vitri[3] = double.Parse(y[1].Replace("]", ""));
                            Point vitritrungtam = new Point();
                            vitritrungtam.X = Convert.ToInt32((vitri[0] + vitri[2]) / 2);
                            vitritrungtam.Y = Convert.ToInt32((vitri[1] + vitri[3]) / 2);
                            NguyenHelper.Tap(deviceChose, (int)vitritrungtam.X, (int)vitritrungtam.Y);
                            return true;
                        }
                    }
                }
                solanLap--;
            }
            return false;
        }

        public static bool clickElementXpath(string deviceChose, string xPathElement, XDocument xml = null, int solanLap = 5)
        {
            while (solanLap > 0)
            {
                if (xml == null)
                {
                    xml = refeshDump(deviceChose);
                }

                var node_allow1 = xml.XPathSelectElement(xPathElement);
                if (node_allow1 != null)
                {
                    var x = Regex.Matches(node_allow1.Attribute("bounds").Value, @"\[(.*?)\]", RegexOptions.Singleline);
                    Double[] vitri = new double[4];
                    if (x.Count > 0)
                    {
                        string[] y = x[0].Value.Split(',');
                        vitri[0] = double.Parse(y[0].Replace("[", ""));
                        vitri[1] = double.Parse(y[1].Replace("]", ""));
                        y = x[1].Value.Split(',');
                        vitri[2] = double.Parse(y[0].Replace("[", ""));
                        vitri[3] = double.Parse(y[1].Replace("]", ""));
                        Point vitritrungtam = new Point();
                        vitritrungtam.X = Convert.ToInt32((vitri[0] + vitri[2]) / 2);
                        vitritrungtam.Y = Convert.ToInt32((vitri[1] + vitri[3]) / 2);
                        NguyenHelper.Tap(deviceChose, (int)vitritrungtam.X, (int)vitritrungtam.Y);
                        return true;
                    }
                }
                solanLap--;
            }
            return false;
        }
        public static void TapByPercent(string deviceID, double x, double y, int count = 1)
        {
            Point screenResolution = GetScreenResolution(deviceID);
            int num = (int)(x * ((double)screenResolution.X * 1.0 / 100.0));
            int num2 = (int)(y * ((double)screenResolution.Y * 1.0 / 100.0));

            NguyenHelper.Tap(deviceID, num, num2);
        }
        public static Point GetScreenResolution(string deviceID)
        {
            string cmdCommand = string.Format("adb -s {0} shell wm size", deviceID);
            string text = NguyenHelper.ExecuteCMD(cmdCommand);
            string pattern = "(?<=Override size: )(.*?)\n";
            MatchCollection matchCollection = Regex.Matches(text, pattern, RegexOptions.Singleline);
            bool flag = matchCollection.Count > 0;
            if (flag)
            {
                string value = matchCollection[0].Groups[0].Value;
                value = value.Replace("\r\n", "");
                string[] array = value.Split(new char[]
                    {
                       'x'
                    });
                int x = Convert.ToInt32(array[0].Trim());
                int y = Convert.ToInt32(array[1].Trim());
                return new Point(x, y);

            }
            return new Point();
        }
        public static string ExecuteCMD(string cmdCommand)
        {
            string result;
            try
            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = NguyenHelper.ADB_FOLDER_PATH,
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    Verb = "runas"
                };
                process.Start();
                process.StandardInput.WriteLine(cmdCommand);
                process.StandardInput.Flush();
                process.StandardInput.Close();
                process.WaitForExit();
                string text = process.StandardOutput.ReadToEnd();
                result = text;
            }
            catch
            {
                result = null;
            }
            return result;
        }
        public static string GetIP()
        {
            using WebClient client = new WebClient();
            try
            {
                string html = client.DownloadString("http://api.ipify.org/?format=json");
                String match = Regex.Match(html, @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b").Groups[0].ToString();
                return match;
            }
            catch { return null; }

        }
        public static bool GetPercentIP()
        {
            using WebClient client = new WebClient();
            string html = client.DownloadString("https://whoer.net/");
            Match match = Regex.Match(html, @"dsbl : ([^\n]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (match.Groups[1].Value.ToString() != "0")
                {
                    return true;
                }
            }
            return false;
        }
        public static string RunCMD(string cmd)
        {
            Process cmdProcess;
            cmdProcess = new Process();
            cmdProcess.StartInfo.FileName = "cmd.exe";
            cmdProcess.StartInfo.Arguments = "/c " + cmd;
            cmdProcess.StartInfo.RedirectStandardOutput = true;
            cmdProcess.StartInfo.UseShellExecute = false;
            cmdProcess.StartInfo.CreateNoWindow = true;
            cmdProcess.Start();
            string output = cmdProcess.StandardOutput.ReadToEnd();
            cmdProcess.WaitForExit();
            if (String.IsNullOrEmpty(output))
                return "";
            return output;
        }
        public static void Tap(string deviceID, int x, int y, int count = 1)
        {
            string text = string.Format(NguyenHelper.TAP_DEVICES, deviceID, x, y);
            for (int i = 1; i < count; i++)
            {
                text = text + " && " + string.Format(NguyenHelper.TAP_DEVICES, deviceID, x, y);
            }
            NguyenHelper.ExecuteCMD(text);
        }
        
    }
}
