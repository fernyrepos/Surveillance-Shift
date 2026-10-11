using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class GuardTextures
    {
        public static readonly Texture2D Guards = ContentFinder<Texture2D>.Get("UI/Guard/Guards");
        public static readonly Texture2D Post = ContentFinder<Texture2D>.Get("UI/Guard/Post");
        public static readonly Texture2D Route = ContentFinder<Texture2D>.Get("UI/Guard/Route");
        public static readonly Texture2D Bodyguard = ContentFinder<Texture2D>.Get("UI/Guard/Bodyguard");
        public static readonly Texture2D AnimalWatch = ContentFinder<Texture2D>.Get("UI/Guard/AnimalWatch");
    }
}
