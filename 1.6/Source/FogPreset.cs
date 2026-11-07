using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class FogPreset : IExposable
    {
        public const string Extension = ".ssfog";

        private bool hardcore;
        private bool pawnsVisible;
        private bool selectInFog;
        private bool hardcoreUndiscovered;
        private float blur;
        private float contrast;
        private float saturation;
        private float brightness;
        private float sightMultiplier;

        public static string FolderPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "FogPresets");

        public static string AbsPath(string fileName) => Path.Combine(FolderPath, fileName + Extension);

        public static IEnumerable<FileInfo> AllFiles
        {
            get
            {
                var directory = new DirectoryInfo(FolderPath);
                if (directory.Exists is false) directory.Create();
                return directory.GetFiles().Where(f => f.Extension == Extension).OrderByDescending(f => f.LastWriteTime);
            }
        }

        public static FogPreset FromCurrent() => new FogPreset
        {
            hardcore = FogStyle.HardcoreFog,
            pawnsVisible = FogStyle.PawnsVisibleInFog,
            selectInFog = FogStyle.SelectInFog,
            hardcoreUndiscovered = FogStyle.HardcoreUndiscovered,
            blur = FogStyle.Blur,
            contrast = FogStyle.Contrast,
            saturation = FogStyle.Saturation,
            brightness = FogStyle.Brightness,
            sightMultiplier = FogStyle.SightMultiplier
        };

        public void Apply()
        {
            FogStyle.HardcoreFog = hardcore;
            FogStyle.PawnsVisibleInFog = pawnsVisible;
            FogStyle.SelectInFog = selectInFog;
            FogStyle.HardcoreUndiscovered = hardcoreUndiscovered;
            FogStyle.Blur = Mathf.Clamp(blur, 0f, FogStyle.BlurMax);
            FogStyle.Contrast = Mathf.Clamp(contrast, FogStyle.ContrastMin, FogStyle.ContrastMax);
            FogStyle.Saturation = Mathf.Clamp(saturation, 0f, FogStyle.SaturationMax);
            FogStyle.Brightness = Mathf.Clamp(brightness, 0f, FogStyle.BrightnessMax);
            FogStyle.SightMultiplier = Mathf.Clamp(sightMultiplier, FogStyle.SightMultiplierMin, FogStyle.SightMultiplierMax);
            FogStyle.Touch();
            FogStyle.RecalculateMaps();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref hardcore, "hardcore", FogStyle.DefaultHardcore);
            Scribe_Values.Look(ref pawnsVisible, "pawnsVisible", FogStyle.DefaultPawnsVisible);
            Scribe_Values.Look(ref selectInFog, "selectInFog", FogStyle.DefaultSelectInFog);
            Scribe_Values.Look(ref hardcoreUndiscovered, "hardcoreUndiscovered", FogStyle.DefaultHardcoreUndiscovered);
            Scribe_Values.Look(ref blur, "blur", FogStyle.DefaultBlur);
            Scribe_Values.Look(ref contrast, "contrast", FogStyle.DefaultContrast);
            Scribe_Values.Look(ref saturation, "saturation", FogStyle.DefaultSaturation);
            Scribe_Values.Look(ref brightness, "brightness", FogStyle.DefaultBrightness);
            Scribe_Values.Look(ref sightMultiplier, "sightMultiplier", FogStyle.DefaultSightMultiplier);
        }

        public static void Save(FogPreset preset, string absPath)
        {
            try
            {
                SafeSaver.Save(absPath, "savedFogPreset", delegate
                {
                    ScribeMetaHeaderUtility.WriteMetaHeader();
                    Scribe_Deep.Look(ref preset, "preset");
                });
            }
            catch (Exception ex)
            {
                Log.Error("Exception while saving fog preset: " + ex);
            }
        }

        public static bool TryLoad(string absPath, out FogPreset preset)
        {
            preset = null;
            try
            {
                Scribe.loader.InitLoading(absPath);
                try
                {
                    ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.Xenotype, true);
                    Scribe_Deep.Look(ref preset, "preset");
                    Scribe.loader.FinalizeLoading();
                }
                catch
                {
                    Scribe.ForceStop();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception loading fog preset: " + ex);
                preset = null;
            }
            return preset != null;
        }
    }

    public abstract class Dialog_FogPresetList : Dialog_FileList
    {
        public override void ReloadFiles()
        {
            files.Clear();
            foreach (var file in FogPreset.AllFiles)
            {
                try
                {
                    var info = new SaveFileInfo(file);
                    info.LoadData();
                    files.Add(info);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception loading " + file.Name + ": " + ex);
                }
            }
        }
    }

    public class Dialog_FogPresetList_Load : Dialog_FogPresetList
    {
        public Dialog_FogPresetList_Load()
        {
            interactButLabel = "LoadGameButton".Translate();
            deleteTipKey = "SS_DeleteFogPreset";
        }

        public override void DoFileInteraction(string fileName)
        {
            if (FogPreset.TryLoad(FogPreset.AbsPath(fileName), out var preset))
            {
                preset.Apply();
            }
            Close();
        }
    }

    public class Dialog_FogPresetList_Save : Dialog_FogPresetList
    {
        private readonly FogPreset saving = FogPreset.FromCurrent();

        public override bool ShouldDoTypeInField => true;

        public Dialog_FogPresetList_Save()
        {
            interactButLabel = "OverwriteButton".Translate();
            deleteTipKey = "SS_DeleteFogPreset";
        }

        public override void DoFileInteraction(string fileName)
        {
            fileName = GenFile.SanitizedFileName(fileName);
            var absPath = FogPreset.AbsPath(fileName);
            LongEventHandler.QueueLongEvent(delegate
            {
                FogPreset.Save(saving, absPath);
            }, "SavingLongEvent", false, null);
            Messages.Message("SavedAs".Translate(fileName), MessageTypeDefOf.SilentInput, false);
            Close();
        }
    }
}
