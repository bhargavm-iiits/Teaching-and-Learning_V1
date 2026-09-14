using System;
using System.Collections.Generic;
using UnityEngine;
namespace ZeroReturn
{
    public class ProgressionService
    {
        [Serializable] class Save { public int attempt; public List<string> seen = new List<string>(); public bool complete; }
        Save data;
        string key;
        public int Attempt => data.attempt;
        public bool Complete => data.complete;
        public HashSet<string> Seen => new HashSet<string>(data.seen);
        public string PlayerId { get; private set; }
        public ProgressionService(string concept)
        {
            PlayerId = PlayerPrefs.GetString("ZeroReturn.Player", "");
            if (string.IsNullOrEmpty(PlayerId)) { PlayerId = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString("ZeroReturn.Player", PlayerId); }
            key = "ZeroReturn." + concept;
            try { data = JsonUtility.FromJson<Save>(PlayerPrefs.GetString(key, "")); } catch { data = null; }
            if (data == null) data = new Save();
        }
        public void SaveSeen(HashSet<string> seen) { data.seen = new List<string>(seen); SaveNow(); }
        public void Fail() { data.attempt++; SaveNow(); }
        public void MarkComplete() { data.complete = true; SaveNow(); }
        public void NewPractice() { data.attempt++; SaveNow(); }
        void SaveNow() { PlayerPrefs.SetString(key, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
    }
}
