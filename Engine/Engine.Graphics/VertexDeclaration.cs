using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Graphics
{
	public  class VertexDeclaration : IEquatable<VertexDeclaration>
	{
        public VertexElement[] m_elements;

        public static List<VertexElement[]> m_allElements = [];

		public ReadOnlyList<VertexElement> VertexElements => new(m_elements);

		public int VertexStride
		{
			get;
			set;
		}

        public VertexDeclaration(params VertexElement[] elements) : this(true, elements){}
		public VertexDeclaration(bool useCache = true, params VertexElement[] elements)
		{
			if (elements.Length == 0)
			{
				throw new ArgumentException("There must be at least one VertexElement.");
			}
			foreach (VertexElement vertexElement in elements)
			{
				if (vertexElement.Offset < 0)
				{
					vertexElement.Offset = VertexStride;
				}
				VertexStride = MathUtils.Max(VertexStride, vertexElement.Offset + vertexElement.Format.GetSize());
			}
            if (!useCache)
            {
                m_elements = elements.ToArray();
                return;
            }
			for (int j = 0; j < m_allElements.Count; j++)
			{
				if (elements.SequenceEqual(m_allElements[j]))
				{
					m_elements = m_allElements[j];
					break;
				}
			}
			if (m_elements == null)
			{
				m_elements = elements.ToArray();
				m_allElements.Add(m_elements);
			}
		}

		public override int GetHashCode()
		{
			return m_elements.GetHashCode();
		}

		public override bool Equals(object other)
		{
            return other is VertexDeclaration && Equals((VertexDeclaration)other);
        }

        public bool Equals(VertexDeclaration other)
		{
            return (object)other != null && m_elements == other.m_elements;
        }

        public static bool operator ==(VertexDeclaration vd1, VertexDeclaration vd2)
		{
			return vd1?.Equals(vd2) ?? ((object)vd2 == null);
		}

		public static bool operator !=(VertexDeclaration vd1, VertexDeclaration vd2)
		{
			return !(vd1 == vd2);
		}
	}
}
