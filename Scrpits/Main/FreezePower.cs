using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
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
    public sealed class FreezePower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Debuff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

        public override string? CustomPackedIconPath => "res://Patchoulib/ArtWorks/Powers/FREEZE32.png";
        public override string? CustomBigIconPath => "res://Patchoulib/ArtWorks/Powers/FREEZE64.png";

        public FreezePower()
        {
        }
        public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
            if(dealer ==null||dealer!=base.Owner)
            {
                return 0m;
            }
            if(!props.IsPoweredAttack_())
            {
                return 0m;
            }
            return -Amount;
		}
        public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (participants.Contains(base.Owner))
            {
                Flash();
                await PowerCmd.Decrement(this);
            }
        }
    }
}

