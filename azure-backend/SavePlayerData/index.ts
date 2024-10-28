import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk"; // Import PlayFab Server SDK

// Environment variables
const OF_API_KEY = process.env.OF_API_KEY;
const OF_SPONSOR_POLICY = process.env.OF_SPONSOR_POLICY;
const PLAYFAB_SECRET_KEY = process.env.PLAYFAB_SECRET_KEY;
const PLAYFAB_TITLE_ID = process.env.PLAYFAB_TITLE_ID;

// Ensure required environment variables are set
if (!OF_API_KEY || !PLAYFAB_SECRET_KEY || !PLAYFAB_TITLE_ID) {
    throw new Error("Required environment variables missing: Ensure OF_API_KEY, PLAYFAB_SECRET_KEY, and PLAYFAB_TITLE_ID are set.");
}

// Configure PlayFab settings
PlayFabServer.settings.titleId = PLAYFAB_TITLE_ID;
PlayFabServer.settings.developerSecretKey = PLAYFAB_SECRET_KEY;

// Initialize Openfort client
const openfort = new Openfort(OF_API_KEY);

// Validate the request body
function validateRequestBody(req: HttpRequest): void {
    if (!req.body
        || !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId
        || !req.body.FunctionArgument.accessToken) {
        throw new Error("Invalid request body: Missing required parameters.");
    }
}

// Azure Function HTTP trigger
const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
    context.log("Starting HTTP trigger function processing.");

    try {
        // Validate the request body
        validateRequestBody(req);
        const masterPlayerAccountId = req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId;
        const accessToken = req.body.FunctionArgument.accessToken;

        // Verify PlayFab access token with Openfort
        const OFplayer = await getOpenfortPlayerByOAuthToken(accessToken);

        context.log(`Player with ID: ${OFplayer.id}`);

        // Get Openfort account for the player
        const OFaccount = await getOpenfortPlayerAccount(OFplayer.id);
        context.log(`Account address: ${OFaccount.address}`);
        
        // Add data to PlayFab UserReadOnlyData
        await addOpenfortPlayerDataToPlayFab(masterPlayerAccountId, OFplayer.id, OFaccount.address);

        // Build and send success response
        context.res = buildSuccessResponse(OFplayer.id, OFaccount.address);
        context.log("Function execution successful and response sent.");
    } catch (error) {
        context.log("An error occurred:", error);
        context.res = {
            status: 500,
            body: JSON.stringify(error),
        };
    }
};

// Verify PlayFab access token with Openfort
async function getOpenfortPlayerByOAuthToken(accessToken: string) {
    const OFplayer = await openfort.iam.verifyOAuthToken({
        provider: 'playfab',
        token: accessToken,
        tokenType: 'idToken',
    });

    if (!OFplayer) {
        throw new Error("Failed to verify PlayFab access token.");
    }

    return OFplayer;
}

// Get Openfort account for the player
async function getOpenfortPlayerAccount(playerId: string) {
    const accounts = await openfort.accounts.list({
        player: playerId,
        limit: 1
    });

    if (!accounts) {
        throw new Error("Failed to get Openfort player accounts.");
    }

    return accounts.data[0];
}

// Add Openfort player data to PlayFab UserReadOnlyData
async function addOpenfortPlayerDataToPlayFab(masterPlayerAccountId: string, OFplayerId: string, OFaccountAddress: string) {
    const data = {
        OpenfortPlayerId: OFplayerId,
        PlayerWalletAddress: OFaccountAddress
    };

    return new Promise((resolve, reject) => {
        PlayFabServer.UpdateUserReadOnlyData({
            PlayFabId: masterPlayerAccountId,
            Data: data,
        }, (error, result) => {
            if (error) {
                reject(new Error(`Failed to update PlayFab UserReadOnlyData: ${error.errorMessage}`));
            } else {
                resolve(result);
            }
        });
    });
}

// Build success response
function buildSuccessResponse(OFplayerId: string, OFaccountAddress: string) {
    return {
        status: 200,
        body: JSON.stringify({
            playerId: OFplayerId,
            playerWalletAddress: OFaccountAddress
        })
    };
}

export default httpTrigger;