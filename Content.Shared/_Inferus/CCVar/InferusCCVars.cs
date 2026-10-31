using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Inferus.CCVar;

/// <summary>
/// Inferus-specific CVars.
/// </summary>
[CVarDefs]
public sealed class InferusCCVars : CVars
{
    /// <summary>
    /// If true, the old pain/crit overlays are used instead of the new shader-based ones.
    /// </summary>
    public static readonly CVarDef<bool> LegacyDamageOverlays =
        CVarDef.Create("inferus.legacy_damage_overlays", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
