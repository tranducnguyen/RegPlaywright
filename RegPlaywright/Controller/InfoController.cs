using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using RegPlaywright.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RegPlaywright.Controller
{
    class InfoController
    {
        private const string pathTen = "ten.txt";
        private const string pathHo = "ho.txt";
        private const string pathCookie = "cookie.txt"; // File Cookie
        private const string pathUAMobile = "user-agents_chrome_iphone_10000.txt"; //File user agent


        public List<Info> Create(int soluong)
        {
            List<Info> listInfo = new List<Info>();
            string[] arrTen = File.ReadAllLines(pathTen);
            string[] arrHo = File.ReadAllLines(pathHo);
            string[] arrCookie = File.ReadAllLines(pathCookie);
            string[] arrUAMobile = File.ReadAllLines(pathUAMobile);
            int indexRandom = 0;
            string sUA = "";
            string sIP = NguyenHelper.GetIP();
            List<String> listCookie = new List<string>(arrCookie);
            Dictionary<String, String> dicCookie = new Dictionary<string, string>();
            string cookieMoidatr = "";
            string cookieMoiFr = "";
            var randomSdt_Vi = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"+84(968|965|986|987|966|977|903|368|366|981|983|976|967)[0-9]{6}" });
            var randomSdt_Us = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"+1(541|704)[0-9]{7}" });
            //var randomSdt_Us = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"+1(209|213|279|310|323|408|415|424|442|510|530|559|562)[0-9]{7}" });
            var randomPass = RandomizerFactory.GetRandomizer(new FieldOptionsTextRegex { Pattern = @"[a-zA-Z0-9]{6}" });
            
            for (int i = 0; i < soluong; i++)
            {
                Info info = new Info
                {
                    Pass = randomPass.Generate(),

                    Ho = arrHo[new Random().Next(arrHo.Length)],
                    Ten = arrTen[new Random().Next(arrTen.Length)]
                };

            layUA:
                sUA = arrUAMobile[new Random().Next(arrUAMobile.Length)];
                if (!dicCookie.ContainsKey(sUA))
                {
                    dicCookie.Add(sUA, i.ToString());
                    info.Ua = sUA.Trim();
                    
                }
                else
                {
                    goto layUA;
                }

                info.Ip = sIP;
                string phone = "";
                bool check = true;
                while (check)
                {
                    phone = randomSdt_Vi.Generate();
                    check = new DbAction().CheckPhone(phone);
                }
                info.Sdt = phone;
                info.Birth_day = new Random().Next(1,28).ToString();
                info.Birth_month = new Random().Next(1,12).ToString();
                info.Birth_year = new Random().Next(1995,1999).ToString();

            layCookie:
                indexRandom = new Random().Next(listCookie.Count);

                cookieMoidatr = Regex.Match(listCookie[indexRandom].Trim().Replace(" ", ""), "datr=(.*?);", RegexOptions.Singleline).Groups[0].ToString().Replace("datr=", "").Replace(";", "").Trim();
                cookieMoiFr = Regex.Match(listCookie[indexRandom].Trim().Replace(" ", ""), "fr=(.*?);", RegexOptions.Singleline).Groups[0].ToString().Replace("fr=", "").Replace(";", "").Trim();

                if (string.IsNullOrWhiteSpace(cookieMoidatr))
                {
                    goto layCookie;
                }
                if (!dicCookie.ContainsKey(cookieMoidatr))
                {
                    dicCookie.Add(cookieMoidatr, i.ToString());
                    info.Cookiedatr = cookieMoidatr;
                    info.CookieFr = cookieMoiFr;
                    info.Cookiemoi = listCookie[indexRandom];
                }
                else
                {
                    goto layCookie;
                }
                listCookie.RemoveAt(indexRandom);
                listInfo.Add(info);
            }
            File.WriteAllLines(pathCookie, listCookie.ToArray());//Ghi đè lên file cũ
            return listInfo;
        }
    }
}
