using System;
using PlaywrightSharp;
using PlaywrightSharp.Chromium;
using RegPlaywright.Model;

namespace RegPlaywright.Controller
{
    class ChroniumReg : IDisposable
    {
        public IPlaywright Playwright { get; set; }
        public IChromiumBrowser Browser { get; set; }
        public IPage Page { get; set; }
        public Info Info { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
