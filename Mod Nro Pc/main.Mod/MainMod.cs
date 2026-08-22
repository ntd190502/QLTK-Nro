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