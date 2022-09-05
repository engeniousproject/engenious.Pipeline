using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using engenious.Graphics;
using engenious.Content.CodeGenerator;
using engenious.Pipeline.Helper;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="EffectContent"/> files from(.glsl).
    /// </summary>
    [ContentImporter(".glsl", DisplayName = "Effect Importer", DefaultProcessor = "EffectProcessor")]
    public class EffectImporter : ContentImporter<EffectContent>
    {
        #region implemented abstract members of ContentImporter

        [DoesNotReturn]
        private static void ThrowXmlError(string message, IXmlLineInfo? location)
        {
            if (location is null)
                throw new FormatException($":error:{message}");
            else
                throw new FormatException($"({location.LineNumber},{location.LinePosition}):error:{message}");
        }

        private static ShaderType ParseShaderType(string? type)
        {
            if (type is null)
                return (ShaderType)(-1);

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

        private static IXmlLineInfo? GetInnerElementPosition(XElement? element)
        {
            return GetElementPosition(element?.FirstNode) ?? GetElementPosition(element);
        }
        private static IXmlLineInfo? GetElementPosition(XObject? element)
        {
            return element;
        }
        private static Color ParseColor(XElement el)
        {
            if (el.HasElements)
            {
                float a = 1.0f, r = 0, g = 0, b = 0;
                foreach (var e in el.Elements())
                {
                    if (e.Name == "A")
                    {
                        a = ParseColorPart(e.Value);
                    }
                    else if (e.Name == "R")
                    {
                        r = ParseColorPart(e.Value);
                    }
                    else if (e.Name == "G")
                    {
                        g = ParseColorPart(e.Value);
                    }
                    else if (e.Name == "B")
                    {
                        b = ParseColorPart(e.Value);
                    }
                    else
                    {
                        ThrowXmlError("'" + e.Name + "' is not an option for the Color element", GetElementPosition(e));
                    }
                }

                return new Color(r, g, b, a);
            }

            if (string.IsNullOrEmpty(el.Value.Trim()))
                ThrowXmlError("Empty value not allowed for Colors", GetInnerElementPosition(el));
            try
            {
                var fI = typeof(Color).GetField(el.Value.Trim(), BindingFlags.Static);
                var value = fI?.GetValue(null);
                if (value != null) return (Color)value;
            }
            catch
            {
                // ignored
            }

            {
                string value = el.Value.Trim();
                int a = 0, r = 0, g = 0, b = 0;
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
                    ThrowXmlError(
                        "Color must either use A/R/G/B Xml Elements or be a Hexadecimal value of Length 3/4/6/8", GetInnerElementPosition(el));
                }


                return new Color(r, g, b, a);
            }
        }

        private static BlendState? ParseBlendState(XElement element)
        {
            if (!element.HasElements || element.Name != "BlendState")
                return null;
            BlendState blendState = new BlendState();
            foreach (var el in element.Elements())
            {
                if (el.Name == "AlphaBlendFunction")
                    blendState.AlphaBlendFunction =
                        (BlendEquationMode)Enum.Parse(typeof(BlendEquationMode), el.Value);
                else if (el.Name == "AlphaDestinationBlend")
                    blendState.AlphaDestinationBlend =
                        (BlendingFactorDest)Enum.Parse(typeof(BlendingFactorDest), el.Value);
                else if (el.Name == "AlphaSourceBlend")
                    blendState.AlphaSourceBlend =
                        (BlendingFactorSrc)Enum.Parse(typeof(BlendingFactorSrc), el.Value);
                /*else if (el.Name == "BlendFactor")
                    blendState.BlendFactor = ParseColor(el);*/
                else if (el.Name == "ColorBlendFunction")
                    blendState.ColorBlendFunction =
                        (BlendEquationMode)Enum.Parse(typeof(BlendEquationMode), el.Value);
                else if (el.Name == "ColorDestinationBlend")
                    blendState.ColorDestinationBlend =
                        (BlendingFactorDest)Enum.Parse(typeof(BlendingFactorDest), el.Value);
                else if (el.Name == "ColorSourceBlend")
                    blendState.ColorSourceBlend =
                        (BlendingFactorSrc)Enum.Parse(typeof(BlendingFactorSrc), el.Value);
                else if (el.Name == "ColorWriteChannels")
                    blendState.ColorWriteChannels =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.Value);
                else if (el.Name == "ColorWriteChannels1")
                    blendState.ColorWriteChannels1 =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.Value);
                else if (el.Name == "ColorWriteChannels2")
                    blendState.ColorWriteChannels2 =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.Value);
                else if (el.Name == "ColorWriteChannels3")
                    blendState.ColorWriteChannels3 =
                        (ColorWriteChannels)Enum.Parse(typeof(ColorWriteChannels), el.Value);
                /*else if (el.Name == "IndependentBlendEnable")
                    blendState.IndependentBlendEnable = bool.Parse(el.Value);
                else if (el.Name == "MultiSampleMask")
                    blendState.MultiSampleMask = int.Parse(el.Value);*/
                else
                    ThrowXmlError("'" + el.Name + "' is not an option of the BlendState", GetElementPosition(el));
            }

            return blendState;
        }

        private static DepthStencilState? ParseDepthStencilState(XElement element)
        {
            if (!element.HasElements || element.Name != "DepthStencilState")
                return null;
            DepthStencilState depthStencilState = new DepthStencilState();
            foreach (var el in element.Elements())
            {
                if (el.Name == "CounterClockwiseStencilDepthBufferFail")
                    depthStencilState.CounterClockwiseStencilDepthBufferFail =
                        (StencilOp)Enum.Parse(typeof(StencilOp), el.Value);
                else if (el.Name == "CounterClockwiseStencilFail")
                    depthStencilState.CounterClockwiseStencilFail =
                        (StencilOp)Enum.Parse(typeof(StencilOp), el.Value);
                else if (el.Name == "CounterClockwiseStencilFunction")
                    depthStencilState.CounterClockwiseStencilFunction =
                        (StencilFunction)Enum.Parse(typeof(StencilFunction), el.Value);
                else if (el.Name == "CounterClockwiseStencilPass")
                    depthStencilState.CounterClockwiseStencilPass =
                        (StencilOp)Enum.Parse(typeof(StencilOp), el.Value);
                else if (el.Name == "DepthBufferEnable")
                    depthStencilState.DepthBufferEnable = bool.Parse(el.Value);
                else if (el.Name == "DepthBufferFunction")
                    depthStencilState.DepthBufferFunction =
                        (DepthFunction)Enum.Parse(typeof(DepthFunction), el.Value);
                else if (el.Name == "DepthBufferWriteEnable")
                    depthStencilState.DepthBufferWriteEnable = bool.Parse(el.Value);
                else if (el.Name == "ReferenceStencil")
                    depthStencilState.ReferenceStencil = int.Parse(el.Value);
                else if (el.Name == "DepthBufferFunction")
                    depthStencilState.StencilDepthBufferFail = (StencilOp)Enum.Parse(typeof(StencilOp), el.Value);
                else if (el.Name == "ReferenceStencil")
                    depthStencilState.StencilEnable = bool.Parse(el.Value);
                else if (el.Name == "StencilFail")
                    depthStencilState.StencilFail = (StencilOp)Enum.Parse(typeof(StencilOp), el.Value);
                else if (el.Name == "StencilFunction")
                    depthStencilState.StencilFunction =
                        (StencilFunction)Enum.Parse(typeof(StencilFunction), el.Value);
                else if (el.Name == "DepthBufferFunction")
                    depthStencilState.StencilMask = int.Parse(el.Value);
                else if (el.Name == "StencilPass")
                    depthStencilState.StencilPass = (StencilOp)Enum.Parse(typeof(StencilOp), el.Value);
                /*else if (el.Name == "DepthBufferFunction")
                    depthStencilState.StencilWriteMask = int.Parse(el.InnerText);*/
                else if (el.Name == "TwoSidedStencilMode")
                    depthStencilState.TwoSidedStencilMode = bool.Parse(el.Value);
                else
                    ThrowXmlError("'" + el.Name + "' is not an option of the DepthStencilState", GetElementPosition(el));
            }

            return depthStencilState;
        }

        private static RasterizerState? ParseRasterizerState(XElement element)
        {
            if (!element.HasElements || element.Name != "RasterizerState")
                return null;
            RasterizerState rasterizerState = new RasterizerState();
            foreach (var el in element.Elements())
            {
                if (el.Name == "CullMode")
                    rasterizerState.CullMode = (CullMode)Enum.Parse(typeof(CullMode), el.Value);
                else if (el.Name == "FillMode")
                    rasterizerState.FillMode = (PolygonMode)Enum.Parse(typeof(PolygonMode), el.Value);
                else if (el.Name == "MultiSampleAntiAlias")
                    rasterizerState.MultiSampleAntiAlias = bool.Parse(el.Value);
                else if (el.Name == "ScissorTestEnable")
                    rasterizerState.ScissorTestEnable = bool.Parse(el.Value);
                else if (el.Name == "SlopeScaleDepthBias")
                    rasterizerState.SlopeScaleDepthBias = float.Parse(el.Value);
                else if (el.Name == "DepthBias")
                    rasterizerState.DepthBias = float.Parse(el.Value);
                /*else if (el.Name == "DepthClipEnable")
                    rasterizerState.DepthClipEnable = bool.Parse(el.Value);*/
                else
                    ThrowXmlError("'" + el.Name + "' is not an option of the RasterizerState", GetElementPosition(el));
            }

            return rasterizerState;
        }

        /// <inheritdoc />
        public override EffectContent? Import(string filename, ContentImporterContext context)
        {
            try
            {
                EffectContent content = new EffectContent();

                var doc = XDocument.Load(filename, LoadOptions.SetLineInfo);
                XNode? current = doc.FirstNode;
                while (current != null && current.NodeType != XmlNodeType.Element)
                {
                    current = current.NextNode;
                }

                if (current == null)
                    ThrowXmlError("no xml element found", GetElementPosition(doc.FirstNode));
                var effectElement = (XElement)current;
                foreach (var element in effectElement.Elements())
                {
                    if (element.Name == "Technique")
                    {
                        content.Techniques.Add(ParseTechnique(filename, context, element));
                    }
                    else if (element.Name == "Settings")
                    {
                        content.Settings ??= new EffectSettings();
                        ParseSettings(content.Settings, context, element);
                    }
                    else
                    {
                        ThrowXmlError("'" + element.Name + "' element not recognized: Expected 'Technique' or 'Settings'.", GetElementPosition(element));
                    }
                }

                return content;
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename, $"{filename}{ex.Message}", BuildMessageEventArgs.BuildMessageType.Error);
            }

            return null;
        }

        private static EffectSettings.SettingType ParseSettingType(string? typeName)
        {
            if (!string.IsNullOrWhiteSpace(typeName)
                && Enum.TryParse<EffectSettings.SettingType>(typeName, true, out var type))
                return type;

            return EffectSettings.SettingType.None;
        }

        private static EffectSettings.Setting ParseSetting(XElement element, EffectSettings.SettingKind kind)
        {
            var nameAttr = element.Attribute("name");
            var name = nameAttr?.Value;
            if (nameAttr is null)
                ThrowXmlError("'name' attribute required.", GetElementPosition(element));
            else if (string.IsNullOrWhiteSpace(name))
                ThrowXmlError("Invalid name.", GetElementPosition(nameAttr));
            return new EffectSettings.Setting(name, ParseSettingType(element.Attribute("type")?.Value), kind, element.Value);
        }

        private static void ParseSettings(EffectSettings settings, ContentImporterContext context, XElement element)
        {
            foreach (var el in element.Elements())
            {
                switch (el.Name.LocalName)
                {
                    case "Define":
                        settings.Add(ParseSetting(el, EffectSettings.SettingKind.Define));
                        break;
                    case "Const":
                        var s = ParseSetting(el, EffectSettings.SettingKind.Const);
                        if (s.Type == EffectSettings.SettingType.None)
                            ThrowXmlError("Const setting requires a valid type.", GetElementPosition(el));
                        settings.Add(s);
                        break;
                    default:
                        ThrowXmlError("'" + el.Name + "' element not recognized: Expected 'Define'.", GetElementPosition(el));
                        break;
                }
            }
        }
        private static EffectTechnique ParseTechnique(string filename, ContentImporterContext context, XElement technique)
        {
            EffectTechnique info = new EffectTechnique();
            var nameAttr = technique.Attribute("name");

            if (nameAttr is null || string.IsNullOrWhiteSpace(nameAttr.Value))
                ThrowXmlError("Valid name required.", nameAttr);

            info.Name = nameAttr.Value;
            foreach (var element in technique.Elements())
            {
                if (element.Name == "Pass")
                {
                    info.Passes.Add(ParsePass(filename, context, element));
                }
                else if (element.Name == "Settings")
                {
                    info.Settings ??= new EffectSettings();
                    ParseSettings(info.Settings, context, element);
                }
                else
                {
                    ThrowXmlError("'" + element.Name + "' element not recognized: Expected 'Pass' or 'Settings'.", element);
                }
            }

            return info;
        }

        private static EffectPass ParsePass(string filename, ContentImporterContext context, XElement pass)
        {
            var nameAttr = pass.Attribute("name");

            if (nameAttr is null || string.IsNullOrWhiteSpace(nameAttr.Value))
                ThrowXmlError("Valid name required.", nameAttr);

            EffectPass pi = new EffectPass(nameAttr.Value);
            foreach (var sh in pass.Elements())
            {
                if (sh.Name == "Shader")
                {
                    ShaderType type = ParseShaderType(sh.Attribute("type")?.Value);
                    if ((int)type == -1)
                        ThrowXmlError("Unsupported Shader type detected", sh);
                    var fnAttr = sh.Attribute("filename")?.Value;
                    if (fnAttr is null)
                        ThrowXmlError("'filename' attribute is required.", sh);
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
                    foreach (var attr in sh.Elements())
                    {
                        if (attr.Name == "attribute")
                        {
                            VertexElementUsage usage =
                                (VertexElementUsage)Enum.Parse(typeof(VertexElementUsage), attr.Value);
                            var nmAttr = attr.Attribute("name");
                            if (nmAttr is null || string.IsNullOrWhiteSpace(nmAttr.Value))
                                ThrowXmlError("Not a valid attribute name'" + nmAttr?.Value + "'", GetElementPosition((XObject?)nmAttr ?? attr));
                            pi.Attributes.Add(usage, nmAttr.Value);
                        }
                    }
                }
                else if (sh.Name == "Materials")
                {
                    foreach (var matEl in sh.Elements())
                    {
                        var matNameAttr = matEl.Attribute("name");
                        if (matNameAttr is null || string.IsNullOrWhiteSpace(matNameAttr.Value))
                            ThrowXmlError("Valid name required.", GetElementPosition((XObject?)matNameAttr ?? matEl));
                        var mat = new EffectMaterial(matNameAttr.Value);
                        pi.Materials.Add(mat);
                        foreach (var attr in matEl.Elements())
                        {
                            if (attr.Name == "binding")
                            {
                                var nmAttr = attr.Attribute("name");

                                if (nmAttr is null || string.IsNullOrWhiteSpace(nmAttr.Value))
                                    ThrowXmlError("Valid name required.", GetElementPosition((XObject?)nmAttr ?? attr));
                                mat.Bindings.Add(nmAttr.Value, (attr.Value, null));
                            }
                        }
                    }
                }
                else if (sh.Name == "Settings")
                {
                    pi.Settings ??= new EffectSettings();
                    ParseSettings(pi.Settings, context, sh);
                }
                else
                {
                    ThrowXmlError("'" + sh.Name + "' element not recognized", GetElementPosition(sh));
                }
            }

            return pi;
        }

        #endregion
    }

    /// <summary>
    /// Collection of setings for shaders.
    /// </summary>
    public class EffectSettings
    {
        /// <summary>
        /// Enumeration of possible setting kinds.
        /// </summary>
        public enum SettingKind
        {
            /// <summary>
            /// The setting is a preprocessor define.
            /// </summary>
            Define,

            /// <summary>
            /// The setting is a const variable.
            /// </summary>
            Const
        }

        /// <summary>
        /// Enumeration of possible types for settings.
        /// </summary>
        public enum SettingType
        {
            /// <summary>
            ///     No type.
            /// </summary>
            None,
            /// <summary>
            ///     Boolean type.
            /// </summary>
            Bool,
            /// <summary>
            ///     Integer type.
            /// </summary>
            Int,
            /// <summary>
            ///     Unsigned integer type.
            /// </summary>
            UInt,
            /// <summary>
            ///     Single precision floating point type.
            /// </summary>
            Float,
            /// <summary>
            ///     Double precision floating point type.
            /// </summary>
            Double,
            /// <summary>
            ///     2 component bool vector type.
            /// </summary>
            BVec2 = 0x100,
            /// <summary>
            ///     3 component bool vector type.
            /// </summary>
            BVec3,
            /// <summary>
            ///     4 component bool vector type.
            /// </summary>
            BVec4,
            /// <summary>
            ///     2 component int vector type.
            /// </summary>
            IVec2,
            /// <summary>
            ///     3 component int vector type.
            /// </summary>
            IVec3,
            /// <summary>
            ///     4 component int vector type.
            /// </summary>
            IVec4,
            /// <summary>
            ///     2 component unsigned int vector type.
            /// </summary>
            UVec2,
            /// <summary>
            ///     3 component unsigned int vector type.
            /// </summary>
            UVec3,
            /// <summary>
            ///     4 component unsigned int vector type.
            /// </summary>
            UVec4,
            /// <summary>
            ///     2 component floating point vector type.
            /// </summary>
            Vec2,
            /// <summary>
            ///     3 component floating point vector type.
            /// </summary>
            Vec3,
            /// <summary>
            ///     4 component floating point vector type.
            /// </summary>
            Vec4,
            /// <summary>
            ///     4x4 Matrix type.
            /// </summary>
            DVec2,
            /// <summary>
            ///     4x4 Matrix type.
            /// </summary>
            DVec3,
            /// <summary>
            ///     4x4 Matrix type.
            /// </summary>
            DVec4,
            /// <summary>
            ///     2x2 Matrix type.
            /// </summary>
            Mat2x2,
            /// <summary>
            ///     3x2 Matrix type.
            /// </summary>
            Mat3x2,
            /// <summary>
            ///     4x2 Matrix type.
            /// </summary>
            Mat4x2,
            /// <summary>
            ///     2x3 Matrix type.
            /// </summary>
            Mat2x3,
            /// <summary>
            ///     3x3 Matrix type.
            /// </summary>
            Mat3x3,
            /// <summary>
            ///     4x3 Matrix type.
            /// </summary>
            Mat4x3,
            /// <summary>
            ///     2x4 Matrix type.
            /// </summary>
            Mat2x4,
            /// <summary>
            ///     3x4 Matrix type.
            /// </summary>
            Mat3x4,
            /// <summary>
            ///     4x4 Matrix type.
            /// </summary>
            Mat4x4,
            /// <summary>
            ///     Shorthand for <see cref="Mat2x2"/>.
            /// </summary>
            Mat2 = Mat2x2,
            /// <summary>
            ///     Shorthand for <see cref="Mat3x3"/>.
            /// </summary>
            Mat3 = Mat3x3,
            /// <summary>
            ///     Shorthand for <see cref="Mat4x4"/>.
            /// </summary>
            Mat4 = Mat4x4
        }
        /// <summary>
        /// Setting variable for shaders.
        /// </summary>
        public readonly struct Setting
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Setting"/> struct.
            /// </summary>
            /// <param name="name">The name of the setting variable.</param>
            /// <param name="type">The variable type.</param>
            /// <param name="kind">The variable kind.</param>
            /// <param name="defaultValue">The value to default to.</param>
            public Setting(string name, SettingType type, SettingKind kind, string defaultValue)
            {
                Name = name;
                Type = type;
                Kind = kind;
                DefaultValue = defaultValue;
            }

            /// <summary>
            /// Gets the name of the setting variable.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets the type of the setting variable.
            /// </summary>
            public SettingType Type { get; }

            /// <summary>
            /// Gets the default of the setting variable.
            /// </summary>
            public string? DefaultValue { get; }

            /// <summary>
            /// Gets the kind of the setting variable.
            /// </summary>
            public SettingKind Kind { get; }

            /// <summary>
            /// Converts the <see cref="Type"/> to a type string.
            /// </summary>
            /// <returns>The type string.</returns>
            /// <exception cref="InvalidOperationException">
            /// Thrown when <see cref="Type"/> is <see cref="SettingType.None"/>.
            /// </exception>
            public string ToTypeString()
            {
                if (Type == SettingType.None)
                    throw new InvalidOperationException();
                return Type.ToString().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Add a setting to this collection of settings.
        /// </summary>
        /// <param name="setting">The setting to add.</param>
        public void Add(Setting setting)
        {
            Settings.Add(setting.Name, setting);
        }

        /// <summary>
        /// Merge this <see cref="EffectSettings"/> with another <see cref="EffectSettings"/>.
        /// </summary>
        /// <param name="settings">The other settings to merge with.</param>
        public void MergeWith(EffectSettings settings)
        {
            foreach (var s in settings.Settings)
                Settings.TryAdd(s.Key, s.Value);
        }

        /// <summary>
        /// Gets a map of setting names to the given setting.
        /// </summary>
        public Dictionary<string, Setting> Settings { get; } = new();
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

        /// <summary>
        ///     Gets the settings for this effect.
        /// </summary>
        public EffectSettings? Settings { get; internal set; }
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

        /// <summary>
        ///     Gets the settings for this technique.
        /// </summary>
        public EffectSettings? Settings { get; internal set; }
    }

    /// <summary>
    ///     Represents a material of a effect.
    /// </summary>
    public class EffectMaterial
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EffectMaterial"/> class.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        public EffectMaterial(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Gets the name of the material.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets a list of mappings from material property names to uniform name and parameterinfo.
        /// </summary>
        public Dictionary<string, (string, ParameterInfo?)> Bindings { get; } = new();
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
            Materials = new List<EffectMaterial>();
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
        
        /// <summary>
        ///     Gets a list of all materials in this pass.
        /// </summary>
        public List<EffectMaterial> Materials { get; }

        /// <summary>
        ///     Gets the settings for this pass.
        /// </summary>
        public EffectSettings? Settings { get; internal set; }
    }

    /// <summary>
    ///     A class containing effect parameter information for loading and processing.
    /// </summary>
    public class ParameterInfo : IEquatable<ParameterInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterInfo"/> class.
        /// </summary>
        /// <param name="fullName">The full name of the parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        public ParameterInfo(string fullName, string name, (Type type, bool nullable) type)
            : this(fullName, name, type.ToTypeReference())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterInfo"/> class.
        /// </summary>
        /// <param name="fullName">The full name of the parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        public ParameterInfo(string fullName, string name, TypeReference type)
        {
            FullName = fullName;
            Name = name;
            Type = type;
        }

        /// <summary>
        ///     Gets the full name of the parameter.
        /// </summary>
        public string FullName { get; }

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
        public StructParameterInfo(string attributeName, string name, (Type type, bool nullable) type, int offset)
            : base(attributeName, name, type)
        {
            Offset = offset;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StructParameterInfo"/> class.
        /// </summary>
        /// <param name="attributeName">The name of the struct attribute in glsl.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="offset">The offset inside the struct.</param>
        public StructParameterInfo(string attributeName,string name, TypeReference type, int offset)
            : base(attributeName, name, type)
        {
            Offset = offset;
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
        public string AttributeName => FullName;
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
        public ArrayParameterInfo(string attributeName, string name, (Type type, bool nullable) type, int offset)
            : base(attributeName, name, type)
        {
            Offset = offset;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArrayParameterInfo"/> class.
        /// </summary>
        /// <param name="attributeName">The name of the array attribute in glsl.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="offset">The offset inside the array.</param>
        public ArrayParameterInfo(string attributeName, string name, TypeReference type, int offset)
            : base(attributeName, name, type)
        {
            Offset = offset;
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
        public string AttributeName => FullName;
    }
}