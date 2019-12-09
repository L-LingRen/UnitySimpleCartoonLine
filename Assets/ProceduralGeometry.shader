// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/ProceduralGeometry" {
    Properties{
        //_MainTex("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    StructuredBuffer<float3> _Vertices;
    StructuredBuffer<float3> _Normals;
    StructuredBuffer<float2> _Uvs;
    float EdgeWidth = 0.01;
    ENDCG

        SubShader{
            Tags { "RenderType" = "Opaque" }

            Pass {
                Cull Back
                CGPROGRAM
                #include "UnityCG.cginc"
                #pragma target 5.0  
                #pragma vertex vertex_shader
                #pragma fragment fragment_shader
                #pragma geometry geometry_shader

                struct g2f {
                    float4 position : SV_POSITION;
                };

                struct v2g {
                    float4 vertex1 : POSITION;
                    float4 vertex2 : COLOR;
                };

                struct Line {
                    int vertex1;
                    int vertex2;
                    int triangle1_vertex3;
                    int triangle2_vertex3;
                };

                StructuredBuffer<Line> _DegradedRectangles;

                v2g vertex_shader(uint id : SV_VertexID, uint inst : SV_InstanceID) {
                    v2g o;
                    //获取退化四边形数据，并得到实际的顶点数据。
                    Line _line = _DegradedRectangles[id];
                    float4 vertex1 = float4(_Vertices[_line.vertex1], 1.0f);
                    float4 vertex2 = float4(_Vertices[_line.vertex2], 1.0f);
                    float3 vertex1_normal = _Normals[_line.vertex1];
                    float3 vertex2_normal = _Normals[_line.vertex2];
                    float4 triangle1_vertex3 = float4(_Vertices[_line.triangle1_vertex3], 1.0f);
                    float4 center_point = (vertex1 + vertex2 + triangle1_vertex3) / 3.0f;
                    float3 view_dir = ObjSpaceViewDir(center_point);

                    bool is_edge = 1;
                    if (_line.triangle2_vertex3 > 0) { // 非边界边
                        float4 triangle2_vertex3 = float4(_Vertices[_line.triangle2_vertex3], 1.0f);

                        float3 v1 = vertex2 - vertex1;
                        float3 v2 = triangle1_vertex3 - vertex1;
                        float3 v3 = triangle2_vertex3 - vertex1;

                        float3 face1Normal = cross(v1, v2);
                        float3 face2Normal = cross(v3, v1);

                        bool is_outline = !step(0, dot(face1Normal, view_dir) * dot(face2Normal, view_dir));
                        bool is_crease = step(pow(dot(face1Normal, face2Normal) / cos(1.0472f), 2), dot(face1Normal, face1Normal) * dot(face2Normal, face2Normal));

                        is_edge = is_outline | is_crease;
                    }

                    // 把顶点转换到裁剪空间
                    o.vertex1 = UnityObjectToClipPos(vertex1 + vertex1_normal * 0.001f) * is_edge;
                    o.vertex2 = UnityObjectToClipPos(vertex2 + vertex2_normal * 0.001f) * is_edge;
                    return o;
                }

                /*
                使用"LineStream"画线条
                use "LineStream" to draw line
                */
                //[maxvertexcount(2)]
                //void geometry_shader(point v2g input[1], inout LineStream<g2f> stream) {
                //    // 使用几何着色器把退化四边形进化成线条
                //    // 直接使用stream.RestartStrip();即可，如有更好的方法请自行实现。
                //    g2f o;
                //    o.position = input[0].vertex1;
                //    stream.Append(o);
                //    o.position = input[0].vertex2;
                //    stream.Append(o);
                //    stream.RestartStrip();
                //}

                /*
                使用"TriangleStream"画2个三角形, 来组成1个四边形模拟线条粗细。
                use "TriangleStream" to draw two triangle to make a quad to control the width of line.
                */
                [maxvertexcount(6)]
                void geometry_shader(point v2g input[1], inout TriangleStream<g2f> stream) {
                    // 使用几何着色器把退化四边形进化成线条
                    // 直接使用stream.RestartStrip();即可，如有更好的方法请自行实现。
                    g2f o;

                    float PctExtend = 0.01;

                    float3 e0 = input[0].vertex1.xyz / input[0].vertex1.w;
                    float3 e1 = input[0].vertex2.xyz / input[0].vertex2.w;
                    float2 ext = PctExtend * (e1.xy - e0.xy);
                    float2 v = normalize(float3(e1.xy - e0.xy, 0)).xy;
                    float2 n = float2(-v.y, v.x) * EdgeWidth;

                    float4 v0 = float4(e0.xy + n / 2.0 - ext, e0.z, 1.0);
                    float4 v1 = float4(e0.xy - n / 2.0 - ext, e0.z, 1.0);
                    float4 v2 = float4(e1.xy + n / 2.0 + ext, e1.z, 1.0);
                    float4 v3 = float4(e1.xy - n / 2.0 + ext, e1.z, 1.0);

                    o.position = v0;
                    stream.Append(o);
                    o.position = v3;
                    stream.Append(o);
                    o.position = v2;
                    stream.Append(o);
                    stream.RestartStrip();

                    o.position = v0;
                    stream.Append(o);
                    o.position = v1;
                    stream.Append(o);
                    o.position = v3;
                    stream.Append(o);
                    stream.RestartStrip();
                }
                

                fixed4 fragment_shader(g2f i) : SV_Target{
                    // 给线条一个颜色
                    return fixed4(0, 0, 0, 0);
                }

                ENDCG
            }
    }
}