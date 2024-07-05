// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.Chemistry.ReactionEffects
{
    /// <summary>
    /// Used when someone eats MiGoShroom
    /// </summary>
    [UsedImplicitly]
    public sealed partial class MiGomiceliumEffect : ReagentEffect //stub as vomit, will change when figure out
    {
        /// <summary>
        /// Minimum quantity of reagent required to trigger this effect.
        /// </summary>
        [DataField]
        public float AmountThreshold = 0.5f;

        /// How many units of thirst to add each time we vomit
        [DataField]
        public float ThirstAmount = -8f;
        /// How many units of hunger to add each time we vomit
        [DataField]
        public float HungerAmount = -8f;

        public override void Effect(ReagentEffectArgs args)
        {

            if (args.Reagent == null || args.Quantity < AmountThreshold)
                return;

            var entityManager = args.EntityManager;

            
            if (entityManager.TryGetComponent<CultYoggComponent>(args.SolutionEntity, out var comp))
            {
                args.EntityManager.System<SharedCultYoggSystem>().ModifyEtaenShrooms(args.SolutionEntity, comp);
                return;
            }

            if (entityManager.HasComponent<HumanoidAppearanceComponent>(args.SolutionEntity))
            {
                return;
            }

            var vomitSys = entityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.SolutionEntity, ThirstAmount, HungerAmount);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-phalanximine", ("chance", Probability));
    }
}
