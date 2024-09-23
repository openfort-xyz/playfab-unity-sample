using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Openfort.OpenfortSDK;
using Openfort.OpenfortSDK.Model;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class OpenfortController : MonoBehaviour
{
    [System.Serializable]
    public class SavePlayerDataResponse
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
        public string userOpHash;
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

    public UnityEvent savePlayerDataErrorEvent;
    public GameObject uiCanvas;
    public GameObject mintPanel;
    public NftPrefab nftPrefab;
    public TextMeshProUGUI statusText;
    
    private string _playerId;
    private string _playerWalletAddress;
    
    public static OpenfortController Instance { get; private set; }
    
    private const string PublishableKey = "pk_test_b3dace8a-6d2b-5163-90e2-2a40065a3803";

    private OpenfortSDK openfort;

    [HideInInspector] public string oauthAccessToken;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start() {
        openfort = await OpenfortSDK.Init(PublishableKey);

        Debug.Log(openfort);
    }

    public async void Authenticate(string idToken)
    {
        Debug.Log("PlayFab session ticket: " + idToken);
        
        var request = new ThirdPartyOAuthRequest(
            ThirdPartyOAuthProvider.Playfab,
            idToken,
            TokenType.IdToken
        );
        
        try
        {
            await openfort.AuthenticateWithThirdPartyProvider(request);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }

        /* 
        mOpenfort = new OpenfortSDK(PublishableKey); 
        oauthAccessToken = await mOpenfort.AuthenticateWithOAuth(OAuthProvider.Playfab, idToken);
        Debug.Log("Access Token: " + oauthAccessToken);
        
        
        try
        {
            mOpenfort.ConfigureEmbeddedSigner(80001);
        }
        catch (MissingRecoveryMethod)
        {
            await mOpenfort.ConfigureEmbeddedRecovery(new PasswordRecovery("secret"));
        }
        */
        
        // TODO SavePlayerData();
    }

    #region AZURE_FUNCTION_CALLERS

    private void SavePlayerData()
    {
        if (string.IsNullOrEmpty(oauthAccessToken))
        {
            Debug.LogError("OAuth access token is null or empty.");
            return;
        }

        statusText.text = "Saving player data...";

        var request = new ExecuteFunctionRequest
        {
            FunctionName = "SavePlayerData",
            FunctionParameter = new
            {
                accessToken = oauthAccessToken,
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnSavePlayerDataSuccess, OnSavePlayerDataError);
    }
    
    public void PlayFabAuth_OnLoginSuccess_Handler(LoginResult loginResult)
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
                // Get sessionTicket from LoginResult (PlayFab LoginResult)
                var sessionTicket = loginResult.SessionTicket;
                Authenticate(sessionTicket);
            }
        }, error =>
        {
            statusText.text = "Login error. Please retry.";
            savePlayerDataErrorEvent?.Invoke();
        });
    }

    public void MintNFT()
    {
        if (string.IsNullOrEmpty(oauthAccessToken))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        statusText.text = "Minting NFT...";

        var request = new ExecuteFunctionRequest
        {
            FunctionName = "MintNFT",
            FunctionParameter = new
            {
                accessToken = oauthAccessToken
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
    private void OnSavePlayerDataSuccess(ExecuteFunctionResult result)
    {
        statusText.text = "Player data saved!";

        string json = result.FunctionResult.ToString();
        SavePlayerDataResponse dataResponse = JsonUtility.FromJson<SavePlayerDataResponse>(json);

        Debug.Log($"Player ID: {dataResponse.playerId}, Player Wallet Address: {dataResponse.playerWalletAddress}");
        _playerId = dataResponse.playerId;
        _playerWalletAddress = dataResponse.playerWalletAddress;

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

    private async void OnGetTransactionIntentSuccess(ExecuteFunctionResult result)
    {
        var responseObject = JsonUtility.FromJson<GetTransactionIntentResponse>(result.FunctionResult.ToString());
        if (responseObject.minted)
        {
            statusText.text = "Transaction signed. Fetching inventory...";
            Debug.Log("Minted is true");

            UniTask.Delay(2000);
            GetPlayerNftInventory(_playerId);
        }
        else
        {
            Debug.Log("Minted is false");

            var txId = responseObject.id;
            var userOpHash = responseObject.userOpHash;

            if (string.IsNullOrEmpty(userOpHash))
            {
                Debug.LogWarning("userOpHash is null.");
                GetTransactionIntent(responseObject.id);
                return;
            }
            
            Debug.Log($"userOpHash: {userOpHash}");

            try
            {
                // TODO
                /*
                var intentResponse = await mOpenfort.SendSignatureTransactionIntentRequest(txId, userOpHash);
                var transactionHash = intentResponse.Response.TransactionHash;

                Debug.Log($"Transaction: {transactionHash} signed!");
                statusText.text = "Transaction signed. Fetching inventory...";
                
                */ 
                GetTransactionIntent(txId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    private void OnGetPlayerNftInventorySuccess(ExecuteFunctionResult result)
    {
        statusText.text = "NFT inventory retrieved!";
        Debug.Log(result.FunctionResult);
        var json = result.FunctionResult.ToString();
        List<NftItem> nftItems = JsonConvert.DeserializeObject<List<NftItem>>(json);

        if (nftItems.Count == 0)
        {
            statusText.text = "NFT inventory is empty.";
            mintPanel.SetActive(true);
        }
        else
        {
            foreach (var nft in nftItems)
            {
                //TODO instantiate into scroll grid layout in case more than 1 nft.
                var instantiatedNft = Instantiate(nftPrefab, uiCanvas.transform);
                instantiatedNft.Setup(nft.assetType, nft.tokenId.ToString());
                Debug.Log(nft);
            }   
        }
    }

    #endregion

    #region ERROR_CALLBACK_HANDLERS

    private void OnSavePlayerDataError(PlayFabError error)
    {
        statusText.text = "Error saving player data.";
        Debug.LogError($"Failed to save player data: {error.GenerateErrorReport()}");
        savePlayerDataErrorEvent?.Invoke();
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