using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mcps.Samples;

[McpServerToolType]
public static class McpDate
{
    [McpServerTool(Name = "get_current_time"), Description("Gets current time based on iso culture, format and timezone")]
    public static string get_current_time([Description("User ISO culture used for dates. eg: es-ES")]string culture,
                                          [Description("Date time string output format. Can use specific C# 12 string date formats. eg: s")] string format,
                                          [Description("Selected time zone to parse UTC to local date. eg: Europe/Madrid")]string timeZone)
    {
        var utcNow = DateTime.UtcNow;
        try
        {
            var cultureInfo = new CultureInfo(culture);
            var currentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, currentTimeZone);
            return localTime.ToString(format, cultureInfo);
        }
        catch(CultureNotFoundException)
        {
            Console.WriteLine($"Error: Culture '{culture}' not found. Using default 'es-ES'.");
            return utcNow.ToString(format, new CultureInfo("es-ES"));

        }
        catch(TimeZoneNotFoundException)
        {
            Console.WriteLine($"Error: Time zone '{timeZone}' not found. Using server local time.");
            return DateTime.Now.ToString(format, new CultureInfo(culture));
        }
        catch(FormatException)
        {
            Console.WriteLine($"Error: Format string '{format}' is invalid. Using default 'O'.");
            return utcNow.ToString("O", new CultureInfo(culture));
        }
    }
}
