using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace PitchFixer
{
    [HarmonyPatch]
    [BepInPlugin("com.steven.trombone.pitchfixer", "Pitch Fixer", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
		static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        void Awake()
        {
            var harmony = new Harmony("com.steven.trombone.pitchfixer");
            harmony.PatchAll();
        }


		[HarmonyPatch(typeof(GameController), "Update")]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> input)
		{
			var output = input.ToList();

			var fixPitchMethod = typeof(Plugin).GetMethod(nameof(FixPitch), flags);

			var j = 0;
			var count = output.Count();

			for (var i = 0; i < count; i++)
			{
				if (output[++i].opcode == OpCodes.Ldarg_0 && output[++i].opcode == OpCodes.Ldfld && output[++i].opcode == OpCodes.Ldc_R4 && output[++i].opcode == OpCodes.Callvirt && j++ == 2)
				{
					i += 2;

					output.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldfld, typeof(GameController).GetField("currentnotesound", flags)));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldfld, typeof(GameController).GetField("notelinepos", flags)));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldflda, typeof(GameController).GetField("notestartpos", flags)));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldfld, typeof(GameController).GetField("trombclips", flags)));
					output.Insert(i++, new CodeInstruction(OpCodes.Call, fixPitchMethod));
					output.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));

					break;
				}
			}

			return output;
		}

		public static void FixPitch(GameController controller, AudioSource currentnotesound, float[] notelinepos, ref float notestartpos, AudioClipsTromb trombclips)
		{
			var time = currentnotesound.time;

			var closestRealNoteDistance = 9999f;
			var closestRealNote = 0;

			for (var i = 0; i < 15; i++)
			{
				var distanceFromRealNote = Mathf.Abs(notelinepos[i] - controller.pointer.transform.localPosition.y);
				if (distanceFromRealNote < closestRealNoteDistance)
				{
					closestRealNoteDistance = distanceFromRealNote;
					closestRealNote = i;
				}
			}

			notestartpos = notelinepos[closestRealNote];

			if (currentnotesound.clip != trombclips.tclips[Mathf.Abs(closestRealNote - 14)])
			{
				currentnotesound.clip = trombclips.tclips[Mathf.Abs(closestRealNote - 14)];
				currentnotesound.time = time;
				currentnotesound.Play();
			}
		}
	}
}
