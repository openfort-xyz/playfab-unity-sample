# Openfort-PlayFab Integration in Unity

## Overview
[PlayFab](https://playfab.com/) is a backend service provided by Microsoft for game developers, offering tools for live game management, all powered by Azure's cloud infrastructure.

In this sample we use PlayFab's email & password authentication method to register a new user or log in with an existing one. Once authenticaded we use PlayFab's user identity token to create a self-custodial account using [Embedded Smart Accounts](https://www.openfort.xyz/blog/embedded-smart-accounts).

Moreover, by integrating the [Openfort SDK](https://github.com/openfort-xyz/openfort-node) into Azure Functions, we establish a seamless connection to PlayFab. Unity clients using the PlayFab Unity SDK can tap into these functions, accessing the full range of Openfort features within the game environment.

## Application Workflow

<div align="center">
    <img
      width="100%"
      height="100%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_workflow_43cf69904d.png?updated_at=2024-10-17T11:56:43.141Z"
      alt='Openfort PlayFab integration workflow'
    />
</div>

## Prerequisites
+ [Create a PlayFab account and title](https://learn.microsoft.com/en-us/gaming/playfab/gamemanager/quickstart)
+ Set up your Azure development environment:
    + [Configure your environment](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-node?pivots=nodejs-model-v4#configure-your-environment)
    + [Sign in to Azure](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-node?pivots=nodejs-model-v4#sign-in-to-azure)
    + [Create a function app](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-node?pivots=nodejs-model-v4#create-the-function-app-in-azure)
+ [Sign in to dashboard.openfort.xyz](http://dashboard.openfort.xyz) and create a new project
+ Download or clone the [sample project](https://github.com/openfort-xyz/playfab-unity-sample): 
    + Open [unity-client](https://github.com/openfort-xyz/playfab-unity-sample/tree/main/unity-client) with Unity
    + Open [azure-backend](https://github.com/openfort-xyz/playfab-unity-sample/tree/main/azure-backend) with VS Code

## Set up Openfort

1. #### [Add PlayFab as a provider](https://dashboard.openfort.xyz/players/auth/providers)

   Add your PlayFab title and choose ***Save***:
   <div align="center">
    <img
      width="50%"
      height="50%"
      src="https://blog-cms.openfort.xyz/uploads/playfab_integration_provider_750b4ed963.png?updated_at=2024-03-20T08:07:36.965Z"
      alt='PlayFab provider'
    />
   </div>

2. #### [Create Shield Keys](https://dashboard.openfort.xyz/developers/api-keys)
   In order to create secure self-custodial accounts for our players, we need to create [Shield Keys](https://www.openfort.xyz/docs/guides/client/api-keys#shield-secret-and-publishable-keys):

   <div align="center">
    <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_1_a_fd65cb50f3.png?updated_at=2024-10-17T10:03:12.057Z"
      alt='playfab_integration_1_a_fd65cb50f3'
    />
   </div>

   After the creation, it's very important you save **Shield Encryption Share Key**, you will need it later:

   <div align="center">
    <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_1_b_b57537dc82.png?updated_at=2024-10-17T10:03:12.650Z"
      alt='playfab_integration_1_b_b57537dc82'
    />
   </div> 

3. #### [Add a Contract](https://dashboard.openfort.xyz/assets)
   This sample requires a contract to run. We use [0x51216BFCf37A1D2002A9F3290fe5037C744a6438](https://sepolia.etherscan.io/address/0x51216BFCf37A1D2002A9F3290fe5037C744a6438) (NFT contract deployed in Sepolia - 11155111). You can use the same to ease up things:

   <div align="center">
    <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_1_95cbd5e3b9.png?updated_at=2024-10-17T09:13:44.426Z"
      alt='playfab_integration_1_95cbd5e3b9'
    />
   </div>

4. #### [Add a Policy](https://dashboard.openfort.xyz/policies/new)
   We aim to cover gas fees for users. Set a new gas policy:

   <div align="center">
    <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_2_dc0ec65caa.png?updated_at=2024-10-17T09:13:44.439Z"
      alt='playfab_integration_2_dc0ec65caa'
    />
   </div>

   Now, add a rule to make our contract benefit from it:

   <div align="center">
    <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_3_8c21c821c5.png?updated_at=2024-10-17T09:13:44.439Z"
      alt='playfab_integration_3_8c21c821c5'
    />
   </div>

## Deploy Azure Backend
Open [azure-backend](https://github.com/openfort-xyz/playfab-unity-sample/tree/main/azure-backend) with VS Code and sign in to Azure:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_4_7deea77cf3.png?updated_at=2024-10-17T09:22:01.129Z"
    alt='playfab_integration_4_7deea77cf3'
  />
</div>

Ensure your Function App (here, it's "openfort-playfab") is listed:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_5_6d5e73b886.png?updated_at=2024-10-17T09:22:01.428Z"
    alt='playfab_integration_5_6d5e73b886'
  />
</div>

In the terminal, run:
```
npm install
```

In the explorer, right-click on a function and select ***Deploy to Function App***:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_6_ffc63d55ed.png?updated_at=2024-10-17T09:22:01.628Z"
    alt='playfab_integration_6_ffc63d55ed'
  />
</div>

Next, choose your Function App:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_7_977d0a46c8.png?updated_at=2024-10-17T09:22:00.931Z"
    alt='playfab_integration_7_977d0a46c8'
  />
</div>

Then, click on ***Deploy***:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_8_1119fa1634.png?updated_at=2024-10-17T09:22:01.232Z"
    alt='playfab_integration_8_1119fa1634'
  />
</div>

Navigate to your [Azure Portal](https://portal.azure.com/#home) and open your Function App. You should see all the functions listed:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_9_fb12c55de4.png?updated_at=2024-10-17T09:29:35.428Z"
    alt='playfab_integration_9_fb12c55de4'
  />
</div>

Click on any function and select ***Get Function Url***:

<div align="center">
  <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_10_c4ebde5781.png?updated_at=2024-10-17T09:29:35.242Z"
    alt='playfab_integration_10_c4ebde5781'
  />  
</div>

Subsequently, add this URL (along with all others) to PlayFab to enable access to our Azure Functions from within PlayFab.

## Set up PlayFab Title

1. #### Register Azure Functions
    Visit the [PlayFab developer dashboard](https://developer.playfab.com/), choose your title, and click on ***Automation***:

    <div align="center">
      <img
        width="50%"
        height="50%"
        src="https://strapi-oube.onrender.com/uploads/playfab_integration_11_5615028c17.png?updated_at=2024-10-17T09:36:46.329Z"
        alt='playfab_integration_11_5615028c17'
      />  
    </div>

    Our functions are already registered. To do the same, click ***Register function*** and provide the function name along with its URL:

    <div align="center">
      <img
        width="50%"
        height="50%"
        src="https://strapi-oube.onrender.com/uploads/playfab_integration_12_644b97fdaf.png?updated_at=2024-10-17T09:36:45.831Z"
        alt='playfab_integration_12_644b97fdaf'
      />  
    </div>

    Repeat this for all deployed functions.

## Set up Azure Backend

Our Azure backend requires environment variables from both PlayFab and Openfort. Let's configure them.

1. #### Add Openfort Environment Variables
    - Navigate to the [Azure Portal](https://portal.azure.com/#home) and select your Function App.
    - Under ***Settings --> Environment variables***, click ***Add***:
      
    <div align="center">
      <img
        width="50%"
        height="50%"
        src="https://strapi-oube.onrender.com/uploads/playfab_integration_13_c828ed6ed8.png?updated_at=2024-10-17T09:45:44.934Z"
        alt='playfab_integration_13_c828ed6ed8'
      />  
    </div>

    - Provide the following details:
      + Name: `OF_API_KEY`
      + Value: [Retrieve the **API Secret key**](https://dashboard.openfort.xyz/developers/api-keys)
    
    - Add another application setting:
      + Name: `OF_SHIELD_PUB_KEY`
      + Value: [Retrieve the **Shield Publishable Key**](https://dashboard.openfort.xyz/developers/api-keys)
    
    - Add another application setting:
      + Name: `OF_SHIELD_SECRET_KEY`
      + Value: [Retrieve the **Shield Secret Key**](https://dashboard.openfort.xyz/developers/api-keys)

    - Add another application setting:
      + Name: `OF_SHIELD_ENCRYPTION_SHARE`
      + Value: It's the **Shield Encryption Share Key** you saved before.
    
    - Add another application setting:
      + Name: `OF_NFT_CONTRACT`
      + Value: [Retrieve the **Contract API ID**](https://dashboard.openfort.xyz/assets)

    - Add another application setting:
      + Name: `OF_SPONSOR_POLICY`
      + Value: [Retrieve the **Policy API ID**](https://dashboard.openfort.xyz/policies)

    - And another application setting:
      + Name: `OF_CHAIN_ID`
      + Value: 11155111

2. #### Add PlayFab Environment Variables
    - Visit the [PlayFab developer dashboard](https://developer.playfab.com/), select your title, and navigate to ***Settings wheel --> Title settings***:

      <div align="center">
        <img
        width="50%"
        height="50%"
        src="https://strapi-oube.onrender.com/uploads/playfab_integration_14_5bc8927522.png?updated_at=2024-10-17T11:03:15.964Z"
        alt='playfab_integration_14_5bc8927522'
        />  
      </div>

    - In the ***API Features*** section, copy your ***Title ID***:

      <div align="center">
        <img
        width="50%"
        height="50%"
        src="https://strapi-oube.onrender.com/uploads/playfab_integration_15_711115c4a8.png?updated_at=2024-10-17T11:03:16.150Z"
        alt='playfab_integration_15_711115c4a8'
        />  
      </div>

    - Under ***Secret Keys***, note down your ***Secret key***:

      <div align="center">
        <img
        width="50%"
        height="50%"
        src="https://strapi-oube.onrender.com/uploads/playfab_integration_16_56663676c7.png?updated_at=2024-10-17T11:03:16.644Z"
        alt='playfab_integration_16_56663676c7'
        />  
      </div>
 
    - Return to the [Azure Portal](https://portal.azure.com/#home) and choose your Function App.
    - Under ***Settings --> Environment variables***, click ***Add***:
      + Name: `PLAYFAB_TITLE_ID`
      + Value: [Your Title ID]

    - Add another application setting:
      + Name: `PLAYFAB_SECRET_KEY`
      + Value: [Your Secret Key]


  After adding all the environment variables, your configuration panel should look like the following. Confirm your changes by clicking ***Save***:

  <div align="center">
    <img
    width="50%"
    height="50%"
    src="https://strapi-oube.onrender.com/uploads/playfab_integration_17_6748a73edf.png?updated_at=2024-10-17T11:06:47.553Z"
    alt='playfab_integration_17_6748a73edf'
    />  
  </div>

## Set up Unity Client

This Unity sample project is already equipped with:
+ [PlayFab Unity SDK](https://github.com/PlayFab/UnitySDK)
+ [Openfort SDK](https://github.com/openfort-xyz/openfort-csharp-unity)

To begin, open [unity-client](https://github.com/openfort-xyz/playfab-unity-sample/tree/main/unity-client) with Unity:

1. #### Configure PlayFab SDK
    - Navigate to the ***Project*** tab.
    - Search for `PlayFabSharedSettings` and input your PlayFab ***Title ID***:

    <div align="center">
      <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_18_591d3cad7d.png?updated_at=2024-10-17T11:13:05.455Z"
      alt='playfab_integration_18_591d3cad7d'
      />  
    </div>

2. #### Configure Openfort SDK

    - Open the *Login scene* and add the **API Publishable Key** and the **Shield Publishable Key** to the *OpenfortController* config section:

    <div align="center">
      <img
      width="50%"
      height="50%"
      src="https://strapi-oube.onrender.com/uploads/playfab_integration_19_24bc025e75.png?updated_at=2024-10-17T11:13:05.251Z"
      alt='playfab_integration_19_24bc025e75'
      />  
    </div>

## Test in Editor

Play ***Login*** scene, opt for ***Register***, provide an email and password, then click ***Register*** again. This scene should appear:

<div align="center">
  <img
  width="50%"
  height="50%"
  src="https://strapi-oube.onrender.com/uploads/playfab_integration_20_f934393700.png?updated_at=2024-10-17T11:20:35.838Z"
  alt='playfab_integration_20_f934393700'
  />  
</div>

Select ***Mint***. After a brief period, you should see a representation of your newly minted NFT:

<div align="center">
  <img
  width="50%"
  height="50%"
  src="https://strapi-oube.onrender.com/uploads/playfab_integration_21_8213efd860.png?updated_at=2024-10-17T11:20:36.645Z"
  alt='playfab_integration_21_8213efd860'
  />  
</div>

In the [Openfort Players dashboard](https://dashboard.openfort.xyz/players), a new player entry should be visible. On selecting this player:

<div align="center">
  <img
  width="50%"
  height="50%"
  src="https://strapi-oube.onrender.com/uploads/playfab_integration_22_1b307cda7e.png?updated_at=2024-10-17T11:20:36.654Z"
  alt='playfab_integration_22_1b307cda7e'
  />  
</div>

You'll notice that a `mint` transaction has been successfully processed:

<div align="center">
  <img
  width="50%"
  height="50%"
  src="https://strapi-oube.onrender.com/uploads/playfab_integration_23_eca707b92d.png?updated_at=2024-10-17T11:27:27.546Z"
  alt='playfab_integration_23_eca707b92d'
  />  
</div>

Additionally, by choosing your **Sepolia Wallet Address**, the explorer will open and by selecting ***NFT Transfers*** tab you'll see the transaction is further confirmed:

<div align="center">
  <img
  width="50%"
  height="50%"
  src="https://strapi-oube.onrender.com/uploads/playfab_integration_24_f29150e8b8.png?updated_at=2024-10-17T11:27:28.651Z"
  alt='playfab_integration_24_f29150e8b8'
  />  
</div>

<div align="center">
  <img
  width="50%"
  height="50%"
  src="https://strapi-oube.onrender.com/uploads/playfab_integration_25_8aa4b7ec6a.png?updated_at=2024-10-17T11:27:28.239Z"
  alt='playfab_integration_25_8aa4b7ec6a'
  />  
</div>

## Conclusion

Upon completing the above steps, your Unity game will be fully integrated with Openfort and PlayFab. Always remember to test every feature before deploying to guarantee a flawless player experience.

## Get support
If you found a bug or want to suggest a new [feature/use case/sample], please [file an issue](../../issues).

If you have questions, comments, or need help with code, we're here to help:
- on Twitter at https://twitter.com/openfortxyz
- on Discord: https://discord.com/invite/t7x7hwkJF4
- by email: support+youtube@openfort.xyz
