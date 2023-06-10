namespace ARPEG.Spot.Trader.Services;

public class NowManualBatteryService
{
    public enum BatteryChargeDirection
    {
        None,
        Charge,
        Discharge
    }

    private DateTime ValidUntil { get; set; }

    private BatteryChargeDirection Direction { get; set; }

    public BatteryChargeDirection GetActiveDirection()
        => ValidUntil > DateTime.Now ? Direction : BatteryChargeDirection.None;

    public void SetBatteryMode(BatteryChargeDirection direction)
    {
        Direction = direction;
        var hour = DateTime.Now.Hour + 1;
        ValidUntil = DateTime.Now.Date.AddHours(hour);
    }

}