using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

public interface IForseenListener
{
    public virtual async Task TriggerWhenForseen(PlayerChoiceContext context,Player player,List<CardModel> cardsSeen)
    {
        await Task.CompletedTask;
    }
}