using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patchoulib.Scrpits.Main
{

    public abstract class CustomOption : RestSiteOption
    {
        public virtual string TexturePath => "";
        public Texture2D CustomTexture;
        public CustomOption(Player owner)
            : base(owner)
        {
            CustomTexture= GD.Load<Texture2D>(TexturePath);
        }

      
    }
 
}
