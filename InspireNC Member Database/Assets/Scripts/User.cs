using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class User : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nameText, classificationText;

    [HideInInspector]
    public string memberName, userID;

    public void SetName(string name)
    {
        nameText.text = name;
    }

    public void SetClassification(string classification)
    {
        classificationText.text = classification;
    }

    public void OnClick()
    {
        GameObject.FindGameObjectWithTag("Main Screen").GetComponent<MainScreenManager>().UserButton(memberName, userID);
    }
}
