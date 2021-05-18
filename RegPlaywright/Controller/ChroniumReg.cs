using System;
using System.Threading.Tasks;
using PlaywrightSharp;
using PlaywrightSharp.Chromium;
using RegPlaywright.Model;

namespace RegPlaywright.Controller
{
    class ChroniumReg : IDisposable
    {
        public IPlaywright Playwright { get; set; }
        public IChromiumBrowserContext Browser { get; set; }
        public IPage Page { get; set; }
        public Info Info { get; set; }

        public bool IsNote { get; set; }
        public int Index { get; set; }

        private bool CheckError(string fullcontten)
        {
            bool error = false;

            error = fullcontten.Contains("We Need More Information") || fullcontten.Contains("Chúng tôi cần thêm thông tin");
            if (error)
            {
                new DbAction().AddPhone(new PhoneList { Phone = Info.Sdt, Active = "checkpoint" });
                return true;
            }
            error = fullcontten.Contains("Please enter a valid") || fullcontten.Contains("有効な") || fullcontten.Contains("Vui lòng nhập số điện thoại hợp lệ.");
            if (error)
            {
                new DbAction().AddPhone(new PhoneList { Phone = Info.Sdt, Active = "block" });
                return true;
            }
            error = fullcontten.Contains("Registration Error") || fullcontten.Contains("Lỗi đăng ký") || fullcontten.Contains("Confirm your name") || fullcontten.Contains("We require everyone to use the name") || fullcontten.Contains("Chúng tôi yêu cầu mọi người sử dụng tên họ dùng ") || fullcontten.Contains("登録エラー") || fullcontten.Contains("実名を入力してください") || fullcontten.Contains("リクエストを処理できませんでした");
            if (error)
                return true;

            error = Page.Url.Contains("checkpoint");
            if (error)
            {
                new DbAction().AddPhone(new PhoneList { Phone = Info.Sdt, Active = "checkpoint" });
                return true;
            }
            error = Page.Url.Contains("error");
            if (error)
            {
                return true;
            }
            return false;
        }
        private bool CheckComplete(string fullcontten)
        {
            bool Done = false;

            Done = fullcontten.Contains("Log In With One Tap") ||/* fullcontten.Contains("FB-") ||*/ fullcontten.Contains("Save your pass") || fullcontten.Contains("Đăng nhập bằng") || fullcontten.Contains("ワンタップでログイン");
            if (Done)
                return true;
            string url = Page.Url;
            Done = url.Contains("login/save-device") || url.Contains("confirmemail");
            if (Done)
                return true;
            return false;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.Collect();
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Page?.CloseAsync();
                Browser?.CloseAsync();
                Browser = Playwright?.Chromium.LaunchAsync().Result.NewContextAsync().Result;
                Browser?.CloseAsync();
                Browser?.DisposeAsync();
                Playwright?.Dispose();
            }
        }
        public void DisposeBrowser()
        {
            DisposeBrowser(true);
            GC.Collect();
            GC.SuppressFinalize(this);
        }
        private void DisposeBrowser(bool disposing)
        {
            if (disposing)
            {
                Browser?.CloseAsync();
                Browser = Playwright?.Chromium.LaunchAsync().Result.NewContextAsync().Result;
                Browser?.CloseAsync();
                Browser?.DisposeAsync();
            }
        }
    }
}
