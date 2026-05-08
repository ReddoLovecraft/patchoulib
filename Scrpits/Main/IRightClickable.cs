using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Patchouib.Scrpits.Main
{
    public interface IRightCilckable
    {

        public abstract Task OnRightClick(PlayerChoiceContext context);

    }

    public interface IRightClickableCardModel
    {
        List<PileType> Pile { get; }
        bool IsCombat { get; }
        Task OnRightClick(PlayerChoiceContext context);
    }
}
