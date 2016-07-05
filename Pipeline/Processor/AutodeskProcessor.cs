using System;
using engenious.Content.Pipeline;
using engenious.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using FBXImporter;
namespace engenious.Pipeline
{
    [ContentProcessor(DisplayName = "Autodesk Processor")]
    public class AutodeskProcessor : ContentProcessor<FbxScene,ModelContent,ModelProcessorSettings>
    {
        public AutodeskProcessor()
        {

        }

        private List<FbxMesh> traverseMeshes(FbxNode n,List<FbxMesh> lst=null)
        {
            if (lst == null)
                lst= new List<FbxMesh>();
            if (n.NodeType == NodeType.eMesh)
                lst.Add((FbxMesh)n);
            foreach(var c in n.ChildNodes)
                traverseMeshes(c,lst);
            return lst;
        }
        private List<NodeContent> traverseBones(NodeContent n,List<NodeContent> lst=null)
        {
            if (lst == null)
                lst= new List<NodeContent>();
            lst.Add(n);
            foreach(var c in n.Children)
                traverseBones(c,lst);
            return lst;
        }
        private NodeContent traverseBones(FbxNode n,Dictionary<string,int> meshTable,Matrix transformMatrix,NodeContent parent=null)
        {
            var bone = analyzeBone(meshTable,transformMatrix,n);
            foreach(var c in n.ChildNodes)
            {
                if (c.NodeType == NodeType.eSkeleton)
                {
                    bone.Children.Add(traverseBones(c,meshTable,transformMatrix,bone));
                }
                else
                    traverseBones(c,meshTable,transformMatrix,bone);
            }
            return bone;
        }
        //private int findMesh(ModelContent model,
        private NodeContent analyzeBone(Dictionary<string,int> meshTable,Matrix transformMatrix,FbxNode node)
        {
            NodeContent content = new NodeContent(){Name = node.Name};
            content.Transformation = transformMatrix*node.Transform;
            content.Meshes = new List<int>();
            content.Children = new List<NodeContent>();
            //if (node.ChildNodes.Count > 1){
                foreach(var c in node.ChildNodes)
                {
                    if (c.NodeType == NodeType.eMesh)
                    {
                        var transformBone = new NodeContent();
                        transformBone.Name = c.Name;// + "_$Transformation$";
                        transformBone.Meshes = new List<int>();
                        transformBone.Children = new List<NodeContent>();
                        if (meshTable.ContainsKey(c.Name)){
                            transformBone.Meshes.Add(meshTable[c.Name]);
                            transformBone.Transformation = transformMatrix*c.Transform;
                            content.Children.Add(transformBone);
                        }
                    }
                }
            /*}else if (node.ChildNodes.Count == 1){//TODO verify
                var c = node.ChildNodes.First();
                if (c.NodeType == NodeType.eMesh){
                    if (meshTable.ContainsKey(c.Name)){
                        content.Meshes.Add(meshTable[c.Name]);
                        content.Transformation =transformMatrix * content.Transformation * c.Transform;
                    }
                }
            }*/

            return content;
        }
        public override ModelContent Process(FbxScene scene, string filename, ContentProcessorContext context)
        {
            Matrix transformMatrix = Matrix.Identity;
            Matrix rotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(settings.Rotation.Z))*
                Matrix.CreateRotationY(MathHelper.ToRadians(settings.Rotation.Y))*
                Matrix.CreateRotationX(MathHelper.ToRadians(settings.Rotation.X));
            transformMatrix = Matrix.CreateTranslation(settings.Translate)*rotationMatrix*Matrix.CreateScaling(settings.Scale);

            ModelContent model = new ModelContent();
            var meshes = traverseMeshes(scene.RootNode);
            model.Meshes = new MeshContent[meshes.Count];
            Dictionary<string,int> meshTable = new Dictionary<string, int>();
            int i=0;
            foreach(var m in meshes)
            {
                var meshContent = new MeshContent(){Vertices = m.Vertices,PrimitiveCount = m.Vertices.Length/3};

                meshTable.Add(m.Name,i);
                model.Meshes[i++] = meshContent;
            }

            model.Nodes = new List<NodeContent>();
            var rootBone = traverseBones(scene.RootNode,meshTable,transformMatrix);
            foreach(var node in traverseBones(rootBone))
            {
                model.Nodes.Add(node);
            }

            model.Animations = new List<AnimationContent>();
            string nodeNames = string.Join(",",model.Nodes.Select(n=>n.Name));
            foreach(var anim in scene.AnimStack)
            {
                var animation = new AnimationContent();
                animation.Channels = new List<AnimationNodeContent>();
                float maxTime = 0;
                foreach(var layer in anim.Layer)
                {
                    foreach(var c in layer.Channels)
                    {
                        if (c.Frames.Count == 0)
                            continue;
                        AnimationNodeContent node = new AnimationNodeContent();
                        node.Frames = new List<AnimationFrame>();

                        var curNode = model.Nodes.FirstOrDefault(n => n.Name == c.Node.Name);
                        node.Node = curNode;
                        foreach(var f in c.Frames)
                        {
                            AnimationFrame frame = new AnimationFrame();
                            frame.Frame = f.Time;
                            frame.Transform = new AnimationTransform(node.Node?.Name,f.Translation,f.Scaling,f.Rotation);
                            maxTime = Math.Max(frame.Frame, maxTime);
                            node.Frames.Add(frame);
                        }

                        animation.Channels.Add(node);
                    }
                }
                //animation.Channels.Sort((x,y)=>string.Compare(x.Node.Name,y.Node.Name));
                animation.MaxTime = maxTime;
                model.Animations.Add(animation);
            }

            model.RootNode = rootBone;
            return model;
        }
    }

}

