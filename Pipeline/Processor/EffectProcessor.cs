﻿using System;
using engenious.Graphics;
using System.Collections.Generic;
using OpenTK;

namespace engenious.Content.Pipeline
{
    [ContentProcessor(DisplayName = "Effect Processor")]
    public class EffectProcessor : ContentProcessor<EffectContent, EffectContent>
    {
        public EffectProcessor()
        {
        }
        private string PreprocessMessage(ContentProcessorContext context,string file, string msg, BuildMessageEventArgs.BuildMessageType messageType)
        {
            string[] lines = msg.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                {
                    lines[i] = lines[i].Substring("ERROR: 0:".Length);
                    int eInd = lines[i].IndexOf(':');
                    string errorLoc = "";
                    if (eInd != -1)
                    {
                        errorLoc = "(" + lines[i].Substring(0, eInd) + ")";
                        lines[i] = lines[i].Substring(eInd + 1);
                    }
                    lines[i] = errorLoc + ":ERROR:" + lines[i];
                }
                else
                {

                }
                context.RaiseBuildMessage(file,lines[i],messageType);
            }

            return string.Join("\n", lines);
        }
        public override EffectContent Process(EffectContent input, string filename, ContentProcessorContext context)
        {
            try
            {
                //Passthrough and verification
                foreach (var technique in input.Techniques)
                {
                    foreach (var pass in technique.Passes)
                    {
                        engenious.Graphics.EffectPass compiledPass = new engenious.Graphics.EffectPass(pass.Name);
                        compiledPass.Apply();
                        foreach (var attr in pass.Attributes)
                        {
                            compiledPass.BindAttribute(attr.Key, attr.Value);
                        }
                        foreach (var shader in pass.Shaders)
                        {
                            try
                            {
                                var tmp = new Shader(shader.Key, System.IO.File.ReadAllText(shader.Value));
                                tmp.Compile();
                                compiledPass.AttachShader(tmp);
                            }
                            catch (Exception ex)
                            {
                                PreprocessMessage(context,shader.Value, ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
                                
                            }
                        }

                        compiledPass.Link();
                    }
                }
                return input;
            }
            catch (Exception ex)
            {
                PreprocessMessage(context,System.IO.Path.GetFileName(filename), ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
}

