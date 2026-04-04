using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
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
            public static HoverTip GetStaticKeyword(string entry)
            {
                string text = StringHelper.Slugify(entry);
                LocString locString = L10NStatic(text + ".title");
                LocString locString2 = L10NStatic(text + ".description");
                return new HoverTip(locString, locString2);
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
