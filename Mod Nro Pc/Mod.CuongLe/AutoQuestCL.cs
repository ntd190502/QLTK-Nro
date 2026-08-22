using System;
using main.Mod;
using Xmap;

namespace Mod.CuongLe
{
    /// <summary>
    /// Quest orchestrator for early-game quests (1 -> 7).
    /// It deliberately reuses existing automation modules instead of reimplementing
    /// movement, training, recovery and login logic.
    ///
    /// IMPORTANT: quest-specific taskId/index/map/mob/npc bindings must be filled
    /// from live server observations before enabling a quest step. Until a binding
    /// is confirmed, the engine stays in a safe WAIT state rather than guessing.
    /// </summary>
    public static class AutoQuestCL
    {
        public enum QuestRunState
        {
            Off,
            WaitingForLogin,
            ReadingQuest,
            Navigating,
            Training,
            Interacting,
            Recovering,
            WaitingForBinding,
            Completed,
            Error
        }

        public sealed class QuestBinding
        {
            public short TaskId;
            public int Index;
            public int TargetMapId = -1;
            public int TargetMobTemplateId = -1;
            public int TargetNpcTemplateId = -1;
            public int MenuId = -1;
            public int OptionId = -1;
            public long RequiredPower;
            public bool StopAfterCompletion;
        }

        public static bool Enabled;
        public static QuestRunState State { get; private set; } = QuestRunState.Off;
        public static string StatusText { get; private set; } = "Auto Quest: OFF";

        // Bindings are intentionally empty-by-default. We do not guess protocol IDs.
        // They can be populated after observing real taskId/index values on the live server.
        private static readonly QuestBinding[] Bindings = new QuestBinding[7];

        private static long _nextTick;
        private static short _lastTaskId = -1;
        private static int _lastTaskIndex = -1;

        public static void Enable()
        {
            Enabled = true;
            State = QuestRunState.WaitingForLogin;
            SetStatus("Auto Quest 1-7: ON");
        }

        public static void Disable(string reason = null)
        {
            Enabled = false;
            StopQuestOwnedAutomation();
            State = QuestRunState.Off;
            SetStatus(string.IsNullOrEmpty(reason) ? "Auto Quest: OFF" : reason);
        }

        public static void Update()
        {
            if (!Enabled) return;

            long now = mSystem.currentTimeMillis();
            if (now < _nextTick) return;
            _nextTick = now + 500;

            try
            {
                Char me = Char.myCharz();
                if (me == null || string.IsNullOrEmpty(me.cName))
                {
                    State = QuestRunState.WaitingForLogin;
                    SetStatus("Auto Quest: chờ đăng nhập");
                    return;
                }

                // Let existing death/recovery/goback automation own recovery.
                if (me.meDead || (AutoTrainCL.isGoBack && !AutoTrainCL.ReturnedGoback))
                {
                    State = QuestRunState.Recovering;
                    SetStatus("Auto Quest: đang hồi phục/goback");
                    return;
                }

                Task task = GetCurrentTask();
                if (task == null)
                {
                    State = QuestRunState.ReadingQuest;
                    SetStatus("Auto Quest: chờ dữ liệu nhiệm vụ");
                    return;
                }

                if (_lastTaskId != task.taskId || _lastTaskIndex != task.index)
                {
                    _lastTaskId = task.taskId;
                    _lastTaskIndex = task.index;
                    StopQuestOwnedAutomation();
                }

                QuestBinding binding = FindBinding(task.taskId, task.index);
                if (binding == null)
                {
                    State = QuestRunState.WaitingForBinding;
                    SetStatus($"Auto Quest: chưa map taskId={task.taskId}, index={task.index}");
                    return;
                }

                // Generic power gate. Existing AutoTrainCL handles train, stamina,
                // death, HP/MP recovery, goback and optional zone changing.
                if (binding.RequiredPower > 0 && me.cPower < binding.RequiredPower)
                {
                    HandleTraining(binding);
                    return;
                }

                // Generic navigation gate.
                if (binding.TargetMapId >= 0 && TileMap.mapID != binding.TargetMapId)
                {
                    HandleNavigation(binding.TargetMapId);
                    return;
                }

                // Generic kill/train step.
                if (binding.TargetMobTemplateId >= 0)
                {
                    HandleMobStep(binding.TargetMobTemplateId);
                    return;
                }

                // Generic NPC interaction step.
                if (binding.TargetNpcTemplateId >= 0)
                {
                    HandleNpcStep(binding);
                    return;
                }

                State = QuestRunState.ReadingQuest;
                SetStatus($"Auto Quest: task {task.taskId}/{task.index} không cần action chung");
            }
            catch (Exception ex)
            {
                State = QuestRunState.Error;
                SetStatus("Auto Quest lỗi: " + ex.Message);
            }
        }

        private static Task GetCurrentTask()
        {
            // NRO client stores current quest in Char.myCharz().taskMaint in common builds.
            // Use reflection fallback to avoid hard dependency if this decompiled build renamed it.
            try
            {
                var me = Char.myCharz();
                var field = me.GetType().GetField("taskMaint");
                if (field != null)
                    return field.GetValue(me) as Task;
            }
            catch { }
            return null;
        }

        private static QuestBinding FindBinding(short taskId, int index)
        {
            for (int i = 0; i < Bindings.Length; i++)
            {
                QuestBinding b = Bindings[i];
                if (b != null && b.TaskId == taskId && b.Index == index)
                    return b;
            }
            return null;
        }

        private static void HandleNavigation(int mapId)
        {
            State = QuestRunState.Navigating;
            AutoTrainCL.isAutoTrain = false;
            Char.myCharz().mobFocus = null;

            if (!MainXmapCL.isXmaping)
                MainXmapCL.StartGoToMap(mapId);

            SetStatus("Auto Quest: xmap tới " + mapId);
        }

        private static void HandleTraining(QuestBinding binding)
        {
            State = QuestRunState.Training;

            if (binding.TargetMapId >= 0 && TileMap.mapID != binding.TargetMapId)
            {
                HandleNavigation(binding.TargetMapId);
                return;
            }

            AutoTrainCL.isGoBack = true;
            AutoTrainCL.isGobackCoordinate = false;
            AutoTrainCL.gobackMapID = TileMap.mapID;
            AutoTrainCL.gobackZoneID = TileMap.zoneID;
            AutoTrainCL.autoChangeZone = true;
            AutoTrainCL.isAutoTrain = true;

            SetStatus($"Auto Quest: train SM {Char.myCharz().cPower}/{binding.RequiredPower}");
        }

        private static void HandleMobStep(int templateId)
        {
            State = QuestRunState.Training;

            AutoTrainCL.isGoBack = true;
            AutoTrainCL.isGobackCoordinate = false;
            AutoTrainCL.gobackMapID = TileMap.mapID;
            AutoTrainCL.gobackZoneID = TileMap.zoneID;
            AutoTrainCL.autoChangeZone = true;

            var list = AutoTrainCL.GetCurrentMapMobList();
            if (list.Count == 0)
            {
                for (int i = 0; i < GameScr.vMob.size(); i++)
                {
                    Mob mob = (Mob)GameScr.vMob.elementAt(i);
                    if (!mob.isMobMe && mob.templateId == templateId)
                        list.Add(mob.mobId);
                }
            }

            AutoTrainCL.isAutoTrain = true;
            SetStatus("Auto Quest: đánh mob template " + templateId);
        }

        private static void HandleNpcStep(QuestBinding binding)
        {
            State = QuestRunState.Interacting;
            AutoTrainCL.isAutoTrain = false;
            Char.myCharz().mobFocus = null;

            // Server-side quest interaction primitive already exists in Service.cs.
            Service.gI().getTask(binding.TargetNpcTemplateId, binding.MenuId, binding.OptionId);
            SetStatus($"Auto Quest: NPC {binding.TargetNpcTemplateId}");
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
            StatusText = text;
            try { GameScr.info1?.addInfo(text); } catch { }
        }

        // Live-test helper: once taskId/index values are observed, call this from a
        // temporary debug hook or populate the table permanently.
        public static void SetQuestBinding(int slot, short taskId, int index,
            int targetMapId = -1, int targetMobTemplateId = -1,
            int targetNpcTemplateId = -1, int menuId = -1, int optionId = -1,
            long requiredPower = 0, bool stopAfterCompletion = false)
        {
            if (slot < 1 || slot > 7) return;
            Bindings[slot - 1] = new QuestBinding
            {
                TaskId = taskId,
                Index = index,
                TargetMapId = targetMapId,
                TargetMobTemplateId = targetMobTemplateId,
                TargetNpcTemplateId = targetNpcTemplateId,
                MenuId = menuId,
                OptionId = optionId,
                RequiredPower = requiredPower,
                StopAfterCompletion = stopAfterCompletion
            };
        }
    }
}
