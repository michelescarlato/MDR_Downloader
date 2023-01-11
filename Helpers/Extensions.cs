using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System.Text.RegularExpressions;


namespace MDR_Downloader.Helpers;


public static class StringExtensions
{
    public static string? Tidy(this string? instring)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise trims the 
        // string

        if (instring is null || instring == "NULL" || instring == "null"
                             || instring == "\"NULL\"" || instring == "\"null\"")
        {
            return null;
        }
        else
        {
            if (!instring.StartsWith('"'))
            {
                // some strings will have start and end quotes
                // a start quote should indicate leaving both

                char[] chars1 = { ' ', ';' };
                instring = instring.Trim(chars1);
            }
            else
            {
                char[] chars2 = { '"', ' ', ';' };
                instring = instring.Trim(chars2);
            }

            return instring == "" ? null : instring;
        }
    }


    public static string? ReplaceUnicodes(this string? instring)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise thrims the 
        // string

        if (instring is null || instring == "NULL" || instring == "null"
                             || instring == "\"NULL\"" || instring == "\"null\""
                             || instring.Trim() == "")
        {
            return null;
        }
        else
        {
            instring = instring.Replace("&#32;", " ").Replace("&#37;", "%");
            instring = instring.Replace("&#39;", "’").Replace("&quot;", "'");
            instring = instring.Replace("#gt;", ">").Replace("#lt;", "<");
            return instring;
        }
    }


    public static string? ReplacHtmlTags(this string? instring)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise thrims the 
        // string

        if (instring is null || instring == "NULL" || instring == "null"
                             || instring == "\"NULL\"" || instring == "\"null\""
                             || instring.Trim() == "")
        {
            return null;
        }
        else
        {
            instring = instring.Replace("<br>", "\n");
            instring = instring.Replace("<br/>", "\n");
            instring = instring.Replace("<br />", "\n");
            instring = instring.Replace("\n\n", "\n").Replace("\n \n", "\n");

            return instring;
        }
    }

    public static string? CompressSpaces(this string? instring)
    {
        if (string.IsNullOrEmpty(instring))
        {
            return null;
        }
        else
        {
            string outstring = instring.Trim();

            outstring = outstring.Replace("\r\n", "\n");    // regularise endings
            outstring = outstring.Replace("\r", "\n");

            while (outstring.Contains("  "))
            {
                outstring = outstring.Replace("  ", " ");
            }
            outstring = outstring.Replace("\n:\n", ":\n");
            outstring = outstring.Replace("\n ", "\n");
            while (outstring.Contains("\n\n"))
            {
                outstring = outstring.Replace("\n\n", "\n");
            }
            outstring = outstring.TrimEnd('\n');
            return outstring;
        }
    }


    public static string? ReplacNBSpaces(this string? instring)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise thrims the 
        // string
        if (string.IsNullOrEmpty(instring))
        {
            return null;
        }
        else
        {
            instring = instring.Replace('\u00A0', ' ');
            instring = instring.Replace('\u2000', ' ').Replace('\u2001', ' ');
            instring = instring.Replace('\u2002', ' ').Replace('\u2003', ' ');
            instring = instring.Replace('\u2007', ' ').Replace('\u2008', ' ');
            instring = instring.Replace('\u2009', ' ').Replace('\u200A', ' ');

            return instring;
        }
    }
}


public static class ScrapingExtensions
{
    public static string InnerValue(this HtmlNode node)
    {
        string allInner = node.InnerText?.Replace("\n", "")?.Replace("\r", "")?.Trim() ?? "";
        string label = node.CssSelect(".label").FirstOrDefault()?.InnerText?.Trim() ?? "";
        return allInner[(label.Length)..]?.Trim() ?? "";
    }


    public static string TrimmedContents(this HtmlNode node)
    {
        return node.InnerText?.Replace("\n", "")?.Replace("\r", "")?.Trim() ?? "";
    }


    public static string TrimmedLabel(this HtmlNode node)
    {
        return node.CssSelect(".label").FirstOrDefault()?.InnerText?.Trim() ?? "";
    }


    public static string RemoveLabelAndSupp(this string attribute_value,
                                   string attribute_name, string? suppText)
    {
        // drop the attribute name from the text
        string attValue = attribute_value.Replace(attribute_name, "");

        // also drop any supplementary entry title
        if (suppText is not null)
        {
            attValue = attValue.Replace(suppText, "");
        }

        // drop carriage returns and trim 
        return attValue.Replace("\n", "").Replace("\r", "").Trim();
    }

}


public static partial class DateExtensions
{
    public static int GetMonthAsInt(this string month_name)
    {
        try
        {
            return (int)Enum.Parse<MonthsFull>(month_name);
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }


    public static int GetMonth3AsInt(this string month_abbrev)
    {
        try
        {
            return (int)Enum.Parse<Months3>(month_abbrev);
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }


    [GeneratedRegex("^(19|20)\\d{2}-(0?[1-9]|1[0-2])-(0?[1-9]|1\\d|2\\d|3[0-1])$")]
    private static partial Regex yyyymmddRegex();
    [GeneratedRegex("^(0?[1-9]|1\\d|2\\d|3[0-1])-(0?[1-9]|1[0-2])-(19|20)\\d{2}$")]
    private static partial Regex ddmmyyyyRegex();
    [GeneratedRegex("^(0?[1-9]|1\\d|2\\d|3[0-1]) (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) (19|20)\\d{2}$")]
    private static partial Regex ddMMMyyyy();
    [GeneratedRegex("^(0?[1-9]|1\\d|2\\d|3[0-1]) (January|February|March|April|May|June|July|August|September|October|November|December) (19|20)\\d{2}$")]
    private static partial Regex ddMMMMyyyy();


    public static string? AsISODate(this string? instring)
    {
        string? interim_string = instring.Tidy();

        if (interim_string is null || interim_string == "1900-01-01" || interim_string == "01/01/1900"
                      || interim_string == "Jan  1 1900" || interim_string == "Jan  1 1900 12:00AM")
        {
            return null;
        }
        else
        {
            // First make the delimiter constant and remove commas.

            string datestring = interim_string.Replace('/', '-').Replace('.', '-').Replace(",", "");

            if (yyyymmddRegex().Match(datestring).Success)
            {
                if (datestring.Length == 10)
                {
                    // already OK
                    return datestring;
                } 
                else
                {
                    int dash1 = datestring.IndexOf('-');
                    int dash2 = datestring.LastIndexOf('-');
                    string year_s = datestring[..4];
                    string month_s = datestring[(dash1 + 1)..(dash2 - dash1 - 1)];
                    if (month_s.Length == 1) month_s = "0" + month_s;
                    string day_s = datestring[(dash2 + 1)..];
                    if (day_s.Length == 1) day_s = "0" + day_s;
                    return year_s + "-" + month_s + "-" + day_s;
                }
            }
            else if (ddmmyyyyRegex().Match(datestring).Success)
            {
                int dash1 = datestring.IndexOf('-');
                int dash2 = datestring.LastIndexOf('-');
                string year_s = datestring[^4..];
                string month_s = datestring[(dash1 + 1)..dash2];
                if (month_s.Length == 1) month_s = "0" + month_s;
                string day_s = datestring[..(dash1)];
                if (day_s.Length == 1) day_s = "0" + day_s;
                return year_s + "-" + month_s + "-" + day_s;
            }
            else if (ddMMMyyyy().Match(datestring).Success)
            {
                int dash1 = datestring.IndexOf(' ');
                int dash2 = datestring.LastIndexOf(' ');
                string year_s = datestring[^4..];
                string month = datestring[(dash1 + 1)..dash2];
                string month_s = month.GetMonth3AsInt().ToString("00");
                string day_s = datestring[..(dash1)];
                if (day_s.Length == 1) day_s = "0" + day_s;
                return year_s + "-" + month_s + "-" + day_s;
            }
            else if (ddMMMMyyyy().Match(datestring).Success)
            {
                int dash1 = datestring.IndexOf(' ');
                int dash2 = datestring.LastIndexOf(' ');
                string year_s = datestring[^4..];
                string month = datestring[(dash1 + 1)..dash2];
                string month_s = month.GetMonthAsInt().ToString("00");
                string day_s = datestring[..(dash1)];
                if (day_s.Length == 1) day_s = "0" + day_s;
                return year_s + "-" + month_s + "-" + day_s;
            }
            else
            {
                // to investigate other date forms.....
                return interim_string;
            }
        }
    }


    public static string? GetTimeUnits(this string? instring)
    {
        if (string.IsNullOrEmpty(instring))
        {
            return null;
        }
        else
        {
            string time_string = instring.ToLower();
            return time_string switch 
            { 
                string when time_string.Contains("year") => "Years",
                string when time_string.Contains("month") => "Months",
                string when time_string.Contains("week") => "Weeks",
                string when time_string.Contains("day") => "Days",
                string when time_string.Contains("hour") => "Hours",
                string when time_string.Contains("min") => "Minutes",
                _ => "Other (" + time_string + ")"

            };
        }
    }


    public static DateTime? FetchDateTimeFromISO(this string iso_string)
    {
        // iso_string assumed to be in format yyyy-mm-dd.
        if (string.IsNullOrEmpty(iso_string))

        {
            return null;
        }

        if (iso_string.Length > 10)
        {
            iso_string = iso_string[0..10];  // if date-time only interested in the date
        }

        int year = int.Parse(iso_string[0..4]);
        int month = int.Parse(iso_string[5..7]);
        int day = int.Parse(iso_string[^2..]);
        return new DateTime(year, month, day);
    }


    public static SplitDate? GetDateParts(this string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        // input date string is in the form of "<month name> day, year"
        // or in some cases in the form "<month name> year"
        // split the string on the comma.

        string year_string, month_name, day_string;
        int? year_num, month_num, day_num;

        int comma_pos = dateString.IndexOf(',');
        if (comma_pos > 0)
        {
            year_string = dateString[(comma_pos + 1)..].Trim();
            string first_part = dateString[..(comma_pos)].Trim();

            // first part should split on the space
            int space_pos = first_part.IndexOf(' ');
            day_string = first_part[(space_pos + 1)..].Trim();
            month_name = first_part[..(space_pos)].Trim();
        }
        else
        {
            int space_pos = dateString.IndexOf(' ');
            year_string = dateString[(space_pos + 1)..].Trim();
            month_name = dateString[..(space_pos)].Trim();
            day_string = "";
        }

        // convert strings into integers
        if (int.TryParse(year_string, out int y)) year_num = y; else year_num = null;
        month_num = month_name.GetMonthAsInt();
        string month_as3 = ((Months3)month_num).ToString();
        if (int.TryParse(day_string, out int d)) day_num = d; else day_num = null;


        // get date as string
        string? date_as_string;
        if (year_num is not null && month_num is not null && day_num is not null)
        {
            date_as_string = year_num.ToString() + " " + month_as3 + " " + day_num.ToString();
        }
        else if (year_num is not null && month_num is not null && day_num is null)
        {
            date_as_string = year_num.ToString() + ' ' + month_as3;
        }
        else if (year_num is not null && month_num is null && day_num is null)
        {
            date_as_string = year_num.ToString();
        }
        else
        {
            date_as_string = null;
        }

        return new SplitDate(year_num, month_num, day_num, date_as_string);
    }



    public static DateTime? FetchDateTimeFromDateString(this string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        SplitDate? sd = dateString.GetDateParts();
        if (sd is not null && sd.year is not null
                           && sd.month is not null && sd.day is not null)
        {
            return new DateTime((int)sd.year, (int)sd.month, (int)sd.day);
        }
        else
        {
            return null;
        }

    }
}


public class SplitDate
{
    public int? year;
    public int? month;
    public int? day;
    public string? date_string;

    public SplitDate(int? _year, int? _month, int? _day, string? _date_string)
    {
        year = _year;
        month = _month;
        day = _day;
        date_string = _date_string;
    }
}


public enum MonthsFull
{
    January = 1, February, March, April, May, June,
    July, August, September, October, November, December
};


public enum Months3
{
    Jan = 1, Feb, Mar, Apr, May, Jun,
    Jul, Aug, Sep, Oct, Nov, Dec
};


