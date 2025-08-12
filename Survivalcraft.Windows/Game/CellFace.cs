using Engine;
using System;

namespace Game
{
	public struct CellFace : IEquatable<CellFace>
	{
		public int X;

		public int Y;

		public int Z;

		public int Face;

		public static readonly int[] m_oppositeFaces = new int[6]
		{
			2,
			3,
			0,
			1,
			5,
			4
		};

		public static readonly Point3[] m_faceToPoint3 = new Point3[6]
		{
			new(0, 0, 1),
			new(1, 0, 0),
			new(0, 0, -1),
			new(-1, 0, 0),
			new(0, 1, 0),
			new(0, -1, 0)
		};

		public static readonly Vector3[] m_faceToVector3 = new Vector3[6]
		{
			new(0f, 0f, 1f),
			new(1f, 0f, 0f),
			new(0f, 0f, -1f),
			new(-1f, 0f, 0f),
			new(0f, 1f, 0f),
			new(0f, -1f, 0f)
		};

		public Point3 Point
		{
			get
			{
				return new Point3(X, Y, Z);
			}
			set
			{
				X = value.X;
				Y = value.Y;
				Z = value.Z;
			}
		}

		public CellFace(int x, int y, int z, int face)
		{
			X = x;
			Y = y;
			Z = z;
			Face = face;
		}

		public static int OppositeFace(int face)
		{
			return m_oppositeFaces[face];
		}

		public static Point3 FaceToPoint3(int face)
		{
			return m_faceToPoint3[face];
		}

		public static Vector3 FaceToVector3(int face)
		{
			return m_faceToVector3[face];
		}

		public static int Point3ToFace(Point3 p, int maxFace = 5)
		{
			for (int i = 0; i < maxFace; i++)
			{
				if (m_faceToPoint3[i] == p)
				{
					return i;
				}
			}
			throw new InvalidOperationException("Invalid Point3.");
		}

		public static int Vector3ToFace(Vector3 v, int maxFace = 5)
		{
			float num = -1f / 0f;
			int result = 0;
			for (int i = 0; i <= maxFace; i++)
			{
				float num2 = Vector3.Dot(m_faceToVector3[i], v);
				if (num2 > num)
				{
					result = i;
					num = num2;
				}
			}
			return result;
		}

		public static CellFace FromAxisAndDirection(int x, int y, int z, int axis, float direction)
		{
			CellFace result = default;
			result.X = x;
			result.Y = y;
			result.Z = z;
			switch (axis)
			{
				case 0:
					result.Face = (direction > 0f) ? 1 : 3;
					break;
				case 1:
					result.Face = (direction > 0f) ? 4 : 5;
					break;
				case 2:
					result.Face = (!(direction > 0f)) ? 2 : 0;
					break;
			}
			return result;
		}

		public Plane CalculatePlane()
		{
			switch (Face)
			{
				case 0:
					return new Plane(new Vector3(0f, 0f, 1f), -(Z + 1));
				case 1:
					return new Plane(new Vector3(-1f, 0f, 0f), X + 1);
				case 2:
					return new Plane(new Vector3(0f, 0f, -1f), Z);
				case 3:
					return new Plane(new Vector3(1f, 0f, 0f), -X);
				case 4:
					return new Plane(new Vector3(0f, 1f, 0f), -(Y + 1));
				default:
					return new Plane(new Vector3(0f, -1f, 0f), Y);
			}
		}

		public override int GetHashCode()
		{
			return (X << 11) + (Y << 7) + (Z << 3) + Face;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CellFace))
			{
				return false;
			}
			return Equals((CellFace)obj);
		}

		public bool Equals(CellFace other)
		{
			if (other.X == X && other.Y == Y && other.Z == Z)
			{
				return other.Face == Face;
			}
			return false;
		}

		public override string ToString()
		{
			return X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ", face " + Face.ToString();
		}

		public static bool operator ==(CellFace c1, CellFace c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(CellFace c1, CellFace c2)
		{
			return !c1.Equals(c2);
		}
	}
}
