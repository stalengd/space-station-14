// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.SS220.CultYogg;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.Chemistry.ReactionEffects
{
    /// <summary>
    /// Used when someone eats MiGoShroom
    /// </summary>
    [UsedImplicitly]
    public sealed partial class MiGomiceliumEffect : EntityEffect //stub as vomit, will change when figure out
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

        [DataField]
        public float Time = 2.0f;
        public override void Effect(EntityEffectBaseArgs args)
        {

        }
        /*
        public override void Effect(EntityEffectReagentArgs args)
        {

            if (args.Reagent == null || args.Quantity < AmountThreshold)
                return;

            var time = Time;
            time *= args.Scale;

            var entityManager = args.EntityManager;

            if (entityManager.TryGetComponent<CultYoggComponent>(args.SolutionEntity, out var comp))
            {
                entityManager.System<SharedCultYoggSystem>().ModifyEatenShrooms(args.SolutionEntity, comp);
                return;
            }
            //ADD here galutination + drunk
            //var vomitSys = entityManager.EntitySysManager.GetEntitySystem<VomitSystem>();//delete this after
            //vomitSys.Vomit(args.SolutionEntity, ThirstAmount, HungerAmount);

            if (!entityManager.HasComponent<HumanoidAppearanceComponent>(args.SolutionEntity)) //if its an animal -- corrupt it
            {
                entityManager.System<CultYoggAnimalCorruptionSystem>().AnimalCorruption(args.SolutionEntity);
            }

            if (entityManager.HasComponent<HumanoidAppearanceComponent>(args.SolutionEntity))
            {
                entityManager.System<SharedRaveSystem>().TryApplyRavenness(args.SolutionEntity, time);
            }
        }
        */
            //DoNot forget to add note in guidebook
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-phalanximine", ("chance", Probability));
    }
}
