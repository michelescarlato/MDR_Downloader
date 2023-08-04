using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System.Text.RegularExpressions;

namespace MDR_Downloader.Helpers;

public static class StringExtensions
{
    public static string? Tidy(this string? input_string)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise trims the string

        if (input_string is null or "NULL" or "null" or "\"NULL\"" or "\"null\""
            || input_string.Trim() == "")
        {
            return null;
        }

        if (!input_string.StartsWith('"'))
        {
            // some strings will have start and end quotes
            // a start quote should indicate leaving both 

            char[] chars1 = { ' ', ';' };
            input_string = input_string.Trim(chars1);
        }
        else
        {
            char[] chars2 = { '"', ' ', ';' };
            input_string = input_string.Trim(chars2);
        }

        return input_string == "" ? null : input_string;

    }


    public static string? ReplaceUnicodes(this string? input_string)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise trims the 
        // string and replace escaped non-ascii codes.

        if (input_string is null or "NULL" or "null" or "\"NULL\"" or "\"null\"" 
            || input_string.Trim() == "")
        {
            return null;
        }
        
        string output_string = input_string.Replace("&#32;", " ").Replace("&#37;", "%");
        output_string = output_string.Replace("&#39;", "’").Replace("&quot;", "'");
        output_string = output_string.Replace("#gt;", ">").Replace("#lt;", "<");
        output_string = output_string.Replace("&amp;", "&");
        return output_string;

    }


    public static string? ReplaceHtmlTags(this string? input_string)
    {
        // Simple extension that returns null for null values and
        // text based 'NULL equivalents', and otherwise trims the 
        // string

        if (input_string is null or "NULL" or "null" or "\"NULL\"" or "\"null\"" 
            || input_string.Trim() == "")
        {
            return null;
        }

        string output_string = input_string.Replace("<br>", "\n");
        output_string = output_string.Replace("<br/>", "\n");
        output_string = output_string.Replace("<br />", "\n");
        output_string = output_string.Replace("\n\n", "\n").Replace("\n \n", "\n");

        return output_string;
    }


    public static string? CompressSpaces(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string output_string = input_string.Trim();

        output_string = output_string.Replace("\r\n", "\n");    // regularise endings
        output_string = output_string.Replace("\r", "\n");

        while (output_string.Contains("  "))
        {
            output_string = output_string.Replace("  ", " ");
        }
        output_string = output_string.Replace("\n:\n", ":\n");
        output_string = output_string.Replace("\n ", "\n");
        while (output_string.Contains("\n\n"))
        {
            output_string = output_string.Replace("\n\n", "\n");
        }
        output_string = output_string.TrimEnd('\n');
        return output_string;

    }


    public static string? ReplaceNBSpaces(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string output_string = input_string.Replace('\u00A0', ' ');
        output_string = output_string.Replace('\u2000', ' ').Replace('\u2001', ' ');
        output_string = output_string.Replace('\u2002', ' ').Replace('\u2003', ' ');
        output_string = output_string.Replace('\u2007', ' ').Replace('\u2008', ' ');
        output_string = output_string.Replace('\u2009', ' ').Replace('\u200A', ' ');

        return output_string;
    }


    public static string? lang_3_to_2(this string input_lang_code)
    {
        if (string.IsNullOrEmpty(input_lang_code))
        {
            return null;
        }
        
        return input_lang_code switch
        {
            "fre" => "fr",
            "ger" => "de",
            "spa" => "es",
            "ita" => "it",
            "por" => "pt",
            "rus" => "ru",
            "tur" => "tr",
            "hun" => "hu",
            "pol" => "pl",
            "swe" => "sv",
            "nor" => "no",
            "dan" => "da",
            "fin" => "fi",
            _ => "??"
        };
    }
}


public static class ScrapingExtensions
{
    public static string? InnerValue(this HtmlNode node)
    {
        if (string.IsNullOrEmpty(node.InnerText))
        {
            return null;
        }

        string output_string = node.InnerText.Replace("\n", "").Replace("\r", "").Trim();
        string? label = node.CssSelect(".label").FirstOrDefault()?.InnerText?.Trim();
        if (!string.IsNullOrEmpty(label))
        {
            output_string = output_string[(label.Length)..].Trim();
        }
        return output_string;
    }


    public static string? TrimmedContents(this HtmlNode node)
    {
        if (string.IsNullOrEmpty(node.InnerText))
        {
            return null;
        }
        return node.InnerText?.Replace("\n", "").Replace("\r", "").Trim() ?? "";
    }


    public static string? RemoveLabelAndSupp(this string attribute_value,
                                   string attribute_name, string? suppText)
    {
        if (string.IsNullOrEmpty(attribute_value))
        {
            return null;
        }
        
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


public static class DateExtensions
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

    public static string? AsISODate(this string? input_string)
    {
        string? interim_string = input_string.Tidy();

        if (interim_string is null or "1900-01-01" or "01/01/1900" or "Jan  1 1900" or "Jan  1 1900 12:00AM")
        {
            return null;
        }

        // First make the delimiter constant and remove commas,
        // before checking against regexes for the different date formats.

        string date_string = interim_string.Replace('/', '-').Replace('.', '-').Replace(",", "");

        string yyyy_mm_dd = @"^(19|20)\d{2}-(0?[1-9]|1[0-2])-(0?[1-9]|1\d|2\d|3[0-1])$";
        string dd_mm_yyyy = @"^(0?[1-9]|1\d|2\d|3[0-1])-(0?[1-9]|1[0-2])-(19|20)\d{2}$";
        string dd_MMM_yyyy = @"^(0?[1-9]|1\d|2\d|3[0-1]) (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) (19|20)\d{2}$";
        string dd_MMMM_yyyy = @"^(0?[1-9]|1\d|2\d|3[0-1]) (January|February|March|April|May|June|July|August|September|October|November|December) (19|20)\d{2}$";
        
        if (Regex.Match(date_string, yyyy_mm_dd).Success)
        {
            if (date_string.Length == 10)
            {
                return date_string;  // already OK
            }
            int dash1 = date_string.IndexOf('-');
            int dash2 = date_string.LastIndexOf('-');
            string year_s = date_string[..4];
            string month_s = date_string[(dash1 + 1)..(dash2 - dash1 - 1)];
            if (month_s.Length == 1) month_s = "0" + month_s;
            string day_s = date_string[(dash2 + 1)..];
            if (day_s.Length == 1) day_s = "0" + day_s;
            return year_s + "-" + month_s + "-" + day_s;
        }
        else if (Regex.Match(date_string, dd_mm_yyyy).Success)
        {
            int dash1 = date_string.IndexOf('-');
            int dash2 = date_string.LastIndexOf('-');
            string year_s = date_string[^4..];
            string month_s = date_string[(dash1 + 1)..dash2];
            if (month_s.Length == 1) month_s = "0" + month_s;
            string day_s = date_string[..(dash1)];
            if (day_s.Length == 1) day_s = "0" + day_s;
            return year_s + "-" + month_s + "-" + day_s;
        }
        else if (Regex.Match(date_string, dd_MMM_yyyy).Success)
        {
            int dash1 = date_string.IndexOf(' ');
            int dash2 = date_string.LastIndexOf(' ');
            string year_s = date_string[^4..];
            string month = date_string[(dash1 + 1)..dash2];
            string month_s = month.GetMonth3AsInt().ToString("00");
            string day_s = date_string[..(dash1)];
            if (day_s.Length == 1) day_s = "0" + day_s;
            return year_s + "-" + month_s + "-" + day_s;
        }
        else if (Regex.Match(date_string, dd_MMMM_yyyy).Success)
        {
            int dash1 = date_string.IndexOf(' ');
            int dash2 = date_string.LastIndexOf(' ');
            string year_s = date_string[^4..];
            string month = date_string[(dash1 + 1)..dash2];
            string month_s = month.GetMonthAsInt().ToString("00");
            string day_s = date_string[..(dash1)];
            if (day_s.Length == 1) day_s = "0" + day_s;
            return year_s + "-" + month_s + "-" + day_s;
        }
        else
        {
            // To investigate other date forms.....
            
            return interim_string;
        }
    }


    public static string? GetTimeUnits(this string? input_string)
    {
        if (string.IsNullOrEmpty(input_string))
        {
            return null;
        }

        string time_string = input_string.ToLower();
        return time_string switch 
        { 
            _ when time_string.Contains("year") => "Years",
            _ when time_string.Contains("month") => "Months",
            _ when time_string.Contains("week") => "Weeks",
            _ when time_string.Contains("day") => "Days",
            _ when time_string.Contains("hour") => "Hours",
            _ when time_string.Contains("min") => "Minutes",
            _ => "Other (" + time_string + ")"
        };
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
            iso_string = iso_string[..10];  // if date-time only interested in the date
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

        // input date string is in the ISO format of ""
        // or in some cases in the form "<month name> year"
        // split the string on the comma.

        string year_string, month_name, day_string;
        int? year_num, day_num;
        string? month_as3 = null;

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
        int month_num = month_name.GetMonthAsInt();
        if (month_num > 0)
        {
            month_as3 = ((Months3)month_num).ToString();
        }

        if (int.TryParse(day_string, out int d)) day_num = d; else day_num = null;


        // get date as string
        string? date_as_string;
        if (year_num is not null && month_as3 is not null && day_num is not null)
        {
            date_as_string = year_num + " " + month_as3 + " " + day_num;
        }
        else if (year_num is not null && month_as3 is not null && day_num is null)
        {
            date_as_string = year_num + " " + month_as3;
        }
        else if (year_num is not null && month_as3 is null && day_num is null)
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
        if (sd is null || sd.year is null || sd.month is null || sd.day is null)
        {
            return null;
        }
        return new DateTime((int)sd.year, (int)sd.month, (int)sd.day);
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


