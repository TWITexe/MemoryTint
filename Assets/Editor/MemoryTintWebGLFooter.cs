using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;

// Keep the exported game free of the default Unity footer on every build.
public static class MemoryTintWebGLFooter
{
    [PostProcessBuild(1000)]
    public static void RemoveFooter(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.WebGL) return;
        string indexPath = Path.Combine(buildPath, "index.html");
        if (!File.Exists(indexPath)) return;
        string html = File.ReadAllText(indexPath);
        html = Regex.Replace(html,
            "<div id=\"unity-footer\">\\s*<div id=\"unity-logo-title-footer\"></div>\\s*<div id=\"unity-fullscreen-button\"></div>\\s*<div id=\"unity-build-title\">[^<]*</div>\\s*</div>", "");
        html = Regex.Replace(html,
            "document\\.querySelector\\(\"#unity-fullscreen-button\"\\)\\.onclick\\s*=\\s*\\(\\)\\s*=>\\s*\\{[^}]*\\};", "");
        const string marker = "<!-- MemoryTint: footer-free layout -->";
        if (!html.Contains(marker))
            html = html.Replace("</head>", marker + "\n<style>html,body{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:#000}#unity-container.unity-desktop,#unity-container.unity-mobile{position:fixed!important;left:50%!important;top:50%!important;transform:translate(-50%,-50%)!important;width:min(100vw,177.7777778vh)!important;height:min(100vh,56.25vw)!important;width:min(100vw,177.7777778dvh)!important;height:min(100dvh,56.25vw)!important}#unity-canvas{display:block;width:100%!important;height:100%!important}#unity-footer{display:none!important;height:0!important}</style>\n</head>");
        File.WriteAllText(indexPath, html, new UTF8Encoding(false));
        string cssPath = Path.Combine(buildPath, "TemplateData", "style.css");
        if (File.Exists(cssPath))
        {
            string css = File.ReadAllText(cssPath);
            css = Regex.Replace(css, @"(?m)^.*#unity-(footer|logo-title-footer|build-title|fullscreen-button)[^\r\n]*(\r?\n|$)", "");
            File.WriteAllText(cssPath, css, new UTF8Encoding(false));
        }
    }
}
