// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.SS220.CultYogg;
using Content.Shared.Humanoid;
using Content.Server.EntityEffects.Effects;

namespace Content.Server.SS220.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class CultYoggCleanse : EntityEffect
    {
        [DataField]
        public float Time = 2.0f;
        public override void Effect(EntityEffectBaseArgs args)
        {
            var time = Time;

            var entityManager = args.EntityManager;

            if (!entityManager.HasComponent<CultYoggComponent>(args.TargetEntity))
                return;

            var cleansedComp = entityManager.EnsureComponent<CultYoggCleansedComponent>(args.TargetEntity);
            cleansedComp.BeforeDeclinesTime = Time;//renew time to aviod uncleasing timespan
            if (args is EntityEffectReagentArgs reagentArgs)//ToDo test scaling
                cleansedComp.AmountOfHolyWater += reagentArgs.Scale.Float();
        }
        //ToDos check the guidebook
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-ss220-cult-cleanse", ("chance", Probability));
    }
}
