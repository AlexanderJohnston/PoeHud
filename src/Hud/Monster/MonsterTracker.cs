using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Game;
using PoeHUD.Poe.EntityComponents;
using PoeHUD.Settings;
using SlimDX.Direct3D9;

namespace PoeHUD.Hud.Monster
{
	public class MonsterTracker : HUDPluginBase, EntityListObserver, HUDPluginWithMapIcons
	{
		public class MonstersSettings : SettingsForModule
		{
			public MonstersSettings() : base("Monster Alerts") { }

			public readonly Setting<bool> PlaySound = new Setting<bool>("Play Sound", true);
			public readonly Setting<bool> ShowText = new Setting<bool>("Show Text", true);
			public readonly SettingIntRange TextFontSize = new SettingIntRange("Font Size", 7, 30, 16);
			public readonly SettingIntRange TextBgAlpha = new SettingIntRange("Bg Alpha",0, 255, 128);
			public readonly SettingIntRange YPosition = new SettingIntRange("Y Position", 0, 98, 20);
			public readonly Setting<bool> StackUp = new Setting<bool>("Stack Up", false);
		}


		public readonly MonstersSettings Settings = new MonstersSettings();
		private HashSet<int> alreadyAlertedOf;
		private Dictionary<string, List<EntityWrapper>> alertsText;
		private readonly Dictionary<EntityWrapper, MapIcon> currentIcons = new Dictionary<EntityWrapper, MapIcon>();


		private Dictionary<string, string> ModsToAlertOf;
		private Dictionary<string, string> NamesToAlertOf;

		public override void OnEnable()
		{
			alreadyAlertedOf = new HashSet<int>();
			alertsText = new Dictionary<string, List<EntityWrapper>>();
			InitAlertStrings();

			currentIcons.Clear();
			foreach (EntityWrapper current in model.Entities)
			{
				EntityAdded(current);
			}
		}

		public override SettingsForModule SettingsNode { get { return Settings; } }

		public void EntityRemoved(EntityWrapper entity)
		{
			string ktd = null;
			foreach (KeyValuePair<string, List<EntityWrapper>> kv in alertsText) {
				kv.Value.Remove(entity);
				if (kv.Value.Count == 0)
					ktd = kv.Key;
			}
			if (null != ktd)
				alertsText.Remove(ktd);
			currentIcons.Remove(entity);
		}

		public void EntityAdded(EntityWrapper entity)
		{
			if (!Settings.Enabled || currentIcons.ContainsKey(entity))
			{
				return;
			}
			if (entity.IsAlive && entity.HasComponent<Poe.EntityComponents.Monster>())
			{
				currentIcons[entity] = GetMapIconForMonster(entity);
				string text = entity.Path;
				if (text.Contains('@'))
				{
					text = text.Split('@')[0];
				}
				if (NamesToAlertOf.ContainsKey(text))
				{
					addEntity(entity, NamesToAlertOf[text]);
					return;
				}
				foreach (string current in entity.GetComponent<ObjectMagicProperties>().Mods.Where(current => ModsToAlertOf.ContainsKey(current)))
				{
					addEntity(entity, ModsToAlertOf[current]);
				}
			}
		}

		private void addEntity(EntityWrapper entity, string key)
		{
			List<EntityWrapper> lew;
			if (!alertsText.TryGetValue(key, out lew))
				alertsText[key] = lew = new List<EntityWrapper>();
			lew.Add(entity);
			PlaySound(entity);
		}

		private void PlaySound(EntityWrapper entity)
		{
			if (!Settings.PlaySound)
			{
				return;
			}
			if (!alreadyAlertedOf.Contains(entity.Id))
			{
				Sounds.DangerSound.Play();
				alreadyAlertedOf.Add(entity.Id);
			}
		}
		public override void OnAreaChange(AreaController area)
		{
			alreadyAlertedOf.Clear();
			alertsText.Clear();
			currentIcons.Clear();
		}
		public override void Render(RenderingContext rc, Dictionary<UiMountPoint, Vec2> mountPoints)
		{
			if (!Settings.ShowText || !alertsText.Any())
			{
				return;
			}

			Rect rect = model.Window.ClientRect();
			int xScreenCenter = rect.W / 2 + rect.X;
			int yPos = rect.H * Settings.YPosition / 100 + rect.Y;

			var playerPos = model.Player.GetComponent<Positioned>().GridPos;
			int fontSize = Settings.TextFontSize;
			bool first = true;
			Rect rectBackground = new Rect();
			foreach (var alert in alertsText)
			{
				int cntAlive = alert.Value.Count(c => c.IsAlive);
				if (cntAlive == 0)
					continue;

				Vec2 textSize = rc.MeasureString(alert.Key, fontSize, DrawTextFormat.Center);

				int iconSize = 3 + textSize.Y;

				int xPos = xScreenCenter - textSize.X / 2 - 6;
				rc.AddTextWithHeight(new Vec2(xPos + 6, yPos), alert.Key, Color.Red, fontSize, DrawTextFormat.Left);
				

				int cntArrows = 1;
				rectBackground = new Rect(xPos - cntAlive * iconSize, yPos, textSize.X + 12 + cntAlive * iconSize, textSize.Y);
				if (first) // vertical padding above
				{
					if( !Settings.StackUp)
						rectBackground.Y -= 5;
					rectBackground.H += 5;
					first = false;
				}
				rc.AddBox(rectBackground, Color.FromArgb(Settings.TextBgAlpha, 1, 1, 1));

				foreach (EntityWrapper mob in alert.Value)
				{
					if (!mob.IsAlive)
						continue;
					Vec2 delta = mob.GetComponent<Positioned>().GridPos - playerPos;
					double phi;
					double distance = delta.GetPolarCoordinates(out phi);
					RectUV uv = GetDirectionsUv(phi, distance);

					Rect rectDirection = new Rect(xPos - cntArrows * iconSize, yPos, rectBackground.H, rectBackground.H);
					cntArrows++;
					rc.AddSprite("directions.png", rectDirection, uv, Color.Red);
				}



				yPos += Settings.StackUp ? -textSize.Y : textSize.Y;
			}

			if (!first)  // vertical padding below
			{
				rectBackground.Y = rectBackground.Y + (Settings.StackUp ? - rectBackground.H - 5: rectBackground.H);
				rectBackground.H = 5;
				rc.AddBox(rectBackground, Color.FromArgb(Settings.TextBgAlpha, 1, 1, 1));
			}
		}

		public IEnumerable<MapIcon> GetIcons()
		{
			List<EntityWrapper> toRemove = new List<EntityWrapper>();
			foreach (KeyValuePair<EntityWrapper, MapIcon> kv in currentIcons)
			{
				if (kv.Value.IsEntityStillValid())
					yield return kv.Value;
				else
					toRemove.Add(kv.Key);
			}
			foreach (EntityWrapper wrapper in toRemove)
			{
				currentIcons.Remove(wrapper);
			}
		}


		private void InitAlertStrings()
		{
			ModsToAlertOf = FsUtils.LoadKeyValueCommaSeparatedFromFile("config/monster_mod_alerts.txt");
			NamesToAlertOf = FsUtils.LoadKeyValueCommaSeparatedFromFile("config/monster_name_alerts.txt");
		}

		private MapIcon GetMapIconForMonster(EntityWrapper e)
		{
			Rarity rarity = e.GetComponent<ObjectMagicProperties>().Rarity;
			if (!e.IsHostile)
				return new MapIconCreature(e, new HudTexture("monster_ally.png"), 6) { Rarity = rarity, Type = MapIcon.IconType.Minion };

			switch (rarity)
			{
				case Rarity.White: return new MapIconCreature(e, new HudTexture("monster_enemy.png"), 6) { Type = MapIcon.IconType.Monster, Rarity = rarity };
				case Rarity.Magic: return new MapIconCreature(e, new HudTexture("monster_enemy_blue.png"), 8) { Type = MapIcon.IconType.Monster, Rarity = rarity };
				case Rarity.Rare: return new MapIconCreature(e, new HudTexture("monster_enemy_yellow.png"), 10) { Type = MapIcon.IconType.Monster, Rarity = rarity };
				case Rarity.Unique: return new MapIconCreature(e, new HudTexture("monster_enemy_orange.png"), 10) { Type = MapIcon.IconType.Monster, Rarity = rarity };
			}
			return null;
		}
	}
}
