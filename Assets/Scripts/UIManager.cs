using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GGJ
{
    public class UIManager : MonoSingleton<UIManager>
    {
        public List<PlayerUI> PlayerUIList;
        public Button ReloadBtn;

        public void InitPlayer(PlayerController pc, int idx)
        {
            if (idx < 0 || idx >= PlayerUIList.Count || PlayerUIList[idx] == null) Debug.LogError("PlayerUI列表不足");
            PlayerUIList[idx].Init(pc);
        }
        
        protected override void Awake()
        {
            ReloadBtn.onClick.AddListener(() => GameManager.Instance.Reload());
        }
    }
}