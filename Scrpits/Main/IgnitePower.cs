using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Patchouib.Scrpits.Main
{
    public sealed class IgnitePower : HealthBarOverlayPowerModel
    {
        public override PowerType Type => PowerType.Debuff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

        public override string? CustomPackedIconPath => "res://Patchoulib/ArtWorks/Powers/IGNITE32.png";
        public override string? CustomBigIconPath => "res://Patchoulib/ArtWorks/Powers/IGNITE64.png";

        public IgnitePower()
        {
        }

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != Owner.Side || !participants.Contains(Owner))
            {
                return;
            }
            await TriggerEffect();
        }

        public async Task TriggerEffect()
        {
            Flash();
            await IgniteIntegration.Run(this);

            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            if (Owner.IsAlive)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, 1, null, null);
            }
            else
            {
                await Cmd.CustomScaledWait(0.1f, 0.25f);
            }
        }

        public override Color GetHealthBarOverlayColor()
        {
            return new Color(0.641f, 0.219f, 0.0f, 1.0f);
        }
    }
}

