using System;
using System.Windows.Forms;

namespace T2B
{
	public  sealed partial class SettingsForm : Form

	{
		private readonly Settings _original;
		private NumericUpDown _interval;
		private NumericUpDown _duration;
		private TextBox _videoPath;
		private Button _browse;
		private Button _ok;
		private Button _cancel;

		public Settings Result { get; private set; }

		public SettingsForm(Settings settings)
		{
			_original = settings;
			BuildUi();
			LoadValues();
		}

		private void BuildUi()
		{
			Text = "BehzadRest — تنظیمات";
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Width = 460;
			Height = 240;
			StartPosition = FormStartPosition.CenterScreen;

			var lblInterval = new Label { Text = "فاصلهٔ استراحت (دقیقه):", Left = 20, Top = 20, AutoSize = true };
			_interval = new NumericUpDown { Left = 200, Top = 18, Width = 120, Minimum = 1, Maximum = 240 };

			var lblDuration = new Label { Text = "مدت استراحت (دقیقه):", Left = 20, Top = 55, AutoSize = true };
			_duration = new NumericUpDown { Left = 200, Top = 53, Width = 120, Minimum = 1, Maximum = 60 };

			var lblVideo = new Label { Text = "پوشهٔ ویدیو:", Left = 20, Top = 90, AutoSize = true };
			_videoPath = new TextBox { Left = 20, Top = 110, Width = 300, ReadOnly = true };
			_browse = new Button { Text = "…", Left = 325, Top = 108, Width = 40 };

			_ok = new Button { Text = "ذخیره", DialogResult = DialogResult.OK, Left = 240, Top = 155, Width = 90 };
			_cancel = new Button { Text = "انصراف", DialogResult = DialogResult.Cancel, Left = 335, Top = 155, Width = 90 };

			_browse.Click += Browse_Click;
			_ok.Click += Ok_Click;
			CancelButton = _cancel;
			AcceptButton = _ok;

			Controls.AddRange(new Control[]
			{
				lblInterval, _interval,
				lblDuration, _duration,
				lblVideo, _videoPath, _browse,
				_ok, _cancel
			});
		}

		private void LoadValues()
		{
			if (_original is null) return;
			_interval.Value = Math.Max(1, _original.BreakInterval);
			_duration.Value = Math.Max(1, _original.BreakDuration);
			_videoPath.Text = _original.VideoFolderPath ?? string.Empty;
		}

		private void Browse_Click(object? sender, EventArgs e)
		{
			using var fbd = new FolderBrowserDialog
			{
				Description = "پوشهٔ فیلم‌های استراحت را انتخاب کنید",
				UseDescriptionForTitle = true,
				ShowNewFolderButton = true,
				SelectedPath = Directory.Exists(_videoPath.Text) ? _videoPath.Text
					: Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
			};

			if (fbd.ShowDialog(this) == DialogResult.OK)
				_videoPath.Text = fbd.SelectedPath;
		}

		private void Ok_Click(object? sender, EventArgs e)
		{
			Result = new Settings
			{
				BreakInterval = (int)_interval.Value,
				BreakDuration = (int)_duration.Value,
				VideoFolderPath = _videoPath.Text
			};
		}
	}
}
