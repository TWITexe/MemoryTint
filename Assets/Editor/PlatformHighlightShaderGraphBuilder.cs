using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PlatformHighlightShaderGraphBuilder
{
    private const string GraphPath = "Assets/Shaders/SG_PlatformHighlight.shadergraph";
    private const string MaterialPath = "Assets/Materials/M_PlatformHighlight.mat";

    private static readonly BindingFlags AllBindings =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [MenuItem("Tools/MemoryTint/Build Platform Highlight Shader Graph")]
    public static void BuildPlatformHighlightShaderGraph()
    {
        EnsureFolder("Assets/Shaders");
        EnsureFolder("Assets/Materials");

        object graph = LoadOrCreateGraph();
        SetGraphToTransparentSurface(graph);
        ClearCustomNodes(graph);
        ClearGraphInputs(graph);

        object mainTexProp = AddTextureProperty(graph, "MainTex", true);
        object baseColorProp = AddColorProperty(graph, "BaseColor", Color.white);
        object glowColorProp = AddColorProperty(graph, "GlowColor", Color.white);
        object outlineWidthProp = AddSliderProperty(graph, "OutlineWidthPx", 1.2f, 0f, 4f);
        object glowStrengthProp = AddSliderProperty(graph, "GlowStrength", 0.45f, 0f, 2f);
        object glowAlphaProp = AddSliderProperty(graph, "GlowAlpha", 0.5f, 0f, 1f);
        object timeFxAmountProp = AddSliderProperty(graph, "TimeFxAmount", 0.08f, 0f, 0.5f);
        object timeFxSpeedProp = AddSliderProperty(graph, "TimeFxSpeed", 1f, 0f, 5f);

        object pMainTex = CreatePropertyNode(graph, mainTexProp, new Vector2(-1700f, -300f));
        object pBaseColor = CreatePropertyNode(graph, baseColorProp, new Vector2(-1700f, -40f));
        object pGlowColor = CreatePropertyNode(graph, glowColorProp, new Vector2(-1700f, 200f));
        object pOutlineWidth = CreatePropertyNode(graph, outlineWidthProp, new Vector2(-1700f, 520f));
        object pGlowStrength = CreatePropertyNode(graph, glowStrengthProp, new Vector2(-1700f, 740f));
        object pGlowAlpha = CreatePropertyNode(graph, glowAlphaProp, new Vector2(-1700f, 940f));
        object pTimeFxAmount = CreatePropertyNode(graph, timeFxAmountProp, new Vector2(-1700f, 1140f));
        object pTimeFxSpeed = CreatePropertyNode(graph, timeFxSpeedProp, new Vector2(-1700f, 1340f));

        object nUv = CreateNode(graph, "UnityEditor.ShaderGraph.UVNode", new Vector2(-1450f, -360f));
        object nVertexColor = CreateNode(graph, "UnityEditor.ShaderGraph.VertexColorNode", new Vector2(-1450f, 20f));
        object nTextureSize = CreateNode(graph, "UnityEditor.ShaderGraph.Texture2DPropertiesNode", new Vector2(-1450f, 480f));

        object nCombineTexel = CreateNode(graph, "UnityEditor.ShaderGraph.CombineNode", new Vector2(-1200f, 480f));
        object nMulOutline = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(-960f, 500f));
        object nSplitOffset = CreateNode(graph, "UnityEditor.ShaderGraph.SplitNode", new Vector2(-720f, 500f));
        object nZero = CreateNode(graph, "UnityEditor.ShaderGraph.Vector1Node", new Vector2(-960f, 900f));

        object nCombineOffsetX = CreateNode(graph, "UnityEditor.ShaderGraph.CombineNode", new Vector2(-480f, 380f));
        object nCombineOffsetY = CreateNode(graph, "UnityEditor.ShaderGraph.CombineNode", new Vector2(-480f, 620f));
        object nUvPlusX = CreateNode(graph, "UnityEditor.ShaderGraph.AddNode", new Vector2(-240f, 300f));
        object nUvMinusX = CreateNode(graph, "UnityEditor.ShaderGraph.SubtractNode", new Vector2(-240f, 420f));
        object nUvPlusY = CreateNode(graph, "UnityEditor.ShaderGraph.AddNode", new Vector2(-240f, 540f));
        object nUvMinusY = CreateNode(graph, "UnityEditor.ShaderGraph.SubtractNode", new Vector2(-240f, 660f));

        object nSampleBase = CreateNode(graph, "UnityEditor.ShaderGraph.SampleTexture2DNode", new Vector2(20f, -220f));
        object nSamplePx = CreateNode(graph, "UnityEditor.ShaderGraph.SampleTexture2DNode", new Vector2(20f, 220f));
        object nSampleMx = CreateNode(graph, "UnityEditor.ShaderGraph.SampleTexture2DNode", new Vector2(20f, 360f));
        object nSamplePy = CreateNode(graph, "UnityEditor.ShaderGraph.SampleTexture2DNode", new Vector2(20f, 500f));
        object nSampleMy = CreateNode(graph, "UnityEditor.ShaderGraph.SampleTexture2DNode", new Vector2(20f, 640f));

        object nMulTint = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(260f, -40f));
        object nMulBase = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(520f, -150f));
        object nSplitBase = CreateNode(graph, "UnityEditor.ShaderGraph.SplitNode", new Vector2(760f, -220f));

        object nMaxX = CreateNode(graph, "UnityEditor.ShaderGraph.MaximumNode", new Vector2(520f, 280f));
        object nMaxY = CreateNode(graph, "UnityEditor.ShaderGraph.MaximumNode", new Vector2(520f, 560f));
        object nMaxAll = CreateNode(graph, "UnityEditor.ShaderGraph.MaximumNode", new Vector2(760f, 420f));
        object nSubMask = CreateNode(graph, "UnityEditor.ShaderGraph.SubtractNode", new Vector2(980f, 420f));
        object nSaturateMask = CreateNode(graph, "UnityEditor.ShaderGraph.SaturateNode", new Vector2(1180f, 420f));

        object nMulGlowMask = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(980f, 700f));
        object nTime = CreateNode(graph, "UnityEditor.ShaderGraph.TimeNode", new Vector2(980f, 980f));
        object nMulTimeSpeed = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(1180f, 980f));
        object nSine = CreateNode(graph, "UnityEditor.ShaderGraph.SineNode", new Vector2(1380f, 980f));
        object nMulTimeAmount = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(1580f, 980f));
        object nOne = CreateNode(graph, "UnityEditor.ShaderGraph.Vector1Node", new Vector2(1580f, 1180f));
        object nAddTimeBias = CreateNode(graph, "UnityEditor.ShaderGraph.AddNode", new Vector2(1780f, 980f));
        object nMulTimedGlowStrength = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(1980f, 780f));
        object nMulGlowStrength = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(2180f, 700f));
        object nAddFinalRgba = CreateNode(graph, "UnityEditor.ShaderGraph.AddNode", new Vector2(980f, -40f));
        object nSplitFinal = CreateNode(graph, "UnityEditor.ShaderGraph.SplitNode", new Vector2(1200f, -40f));
        object nCombineFinalRgb = CreateNode(graph, "UnityEditor.ShaderGraph.CombineNode", new Vector2(1420f, -40f));

        object nMulGlowAlpha = CreateNode(graph, "UnityEditor.ShaderGraph.MultiplyNode", new Vector2(1420f, 420f));
        object nMaxAlpha = CreateNode(graph, "UnityEditor.ShaderGraph.MaximumNode", new Vector2(1640f, 420f));

        SetVector1NodeValue(nOne, 1f);

        Connect(graph, pMainTex, 0, nSampleBase, 1);
        Connect(graph, pMainTex, 0, nSamplePx, 1);
        Connect(graph, pMainTex, 0, nSampleMx, 1);
        Connect(graph, pMainTex, 0, nSamplePy, 1);
        Connect(graph, pMainTex, 0, nSampleMy, 1);
        Connect(graph, pMainTex, 0, nTextureSize, 1);

        Connect(graph, nUv, 0, nSampleBase, 2);

        Connect(graph, nTextureSize, 3, nCombineTexel, 0);
        Connect(graph, nTextureSize, 4, nCombineTexel, 1);
        Connect(graph, nCombineTexel, 6, nMulOutline, 0);
        Connect(graph, pOutlineWidth, 0, nMulOutline, 1);
        Connect(graph, nMulOutline, 2, nSplitOffset, 0);

        Connect(graph, nSplitOffset, 1, nCombineOffsetX, 0);
        Connect(graph, nZero, 0, nCombineOffsetX, 1);
        Connect(graph, nZero, 0, nCombineOffsetX, 2);
        Connect(graph, nZero, 0, nCombineOffsetX, 3);

        Connect(graph, nZero, 0, nCombineOffsetY, 0);
        Connect(graph, nSplitOffset, 2, nCombineOffsetY, 1);
        Connect(graph, nZero, 0, nCombineOffsetY, 2);
        Connect(graph, nZero, 0, nCombineOffsetY, 3);

        Connect(graph, nUv, 0, nUvPlusX, 0);
        Connect(graph, nCombineOffsetX, 4, nUvPlusX, 1);
        Connect(graph, nUv, 0, nUvMinusX, 0);
        Connect(graph, nCombineOffsetX, 4, nUvMinusX, 1);
        Connect(graph, nUv, 0, nUvPlusY, 0);
        Connect(graph, nCombineOffsetY, 4, nUvPlusY, 1);
        Connect(graph, nUv, 0, nUvMinusY, 0);
        Connect(graph, nCombineOffsetY, 4, nUvMinusY, 1);

        Connect(graph, nUvPlusX, 2, nSamplePx, 2);
        Connect(graph, nUvMinusX, 2, nSampleMx, 2);
        Connect(graph, nUvPlusY, 2, nSamplePy, 2);
        Connect(graph, nUvMinusY, 2, nSampleMy, 2);

        Connect(graph, pBaseColor, 0, nMulTint, 0);
        Connect(graph, nVertexColor, 0, nMulTint, 1);
        Connect(graph, nSampleBase, 0, nMulBase, 0);
        Connect(graph, nMulTint, 2, nMulBase, 1);
        Connect(graph, nMulBase, 2, nSplitBase, 0);

        Connect(graph, nSamplePx, 7, nMaxX, 0);
        Connect(graph, nSampleMx, 7, nMaxX, 1);
        Connect(graph, nSamplePy, 7, nMaxY, 0);
        Connect(graph, nSampleMy, 7, nMaxY, 1);
        Connect(graph, nMaxX, 2, nMaxAll, 0);
        Connect(graph, nMaxY, 2, nMaxAll, 1);

        Connect(graph, nMaxAll, 2, nSubMask, 0);
        Connect(graph, nSplitBase, 4, nSubMask, 1);
        Connect(graph, nSubMask, 2, nSaturateMask, 0);

        Connect(graph, pGlowColor, 0, nMulGlowMask, 0);
        Connect(graph, nSaturateMask, 1, nMulGlowMask, 1);
        Connect(graph, nTime, 0, nMulTimeSpeed, 0);
        Connect(graph, pTimeFxSpeed, 0, nMulTimeSpeed, 1);
        Connect(graph, nMulTimeSpeed, 2, nSine, 0);
        Connect(graph, nSine, 1, nMulTimeAmount, 0);
        Connect(graph, pTimeFxAmount, 0, nMulTimeAmount, 1);
        Connect(graph, nOne, 0, nAddTimeBias, 0);
        Connect(graph, nMulTimeAmount, 2, nAddTimeBias, 1);
        Connect(graph, pGlowStrength, 0, nMulTimedGlowStrength, 0);
        Connect(graph, nAddTimeBias, 2, nMulTimedGlowStrength, 1);
        Connect(graph, nMulGlowMask, 2, nMulGlowStrength, 0);
        Connect(graph, nMulTimedGlowStrength, 2, nMulGlowStrength, 1);

        Connect(graph, nMulBase, 2, nAddFinalRgba, 0);
        Connect(graph, nMulGlowStrength, 2, nAddFinalRgba, 1);
        Connect(graph, nAddFinalRgba, 2, nSplitFinal, 0);

        Connect(graph, nSplitFinal, 1, nCombineFinalRgb, 0);
        Connect(graph, nSplitFinal, 2, nCombineFinalRgb, 1);
        Connect(graph, nSplitFinal, 3, nCombineFinalRgb, 2);

        Connect(graph, nSaturateMask, 1, nMulGlowAlpha, 0);
        Connect(graph, pGlowAlpha, 0, nMulGlowAlpha, 1);
        Connect(graph, nSplitBase, 4, nMaxAlpha, 0);
        Connect(graph, nMulGlowAlpha, 2, nMaxAlpha, 1);

        object baseColorBlock = FindBlockNodeByName(graph, "SurfaceDescription.BaseColor");
        object alphaBlock = FindBlockNodeByName(graph, "SurfaceDescription.Alpha");

        Connect(graph, nCombineFinalRgb, 5, baseColorBlock, 0);
        Connect(graph, nMaxAlpha, 2, alphaBlock, 0);

        SaveGraph(graph);
        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        CreateOrUpdateMaterial();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SG_PlatformHighlight.shadergraph and material were rebuilt.");
    }

    public static void BuildFromBatch()
    {
        BuildPlatformHighlightShaderGraph();
    }

    private static object LoadOrCreateGraph()
    {
        Type graphType = FindType("UnityEditor.ShaderGraph.GraphData");
        object graph = Activator.CreateInstance(graphType, true);

        if (File.Exists(GraphPath))
        {
            string text = File.ReadAllText(GraphPath);
            if (!string.IsNullOrWhiteSpace(text))
            {
                Type multiJsonType = FindType("UnityEditor.ShaderGraph.Serialization.MultiJson");
                MethodInfo deserialize = multiJsonType
                    .GetMethods(AllBindings)
                    .First(m => m.Name == "Deserialize" && m.IsGenericMethodDefinition);
                deserialize.MakeGenericMethod(graphType).Invoke(null, new object[] { graph, text, null, false });
                return graph;
            }
        }

        Invoke(graph, "AddContexts");

        Type targetType = FindType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
        object target = Activator.CreateInstance(targetType, true);
        Type subTargetType = FindType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalSpriteUnlitSubTarget");
        Invoke(target, "TrySetActiveSubTarget", subTargetType);

        Type blockFieldType = FindType("UnityEditor.ShaderGraph.Internal.BlockFieldDescriptor");
        Array blocks = Array.CreateInstance(blockFieldType, 5);
        blocks.SetValue(GetStaticMember("UnityEditor.ShaderGraph.BlockFields+VertexDescription", "Position"), 0);
        blocks.SetValue(GetStaticMember("UnityEditor.ShaderGraph.BlockFields+VertexDescription", "Normal"), 1);
        blocks.SetValue(GetStaticMember("UnityEditor.ShaderGraph.BlockFields+VertexDescription", "Tangent"), 2);
        blocks.SetValue(GetStaticMember("UnityEditor.ShaderGraph.BlockFields+SurfaceDescription", "BaseColor"), 3);
        blocks.SetValue(GetStaticMember("UnityEditor.ShaderGraph.BlockFields+SurfaceDescription", "Alpha"), 4);

        Type targetBaseType = FindType("UnityEditor.ShaderGraph.Target");
        Array targets = Array.CreateInstance(targetBaseType, 1);
        targets.SetValue(target, 0);

        Invoke(graph, "InitializeOutputs", targets, blocks);

        Type categoryType = FindType("UnityEditor.ShaderGraph.CategoryData");
        MethodInfo defaultCategoryMethod = categoryType.GetMethod("DefaultCategory", AllBindings);
        object category = defaultCategoryMethod.Invoke(null, null);
        Invoke(graph, "AddCategory", category);

        SetMember(graph, "path", "Shader Graphs");
        return graph;
    }

    private static void SetGraphToTransparentSurface(object graph)
    {
        IEnumerable activeTargets = (IEnumerable)GetMember(graph, "activeTargets");
        if (activeTargets == null)
        {
            return;
        }

        foreach (object target in activeTargets)
        {
            if (target == null || target.GetType().FullName != "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget")
            {
                continue;
            }

            SetField(target, "m_SurfaceType", Enum.ToObject(GetFieldType(target, "m_SurfaceType"), 1));
            SetField(target, "m_AlphaMode", Enum.ToObject(GetFieldType(target, "m_AlphaMode"), 0));
            break;
        }
    }

    private static void ClearCustomNodes(object graph)
    {
        Type abstractNodeType = FindType("UnityEditor.ShaderGraph.AbstractMaterialNode");
        Type blockNodeType = FindType("UnityEditor.ShaderGraph.BlockNode");
        List<object> nodes = GetNodes(graph, abstractNodeType);

        foreach (object node in nodes)
        {
            if (node != null && !blockNodeType.IsInstanceOfType(node))
            {
                Invoke(graph, "RemoveNode", node);
            }
        }
    }

    private static void ClearGraphInputs(object graph)
    {
        List<object> inputs = new List<object>();
        CollectInputs(inputs, GetMember(graph, "properties") as IEnumerable);
        CollectInputs(inputs, GetMember(graph, "keywords") as IEnumerable);
        CollectInputs(inputs, GetMember(graph, "dropdowns") as IEnumerable);

        foreach (object input in inputs)
        {
            if (input != null)
            {
                Invoke(graph, "RemoveGraphInput", input);
            }
        }
    }

    private static void CollectInputs(List<object> target, IEnumerable source)
    {
        if (source == null)
        {
            return;
        }

        foreach (object item in source)
        {
            target.Add(item);
        }
    }

    private static object AddTextureProperty(object graph, string displayName, bool isMainTexture)
    {
        Type propertyType = FindType("UnityEditor.ShaderGraph.Internal.Texture2DShaderProperty");
        object property = Activator.CreateInstance(propertyType, true);
        SetMember(property, "displayName", displayName);
        Type defaultType = propertyType.GetNestedType("DefaultType", AllBindings);
        SetMember(property, "defaultType", Enum.Parse(defaultType, "White"));
        SetField(property, "isMainTexture", isMainTexture);
        TrySetMember(property, "useTilingAndOffset", false);

        AddGraphInput(graph, property);
        return property;
    }

    private static object AddColorProperty(object graph, string displayName, Color color)
    {
        Type propertyType = FindType("UnityEditor.ShaderGraph.Internal.ColorShaderProperty");
        object property = Activator.CreateInstance(propertyType, true);
        SetMember(property, "displayName", displayName);
        SetMember(property, "value", color);

        Type colorModeType = FindType("UnityEditor.ShaderGraph.Internal.ColorMode");
        SetMember(property, "colorMode", Enum.Parse(colorModeType, "Default"));

        AddGraphInput(graph, property);
        return property;
    }

    private static object AddSliderProperty(object graph, string displayName, float value, float min, float max)
    {
        Type propertyType = FindType("UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty");
        object property = Activator.CreateInstance(propertyType, true);
        SetMember(property, "displayName", displayName);
        SetMember(property, "value", value);

        Type floatType = FindType("UnityEditor.ShaderGraph.Internal.FloatType");
        SetMember(property, "floatType", Enum.Parse(floatType, "Slider"));
        SetMember(property, "rangeValues", new Vector2(min, max));

        AddGraphInput(graph, property);
        return property;
    }

    private static void AddGraphInput(object graph, object input)
    {
        MethodInfo method = graph.GetType().GetMethods(AllBindings)
            .First(m => m.Name == "AddGraphInput" && m.GetParameters().Length == 2);
        method.Invoke(graph, new[] { input, (object)(-1) });
    }

    private static object CreatePropertyNode(object graph, object property, Vector2 position)
    {
        object node = CreateNode(graph, "UnityEditor.ShaderGraph.PropertyNode", position);
        SetMember(node, "property", property);
        return node;
    }

    private static object CreateNode(object graph, string typeName, Vector2 position)
    {
        Type nodeType = FindType(typeName);
        object node = Activator.CreateInstance(nodeType, true);
        SetNodePosition(node, position);
        Invoke(graph, "AddNode", node);
        return node;
    }

    private static void SetVector1NodeValue(object node, float value)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (TrySetMember(node, "value", value))
        {
            return;
        }

        bool assigned = TrySetMember(node, "m_Value", value);
        if (assigned)
        {
            TryInvoke(node, "UpdateNodeAfterDeserialization");
        }

        MethodInfo findInputSlot = node.GetType().GetMethods(AllBindings)
            .FirstOrDefault(m => m.Name == "FindInputSlot" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
        if (findInputSlot != null)
        {
            Type vector1SlotType = FindType("UnityEditor.ShaderGraph.Vector1MaterialSlot");
            object slot = findInputSlot.MakeGenericMethod(vector1SlotType).Invoke(node, new object[] { 1 });
            if (slot != null)
            {
                assigned = TrySetMember(slot, "value", value) || TrySetMember(slot, "m_Value", value) || assigned;
            }
        }

        if (!assigned)
        {
            throw new MissingMemberException(node.GetType().FullName, "value/m_Value");
        }
    }

    private static void SetNodePosition(object node, Vector2 position)
    {
        object drawState = GetMember(node, "drawState");
        if (drawState == null)
        {
            return;
        }

        SetMember(drawState, "expanded", true);
        SetMember(drawState, "position", new Rect(position.x, position.y, 240f, 180f));
        SetMember(node, "drawState", drawState);
    }

    private static void TryInvoke(object instance, string methodName, params object[] args)
    {
        MethodInfo method = instance.GetType().GetMethods(AllBindings)
            .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);
        method?.Invoke(instance, args);
    }

    private static object FindBlockNodeByName(object graph, string blockName)
    {
        Type blockType = FindType("UnityEditor.ShaderGraph.BlockNode");
        List<object> blocks = GetNodes(graph, blockType);
        foreach (object block in blocks)
        {
            if (Equals(GetMember(block, "name"), blockName))
            {
                return block;
            }
        }

        throw new InvalidOperationException($"Block node '{blockName}' was not found.");
    }

    private static List<object> GetNodes(object graph, Type nodeType)
    {
        MethodInfo method = graph.GetType().GetMethods(AllBindings)
            .First(m => m.Name == "GetNodes" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
            .MakeGenericMethod(nodeType);

        IEnumerable enumerable = (IEnumerable)method.Invoke(graph, null);
        return enumerable.Cast<object>().ToList();
    }

    private static void Connect(object graph, object fromNode, int fromSlot, object toNode, int toSlot)
    {
        object outputRef = Invoke(fromNode, "GetSlotReference", fromSlot);
        object inputRef = Invoke(toNode, "GetSlotReference", toSlot);

        MethodInfo connectMethod = graph.GetType().GetMethods(AllBindings)
            .First(m => m.Name == "Connect" && m.GetParameters().Length == 2);
        connectMethod.Invoke(graph, new[] { outputRef, inputRef });
    }

    private static void SaveGraph(object graph)
    {
        Type multiJsonType = FindType("UnityEditor.ShaderGraph.Serialization.MultiJson");
        MethodInfo serialize = multiJsonType.GetMethod("Serialize", AllBindings);
        string text = (string)serialize.Invoke(null, new[] { graph });
        File.WriteAllText(GraphPath, text);
    }

    private static void CreateOrUpdateMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        if (shader == null)
        {
            throw new InvalidOperationException("Shader Graph import failed: shader is null.");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (material.HasProperty("_GlowColor"))
        {
            material.SetColor("_GlowColor", Color.white);
        }

        if (material.HasProperty("_OutlineWidthPx"))
        {
            material.SetFloat("_OutlineWidthPx", 1.2f);
        }

        if (material.HasProperty("_GlowStrength"))
        {
            material.SetFloat("_GlowStrength", 0.45f);
        }

        if (material.HasProperty("_GlowAlpha"))
        {
            material.SetFloat("_GlowAlpha", 0.5f);
        }

        if (material.HasProperty("_TimeFxAmount"))
        {
            material.SetFloat("_TimeFxAmount", 0.08f);
        }

        if (material.HasProperty("_TimeFxSpeed"))
        {
            material.SetFloat("_TimeFxSpeed", 1f);
        }

        EditorUtility.SetDirty(material);
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        if (parts.Length < 2 || parts[0] != "Assets")
        {
            return;
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static object Invoke(object instance, string methodName, params object[] args)
    {
        MethodInfo method = instance.GetType().GetMethods(AllBindings)
            .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);

        if (method == null)
        {
            throw new MissingMethodException(instance.GetType().FullName, methodName);
        }

        return method.Invoke(instance, args);
    }

    private static object GetStaticMember(string typeName, string memberName)
    {
        Type type = FindType(typeName);
        FieldInfo field = type.GetField(memberName, AllBindings);
        if (field != null)
        {
            return field.GetValue(null);
        }

        PropertyInfo property = type.GetProperty(memberName, AllBindings);
        if (property != null)
        {
            return property.GetValue(null);
        }

        throw new MissingMemberException(typeName, memberName);
    }

    private static object GetMember(object instance, string memberName)
    {
        Type type = instance.GetType();
        FieldInfo field = type.GetField(memberName, AllBindings);
        if (field != null)
        {
            return field.GetValue(instance);
        }

        PropertyInfo property = type.GetProperty(memberName, AllBindings);
        if (property != null)
        {
            return property.GetValue(instance);
        }

        return null;
    }

    private static void SetMember(object instance, string memberName, object value)
    {
        Type type = instance.GetType();
        FieldInfo field = type.GetField(memberName, AllBindings);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        PropertyInfo property = type.GetProperty(memberName, AllBindings);
        if (property != null)
        {
            property.SetValue(instance, value);
            return;
        }

        throw new MissingMemberException(type.FullName, memberName);
    }

    private static bool TrySetMember(object instance, string memberName, object value)
    {
        Type type = instance.GetType();
        FieldInfo field = type.GetField(memberName, AllBindings);
        if (field != null)
        {
            field.SetValue(instance, value);
            return true;
        }

        PropertyInfo property = type.GetProperty(memberName, AllBindings);
        if (property != null && property.CanWrite)
        {
            property.SetValue(instance, value);
            return true;
        }

        return false;
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, AllBindings);
        if (field == null)
        {
            throw new MissingFieldException(instance.GetType().FullName, fieldName);
        }

        field.SetValue(instance, value);
    }

    private static Type GetFieldType(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, AllBindings);
        if (field == null)
        {
            throw new MissingFieldException(instance.GetType().FullName, fieldName);
        }

        return field.FieldType;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }
        }

        throw new TypeLoadException($"Type '{fullName}' not found.");
    }
}
