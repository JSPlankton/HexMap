using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SaveLoadItem : MonoBehaviour
{
    public SaveLoadMenu menu;
	
    public string MapName {
        get {
            return mapName;
        }
        set {
            mapName = value;
            transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = value;
        }
    }
	
    string mapName;
	
    public void Select () {
        menu.SelectItem(mapName);
    }
    
}
