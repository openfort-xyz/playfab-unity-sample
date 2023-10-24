import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk"; // Import PlayFab Server SDK

const OF_API_KEY = process.env.OF_API_KEY;
const PLAYFAB_SECRET_KEY = process.env.PLAYFAB_SECRET_KEY;
const PLAYFAB_TITLE_ID = process.env.PLAYFAB_TITLE_ID;
const CHAIN_ID = 80001; // Mumbai

if (!OF_API_KEY || !PLAYFAB_SECRET_KEY || !PLAYFAB_TITLE_ID) {
    throw new Error("Required environment variables missing: Ensure OF_API_KEY, PLAYFAB_SECRET_KEY, and PLAYFAB_TITLE_ID are set.");
}

PlayFabServer.settings.titleId = PLAYFAB_TITLE_ID;
PlayFabServer.settings.developerSecretKey = PLAYFAB_SECRET_KEY;

const openfort = new Openfort(OF_API_KEY);

function validateRequestBody(req: HttpRequest): void {
    if (!req.body || !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId) {
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

        context.log("Creating player in Openfort...");
        const OFplayer = await createOpenfortPlayer(masterPlayerAccountId);

        context.log(`Player with ID ${OFplayer.id} created. Proceeding to create account in Openfort...`);
        const OFaccount = await createOpenfortAccount(OFplayer.id);

        context.log(`Account with address ${OFaccount.address} created.`);
        
        // Adding data to PlayFab UserReadOnlyData
        await addDataToPlayFab(masterPlayerAccountId, OFplayer.id, OFaccount.address);

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

async function createOpenfortPlayer(masterPlayerAccountId: string) {
    const OFplayer = await openfort.players.create({ name: masterPlayerAccountId });
  
    if (!OFplayer) {
        throw new Error("Failed to create Openfort player.");
    }
    return OFplayer;
}

async function createOpenfortAccount(playerId: string) {
    const OFaccount = await openfort.accounts.create({
        player: playerId,
        chainId: CHAIN_ID,
    });

    if (!OFaccount) {
        throw new Error("Failed to create Openfort account.");
    }
    return OFaccount;
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
