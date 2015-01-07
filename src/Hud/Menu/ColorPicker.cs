using PoeHUD.Framework;
using PoeHUD.Settings;
using SlimDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PoeHUD.Hud.Menu
{
	class ColorPicker : MenuItem
	{
		private int barBeingDragged = -1;
		private Color value;
		private readonly string text;
		private readonly Setting<Color> setting;
		private readonly Dictionary<int, Color> bars = new Dictionary<int, Color>() { { 0, Color.Red }, { 1, Color.Green }, { 2, Color.Blue } };

		public override int Height { get { return base.Height * 3 + 15; } }

		public ColorPicker(Menu.MenuSettings menuSettings, string text, Setting<Color> setting)
			: base(menuSettings)
		{
			this.text = text;
			this.value = setting.Value;
			this.setting = setting;
		}

		private void CalcValue(int x)
		{
			int lBound = base.Bounds.X + 5;
			int rBound = base.Bounds.X + base.Bounds.W - 20;
			float num3 = x <= lBound ? 0f : (x >= rBound ? 1f : (float)(x - lBound) / (rBound - lBound));
			switch (barBeingDragged)
			{
				case 0:
					this.value = Color.FromArgb((int)Math.Round(num3 * 255), value.G, value.B);
					break;
				case 1:
					this.value = Color.FromArgb(value.R, (int)Math.Round(num3 * 255), value.B);
					break;
				case 2:
					this.value = Color.FromArgb(value.R, value.G, (int)Math.Round(num3 * 255));
					break;
			}

			setting.Value = value;

		}
		protected override bool TestBounds(Vec2 pos)
		{
			return this.barBeingDragged >= 0 || base.TestBounds(pos);
		}

		protected override void HandleEvent(MouseEventID id, Vec2 pos)
		{
			int colorHovered = (int)Math.Floor((double)(pos.Y - base.Bounds.Y) / (double)(base.Bounds.H / 3));

			if (id == MouseEventID.LeftButtonDown)
			{
				this.barBeingDragged = colorHovered;
				return;
			}
			if (id == MouseEventID.LeftButtonUp)
			{
				this.CalcValue(pos.X);
				this.barBeingDragged = -1;
				return;
			}
			if (this.barBeingDragged != -1 && id == MouseEventID.MouseMove)
			{
				this.CalcValue(pos.X);
			}
		}

		public override void Render(RenderingContext rc)
		{
			if (!this.isVisible)
			{
				return;
			}
			
			rc.AddBox(base.Bounds, Color.Black);
			rc.AddBox(new Rect(base.Bounds.X + 1, base.Bounds.Y + 1, base.Bounds.W - 2, base.Bounds.H - 2), Color.Gray);
			
			for (int c = 0; c < 3; c++ )
			{
				Rect barBounds = new Rect(base.Bounds.X, base.Bounds.Y + (base.Bounds.H / 3 * c), base.Bounds.W - 15, base.Bounds.H / 3);
				rc.AddTextWithHeight(new Vec2(barBounds.X + barBounds.W / 2, barBounds.Y + barBounds.H / 3), bars[c].Name + ": " + this.value.PrimaryColorValue(bars[c]), Color.White, 11, DrawTextFormat.VerticalCenter | DrawTextFormat.Center);
				rc.AddBox(new Rect(barBounds.X + 5, barBounds.Y + (3 * barBounds.H / 4), barBounds.W - 10, 4), bars[c]);
				rc.AddBox(new Rect(barBounds.X + 5 + ((barBounds.W - 10) * this.value.PrimaryColorValue(bars[c]) / 255) - 2, barBounds.Y + (3 * barBounds.H / 4) - 2, 4, 8), Color.White);
			}

			Rect preview = new Rect(base.Bounds.X + base.Bounds.W - 12, base.Bounds.Y + 2, 10, base.Bounds.H - 4);
			rc.AddBox(preview, Color.Black);
			rc.AddBox(new Rect(preview.X + 1, preview.Y + 1, preview.W - 2, preview.H - 2), this.value);
		}
	}
}
