using System;
using System.Globalization;

namespace T2B.Services
{
    public static class PersianDateService
    {
        public static string NowPersianDateString()
        {
            var pc = new PersianCalendar();
            var now = DateTime.Now;

            int y = pc.GetYear(now);
            int m = pc.GetMonth(now);
            int d = pc.GetDayOfMonth(now);

            return string.Format("{0:0000}/{1:00}/{2:00}", y, m, d);
        }
    }
}
