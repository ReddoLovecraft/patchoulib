using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patchoulib.Scrpits.Main
{
        public class Tools
        {
            public static LocString L10NStatic(string entry)
            {
                return new LocString("static_hover_tips", entry);
            }
              public static LocString L1oNStatic(string entry,string targetTable="static_hover_tips")
            {
                return new LocString(targetTable, entry);
            }
            public static LocString GetCustomText(string targetTable,string entry,string postfix)
            {
                string text = StringHelper.Slugify(entry);
                LocString res = L1oNStatic(text + postfix, targetTable);
                return res;
            }
            public static HoverTip GetStaticKeyword(string entry)
            {
                string text = StringHelper.Slugify(entry);
                LocString locString = L10NStatic(text + ".title");
                LocString locString2 = L10NStatic(text + ".description");
                return new HoverTip(locString, locString2);
            }
            public static async Task Forseen(PlayerChoiceContext context,Player player,int amount=1)
            {
              List<CardModel> cards = PileType.Draw.GetPile(player).Cards.Take(amount).ToList();
              List<CardModel> list = (await CardSelectCmd.FromSimpleGrid(context, cards, player, new CardSelectorPrefs(GetCustomText("static_hover_tips","forseen",".selectionScreenPrompt"), 0,amount))).ToList();
              foreach (CardModel card in list)
              {
                await CardCmd.Discard(context,card);
              }
              await TriggerWhenForseen(context,player,cards);
            }
            public static async Task TriggerWhenForseen(PlayerChoiceContext context,Player player,List<CardModel> cardsSeen)
            {
                foreach (PowerModel pm in player.Creature.Powers.ToList())
                {
                   if(pm is IForseenListener forseenListener)
                   {
                        await forseenListener.TriggerWhenForseen(context,player,cardsSeen);
                   }
                    else
                    {
                    continue;
                    }
                }
                foreach (RelicModel relic in player.Relics.ToList())
                {
                   if(relic is IForseenListener forseenListener)
                   {
                        await forseenListener.TriggerWhenForseen(context,player,cardsSeen);
                   }
                    else
                    {
                    continue;
                    }
                }
            }
            private const string ModSfxPrefix = "mod_sfx://";

            public static string ToModSfxPath(string localPath)
            {
            return ModSfxPrefix + localPath;
            }
        public static NSpeechBubbleVfx? Talk(String TalkText, Creature speaker, double secondsToDisplay = -1.0, VfxColor vfxColor = VfxColor.White)
        {
            if (speaker.IsDead)
            {
                return null;
            }
            if (secondsToDisplay < 0.0)
            {
                secondsToDisplay = (double)TalkText.Length * 0.08;
            }
            if (secondsToDisplay < 1.5)
            {
                secondsToDisplay = 1.5;
            }
            NSpeechBubbleVfx nSpeechBubbleVfx = NSpeechBubbleVfx.Create(TalkText, speaker, secondsToDisplay, vfxColor);
            if (nSpeechBubbleVfx != null)
            {
                NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(nSpeechBubbleVfx);
            }
            return nSpeechBubbleVfx;
        }
    }

   

}
