namespace ARPEG.Sport.Licence.Server.DTO;
public class BatteryCommand
{
    /// <summary>
    /// Seriove cislo daneho OPM, kteremu je zprava urcena
    /// </summary>
    public required string SerialNumber { get; set; }

    /// <summary>
    /// Typ akce, ktera se ma provest
    /// </summary>
    public OpmActionEnum OpmAction { get; set; }

    /// <summary>
    /// Mnozstvi, ktere se ma prodat nebo koupit [kW]
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Start casoveho okna, ve kterem se ma akce provest
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Konec casoveho okna, ve kterem se ma akce provest
    /// </summary>
    public DateTime End { get; set; }
}

public enum OpmActionEnum
{
    SELL,
    BUY
}
