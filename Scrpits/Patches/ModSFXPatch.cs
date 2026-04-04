using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patchoulib.Scrpits.Patches
{
    [HarmonyPatch(typeof(NAudioManager), "PlayOneShot", [typeof(string), typeof(float)])]
    public static class ModSFXPatch
    {
        static bool Prefix(string path, float volume)
        {
            if (path.StartsWith("mod_sfx://"))
            {
                try
                {
                    string resPath = "res://" + path.Substring(10); // 10 is "mod_sfx://".Length
                    var stream = ResourceLoader.Load<AudioStream>(resPath);
                    if (stream != null)
                    {
                        var player = new AudioStreamPlayer();
                        player.Stream = stream;
                        player.VolumeDb = Mathf.LinearToDb(volume);
                        NGame.Instance.AddChild(player);
                        player.Play();
                        player.Connect("finished", Callable.From(player.QueueFree));
                    }
                }
                catch (System.Exception e)
                {
                    Log.Error($"Failed to play mod sfx: {path}. Error: {e.Message}");
                }
                return false; // 拦截原本的 FMOD 播放
            }
            return true;
        }
    }
}
