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
        public int Index { get; set; }
        public async void Dispose()
        {
            await Browser.CloseAsync();
            await Browser.DisposeAsync();
        }

        public static implicit operator Task<object>(ChroniumReg v)
        {
            throw new NotImplementedException();
        }
    }
}
