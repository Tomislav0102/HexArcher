using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class PlayerLeveling
{
    static int[] xpMilestones = new int[] { 0, 250, 300, 500, 800, 1000, 1500, 2000, 2500, 3000 };

    public static void CalculateLevelFromXp(out int lv, out int toNext)
    {
        int xp = PlayerPrefs.GetInt(Utils.PlXp_Int);

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

    public static void AddToXp(GenFinish finishType)
    {
        float diffMod = Mod(PlayerPrefs.GetInt(Utils.Difficulty_Int));
        float sizeMod = Mod(PlayerPrefs.GetInt(Utils.Size_Int));
        
        Vector3Int xpWinDrawLoseSp = new Vector3Int(100, 50, 50);
        Vector3Int xpWinDrawLoseMp = new Vector3Int(300, 100, 100);
        Vector3Int spOrMp = Utils.GameType == MainGameType.Singleplayer ? xpWinDrawLoseSp : xpWinDrawLoseMp;
        
        int wld = 0;
        switch (finishType)
        {
            case GenFinish.Win:
                wld = spOrMp.x;
                break;
            case GenFinish.Lose:
                wld = spOrMp.y;
                break;
            case GenFinish.Draw:
                wld = spOrMp.z;
                break;
        }
        
        int final = PlayerPrefs.GetInt(Utils.PlXp_Int) + (int)(diffMod * sizeMod * wld);
        PlayerPrefs.SetInt(Utils.PlXp_Int, final);
        Utils.PlayerXpUpdated?.Invoke();
        
        float Mod(int rank) => 1 + rank * 0.5f;
    }
    
    #region DEBUG
    public static void GetMeToLevel(int targetLevel)
    {
        CalculateLevelFromXp(out int lv, out int toNext);
        if(targetLevel <= lv || targetLevel > 10) return;
        PlayerPrefs.SetInt(Utils.PlXp_Int, xpMilestones[targetLevel - 1]);
    }
    #endregion
}