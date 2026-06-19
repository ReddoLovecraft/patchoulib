using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Patchoulib.Scrpits.Main
{
    public class PatchoulibCardModifier 
    {
        [CustomEnum("CANNOT_ESCAPE")]
        [KeywordProperties(AutoKeywordPosition.After)]
        public static CardKeyword CannotEscapeKeyword;
    }
}