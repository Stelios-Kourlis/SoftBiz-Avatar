using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class MarkdownToTMPConverter
{

    private static void LineEnded(List<MarkdownSymbol> list, StringBuilder sb)
    {
        if (list.Any(s => s.Symbol.Contains("#"))) //end of header line
        {
            sb.Append("</size>");
            int index = list.FindLastIndex(s => s.Symbol.Contains("#"));
            if (index != -1)
                list.RemoveAt(index);
        }

        list.RemoveAll(s => s.Symbol.Contains("^"));
        if (list.Any(s => s.Symbol == "`")) //Close single tick code
        {
            sb.Append("</font>");
            int index = list.FindLastIndex(s => s.Symbol == "`");
            if (index != -1)
                list.RemoveAt(index);
        }
    }

    private static void ReplaceLatestSymbol(List<MarkdownSymbol> list, StringBuilder sb)
    {
        MarkdownSymbol mds = list.TryLast(out MarkdownSymbol top) ? top : MarkdownSymbol.Empty;
        if (mds.IsEmpty || mds.Replaced) return;

        Debug.Log($"Replacing symbol: {mds.Symbol}, IsOpener: {mds.IsOpener}");
        switch (mds.Symbol)
        {
            case "*":
            case "_":
                if (mds.IsOpener)
                {
                    sb.Append("<i>");
                    mds.Replaced = true;
                }
                else
                {
                    sb.Append("</i>");
                    list.Remove(mds);
                    int index = list.FindLastIndex(s => s.Symbol.Contains("*"));
                    if (index != -1)
                        list.RemoveAt(index);
                }
                break;
            case "__":
            case "**":
                if (mds.IsOpener)
                {
                    sb.Append("<b>");
                    mds.Replaced = true;
                }
                else
                {
                    sb.Append("</b>");
                    list.Remove(mds);
                    int index = list.FindLastIndex(s => s.Symbol.Contains("**"));
                    if (index != -1)
                        list.RemoveAt(index);
                }
                break;
            case "***":
            case "___":
                if (mds.IsOpener)
                {
                    sb.Append("<i><b>");
                    mds.Replaced = true;
                }
                else
                {
                    sb.Append("</b></i>");
                    list.Remove(mds);
                    int index = list.FindLastIndex(s => s.Symbol.Contains("***"));
                    if (index != -1)
                        list.RemoveAt(index);
                }
                break;
            case "\n":
                list.Remove(mds);
                break;
            case "# ":
                sb.Append("<size=160%>");
                mds.Replaced = true;
                break;
            case "## ":
                sb.Append("<size=150%>");
                mds.Replaced = true;
                break;
            case "### ":
                sb.Append("<size=140%>");
                mds.Replaced = true;
                break;
            case "#### ":
                sb.Append("<size=130%>");
                mds.Replaced = true;
                break;
            case "##### ":
                sb.Append("<size=120%>");
                mds.Replaced = true;
                break;
            case "###### ":
                sb.Append("<size=110%>");
                mds.Replaced = true;
                break;
            case "> ":
                sb.Append(" | ");
                list.Remove(mds);
                break;
            case ">> ":
                sb.Append(" | | ");
                list.Remove(mds);
                break;
            case "- ":
                sb.Append(" • ");
                list.Remove(mds);
                break;
            case "^-":
                sb.Append("-"); //Just a slash, wasnt followed by space
                list.Remove(mds);
                break;
            case "```":
            case "`":
                if (mds.IsOpener)
                {
                    sb.Append("<font=\"Consolas SDF\">");
                    mds.Replaced = true;
                }
                else
                {
                    sb.Append("</font>");
                    list.Remove(mds);
                    int index = list.FindLastIndex(s => s.Symbol.Contains("`"));
                    if (index != -1)
                        list.RemoveAt(index);
                }
                break;

        }
    }

    public static string ConvertToTMPCompatibleText(string markdownText)
    {
        StringBuilder sb = new();
        List<MarkdownSymbol> symbolList = new();
        int index = 0;
        MarkdownSymbol last;
        markdownText = "\n" + markdownText;
        foreach (char letter in markdownText)
        {
            last = symbolList.TryLast(out last) ? last : MarkdownSymbol.Empty;
            if (symbolList.Any(s => s.Symbol.Contains("`")) && letter != '`' && letter != '\n')
            {
                ReplaceLatestSymbol(symbolList, sb);
                sb.Append(letter);
                continue;
            }

            switch (letter)
            {
                case '\n':
                    LineEnded(symbolList, sb);
                    symbolList.AddOrJoin(new MarkdownSymbol("\n", index));
                    sb.Append("\n");
                    break;
                case '#':
                    if (last.Symbol == "\n" || last.Symbol.Contains("#"))
                    {
                        symbolList.AddOrJoin(new MarkdownSymbol("#", index), 6);
                        if (symbolList.Last().Symbol.Length == 6) ReplaceLatestSymbol(symbolList, sb);
                    }
                    break;
                case '>':
                    if (last.Symbol == "\n" || last.Symbol.Contains(">"))
                    {
                        symbolList.AddOrJoin(new MarkdownSymbol(">", index), 2);
                        if (symbolList.Last().Symbol.Length == 2) ReplaceLatestSymbol(symbolList, sb);
                    }
                    break;
                case '-':
                    if (last.Symbol == "\n")
                        symbolList.AddOrJoin(new MarkdownSymbol("^-", index));
                    break;
                case '`':
                    symbolList.AddOrJoin(new MarkdownSymbol("`", index), 3);
                    if (symbolList.Last().Symbol.Length == 3) ReplaceLatestSymbol(symbolList, sb);
                    break;
                case ' ':
                    sb.Append(" ");
                    if (last.Symbol == "^*")
                        last.Symbol = "- "; //Convert to - if new line * followed by space
                    if (last.Symbol == "^-")
                        last.Symbol = "- ";
                    else if (last.Symbol.Contains("#"))
                        last.Symbol += " ";
                    else if (last.Symbol.Contains(">"))
                        last.Symbol += " ";
                    else
                        ReplaceLatestSymbol(symbolList, sb);
                    break;
                case '_':
                case '*':
                    //Up to 3 asteriks at a time
                    if (last.Symbol == "\n")
                    {
                        symbolList.Add(new MarkdownSymbol($"^*", index));
                        break;
                    }
                    symbolList.AddOrJoin(new MarkdownSymbol("*", index), 3);
                    if (symbolList.Last().Symbol.Length == 3) ReplaceLatestSymbol(symbolList, sb);
                    break;
                default:
                    ReplaceLatestSymbol(symbolList, sb);
                    sb.Append(letter);
                    break;
            }
            index++;
        }

        // string res = markdownText;

        // // Bold + Italic: ***text*** or ___text___
        // res = Regex.Replace(res, @"(\*\*\*|___)(.+?)\1", "<b><i>$2</i></b>");

        // // Bold: **text** or __text__
        // res = Regex.Replace(res, @"(\*\*|__)(.+?)\1", "<b>$2</b>");

        // // Italic: *text* or _text_
        // res = Regex.Replace(res, @"(\*|_)(.+?)\1", "<i>$2</i>");

        // // Headings 1-3: #, ##, ###
        // res = Regex.Replace(res, @"^# (.+)$", "<size=160%><b>$1</b></size>", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^## (.+)$", "<size=150%><b>$1</b></size>", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^### (.+)$", "<size=140%><b>$1</b></size>", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^#### (.+)$", "<size=130%><b>$1</b></size>", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^##### (.+)$", "<size=120%><b>$1</b></size>", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^###### (.+)$", "<size=110%><b>$1</b></size>", RegexOptions.Multiline);

        // //Block quotes
        // res = Regex.Replace(res, @"^> (.+)$", " | $1", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^>> (.+)$", " | | $1", RegexOptions.Multiline);

        // //Unordered lists: *, -, +
        // res = Regex.Replace(res, @"^(\s*)\* (.+)$", "$1• $2", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^(\s*)- (.+)$", "$1• $2", RegexOptions.Multiline);
        // res = Regex.Replace(res, @"^(\s*)\+ (.+)$", "$1• $2", RegexOptions.Multiline);



        return sb.ToString();
    }


}
