using System;
using engenious.Graphics;
using System.Collections.Generic;
using OpenTK;

namespace engenious.Content.Pipeline
{
    [ContentProcessor(DisplayName = "Effect Processor")]
    public class EffectProcessor : ContentProcessor<EffectContent,EffectContent>
    {
        public EffectProcessor()
        {
        }

        public override EffectContent Process(EffectContent input, ContentProcessorContext context)
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
                        var tmp = new Shader(shader.Key, shader.Value);
                        tmp.Compile();
                        compiledPass.AttachShader(tmp);
                    }
                    
                    compiledPass.Link();
                }
            }
            return input;
        }
    }
}

