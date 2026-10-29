using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HelmetVisorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField]
    public EntProtoId Action = "ActionToggleHelmetVisor";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public string StateUp = "";

    [DataField]
    public string StateDown = "equipped-head-up";

    [DataField]
    public string VisualLayer = "visor";

    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public ComponentRegistry Components = new();

    // BEGIN Inferus - species fallback and optional RSI for the visor layer
    /// <summary>
    /// Species state suffixes (e.g. "-vox") that have a matching visor-down sprite in the RSI.
    /// Any other suffix (feroxi, harpy, etc.) falls back to the base <see cref="StateDown"/> instead of an error sprite.
    /// </summary>
    [DataField]
    public HashSet<string> SupportedSuffixes = new() { "-dog", "-reptilian", "-vox", "-vulpkanin" };

    /// <summary>
    /// Optional RSI to draw the equipped visor layer from. If null, the clothing's own RSI is used.
    /// </summary>
    [DataField]
    public string? RsiPath;
    // END Inferus
}

public sealed partial class ToggleHelmetVisorEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum HelmetVisorVisuals : byte
{
    IsDown
}
