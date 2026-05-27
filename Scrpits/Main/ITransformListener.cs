using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Patchouib.Scrpits.Main
{
    public interface ITransformListener
    {
        Task AfterCardTransformed(PlayerChoiceContext choiceContext, CardModel transformedCard, CardModel resultCard, Creature player);
    }
}
