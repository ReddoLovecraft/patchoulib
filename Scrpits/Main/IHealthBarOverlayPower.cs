using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Patchouib.Scrpits.Main
{
    public interface IHealthBarOverlayPower
    {
        int GetHealthBarOverlayValue(Creature owner);
        Color GetHealthBarOverlayColor();
        bool IsOverlayFromEnd();
        bool IsOverlayLethal(Creature owner);
    }
    public abstract class HealthBarOverlayPowerModel : PowerModel, IHealthBarOverlayPower
    {
        public virtual int GetHealthBarOverlayValue(Creature owner)
        {
            return Amount;
        }

        public abstract Color GetHealthBarOverlayColor();

        public virtual bool IsOverlayFromEnd()
        {
            return true;
        }

        public virtual bool IsOverlayLethal(Creature owner)
        {
            int overlayValue = GetHealthBarOverlayValue(owner);
            return overlayValue >= owner.CurrentHp;
        }
    }
}
