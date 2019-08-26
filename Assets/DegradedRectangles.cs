using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DegradedRectanglesData", menuName = "Degraded Rectangles")]
public class DegradedRectangles : ScriptableObject {
    public Mesh mesh;
    public List<DegradedRectangle> degraded_rectangles;

    [ContextMenu("Generate Degraded Rectangle")]
    private void GenerateDegradedRectangle() {
        if (mesh == null) {
            Debug.LogError("mesh is null");
            return;
        }

        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        // 遍历Mesh.triangles来找到所有退化四边形，要求无重复
        var custom_lines = new List<MeshLine>();
        int length = triangles.Length / 3;
        for (int i = 0; i < length; i++) {
            int vertex1_index = triangles[i * 3];
            int vertex2_index = triangles[i * 3 + 1];
            int vertex3_index = triangles[i * 3 + 2];

            AddCustomLine(vertex1_index, vertex2_index, vertex3_index, vertices, custom_lines);//添加三角图元vertex1和vertex2构成的退化四边形（或叫边）
            AddCustomLine(vertex2_index, vertex3_index, vertex1_index, vertices, custom_lines);//添加三角图元vertex2和vertex3构成的退化四边形（或叫边）
            AddCustomLine(vertex3_index, vertex1_index, vertex2_index, vertices, custom_lines);//添加三角图元vertex3和vertex1构成的退化四边形（或叫边）
        }

        degraded_rectangles = new List<DegradedRectangle>(custom_lines.Count);
        for (int i = 0; i < custom_lines.Count; i++) {
            degraded_rectangles.Add(custom_lines[i].degraded_rectangle);
        }
        Debug.Log("成功生产退化四边形");
    }
    private void AddCustomLine(int vertex1Index, int vertex2Index, int vertex3Index, Vector3[] meshVertices, List<MeshLine> customLines) {
        Vector3 point1 = meshVertices[vertex1Index];
        Vector3 point2 = meshVertices[vertex2Index];
        MeshLine customLine = new MeshLine(point1, point2);
        if (!customLines.Contains(customLine)) {
            customLine.degraded_rectangle = new DegradedRectangle();
            customLine.degraded_rectangle.vertex1 = vertex1Index;
            customLine.degraded_rectangle.vertex2 = vertex2Index;
            customLine.degraded_rectangle.triangle1_vertex3 = vertex3Index;
            customLine.degraded_rectangle.triangle2_vertex3 = -1;
            customLines.Add(customLine);
        }
        else {
            int i = customLines.IndexOf(customLine);
            var rectangle = customLines[i].degraded_rectangle;
            if (rectangle.triangle2_vertex3 == -1) {
                rectangle.triangle2_vertex3 = vertex3Index;
                customLines[i].degraded_rectangle = rectangle;
            }
        }
    }
    private class MeshLine {
        public Vector3 point1;
        public Vector3 point2;
        public DegradedRectangle degraded_rectangle;

        public MeshLine(Vector3 point1, Vector3 point2) {
            this.point1 = point1;
            this.point2 = point2;
        }

        public static bool operator ==(MeshLine line1, MeshLine line2) {
            return line1.Equals(line2);
        }

        public static bool operator !=(MeshLine line1, MeshLine line2) {
            return !line1.Equals(line2);
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            MeshLine line2 = (MeshLine)obj;

            if (point1 == line2.point1 && point2 == line2.point2) {
                return true;
            }

            if (point1 == line2.point2 && point2 == line2.point1) {
                return true;
            }

            return false;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override string ToString() {
            return string.Format("point1: {0}\npoint2: {1}\nindex.point1: {2}\nindex.point2: {3}\nindex.face_point1: {4}\nindex.face_point2: {5}", point1, point2, degraded_rectangle.vertex1, degraded_rectangle.vertex2, degraded_rectangle.triangle1_vertex3, degraded_rectangle.triangle2_vertex3);
        }
    }

}

//退化四边形
[System.Serializable]
public struct DegradedRectangle {
    public int vertex1;// 构成边的顶点1的索引
    public int vertex2;// 构成边的顶点2的索引
    public int triangle1_vertex3;// 边所在三角面1的顶点3索引
    public int triangle2_vertex3;// 边所在三角面2的顶点3索引
}

public class CustomLine {
    public Vector3 point1;
    public Vector3 point2;
    public DegradedRectangle degraded_rectangle;
}