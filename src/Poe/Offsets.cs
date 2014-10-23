using PoeHUD.Framework;

namespace PoeHUD.Poe
{
	public class Offsets
	{
		public string ExeName = "PathOfExile";

		public int IgsOffset;
		public int IgsDelta;

		public int Base;
		public int FileRoot;
		public int MaphackFunc;
		public int ZoomHackFunc;
		public int AreaChangeCount;
		public int Fullbright1;
		public int Fullbright2;


		public static Offsets Regular = new Offsets { IgsOffset = 0, IgsDelta = 0, ExeName = "PathOfExile" };
		public static Offsets Steam = new Offsets { IgsOffset = 24, IgsDelta = 0, ExeName = "PathOfExileSteam" };
		/* offsets from some older steam version: 
		 	Base = 8841968;
			FileRoot = 8820476;
			MaphackFunc = 4939552;
			ZoomHackFunc = 2225383;
			AreaChangeCount = 8730996;
			Fullbright1 = 7639804;
			Fullbright2 = 8217084;
		*/

        // 51 8B 46 68 8B 08 68 00 20 00 00 8D 54 24 04 52 6A 00 6A 00 50 8B 41 2C FF D0 8B 46 48 3B 46 4C
		private static Pattern maphackPattern = new Pattern(new byte[]
		{
			81, 139, 70, 104, 139, 8, 104, 0, 32, 0, 0, 141, 84, 36, 4, 82, 
			106, 0, 106, 0, 80, 139, 65, 44, 255, 208, 139, 70, 72, 59, 70, 76 
		}, "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

        // 55 8B EC 83 E4 F8 8B 45 0C 83 EC 2C 80 38 00 53 56 57 8B D9 0F 85 E9 00 00 00 83 BB
		private static Pattern zoomhackPattern = new Pattern(new byte[]
		{
			85, 139, 236, 131, 228, 248, 139, 69, 12, 131, 236, 44, 128, 56, 0, 83, 
			86, 87, 139, 217, 15, 133, 233, 0, 0, 0, 131, 187
		}, "xxxxxxxxxxxxxxxxxxxxxxxxxxxx");

        //55 8B EC 83 E4 F8 6A FF 68 ?? ?? ?? ?? 64 A1 00 00 00 00 50 64 89 25 00 00 00 00 81 EC A0 00 00 00 53 8B 5D 10 C7 44 24 44 00 00 00 00 8B
		private static Pattern fullbrightPattern = new Pattern(new byte[]
		{
			85, 139, 236, 131, 228, 248, 106, 255, 104, 0, 0, 0, 0, 100, 161, 0,
			0, 0, 0, 80, 100, 137, 37, 0, 0, 0, 0, 129, 236, 160, 0, 0,
			0, 83, 139, 93, 16, 199, 68, 36, 68, 0, 0, 0, 0, 139
		}, "xxxxxxxxx????xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

        //64 A1 00 00 00 00 6A FF 68 ?? ?? ?? ?? 50 64 89 25 00 00 00 00 A1 ?? ?? ?? ?? 81 EC 90 00 00 00 53 55 56 57 33 FF 3B C7 - 1.2.3b
        //64 A1 00 00 00 00 6A FF 68 ?? ?? ?? ?? 50 64 89 25 00 00 00 00 A1 ?? ?? ?? ?? 81 EC C8 00 00 00 53 55 33 DB 56 57 3B C3 - 1.2.4
		private static Pattern basePtrPattern = new Pattern(new byte[]
		{
			100, 161, 0, 0, 0, 0, 106, 255, 104, 0, 0, 0, 0, 80, 100, 137,
			37, 0, 0, 0, 0, 161, 0, 0, 0, 0, 129, 236, 144, 0, 0, 0,
			83, 85, 51, 219, 86, 87, 59, 195

		}, "xxxxxxxxx????xxxxxxxxx????xxxxxxxxxxxxxx");

        //6A FF 68 ?? ?? ?? ?? 64 A1 00 00 00 00 50 64 89 25 00 00 00 00 83 EC 30 FF 05 ?? ?? ?? ?? 53 55 8B 2D ?? ?? ?? ?? 56 B8
		private static Pattern fileRootPattern = new Pattern(new byte[]
		{
			106, 255, 104, 0, 0, 0, 0, 100, 161, 0, 0, 0, 0, 80, 100, 137,
			37, 0, 0, 0, 0, 131, 236, 48, 255, 5, 0, 0, 0, 0, 83, 85,
			139, 45, 0, 0, 0, 0, 86, 184
		}, "xxx????xxxxxxxxxxxxxxxxxxx????xxxx????xx");

        //8B 09 89 08 85 C9 74 0C FF 41 28 8B 15  89 51 24 C3 CC
		private static Pattern areaChangePattern = new Pattern(new byte[]
		{
			139, 9, 137, 8, 133, 201, 116, 12, 255, 65, 40, 139, 21, 0, 0, 0,
			0, 137, 81, 36, 195, 204
		}, "xxxxxxxxxxxxx????xxxxx");
		public void DoPatternScans(Memory m)
		{
			int[] array = m.FindPatterns(new Pattern[]
			{
				Offsets.maphackPattern,
				Offsets.zoomhackPattern,
				Offsets.fullbrightPattern,
				Offsets.basePtrPattern,
				Offsets.fileRootPattern,
				Offsets.areaChangePattern
			});
			MaphackFunc = array[0];
			ZoomHackFunc = array[1] + 247;
			Fullbright1 = m.ReadInt(m.BaseAddress + array[2] + 1487) - m.BaseAddress;
			Fullbright2 = m.ReadInt(m.BaseAddress + array[2] + 1573) - m.BaseAddress;
			Base = m.ReadInt(m.BaseAddress + array[3] + 80) - m.BaseAddress;
			FileRoot = m.ReadInt(m.BaseAddress + array[4] + 40) - m.BaseAddress;
			AreaChangeCount = m.ReadInt(m.BaseAddress + array[5] + 13) - m.BaseAddress;
		}
	}
}
