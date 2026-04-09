using Robust.Shared.Audio;

namespace Content.Server._Forge.Bioscanner;

[RegisterComponent]
public sealed partial class BioscannerComponent : Component
{
    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Who was the target of the last scan?
    /// </summary>
    [ViewVariables]
    public EntityUid? LastTarget;

    /// <summary>
    /// Was the last scanned target infested?
    /// </summary>
    [ViewVariables]
    public bool WasInfested;

}
