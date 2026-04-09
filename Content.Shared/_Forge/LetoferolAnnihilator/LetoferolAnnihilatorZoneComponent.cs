using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.NPC.Prototypes;

namespace Content.Shared._Forge.LetoferolAnnihilator
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class LetoferolAnnihilatorZoneComponent : Component
    {
        [ViewVariables]
        public TimeSpan NextUpdate;

        [DataField]
        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

        [DataField, AutoNetworkedField]
        public int Radius = 5;

        [DataField(required: true)]
        public ProtoId<NpcFactionPrototype> Target = string.Empty;

        [DataField]
        public DamageSpecifier Damage = new();

        [DataField, AutoNetworkedField]
        public Color ZoneColor = Color.White;

        [ViewVariables]
        public EntityUid? Generator;

        [ViewVariables]
        public EntityUid? GridZone;
    }
}
