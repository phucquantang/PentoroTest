using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    public void LoadGameplayScene(string mode)
    {
        AudioManager.Instance.PlaySFX("Pop");
        GameManager.Instance.LoadGameplayScene(mode);
    }
}
