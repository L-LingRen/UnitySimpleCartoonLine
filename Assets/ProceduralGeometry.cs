using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;

/*
思路是退化成点，输入到顶点着色器中，并在几何着色器等图元着色器进化成边
输入：
所有顶点，长度为vertices.length。
所有边的点1索引，长度为n。
所有边的点2索引，长度为n。
邻接面1除开边2点的点索引，长度为n。
邻接面2除开边2点的点索引，长度为n。
(注：退化四边形和三角面一样，模型建立完成后顶点的索引不会改变，因此可以保存。)
*/
[ExecuteInEditMode]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class ProceduralGeometry : MonoBehaviour {
    [Range(0, 0.01f)]
    public float edgeWidth = 0.01f;
    public Material prefab_material;              // 预置材质，用于实例化
    public DegradedRectangles degraded_rectangles;// 退化四边形资源文件

    private Mesh bake_mesh;                       // 用于接收动态网格
    private Material material;                    // 动态网格用到的描边材质，由预置材质实例化生成
    private SkinnedMeshRenderer mesh_renderer;
    private CommandBuffer command_buffer;         // 指令缓存
    private List<Vector3> mesh_vertices;          // 网格顶点，用来接收动态网格的顶点信息
    private MaterialBufferManager buffer_manager; // 材质缓存管理器
    private List<Camera> cameras;                 // 用来清空指令缓存
    private int degraded_rectangles_count = 0;    // 退化四边形的个数
    private bool is_visible = false;// 该动态网格是否可见
    private CameraEvent camera_event = CameraEvent.AfterForwardOpaque;

    private void OnEnable() {
        if (prefab_material == null || degraded_rectangles == null) {
            return;
        }
        ReleaseBuffer();
        bake_mesh = new Mesh();
        material = Instantiate(prefab_material);
        mesh_renderer = GetComponent<SkinnedMeshRenderer>();
        mesh_vertices = new List<Vector3>();
        command_buffer = new CommandBuffer();
        buffer_manager = new MaterialBufferManager(mesh_renderer.sharedMesh, degraded_rectangles.degraded_rectangles, material);
        degraded_rectangles_count = buffer_manager.GetLines().count;
        cameras = new List<Camera>();
        Camera.onPreCull += DrawWithCamera;
        command_buffer.name = "Cartoon Line";// 让描边同时在Scene视图和Game视图显示
    }

    private void DrawWithCamera(Camera camera) {
        mesh_renderer.BakeMesh(bake_mesh);
        bake_mesh.GetVertices(mesh_vertices);
        buffer_manager.GetVertices().SetData(mesh_vertices);

        if (camera) {
            if (!cameras.Contains(camera)) {
                cameras.Add(camera);
                camera.AddCommandBuffer(camera_event, command_buffer);
            }

            command_buffer.Clear();
            if (is_visible) { // 模型可见时才进行描边
                command_buffer.DrawProcedural(transform.localToWorldMatrix, material, 0, MeshTopology.Points, degraded_rectangles_count);
                //Graphics.ExecuteCommandBuffer(command_buffer);
                material.SetFloat("EdgeWidth", edgeWidth);
                buffer_manager.SetBuffer();
            }
        }
    }

    private void OnDestroy() {
        ReleaseBuffer();
    }

    private void OnDisable() {
        ReleaseBuffer();
    }

    private void ReleaseBuffer() {
        if (cameras != null) {
            for (int i = 0; i < cameras.Count; i++) {
                var camera = cameras[i];
                if (camera != null && command_buffer != null) {
                    camera.RemoveCommandBuffer(camera_event, command_buffer);
                }
            }
        }

        if (command_buffer != null) command_buffer.Release();
        if (buffer_manager != null) buffer_manager.Release();

        buffer_manager = null;
        command_buffer = null;

        Camera.onPreCull -= DrawWithCamera;
    }

    void OnBecameVisible() {
        is_visible = true;
    }

    void OnBecameInvisible() {
        is_visible = false;
    }
}

// ComputeBuffer比较多，新建一个类来进行管理
public class MaterialBufferManager {
    private Material material;
    private ComputeBuffer vertices;
    private ComputeBuffer normals;
    private ComputeBuffer uvs;
    private ComputeBuffer degraded_rectangles;

    public MaterialBufferManager(Mesh mesh, List<DegradedRectangle> degraded_rectangles, Material material) {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;

        this.normals = new ComputeBuffer(normals.Length, 12, ComputeBufferType.Default);// normals中每个元素都是3个4位的float, 所以是3 * 4 = 12
        this.uvs = new ComputeBuffer(uvs.Length, 8, ComputeBufferType.Default);
        this.degraded_rectangles = new ComputeBuffer(degraded_rectangles.Count, Marshal.SizeOf(typeof(DegradedRectangle)), ComputeBufferType.Default);
        this.vertices = new ComputeBuffer(mesh.vertexCount, 12, ComputeBufferType.Default);

        this.uvs.SetData(uvs);
        this.normals.SetData(normals);
        this.degraded_rectangles.SetData(degraded_rectangles);

        // SetBuffer只需一次，后续直接操作ComputeBuffer即可
        this.material = material;
        material.SetBuffer("_Normals", this.normals);
        material.SetBuffer("_Uvs", this.uvs);
        material.SetBuffer("_DegradedRectangles", this.degraded_rectangles);
        material.SetBuffer("_Vertices", this.vertices);
    }

    public void SetBuffer() {
        material.SetBuffer("_Normals", normals);
        material.SetBuffer("_Uvs", uvs);
        material.SetBuffer("_DegradedRectangles", degraded_rectangles);
        material.SetBuffer("_Vertices", vertices);
    }

    ~MaterialBufferManager() {
        Release();
    }

    public ComputeBuffer GetVertices() {
        return vertices;
    }

    public ComputeBuffer GetNormals() {
        return normals;
    }

    public ComputeBuffer GetUvs() {
        return uvs;
    }

    public ComputeBuffer GetLines() {
        return degraded_rectangles;
    }

    public void Release() {
        if (vertices != null) vertices.Release();
        if (normals != null) normals.Release();
        if (uvs != null) uvs.Release();
        if (degraded_rectangles != null) degraded_rectangles.Release();

        vertices = null;
        normals = null;
        uvs = null;
        degraded_rectangles = null;
    }
}