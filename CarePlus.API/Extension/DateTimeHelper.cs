namespace CarePlus.API.Extension
{
    public static class DateTimeHelper
    {
        public static DateTime MalaysiaNow()
        {
            TimeZoneInfo malaysiaTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                malaysiaTimeZone
            );
        }
    }
}
