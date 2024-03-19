import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk"; // Import PlayFab Server SDK

const OF_API_KEY = process.env.OF_API_KEY;
const PLAYFAB_SECRET_KEY = process.env.PLAYFAB_SECRET_KEY;
const PLAYFAB_TITLE_ID = process.env.PLAYFAB_TITLE_ID;

if (!OF_API_KEY || !PLAYFAB_SECRET_KEY || !PLAYFAB_TITLE_ID) {
    throw new Error("Required environment variables missing: Ensure OF_API_KEY, PLAYFAB_SECRET_KEY, and PLAYFAB_TITLE_ID are set.");
}

PlayFabServer.settings.titleId = PLAYFAB_TITLE_ID;
PlayFabServer.settings.developerSecretKey = PLAYFAB_SECRET_KEY;

const openfort = new Openfort(OF_API_KEY);

function validateRequestBody(req: HttpRequest): void {
    if (!req.body
        || !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId
        || !req.body.FunctionArgument.accessToken) {
        throw new Error("Invalid request body: Missing required parameters.");
    }
}

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
    context.log("Starting HTTP trigger function processing.");

    try {
        validateRequestBody(req);
        const masterPlayerAccountId = req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId;
        const accessToken = req.body.FunctionArgument.accessToken;

        const OFplayer = await openfort.iam.verifyAuthToken(accessToken)
        if (!OFplayer) {
            throw new Error("Could not get Openfort player with access token.");
        }

        context.log(`Player with ID: ${OFplayer.playerId}`);
        const OFaccount = await getPlayerAccount(OFplayer.playerId);

        context.log(`Account with address ${OFaccount.address} created.`);
        
        // Adding data to PlayFab UserReadOnlyData
        await addDataToPlayFab(masterPlayerAccountId, OFplayer.playerId, OFaccount.address);

        context.res = buildSuccessResponse(OFplayer.playerId, OFaccount.address);
        context.log("Function execution successful and response sent.");
    } catch (error) {
        context.log("An error occurred:", error);
        context.res = {
            status: 500,
            body: JSON.stringify(error),
        };
    }
};

async function addDataToPlayFab(masterPlayerAccountId: string, OFplayerId: string, OFaccountAddress: string) {
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

async function getPlayerAccount(playerId: string) {
    const accounts = await openfort.accounts.list({
        player: playerId,
        limit: 1
    });

    if (!accounts) {
        throw new Error("Failed to get Openfort player accounts.");
    }

    return accounts.data[0];
}

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
