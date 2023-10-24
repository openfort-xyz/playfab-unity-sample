using System.Collections.Generic;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using EntityKey = PlayFab.CloudScriptModels.EntityKey;

public class OpenfortController : MonoBehaviour
{
    [System.Serializable]
    public class CreatePlayerResponse
    {
        public string playerId;
        public string playerWalletAddress;
    }
    
    [System.Serializable]
    public class FindTransactionIntentResponse
    {
        public string id;
    }
    
    [System.Serializable]
    private class GetTransactionIntentResponse
    {
        public bool minted;
        public string id;
    }
    
    [System.Serializable]
    public class NftItemList
    {
        public NftItem[] items;
    }
    
    [System.Serializable]
    public class NftItem
    {
        public string assetType;
        public string amount;
        public int tokenId;
        public string address;
        public long lastTransferredAt;
    }

    public UnityEvent OnCreatePlayerErrorEvent;
    public GameObject uiCanvas;
    public GameObject mintPanel;
    public NftPrefab nftPrefab;
    public TextMeshProUGUI statusText;
    
    private string _playerId;
    private string _playerWalletAddress;

    #region AZURE_FUNCTION_CALLERS

    public void PlayFabAuth_OnLoginSuccess_Handler()
    {
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserReadOnlyData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey("OpenfortPlayerId") &&
                result.Data.ContainsKey("PlayerWalletAddress"))
            {
                _playerId = result.Data["OpenfortPlayerId"].Value;
                _playerWalletAddress = result.Data["PlayerWalletAddress"].Value;
                GetPlayerNftInventory(_playerId);
            }
            else
            {
                CreatePlayer();
            }
        }, error =>
        {
            statusText.text = "Login error. Please retry.";
            OnCreatePlayerErrorEvent?.Invoke();
        });
    }
    
    private void CreatePlayer()
    {
        statusText.text = "Creating player...";

        var request = new ExecuteFunctionRequest()
        {
            Entity = new EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType
            },
            FunctionName = "CreateOpenfortPlayer",
            GeneratePlayStreamEvent = true,
        };

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnCreatePlayerSuccess, OnCreatePlayerError);
    }

    public void MintNFT()
    {
        if (string.IsNullOrEmpty(_playerId) || string.IsNullOrEmpty(_playerWalletAddress))
        {
            Debug.LogError("Player ID or Player Wallet Address is null or empty.");
            return;
        }

        statusText.text = "Minting NFT...";

        var request = new ExecuteFunctionRequest
        {
            FunctionName = "MintNFT",
            FunctionParameter = new
            {
                playerId = _playerId,
                receiverAddress = _playerWalletAddress
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnMintNftSuccess, OnMintNftError);
    }

    public void FindTransactionIntent()
    {
        if (string.IsNullOrEmpty(_playerId) || string.IsNullOrEmpty(_playerWalletAddress))
        {
            Debug.LogError("Player ID or Player Wallet Address is null or empty.");
            return;
        }

        statusText.text = "Searching for transaction intent...";

        var request = new ExecuteFunctionRequest
        {
            FunctionName = "FindTransactionIntent",
            FunctionParameter = new
            {
                playerId = _playerId,
                receiverAddress = _playerWalletAddress
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnFindTransactionIntentSuccess, OnGeneralError);
    }

    private void GetTransactionIntent(string transactionIntentId)
    {
        statusText.text = "Getting transaction intent...";

        var request = new ExecuteFunctionRequest
        {
            FunctionName = "GetTransactionIntent",
            FunctionParameter = new
            {
                transactionIntentId
            }
        };

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnGetTransactionIntentSuccess, OnGeneralError);
    }

    private void GetPlayerNftInventory(string playerId)
    {
        statusText.text = "Fetching NFT inventory...";

        var request = new ExecuteFunctionRequest
        {
            FunctionName = "GetPlayerNftInventory",
            FunctionParameter = new
            {
                playerId
            }
        };

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnGetPlayerNftInventorySuccess, OnGeneralError);
    }

    #endregion

    #region SUCCESS_CALLBACK_HANDLERS

    private void OnCreatePlayerSuccess(ExecuteFunctionResult result)
    {
        statusText.text = "Player created successfully!";

        string json = result.FunctionResult.ToString();
        CreatePlayerResponse response = JsonUtility.FromJson<CreatePlayerResponse>(json);

        Debug.Log($"Player ID: {response.playerId}, Player Wallet Address: {response.playerWalletAddress}");
        _playerId = response.playerId;
        _playerWalletAddress = response.playerWalletAddress;

        mintPanel.SetActive(true);
    }

    private void OnMintNftSuccess(ExecuteFunctionResult result)
    {
        statusText.text = "NFT minted successfully!";
        Debug.Log("minted = true");
        GetPlayerNftInventory(_playerId);
    }

    private void OnFindTransactionIntentSuccess(ExecuteFunctionResult result)
    {
        statusText.text = "Transaction intent found!";
        Debug.Log(result.FunctionResult);
        var json = result.FunctionResult.ToString();
        var responseObject = JsonUtility.FromJson<FindTransactionIntentResponse>(json);

        GetTransactionIntent(responseObject.id);
    }

    private void OnGetTransactionIntentSuccess(ExecuteFunctionResult result)
    {
        var responseObject = JsonUtility.FromJson<GetTransactionIntentResponse>(result.FunctionResult.ToString());
        if (responseObject.minted)
        {
            statusText.text = "Transaction confirmed. Minted = true";
            Debug.Log("Minted is true");
            GetPlayerNftInventory(_playerId);
        }
        else
        {
            statusText.text = "Transaction pending. Minted = false";
            Debug.Log("Minted is false");
            GetTransactionIntent(responseObject.id);
        }
    }

    private void OnGetPlayerNftInventorySuccess(ExecuteFunctionResult result)
    {
        statusText.text = "NFT inventory retrieved!";
        Debug.Log(result.FunctionResult);
        var json = result.FunctionResult.ToString();
        List<NftItem> nftItems = JsonConvert.DeserializeObject<List<NftItem>>(json);

        foreach (var nft in nftItems)
        {
            //TODO instantiate into scroll grid layout in case more than 1 nft.
            var instantiatedNft = Instantiate(nftPrefab, uiCanvas.transform);
            instantiatedNft.Setup(nft.assetType, nft.tokenId.ToString());
            Debug.Log(nft);
        }
    }

    #endregion

    #region ERROR_CALLBACK_HANDLERS

    private void OnCreatePlayerError(PlayFabError error)
    {
        statusText.text = "Error creating player!";
        Debug.LogError($"Failed to call CreateOpenfortPlayer: {error.GenerateErrorReport()}");
        OnCreatePlayerErrorEvent?.Invoke();
    }

    private void OnMintNftError(PlayFabError error)
    {
        Debug.Log(error);
        if (error.GenerateErrorReport().Contains("10000ms"))
        {
            statusText.text = "Timeout during NFT minting. Checking transaction...";
            FindTransactionIntent();
        }
        else
        {
            statusText.text = "Error minting NFT!";
            mintPanel.SetActive(true);
            Debug.LogWarning(error.GenerateErrorReport());
        }
    }

    private void OnGeneralError(PlayFabError error)
    {
        statusText.text = "An error occurred!";
        mintPanel.SetActive(true);
        Debug.LogWarning(error.GenerateErrorReport());
    }

    #endregion
}