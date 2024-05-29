// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Nutrition;
using Content.Shared.Humanoid;

namespace Content.Shared.SS220.Cult;

public abstract class SharedFoodBehaviourSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodBehaviourComponent, ConsumeDoAfterEvent>(OnDoAfter);
    }
    private void OnDoAfter(Entity<FoodBehaviourComponent> entity, ref ConsumeDoAfterEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            //beast must be transformed
            return;
        }
        if (!_entityManager.TryGetComponent<CultComponent>(args.Target, out var comp))
        {
            //bigure out function to increase amount of consumed shrooms
            return;
        }
    }
}
