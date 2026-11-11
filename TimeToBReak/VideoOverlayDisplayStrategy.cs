using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace T2B.Strategies
{
	public class VideoOverlayDisplayStrategy : IBreakDisplayStrategy
	{
		private readonly string _videoPath;

		public VideoOverlayDisplayStrategy(string videoPath)
		{
			_videoPath = videoPath;
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

					string videoDir = Path.GetDirectoryName(_videoPath)!;
					string videoFile = Path.GetFileName(_videoPath);
					webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
						"media.local", videoDir, CoreWebView2HostResourceAccessKind.Allow);

					string initialTime = getRemaining().ToString(@"mm\:ss");
					string userName = getUserDisplayName();
					string ipAddress = Settings.GetLocalIPAddress();

					string html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8' />
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; overflow: hidden; }}
    body {{ background-color: #000; font-family: 'Segoe UI', Tahoma, sans-serif; }}
    video {{
        position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
        object-fit: cover; z-index: 1; filter: brightness(0.75);
    }}
    .overlay {{
        position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
        z-index: 2; display: flex; flex-direction: column;
        justify-content: center; align-items: center; pointer-events: none;
        background: radial-gradient(circle, rgba(0,0,0,0.15) 0%, rgba(0,0,0,0.65) 100%);
    }}
    .timer {{
        font-size: 6.5rem; font-weight: 800; color: #00f3ff;
        text-shadow: 0 0 20px #00f3ff, 0 0 40px #0088ff;
        letter-spacing: 4px;
    }}
    .info-badge {{
        margin-top: 15px; font-size: 1.8rem; color: #ff007f;
        text-shadow: 0 0 15px #ff007f; font-weight: 600;
    }}
    .ip-badge {{
        margin-top: 8px; font-size: 1.2rem; color: #00ffaa;
        text-shadow: 0 0 10px #00ffaa; font-family: Consolas, monospace;
    }}
    .hint {{
        margin-top: 25px; font-size: 1.1rem; color: #ffffff;
        opacity: 0.8; text-shadow: 0 0 8px #000;
    }}
    /* نوار اسکرول متحرک در پایین‌ترین قسمت مانیتور بدون پرش */
    .ticker-container {{
        position: fixed; bottom: 0; left: 0; width: 100vw; height: 42px;
        background: linear-gradient(180deg, rgba(0,0,0,0) 0%, rgba(0,0,0,0.85) 100%);
        z-index: 10; overflow: hidden; pointer-events: none;
        display: flex; align-items: center;
    }}
    .ticker-content {{
        display: inline-block;
        white-space: nowrap;
        font-size: 1.1rem;
        font-weight: 600;
        color: #fff;
        text-shadow: 0 0 10px rgba(0,243,255,0.8);
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
    <video autoplay loop muted playsinline>
        <source src='https://media.local/{Uri.EscapeDataString(videoFile)}' type='video/mp4'>
    </video>
    <div class='overlay'>
        <div id='clock' class='timer'>{initialTime}</div>
        <div class='info-badge'>{userName}</div>
        <div class='ip-badge'>IP: {ipAddress}</div>
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
