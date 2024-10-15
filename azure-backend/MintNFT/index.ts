import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort, {
    CreateTransactionIntentRequest,
    Interaction,
    TransactionIntentResponse
} from "@openfort/openfort-node";

const OF_API_KEY = process.env.OF_API_KEY;
const OF_CHAIN_ID = process.env.OF_CHAIN_ID;
const OF_NFT_CONTRACT = process.env.OF_NFT_CONTRACT;
const OF_SPONSOR_POLICY = process.env.OF_SPONSOR_POLICY;

if (!OF_API_KEY || !OF_NFT_CONTRACT || !OF_SPONSOR_POLICY) {
    throw new Error("Required environment variables are not set.");
}

const openfort = new Openfort(OF_API_KEY);

function validateRequestBody(req: HttpRequest): void {
    if (!req.body || 
        !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
        !req.body.FunctionArgument.accessToken) {
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
        context.log("Request body validated.");

        const accessToken = req.body.FunctionArgument.accessToken;

        const player = await getOpenfortPlayerByOAuthToken(accessToken);

        context.log(`Creating transaction intent...`);
        const transactionIntent = await createTransactionIntent(player.id);

        context.res = buildSuccessResponse(transactionIntent);
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

async function createTransactionIntent(playerId: string): Promise<TransactionIntentResponse> {
    const interaction: Interaction = {
        contract: OF_NFT_CONTRACT,
        functionName: "mint",
        functionArgs: [
            playerId // _receiver (address)
        ]
    };
    

    const transactionIntentRequest: CreateTransactionIntentRequest = {
        player: playerId,
        policy: OF_SPONSOR_POLICY,
        chainId: Number(OF_CHAIN_ID),
        interactions: [interaction]
    };

    const transactionIntent = await openfort.transactionIntents.create(transactionIntentRequest);

    if (!transactionIntent) {
        throw new Error("Failed to create transaction intent.");
    }
    return transactionIntent;
}

function buildSuccessResponse(transactionIntent: TransactionIntentResponse) {
    return {
        status: 200,
        body: transactionIntent
    };
}

export default httpTrigger;