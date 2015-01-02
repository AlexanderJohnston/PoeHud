using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using PoeHUD.Controllers;
using PoeHUD.Framework;
using PoeHUD.Settings;
using SlimDX.Direct3D9;

namespace PoeHUD.Hud.DPS
{
	public class FpsMeter : HUDPluginBase
	{
		private bool hasStarted;
		private Stopwatch watch;


		public class FpsDisplaySettings : SettingsForModule
		{
			public SettingIntRange DpsFontSize = new SettingIntRange("FPS font size", 10, 30, 16);
			public FpsDisplaySettings() : base("FPS-meter") { }
		}

		public FpsDisplaySettings Settings = new FpsDisplaySettings(); 


		public override void OnAreaChange(AreaController area)
		{
			hasStarted = false;
		}

		public override void Render(RenderingContext rc, Dictionary<UiMountPoint, Vec2> mountPoints)
		{
			if (!hasStarted)
			{
				watch = new Stopwatch();
				watch.Start();
				hasStarted = true;
				return;
			}


			Vec2 mapWithOffset = mountPoints[UiMountPoint.LeftOfMinimap];
			float ms = watch.ElapsedMilliseconds;
			watch.Restart();
			
			var textSize = rc.AddTextWithHeight(mapWithOffset,  ms + " ms/frame", Color.White, Settings.DpsFontSize, DrawTextFormat.Right);
		

			int width = textSize.X;
			Rect rect = new Rect(mapWithOffset.X - 5 - width, mapWithOffset.Y - 5, width + 10, textSize.Y + 10);
			
			rc.AddBox(rect, Color.FromArgb(160, Color.Black));

			mountPoints[UiMountPoint.LeftOfMinimap] = new Vec2(mapWithOffset.X, mapWithOffset.Y + 5 + rect.H);
		}

		public override SettingsForModule SettingsNode
		{
			get { return Settings; }
		}
	}
}
