namespace PoeHUD.Poe.EntityComponents
{
	public abstract class Component : RemoteMemoryObject
	{
		public Entity Owner { get { return base.ReadObjectAt<Entity>(4); } }
	}
}
