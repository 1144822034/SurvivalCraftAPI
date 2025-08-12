using Engine;

namespace Game
{
	public class SwitchElectricElement : MountedElectricElement
	{
		public float m_voltage;

		public SwitchElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int value)
			: base(subsystemElectricity, cellFace)
		{
			int voltageLevel = SwitchBlock.GetVoltageLevel(Terrain.ExtractData(value));
			m_voltage = SwitchBlock.GetLeverState(value) ? ((float)voltageLevel / 15f) : 0f;
		}

		public override float GetOutputVoltage(int face)
		{
			return m_voltage;
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			CellFace cellFace = base.CellFaces[0];
			int cellValue = base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
			int value = SwitchBlock.SetLeverState(cellValue, !SwitchBlock.GetLeverState(cellValue));
			base.SubsystemElectricity.SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value);
			base.SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, autoDelay: true);
			return true;
		}
	}
}
