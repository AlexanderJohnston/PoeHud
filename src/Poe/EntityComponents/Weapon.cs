namespace PoeHUD.Poe.EntityComponents
{
	public class Weapon : Component
	{
		public class AttackProps : RemoteMemoryObject
		{
			public int Value0 { get { return M.ReadInt(Address); } }
			public int DamageMin { get { return M.ReadInt(Address + 4); }}
			public int DamageMax { get { return M.ReadInt(Address + 8); } }
			public int AttackTime { get { return M.ReadInt(Address + 0xC); } } // milliseconds
			public int CritChance { get { return M.ReadInt(Address + 0x10); } } // percent times 100

			public int Value14 { get { return M.ReadInt(Address + 0x14); } }
			public int Value18 { get { return M.ReadInt(Address + 0x18); } }
			public int Value1C { get { return M.ReadInt(Address + 0x1C); } }
			public int Value20 { get { return M.ReadInt(Address + 0x20); } }
			public int Value24 { get { return M.ReadInt(Address + 0x24); } }

		}
		public AttackProps Attack { get { return base.ReadObjectAt<AttackProps>(0x10); } }

	}



}
