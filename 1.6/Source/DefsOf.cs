using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [DefOf]
    public static class DefsOf
    {
        public static ThingDef SS_Mote_PingHostile;
        public static ThingDef SS_Mote_PingFriendly;
        public static ThingDef SS_Mote_Spyglass;
        public static ThingDef SS_Mote_Binoculars;
        public static SoundDef SS_Sound_Alarm;
        public static SoundDef SS_Sound_LoudspeakerHostile;
        public static SoundDef SS_Sound_LoudspeakerNeutral;
        public static SoundDef SS_Sound_PingHostile;
        public static SoundDef SS_Sound_PingFriendly;
        public static JobDef SS_OperateSurveillanceStation;
        public static JobDef SS_ViewCameraFeeds;
        public static JobDef SS_ManWatchtower;
        public static JobDef SS_MonitorRemoteDevice;
        public static JobDef SS_LinkDeviceToRouter;
        public static JobDef SS_PushAlarmTrigger;
        public static JobDef SS_AnimalReportThreat;
        public static JobDef SS_ReportThreatInPerson;
        public static ThingDef SS_SurveillanceStation;
        public static ThingDef SS_Mote_RemoteSurveillanceDevice;
        public static ThingDef SS_WalkieTalkie;
        [MayRequire("dubwise.dubsmintminimap")]
        public static SoundDef Sound_MapBoot;
        [MayRequire("dubwise.dubsmintminimap")]
        public static SoundDef Sound_MapFail;
        [MayRequire("Thek.GuardingP")]
        public static TrainableDef SS_KeepWatch;

        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
