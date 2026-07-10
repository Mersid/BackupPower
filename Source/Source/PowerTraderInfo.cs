using RimWorld;

namespace BackupPower;

/// <summary>
/// Represents a power producer or consumer and some information about it.
/// It beats using a ridiculously large tuple.
/// </summary>
public sealed class PowerTraderInfo
{
	/// <summary>
	/// The power trader itself. These are producers or consumers, but not batteries.
	/// </summary>
	public required CompPowerTrader Comp { get; set; }

	/// <summary>
	/// The backup battery box building, if any.
	/// </summary>
	public Building_BackupPowerAttachment? Broker { get; init; }

    public float Consumption { get; init; }
    public float CurrentProduction { get; init; }
    public float PotentialProduction { get; set; }
}