using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xmap;

namespace Mod.CuongLe
{
    public static class AutoQuestCL
    {
        public enum QuestRunState
        {
            Off,
            WaitingForLogin,
            Learning,
            Navigating,
            Training,
            Interacting,
            Recovering,
            Completed,
            Error
        }

        private const int MaxQuestOrdinal = 7;
        private static bool initialized;
        private static long nextTick;
        private static long nextNpcTry;
        private static short lastTaskId = -1;
        private static int lastTaskIndex = -1;
        private static short pendingTaskId = -1;
        private static int pendingTaskIndex = -1;
        private static int pendingMobTemplateId = -1;
        private static int pendingNpcTemplateId = -1;
        private static short pendingCount;

        public static bool Enabled { get; private set; }
        public static QuestRunState State { get; private set; } = QuestRunState.Off;
        public static string StatusText { get; private set; } = "Auto Quest: OFF";

        public static void Enable()
        {
            Initialize();
            Enabled = true;
            Rms.saveRMSInt("AUTOQUEST_1_7", 1);
            State = QuestRunState.WaitingForLogin;
            SetStatus("Auto Quest 1-7: ON (Learning)");
            QuestLearningStore.Log("ENGINE ENABLE");
        }

        public static void Disable(string reason = null)
        {
            Enabled = false;
            Rms.saveRMSInt("AUTOQUEST_1_7", 0);
            StopQuestOwnedAutomation();
            State = QuestRunState.Off;
            SetStatus(string.IsNullOrEmpty(reason) ? "Auto Quest: OFF" : reason);
            QuestLearningStore.Log("ENGINE DISABLE " + reason);
        }

        public static void Toggle()
        {
            if (Enabled) Disable();
            else Enable();
        }

        public static void Update()
        {
            Initialize();
            if (!Enabled) return;

            long now = mSystem.currentTimeMillis();
            if (now < nextTick) return;
            nextTick = now + 450;

            try
            {
                Char me = Char.myCharz();
                if (me == null || string.IsNullOrEmpty(me.cName))
                {
                    State = QuestRunState.WaitingForLogin;
                    SetStatus("Auto Quest: chờ đăng nhập");
                    return;
                }

                if (me.meDead || (AutoTrainCL.isGoBack && !AutoTrainCL.ReturnedGoback))
                {
                    State = QuestRunState.Recovering;
                    SetStatus("Auto Quest: hồi sinh / goback");
                    return;
                }

                Task task = me.taskMaint;
                if (task == null)
                {
                    State = QuestRunState.Learning;
                    SetStatus("Auto Quest: chờ nhiệm vụ");
                    return;
                }

                VerifyPendingLearning(task);

                int ordinal = QuestLearningStore.ResolveQuestOrdinal(task.taskId);
                if (ordinal > MaxQuestOrdinal)
                {
                    StopQuestOwnedAutomation();
                    State = QuestRunState.Completed;
                    SetStatus("Auto Quest: đã qua NV7 - STOP");
                    QuestLearningStore.Log("COMPLETE AFTER QUEST 7 task=" + task.taskId);
                    return;
                }

                if (lastTaskId != task.taskId || lastTaskIndex != task.index)
                {
                    QuestLearningStore.Log("TASK_CHANGE q=" + ordinal + " task=" + task.taskId + " index=" + task.index + " map=" + TileMap.mapID + " count=" + task.count);
                    lastTaskId = task.taskId;
                    lastTaskIndex = task.index;
                    StopQuestOwnedAutomation();
                }

                LearnedQuestProfile profile = QuestLearningStore.Find(task.taskId, task.index);
                if (profile == null)
                {
                    profile = DiscoverProfile(task, ordinal);
                    QuestLearningStore.SaveOrUpdate(profile);
                }

                if (profile.RequiredPower > 0 && me.cPower < profile.RequiredPower)
                {
                    TrainForPower(profile.RequiredPower);
                    return;
                }

                if (profile.MobVerified && profile.MobTemplateId >= 0)
                {
                    if (profile.MapId >= 0 && TileMap.mapID != profile.MapId)
                    {
                        GoToMap(profile.MapId);
                        return;
                    }
                    StartTrainMob(profile.MobTemplateId, task);
                    return;
                }

                if (profile.NpcVerified && profile.NpcTemplateId >= 0)
                {
                    TryQuestNpc(profile.NpcTemplateId, profile.MenuId, profile.OptionId, task);
                    return;
                }

                LearnAndAct(task, profile);
            }
            catch (Exception ex)
            {
                State = QuestRunState.Error;
                SetStatus("Auto Quest lỗi: " + ex.Message);
                QuestLearningStore.Log("ENGINE_ERROR " + ex);
            }
        }

        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            QuestLearningStore.Load();

            int saved = Rms.loadRMSInt("AUTOQUEST_1_7");
            // On this dedicated feature branch, accounts launched through QLTK auto-login
            // start Auto Quest automatically on first run. The flag persists afterwards.
            if (saved == 1 || (saved == -1 && AutoLoginCL.IsEnabled))
            {
                Enabled = true;
                if (saved == -1) Rms.saveRMSInt("AUTOQUEST_1_7", 1);
                State = QuestRunState.WaitingForLogin;
                StatusText = "Auto Quest 1-7: ON (Learning)";
                QuestLearningStore.Log("ENGINE AUTO-ENABLE");
            }
        }

        private static LearnedQuestProfile DiscoverProfile(Task task, int ordinal)
        {
            int npcId = SafeGetTaskNpcId();
            int mobId = InferMobTemplate(task);
            long requiredPower = InferRequiredPower(task);

            LearnedQuestProfile p = new LearnedQuestProfile
            {
                QuestOrdinal = ordinal,
                TaskId = task.taskId,
                Index = task.index,
                MapId = TileMap.mapID,
                MobTemplateId = mobId,
                NpcTemplateId = npcId,
                MenuId = 0,
                OptionId = -1,
                RequiredPower = requiredPower,
                MobVerified = false,
                NpcVerified = false,
                Evidence = 0
            };

            QuestLearningStore.Log("DISCOVER q=" + ordinal + " task=" + task.taskId + " index=" + task.index +
                " map=" + TileMap.mapID + " mobCandidate=" + mobId + " npcCandidate=" + npcId + " power=" + requiredPower);
            return p;
        }

        private static void LearnAndAct(Task task, LearnedQuestProfile profile)
        {
            State = QuestRunState.Learning;

            int mobCandidate = InferMobTemplate(task);
            if (mobCandidate >= 0)
            {
                profile.MobTemplateId = mobCandidate;
                profile.MapId = TileMap.mapID;
                QuestLearningStore.SaveOrUpdate(profile);
                StartTrainMob(mobCandidate, task);
                SetStatus("Auto Quest học: thử mob " + mobCandidate);
                return;
            }

            int npcCandidate = SafeGetTaskNpcId();
            if (npcCandidate >= 0 && IsNpcInCurrentMap(npcCandidate))
            {
                profile.NpcTemplateId = npcCandidate;
                profile.MapId = TileMap.mapID;
                QuestLearningStore.SaveOrUpdate(profile);
                TryQuestNpc(npcCandidate, 0, -1, task);
                SetStatus("Auto Quest học: thử NPC nhiệm vụ " + npcCandidate);
                return;
            }

            // If the task explicitly asks for a power threshold, existing AutoTrainCL
            // is used even when the target mob has not been learned yet.
            if (profile.RequiredPower > 0 && Char.myCharz().cPower < profile.RequiredPower)
            {
                TrainForPower(profile.RequiredPower);
                return;
            }

            SetStatus("Auto Quest học: đang quan sát task " + task.taskId + "/" + task.index);
        }

        private static void StartTrainMob(int templateId, Task task)
        {
            State = QuestRunState.Training;
            PrepareGoback();

            List<int> list = AutoTrainCL.GetCurrentMapMobList();
            list.Clear();
            for (int i = 0; i < GameScr.vMob.size(); i++)
            {
                Mob mob = (Mob)GameScr.vMob.elementAt(i);
                if (!mob.isMobMe && mob.templateId == templateId && mob.hp > 0)
                    list.Add(mob.mobId);
            }

            if (list.Count == 0)
            {
                AutoTrainCL.isAutoTrain = false;
                SetStatus("Auto Quest: chờ mob template " + templateId);
                return;
            }

            pendingTaskId = task.taskId;
            pendingTaskIndex = task.index;
            pendingMobTemplateId = templateId;
            pendingNpcTemplateId = -1;
            pendingCount = task.count;
            AutoTrainCL.autoChangeZone = true;
            AutoTrainCL.isAutoTrain = true;
            SetStatus("Auto Quest: đánh mob " + templateId + " (đang xác minh)");
        }

        private static void TrainForPower(long requiredPower)
        {
            State = QuestRunState.Training;
            PrepareGoback();
            List<int> list = AutoTrainCL.GetCurrentMapMobList();
            if (list.Count == 0)
            {
                for (int i = 0; i < GameScr.vMob.size(); i++)
                {
                    Mob mob = (Mob)GameScr.vMob.elementAt(i);
                    if (!mob.isMobMe && mob.hp > 0) list.Add(mob.mobId);
                }
            }
            AutoTrainCL.autoChangeZone = true;
            AutoTrainCL.isAutoTrain = true;
            SetStatus("Auto Quest: cày SM " + Char.myCharz().cPower + "/" + requiredPower);
        }

        private static void TryQuestNpc(int npcTemplateId, int menuId, int optionId, Task task)
        {
            long now = mSystem.currentTimeMillis();
            if (now < nextNpcTry) return;
            nextNpcTry = now + 1800;

            State = QuestRunState.Interacting;
            AutoTrainCL.isAutoTrain = false;
            Char.myCharz().mobFocus = null;

            if (!IsNpcInCurrentMap(npcTemplateId))
            {
                SetStatus("Auto Quest: chưa thấy NPC " + npcTemplateId + " ở map hiện tại");
                return;
            }

            pendingTaskId = task.taskId;
            pendingTaskIndex = task.index;
            pendingNpcTemplateId = npcTemplateId;
            pendingMobTemplateId = -1;
            pendingCount = task.count;

            // Command 40 is the dedicated quest/NPC task protocol. option=-1 only
            // requests the task interaction without blindly selecting a shop/menu option.
            Service.gI().getTask(npcTemplateId, menuId, optionId);
            SetStatus("Auto Quest: tương tác NPC nhiệm vụ " + npcTemplateId);
        }

        private static void VerifyPendingLearning(Task current)
        {
            if (pendingTaskId < 0) return;

            bool taskAdvanced = current.taskId != pendingTaskId || current.index != pendingTaskIndex;
            bool countAdvanced = current.taskId == pendingTaskId && current.index == pendingTaskIndex && current.count > pendingCount;
            if (!taskAdvanced && !countAdvanced) return;

            if (pendingMobTemplateId >= 0)
                QuestLearningStore.ConfirmMob(pendingTaskId, pendingTaskIndex, pendingMobTemplateId);
            if (pendingNpcTemplateId >= 0)
                QuestLearningStore.ConfirmNpc(pendingTaskId, pendingTaskIndex, pendingNpcTemplateId, 0, -1);

            pendingTaskId = -1;
            pendingTaskIndex = -1;
            pendingMobTemplateId = -1;
            pendingNpcTemplateId = -1;
        }

        private static int InferMobTemplate(Task task)
        {
            string text = BuildTaskText(task);
            int bestTemplate = -1;
            int bestNameLength = 0;
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < GameScr.vMob.size(); i++)
            {
                Mob mob = (Mob)GameScr.vMob.elementAt(i);
                if (mob == null || mob.isMobMe || seen.Contains(mob.templateId)) continue;
                seen.Add(mob.templateId);

                string name = mob.getTemplate()?.name;
                if (string.IsNullOrEmpty(name)) continue;
                if (text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 && name.Length > bestNameLength)
                {
                    bestTemplate = mob.templateId;
                    bestNameLength = name.Length;
                }
            }
            return bestTemplate;
        }

        private static long InferRequiredPower(Task task)
        {
            string text = BuildTaskText(task);
            if (text.IndexOf("sức mạnh", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("suc manh", StringComparison.OrdinalIgnoreCase) < 0)
                return 0;

            long best = 0;
            MatchCollection numbers = Regex.Matches(text.Replace(".", string.Empty).Replace(",", string.Empty), "\\d{3,}");
            foreach (Match m in numbers)
            {
                if (long.TryParse(m.Value, out long value) && value > best) best = value;
            }
            return best;
        }

        private static string BuildTaskText(Task task)
        {
            StringBuilder sb = new StringBuilder();
            AppendArray(sb, task.names);
            AppendArray(sb, task.details);
            AppendArray(sb, task.subNames);
            AppendArray(sb, task.contentInfo);
            return sb.ToString();
        }

        private static void AppendArray(StringBuilder sb, string[] values)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrEmpty(values[i])) sb.Append(values[i]).Append(' ');
        }

        private static int SafeGetTaskNpcId()
        {
            try { return GameScr.getTaskNpcId(); }
            catch { return -1; }
        }

        private static bool IsNpcInCurrentMap(int templateId)
        {
            try
            {
                for (int i = 0; i < GameScr.vNpc.size(); i++)
                {
                    Npc npc = (Npc)GameScr.vNpc.elementAt(i);
                    if (npc?.template != null && npc.template.npcTemplateId == templateId) return true;
                }
            }
            catch { }
            return false;
        }

        private static void GoToMap(int mapId)
        {
            State = QuestRunState.Navigating;
            AutoTrainCL.isAutoTrain = false;
            Char.myCharz().mobFocus = null;
            if (!MainXmapCL.isXmaping) MainXmapCL.StartGoToMap(mapId);
            SetStatus("Auto Quest: Xmap tới " + mapId);
        }

        private static void PrepareGoback()
        {
            AutoTrainCL.isGoBack = true;
            AutoTrainCL.isGobackCoordinate = false;
            AutoTrainCL.gobackMapID = TileMap.mapID;
            AutoTrainCL.gobackZoneID = TileMap.zoneID;
        }

        private static void StopQuestOwnedAutomation()
        {
            AutoTrainCL.isAutoTrain = false;
            AutoTrainCL.autoChangeZone = false;
            Char me = Char.myCharz();
            if (me != null) me.mobFocus = null;
        }

        private static void SetStatus(string text)
        {
            if (StatusText == text) return;
            StatusText = text;
            try { GameScr.info1?.addInfo(text); } catch { }
        }
    }
}
