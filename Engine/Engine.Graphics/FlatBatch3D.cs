using System.Collections.Generic;

namespace Engine.Graphics
{
    public class FlatBatch3D : BaseFlatBatch
    {
        public FlatBatch3D()
        {
            base.DepthStencilState = DepthStencilState.Default;
            base.RasterizerState = RasterizerState.CullNoneScissor;
            base.BlendState = BlendState.AlphaBlend;
        }
        /// <summary>
        /// 绘制球
        /// </summary>
        public void QueueSphere(Vector3 center, float radius, int slices, int stacks, Color color)
        {
            if (slices < 3)
                slices = 3;
            if (stacks < 2)
                stacks = 2;

            var latitudeStep = (float)Math.PI / stacks;
            var longitudeStep = 2.0f * (float)Math.PI / slices;

            for (int lat = 0; lat <= stacks; lat++)
            {
                var a1 = (float)(-Math.PI / 2 + lat * latitudeStep);
                var a2 = (float)(-Math.PI / 2 + (lat + 1) * latitudeStep);

                for (int lon = 0; lon <= slices; lon++)
                {
                    var b1 = lon * longitudeStep;
                    var b2 = (lon + 1) * longitudeStep;

                    // 上半球顶点
                    var v1 = center + new Vector3((float)(Math.Sin(a1) * Math.Cos(b1)) * radius, (float)(Math.Cos(a1)) * radius, (float)(Math.Sin(a1) * Math.Sin(b1)) * radius);
                    var v2 = center + new Vector3((float)(Math.Sin(a2) * Math.Cos(b1)) * radius, (float)(Math.Cos(a2)) * radius, (float)(Math.Sin(a2) * Math.Sin(b1)) * radius);
                    var v3 = center + new Vector3((float)(Math.Sin(a1) * Math.Cos(b2)) * radius, (float)(Math.Cos(a1)) * radius, (float)(Math.Sin(a1) * Math.Sin(b2)) * radius);
                    var v4 = center + new Vector3((float)(Math.Sin(a2) * Math.Cos(b2)) * radius, (float)(Math.Cos(a2)) * radius, (float)(Math.Sin(a2) * Math.Sin(b2)) * radius);

                    // 下半球顶点
                    var v5 = center - new Vector3((float)(Math.Sin(-a1) * Math.Cos(b1)) * radius, (float)(Math.Cos(-a1)) * radius, (float)(Math.Sin(-a1) * Math.Sin(b1)) * radius);
                    var v6 = center - new Vector3((float)(Math.Sin(-a1) * Math.Cos(b2)) * radius, (float)(Math.Cos(-a1)) * radius, (float)(Math.Sin(-a1) * Math.Sin(b2)) * radius);
                    var v7 = center - new Vector3((float)(Math.Sin(-a2) * Math.Cos(b2)) * radius, (float)(Math.Cos(-a2)) * radius, (float)(Math.Sin(-a2) * Math.Sin(b2)) * radius);
                    var v8 = center - new Vector3((float)(Math.Sin(-a2) * Math.Cos(b1)) * radius, (float)(Math.Cos(-a2)) * radius, (float)(Math.Sin(-a2) * Math.Sin(b1)) * radius);


                    // 上半球三角形
                    QueueTriangle(v1, v2, v3, color);
                    QueueTriangle(v2, v4, v3, color);
                    // 下半球三角形
                    QueueTriangle(v5, v6, v7, color);
                    QueueTriangle(v5, v7, v8, color);

                }
            }
        }
        /// <summary>
        /// 绘制球线框
        /// </summary>
        /// <param name="center">球心</param>
        /// <param name="radius">半径</param>
        /// <param name="longitudeLines">经度线</param>
        /// <param name="latitudeLines">纬度线</param>
        /// <param name="draw">0表示全部绘制，1表示只绘制纬线球，2表示只绘制经线球</param>
        public void QueueSphereWithLines(Vector3 center, Color color, float radius = 1, int longitudeLines = 20, int latitudeLines = 20, int draw = 0)
        {
            if (longitudeLines < 3)
                longitudeLines = 3;
            if (latitudeLines < 2)
                latitudeLines = 2;
            if (draw == 1 || draw == 0)
            {
                // 绘制纬线球
                for (int lat = 0; lat <= latitudeLines; lat++)
                {
                    var angle = (float)(Math.PI / 2 - lat * Math.PI / latitudeLines);
                    var radiusAtLatitude = radius * (float)Math.Cos(angle); // 计算纬度上的圆半径
                    var offset = new Vector3(0, (float)(Math.Sin(angle) * radius), 0);
                    QueueCircle(center + offset, radiusAtLatitude, longitudeLines, color);
                }
            }
            if (draw == 2 || draw == 0)
            {
                // 绘制经线球
                for (int lon = 0; lon < longitudeLines; lon++) // 注意：这里不需要包括最后一个经度线，因为它会和第一个经度线重合
                {
                    var longitudeAngle = lon * 2 * (float)Math.PI / longitudeLines; // 经度角度
                    var points = new List<Vector3>();

                    for (int lat = 0; lat <= latitudeLines; lat++)
                    {
                        var latitudeAngle = (float)(Math.PI / 2 - lat * Math.PI / latitudeLines);
                        points.Add(center + new Vector3(
                            (float)(Math.Cos(latitudeAngle) * Math.Cos(longitudeAngle)) * radius,
                            (float)(Math.Sin(latitudeAngle)) * radius,
                            (float)(Math.Cos(latitudeAngle) * Math.Sin(longitudeAngle)) * radius));
                    }
                    // 绘制经线
                    QueueLineStrip(points, color);
                }
            }
        }
        /// <summary>
        /// 绘制圆
        /// </summary>
        public void QueueCircle(Vector3 center, float radius, int segments, Color color, bool useLineStrip = true)
        {
            if (segments < 3)
                segments = 3;

            var step = (float)(2 * Math.PI) / segments;

            var points = new List<Vector3>();

            for (int i = 0; i <= segments; i++)
            {
                var angle = step * i;
                var point = center + new Vector3((float)Math.Cos(angle) * radius, 0, (float)Math.Sin(angle) * radius);
                points.Add(point);
            }

            if (useLineStrip)
            {
                QueueLineStrip(points, color);
            }
            else
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    QueueLine(points[i], points[i + 1], color);
                }
            }
        }
        /// <summary>
        /// 绘制圆柱
        /// </summary>
        public void QueueCurvedCylinder(Vector3 start, Vector3 end, float radius, Color color, int segments = 12, bool drawTopAndBottom = true)
        {
            // 计算圆柱的高度
            float height = Vector3.Distance(start, end);

            // 计算圆柱的方向
            Vector3 direction = Vector3.Normalize(end - start);

            // 计算圆柱的基准点
            Vector3 baseCenter = start + (direction * (height / 2));

            // 绘制圆柱的侧面
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)(2 * Math.PI * i / segments);
                float angle2 = (float)(2 * Math.PI * (i + 1) / segments);

                Vector3 point1 = baseCenter + new Vector3(radius * MathF.Cos(angle1), -height / 2, radius * MathF.Sin(angle1));
                Vector3 point2 = baseCenter + new Vector3(radius * MathF.Cos(angle2), -height / 2, radius * MathF.Sin(angle2));
                Vector3 point3 = baseCenter + new Vector3(radius * MathF.Cos(angle1), height / 2, radius * MathF.Sin(angle1));
                Vector3 point4 = baseCenter + new Vector3(radius * MathF.Cos(angle2), height / 2, radius * MathF.Sin(angle2));

                // 绘制侧面
                QueueTriangle(point1, point2, point3, color);
                QueueTriangle(point2, point4, point3, color);
            }
            if (drawTopAndBottom)
            {
                // 绘制圆柱的顶部和底部
                QueueCircle(baseCenter + new Vector3(0, height / 2, 0), radius, segments, color);
                QueueCircle(baseCenter + new Vector3(0, -height / 2, 0), radius, segments, color);
            }
        }

        public void QueueBatchTriangles(FlatBatch3D batch, Matrix? matrix = null, Color? color = null)
        {
            int count = TriangleVertices.Count;
            TriangleVertices.AddRange(batch.TriangleVertices);
            int count2 = TriangleIndices.Count;
            int count3 = batch.TriangleIndices.Count;
            TriangleIndices.Count += count3;
            for (int i = 0; i < count3; i++)
            {
                TriangleIndices[i + count2] = (ushort)(batch.TriangleIndices[i] + count);
            }
            if (matrix.HasValue && matrix != Matrix.Identity)
            {
                TransformTriangles(matrix.Value, count);
            }
            if (color.HasValue && color != Color.White)
            {
                TransformTrianglesColors(color.Value, count);
            }
        }

        public void QueueBatchLines(FlatBatch3D batch, Matrix? matrix = null, Color? color = null)
        {
            int count = LineVertices.Count;
            LineVertices.AddRange(batch.LineVertices);
            int count2 = LineIndices.Count;
            int count3 = batch.LineIndices.Count;
            LineIndices.Count += count3;
            for (int i = 0; i < count3; i++)
            {
                LineIndices[i + count2] = (ushort)(batch.LineIndices[i] + count);
            }
            if (matrix.HasValue && matrix != Matrix.Identity)
            {
                TransformLines(matrix.Value, count);
            }
            if (color.HasValue && color != Color.White)
            {
                TransformLinesColors(color.Value, count);
            }
        }

        public void QueueBatch(FlatBatch3D batch, Matrix? matrix = null, Color? color = null)
        {
            QueueBatchLines(batch, matrix, color);
            QueueBatchTriangles(batch, matrix, color);
        }
        public void QueueLine(Vector3 p1, Vector3 p2, Color color)
        {
            int count = LineVertices.Count;
            LineVertices.Add(new VertexPositionColor(p1, color));
            LineVertices.Add(new VertexPositionColor(p2, color));
            LineIndices.Add(count);
            LineIndices.Add(count + 1);
        }

        public void QueueLine(Vector3 p1, Vector3 p2, Color color1, Color color2)
        {
            int count = LineVertices.Count;
            LineVertices.Add(new VertexPositionColor(p1, color1));
            LineVertices.Add(new VertexPositionColor(p2, color2));
            LineIndices.Add(count);
            LineIndices.Add(count + 1);
        }

        public void QueueLineStrip(IEnumerable<Vector3> points, Color color)
        {
            int count = LineVertices.Count;
            int num = 0;
            foreach (Vector3 point in points)
            {
                LineVertices.Add(new VertexPositionColor(point, color));
                num++;
            }
            for (int i = 0; i < num - 1; i++)
            {
                LineIndices.Add(count + i);
                LineIndices.Add(count + i + 1);
            }
        }

        public void QueueTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color color)
        {
            int count = TriangleVertices.Count;
            TriangleVertices.Add(new VertexPositionColor(p1, color));
            TriangleVertices.Add(new VertexPositionColor(p2, color));
            TriangleVertices.Add(new VertexPositionColor(p3, color));
            TriangleIndices.Add(count);
            TriangleIndices.Add(count + 1);
            TriangleIndices.Add(count + 2);
        }

        public void QueueTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color color1, Color color2, Color color3)
        {
            int count = TriangleVertices.Count;
            TriangleVertices.Add(new VertexPositionColor(p1, color1));
            TriangleVertices.Add(new VertexPositionColor(p2, color2));
            TriangleVertices.Add(new VertexPositionColor(p3, color3));
            TriangleIndices.Add(count);
            TriangleIndices.Add(count + 1);
            TriangleIndices.Add(count + 2);
        }
        /// <summary>
        /// 绘制矩形(支持渐变)
        /// </summary>
        public void QueueQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color1, Color color2, Color color3, Color color4)
        {
            int count = TriangleVertices.Count;
            TriangleVertices.Add(new VertexPositionColor(p1, color1)); // 左上
            TriangleVertices.Add(new VertexPositionColor(p2, color2)); // 右上
            TriangleVertices.Add(new VertexPositionColor(p3, color3)); // 左下
            TriangleVertices.Add(new VertexPositionColor(p4, color4)); // 右下

            TriangleIndices.Add(count);
            TriangleIndices.Add(count + 1);
            TriangleIndices.Add(count + 2);

            TriangleIndices.Add(count + 2);
            TriangleIndices.Add(count + 1);
            TriangleIndices.Add(count + 3);
        }
        public void QueueQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
        {
            int count = TriangleVertices.Count;
            TriangleVertices.Add(new VertexPositionColor(p1, color));
            TriangleVertices.Add(new VertexPositionColor(p2, color));
            TriangleVertices.Add(new VertexPositionColor(p3, color));
            TriangleVertices.Add(new VertexPositionColor(p4, color));
            TriangleIndices.Add(count);
            TriangleIndices.Add(count + 1);
            TriangleIndices.Add(count + 2);
            TriangleIndices.Add(count + 2);
            TriangleIndices.Add(count + 3);
            TriangleIndices.Add(count);
        }

        public void QueueBoundingBox(BoundingBox boundingBox, Color color)
        {
            QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), color);
            QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), color);
            QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), color);
            QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z), color);
            QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z), new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z), color);
            QueueLine(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), color);
        }

        public void QueueBoundingFrustum(BoundingFrustum boundingFrustum, Color color)
        {
            ReadOnlyList<Vector3> array = boundingFrustum.Corners;
            QueueLine(array[0], array[1], color);
            QueueLine(array[1], array[2], color);
            QueueLine(array[2], array[3], color);
            QueueLine(array[3], array[0], color);
            QueueLine(array[4], array[5], color);
            QueueLine(array[5], array[6], color);
            QueueLine(array[6], array[7], color);
            QueueLine(array[7], array[4], color);
            QueueLine(array[0], array[4], color);
            QueueLine(array[1], array[5], color);
            QueueLine(array[2], array[6], color);
            QueueLine(array[3], array[7], color);
        }
    }
}
