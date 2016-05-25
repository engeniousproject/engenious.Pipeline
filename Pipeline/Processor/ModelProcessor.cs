﻿using System;
using engenious.Content.Pipeline;
using engenious.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace engenious.Pipeline
{
    [ContentProcessor(DisplayName = "Model Processor")]
    public class ModelProcessor : ContentProcessor<Assimp.Scene, ModelContent,ModelProcessorSettings>
    {
        public ModelProcessor()
        {
        }

        private NodeContent ParseNode(ModelContent model, Assimp.Node node)
        {
            NodeContent n = new NodeContent();
            Matrix matrix = ConvertMatrix(node.Transform);

            model.Nodes.Add(n);
            n.Transformation = matrix;

            n.Name = node.Name;
            n.Meshes = new List<int>();
            foreach (var meshIndex in node.MeshIndices)
                n.Meshes.Add(meshIndex);
            n.Children = new List<NodeContent>();
            foreach (var child in node.Children)
                n.Children.Add(ParseNode(model, child));
            return n;
        }

        private Matrix ConvertMatrix(Assimp.Matrix4x4 m)
        {
            return new Matrix(m.A1, m.A2, m.A3, m.A4,
                m.B1, m.B2, m.B3, m.B4,
                m.C1, m.C2, m.C3, m.C4,
                m.D1, m.D2, m.D3, m.D4);
        }

        public override ModelContent Process(Assimp.Scene scene, string filename, ContentProcessorContext context)
        {
            try
            {
                ModelContent content = new ModelContent();
                content.Meshes = new MeshContent[scene.MeshCount];
                Matrix matrix= Matrix.CreateScaling(settings.Scale)*Matrix.CreateTranslation(settings.Translate);
                for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
                {
                    var sceneMesh = scene.Meshes[meshIndex];
                    var meshContent = new MeshContent();
                    meshContent.PrimitiveCount = sceneMesh.FaceCount;

                    meshContent.Vertices = new VertexPositionNormalTexture[sceneMesh.VertexCount];
                    for (int i = 0; i < sceneMesh.VertexCount; i++)
                    {
                        var pos = sceneMesh.Vertices[i];
                        var norm = sceneMesh.Normals[i];
                        var tex = sceneMesh.TextureCoordinateChannels[0][i];

                        meshContent.Vertices[i] = new VertexPositionNormalTexture(
                            Vector3.Transform(new Vector3(pos.X, pos.Y, pos.Z),matrix),
                            new Vector3(norm.X, norm.Y, norm.Z),
                            new Vector2(tex.X, -tex.Y));
                    }

                    content.Meshes[meshIndex] = meshContent;
                }
                content.Nodes = new List<NodeContent>();
                content.RootNode = ParseNode(content, scene.RootNode);
                content.Animations = new List<AnimationContent>();

                foreach(var animation in scene.Animations){
                    var anim = new AnimationContent();
                    anim.Channels = new List<AnimationNodeContent>();
                    float maxTime = 0;
                    foreach (var channel in animation.NodeAnimationChannels)
                    {
                        AnimationNodeContent node = new AnimationNodeContent();
                        var curNode = content.Nodes.First(n => n.Name == channel.NodeName);
                        node.Node = curNode;
                        node.Frames = new List<AnimationFrame>();
                        for (int i = 0; i < Math.Max(Math.Max(channel.PositionKeyCount, channel.RotationKeyCount), channel.ScalingKeyCount); i++)
                        {
                            AnimationFrame frame = new AnimationFrame();

                            if (i < channel.PositionKeyCount)
                                frame.Frame = (float)channel.PositionKeys[i].Time;
                            else if (i < channel.RotationKeyCount)
                                frame.Frame = (float)channel.RotationKeys[i].Time;
                            else if (i < channel.ScalingKeyCount)
                                frame.Frame = (float)channel.ScalingKeys[i].Time;
                            frame.Frame = (float)(frame.Frame / animation.TicksPerSecond);
                            maxTime = Math.Max(frame.Frame, maxTime);
                            //TODO: interpolation
                            var rot = channel.RotationKeyCount == 0 ? new Assimp.Quaternion(1, 0, 0, 0) : i >= channel.RotationKeyCount ? channel.RotationKeys.Last().Value : channel.RotationKeys[i].Value;
                            var loc = channel.PositionKeyCount == 0 ? new Assimp.Vector3D() : i >= channel.PositionKeyCount ? channel.PositionKeys.Last().Value : channel.PositionKeys[i].Value;
                            var sca = channel.ScalingKeyCount == 0 ? new Assimp.Vector3D(1, 1, 1) : i >= channel.ScalingKeyCount ? channel.ScalingKeys.Last().Value : channel.ScalingKeys[i].Value;
                            rot.Normalize();

                            frame.Transform = new AnimationTransform(new Vector3(loc.X, loc.Y, loc.Z), new Vector3(sca.X, sca.Y, sca.Z), new Quaternion(rot.X, rot.Y, rot.Z, rot.W));
                            node.Frames.Add(frame);
                        }
                        anim.MaxTime = maxTime;
                        anim.Channels.Add(node);
                    }
                    content.Animations.Add(anim);
                }
                return content;
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename, ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
    public class ModelProcessorSettings : ProcessorSettings
    {
        [Category("Settings")]
        [DefaultValue("[1, 1, 1]")]
        public Vector3 Scale{get;set;}=new Vector3(1);
        [Category("Settings")]
        [DefaultValue("[0, 0, 0]")]
        public Vector3 Translate{get;set;} = new Vector3();
    }
}

