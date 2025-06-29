using Engine;
using GameEntitySystem;
using TemplatesDatabase;
namespace Game
{
	public interface IComponentEscapeBehavior
	{
		public float LowHealthToEscape { get; set; }
		public bool IsActive { get; set; }
		public float ImportanceLevel { get; }
	}
}
