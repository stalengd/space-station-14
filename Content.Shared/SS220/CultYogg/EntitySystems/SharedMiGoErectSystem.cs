// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Content.Shared.Materials;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public sealed class SharedMiGoErectSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private readonly List<EntityUid> _dropEntitiesBuffer = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MiGoComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<MiGoComponent, MiGoErectBuildingSelectedMessage>(OnBuildingSelectedMessage);

        SubscribeLocalEvent<CultYoggBuildingFrameComponent, ComponentInit>(OnBuildingFrameInit);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, InteractUsingEvent>(OnBuildingFrameInteractUsing);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, GetVerbsEvent<InteractionVerb>>(AddInteractionVerbs);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, ExaminedEvent>(OnBuildingFrameExamined);
    }

    public void OpenUI(Entity<MiGoComponent> entity, ActorComponent actor)
    {
        _userInterfaceSystem.TryToggleUi(entity.Owner, MiGoErectUiKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(Entity<MiGoComponent> entity, ref BoundUIOpenedEvent args)
    {
        _userInterfaceSystem.SetUiState(args.Entity, MiGoErectUiKey.Key, new MiGoErectBuiState()
        {
            Buildings = _prototypeManager.GetInstances<CultYoggBuildingPrototype>().Values.ToList(),
        });
    }

    private void OnBuildingSelectedMessage(Entity<MiGoComponent> entity, ref MiGoErectBuildingSelectedMessage args)
    {
        if (!_prototypeManager.TryIndex(args.BuildingId, out var buildingPrototype))
            return;
        var transform = Transform(entity);
        var location = transform.Coordinates;
        var tileRef = location.GetTileRef();
        if (tileRef == null || _turfSystem.IsTileBlocked(tileRef.Value, Physics.CollisionGroup.MachineMask))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-building-tile-blocked-popup"), entity, entity);
            return;
        }
        var frameEntity = SpawnAtPosition(buildingPrototype.FrameEntityId, location);
        Transform(frameEntity).LocalRotation = transform.LocalRotation.GetCardinalDir().ToAngle();
        var resultEntityProto = _prototypeManager.Index(buildingPrototype.ResultEntityId);
        _metaDataSystem.SetEntityName(frameEntity, Loc.GetString("cult-yogg-building-frame-name-template", ("name", resultEntityProto.Name)));
        var frame = EnsureComp<CultYoggBuildingFrameComponent>(frameEntity);
        frame.BuildingPrototypeId = buildingPrototype.ID;
        while (frame.AddedMaterialsAmount.Count < buildingPrototype.Materials.Count)
        {
            frame.AddedMaterialsAmount.Add(0);
        }
        Dirty(entity);
    }

    private void OnBuildingFrameInit(Entity<CultYoggBuildingFrameComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Container = _containerSystem.EnsureContainer<Container>(entity, CultYoggBuildingFrameComponent.ContainerId);
    }

    private void OnBuildingFrameInteractUsing(Entity<CultYoggBuildingFrameComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryInsert(entity, args.Used))
            args.Handled = true;
    }

    private void AddInteractionVerbs(Entity<CultYoggBuildingFrameComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;
        if (args.Using == null || !_actionBlockerSystem.CanDrop(args.User))
            return;
        if (!CanInsert(entity, args.Using.Value))
            return;

        var verbSubject = Name(args.Using.Value);

        var item = args.Using.Value;
        InteractionVerb insertVerb = new()
        {
            IconEntity = GetNetEntity(args.Using),
            Act = () => TryInsert(entity, item)
        };

        insertVerb.Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject));
        insertVerb.Icon =
            new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/VerbIcons/drop.svg.192dpi.png"));

        args.Verbs.Add(insertVerb);
    }

    private void AddVerbs(Entity<CultYoggBuildingFrameComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess)
            return;

        Verb destroyVerb = new()
        {
            Text = Loc.GetString("cult-yogg-building-frame-verb-destroy"),
            Act = () => DestroyFrame(entity),
        };
        args.Verbs.Add(destroyVerb);
    }

    private void OnBuildingFrameExamined(Entity<CultYoggBuildingFrameComponent> entity, ref ExaminedEvent args)
    {
        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return;
        using (args.PushGroup(nameof(CultYoggBuildingFrameComponent)))
        {
            for (var i = 0; i < neededMaterials.Count; i++)
            {
                var neededMaterial = neededMaterials[i];
                var addedCount = entity.Comp.AddedMaterialsAmount[i];
                var locKey = addedCount >= neededMaterial.Count ?
                    "cult-yogg-building-frame-examined-material-full" :
                    "cult-yogg-building-frame-examined-material-needed";
                if (!_prototypeManager.TryIndex(neededMaterial.StackType, out var stackType))
                    continue;
                var materialName = Loc.GetString(stackType.Name);
                args.PushMarkup(Loc.GetString(locKey, ("material", materialName), ("currentAmount", addedCount), ("totalAmount", neededMaterial.Count)));
            }
        }
    }

    private bool CanInsert(Entity<CultYoggBuildingFrameComponent> entity, EntityUid item)
    {
        return CanInsert(entity, item, out _);
    }

    private bool CanInsert(Entity<CultYoggBuildingFrameComponent> entity, EntityUid item, out int materialIndex)
    {
        materialIndex = 0;
        if (!HasComp<MaterialComponent>(item) || !TryComp<StackComponent>(item, out var stack))
            return false;
        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return false;
        for (var i = 0; i < neededMaterials.Count; i++)
        {
            var materialToBuild = neededMaterials[i];
            if (stack.StackTypeId == materialToBuild.StackType)
            {
                materialIndex = i;
                return true;
            }
        }
        return false;
    }

    private bool TryInsert(Entity<CultYoggBuildingFrameComponent> entity, EntityUid item)
    {
        if (!CanInsert(entity, item, out var materialIndex))
            return false;
        if (!TryComp<StackComponent>(item, out var stack))
            return false;
        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return false;

        var materialToBuild = neededMaterials[materialIndex];
        var countToAdd = stack.Count;
        var containedCount = entity.Comp.AddedMaterialsAmount[materialIndex];
        var canAdd = Math.Min(countToAdd, materialToBuild.Count - containedCount);
        var leftCount = countToAdd - canAdd;
        if (canAdd <= 0)
            return false;

        if (_gameTiming.InPrediction)
            return true; // In prediction just say that we can, all the heavy lifting is up to server

        EntityUid materialEntityToInsert;
        if (leftCount == 0)
        {
            materialEntityToInsert = item;
        }
        else
        {
            var stackTypeProto = _prototypeManager.Index(materialToBuild.StackType);
            materialEntityToInsert = Spawn(stackTypeProto.Spawn);
            _stackSystem.SetCount(materialEntityToInsert, canAdd);
            var materialEntityToLeft = item;
            _stackSystem.SetCount(materialEntityToLeft, leftCount);
        }
        _containerSystem.Insert(materialEntityToInsert, entity.Comp.Container);
        entity.Comp.AddedMaterialsAmount[materialIndex] = containedCount + canAdd;
        Dirty(entity);
        if (IsBuildingFrameCompleted(entity))
            CompleteBuilding(entity);

        return true;
    }

    private bool TryGetNeededMaterials(Entity<CultYoggBuildingFrameComponent> entity, [NotNullWhen(true)] out List<CultYoggBuildingMaterial>? materials)
    {
        materials = null;
        if (!_prototypeManager.TryIndex(entity.Comp.BuildingPrototypeId, out var prototype))
            return false;
        materials = prototype.Materials;
        return true;
    }

    private bool IsBuildingFrameCompleted(Entity<CultYoggBuildingFrameComponent> entity)
    {
        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return false;
        for (var i = 0; i < neededMaterials.Count; i++)
        {
            var materialToBuild = neededMaterials[i];
            var addedAmount = entity.Comp.AddedMaterialsAmount[i];
            if (addedAmount < materialToBuild.Count)
                return false;
        }
        return true;
    }

    private EntityUid CompleteBuilding(Entity<CultYoggBuildingFrameComponent> entity)
    {
        if (_gameTiming.InPrediction) // this should never run in client
            return default;
        if (!_prototypeManager.TryIndex(entity.Comp.BuildingPrototypeId, out var prototype, logError: true))
            return default;
        var transform = Transform(entity);
        var resultEntity = SpawnAtPosition(prototype.ResultEntityId, transform.Coordinates);
        Transform(resultEntity).LocalRotation = transform.LocalRotation;
        Del(entity);
        return resultEntity;
    }

    private void DestroyFrame(Entity<CultYoggBuildingFrameComponent> entity)
    {
        if (_gameTiming.InPrediction) // this should never run in client
            return;
        _dropEntitiesBuffer.Clear();
        var coords = Transform(entity).Coordinates;
        foreach (var item in entity.Comp.Container.ContainedEntities)
        {
            _dropEntitiesBuffer.Add(item);
        }
        foreach (var item in _dropEntitiesBuffer)
        {
            _transformSystem.AttachToGridOrMap(item);
            _transformSystem.SetCoordinates(item, coords);
        }
        _dropEntitiesBuffer.Clear();
        Del(entity);
    }
}
