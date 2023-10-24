using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NftPrefab : MonoBehaviour
{
    public TextMeshProUGUI assetTypeText;
    public TextMeshProUGUI tokenIdText;
    
    public void Setup(string assetType, string tokenId)
    {
        assetTypeText.text = "Asset type: " + assetType;
        tokenIdText.text = "Token ID: " + tokenId;
    }
}
