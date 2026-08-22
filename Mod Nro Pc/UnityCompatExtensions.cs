using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class UnityCompatExtensions
{
    public static byte[] EncodeToPNG(this Texture2D texture)
    {
        if (texture == null) return null;

        Type imageConversion = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", false)
            ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine", false);

        if (imageConversion != null)
        {
            MethodInfo method = imageConversion.GetMethod(
                "EncodeToPNG",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Texture2D) },
                null);

            if (method != null)
                return (byte[])method.Invoke(null, new object[] { texture });
        }

        // Some Unity players expose EncodeToPNG as an actual Texture2D instance method.
        MethodInfo instanceMethod = texture.GetType().GetMethod(
            "EncodeToPNG",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        if (instanceMethod != null)
            return (byte[])instanceMethod.Invoke(texture, null);

        throw new MissingMethodException("Unity Texture2D PNG encoding API is unavailable in this player.");
    }
}

public static class AutoQuestBuildCompat
{
    public static List<int> GetCurrentMapMobList()
    {
        int mapId = TileMap.mapID;
        if (!Mod.CuongLe.AutoTrainCL.listMobIds.ContainsKey(mapId))
            Mod.CuongLe.AutoTrainCL.listMobIds[mapId] = new List<int>();

        return Mod.CuongLe.AutoTrainCL.listMobIds[mapId];
    }
}
