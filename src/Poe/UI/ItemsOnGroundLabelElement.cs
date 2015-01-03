using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoeHUD.Poe.UI
{
	public class ItemsOnGroundLabelElement : Element
	{
		public Entity ItemOnGround
		{
			get { return base.ReadObject<Entity>(Address + 0xC); }
		}

		public Element Label
		{
			get { return base.ReadObject<Element>(Address + 0x8); }
		}

		public new IEnumerable<ItemsOnGroundLabelElement> Children
		{
			get
			{
				int address = M.ReadInt(Address + 0x9ac);
				for (int nextAddress = M.ReadInt(address); nextAddress != address; nextAddress = M.ReadInt(nextAddress))
				{
					yield return GetObject<ItemsOnGroundLabelElement>(nextAddress);
				}
			}
		}
	}
}
