using Assets.src.g;
using DoHoa;
using DoHoa.CustomMenu;
using DoHoa.CustomMenu.Shared;
using Mod.community;
using Mod.CuongLe;
using Mod_nro.MenuDataGame;
using ModCak.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using Xmap;

namespace main.Mod;

public class MainMod : IActionListener, IChatable
{
    public static MainMod _Instance;

    public static int int_0;

    public static int runSpeed;

    public static bool isAutoRevive;

    public static bool isLockFocus;

    public static int charIDLock;

    public static string[] inputLockFocusCharID;

    public static int zoneIdNRD;

    public static int mapIdNRD;

    public static bool isOpenMenuNPC;

    public static bool isAutoEnterNRDMap;

    public static string[] nameMapsNRD;

    public static int int_4;

    public static bool bool_1;

    public static string[] inputHPPercentFusionDance;

    public static string[] inputHPFusionDance;

    public static int minumumHPPercentFusionDance;

    public static int minimumHPFusionDance;

    public static List<int> listCharIDs;

    public static string[] inputCharID;

    public static bool isAutoLockControl;

    public static bool isAutoTeleport;

    private static string thongbaoVIPne;

    public static bool isAutoAttackBoss;

    public static int HPLimit;

    public static string[] inputHPLimit;

    public static bool isAutoAttackOtherChars;

    public static int limitHPChar;

    public static string[] inputHPChar;

    public static List<Boss> listBosses;

    public static Image logoServerListScreen;

    public static Image logoGameScreen;

    public static List<Image> listBackgroundImages;

    public static List<Color> listFlagColor;

    public static int widthRect;

    public static int heightRect;

    public static List<Char> listCharsInMap;

    public static bool isUsingSkill;

    public static long lastTimeConnected;

    public static bool isUsingCapsule;

    public static string linkFb;

    public static int delay;

    public static int indexBackgroundImages;

    public static long lastTimeChangeBackground;

    public static int server;

    public static string APIKey;

    public static string APIServer;


    public static bool isAutoT77;

    public static bool isAutoBomPicPoc;

    private static bool thongBao;

    public static int time_;

    public static bool toiUuCPU;

    public static long GoldCurrent;

    public static bool infoTrainGold;

    public static bool infoTrainPet;

    public static long GoldUpdate;

    public static long GoldUpdateRealTime;

    public static string VersionMod;

    private static int frameCount;

    private static long lastTime;

    private static int fps;

    private static int stopMap;

    private static bool checkSkill;

    public static bool blockAutoGame;

    public static bool AutoCapCha;

    public static bool configStartGame;

    private static readonly Dictionary<int, long> lastPower;

    private static readonly Dictionary<int, long> lastUpdateTime;

    public static long basePetPower;

    public static bool basePowerSet;

    public static long lastPetInfoCallTime;

    public static string[] inputAutoLoginOffline;

    public static MainMod getInstance()
    {
        if (_Instance == null)
        {
            _Instance = new MainMod();
        }
        return _Instance;
    }

    public static void Update()
    {
        DataItem.HandleInput();
        SocketGame.ProcessMessages();
        AutoPean.Update();
        ModProCL.update();
        MainXmapCL.Update();
        AutoQuestCL.Update();
        if (!blockAutoGame)
        {
            AutoboMongCL.update();
            AutoTrainCL.Update();
            AutoBossCL.Update();
            AutoBuyItemCL.update();
            YardatCL.update();
            AutoPetCL.update();
            NhapCodeLive.getInstance().update();
        }
        ShowSetKH.update();
        AutoVutDoCL.update();
        if (thongBao && GameCanvas.gameTick % 700 == 0)
        {
            GameScr.gI().chatVip(thongbaoVIPne);
            thongBao = false;
        }

        if (AutoCapCha)
        {
            CaptchaSolver.Update();
        }

        if (GraphicsManagement.isShowCharsInMap)
        {
            listCharsInMap.Clear();
            for (int i = 0; i < GameScr.vCharInMap.size(); i++)
            {
                Char obj = (Char)GameScr.vCharInMap.elementAt(i);
                if (obj.cName != null && obj.cName != "" && !obj.isPet && !obj.isMiniPet && !obj.cName.StartsWith("#") && !obj.cName.StartsWith("$") && obj.cName != "Trọng tài")
                {
                    listCharsInMap.Add(obj);
                }
            }
        }
        if (isAutoEnterNRDMap)
        {
            EnterNRDMap();
        }
        if (isAutoRevive)
        {
            Revive();
        }
        if (isLockFocus)
        {
            FocusTo(charIDLock);
        }
        AutoItem.Update();
        AutoChat.Update();
        AutoSkill.Update();
        AutoPick.Update();
        AutoPoint.Update();
        Char.myCharz().cspeed = runSpeed;
    }

    public static int CalculateFPS()
    {
        frameCount++;
        long num = mSystem.currentTimeMillis();
        long num2 = num - lastTime;
        if (num2 >= 500)
        {
            fps = (int)((double)frameCount * 1000.0 / (double)num2 + 0.5);
            frameCount = 0;
            lastTime = num;
        }
        return fps;
    }

    private static void PaintFocus(mGraphics g)
    {
        string text = null;
        if (Char.myCharz().mobFocus != null)
        {
            text = Char.myCharz().mobFocus.getTemplate().name + " [" + Res.FormatNumberVIP(Char.myCharz().mobFocus.hp) + "/" + Res.FormatNumberVIP(Char.myCharz().mobFocus.maxHp) + "]";
        }
        else if (Char.myCharz().charFocus != null && isBoss(Char.myCharz().charFocus))
        {
            text = Char.myCharz().charFocus.cName + " [" + Res.FormatNumberVIP(Char.myCharz().charFocus.cHP) + "/" + Res.FormatNumberVIP(Char.myCharz().charFocus.cHPFull) + "]";
        }
        if (!string.IsNullOrEmpty(text))
        {
            int x = GameCanvas.w / 2;
            int y = GameScr.logoInGame.getHeight() + 10;
            mFont.tahoma_7_yellow.drawString(g, text, x, y, mFont.CENTER, mFont.tahoma_7_grey);
        }
    }

    public static void Paint(mGraphics g)
    {
        DataItem.Paint(g);
        if (DataItem.IsShow)
        {
            return;
        }
        bool flag = isMeInNRDMap();
        GraphicsManagement.Paint(g);
        if (MainMenu.isShowMenuVIP)
        {
            MainMenu.Paint(g);
            return;
        }
        if (GameScr.logoInGame != null && GraphicsManagement.HienThiLogo)
        {
            g.drawImage(GameScr.logoInGame, GameCanvas.w / 2, 1, 1);
        }
        paintListBosses(g);
        if (GraphicsManagement.isShowCharsInMap)
        {
            paintListCharsInMap(g);
        }
        if (!flag)
        {
            PaintFocus(g);
        }
        int num = 8;
        int num2 = GameCanvas.h - 197;
        GraphicsManagement.DrawFont.drawString(g, TileMap.mapNames[TileMap.mapID] + " [" + TileMap.mapID + "] Zone: " + TileMap.zoneID, 10, num2, 0);
        num2 += num;
        GraphicsManagement.DrawFont.drawString(g, "x: " + Char.myCharz().cx + " y: " + Char.myCharz().cy, 10, num2, 0);
        num2 += num;
        string st = string.Format("[{0}] {1}", CalculateFPS(), SocketGame.IsRunning ? "QLTK: OK" : "QLTK: Đéo");
        int width = GameScr.imgPanel.getWidth();
        int height = GameScr.imgPanel.getHeight();
        GraphicsManagement.DrawFont.drawString(g, st, width / 2 + width * 5 / 100, height - height * 30 / 100, 0);
        if (AutoBossCL.DoBoss)
        {
            GraphicsManagement.DrawFont.drawString(g, "Bắt đầu tìm khu boss từ " + TileMap.zoneID + "->" + AutoBossCL.CountZoneMap, 10, num2, 0);
            num2 += num;
        }
        if (AutoSkill.isAutoSendAttack)
        {
            GraphicsManagement.DrawFont.drawString(g, "Tự đánh: on", 10, num2, 0);
            num2 += num;
        }
        if (isAutoRevive)
        {
            GraphicsManagement.DrawFont.drawString(g, "Hồi sinh: on", 10, num2, 0);
            num2 += num;
        }
        if (AutoPetCL.DeSuaLapem)
        {
            GraphicsManagement.DrawFont.drawString(g, $"Đệ kêu: {AutoPetCL.soLanDeKeu} lần | đã đấm: {AutoPetCL.soLanTanCong} lần", 10, num2, 0);
            num2 += num;
        }
        if (AutoPetCL.TTNL)
        {
            GraphicsManagement.DrawFont.drawString(g, $"Tự động TTNL khi HP dưới {AutoPetCL.PercentCharge}%", 10, num2, 0);
            num2 += num;
        }
        if (AutoPetCL.AutoNhatItemPet)
        {
            GraphicsManagement.DrawFont.drawString(g, $"Số item nhặt từ đệ: {AutoPetCL.SoItemNhatTuPet}", 10, num2, 0);
            num2 += num;
        }

        if (CaptchaSolver.IsSolving())
        {
            GraphicsManagement.DrawFont.drawString(g, CaptchaSolver.GetStatus(), 10, num2, 0);
            num2 += num;
            GraphicsManagement.DrawFont.drawString(g, "Số lần giải Capcha thành công: " + CaptchaSolver.GetSolvedCount(), 10, num2, 0);
            num2 += num;
        }

        if (AutoFarmBossNappa.DoSatBossNapa)
        {
            string text = ((AutoFarmBossNappa.typeBoss == 0) ? "Kuku" : ((AutoFarmBossNappa.typeBoss == 1) ? "Mập" : "Rambo"));
            GraphicsManagement.DrawFont.drawString(g, "Auto Farm Nappa: " + text + "- " + AutoFarmBossNappa.statusBossNappa, 10, num2, 0);
            num2 += num;
        }
        if (AutoBossCL.findBossMod)
        {
            GraphicsManagement.DrawFont.drawString(g, "Auto Map Trứng Mabu: on", 10, num2, 0);
            num2 += num;
        }
        if (AutoPick.isAutoPick)
        {
            GraphicsManagement.DrawFont.drawString(g, "Auto nhặt: on", 10, num2, 0);
            num2 += num;
        }
        if (AutoBossCL.aGimBoss)
        {
            GraphicsManagement.DrawFont.drawString(g, "Auto gim Boss:  on", 10, num2, 0);
            num2 += num;
        }
        if (AutoBossCL.AutoteleBoss)
        {
            GraphicsManagement.DrawFont.drawString(g, "Auto tele Boss:  on", 10, num2, 0);
            num2 += num;
        }
        if (AutoBossCL.tanCongBoss)
        {
            GraphicsManagement.DrawFont.drawString(g, "Auto tan cong Boss:  on", 10, num2, 0);
            num2 += num;
        }
        if (AutoboMongCL.autoboMong)
        {
            string difficulty = char.ToUpper(AutoboMongCL.Settings.Difficulty[0]) + AutoboMongCL.Settings.Difficulty.Substring(1);
            string statusText = "Bò Mộng: " + difficulty + "-" + AutoboMongCL.StatusBoMong + " [Hoàn thành: " + AutoboMongCL.completedTasks + " | Hủy: " + AutoboMongCL.cancekTasks + "]";
            GraphicsManagement.DrawFont.drawString(g, statusText, 10, num2, 0);
            num2 += num;
        }
        if (isLockFocus)
        {
            GraphicsManagement.DrawFont.drawString(g, "Khóa: " + charIDLock, 10, num2, 0);
            num2 += num;
        }
        if (isAutoEnterNRDMap)
        {
            GraphicsManagement.DrawFont.drawString(g, "Đang auto nrd: " + mapIdNRD + "sk" + zoneIdNRD, 15, num2, 0);
            num2 += num;
        }
        if (AutoPetCL.aGimPet)
        {
            GraphicsManagement.DrawFont.drawString(g, "Auto Gim Đệ: " + (AutoPetCL.aGimPet ? "Bật" : "Tắt"), 10, num2, 0);
            num2 += num;
        }
        if (!MainXmapCL.isXmaping)
        {
            if (ModProCL.petw)
            {
                infoCharView(g, Char.myPetz(), 10, num2);
                num2 += num * 5;
            }
            if (infoTrainGold)
            {
                infoTrain(g, 10, num2);
                num2 += num * 3;
            }
        }
        if (ModProCL.hienThiDoKH)
        {
            ShowSetKH.paintDOKH(10, num2, g);
            num2 += num;
        }
        if (ModProCL.banDo)
        {
            string sellStatus = "Đang auto bán đồ rác | Đã bán: " + ShowSetKH.GetSellCount() + " cái";
            GraphicsManagement.DrawFont.drawString(g, sellStatus, 10, num2, 0);
            num2 += num;
        }
        if (ModProCL.catDoVIP)
        {
            string storeStatus;
            if (ModProCL.isFULLBox())
            {
                storeStatus = "Rương FULL rồi | Đã cất: " + ShowSetKH.GetStoreCount() + " cái";
            }
            else
            {
                storeStatus = "Đang auto cất đồ (TL+KH) vào rương | Đã cất: " + ShowSetKH.GetStoreCount() + " cái";
            }
            GraphicsManagement.DrawFont.drawString(g, storeStatus, 10, num2, 0);
            num2 += num;
        }
        if (MainXmapCL.isXmaping)
        {
            int width2 = GameScr.imgLbtn.getWidth();
            int height2 = GameScr.imgLbtn.getHeight();
            int num3 = GameCanvas.w / 2 - width2 / 2;
            int num4 = GameCanvas.h / 2 - height2 / 2;
            g.drawImage((stopMap != 1) ? GameScr.imgLbtn : GameScr.imgLbtnFocus, num3, num4);
            int x = num3 + width2 / 2;
            int y = num4 + height2 / 2 - 3;
            mFont.tahoma_7b_dark.drawString(g, "Stop Xmap", x, y, mFont.CENTER);
            string st2 = "Đang tới: " + TileMap.mapNames[MainXmapCL.IdMapEnd];
            if (MainXmapCL.xmapErrr)
            {
                st2 = "Bạn chưa thể tới: " + TileMap.mapNames[MainXmapCL.IdMapEnd];
            }
            GraphicsManagement.DrawFont.drawString(g, st2, x, num4 - 12, mFont.CENTER);
        }
        AutoBuyItemCL.Paint(g, ref num2, num);
    }

    public static void paintListCharsInMap(mGraphics g)
    {
        bool flag = isMeInNRDMap();
        int num = (flag ? 35 : 88);
        widthRect = 120;
        heightRect = 7;
        GUIStyle other = new GUIStyle
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf"),
            fontSize = 7 * mGraphics.zoomLevel,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        List<string> list = new List<string>();
        if (Char.myCharz().holder)
        {
            if (Char.myCharz().charHold != null)
            {
                list.Add("Bạn đang trói " + Char.myCharz().charHold.cName);
            }
            else if (Char.myCharz().mobHold != null)
            {
                list.Add("Bạn đang trói " + Char.myCharz().mobHold.getTemplate().name);
            }
        }
        for (int i = 0; i < listCharsInMap.Count; i++)
        {
            Char obj = listCharsInMap[i];
            if (obj.cName == null || obj.cName == "" || obj.isPet || obj.isMiniPet || obj.cName.StartsWith("#") || obj.cName.StartsWith("$") || obj.cName == "Trọng tài")
            {
                continue;
            }
            g.setColor(2721889, 0.5f);
            g.fillRect(GameCanvas.w - widthRect, num + 1, widthRect - 2, heightRect);
            paintPKFlag(g, obj, num);
            if (obj.isNRD)
            {
                paintCharNRD(g, obj);
            }
            string text = obj.cName + " [" + NinjaUtil.getMoneys(obj.cHP) + "]";
            bool flag2 = isBoss(obj);
            if (!flag2)
            {
                text = obj.cName + " [" + NinjaUtil.getMoneys(obj.cHP) + " - " + obj.getGender() + "]";
            }
            GUIStyle gUIStyle = new GUIStyle(other);
            if (Char.myCharz().charFocus != null && Char.myCharz().charFocus.cName == obj.cName)
            {
                g.setColor(14155776);
                g.drawLine(Char.myCharz().cx - GameScr.cmx, Char.myCharz().cy - GameScr.cmy + 1, obj.cx - GameScr.cmx, obj.cy - GameScr.cmy);
                gUIStyle.normal.textColor = Color.red;
            }
            else if (flag2)
            {
                g.setColor(16383818);
                g.drawLine(Char.myCharz().cx - GameScr.cmx, Char.myCharz().cy - GameScr.cmy + 1, obj.cx - GameScr.cmx, obj.cy - GameScr.cmy);
                gUIStyle.normal.textColor = Color.yellow;
            }
            else if (obj.cHPFull > 100000000 && obj.cHP > 0 && flag && !obj.isNRD)
            {
                gUIStyle.normal.textColor = Color.magenta;
            }
            else
            {
                gUIStyle.normal.textColor = Color.black;
            }
            g.drawString(i + 1 + ". " + text, GameCanvas.w - widthRect + 2, num, gUIStyle);
            num += heightRect + 1;
            if (obj.holder)
            {
                if (obj.charHold == Char.myCharz())
                {
                    list.Add(obj.cName + " đang trói bạn");
                }
                else if (obj.charHold != null)
                {
                    list.Add(obj.cName + " đang trói " + obj.charHold.cName);
                }
                else if (obj.mobHold != null)
                {
                    list.Add(obj.cName + " đang trói " + obj.mobHold.getTemplate().name);
                }
            }
        }
        int height = GameScr.logoInGame.getHeight();
        int num2 = ((!flag) ? (height + 20) : (height + 30));
        foreach (string item in list)
        {
            mFont.tahoma_7b_dark.drawString(g, item, GameCanvas.w / 2, num2, mFont.CENTER);
            num2 += 9;
        }
    }

    public static void paintCharNRD(mGraphics g, Char ch)
    {
        int num = GameScr.logoInGame.getHeight() + 20;
        int num2 = 9;
        string text = ch.cName + " [" + NinjaUtil.getMoneys(ch.cHP) + "/" + NinjaUtil.getMoneys(ch.cHPFull) + "]";
        if (ch.isNRD)
        {
            text = text + " - Còn: " + ch.timeNRD + " giây";
        }
        if (ch.isFreez)
        {
            text = text + " - Bị TDHS: " + ch.freezSeconds + " giây";
        }
        mFont.tahoma_7b_red.drawString(g, text, GameCanvas.w / 2, num, mFont.CENTER);
        num += num2;
    }

    public static void paintListBosses(mGraphics g)
    {
        if (GraphicsManagement.isHuntingBoss && !isMeInNRDMap())
        {
            int num = 42;
            for (int i = 0; i < listBosses.Count; i++)
            {
                listBosses[i].Paint(g, GameCanvas.w - 2 - mFont.tahoma_7_white.getWidth(" [Go]"), num, mFont.RIGHT);
                num += 8;
            }
        }
    }
