using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using engenious.Graphics;
using engenious.Content.CodeGenerator;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="EffectContent"/> files from(.glsl).
    /// </summary>
    [ContentImporter(".glsl", DisplayName = "Effect Importer", DefaultProcessor = "EffectProcessor")]
    public class EffectImporter : ContentImporter<EffectContent>
    {
        #region implemented abstract members of ContentImporter

        private static ShaderType ParseShaderType(string type)
        {
            if (type == "PixelShader" || type == "FragmentShader")
                return ShaderType.FragmentShader;
            if (type == "VertexShader")
                return ShaderType.VertexShader;

            if (type == "GeometryShader")
                return ShaderType.GeometryShader;
            if (type == "TessControlShader")
                return ShaderType.TessControlShader;
            if (type == "TessEvaluationShader")
                return ShaderType.TessEvaluationShader;
            if (type == "ComputeShader")
                return ShaderType.ComputeShader;

            return (ShaderType)(-1);
        }

        private static float ParseColorPart(string value)
        {
            value = value.Trim();
            int tmp;
            if (int.TryParse(value, out tmp))
                return tmp / 255.0f;
            return float.Parse(value);
        }

        private static Color ParseColor(XmlElement el)
        {
            if (el.HasChildNodes)
            {
                float a = 1.0f, r = 0, g = 0, b = 0;
                foreach (XmlElement e in el.ChildNodes.OfType<XmlElement>())
                {
                    if (e.Name == "A")
                    {
                        a = ParseColorPart(e.InnerText);
                    }
                    else if (e.Name == "R")
                    {
                        r = ParseColorPart(e.InnerText);
                    }
                    else if (e.Name == "G")
                    {
                        g = ParseColorPart(e.InnerText);
                    }
                    else if (e.Name == "B")
                    {
                        b = ParseColorPart(e.InnerText);
                    }
                    else
                    {
                        throw new Exception("'" + e.Name + "' is not an option for the Color element");
                    }
                }

                return new Color(r, g, b, a);
            }

            if (string.IsNullOrEmpty(el.InnerText.Trim()))
                throw new Exception("Empty value not allowed for Colors");
            try
            {
                var fI = typeof(Color).GetField(el.InnerText.Trim(), BindingFlags.Static);
                var value = fI?.GetValue(null);
                if (value != null) return (Color)value;
            }
            catch
            {
                // ignored
            }

            {
                string value = el.InnerText.Trim();
                int a, r, g, b;
                if (value.Length == 4 || value.Length == 3)
                {
                    int index = 0;
                    if (value.Length == 4)
                        a = Convert.ToInt16(value[index++].ToString(), 16);
                    else
                        a = 0xF;
                    r = Convert.ToInt16(value[index++].ToString(), 16);
                    g = Convert.ToInt16(value[index++].ToString(), 16);
                    b = Convert.ToInt16(value[index].ToString(), 16);

                    a = a << 4 | a;
                    r = r << 4 | r;
                    g = g << 4 | g;
                    b = b << 4 | b;
                }
                else if (value.Length == 6 || value.Length == 8)
                {
                    int index = 0;
                    if (value.Length == 6)
                        a = Convert.ToInt16(value.Substring(index += 2, 2), 16);
                    else
                        a = 0xFF;
                    r = Convert.ToInt16(value.Substring(index += 2, 2), 16);
                    g = Convert.ToInt16(value.Substring(index += 2, 2), 16);
                    b = Convert.ToInt16(value.Substring(index, 2), 16);
                }
                else
                {
                    throw new Exception(
                        "Color must either use A/R/G/B Xml Elements or be a Hexadecimal value of Length 3/4/6/8");
                }


                return new Color(r, g, b, a);
            }
        }

        private static BlendState? ParseBlendState(XmlElement element)
        {
            if (!element.HasChildNodes || element.Name != "BlendState")
                return null;
            BlendState blendState = new BlendState();
            foreach (XmlElement el in element.ChildNodes.OfType<XmlElement>())
            {
                if (el.Name == "AlphaBlendFunction")
                    blendState.AlphaBlendFunction =
                        (BlendEquationMode)Enum.Parse(typeof(BlendEquationMode), el.InnerText);
                else if (el.Name == "AlphaDestinationBlend")
                    blendState.AlphaDestinationBlend =
                        (BlendingFactorDest)Enum.Parse(typeof(BlendingFactorDest), el.InnerText);
                else if (el.Name == "AlphaSourceBlend")
                    blendState.AlphaSourceBlend =
                        (BlendingFactorSrc)Enum.Parse(typeof(BlendingFactorSrc), el.InnerText);
                /*else if (el.Name == "BlendFactor")
                    blendState.BlendFactor = ParseColor(el);*/
                else if (el.Name == "ColorBlendFunction")
                    blendState.ColorBlendFunction =
                        (BlendEquationMode)Enum.Parse(typeof(BlendEquationMode), el.InnerText);
                else if (el.Name == "ColorDestinationBlend")
                    blendState.ColorDestinationBlend =
                        (BlendingFactorDest)Enum.Parse(typeof(BlendingFactorDest), el.InnerText);
                else if (el.Name == "ColorSourceBlend")
                    blendState.ColorSourceBlend =
                        (BlendingFactorSrc)Enum.Parse(typeof(BlendingFactorSrc), el.InnerText);
                else if (el.Name == "ColorWriteChannels")
                    blendState.ColorWriteChannels =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.InnerText);
                else if (el.Name == "ColorWriteChannels1")
                    blendState.ColorWriteChannels1 =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.InnerText);
                else if (el.Name == "ColorWriteChannels2")
                    blendState.ColorWriteChannels2 =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.InnerText);
                else if (el.Name == "ColorWriteChannels3")
                    blendState.ColorWriteChannels3 =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.InnerText);
                /*else if (el.Name == "IndependentBlendEnable")
                    blendState.IndependentBlendEnable = bool.Parse(el.InnerText);
                else if (el.Name == "MultiSampleMask")
                    blendState.MultiSampleMask = int.Parse(el.InnerText);*/
                else
                    throw new Exception("'" + el.Name + "' is not an option of the BlendState");
            }

            return blendState;
        }

        private static DepthStencilState? ParseDepthStencilState(XmlElement element)
        {
            if (!element.HasChildNodes || element.Name != "DepthStencilState")
                return null;
            DepthStencilState depthStencilState = new DepthStencilState();
            foreach (XmlElement el in element.ChildNodes.OfType<XmlElement>())
            {
                if (el.Name == "CounterClockwiseStencilDepthBufferFail")
                    depthStencilState.CounterClockwiseStencilDepthBufferFail =
                        (StencilOp)Enum.Parse(typeof(StencilOp), el.InnerText);
                else if (el.Name == "CounterClockwiseStencilFail")
                    depthStencilState.CounterClockwiseStencilFail =
                        (StencilOp)Enum.Parse(typeof(StencilOp), el.InnerText);
                else if (el.Name == "CounterClockwiseStencilFunction")
                    depthStencilState.CounterClockwiseStencilFunction =
                        (StencilFunction)Enum.Parse(typeof(StencilFunction), el.InnerText);
                else if (el.Name == "CounterClockwiseStencilPass")
                    depthStencilState.CounterClockwiseStencilPass =
                        (StencilOp)Enum.Parse(typeof(StencilOp), el.InnerText);
                else if (el.Name == "DepthBufferEnable")
                    depthStencilState.DepthBufferEnable = bool.Parse(el.InnerText);
                else if (el.Name == "DepthBufferFunction")
                    depthStencilState.DepthBufferFunction =
                        (DepthFunction)Enum.Parse(typeof(DepthFunction), el.InnerText);
                else if (el.Name == "DepthBufferWriteEnable")
                    depthStencilState.DepthBufferWriteEnable = bool.Parse(el.InnerText);
                else if (el.Name == "ReferenceStencil")
                    depthStencilState.ReferenceStencil = int.Parse(el.InnerText);
                else if (el.Name == "DepthBufferFunction")
                    depthStencilState.StencilDepthBufferFail = (StencilOp)Enum.Parse(typeof(StencilOp), el.InnerText);
                else if (el.Name == "ReferenceStencil")
                    depthStencilState.StencilEnable = bool.Parse(el.InnerText);
                else if (el.Name == "StencilFail")
                    depthStencilState.StencilFail = (StencilOp)Enum.Parse(typeof(StencilOp), el.InnerText);
                else if (el.Name == "StencilFunction")
                    depthStencilState.StencilFunction =
                        (StencilFunction)Enum.Parse(typeof(StencilFunction), el.InnerText);
                else if (el.Name == "DepthBufferFunction")
                    depthStencilState.StencilMask = int.Parse(el.InnerText);
                else if (el.Name == "StencilPass")
                    depthStencilState.StencilPass = (StencilOp)Enum.Parse(typeof(StencilOp), el.InnerText);
                /*else if (el.Name == "DepthBufferFunction")
                    depthStencilState.StencilWriteMask = int.Parse(el.InnerText);*/
                else if (el.Name == "TwoSidedStencilMode")
                    depthStencilState.TwoSidedStencilMode = bool.Parse(el.InnerText);
                else
                    throw new Exception("'" + el.Name + "' is not an option of the DepthStencilState");
            }

            return depthStencilState;
        }

        private static RasterizerState? ParseRasterizerState(XmlElement element)
        {
            if (!element.HasChildNodes || element.Name != "RasterizerState")
                return null;
            RasterizerState rasterizerState = new RasterizerState();
            foreach (XmlElement el in element.ChildNodes.OfType<XmlElement>())
            {
                if (el.Name == "CullMode")
                    rasterizerState.CullMode = (CullMode)Enum.Parse(typeof(CullMode), el.InnerText);
                else if (el.Name == "FillMode")
                    rasterizerState.FillMode = (PolygonMode)Enum.Parse(typeof(PolygonMode), el.InnerText);
                else if (el.Name == "MultiSampleAntiAlias")
                    rasterizerState.MultiSampleAntiAlias = bool.Parse(el.InnerText);
                else if (el.Name == "ScissorTestEnable")
                    rasterizerState.ScissorTestEnable = bool.Parse(el.InnerText);
                else if (el.Name == "SlopeScaleDepthBias")
                    rasterizerState.SlopeScaleDepthBias = float.Parse(el.InnerText);
                else if (el.Name == "DepthBias")
                    rasterizerState.DepthBias = float.Parse(el.InnerText);
                /*else if (el.Name == "DepthClipEnable")
                    rasterizerState.DepthClipEnable = bool.Parse(el.InnerText);*/
                else
                    throw new Exception("'" + el.Name + "' is not an option of the RasterizerState");
            }

            return rasterizerState;
        }

        /// <inheritdoc />
        public override EffectContent? Import(string filename, ContentImporterContext context)
        {
            try
            {
                EffectContent content = new EffectContent();

                XmlDocument doc = new XmlDocument();
                doc.Load(filename);
                XmlNode? current = doc.FirstChild;
                while (current != null && current.NodeType != XmlNodeType.Element)
                {
                    current = current.NextSibling;
                }

                if (current == null)
                    throw new XmlException("no xml element found");
                var effectElement = (XmlElement)current;
                foreach (XmlElement technique in effectElement.ChildNodes.OfType<XmlElement>())
                {
                    EffectTechnique info = new EffectTechnique();
                    info.Name = technique.GetAttribute("name");
                    foreach (XmlElement pass in technique.ChildNodes.OfType<XmlElement>())
                    {
                        EffectPass pi = new EffectPass(pass.GetAttribute("name"));
                        foreach (XmlElement sh in pass.ChildNodes.OfType<XmlElement>())
                        {
                            if (sh.Name == "Shader")
                            {
                                ShaderType type = ParseShaderType(sh.GetAttribute("type"));
                                if ((int)type == -1)
                                    throw new Exception("Unsupported Shader type detected");
                                var fnAttr = sh.GetAttribute("filename");
                                var dirName = Path.GetDirectoryName(filename);
                                string shaderFile = dirName == null ? fnAttr : Path.Combine(dirName, fnAttr);
                                pi.Shaders.Add(type, shaderFile);
                                context.Dependencies.Add(shaderFile);
                            }
                            else if (sh.Name == "BlendState")
                            {
                                pi.BlendState = ParseBlendState(sh);
                            }
                            else if (sh.Name == "DepthStencilState")
                            {
                                pi.DepthStencilState = ParseDepthStencilState(sh);
                            }
                            else if (sh.Name == "RasterizerState")
                            {
                                pi.RasterizerState = ParseRasterizerState(sh);
                            }
                            else if (sh.Name == "Attributes")
                            {
                                foreach (XmlElement attr in sh.ChildNodes.OfType<XmlElement>())
                                {
                                    if (attr.Name == "attribute")
                                    {
                                        VertexElementUsage usage =
                                            (VertexElementUsage)Enum.Parse(typeof(VertexElementUsage), attr.InnerText);
                                        string nm = attr.GetAttribute("name");
                                        if (nm.Length < 1)
                                            throw new Exception("Not a valid attribute name'" + nm + "'");
                                        pi.Attributes.Add(usage, nm);
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("'" + sh.Name + "' element not recognized");
                            }
                        }

                        info.Passes.Add(pi);
                    }

                    content.Techniques.Add(info);
                }

                return content;
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename, ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    ///     A class containing effect information for loading and processing.
    /// </summary>
    public class EffectContent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EffectContent"/> class.
        /// </summary>
        public EffectContent()
        {
            Techniques = new List<EffectTechnique>();
        }

        /// <summary>
        ///     Gets a value indicating whether to create user code for the effect.
        /// </summary>
        public bool CreateUserEffect { get; internal set; }

        /// <summary>
        ///     Gets the name of the created user effect.
        /// </summary>
        public string? UserEffectName { get; internal set; }

        /// <summary>
        ///     Gets a collection of the effect techniques of this effect.
        /// </summary>
        public List<EffectTechnique> Techniques { get; }
    }

    /// <summary>
    ///     A class containing effect technique information for loading and processing.
    /// </summary>
    public class EffectTechnique
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EffectTechnique"/> class.
        /// </summary>
        public EffectTechnique()
        {
            Passes = new List<EffectPass>();
            Name = null!;
            UserTechniqueName = null!;
        }

        /// <summary>
        ///     Gets the name of the technique.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Gets the user technique name used for created source code.
        /// </summary>
        /// <remarks><c>null</c></remarks>
        public string? UserTechniqueName { get; internal set; }

        /// <summary>
        ///     Gets a collection of passes this technique uses.
        /// </summary>
        public List<EffectPass> Passes { get; }
    }

    /// <summary>
    ///     A class containing effect pass information for loading and processing.
    /// </summary>
    public class EffectPass
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EffectPass"/> class.
        /// </summary>
        public EffectPass(string name)
        {
            Name = name;
            Shaders = new Dictionary<ShaderType, string>();
            Attributes = new Dictionary<VertexElementUsage, string>();
            Parameters = new List<ParameterInfo>();
        }

        /// <summary>
        ///     Gets the name of the pass.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the <see cref="engenious.Graphics.BlendState"/> used for rendering this pass.
        /// </summary>
        /// <remarks>
        ///     <c>null</c> for using the current <see cref="engenious.Graphics.BlendState"/> of the graphics pipeline.
        /// </remarks>
        public BlendState? BlendState { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="engenious.Graphics.DepthStencilState"/> used for rendering this pass.
        /// </summary>
        /// <remarks>
        ///     <c>null</c> for using the current <see cref="engenious.Graphics.DepthStencilState"/>
        ///     of the graphics pipeline.
        /// </remarks>
        public DepthStencilState? DepthStencilState { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="engenious.Graphics.RasterizerState"/> used for rendering this pass.
        /// </summary>
        /// <remarks>
        ///     <c>null</c> for using the current <see cref="engenious.Graphics.RasterizerState"/>
        ///     of the graphics pipeline.
        /// </remarks>
        public RasterizerState? RasterizerState { get; internal set; }

        /// <summary>
        ///     Gets a mapping of <see cref="ShaderType"/> to a shader file for this pass.
        /// </summary>
        public Dictionary<ShaderType, string> Shaders { get; }

        /// <summary>
        ///     Gets a mapping of <see cref="VertexElementUsage"/> to the attribute names for this pass.
        /// </summary>
        public Dictionary<VertexElementUsage, string> Attributes { get; }

        /// <summary>
        ///     Gets a list of all parameters in this pass.
        /// </summary>
        public List<ParameterInfo> Parameters { get; }
    }

    /// <summary>
    ///     A class containing effect parameter information for loading and processing.
    /// </summary>
    public class ParameterInfo : IEquatable<ParameterInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        public ParameterInfo(string name, Type type)
            : this(name, new TypeReference(type.Namespace, type.Name))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        public ParameterInfo(string name, TypeReference type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        ///     Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the type of the parameter.
        /// </summary>
        public TypeReference Type { get; }

        /// <inheritdoc />
        public bool Equals(ParameterInfo? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Type.Equals(other.Type);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((ParameterInfo)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
    }

    /// <summary>
    ///     A class containing effect parameter struct element information for loading and processing.
    /// </summary>
    public class StructParameterInfo : ParameterInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StructParameterInfo"/> class.
        /// </summary>
        /// <param name="attributeName">The name of the struct attribute in glsl.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="offset">The offset inside the struct.</param>
        public StructParameterInfo(string attributeName, string name, Type type, int offset)
            : base(name, type)
        {
            Offset = offset;
            AttributeName = attributeName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StructParameterInfo"/> class.
        /// </summary>
        /// <param name="attributeName">The name of the struct attribute in glsl.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="offset">The offset inside the struct.</param>
        public StructParameterInfo(string attributeName, string name, TypeReference type, int offset)
            : base(name, type)
        {
            Offset = offset;
            AttributeName = attributeName;
        }

        /// <summary>
        ///     Gets the sub parameters this parameter consists of.
        /// </summary>
        /// <remarks>Returns no elements for primitives and 1 or more for nested structs.</remarks>
        public HashSet<ParameterInfo> SubParameters { get; } = new();

        /// <summary>
        ///     Gets the offset inside the struct.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        ///     Gets or sets the glsl layout size of the struct.
        /// </summary>
        public int LayoutSize { get; set; }

        /// <summary>
        ///     Gets the name of the struct attribute in glsl.
        /// </summary>
        public string AttributeName { get; }
    }

    /// <summary>
    ///     A class containing effect parameter array element information for loading and processing.
    /// </summary>
    public class ArrayParameterInfo : ParameterInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ArrayParameterInfo"/> class.
        /// </summary>
        /// <param name="attributeName">The name of the array attribute in glsl.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="offset">The offset inside the array.</param>
        public ArrayParameterInfo(string attributeName, string name, Type type, int offset)
            : base(name, type)
        {
            Offset = offset;
            AttributeName = attributeName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArrayParameterInfo"/> class.
        /// </summary>
        /// <param name="attributeName">The name of the array attribute in glsl.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="offset">The offset inside the array.</param>
        public ArrayParameterInfo(string attributeName, string name, TypeReference type, int offset)
            : base(name, type)
        {
            Offset = offset;
            AttributeName = attributeName;
        }

        /// <summary>
        ///     Gets the offset inside the struct.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        ///     Gets or sets the length of the array.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        ///     Gets or sets the glsl layout size of the array.
        /// </summary>
        public int LayoutSize { get; set; }

        /// <summary>
        ///     Gets the name of the struct attribute in glsl.
        /// </summary>
        public string AttributeName { get; }
    }
}