using System.Text;

namespace Engine.Serialization
{
	public class EngineBinaryReader : BinaryReader
	{
		public EngineBinaryReader(Stream stream, bool leaveOpen = false)
			: base(stream, Encoding.UTF8, leaveOpen)
		{
		}

		public new int Read7BitEncodedInt()
		{
			return base.Read7BitEncodedInt();
		}

		public virtual Color ReadColor()
		{
			return new Color(ReadUInt32());
		}

		public virtual Point2 ReadPoint2()
		{
			return new Point2(ReadInt32(), ReadInt32());
		}

		public virtual Point3 ReadPoint3()
		{
			return new Point3(ReadInt32(), ReadInt32(), ReadInt32());
		}

		public virtual Rectangle ReadRectangle()
		{
			return new Rectangle(ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32());
		}

		public virtual Box ReadBox()
		{
			return new Box(ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32());
		}

		public virtual Vector2 ReadVector2()
		{
			return new Vector2(ReadSingle(), ReadSingle());
		}

		public virtual Vector3 ReadVector3()
		{
			return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
		}

		public virtual Vector4 ReadVector4()
		{
			return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
		}

		public virtual BoundingRectangle ReadBoundingRectagle()
		{
			return new BoundingRectangle(ReadVector2(), ReadVector2());
		}

		public virtual BoundingBox ReadBoundingBox()
		{
			return new BoundingBox(ReadVector3(), ReadVector3());
		}

		public virtual Plane ReadPlane()
		{
			return new Plane(ReadVector3(), ReadSingle());
		}

		public virtual Quaternion ReadQuaternion()
		{
			return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
		}

		public virtual Matrix ReadMatrix()
		{
			return new Matrix(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
		}

		public virtual T ReadStruct<T>() where T : struct
		{
			return Utilities.ArrayToStructure<T>(ReadBytes(Utilities.SizeOf<T>()));
		}
	}
}
