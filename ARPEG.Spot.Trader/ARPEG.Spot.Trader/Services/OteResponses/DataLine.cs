namespace ARPEG.Spot.Trader.Services;

public class DataLine
{
    public bool bold { get; set; }
    public string colour { get; set; }
    public Point[] point { get; set; }
    public string title { get; set; }
    public string tooltip { get; set; }
    public int tooltipDecimalsY { get; set; }
    public string type { get; set; }
    public bool useTooltip { get; set; }
    public bool useY2 { get; set; }
}