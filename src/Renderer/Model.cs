using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Materials;
using Silk.NET.Assimp;

namespace Renderer;

public class Model : IDisposable
{
    private Assimp _assimp;
    
    private Renderable _renderable;
    
    public unsafe Model(Renderer renderer, string path)
    {
        _assimp = Assimp.GetApi();
        Scene* scene = _assimp.ImportFile(path,
            (uint) (PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals |
                    PostProcessSteps.GenerateUVCoords | PostProcessSteps.MakeLeftHanded));

        if (scene == null)
            throw new Exception($"Failed to load scene! {_assimp.GetErrorStringS()}");

        Mesh* mesh = scene->MMeshes[0];
        Vector3* positions = mesh->MVertices;
        Vector3* texCoords = mesh->MTextureCoords.Element0;
        Vector4* colors = mesh->MColors.Element0;
        Vector3* normals = mesh->MNormals;

        Vertex[] vertices = new Vertex[mesh->MNumVertices];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vertex
            {
                Position = positions[i],
                Color = Color.White,
                Normal = normals[i]
            };

            if (texCoords != null)
                vertices[i].TexCoord = new Vector2(texCoords[i].X, texCoords[i].Y);

            if (colors != null)
                vertices[i].Color = new Color(colors[i].X, colors[i].Y, colors[i].Z, colors[i].W);
        }

        List<uint> indices = [];
        for (int f = 0; f < mesh->MNumFaces; f++)
        {
            Face* face = &mesh->MFaces[f];
            
            for (int i = 0; i < face->MNumIndices; i++)
                indices.Add(face->MIndices[i]);
        }

        //Silk.NET.Assimp.Material* material = scene->MMaterials[mesh->MMaterialIndex];
        //_assimp.GetMaterialProperty(material, Assimp.MaterialTe)
        //scene->MTextures[0].

        //byte[] texData = new byte[str.Length];
        //fixed (byte* pData = texData)
        //    Unsafe.CopyBlock(pData, str.Data, str.Length);

        //Texture albedo = new Texture(renderer, new Bitmap(texData));
        Texture albedo = renderer.WhiteTexture;

        _renderable = new Renderable(renderer, new StandardMaterial(renderer, albedo), vertices,
            indices.ToArray());
    }
    
    public void Dispose()
    {
        
    }

    public void Draw(Renderer renderer, in Matrix4x4 worldMatrix)
    {
        renderer.Draw(_renderable, in worldMatrix);
    }
}