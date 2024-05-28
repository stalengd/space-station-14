// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Nutrition;

namespace Content.Shared.SS220.Cult;

public abstract class SharedFoodBehaviourSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodBehaviourComponent, ConsumeDoAfterEvent>(OnDoAfter);
    }
    private void OnDoAfter(Entity<FoodBehaviourComponent> entity, ref ConsumeDoAfterEvent args)
    {
        ;
    }
}
