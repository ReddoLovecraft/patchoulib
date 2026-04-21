using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Patchouib.Scrpits.Main
{
     //示例Power
    public class ExampleHealthBarOverlayPower : HealthBarOverlayPowerModel  
    {
        public override PowerType Type => PowerType.Debuff;
        public override PowerStackType StackType => PowerStackType.Counter;

        //这里自定义颜色为蓝色
        public override Color GetHealthBarOverlayColor()
        {
            return new Color(0.2f, 0.4f, 0.8f, 1f); 
        }
        // 可以重写 GetHealthBarOverlayValue 来自定义比例
        // 例如：让覆盖值是 Amount 的两倍
        public override int GetHealthBarOverlayValue(Creature owner)
        {
            return Amount * 2; 
        }
        // 如果需要，也可以重写 IsOverlayFromEnd 或 IsOverlayLethal来决定渲染血条的长度和位置（从哪头开始施工）
    }
}
