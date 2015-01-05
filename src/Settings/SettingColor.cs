using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace PoeHUD.Settings
{
	public class SettingColor : SettingsBlock
	{
		public SettingColor(string name, int Red = 255, int Green = 255, int Blue = 255) : base(name)
		{
			this.Red.Default = Red;
			this.Green.Default = Green;
			this.Blue.Default = Blue;
		}

		public Color FromRGB { get { return Color.FromArgb(this.Red, this.Green, this.Blue); } }
		public SettingIntRange Red = new SettingIntRange("Red", 0, 255);
		public SettingIntRange Green = new SettingIntRange("Green", 0, 255);
		public SettingIntRange Blue = new SettingIntRange("Blue", 0, 255);
	}
}
