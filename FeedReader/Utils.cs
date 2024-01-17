using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedReader
{
    public static class Utils
    {
        public static DateTime TimestampToDateTime(double timestamp)
        {
            DateTime datetime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            datetime = datetime.AddSeconds(timestamp);
            return datetime;
        }

        public static DateTime TimestampMsToDateTime(double timestamp)
        {
            DateTime datetime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            datetime = datetime.AddMilliseconds(timestamp);
            return datetime;
        }

        public static double DateTimeToTimestamp(DateTime datetime, bool ms = false)
        {
            DateTime startDatetime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            if (ms)
            {
                return (datetime - startDatetime).TotalMilliseconds;
            }
            else
            {
                return (datetime - startDatetime).TotalSeconds;
            }
        }

        public static DateTime StartOfWeek(DateTime dt, DayOfWeek firstDay = DayOfWeek.Monday)
        {
            int diff = (7 + (dt.DayOfWeek - firstDay)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static string BoolToIntStr(bool b)
        {
            return b ? "1" : "0";
        }
    }
}
