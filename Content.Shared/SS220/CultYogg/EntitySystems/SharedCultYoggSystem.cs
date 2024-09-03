// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedCultYoggSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly SharedCultYoggCorruptedSystem _cultYoggCorruptedSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentStartup>(OnCompInit);

        SubscribeLocalEvent<CultYoggComponent, ExaminedEvent>(OnExamined);

        // actions
        SubscribeLocalEvent<CultYoggComponent, CultYoggPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggDigestEvent>(DigestAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemInHandEvent>(CorruptItemInHandAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggAscendingEvent>(AscendingAction);
    }

    protected virtual void OnCompInit(Entity<CultYoggComponent> uid, ref ComponentStartup args)
    {
        //_actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);//delete after testing
        //_actions.AddAction(uid, ref uid.Comp.AscendingActionEntity, uid.Comp.AscendingAction);//delete after testing
        _actions.AddAction(uid, ref uid.Comp.CorruptItemActionEntity, uid.Comp.CorruptItemAction);
        _actions.AddAction(uid, ref uid.Comp.CorruptItemInHandActionEntity, uid.Comp.CorruptItemInHandAction);
        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }

    private void UpdateStage(EntityUid uid, CultYoggComponent component)
    {
        //rework stage's update for cultist's
        if (!HasComp<CultYoggComponent>(uid))
        {
            return;
        }
        if (component.CurrentStage >= 1)
        {
            if (TryComp<HumanoidAppearanceComponent>(uid, out var huAp))
            {
                huAp.EyeColor = Color.Green; // eye color
                Dirty(uid, huAp);
                // need reworking for eyes sprite
            }
        }

        if (component.CurrentStage == 2)
        {
            // if u need use the golden crown from proto

            /*

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (!prototypeManager.TryIndex<StartingGearPrototype>("CultGear", out var startingGear))
                return;

            if (_inventory.TryGetSlots(uid, out var slotDefinitions))
            {
                foreach (var slot in slotDefinitions)
                {
                    var equipmentStr = ((IEquipmentLoadout) startingGear).GetGear(slot.Name);
                    if (!string.IsNullOrEmpty(equipmentStr))
                    {
                        _inventory.TryUnequip(uid, slot.Name, true, true);
                        var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
                        _inventory.TryEquip(uid, equipmentEntity, slot.Name, silent: true, force: true);
                    }
                }
            }
            */
        }
    }

    private void OnExamined(EntityUid uid, CultYoggComponent component, ExaminedEvent args)
    {
        if (component.CurrentStage == 0)
        {
            return;
        }
        if (TryComp<InventoryComponent>(uid, out var item)
            && _inventory.TryGetSlotEntity(uid, "eyes", out _, item))
        {
            return;
        }

        if (_inventory.TryGetSlotEntity(uid, "head", out var itemHead, item))
        {
            if (TryComp(itemHead, out IdentityBlockerComponent? block)
                && (block.Coverage == IdentityBlockerCoverage.EYES || block.Coverage == IdentityBlockerCoverage.FULL))
            {
                return;
            }
        }

        if (_inventory.TryGetSlotEntity(uid, "mask", out var itemMask, item))
        {
            if (TryComp(itemMask, out IdentityBlockerComponent? block)
                && (block.Coverage == IdentityBlockerCoverage.EYES || block.Coverage == IdentityBlockerCoverage.FULL))
            {
                return;
            }
        }

        args.PushMarkup($"[color=green]{Loc.GetString("Глаза горят неестественно зелёным пламенем", ("ent", uid))}[/color]"); // no locale for right now
    }

    #region Puke
    private void PukeAction(Entity<CultYoggComponent> uid, ref CultYoggPukeShroomEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsClient) // Have to do this because spawning stuff in shared is CBT.
            return;

        _entityManager.SpawnEntity(uid.Comp.PukedLiquid, Transform(uid).Coordinates);
        var shroom = _entityManager.SpawnEntity(uid.Comp.PukedEntity, Transform(uid).Coordinates);
        _audio.PlayPredicted(uid.Comp.PukeSound, uid, shroom);

        args.Handled = true;

        _actions.RemoveAction(uid, uid.Comp.PukeShroomActionEntity);
        _actions.AddAction(uid, ref uid.Comp.DigestActionEntity, uid.Comp.DigestAction);
    }
    private void DigestAction(Entity<CultYoggComponent> uid, ref CultYoggDigestEvent args)
    {
        if (TryComp<HungerComponent>(uid, out var hungerComp)
        && _hungerSystem.IsHungerBelowState(uid, uid.Comp.MinHungerThreshold, hungerComp.CurrentHunger - uid.Comp.HungerCost, hungerComp))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-digest-no-nutritions"), uid);
            //_popup.PopupClient(Loc.GetString("cult-yogg-digest-no-nutritions"), uid, uid);//idk if it isn't working, but OnSericultureStart is an ok
            return;
        }

        _hungerSystem.ModifyHunger(uid, -uid.Comp.HungerCost);

        //maybe add thist
        /*
        if (TryComp<ThirstComponent>(uid, out var thirst))
            _thirst.ModifyThirst(uid, thirst, thirstAdded);
        */

        _actions.RemoveAction(uid, uid.Comp.DigestActionEntity);//if we digested, we should puke after

        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }
    #endregion

    #region Corruption
    private void CorruptItemAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemEvent args)
    {
        if (args.Handled)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), args.Target, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, args.Target, false))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }
        args.Handled = true;
    }

    private void CorruptItemInHandAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_entityManager.TryGetComponent<HandsComponent>(uid, out var hands))
            return;

        if (hands.ActiveHand == null)
            return;

        var handItem = hands.ActiveHand.HeldEntity;
        if (handItem == null)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(handItem.Value))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), handItem.Value, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, handItem.Value, true))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }
        args.Handled = true;
    }
    #endregion

    #region Ascending
    private void AscendingAction(Entity<CultYoggComponent> uid, ref CultYoggAscendingEvent args)
    {
        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(uid.Comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
            _body.GibBody(uid, body: body);
    }
    #endregion
}
