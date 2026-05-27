using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Renderer.Materials;
using Silk.NET.Assimp;
using Material = Renderer.Materials.Material;
using AssimpTexture = Silk.NET.Assimp.Texture;
using AssimpMaterial = Silk.NET.Assimp.Material;

namespace Renderer;

public class Model : IDisposable
{
    private Assimp _assimp;

    private Texture[] _textures;
    private Material[] _materials;
    private Renderable[] _renderables;
    
    private RenderableNode _rootNode;
    
    public unsafe Model(Renderer renderer, string path)
    {
        _assimp = Assimp.GetApi();
        Scene* scene = _assimp.ImportFile(path,
            (uint) (PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals |
                    PostProcessSteps.GenerateUVCoords | PostProcessSteps.FlipUVs));

        if (scene == null)
            throw new Exception($"Failed to load scene! {_assimp.GetErrorStringS()}");

        _textures = new Texture[scene->MNumTextures];
        for (int i = 0; i < scene->MNumTextures; i++)
        {
            AssimpTexture* texture = scene->MTextures[i];
            // TODO: Handle textures with filenames
            // TODO: Handle textures where PcData is the actual texel data
            byte[] texData = new byte[texture->MWidth];
            fixed (byte* pData = texData)
                Unsafe.CopyBlock(pData, texture->PcData, texture->MWidth);
         
            _textures[i] = new Texture(renderer, new Bitmap(texData));
        }

        _materials = new Material[scene->MNumMaterials];
        for (int i = 0; i < scene->MNumMaterials; i++)
        {
            AssimpMaterial* material = scene->MMaterials[i];

            // Fallback to white texture if no albedo/base texture is found.
            if (!TryGetMaterialTexture(material, TextureType.BaseColor, out Texture albedo))
                albedo = renderer.WhiteTexture;

            StandardMaterial mat = new StandardMaterial(renderer, albedo);
            _materials[i] = mat;

            if (TryGetMaterialTexture(material, TextureType.Normals, out Texture normal))
                mat.Normal = normal;
            if (TryGetMaterialTexture(material, TextureType.Metalness, out Texture metallic))
                mat.Metallic = metallic;
            if (TryGetMaterialTexture(material, TextureType.DiffuseRoughness, out Texture roughness))
                mat.Roughness = roughness;
            if (TryGetMaterialTexture(material, TextureType.AmbientOcclusion, out Texture occlusion))
                mat.Occlusion = occlusion;
            // TODO: MetallicRoughness. Assimp may handle glTF differently, not sure here.
        }

        _renderables = new Renderable[scene->MNumMeshes];
        for (int m = 0; m < scene->MNumMeshes; m++)
        {
            Mesh* mesh = scene->MMeshes[m];
            
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

            _renderables[m] = new Renderable(renderer, _materials[mesh->MMaterialIndex], vertices, indices.ToArray());
        }

        _rootNode = ProcessNode(scene->MRootNode);
    }
    
    public void Dispose()
    {
        foreach (Renderable renderable in _renderables)
            renderable.Dispose();
        
        foreach (Material material in _materials)
            material.Dispose();
        
        foreach (Texture texture in _textures)
            texture.Dispose();
        
        _assimp.Dispose();
    }

    public void Draw(Renderer renderer, in Matrix4x4 worldMatrix)
    {
        _rootNode.Draw(renderer, worldMatrix);
    }

    private unsafe RenderableNode ProcessNode(Node* node)
    {
        RenderableNode renderableNode = new RenderableNode();
        renderableNode.Transform = node->MTransformation;
        
        for (int i = 0; i < node->MNumMeshes; i++)
            renderableNode.Renderables.Add(_renderables[node->MMeshes[i]]);

        for (int i = 0; i < node->MNumChildren; i++)
            renderableNode.Children.Add(ProcessNode(node->MChildren[i]));
        
        return renderableNode;
    }

    private unsafe bool TryGetMaterialTexture(AssimpMaterial* material, TextureType type, [NotNullWhen(true)] out Texture? texture)
    {
        AssimpString str;
        Return result = _assimp.GetMaterialString(material, Assimp.MatkeyTextureBase, (uint) type, 0, &str);

        if (result != Return.Success)
        {
            texture = null;
            return false;
        }

        // mmmm strings returning as indexes, gotta love it
        string index = str.AsString.Trim('*');
        texture = _textures[int.Parse(index)];
        return true;
    }

    private class RenderableNode
    {
        public Matrix4x4 Transform;
        public List<Renderable> Renderables;
        public List<RenderableNode> Children;

        public RenderableNode()
        {
            Transform = Matrix4x4.Identity;
            Renderables = [];
            Children = [];
        }

        public void Draw(Renderer renderer, Matrix4x4 transform)
        {
            Matrix4x4 thisTransform = Transform * transform;
            
            foreach (Renderable renderable in Renderables)
                renderer.Draw(renderable, thisTransform);
            
            foreach (RenderableNode node in Children)
                node.Draw(renderer, thisTransform);
        }
    }
}