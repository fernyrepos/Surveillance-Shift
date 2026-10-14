using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [DefOf]
    public static class DefsOf
    {
        public static ThingDef SS_Mote_PingHostile;
        public static ThingDef SS_Mote_PingFriendly;
        public static ThingDef SS_Mote_Binoculars;
        public static ThingDef SS_Mote_Spyglass;
        public static SoundDef SS_Sound_Alarm;
        public static SoundDef SS_Sound_SignalHorn;
        public static SoundDef SS_Sound_LoudspeakerHostile;
        public static SoundDef SS_Sound_LoudspeakerSiren;
        public static SoundDef SS_Sound_LoudspeakerNeutral;
        public static SoundDef SS_Sound_PingHostile;
        public static SoundDef SS_Sound_PingFriendly;
        public static JobDef SS_OperateSurveillanceStation;
        public static JobDef SS_ViewCameraFeeds;
        public static JobDef SS_ManWatchtower;
        public static JobDef SS_MonitorRemoteDevice;
        public static JobDef SS_UseBinoculars;
        public static JobDef SS_UseTelescope;
        public static JobDef SS_LinkDeviceToRouter;
        public static JobDef SS_PushAlarmTrigger;
        public static JobDef SS_ResetAlarmTrigger;
        public static JobDef SS_AnimalReportThreat;
        public static JobDef SS_ReportThreatInPerson;
        public static JobDef SS_NegotiateWithScout;
        public static IncidentDef SS_FriendlyScouts;
        public static IncidentDef SS_EnemyScouts;
        public static ThingDef SS_SurveillanceStation;
        public static ThingDef SS_AlarmTrigger;
        public static ThingDef SS_Mote_RemoteSurveillanceDevice;
        public static ThingDef SS_WalkieTalkie;
        public static ThingDef SS_Binoculars;
        public static ThingDef SS_Telescope;
        [MayRequire("dubwise.dubsmintminimap")]
        public static SoundDef Sound_MapBoot;
        [MayRequire("dubwise.dubsmintminimap")]
        public static SoundDef Sound_MapFail;
        public static TrainableDef SS_KeepWatch;
        public static ThingDef SS_GuardPost;
        public static ThingDef SS_PatrolWaypoint;
        public static JobDef SS_GuardPostDuty;
        public static JobDef SS_PatrolDuty;
        public static JobDef SS_BodyguardDuty;
        public static JobDef SS_GuardEngage;

        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
