using System;
using System.Collections.Generic;
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
    
    private const string PubApiKey = "pk_test_b3dace8a-6d2b-5163-90e2-2a40065a3803";
    private const string PubShieldKey = "7e6ba9bc-fac0-434f-8cec-48445e22e225";

    // TODO: Get ShieldEncryptShare from backend
    string ShieldEncryptShare = "AhpS06In9jYDJHuubFP+uArrmK2lmb4MwZtaWDQXLhOX";

    private OpenfortSDK openfort;

    [HideInInspector] public string oauthAccessToken;
    
    #region UNITY_CALLBACKS

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
        // Initialize Openfort SDK
        statusText.text = "Initializing Openfort SDK...";
        openfort = await OpenfortSDK.Init(PubApiKey, PubShieldKey, ShieldEncryptShare);
        statusText.text = "Openfort SDK initialized.";
    }

    #endregion

    // We get the session ticket from the PlayFab LoginResult, which will be needed to authenticate with Openfort
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
                statusText.text = "Player data found. Fetching NFT inventory...";
                GetPlayerNftInventory(_playerId);
            }
            else
            {
                // Get sessionTicket from LoginResult (PlayFab LoginResult)
                var sessionTicket = loginResult.SessionTicket;
                statusText.text = "Player data not found. Authenticating...";
                Authenticate(sessionTicket);
            }
        }, error =>
        {
            statusText.text = "Login error. Please retry.";
            savePlayerDataErrorEvent?.Invoke();
        });
    }

    // Authenticate using Openfort SDK
    public async void Authenticate(string idToken)
    {
        Debug.Log("PlayFab ID Token (session ticket): " + idToken);
        oauthAccessToken = idToken;
        
        // Create ThirdPartyOAuth request
        var oAuthRequest = new ThirdPartyOAuthRequest(
            ThirdPartyOAuthProvider.Playfab,
            idToken,
            TokenType.IdToken
        );
        
        // Authenticate with Openfort using PlayFab OAuth token
        try
        {
            statusText.text = "Authenticating with Openfort...";
            await openfort.AuthenticateWithThirdPartyProvider(oAuthRequest);
            statusText.text = "Authenticated with Openfort.";
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            statusText.text = "Authentication failed.";
            throw;
        }
        
        // Configure Embedded Signer
        try
        { 
            int chainId = 11155111; // Sepolia chain
            string encryptionSession = await OpenfortSessionManager.GetEncryptionSession();

            ShieldAuthentication shieldConfig = new ShieldAuthentication(ShieldAuthType.Openfort, oauthAccessToken, "playfab", "idToken");
            EmbeddedSignerRequest request = new EmbeddedSignerRequest(chainId, shieldConfig);

            statusText.text = "Configuring Embedded Signer...";
            await openfort.ConfigureEmbeddedSigner(request);
            statusText.text = "Embedded Signer configured.";
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            statusText.text = "Failed to configure Embedded Signer.";
            throw;
        }

        SavePlayerData();
    }

    #region AZURE_FUNCTION_CALLERS

    private void SavePlayerData()
    {
        if (string.IsNullOrEmpty(oauthAccessToken))
        {
            Debug.LogError("OAuth access token is null or empty.");
            statusText.text = "OAuth access token is null or empty.";
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

    public void MintNFT()
    {
        if (string.IsNullOrEmpty(oauthAccessToken))
        {
            Debug.LogError("Player ID is null or empty.");
            statusText.text = "Player ID is null or empty.";
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

        PlayFabCloudScriptAPI.ExecuteFunction(request, OnMintNftSuccessAsync, OnMintNftError);
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

    private async void OnMintNftSuccessAsync(ExecuteFunctionResult result)
    {
        TransactionIntentResponse txResponse = null;
        
        // Deserialize transaction intent
        try
        {
            txResponse = JsonConvert.DeserializeObject<TransactionIntentResponse>(result.FunctionResult.ToString());
            statusText.text = "Transaction intent deserialized.";
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            statusText.text = "Error deserializing transaction intent.";
            throw;
        }

        // Sign the transaction intent
        try
        {
            statusText.text = "Signing transaction intent...";
            SignatureTransactionIntentRequest signatureRequest = new SignatureTransactionIntentRequest(txResponse.Id, txResponse.UserOperationHash);
            TransactionIntentResponse signatureResponse = await openfort.SendSignatureTransactionIntentRequest(signatureRequest);
            statusText.text = "Transaction intent signed.";
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            statusText.text = "Error signing transaction intent.";
            mintPanel.SetActive(true);
            throw;
        }

        statusText.text = "NFT minted successfully!";
        Debug.Log("minted = true");
        GetPlayerNftInventory(_playerId);
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
            statusText.text = "Timeout during NFT minting. Please try again.";
            
            // TODO: Implement FindTransactionIntent()
            mintPanel.SetActive(true);
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