using System.Collections.Generic;

namespace T2B.Strategies
{
	public static class BreakStrategyFactory
	{
		/// <summary>
		/// ساخت استراتژی مناسب برای یک مانیتور مشخص با توزیع ویدیوها
		/// </summary>
		public static IBreakDisplayStrategy CreateStrategyForScreen(Settings settings, List<string> videos, int screenIndex)
		{
			// اگر هیچ فیلمی نبود، صفحه ویژه اطلاع‌رسانی مشکی/درخشان فعال شود
			if (videos == null || videos.Count == 0)
			{
				return new EmptyFolderDisplayStrategy(settings.VideoFolderPath);
			}

			// هر مانیتور یکی از فیلم‌ها را برمی‌دارد؛ در صورت کمبود ویدیو، چرخشی از فیلم اول استفاده می‌شود
			int targetIndex = screenIndex % videos.Count;
			string selectedVideo = videos[targetIndex];

			return new VideoOverlayDisplayStrategy(selectedVideo);
		}
	}
}
