using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace T2B.Strategies
{
	public class EmptyFolderDisplayStrategy : IBreakDisplayStrategy
	{
		private readonly string _videoFolderPath;

		public EmptyFolderDisplayStrategy(string videoFolderPath)
		{
			_videoFolderPath = videoFolderPath;
		}

		public Form CreateBreakForm(Screen screen, Func<TimeSpan> getRemaining, Func<string> getUserDisplayName)
		{
			var form = new Form
			{
				StartPosition = FormStartPosition.Manual,
				Bounds = screen.Bounds,
				FormBorderStyle = FormBorderStyle.None,
				TopMost = true,
				ShowInTaskbar = false,
				BackColor = Color.Black
			};

			var webView = new WebView2
			{
				Dock = DockStyle.Fill
			};
			form.Controls.Add(webView);

			var timer = new System.Windows.Forms.Timer { Interval = 1000 };

			form.Load += async (s, e) =>
			{
				try
				{
					var userDataFolder = Path.Combine(Path.GetTempPath(), "T2B_WebView2_Cache");
					var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
					await webView.EnsureCoreWebView2Async(env);

					webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
					webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

					string initialTime = getRemaining().ToString(@"mm\:ss");
					string userName = getUserDisplayName();
					string ipAddress = Settings.GetLocalIPAddress();
					string safeFolderPath = _videoFolderPath.Replace(@"\", @"\\");

					string html = $@"
<!DOCTYPE html>
<html lang='fa' dir='rtl'>
<head>
<meta charset='utf-8' />
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; overflow: hidden; }}
    body {{
        background-color: #050508;
        font-family: 'Segoe UI', Tahoma, sans-serif;
        display: flex; justify-content: center; align-items: center;
        height: 100vh; width: 100vw;
    }}
    .glow-container {{
        text-align: center;
        padding: 50px;
        border-radius: 24px;
        background: radial-gradient(circle, rgba(255,255,255,0.08) 0%, rgba(0,0,0,0) 75%);
        box-shadow: 0 0 60px rgba(255, 255, 255, 0.15);
        animation: pulseGlow 4s ease-in-out infinite alternate;
        max-width: 80%;
    }}
    @keyframes pulseGlow {{
        0% {{ box-shadow: 0 0 30px rgba(255, 255, 255, 0.1); }}
        100% {{ box-shadow: 0 0 80px rgba(255, 255, 255, 0.35); }}
    }}
    .timer {{
        font-size: 6rem; font-weight: 800; color: #ffffff;
        text-shadow: 0 0 25px #ffffff, 0 0 50px #cfd8dc;
        letter-spacing: 4px;
        direction: ltr;
    }}
    .user {{
        margin-top: 15px; font-size: 1.8rem; color: #00f3ff;
        text-shadow: 0 0 15px #00f3ff;
    }}
    .ip {{
        margin-top: 5px; font-size: 1.2rem; color: #00ffaa;
        direction: ltr; font-family: Consolas, monospace;
    }}
    .notice {{
        margin-top: 35px;
        font-size: 1.35rem;
        line-height: 2rem;
        color: #f1f1f1;
        background: rgba(255, 255, 255, 0.05);
        padding: 20px 30px;
        border: 1px dashed rgba(255, 255, 255, 0.3);
        border-radius: 12px;
        text-shadow: 0 0 10px rgba(255,255,255,0.5);
    }}
    .path-highlight {{
        color: #ffcc00;
        direction: ltr;
        display: inline-block;
        font-family: Consolas, monospace;
        font-size: 1.15rem;
        padding: 2px 8px;
        background: rgba(0,0,0,0.5);
        border-radius: 6px;
    }}
    .hint {{
        margin-top: 25px; font-size: 1.1rem; color: #888;
        direction: ltr;
    }}
    /* اسکرول آرام کپی‌رایت */
    .ticker-container {{
        position: fixed; bottom: 0; left: 0; width: 100vw; height: 42px;
        background: rgba(0,0,0,0.85);
        z-index: 10; overflow: hidden; pointer-events: none;
        display: flex; align-items: center;
    }}
    .ticker-content {{
        display: inline-block;
        white-space: nowrap;
        font-size: 1.1rem;
        font-weight: 600;
        color: #ffffff;
        text-shadow: 0 0 10px rgba(255,255,255,0.6);
        will-change: transform;
        animation: scrollLeftToRight 30s linear infinite;
    }}
    @keyframes scrollLeftToRight {{
        0%   {{ transform: translateX(-100%); }}
        100% {{ transform: translateX(100vw); }}
    }}
</style>
</head>
<body>
    <div class='glow-container'>
        <div id='clock' class='timer'>{initialTime}</div>
        <div class='user'>{userName}</div>
        <div class='ip'>IP: {ipAddress}</div>
        <div class='notice'>
            شما می‌توانید در فولدر <span class='path-highlight'>{safeFolderPath}</span> فیلم‌های مورد علاقه خود را کپی نمایید.
        </div>
        <div class='hint'>Press F2 to Resume</div>
    </div>
    <div class='ticker-container'>
        <div class='ticker-content'>© طراحی و توسعه: بهزاد فرتاش — نرم‌افزار مدیریت زمان استراحت BehzadRest</div>
    </div>
</body>
</html>";
					webView.NavigateToString(html);

					timer.Tick += (st, ev) =>
					{
						if (webView.CoreWebView2 != null)
						{
							var rem = getRemaining().ToString(@"mm\:ss");
							webView.ExecuteScriptAsync($"document.getElementById('clock').innerText = '{rem}';");
						}
					};
					timer.Start();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"WebView2 Init error: {ex.Message}");
				}
			};

			form.FormClosed += (s, e) =>
			{
				timer.Stop();
				timer.Dispose();
				webView.Dispose();
			};

			return form;
		}

		public void Dispose() { }
	}
}
