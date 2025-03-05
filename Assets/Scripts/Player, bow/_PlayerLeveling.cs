using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class PlayerLeveling
{
    static int[] xpMilestones = new int[] { 0, 250, 300, 500, 800, 1000, 1500, 2000, 2500, 3000 };

    public static void CalculateLevelFromXp(out int lv, out int toNext)
    {
        DatabaseManager dm = Launch.Instance.myDatabaseManager;
        int xp = dm.GetValFromKeyEnum<int>(MyData.Xp);

        for (int i = 0; i < xpMilestones.Length; i++)
        {
            if (xp < xpMilestones[i])
            {
                lv = i;
                toNext = xpMilestones[i] - xp;
                return;
            }
        }
        lv =  xpMilestones.Length;
        toNext = 0;
    }

    public static void AddToXp(GenResult resultType)
    {
        float diffMod = Mod(PlayerPrefs.GetInt(Utils.Difficulty_Int));
        float sizeMod = Mod(PlayerPrefs.GetInt(Utils.Size_Int));
        
        Vector3Int xpWinDrawLoseSp = new Vector3Int(100, 50, 50);
        Vector3Int xpWinDrawLoseMp = new Vector3Int(300, 100, 100);
        Vector3Int spOrMp = Utils.GameType == MainGameType.Singleplayer ? xpWinDrawLoseSp : xpWinDrawLoseMp;
        
        int wld = 0;
        switch (resultType)
        {
            case GenResult.Win:
                wld = spOrMp.x;
                break;
            case GenResult.Lose:
                wld = spOrMp.y;
                break;
            case GenResult.Draw:
                wld = spOrMp.z;
                break;
        }
        
        DatabaseManager dm = Launch.Instance.myDatabaseManager;
        int final = dm.GetValFromKeyEnum<int>(MyData.Xp) + (int)(diffMod * sizeMod * wld);
        dm.myData[MyData.Xp] = final.ToString();
        Utils.PlayerXpUpdated?.Invoke();
        
        float Mod(int rank) => 1 + rank * 0.5f;
    }
    
    #region DEBUG
    public static void GetMeToLevel(int targetLevel)
    {
        CalculateLevelFromXp(out int lv, out int toNext);
        if(targetLevel <= lv || targetLevel > 10) return;
        Launch.Instance.myDatabaseManager.myData[MyData.Xp] = xpMilestones[targetLevel - 1].ToString();
    }
    #endregion
}