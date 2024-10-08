using BepInEx;
using BepInEx.Configuration;
using BoplFixedMath;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CameraPlus
{

    [BepInPlugin("com.PizzaMan730.CameraPlus", "CameraPlus", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {

		public static ConfigFile config;

		public static ConfigEntry<int> cameraType;

        private void Awake()
        {
			/*
			Camera types:
			1 - Normal
			2 - Static
			3 - Custom
			4 - Custom, No Vertical
			5 - Follow Local (Custom)
			6 - Follow Local (Custom, No Vertical)
			*/
			Plugin.config = base.Config;
			Plugin.cameraType = Plugin.config.Bind<int>("Camera Plus", "Type", 1, "Camera type to use. 1 = Normal, 2 = Static, 3 = Custom, 4 = Custom (No Vertical), 5 = Follow Local (Custom), 6 = Follow Local (Custom, No Vertical)");
			

            Logger.LogInfo("CameraPlus has loaded!");

            Harmony harmony = new Harmony("com.PizzaMan730.CameraPlus");
            
			MethodInfo original = AccessTools.Method(typeof(PlayerAverageCamera), "UpdateCamera");
            MethodInfo patch = AccessTools.Method(typeof(Patches), "UpdateCamera");
			if (cameraType.Value != 1) harmony.Patch(original, new HarmonyMethod(patch));
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        
        public static bool UpdateCamera(ref PlayerAverageCamera __instance, ref Camera ___camera)
        {
			int cameraType = Plugin.cameraType.Value;
			if (cameraType == 2) return false;

			//Get all the players
            List<Player> list = PlayerHandler.Get().PlayerList();
            int count = 0;
            Vec2 pos = new Vec2();
			Vec2 localPlayerPos = new Vec2();
            //Go through each player
            foreach (Player player in list)
            {
                //Make sure the current player is alive before using them
                if (player.IsAlive)
                {
                    //Add 1 to the player count and add the position to the total position
                    pos += player.Position;
                    count++;
					if (player.IsLocalPlayer) localPlayerPos = player.Position;
                }
            }
			if (cameraType == 5 || cameraType == 6)
			{
				pos = localPlayerPos;
				count = 1;
			}
            //Find the average of the position
            pos.x /= (Fix)count;
            pos.y /= (Fix)count;
            //Turn it into a Vector3
            Vector3 pos2 = new Vector3((float)pos.x, (float)pos.y, __instance.transform.position.z);
            //Easing stuff
            pos2 = __instance.transform.position + (pos2 - __instance.transform.position) * (1 - Mathf.Pow(1 - 0.005f, 2));
            //Clamp so that the water doesn't look weird
            pos2.y = Mathf.Max(__instance.MinHeightAboveFloor - 15, pos2.y);
            //idk
            pos2.x = __instance.RoundToNearestPixel(pos2.x); 
            pos2.y = __instance.RoundToNearestPixel(pos2.y);
            //Actually update the position of the camera properly
			if (cameraType == 4 || cameraType == 6) pos2.y = __instance.MinHeightAboveFloor - 7.5f;
            __instance.transform.position = pos2;
            return false;
        }
    }
}

// dotnet build "C:\Users\ajarc\BoplMods\CameraPlus\CameraPlus.csproj"