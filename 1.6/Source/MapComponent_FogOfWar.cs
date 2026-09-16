using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [StaticConstructorOnStartup]
    [HotSwappable]
    public class MapComponent_FogOfWar : MapComponent
    {
        private const int StandardSightRadius = 26;
        private const int DownedSightRadius = 4;
        private const int UpdateIntervalTicks = 12;
        private const int SectionSize = 16;
        private const int WalkieTalkieSightingRadiusSq = 676;
        private const float ArrowEdgeDistance = 7f;
        private const float ArrowCenterRadiusSq = 9f * 9f;
        private const int ArrowRenderQueue = 3960;
        private static readonly float FogAltitude = AltitudeLayer.Weather.AltitudeFor() + 0.05f;

        private static readonly Material DarkFogMat = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, new ColorInt(25, 25, 25).ToColor, 3900);
        private static readonly float ArrowAltitude = AltitudeLayer.Weather.AltitudeFor() + 0.1f;
        private static readonly Material HostileArrowMat = MaterialPool.MatFrom("UI/Overlays/Arrow", ShaderDatabase.Transparent, DefsOf.SS_Mote_PingHostile.graphicData.color, ArrowRenderQueue);
        private static readonly Material FriendlyArrowMat = MaterialPool.MatFrom("UI/Overlays/Arrow", ShaderDatabase.Transparent, DefsOf.SS_Mote_PingFriendly.graphicData.color, ArrowRenderQueue);

        private bool[] visibleCells;
        private bool[] prevCells;
        private readonly Dictionary<int, Mesh> sectionMeshes = new();
        private readonly List<Vector3> sectionVertices = new(SectionSize * SectionSize * 4);
        private readonly List<Vector2> sectionUVs = new(SectionSize * SectionSize * 4);
        private readonly List<int> sectionIndices = new(SectionSize * SectionSize * 6);
        private int sectionsX;
        private int sectionsZ;
        private bool firstPass = true;
        private List<MutedArrival> mutedArrivals = new();
        private List<Message> mutedMessages = new();
        private List<Letter> mutedLetters = new();
        private HashSet<int> sightedHostileIDs = new();
        private SoftFogRenderer softFog;
        private bool lastSoftFogEnabled = SoftFogRenderer.Enabled;

        private SoftFogRenderer SoftFog => softFog ??= new SoftFogRenderer(map);

        public class MutedArrival : IExposable
        {
            public Letter letter;
            public List<Pawn> pawns = new();
            public bool isHostile;

            public void ExposeData()
            {
                Scribe_Deep.Look(ref letter, "letter");
                Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
                Scribe_Values.Look(ref isHostile, "isHostile");
            }
        }

        public MapComponent_FogOfWar(Map map) : base(map)
        {
            visibleCells = new bool[map.cellIndices.NumGridCells];
            prevCells = new bool[map.cellIndices.NumGridCells];
        }

        public bool IsVisible(IntVec3 cell)
        {
            if ((uint)cell.x >= (uint)map.Size.x || (uint)cell.z >= (uint)map.Size.z) return false;
            return visibleCells[cell.z * map.Size.x + cell.x];
        }

        public void QueueArrival(Letter letter, List<Pawn> pawns, bool isHostile)
        {
            mutedArrivals.Add(new MutedArrival
            {
                letter = letter,
                pawns = pawns,
                isHostile = isHostile
            });
        }

        public void QueueMessage(Message message)
        {
            mutedMessages.Add(message);
        }

        public void QueueLetter(Letter letter)
        {
            mutedLetters.Add(letter);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (State.IsLimitedAwareness)
            {
                LongEventHandler.ExecuteWhenFinished(RecalculateVisibility);
            }
        }

        public override void MapComponentTick()
        {
            if (State.IsLimitedAwareness && Find.TickManager.TicksGame % UpdateIntervalTicks == 0)
            {
                RecalculateVisibility();
                ProcessWalkieTalkieSightings();
                ProcessSignalHornSightings();
                ProcessMutedArrivals();
                ProcessMutedMessages();
                ProcessMutedLetters();
            }
        }

        public override void MapComponentUpdate()
        {
            if (State.IsLimitedAwareness is false || Find.CurrentMap != map || WorldRendererUtility.WorldSelected || WorldComponent_GravshipController.CutsceneInProgress || Find.GravshipController.LandingAreaConfirmationInProgress) return;

            if (SoftFogRenderer.Enabled)
            {
                SoftFog.Draw();
            }
            else
            {
                var viewRect = Find.CameraDriver.CurrentViewRect;
                var minSX = Mathf.Max(0, viewRect.minX / SectionSize);
                var maxSX = Mathf.Min(sectionsX - 1, viewRect.maxX / SectionSize);
                var minSZ = Mathf.Max(0, viewRect.minZ / SectionSize);
                var maxSZ = Mathf.Min(sectionsZ - 1, viewRect.maxZ / SectionSize);

                for (var sz = minSZ; sz <= maxSZ; sz++)
                {
                    var row = sz * sectionsX;
                    for (var sx = minSX; sx <= maxSX; sx++)
                    {
                        if (sectionMeshes.TryGetValue(row + sx, out var mesh))
                        {
                            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, DarkFogMat, 0);
                        }
                    }
                }
            }

            DrawPingArrows();
        }

        private void DrawPingArrows()
        {
            DrawPings(DefsOf.SS_Mote_PingHostile, HostileArrowMat);
            DrawPings(DefsOf.SS_Mote_PingFriendly, FriendlyArrowMat);
        }

        private void DrawPings(ThingDef def, Material mat)
        {
            foreach (var thing in map.listerThings.ThingsOfDef(def))
            {
                DrawPingArrow(((Mote)thing).exactPosition, mat);
            }
        }

        private void DrawPingArrow(Vector3 mapTarget, Material mat)
        {
            var center = UI.UIToMapPosition(UI.screenWidth / 2, UI.screenHeight / 2);
            if ((center - mapTarget).MagnitudeHorizontalSquared() < ArrowCenterRadiusSq)
            {
                var position = mapTarget;
                position.y = ArrowAltitude;
                position.z -= 1.5f;
                Graphics.DrawMesh(MeshPool.plane20, position, Quaternion.identity, mat, 0);
            }
            else
            {
                var dir = (mapTarget - center).Yto0().normalized;
                var edgePos = center + dir * ArrowEdgeDistance;
                edgePos.y = ArrowAltitude;
                Graphics.DrawMesh(MeshPool.plane20, edgePos, Quaternion.LookRotation(dir), mat, 0);
            }
        }

        private void RecalculateVisibility()
        {
            var count = map.cellIndices.NumGridCells;
            if (visibleCells.Length != count)
            {
                visibleCells = new bool[count];
                prevCells = new bool[count];
                firstPass = true;
            }
            if (Find.GravshipController.LandingAreaConfirmationInProgress)
            {
                (visibleCells, prevCells) = (prevCells, visibleCells);
                for (var i = 0; i < count; i++)
                {
                    visibleCells[i] = true;
                }
                PublishFog();
            }
            else
            {
                (visibleCells, prevCells) = (prevCells, visibleCells);
                Array.Clear(visibleCells, 0, count);
                if (ModCompatibility.IsPerspectiveShiftAuthentic())
                {
                    var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
                    if (avatar != null && avatar.Spawned && avatar.Map == map && avatar.Dead is false)
                    {
                        var radius = avatar.Downed ? DownedSightRadius : StandardSightRadius;
                        ShadowCaster.ComputeFieldOfView(avatar.Position, radius, map, visibleCells);
                    }
                    foreach (var b in map.listerBuildings.allBuildingsColonist)
                    {
                        if (b is Building_Camera cam)
                        {
                            if (cam.IsActive && (ModCompatibility.IsPerspectiveShiftCameraLockedTo(cam.Position) || State.ActiveFeedCamera == cam))
                            {
                                cam.ComputeCameraFOV(visibleCells);
                            }
                        }
                        else if (b is Building_Watchtower tower && tower.IsMannedByAvatar)
                        {
                            tower.ComputeWatchtowerFOV(visibleCells);
                        }
                    }
                }
                else
                {
                    foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                    {
                        if (pawn.Dead is false)
                        {
                            var radius = pawn.Downed ? DownedSightRadius : StandardSightRadius;
                            ShadowCaster.ComputeFieldOfView(pawn.Position, radius, map, visibleCells);
                        }
                    }
                    foreach (var b in map.listerBuildings.allBuildingsColonist)
                    {
                        if (b is Building_Camera cam)
                        {
                            cam.ComputeCameraFOV(visibleCells);
                        }
                        else if (b is Building_Watchtower tower)
                        {
                            tower.ComputeWatchtowerFOV(visibleCells);
                        }
                    }
                }

                PublishFog();
            }
        }

        private void PublishFog()
        {
            if (SoftFogRenderer.Enabled != lastSoftFogEnabled)
            {
                lastSoftFogEnabled = SoftFogRenderer.Enabled;
                firstPass = true;
                if (SoftFogRenderer.Enabled)
                {
                    ClearSectionMeshes();
                }
            }
            if (SoftFogRenderer.Enabled)
            {
                SoftFog.Submit(visibleCells);
            }
            else
            {
                RebuildSections();
            }
        }

        private void ClearSectionMeshes()
        {
            foreach (var mesh in sectionMeshes.Values)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            sectionMeshes.Clear();
        }

        private void RebuildSections()
        {
            sectionsX = (map.Size.x + SectionSize - 1) / SectionSize;
            sectionsZ = (map.Size.z + SectionSize - 1) / SectionSize;
            var mapX = map.Size.x;
            var mapZ = map.Size.z;

            for (var sz = 0; sz < sectionsZ; sz++)
            {
                var z0 = sz * SectionSize;
                var zMax = Mathf.Min(z0 + SectionSize, mapZ);
                var zSpan = zMax - z0;

                for (var sx = 0; sx < sectionsX; sx++)
                {
                    var x0 = sx * SectionSize;
                    var xMax = Mathf.Min(x0 + SectionSize, mapX);
                    var totalCells = (xMax - x0) * zSpan;

                    var changed = firstPass;
                    var foggedCount = 0;

                    for (var z = z0; z < zMax; z++)
                    {
                        var row = z * mapX;
                        for (var x = x0; x < xMax; x++)
                        {
                            var idx = row + x;
                            if (visibleCells[idx] is false)
                            {
                                foggedCount++;
                            }
                            if (changed is false && visibleCells[idx] != prevCells[idx])
                            {
                                changed = true;
                            }
                        }
                    }

                    if (changed is false) continue;

                    var key = sz * sectionsX + sx;

                    if (foggedCount == 0)
                    {
                        if (sectionMeshes.Remove(key, out var cleared))
                        {
                            UnityEngine.Object.Destroy(cleared);
                        }
                        continue;
                    }

                    sectionVertices.Clear();
                    sectionUVs.Clear();
                    sectionIndices.Clear();

                    if (foggedCount == totalCells)
                    {
                        AddQuad(x0, z0, xMax, zMax);
                    }
                    else
                    {
                        for (var z = z0; z < zMax; z++)
                        {
                            var row = z * mapX;
                            for (var x = x0; x < xMax; x++)
                            {
                                if (visibleCells[row + x] is false)
                                {
                                    AddQuad(x, z, x + 1, z + 1);
                                }
                            }
                        }
                    }

                    if (sectionMeshes.TryGetValue(key, out var mesh) is false)
                    {
                        mesh = new Mesh
                        {
                            name = "SSFog_" + sx + "_" + sz
                        };
                        sectionMeshes[key] = mesh;
                    }
                    mesh.Clear();
                    mesh.SetVertices(sectionVertices);
                    mesh.SetUVs(0, sectionUVs);
                    mesh.SetIndices(sectionIndices, MeshTopology.Triangles, 0);
                }
            }

            firstPass = false;
        }

        private void AddQuad(float x1, float z1, float x2, float z2)
        {
            var v = sectionVertices.Count;
            sectionVertices.Add(new Vector3(x1, FogAltitude, z1));
            sectionVertices.Add(new Vector3(x1, FogAltitude, z2));
            sectionVertices.Add(new Vector3(x2, FogAltitude, z2));
            sectionVertices.Add(new Vector3(x2, FogAltitude, z1));
            sectionUVs.Add(new Vector2(0f, 0f));
            sectionUVs.Add(new Vector2(0f, 1f));
            sectionUVs.Add(new Vector2(1f, 1f));
            sectionUVs.Add(new Vector2(1f, 0f));
            sectionIndices.Add(v);
            sectionIndices.Add(v + 1);
            sectionIndices.Add(v + 2);
            sectionIndices.Add(v);
            sectionIndices.Add(v + 2);
            sectionIndices.Add(v + 3);
        }

        private void ProcessMutedArrivals()
        {
            for (int i = mutedArrivals.Count - 1; i >= 0; i--)
            {
                var arrival = mutedArrivals[i];
                arrival.pawns.RemoveAll(p => p.Dead || p.Discarded || p.Spawned && p.Map != map);

                if (arrival.pawns.Count == 0)
                {
                    mutedArrivals.RemoveAt(i);
                    continue;
                }

                foreach (var pawn in arrival.pawns)
                {
                    if (pawn.Spawned && pawn.Position.InBounds(map) && visibleCells[map.cellIndices.CellToIndex(pawn.Position)])
                    {
                        arrival.letter.lookTargets = new LookTargets(pawn);
                        Find.LetterStack.ReceiveLetter(arrival.letter);
                        if (Building_Loudspeaker.BroadcastNews(map, arrival.pawns[0].Position, arrival.isHostile) is false)
                        {
                            State.SpawnPing(arrival.pawns[0].Position, map, arrival.isHostile);
                        }
                        mutedArrivals.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void ProcessWalkieTalkieSightings()
        {
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (ModCompatibility.IsPerspectiveShiftAuthentic() && avatar != null && avatar.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie) is false)
            {
                return;
            }

            var spotters = new List<Pawn>();
            foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead is false && colonist.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie))
                {
                    spotters.Add(colonist);
                }
            }
            if (spotters.Count == 0) return;
            foreach (var hostile in map.mapPawns.AllPawnsSpawned)
            {
                if (hostile.HostileTo(Faction.OfPlayer) && hostile.Dead is false && sightedHostileIDs.Contains(hostile.thingIDNumber) is false)
                {
                    foreach (var spotter in spotters)
                    {
                        if (spotter.Position.DistanceToSquared(hostile.Position) <= WalkieTalkieSightingRadiusSq && GenSight.LineOfSight(spotter.Position, hostile.Position, map))
                        {
                            sightedHostileIDs.Add(hostile.thingIDNumber);
                            if (Building_Loudspeaker.BroadcastNews(map, hostile.Position, true) is false)
                            {
                                State.SpawnPing(hostile.Position, map, true);
                            }
                            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("SS_HostileSpotted".Translate(), "SS_HostileSpottedDesc".Translate(spotter.LabelShort, hostile.LabelShort), LetterDefOf.ThreatBig, new LookTargets(hostile)));
                            break;
                        }
                    }
                }
            }
        }

        private void ProcessSignalHornSightings()
        {
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            var hornBearers = new List<(Pawn pawn, CompSignalHorn horn)>();
            foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist.Downed || colonist.InMentalState || colonist.Awake() is false || colonist == avatar) continue;
                var horn = CompSignalHorn.FindSignalHorn(colonist);
                if (horn != null)
                {
                    hornBearers.Add((colonist, horn));
                }
            }
            if (hornBearers.Count == 0) return;

            foreach (var hostile in map.mapPawns.AllPawnsSpawned)
            {
                if (hostile.Dead || hostile.Downed || hostile.HostileTo(Faction.OfPlayer) is false || sightedHostileIDs.Contains(hostile.thingIDNumber)) continue;

                foreach (var (spotter, horn) in hornBearers)
                {
                    var sightRadius = spotter.CurJobDef == DefsOf.SS_ManWatchtower && spotter.CurJob?.targetA.Thing is Building_Watchtower tower
                        ? tower.Extension.sightRadius
                        : StandardSightRadius;
                    if (spotter.Position.DistanceToSquared(hostile.Position) <= sightRadius * sightRadius && GenSight.LineOfSight(spotter.Position, hostile.Position, map, skipFirstCell: true))
                    {
                        sightedHostileIDs.Add(hostile.thingIDNumber);
                        horn.Blow(spotter);
                        return;
                    }
                }
            }
        }

        private void ProcessMutedLetters()
        {
            for (int i = mutedLetters.Count - 1; i >= 0; i--)
            {
                var letter = mutedLetters[i];
                if (HasCheckableTarget(letter.lookTargets) is false)
                {
                    mutedLetters.RemoveAt(i);
                }
                else if (AnyTargetVisible(letter.lookTargets))
                {
                    letter.lookTargets = new LookTargets(letter.lookTargets.targets.Where(t => t.IsValid).ToArray());
                    mutedLetters.RemoveAt(i);
                    Find.LetterStack.ReceiveLetter(letter);
                }
            }
        }

        private void ProcessMutedMessages()
        {
            for (int i = mutedMessages.Count - 1; i >= 0; i--)
            {
                var message = mutedMessages[i];
                if (HasCheckableTarget(message.lookTargets) is false)
                {
                    mutedMessages.RemoveAt(i);
                }
                else if (AnyTargetVisible(message.lookTargets))
                {
                    message.ResetTimer();
                    mutedMessages.RemoveAt(i);
                    Messages.Message(message);
                }
            }
        }

        private bool HasCheckableTarget(LookTargets lookTargets)
        {
            return lookTargets.targets.Any(t => t.ThingDestroyed is false);
        }

        private bool AnyTargetVisible(LookTargets lookTargets)
        {
            return lookTargets.targets.Any(t => t.ThingDestroyed is false && t.Map == map && t.Cell.InBounds(map) && visibleCells[map.cellIndices.CellToIndex(t.Cell)]);
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            softFog?.Dispose();
            softFog = null;
            ClearSectionMeshes();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref mutedArrivals, "SS_MutedArrivals", LookMode.Deep);
            Scribe_Collections.Look(ref mutedMessages, "SS_MutedMessages", LookMode.Deep);
            Scribe_Collections.Look(ref mutedLetters, "SS_MutedLetters", LookMode.Deep);
            Scribe_Collections.Look(ref sightedHostileIDs, "SS_SightedHostileIDs", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                mutedArrivals ??= new List<MutedArrival>();
                mutedMessages ??= new List<Message>();
                mutedLetters ??= new List<Letter>();
                sightedHostileIDs ??= new HashSet<int>();
            }
        }
    }
}
