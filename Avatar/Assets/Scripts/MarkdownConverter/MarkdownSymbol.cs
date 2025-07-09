internal class MarkdownSymbol
{
    public string Symbol { get; set; }
    public bool Replaced { get; set; }
    public bool IsOpener { get; set; }
    public int Length => Symbol.Length;
    // public int Index { get; set; }
    public static MarkdownSymbol Empty => new(string.Empty, -1);
    public bool IsEmpty => Symbol == string.Empty;

    public MarkdownSymbol(string symbol, int index, bool isOpener = true)
    {
        Symbol = symbol;
        Replaced = false;
        // Index = index;
        IsOpener = isOpener;
    }

}