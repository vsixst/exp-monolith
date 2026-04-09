using Robust.Shared.GameStates;

namespace Content.Shared._Forge.LetoferolAnnihilator
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class AnnihilatorZoneVisualsComponent : Component
    {
        [DataField, AutoNetworkedField]
        public int Radius = 5;

        [DataField, AutoNetworkedField]
        public Color ZoneColor = Color.White;

        [ViewVariables]
        public EntityUid? Generator;

        [ViewVariables]
        public EntityUid? GridZone;
    }
}
