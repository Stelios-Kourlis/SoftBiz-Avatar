using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal static class MarkdownListExtensions
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static bool TryLast(this List<MarkdownSymbol> list, out MarkdownSymbol symbol)
    {
        symbol = list.Count > 0 ? list.Last() : null;
        return list.Count > 0;
    }

    public static void AddOrJoin(this List<MarkdownSymbol> list, MarkdownSymbol symbol, int maxRepeated = 1)
    {
        bool OpenerOfSameType;
        if (list.TryLast(out MarkdownSymbol last) && last.Symbol.Contains(symbol.Symbol) && !last.Replaced && last.Length < maxRepeated)
        {
            string log = $"Joined to list: {symbol.Symbol}, JoinedWith: {last.Symbol}";
            last.Symbol += symbol.Symbol;
            OpenerOfSameType = list.Any(s => s.Symbol == last.Symbol && s.IsOpener && s != last);
            last.IsOpener = !OpenerOfSameType;
            Debug.Log($"{log} isOpener: {last.IsOpener} new length: {list.Count}");
            return;
        }

        OpenerOfSameType = list.Any(s => s.Symbol == symbol.Symbol && s.IsOpener);
        symbol.IsOpener = !OpenerOfSameType;
        list.Add(symbol);
        Debug.Log($"Added to list: {symbol.Symbol}, On Top Of: {last?.Symbol ?? "-"} isOpener: {symbol.IsOpener} new length: {list.Count}");
    }
}
