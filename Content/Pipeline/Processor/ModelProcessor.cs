using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assimp;
using engenious.Content.Pipeline;
using engenious.Graphics;
using Node = Assimp.Node;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Processor that processes Assimp scenes to engenious models.
    /// </summary>
    [ContentProcessor(DisplayName = "Model Processor")]
    public class ModelProcessor : ContentProcessor<Scene, ModelContent, ModelProcessorSettings>
    {
        private NodeContent ParseNode(ModelContent model, Node node,NodeContent? parent = null)
        {
            NodeContent n = new(node.Name, parent);
            model.Nodes.Add(n);
            if (_settings.TransformMesh){
                Matrix matrix = ConvertMatrix(node.Transform);
                matrix.M14 *= _settings.Scale.X;
                matrix.M24 *= _settings.Scale.Y;
                matrix.M34 *= _settings.Scale.Z;

                n.Transformation = matrix;
            }else
                n.Transformation = ConvertMatrix(node.Transform);

            foreach (var meshIndex in node.MeshIndices)
                n.Meshes.Add(meshIndex);
            foreach (var child in node.Children)
                n.Children.Add(ParseNode(model, child,n));
            return n;
        }

        private Matrix ConvertMatrix(Matrix4x4 m)
        {
            return new(m.A1, m.B1, m.C1, m.D1,
                            m.A2, m.B2, m.C2, m.D2,
                            m.A3, m.B3, m.C3, m.D3,
                            m.A4, m.B4, m.C4, m.D4);
        }
        private bool CombineWithParentNode(ModelContent content,NodeContent node)
        {
            if (node.Parent == null)
                return false;
            foreach(var c in node.Children)
            {
                c.Parent = node.Parent;

                node.Parent.Children.Add(c);
                
            }
            node.Children.Clear();
            content.Nodes.Remove(node);

            var changed = node.Parent.Children.Remove(node);
            if (changed)
            {
                foreach(var anim in content.Animations)
                {
                    AnimationNodeContent? c1 = null, c2 = null;
                    foreach(var c in anim.Channels)
                    {
                        if (c.Node == node.Parent)
                            c1 = c;
                        else if(c.Node == node)
                            c2 = c;

                        if (c1 != null && c2 != null)
                            break;
                    }
                    CombineAnimationContent(c1, c2);
                    for(int i=anim.Channels.Count-1;i>= 0;i--)
                    {
                        if (anim.Channels[i].Node == node)
                            anim.Channels.RemoveAt(i);
                    }
                }
            }
            if (!node.Name.Contains("PreRotation"))
                node.Parent.Transformation = node.Parent.Transformation * node.Transformation;

            return changed;
        }
        private void PostProcess(ModelContent content, NodeContent node)
        {
            if (node.Name.Contains("$") && node.Name.Contains("Translation"))
                node.Name = node.Name.Replace("Translation","Transform");
            if (node.Name.Contains("$") && !node.Name.Contains("Transform"))
            {
                if(CombineWithParentNode(content, node) && node.Parent != null)
                    PostProcess(content, node.Parent);
            }
            for (int i=node.Children.Count-1;i>=0;i--){
                var child = node.Children[i];
                if (child.Name.Contains("$") && child.Name.Contains("PreRotation"))
                {
                    foreach(var anim in content.Animations)
                    {
                        var c = anim.Channels.FirstOrDefault(x=> x.Node == node);
                        if (c == null)
                            continue;
                        foreach(var f in c.Frames)
                        {
                            var newVec = Vector3.Transform(child.Transformation, f.Transform.Location);
                            newVec = new Vector3(newVec.X,newVec.Y,-newVec.Z);//TODO: hardcoded?
                            f.Transform = new AnimationTransform(string.Empty,newVec,f.Transform.Scale,f.Transform.Rotation);
                        }
                    }
                }
                PostProcess(content,child);
            }

        }
        private void CombineAnimationContent(AnimationNodeContent? c1, AnimationNodeContent? c2)
        {
            if (c1 == null || c2 == null)
                return;
            var empty = new AnimationFrame(0f, new AnimationTransform(string.Empty,new Vector3(),new Vector3(1),new Quaternion(0,0,0,1)));
            for (int i=0;i<Math.Max(c1.Frames.Count,c2.Frames.Count);i++)
            {
                var f1 = i < c1.Frames.Count ? c1.Frames[i] : empty;
                var f2 = i < c2.Frames.Count ? c2.Frames[i] : empty;
                if (f1 == empty)
                {
                    c1.Frames.Add(f2);
                }
                else
                {
                    var t1 = f1.Transform;
                    var t2 = f2.Transform;
                    f1.Transform = t1 + t2;
                }
            }
        }

        /// <inheritdoc />
        public override ModelContent? Process(Scene scene, string filename, ContentProcessorContext context)
        {
            try
            {
                //AssimpContext c = new AssimpContext();
                //ExportFormatDescription des = c.GetSupportedExportFormats()[0];
                //c.ExportFile(scene,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"test.dae"),des.FormatId);
                ModelContent content = new ModelContent(scene.MeshCount);
                for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
                {
                    var sceneMesh = scene.Meshes[meshIndex];
                    if (!sceneMesh.HasVertices)
                        continue;
                    var verts =  new ConditionalVertexArray(sceneMesh.FaceCount*3,sceneMesh.HasVertices,sceneMesh.HasVertexColors(0),sceneMesh.HasNormals,sceneMesh.HasTextureCoords(0));
                    
                    var meshContent = new MeshContent(sceneMesh.FaceCount, verts);
                    
                    int vertex=0;
                    //TODO: indexing
                    
                    foreach(var f in sceneMesh.Faces)
                    {
                        foreach(var i in f.Indices)
                        {

                            if (meshContent.Vertices.HasPositions)
                            {
                                var pos = sceneMesh.Vertices[i];
                                var translated = new Vector3(pos.X, pos.Y, pos.Z)+_settings.Translate;
                                meshContent.Vertices.AsPosition![vertex] =new Vector3(translated.X*_settings.Scale.X,translated.Y*_settings.Scale.Y,translated.Z*_settings.Scale.Z);
                            }

                            if (meshContent.Vertices.HasNormals)
                            {
                                var norm = sceneMesh.Normals[i];
                                meshContent.Vertices.AsNormal![vertex] = new Vector3(norm.X, norm.Y, norm.Z);   
                            }

                            if (meshContent.Vertices.HasColors)
                            {
                                var col = sceneMesh.VertexColorChannels[0][i];
                                meshContent.Vertices.AsColor![vertex] = new Color(col.R,col.G,col.B,col.A);
                            }
                            if (meshContent.Vertices.HasTextureCoordinates && sceneMesh.TextureCoordinateChannels.Length > 0 && sceneMesh.TextureCoordinateChannels[0].Count > i)
                            {
                                var tex = sceneMesh.TextureCoordinateChannels[0][i];
                                
                                meshContent.Vertices.AsTextureCoordinate![vertex] =new Vector2(tex.X, -tex.Y);
                            }

                            ++vertex;
                        }
                    }
                    /*for (int i = 0; i < sceneMesh.VertexCount; i++)
                    {

                    }*/

                    content.Meshes[meshIndex] = meshContent;
                }
                content.RootNode = ParseNode(content, scene.RootNode);
                foreach(var animation in scene.Animations){
                    var anim = new AnimationContent();
                    anim.Channels = new List<AnimationNodeContent>();
                    foreach (var channel in animation.NodeAnimationChannels)
                    {
                        var curNode = content.Nodes.First(n => n.Name == channel.NodeName);
                        AnimationNodeContent node = new AnimationNodeContent(curNode);
                        int frameCount = Math.Max(Math.Max(channel.PositionKeyCount, channel.RotationKeyCount), channel.ScalingKeyCount);
                        float diff=0.0f,maxTime = 0;
                        for (int i = 0; i < frameCount; i++)
                        {
                            float frameTime = 0f;

                            if (i < channel.PositionKeyCount)
                                frameTime = (float)channel.PositionKeys[i].Time;
                            else if (i < channel.RotationKeyCount)
                                frameTime = (float)channel.RotationKeys[i].Time;
                            else if (i < channel.ScalingKeyCount)
                                frameTime = (float)channel.ScalingKeys[i].Time;
                            if (i == 0)
                                diff = frameTime;
                            frameTime -= diff;
                            frameTime = (float)(frameTime / animation.TicksPerSecond);
                            maxTime = Math.Max(frameTime, maxTime);
                            //TODO: interpolation
                            var rot = channel.RotationKeyCount == 0 ? new Assimp.Quaternion(1, 0, 0, 0) : i >= channel.RotationKeyCount ? channel.RotationKeys.Last().Value : channel.RotationKeys[i].Value;
                            var loc = channel.PositionKeyCount == 0 ? new Vector3D() : i >= channel.PositionKeyCount ? channel.PositionKeys.Last().Value : channel.PositionKeys[i].Value;
                            var sca = channel.ScalingKeyCount == 0 ? new Vector3D(1, 1, 1) : i >= channel.ScalingKeyCount ? channel.ScalingKeys.Last().Value : channel.ScalingKeys[i].Value;
                            rot.Normalize();
                            
                            var relativeTransform = Matrix.Invert(node.Node.Transformation);
                            
                            var transform = new AnimationTransform(node.Node.Name,
                                new Vector3((loc.X + _settings.Translate.X), (loc.Y + _settings.Translate.Y),
                                    (loc.Z + _settings.Translate.Z)),
                                new Vector3(sca.X * _settings.Scale.X, sca.Y * _settings.Scale.Y,
                                    sca.Z * _settings.Scale.Z), new Quaternion(rot.X, rot.Y, rot.Z, rot.W));

                            var tmp = transform.ToMatrix() * relativeTransform;

                            var (relLoc, relScal, relRot) = tmp;

                            transform = new AnimationTransform(node.Node.Name,
                                relLoc, relScal, relRot);

                            // if (tmp != transform.ToMatrix())
                            //     throw new Exception();
                            
                            AnimationFrame frame = new AnimationFrame(frameTime, transform);
                            node.Frames.Add(frame);
                        }
                        anim.MaxTime = maxTime;
                        anim.Channels.Add(node);
                    }
                    content.Animations.Add(anim);
                }
                PostProcess(content,content.RootNode);



                return content;
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename, ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }

    }
    /// <summary>
    ///     <see cref="ModelProcessor"/> specific settings.
    /// </summary>
    [Serializable]
    public class ModelProcessorSettings : ProcessorSettings
    {
        /// <summary>
        ///     Gets or sets the scaling transformation to be applied to the model in the processing step.
        /// </summary>
        [Category("Settings")]
        [DefaultValue("[1, 1, 1]")]
        public Vector3 Scale { get; set; } = new Vector3(1);

        /// <summary>
        ///     Gets or sets the translation transformation to be applied to the model in the processing step.
        /// </summary>
        [Category("Settings")]
        [DefaultValue("[0, 0, 0]")]
        public Vector3 Translate { get; set; } = new Vector3();

        /// <summary>
        ///     Gets or sets the rotation transformation to be applied to the model in the processing step.
        /// </summary>
        [Category("Settings")]
        [DefaultValue("[0, 0, 0]")]
        public Vector3 Rotation { get; set; } = new Vector3();

        /// <summary>
        ///     Gets or sets a value indicating whether the model should be transformed in the processing step.
        /// </summary>
        [DefaultValue(true)] public bool TransformMesh { get; set; } = true;
    }
}

