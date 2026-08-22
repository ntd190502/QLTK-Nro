using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Mod.CuongLe
{
    public sealed class LearnedQuestProfile
    {
        public int QuestOrdinal;
        public short TaskId;
        public int Index;
        public int MapId = -1;
        public int MobTemplateId = -1;
        public int NpcTemplateId = -1;
        public int MenuId = 0;
        public int OptionId = -1;
        public long RequiredPower;
        public bool MobVerified;
        public bool NpcVerified;
        public int Evidence;
    }

    public static class QuestLearningStore
    {
        private static readonly List<LearnedQuestProfile> Profiles = new List<LearnedQuestProfile>();
        private static bool loaded;

        private static string RootPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "AutoQuest");
        public static string ProfilePath => Path.Combine(RootPath, "quest_profiles.json");
        public static string LearningLogPath => Path.Combine(RootPath, "quest_learning.log");

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            Profiles.Clear();
            EnsureDirectory();

            if (!File.Exists(ProfilePath)) return;

            try
            {
                string json = File.ReadAllText(ProfilePath, Encoding.UTF8);
                MatchCollection matches = Regex.Matches(json,
                    "\\{\\\"questOrdinal\\\":(?<q>-?\\d+),\\\"taskId\\\":(?<t>-?\\d+),\\\"index\\\":(?<i>-?\\d+),\\\"mapId\\\":(?<m>-?\\d+),\\\"mobTemplateId\\\":(?<mob>-?\\d+),\\\"npcTemplateId\\\":(?<npc>-?\\d+),\\\"menuId\\\":(?<menu>-?\\d+),\\\"optionId\\\":(?<opt>-?\\d+),\\\"requiredPower\\\":(?<pow>-?\\d+),\\\"mobVerified\\\":(?<mv>true|false),\\\"npcVerified\\\":(?<nv>true|false),\\\"evidence\\\":(?<e>-?\\d+)\\}");

                foreach (Match match in matches)
                {
                    Profiles.Add(new LearnedQuestProfile
                    {
                        QuestOrdinal = int.Parse(match.Groups["q"].Value),
                        TaskId = short.Parse(match.Groups["t"].Value),
                        Index = int.Parse(match.Groups["i"].Value),
                        MapId = int.Parse(match.Groups["m"].Value),
                        MobTemplateId = int.Parse(match.Groups["mob"].Value),
                        NpcTemplateId = int.Parse(match.Groups["npc"].Value),
                        MenuId = int.Parse(match.Groups["menu"].Value),
                        OptionId = int.Parse(match.Groups["opt"].Value),
                        RequiredPower = long.Parse(match.Groups["pow"].Value),
                        MobVerified = bool.Parse(match.Groups["mv"].Value),
                        NpcVerified = bool.Parse(match.Groups["nv"].Value),
                        Evidence = int.Parse(match.Groups["e"].Value)
                    });
                }
            }
            catch (Exception ex)
            {
                Log("LOAD_ERROR " + ex.Message);
            }
        }

        public static LearnedQuestProfile Find(short taskId, int index)
        {
            Load();
            for (int i = 0; i < Profiles.Count; i++)
            {
                LearnedQuestProfile p = Profiles[i];
                if (p.TaskId == taskId && p.Index == index) return p;
            }
            return null;
        }

        public static int ResolveQuestOrdinal(short taskId)
        {
            Load();
            int highest = 0;
            for (int i = 0; i < Profiles.Count; i++)
            {
                LearnedQuestProfile p = Profiles[i];
                if (p.TaskId == taskId) return p.QuestOrdinal;
                if (p.QuestOrdinal > highest) highest = p.QuestOrdinal;
            }
            return highest + 1;
        }

        public static void SaveOrUpdate(LearnedQuestProfile profile)
        {
            Load();
            int found = -1;
            for (int i = 0; i < Profiles.Count; i++)
            {
                if (Profiles[i].TaskId == profile.TaskId && Profiles[i].Index == profile.Index)
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0) Profiles[found] = profile;
            else Profiles.Add(profile);

            SaveAll();
        }

        public static void ConfirmMob(short taskId, int index, int templateId)
        {
            LearnedQuestProfile p = Find(taskId, index);
            if (p == null) return;
            p.MobTemplateId = templateId;
            p.MobVerified = true;
            p.Evidence++;
            SaveOrUpdate(p);
            Log("CONFIRM_MOB task=" + taskId + " index=" + index + " mob=" + templateId);
        }

        public static void ConfirmNpc(short taskId, int index, int npcTemplateId, int menuId, int optionId)
        {
            LearnedQuestProfile p = Find(taskId, index);
            if (p == null) return;
            p.NpcTemplateId = npcTemplateId;
            p.MenuId = menuId;
            p.OptionId = optionId;
            p.NpcVerified = true;
            p.Evidence++;
            SaveOrUpdate(p);
            Log("CONFIRM_NPC task=" + taskId + " index=" + index + " npc=" + npcTemplateId + " menu=" + menuId + " option=" + optionId);
        }

        public static void Log(string text)
        {
            try
            {
                EnsureDirectory();
                File.AppendAllText(LearningLogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + text + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }

        private static void SaveAll()
        {
            try
            {
                EnsureDirectory();
                StringBuilder sb = new StringBuilder();
                sb.Append("[\n");
                for (int i = 0; i < Profiles.Count; i++)
                {
                    LearnedQuestProfile p = Profiles[i];
                    sb.Append("  {\"questOrdinal\":").Append(p.QuestOrdinal)
                      .Append(",\"taskId\":").Append(p.TaskId)
                      .Append(",\"index\":").Append(p.Index)
                      .Append(",\"mapId\":").Append(p.MapId)
                      .Append(",\"mobTemplateId\":").Append(p.MobTemplateId)
                      .Append(",\"npcTemplateId\":").Append(p.NpcTemplateId)
                      .Append(",\"menuId\":").Append(p.MenuId)
                      .Append(",\"optionId\":").Append(p.OptionId)
                      .Append(",\"requiredPower\":").Append(p.RequiredPower)
                      .Append(",\"mobVerified\":").Append(p.MobVerified ? "true" : "false")
                      .Append(",\"npcVerified\":").Append(p.NpcVerified ? "true" : "false")
                      .Append(",\"evidence\":").Append(p.Evidence)
                      .Append("}");
                    if (i + 1 < Profiles.Count) sb.Append(',');
                    sb.Append('\n');
                }
                sb.Append("]\n");
                File.WriteAllText(ProfilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log("SAVE_ERROR " + ex.Message);
            }
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(RootPath)) Directory.CreateDirectory(RootPath);
        }
    }
}
