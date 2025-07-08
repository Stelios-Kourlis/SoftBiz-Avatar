using System.Text.RegularExpressions;
using UnityEngine;

public static class MarkdownToTMPConverter
{
    public static string ConvertToTMPCompatibleText(string markdownText)
    {
        string res = markdownText;

        // Bold + Italic: ***text*** or ___text___
        res = Regex.Replace(res, @"(\*\*\*|___)(.+?)\1", "<b><i>$2</i></b>");

        // Bold: **text** or __text__
        res = Regex.Replace(res, @"(\*\*|__)(.+?)\1", "<b>$2</b>");

        // Italic: *text* or _text_
        res = Regex.Replace(res, @"(\*|_)(.+?)\1", "<i>$2</i>");

        // Headings 1-3: #, ##, ###
        res = Regex.Replace(res, @"^# (.+)$", "<size=160%><b>$1</b></size>", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^## (.+)$", "<size=150%><b>$1</b></size>", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^### (.+)$", "<size=140%><b>$1</b></size>", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^#### (.+)$", "<size=130%><b>$1</b></size>", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^##### (.+)$", "<size=120%><b>$1</b></size>", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^###### (.+)$", "<size=110%><b>$1</b></size>", RegexOptions.Multiline);

        //Block quotes
        res = Regex.Replace(res, @"^> (.+)$", " | $1", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^>> (.+)$", " | | $1", RegexOptions.Multiline);

        //Unordered lists: *, -, +
        res = Regex.Replace(res, @"^(\s*)\* (.+)$", "$1• $2", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^(\s*)- (.+)$", "$1• $2", RegexOptions.Multiline);
        res = Regex.Replace(res, @"^(\s*)\+ (.+)$", "$1• $2", RegexOptions.Multiline);



        return res;
    }
}
