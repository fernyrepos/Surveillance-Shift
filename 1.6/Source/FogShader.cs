using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public static class FogShader
    {
        private const string DefName = "SS_FogGrade";

        private static bool resolved;
        private static Shader shader;

        public static bool Available
        {
            get
            {
                Resolve();
                return shader != null;
            }
        }

        public static Shader Shader
        {
            get
            {
                Resolve();
                return shader;
            }
        }

        public static Material CreateMaterial(string name, Texture texture, int renderQueue)
        {
            return new Material(Shader)
            {
                name = name,
                mainTexture = texture,
                color = Color.white,
                renderQueue = renderQueue
            };
        }

        public static void Apply(Material material, float blurRadius)
        {
            material.SetFloat("_BlurRadius", blurRadius);
            material.SetVector("_Grade", new Vector4(FogStyle.Contrast, FogStyle.Saturation, FogStyle.Brightness, 0f));
        }

        public static float CurrentBlurRadius()
        {
            var driver = Find.CameraDriver;
            if (driver is null) return 0f;
            return FogStyle.Blur / (2f * Mathf.Max(1f, driver.ZoomRootSize));
        }

        private static void Resolve()
        {
            if (resolved) return;
            resolved = true;

            try
            {
                var defType = typeof(Def).Assembly.GetType("Verse.ShaderDef");
                if (defType is null) return;

                var databaseType = typeof(DefDatabase<>).MakeGenericType(defType);
                var lookup = databaseType.GetMethod("GetNamedSilentFail", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
                    ?? databaseType.GetMethod("GetNamedSilentFail", BindingFlags.Public | BindingFlags.Static);
                var lookupArgs = new object[lookup?.GetParameters().Length ?? 0];
                if (lookupArgs.Length > 0) lookupArgs[0] = DefName;
                for (var i = 1; i < lookupArgs.Length; i++) lookupArgs[i] = lookup.GetParameters()[i].DefaultValue;
                var def = lookup?.Invoke(null, lookupArgs);
                if (def is null)
                {
                    Log.Warning("[Surveillance Shift] Fog shader def was not loaded; non-hardcore fog will use the hardcore look.");
                    return;
                }

                if (IsRegistered() is false)
                {
                    Log.Warning("[Surveillance Shift] Fog shader is not registered with the engine; non-hardcore fog will use the hardcore look.");
                    return;
                }

                shader = defType.GetProperty("Shader", BindingFlags.Public | BindingFlags.Instance)?.GetValue(def) as Shader;
            }
            catch (Exception e)
            {
                shader = null;
                Log.Warning("[Surveillance Shift] Could not set up the fog shader: " + e.Message);
            }
        }

        private static bool IsRegistered()
        {
            var engine = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Waylith");
            var registry = engine?.GetType("Waylith.Rendering.ShaderRegistry");
            var tryResolve = registry?.GetMethod("TryResolve", BindingFlags.Public | BindingFlags.Static);
            if (tryResolve is null) return false;

            var args = new object[] { DefName, null };
            if ((bool)tryResolve.Invoke(null, args) is false) return false;

            var hasErrors = args[1]?.GetType().GetProperty("HasErrors")?.GetValue(args[1]);
            return hasErrors is not true;
        }
    }
}
