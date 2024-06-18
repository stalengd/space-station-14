// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;


namespace Content.Server.SS220.Chemistry.ReactionEffects
{
    /// <summary>
    /// Used when someone eats MiGoShroom
    /// </summary>
    [UsedImplicitly]
    public sealed partial class MiGomiceliumEffect : ReagentEffect //stub as vomit, will change when figure out
    {

        /// How many units of thirst to add each time we vomit
        [DataField]
        public float ThirstAmount = -8f;
        /// How many units of hunger to add each time we vomit
        [DataField]
        public float HungerAmount = -8f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-chem-vomit", ("chance", Probability));
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.SolutionEntity, ThirstAmount, HungerAmount);
        }
    }
}
