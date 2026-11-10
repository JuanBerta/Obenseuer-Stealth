using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StealthMod
{
	public class StealthSave : SkillSaveBase
	{
		protected override string SaveGuid { get { return "JuanBerta_Stealth_Save_001"; } }
		protected override string SaveTitle { get { return "Stealth"; } }
	}

	[BepInPlugin("JuanBerta.Obenseuer.Stealth", "Stealth", "1.0.0")]
	public class StealthPlugin : BaseUnityPlugin
	{
		internal static CustomSkill Skill;

		// XP
		internal static ConfigEntry<float> XpPerTheft;
		internal static ConfigEntry<float> XpPerLockpick;
		internal static ConfigEntry<float> XpPerTrespassMinute;
		internal static ConfigEntry<float> MaxTrespassXpPerArea;
		internal static ConfigEntry<float> DailyDiminishing;
		internal static ConfigEntry<float> MaxDrunkXpBonus;
		// Detection
		internal static ConfigEntry<float> ReductionPerLevel;
		internal static ConfigEntry<float> MaxReduction;
		internal static ConfigEntry<bool> AffectsCameras;
		internal static ConfigEntry<bool> ShorterSightRange;
		internal static ConfigEntry<float> SightRangeReductionAtMax;
		internal static ConfigEntry<bool> CrouchHelps;
		internal static ConfigEntry<float> CrouchBonus;
		internal static ConfigEntry<bool> DarknessHelps;
		internal static ConfigEntry<float> DarknessBonus;
		internal static ConfigEntry<int> NightStartHour;
		internal static ConfigEntry<int> NightEndHour;
		internal static ConfigEntry<bool> DrunkIsClumsy;
		internal static ConfigEntry<float> DrunkPenalty;
		// Perk
		internal static ConfigEntry<bool> Shadow;
		internal static ConfigEntry<float> ShadowSeverityCut;

		private void Awake()
		{
			Skill = new CustomSkill("Stealth");
			Skill.BindCommon(Config);
			Bind(Config);
			Skill.CardNote = () => "-" + Mathf.RoundToInt((1f - DetectionFactor(false)) * 100f) + "% seen now";
			SkillHost.Init(this, Logger, Skill, "Stealth_Save", go => go.AddComponent<StealthSave>());
			Harmony.CreateAndPatchAll(typeof(Hooks));
			PatchFov(new Harmony("JuanBerta.Obenseuer.Stealth.fov"));
			SceneManager.sceneLoaded += (s, m) => areas = null;
			Logger.LogInfo("Stealth loaded.");
		}

		private static void Bind(ConfigFile cfg)
		{
			XpPerTheft = cfg.Bind("Skill", "XpPerTheft", 10f, CustomSkill.D("Base XP for taking something that isn't yours without anyone seeing. Valuable things give more, up to 3 times this.", 90, new AcceptableValueRange<float>(0f, 200f)));
			XpPerLockpick = cfg.Bind("Skill", "XpPerLockpick", 15f, CustomSkill.D("Base XP for picking a lock without being seen. Harder locks give more.", 88, new AcceptableValueRange<float>(0f, 200f)));
			XpPerTrespassMinute = cfg.Bind("Skill", "XpPerTrespassMinute", 6f, CustomSkill.D("XP per minute spent unseen in a place you're not allowed to be.", 86, new AcceptableValueRange<float>(0f, 100f)));
			MaxTrespassXpPerArea = cfg.Bind("Skill", "MaxTrespassXpPerArea", 30f, CustomSkill.D("Most trespassing XP one area can give in an in-game day.", 84, new AcceptableValueRange<float>(0f, 500f)));
			DailyDiminishing = cfg.Bind("Skill", "DailyDiminishing", 0.03f, CustomSkill.D("Each theft today gives a bit less XP than the one before. Resets each in-game day.", 60, new AcceptableValueRange<float>(0f, 1f)));
			MaxDrunkXpBonus = cfg.Bind("State", "MaxDrunkXpBonus", 1f, CustomSkill.D("Extra XP when drunk, scaled by alcohol in blood (1 = up to +100%). Getting away with it drunk is harder, so you learn more.", 90, new AcceptableValueRange<float>(0f, 3f)));

			ReductionPerLevel = cfg.Bind("Detection", "ReductionPerLevel", 0.05f, CustomSkill.D("How much less likely people and cameras are to notice you, per Stealth level (0.05 = 5% per level, 50% at level 10).", 100, new AcceptableValueRange<float>(0f, 0.1f)));
			MaxReduction = cfg.Bind("Detection", "MaxReduction", 0.75f, CustomSkill.D("The most detection can be reduced, with level, crouching and darkness together.", 95, new AcceptableValueRange<float>(0f, 0.95f)));
			AffectsCameras = cfg.Bind("Detection", "AffectsCameras", true, CustomSkill.D("Stealth also works against security cameras.", 90));
			ShorterSightRange = cfg.Bind("Detection", "ShorterSightRange", true, CustomSkill.D("People notice you from closer only: their sight range shrinks with your Stealth level.", 85));
			SightRangeReductionAtMax = cfg.Bind("Detection", "SightRangeReductionAtMax", 0.3f, CustomSkill.D("How much shorter people's sight range is at level 10 (0.3 = 30% shorter).", 84, new AcceptableValueRange<float>(0f, 0.7f)));
			CrouchHelps = cfg.Bind("Detection", "CrouchHelps", true, CustomSkill.D("Crouching makes you harder to notice (needs at least level 1).", 80));
			CrouchBonus = cfg.Bind("Detection", "CrouchBonus", 0.15f, CustomSkill.D("Extra detection reduction while crouching.", 79, new AcceptableValueRange<float>(0f, 0.5f)));
			DarknessHelps = cfg.Bind("Detection", "DarknessHelps", true, CustomSkill.D("Outdoors at night without a light, you're harder to notice.", 75));
			DarknessBonus = cfg.Bind("Detection", "DarknessBonus", 0.15f, CustomSkill.D("Extra detection reduction in the dark.", 74, new AcceptableValueRange<float>(0f, 0.5f)));
			NightStartHour = cfg.Bind("Detection", "NightStartHour", 21, CustomSkill.D("Hour when it gets dark.", 73, new AcceptableValueRange<int>(0, 23)));
			NightEndHour = cfg.Bind("Detection", "NightEndHour", 6, CustomSkill.D("Hour when it gets light.", 72, new AcceptableValueRange<int>(0, 23)));
			DrunkIsClumsy = cfg.Bind("State", "DrunkIsClumsy", true, CustomSkill.D("Being drunk makes you easier to notice (the Drinking skill makes this smaller).", 80));
			DrunkPenalty = cfg.Bind("State", "DrunkPenalty", 0.5f, CustomSkill.D("How much easier to notice you are when completely drunk (0.5 = 50% more).", 79, new AcceptableValueRange<float>(0f, 2f)));

			Shadow = cfg.Bind("Perk", "Shadow", true, CustomSkill.D("Level 10 perk: when you do get caught for theft, burglary or trespassing, the crime counts as less serious.", 100));
			ShadowSeverityCut = cfg.Bind("Perk", "ShadowSeverityCut", 0.3f, CustomSkill.D("How much less serious (0.3 = 30% less).", 90, new AcceptableValueRange<float>(0f, 1f)));
		}

		// ---- Detection ----

		private static PropertyInfo crouchProp;
		private static UnityEngine.Object controller;
		private static float nextControllerLookup;

		internal static bool Crouching
		{
			get
			{
				try
				{
					if (controller == null && Time.unscaledTime >= nextControllerLookup)
					{
						nextControllerLookup = Time.unscaledTime + 2f;
						Type t = AccessTools.TypeByName("OpenSewerCharacterController");
						if (t != null)
						{
							controller = AccessTools.Field(t, "instance") != null ? AccessTools.Field(t, "instance").GetValue(null) as UnityEngine.Object : null;
							crouchProp = AccessTools.Property(t, "isCrouching");
						}
					}
					return controller != null && crouchProp != null && (bool)crouchProp.GetValue(controller, null);
				}
				catch (Exception) { return false; }
			}
		}

		private static float darkCheckedAt = -10f;
		private static bool darkCached;

		internal static bool InDark
		{
			get
			{
				if (!DarknessHelps.Value) return false;
				if (Time.unscaledTime - darkCheckedAt < 1f) return darkCached;
				darkCheckedAt = Time.unscaledTime;
				string scene = SceneManager.GetActiveScene().name ?? "";
				darkCached = scene.StartsWith("Open Sewer") && SkillHost.IsNight(NightStartHour.Value, NightEndHour.Value) && !SkillHost.HasLight();
				return darkCached;
			}
		}

		// Multiplier on the chance that someone notices you (1 = no change).
		internal static float DetectionFactor(bool camera)
		{
			if (!Skill.Enabled.Value || !Skill.Learned) return 1f;
			if (camera && !AffectsCameras.Value) return 1f;
			float reduction = ReductionPerLevel.Value * Skill.EffectiveLevel;
			if (CrouchHelps.Value && !camera && Crouching) reduction += CrouchBonus.Value;
			if (InDark) reduction += DarknessBonus.Value;
			reduction = Mathf.Clamp(reduction, 0f, MaxReduction.Value);
			float factor = 1f - reduction;
			if (DrunkIsClumsy.Value) factor *= 1f + DrunkPenalty.Value * Skill.Drunkenness;
			return Mathf.Clamp(factor, 0.05f, 2f);
		}

		internal static float SightRangeFactor
		{
			get
			{
				if (!ShorterSightRange.Value || !Skill.Enabled.Value || !Skill.Learned) return 1f;
				return 1f - SightRangeReductionAtMax.Value * Skill.EffectiveLevel / CustomSkill.MaxLevel;
			}
		}

		// NPC_FOV is in the game's NPC namespace; patch it by name so a rename doesn't break the mod.
		private static FieldInfo fovFocus, fovEdge, fovRange;

		private static void PatchFov(Harmony h)
		{
			try
			{
				Type fov = AccessTools.TypeByName("NPC.NPC_FOV") ?? AccessTools.TypeByName("NPC_FOV");
				if (fov == null) { SkillHost.LogWarning("NPC_FOV not found: only the witness chance is changed."); return; }
				fovFocus = AccessTools.Field(fov, "detectionPropabilityFocus");
				fovEdge = AccessTools.Field(fov, "detectionPropabilityEdge");
				fovRange = AccessTools.Field(fov, "maxDistance");
				MethodInfo m = AccessTools.Method(fov, "CheckIfTargetIsDetected", new[] { typeof(Vector3), typeof(bool), typeof(bool) });
				if (m == null || fovFocus == null || fovEdge == null || fovRange == null) { SkillHost.LogWarning("NPC_FOV fields not found."); return; }
				h.Patch(m, new HarmonyMethod(typeof(StealthPlugin), "Fov_Prefix"), new HarmonyMethod(typeof(StealthPlugin), "Fov_Postfix"));
			}
			catch (Exception e) { SkillHost.LogWarning("NPC sight patch failed: " + e.Message); }
		}

		private struct FovState { public bool set; public float focus, edge, range; }

		private static void Fov_Prefix(object __instance, bool player, out FovState __state)
		{
			__state = new FovState();
			if (!player) return;
			try
			{
				float f = DetectionFactor(false), r = SightRangeFactor;
				if (f >= 0.999f && r >= 0.999f) return;
				__state.set = true;
				__state.focus = (float)fovFocus.GetValue(__instance);
				__state.edge = (float)fovEdge.GetValue(__instance);
				__state.range = (float)fovRange.GetValue(__instance);
				fovFocus.SetValue(__instance, __state.focus * f);
				fovEdge.SetValue(__instance, __state.edge * f);
				fovRange.SetValue(__instance, __state.range * r);
			}
			catch (Exception) { __state.set = false; }
		}

		private static void Fov_Postfix(object __instance, FovState __state)
		{
			if (!__state.set) return;
			try
			{
				fovFocus.SetValue(__instance, __state.focus);
				fovEdge.SetValue(__instance, __state.edge);
				fovRange.SetValue(__instance, __state.range);
			}
			catch (Exception) { }
		}

		// ---- XP: unseen thefts (collected per frame, awarded once) ----

		private static int theftDepth;
		private static float theftValue;
		private static int pendingFrame = -1;
		private static bool pendingSeen;
		private static float pendingXp;

		private static void Candidate(float xp, bool seen)
		{
			int frame = Time.frameCount;
			if (frame != pendingFrame) { Flush(); pendingFrame = frame; pendingSeen = false; pendingXp = 0f; }
			pendingSeen |= seen;
			pendingXp = Mathf.Max(pendingXp, xp);
		}

		private static void Flush()
		{
			if (pendingFrame < 0 || pendingSeen || pendingXp <= 0f || SkillHost.Save == null) { pendingFrame = -1; return; }
			pendingFrame = -1;
			float count = SkillHost.Save.AddDaily("thefts", 1f);
			float fresh = 1f / (1f + DailyDiminishing.Value * (count - 1f));
			Award(pendingXp * fresh, "unseen theft");
		}

		private static void Award(float xp, string what)
		{
			float drunk = 1f + MaxDrunkXpBonus.Value * Skill.Alcohol / 100f;
			var notes = new List<string> { what };
			if (Crouching) notes.Add("crouching");
			if (InDark) notes.Add("in the dark");
			if (drunk > 1.01f) notes.Add("drunk +" + Mathf.RoundToInt((drunk - 1f) * 100f) + "%");
			Skill.AddXp(xp * drunk, string.Join(", ", notes.ToArray()));
		}

		private static float TheftXp(float value)
		{
			return XpPerTheft.Value * Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0f, value)) / 5f, 0.5f, 3f);
		}

		// ---- XP: lockpicking ----

		private static bool lockpicking;
		private static bool lockpickSeen;
		private static float lockpickDifficulty;

		// ---- XP: trespassing (checked once a second) ----

		private static List<TrespassingArea> areas;
		private static readonly Dictionary<TrespassingArea, float> unseenSeconds = new Dictionary<TrespassingArea, float>();
		private static float nextAreaCheck;
		private static readonly FieldInfo trespassingField = AccessTools.Field(typeof(TrespassingArea), "trespassing");
		private static readonly FieldInfo detectedField = AccessTools.Field(typeof(TrespassingArea), "detected");

		private void Update()
		{
			SkillHost.Tick();
			if (pendingFrame >= 0 && pendingFrame != Time.frameCount) Flush();
			if (Time.unscaledTime < nextAreaCheck) return;
			nextAreaCheck = Time.unscaledTime + 1f;
			try { CheckTrespass(); } catch (Exception e) { SkillHost.LogWarning("Stealth trespass: " + e.Message); }
		}

		private static void CheckTrespass()
		{
			if (!Skill.Enabled.Value || !SkillHost.InGame || XpPerTrespassMinute.Value <= 0f || Time.timeScale <= 0f) return;
			if (areas == null)
			{
				areas = new List<TrespassingArea>(UnityEngine.Object.FindObjectsByType<TrespassingArea>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
				unseenSeconds.Clear();
			}
			if (trespassingField == null || detectedField == null) return;
			foreach (TrespassingArea area in areas)
			{
				if (area == null || area.AreaDisabled) continue;
				bool inside = (bool)trespassingField.GetValue(area);
				bool detected = (bool)detectedField.GetValue(area);
				if (!inside || detected) { unseenSeconds.Remove(area); continue; }
				float s;
				unseenSeconds.TryGetValue(area, out s);
				s += 1f;
				if (s >= 20f)
				{
					s = 0f;
					string key = "tres@" + area.gameObject.scene.name + "@" + Mathf.RoundToInt(area.transform.position.x) + "," + Mathf.RoundToInt(area.transform.position.z);
					float given = SkillHost.Save.GetDaily(key);
					float xp = Mathf.Min(XpPerTrespassMinute.Value / 3f, MaxTrespassXpPerArea.Value - given);
					if (xp > 0f)
					{
						SkillHost.Save.SetDaily(key, given + xp);
						Award(xp, "unseen where you shouldn't be");
					}
				}
				unseenSeconds[area] = s;
			}
		}

		internal static class Hooks
		{
			// Witness chance: scaled while the game rolls it, then put back.
			[HarmonyPatch(typeof(Witness), "Witnessed")]
			[HarmonyPrefix]
			private static void Witnessed_Prefix(Witness __instance, out float __state)
			{
				__state = __instance.propability;
				try { __instance.propability = __state * DetectionFactor(__instance.GetComponent<SurveilanceCamera>() != null); }
				catch (Exception) { }
			}

			[HarmonyPatch(typeof(Witness), "Witnessed")]
			[HarmonyPostfix]
			private static void Witnessed_Postfix(Witness __instance, float __state)
			{
				__instance.propability = __state;
			}

			[HarmonyPatch(typeof(Witness), "NpcSeesPlayer")]
			[HarmonyPrefix]
			private static void Sees_Prefix(Witness __instance, out float __state)
			{
				__state = __instance.propability;
				try { __instance.propability = __state * DetectionFactor(false); }
				catch (Exception) { }
			}

			[HarmonyPatch(typeof(Witness), "NpcSeesPlayer")]
			[HarmonyPostfix]
			private static void Sees_Postfix(Witness __instance, float __state)
			{
				__instance.propability = __state;
			}

			// Taking an owned item into your inventory: the game checks for witnesses right here.
			[HarmonyPatch(typeof(Inventory), "CheckForAddedStolenItems")]
			[HarmonyPrefix]
			private static void Stolen_Prefix(Item item, int amount)
			{
				theftDepth++;
				try { theftValue = item != null ? item.GetItemSalePriceUnrounded() * Mathf.Max(1, amount) : 0f; }
				catch (Exception) { theftValue = 0f; }
			}

			[HarmonyPatch(typeof(Inventory), "CheckForAddedStolenItems")]
			[HarmonyPostfix]
			private static void Stolen_Postfix()
			{
				theftDepth = Mathf.Max(0, theftDepth - 1);
			}

			// Every theft-like check (owned items, furniture, notes, liquids...) asks IsWitnessed with a target.
			[HarmonyPatch(typeof(Witness), "IsWitnessed")]
			[HarmonyPostfix]
			private static void IsWitnessed_Postfix(bool invoke, WitnessType type, GameObject target, bool __result)
			{
				try
				{
					if (!invoke || type != WitnessType.Crime || !Skill.Enabled.Value || !SkillHost.InGame) return;
					if (theftDepth > 0) Candidate(TheftXp(theftValue), __result);
					else if (target != null) Candidate(TheftXp(0f), __result);
					else if (lockpicking && __result) lockpickSeen = true;
				}
				catch (Exception) { }
			}

			[HarmonyPatch(typeof(LockpickUI), "StartLockpick", new Type[] { typeof(float), typeof(float) })]
			[HarmonyPrefix]
			private static void StartLockpick_Prefix(float difficulty)
			{
				lockpicking = true;
				lockpickSeen = false;
				lockpickDifficulty = difficulty;
			}

			[HarmonyPatch(typeof(LockpickUI), "WasSuccess")]
			[HarmonyPostfix]
			private static void WasSuccess_Postfix(bool __result)
			{
				if (!lockpicking || !__result) return;
				lockpicking = false;
				if (lockpickSeen || !Skill.Enabled.Value) return;
				try { Award(XpPerLockpick.Value * Mathf.Clamp(lockpickDifficulty, 0.5f, 3f), "unseen lockpick"); }
				catch (Exception) { }
			}

			[HarmonyPatch(typeof(Crime), "CommitCrime", new Type[] { typeof(NPC.NPCReference), typeof(int), typeof(Crime.CrimeType), typeof(bool) })]
			[HarmonyPrefix]
			private static void CommitCrime_Prefix(ref int severity, Crime.CrimeType type)
			{
				if (type == Crime.CrimeType.Burglary && lockpicking) lockpickSeen = true;
				if (!Shadow.Value || Skill.Level < CustomSkill.MaxLevel) return;
				if (type == Crime.CrimeType.Theft || type == Crime.CrimeType.Burglary || type == Crime.CrimeType.Trespassing)
					severity = Mathf.Max(1, Mathf.RoundToInt(severity * (1f - ShadowSeverityCut.Value)));
			}
		}
	}
}
