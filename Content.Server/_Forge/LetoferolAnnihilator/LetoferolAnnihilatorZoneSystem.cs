using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Server.Player;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Server.GameStates;
using Content.Shared._Forge.LetoferolAnnihilator;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;

namespace Content.Server._Forge.LetoferolAnnihilator
{
    public sealed class LetoferolAnnihilatorZoneSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly PvsOverrideSystem _pvsSys = default!;

        private const string ZonePrototype = "AnnihilatorZone";

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var toDeleteBuffer = new List<EntityUid>();
            var toCreateBuffer = new List<(EntityUid source, EntityUid parentGrid)>();

            var query = EntityQueryEnumerator<LetoferolAnnihilatorZoneComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                if (HasComp<AnnihilatorZoneVisualsComponent>(uid))
                {
                    if (component.Generator != null && !EntityManager.EntityExists(component.Generator.Value))
                    {
                        toDeleteBuffer.Add(uid);
                        if (component.Generator != null && TryComp<LetoferolAnnihilatorZoneComponent>(component.Generator.Value, out var sourceComp))
                        {
                            sourceComp.Generator = null;
                            sourceComp.GridZone = null;
                        }
                    }
                    continue;
                }

                if (_gameTiming.CurTime < component.NextUpdate)
                    continue;

                UpdateDamageState(uid, component);

                var parent = Transform(uid).GridUid;
                if (parent == null)
                    continue;

                if (component.Generator == null)
                {
                    toCreateBuffer.Add((uid, parent.Value));
                }
            }

            foreach (var zone in toDeleteBuffer)
            {
                EntityManager.QueueDeleteEntity(zone);
            }

            foreach (var (source, parentGrid) in toCreateBuffer)
            {
                if (!TryComp<LetoferolAnnihilatorZoneComponent>(source, out var sourceComp))
                    continue;

                var generator = ZonedEntity(parentGrid, source);
                if (generator != EntityUid.Invalid)
                {
                    sourceComp.Generator = generator;
                    sourceComp.GridZone = parentGrid;
                }
            }
        }

        private void UpdateDamageState(EntityUid uid, LetoferolAnnihilatorZoneComponent component)
        {
            var damage = component.Damage;
            if (damage == null || damage.Empty)
                return;

            var transform = Transform(uid);
            var entities = _lookup.GetEntitiesInRange(transform.Coordinates, component.Radius);

            foreach (var entity in entities)
            {
                if (!_factionSystem.IsMember(entity, component.Target))
                    continue;

                _damageable.TryChangeDamage(entity, damage, ignoreResistances: false);
            }

            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;
        }

        private EntityUid ZonedEntity(EntityUid entity, EntityUid? source = null, MapGridComponent? mapGrid = null)
        {
            if (!Resolve(entity, ref mapGrid, false))
                return EntityUid.Invalid;

            var prototype = ZonePrototype;

            var generator = Spawn(prototype, Transform(entity).Coordinates);
            var generatorComp = EnsureComp<LetoferolAnnihilatorZoneComponent>(generator);
            generatorComp.GridZone = entity;
            generatorComp.Generator = source;

            var zoneVisuals = EnsureComp<AnnihilatorZoneVisualsComponent>(generator);
            if (source != null && TryComp<LetoferolAnnihilatorZoneComponent>(source.Value, out var sourceComp))
            {
                zoneVisuals.ZoneColor = sourceComp.ZoneColor;
                zoneVisuals.Radius = sourceComp.Radius;
                Dirty(generator, zoneVisuals);
            }

            var gridCenter = new EntityCoordinates(entity, mapGrid.LocalAABB.Center);
            _transformSystem.SetCoordinates(generator, gridCenter);
            _transformSystem.SetWorldRotation(generator, _transformSystem.GetWorldRotation(entity));

            _pvsSys.AddGlobalOverride(generator);

            return generator;
        }
        private void UnzonedEntity(EntityUid zoneUid, LetoferolAnnihilatorZoneComponent zoneComp)
        {
            if (zoneComp.Generator != null && TryComp<LetoferolAnnihilatorZoneComponent>(zoneComp.Generator.Value, out var sourceComp))
            {
                sourceComp.Generator = null;
                sourceComp.GridZone = null;
            }

            TryQueueDel(zoneUid);
        }
    }
}
