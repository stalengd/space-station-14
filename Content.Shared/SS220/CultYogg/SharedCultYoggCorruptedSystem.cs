// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg;

/// <summary>
/// Handles all corruption logic.
/// </summary>
public sealed class SharedCultYoggCorruptedSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly TimeSpan _corruptionDuration = TimeSpan.FromSeconds(3);
    private readonly Dictionary<string, CultYoggCorruptedPrototype> _recipes = [];


    public override void Initialize()
    {
        InitializeRecipes();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptDoAfterEvent>(OnDoAfterCorruption);
    }

    /// <summary>
    /// Checks if specified entity has corrupted counterpart.
    /// </summary>
    /// <param name="entity">Entiry to corrupt</param>
    /// <returns><see langword="true"/> if way to corrupt specified entity exists, otherwise <see langword="false"/></returns>
    public bool HasCorruptionRecipe(EntityUid entity)
    {
        return TryGetCorruptionRecipe(entity, out _);
    }

    /// <summary>
    /// Checks if specified entity is already corrupted.
    /// </summary>
    /// <param name="entity">Target entity</param>
    /// <returns><see langword="true"/> if entity is corrupted, otherwise <see langword="false"/></returns>
    public bool IsCorrupted(EntityUid entity)
    {
        return _entityManager.HasComponent<CultYoggCorruptedComponent>(entity);
    }

    /// <summary>
    /// Reverts corruption of the corrupted entity. Note that corrupted entity will be deleted and the new original one returned.
    /// </summary>
    /// <param name="corruptedEntity">Corrupted entity to revert</param>
    /// <returns>Newly created original form of the corrupted entity if any, otherwise <see langword="null"/></returns>
    public EntityUid? RevertCorruption(Entity<CultYoggCorruptedComponent> corruptedEntity)
    {
        if (corruptedEntity.Comp.PreviousForm is null)
            return null;

        var coords = Transform(corruptedEntity).Coordinates;
        var normalEntity = Spawn(corruptedEntity.Comp.PreviousForm, coords);

        _entityManager.DeleteEntity(corruptedEntity);
        return normalEntity;
    }

    /// <summary>
    /// Immediately corrupts specified entity, if possible. Note that original entity will be deleted and the new corrupted one returned.
    /// </summary>
    /// <param name="user">Entity that performs a corruption</param>
    /// <param name="entity">Entity to corrupt</param>
    /// <param name="isInHand">Flag indicating that the target entity is in hand</param>
    /// <returns>Newly created corrupted entity if corruption is possible, otherwise <see langword="null"/></returns>
    public EntityUid? TryCorruptImmediately(EntityUid user, EntityUid entity, bool isInHand)
    {
        if (!TryGetCorruptionRecipe(entity, out var recipe))
        {
            return null;
        }
        return Corrupt(user, entity, recipe, isInHand);
    }

    /// <summary>
    /// Starts a continuous corruption process over specified entity. Note that original entity will be deleted and the new corrupted one returned.
    /// </summary>
    /// <param name="user">Entity that performs a corruption</param>
    /// <param name="entity">Entity to corrupt</param>
    /// <param name="isInHand">Flag indicating that the target entity is in hand</param>
    /// <param name="callback">Optional callback to fire when finished</param>
    /// <returns><see langword="true"/> if corruption process has been started, otherwise <see langword="false"/></returns>
    public bool TryCorruptContinuously(EntityUid user, EntityUid entity, bool isInHand, Action<EntityUid?>? callback = null)
    {
        if (!TryGetCorruptionRecipe(entity, out var recipe))
        {
            return false;
        }
        var e = new CultYoggCorruptDoAfterEvent(recipe, isInHand, callback);
        var doafterArgs = new DoAfterArgs(EntityManager, user, _corruptionDuration, e, user, entity) //ToDo estimate time for corruption
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };
        _doAfter.TryStartDoAfter(doafterArgs);
        return true;
    }

    /// <summary>
    /// We need to re-initialize our recepies if prototypes are reloaded.
    /// </summary>
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<CultYoggCorruptedPrototype>())
            return;
        InitializeRecipes();
    }

    /// <summary>
    /// Continuous corruption after event handler.
    /// </summary>
    private void OnDoAfterCorruption(EntityUid uid, CultYoggComponent component, CultYoggCorruptDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (_net.IsClient)
            return;

        if (args.Proto == null)
            return;

        var corrupted = Corrupt(uid, args.Target.Value, args.Proto, args.InHand);
        args.Callback?.Invoke(corrupted);

        args.Handled = true;
    }

    /// <summary>
    /// Returns recipe to corrupt specified entity, if any.
    /// </summary>
    /// <param name="uid">Entity to corrupt</param>
    /// <param name="corruption">Result recipe</param>
    private bool TryGetCorruptionRecipe(EntityUid uid, [NotNullWhen(true)] out CultYoggCorruptedPrototype? corruption)
    {
        var prototypeId = MetaData(uid).EntityPrototype!.ID;
        if (prototypeId == null)
        {
            corruption = null;
            return false;
        }
        return _recipes.TryGetValue(prototypeId, out corruption);
    }

    /// <summary>
    /// Fills in the recipes dictionary from prototypes cache.
    /// </summary>
    private void InitializeRecipes()
    {
        _recipes.Clear();
        foreach (var recipe in _prototypeManager.EnumeratePrototypes<CultYoggCorruptedPrototype>())
        {
            _recipes.Add(recipe.ID, recipe);
        }
    }

    /// <summary>
    /// Basically replaces entity with its corrupted counterpart according to recipe.
    /// </summary>
    /// <param name="user">Entity that performs a corruption</param>
    /// <param name="entity">Entity to corrupt</param>
    /// <param name="recipe">Recipe prototype</param>
    /// <param name="isInHand">Flag indicating that the target entity is in hand</param>
    /// <returns>Corrupted entity</returns>
    private EntityUid? Corrupt(EntityUid user, EntityUid entity, CultYoggCorruptedPrototype recipe, bool isInHand)
    {
        var coords = Transform(entity).Coordinates;
        var corruptedEntity = Spawn(recipe.Result, coords);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user)} used corrupt on {ToPrettyString(entity)} and made {ToPrettyString(corruptedEntity)}");

        //ToDo if object is a storage, it should drop all its items

        //Every corrupted entity should have this entity at start
        _entityManager.AddComponent<CultYoggCorruptedComponent>(corruptedEntity);//ToDo save previuos form here, so delete it when you do all the corrupted list
        if (!_entityManager.TryGetComponent<CultYoggCorruptedComponent>(corruptedEntity, out var corrupted))
            return null;

        corrupted.PreviousForm = MetaData(entity).EntityPrototype?.ID;
        corrupted.CorruptionReverseEffect = recipe.CorruptionReverseEffect;

        //Delete previous entity
        _entityManager.DeleteEntity(entity);

        if (isInHand)
            _hands.PickupOrDrop(user, corruptedEntity);

        return corruptedEntity;
    }
}

/// <summary>
/// Event raised after completeon of DoAfter
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CultYoggCorruptDoAfterEvent : SimpleDoAfterEvent
{
    public readonly bool InHand;
    public readonly CultYoggCorruptedPrototype? Proto;
    [NonSerialized]
    public readonly Action<EntityUid?>? Callback;

    public CultYoggCorruptDoAfterEvent(CultYoggCorruptedPrototype? proto, bool inHand, Action<EntityUid?>? callback)
    {
        InHand = inHand;
        Proto = proto;
        Callback = callback;
    }
}
