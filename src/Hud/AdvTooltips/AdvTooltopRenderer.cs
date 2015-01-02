using System;
using System.Collections.Generic;
using System.Drawing;
using PoeHUD.Framework;
using PoeHUD.Game;
using PoeHUD.Hud.MaxRolls;
using PoeHUD.Poe;
using PoeHUD.Poe.EntityComponents;
using PoeHUD.Poe.FileSystem;
using PoeHUD.Poe.UI;
using PoeHUD.Settings;
using SlimDX.Direct3D9;

namespace PoeHUD.Hud.AdvTooltips
{
	public class AdvTooltopRenderer : HUDPluginBase
	{
		public class RollsSettings : SettingsForModule
		{
			public Setting<bool> ShowImplicitMod = new Setting<bool>("Mod: Implicit", true);
			public Setting<bool> ShowItemMods = new Setting<bool>("Mod: Rolls", true);
			public Setting<bool> ShowItemLevel = new Setting<bool>("Item Level", true);
			public SettingIntRange ItemLevelFontSize = new SettingIntRange("Item Lvl Font", 8, 16, 12);
			public Setting<bool> ShowDps = new Setting<bool>("DPS on weapon", true);
			public SettingIntRange DpsFontSize = new SettingIntRange("DPS font size", 8, 16, 12);

			public SettingIntRange OffsetInnerX = new SettingIntRange("Padding X", 0, 16, 2);
			public SettingIntRange OffsetInnerY = new SettingIntRange("Padding Y", -16, 16, 1);
			public RollsSettings() : base("Item Tooltips") {}
		}
	
		private Entity _lastHovered;
		private List<RollValue> _explicitMods;
		private List<RollValue> _implicitMods;
		private WeaponAttack _weaponAttack;
		private int _quality = 0;
		public RollsSettings Settings = new RollsSettings();

		public override void OnEnable()
		{
			this._explicitMods = new List<RollValue>();
			this._implicitMods = new List<RollValue>();
			_weaponAttack = null;
			this._lastHovered = null;
		}

		public override SettingsForModule SettingsNode
		{
			get { return Settings; }
		}

		public override void Render(RenderingContext rc, Dictionary<UiMountPoint, Vec2> mountPoints)
		{
			Element uiHover = this.model.Internal.IngameState.ElementUnderCursor;

			Tooltip tooltip = uiHover.AsObject<InventoryItemIcon>().Tooltip;
			if (tooltip == null)
				return;
			Element childAtIndex1 = tooltip.GetChildAtIndex(0);
			if (childAtIndex1 == null)
				return;
			Element childAtIndex2 = childAtIndex1.GetChildAtIndex(1);
			if (childAtIndex2 == null)
				return;
			Rect clientRect = childAtIndex2.GetClientRect();


			Entity poeEntity = uiHover.AsObject<InventoryItemIcon>().Item;
			if (poeEntity.Address == 0 || !poeEntity.IsValid)
				return;

			if (this._lastHovered == null || this._lastHovered.ID != poeEntity.ID) {
				this._lastHovered = poeEntity;

				this._explicitMods = new List<RollValue>();
				this._implicitMods = new List<RollValue>();
				int ilvl = poeEntity.GetComponent<Mods>().ItemLevel;
				foreach (ItemMod item in poeEntity.GetComponent<Mods>().ItemMods)
					this._explicitMods.Add(new RollValue(item, model.Files, ilvl));
				foreach (ItemMod item in poeEntity.GetComponent<Mods>().ImplicitMods)
					this._implicitMods.Add(new RollValue(item, model.Files, ilvl, true));

				_quality = poeEntity.GetComponent<Quality>().ItemQuality;

				Weapon weap = poeEntity.GetComponent<Weapon>();
				if (weap.Address != 0)
				{
					var attack = weap.Attack;
					_weaponAttack = new WeaponAttack() { AttackDelay = attack.AttackTime, CritChancePer10K = attack.CritChance, MinDamage = attack.DamageMin, MaxDamage = attack.DamageMax };
				}
				else
					_weaponAttack = null;
			}

			RenderRolls(rc, clientRect);
			
			if( _weaponAttack != null && Settings.ShowDps)
				RenderWeaponStats(rc, clientRect);
			if (Settings.ShowItemLevel)
				RenderItemLevel(rc, clientRect);


		}

		private void RenderRolls(RenderingContext rc, Rect clientRect)
		{
			int yPosTooltil = clientRect.Y + clientRect.H + 5;
			int i = yPosTooltil + 4;

			// Implicit mods
			if( Settings.ShowImplicitMod)
				foreach (RollValue mod in this._implicitMods)
				{
					i = drawStatLine(rc, mod, clientRect, i, true);
					i += 6;
				}

			if (Settings.ShowItemMods)
				foreach (RollValue item in this._explicitMods)
				{
					i = drawStatLine(rc, item, clientRect, i);
					i += 4;
				}

			if (i > yPosTooltil + 4)
			{
				Rect helpRect = new Rect(clientRect.X + 1, yPosTooltil, clientRect.W, i - yPosTooltil);
				rc.AddBox(helpRect, Color.FromArgb(220, Color.Black));
			}
		}

		private static readonly Color[] eleCols = new[] { Color.White, HudSkin.DmgFireColor, HudSkin.DmgColdColor, HudSkin.DmgLightingColor, HudSkin.DmgChaosColor };

		private void RenderWeaponStats(RenderingContext rc, Rect clientRect)
		{
			const int innerPadding = 3;
			float aSpd = ((float)1000) / _weaponAttack.AttackDelay;
			int cntDamages = Enum.GetValues(typeof(DamageType)).Length;
			float[] doubleDpsPerStat = new float[cntDamages];
			float physDmgMultiplier = 1;
			doubleDpsPerStat[(int)DamageType.Physical] = _weaponAttack.MaxDamage + _weaponAttack.MinDamage;
			foreach (RollValue roll in _explicitMods)
			{
				for (int iStat = 0; iStat < 4; iStat++)
				{
					IntRange range = roll.TheMod.StatRange[iStat];
					if (range.Min == 0 && range.Max == 0)
						continue;

					StatsDat.StatRecord theStat = roll.TheMod.StatNames[iStat];
					int val = roll.StatValue[iStat];
					switch (theStat.Key)
					{
						case "physical_damage_+%":
						case "local_physical_damage_+%":
							physDmgMultiplier += val / 100f;
							break;
						case "local_attack_speed_+%":
							aSpd *= (100f + val) / 100;
							break;
						case "local_minimum_added_physical_damage":
						case "local_maximum_added_physical_damage":
							doubleDpsPerStat[(int)DamageType.Physical] += val;
							break;
						case "local_minimum_added_fire_damage":
						case "local_maximum_added_fire_damage":
						case "unique_local_minimum_added_fire_damage_when_in_main_hand":
						case "unique_local_maximum_added_fire_damage_when_in_main_hand":
							doubleDpsPerStat[(int)DamageType.Fire] += val;
							break;
						case "local_minimum_added_cold_damage":
						case "local_maximum_added_cold_damage":
						case "unique_local_minimum_added_cold_damage_when_in_off_hand":
						case "unique_local_maximum_added_cold_damage_when_in_off_hand":
							doubleDpsPerStat[(int)DamageType.Cold] += val;
							break;
						case "local_minimum_added_lightning_damage":
						case "local_maximum_added_lightning_damage":
							doubleDpsPerStat[(int)DamageType.Lightning] += val;
							break;
						case "unique_local_minimum_added_chaos_damage_when_in_off_hand":
						case "unique_local_maximum_added_chaos_damage_when_in_off_hand":
						case "local_minimum_added_chaos_damage":
						case "local_maximum_added_chaos_damage":
							doubleDpsPerStat[(int)DamageType.Chaos] += val;
							break;

						default:
							break;
					}
				}
			}

			doubleDpsPerStat[(int)DamageType.Physical] *= physDmgMultiplier;
			if (_quality > 0)
				doubleDpsPerStat[(int)DamageType.Physical] += (_weaponAttack.MaxDamage + _weaponAttack.MinDamage) * _quality / 100f;
			float pDps = doubleDpsPerStat[(int)DamageType.Physical] / 2 * aSpd;

			float eDps = 0;
			int firstEmg = 0;
			Color eDpsColor = Color.White;
			
			for(int i = 1; i < cntDamages; i++) {
				eDps += doubleDpsPerStat[i] / 2 * aSpd;
				if (doubleDpsPerStat[i] > 0)
				{
					if (firstEmg == 0)
					{
						firstEmg = i;
						eDpsColor = eleCols[i];
					} else
					{
						eDpsColor = Color.DarkViolet;
					}
				}
			}

			Vec2 sz = new Vec2();
			if (pDps > 0)
				sz = rc.AddTextWithHeight(new Vec2(clientRect.X + clientRect.W - Settings.OffsetInnerX, clientRect.Y + Settings.OffsetInnerY), pDps.ToString("#.#"), Color.White, Settings.DpsFontSize, DrawTextFormat.Right);
			Vec2 sz2 = new Vec2();
			if( eDps > 0 )
				sz2 = rc.AddTextWithHeight(new Vec2(clientRect.X + clientRect.W - Settings.OffsetInnerX, clientRect.Y + Settings.OffsetInnerY + sz.Y), eDps.ToString("#.#"), eDpsColor, Settings.DpsFontSize, DrawTextFormat.Right);
			rc.AddTextWithHeight(new Vec2(clientRect.X + clientRect.W - Settings.OffsetInnerX, clientRect.Y + Settings.OffsetInnerY + sz.Y + sz2.Y), "DPS", Color.White, 8, DrawTextFormat.Right);

		}


		private void RenderItemLevel(RenderingContext rc, Rect clientRect)
		{
			string text = _lastHovered.GetComponent<Mods>().ItemLevel.ToString();
#if DEBUG
			text += " @ " + _lastHovered.Address.ToString("X8");
#endif
			rc.AddTextWithHeight(new Vec2(clientRect.X + Settings.OffsetInnerX, clientRect.Y + Settings.OffsetInnerY), text, Color.White, Settings.ItemLevelFontSize, DrawTextFormat.Left);
		}

		private static int drawStatLine(RenderingContext rc, RollValue roll, Rect clientRect, int yPos, bool isImp = false)
		{
			const int leftRuler = 50;
			Color textColor = isImp ? HudSkin.MagicColor : Color.White;

			bool isUniqAffix = roll.AffixType == ModsDat.ModType.Hidden;
			string prefix = isImp ? "[Impl]" : roll.AffixType == ModsDat.ModType.Prefix
				? "[P]"
				: roll.AffixType == ModsDat.ModType.Suffix ? "[S]" : "[?]";
			if (!isUniqAffix || isImp)
			{
				if( !isImp && roll.CouldHaveTiers())
					prefix += " T" + roll.Tier + " ";

				rc.AddTextWithHeight(new Vec2(clientRect.X + 5, yPos), prefix, textColor, 8, DrawTextFormat.Left);
				var textSize = rc.AddTextWithHeight(new Vec2(clientRect.X + leftRuler, yPos), roll.AffixText, roll.TextColor, 8,
					DrawTextFormat.Left);
				yPos += textSize.Y;
			}

			for (int iStat = 0; iStat < 4; iStat++)
			{
				IntRange range = roll.TheMod.StatRange[iStat];
				if(range.Min == 0 && range.Max == 0)
					continue;

				var theStat = roll.TheMod.StatNames[iStat];
				int val = roll.StatValue[iStat];
				float percents = range.GetPercentage(val);
				bool noSpread = !range.HasSpread();

				double hue = 120 * percents;
				if (noSpread) hue = 300;
				if (percents > 1) hue = 180;
				
				Color col = ColorUtils.ColorFromHsv(hue, 1, 1);

				string line2 = string.Format(noSpread ? "{0}" : "{0} [{1}]", theStat, range);

				rc.AddTextWithHeight(new Vec2(clientRect.X + leftRuler, yPos), line2, Color.White, 8, DrawTextFormat.Left);

				if( null == theStat) // crazy maps
					continue;

				string sValue = theStat.ValueToString(val);
				var txSize = rc.AddTextWithHeight(new Vec2(clientRect.X + leftRuler - 5, yPos), sValue,
					col, 8,
					DrawTextFormat.Right);

				yPos += txSize.Y;
			}
			return yPos;
		}
	}
}