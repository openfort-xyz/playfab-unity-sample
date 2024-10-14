import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";

// Environment variables
const OF_API_KEY = process.env.OF_API_KEY;
const OF_SHIELD_PUB_KEY = process.env.OF_SHIELD_PUB_KEY;
const OF_SHIELD_SECRET_KEY = process.env.OF_SHIELD_SECRET_KEY;
const OF_SHIELD_ENCRYPTION_SHARE = process.env.OF_SHIELD_ENCRYPTION_SHARE;

// Ensure required environment variables are set
if (!OF_API_KEY || !OF_SHIELD_PUB_KEY || !OF_SHIELD_SECRET_KEY || !OF_SHIELD_ENCRYPTION_SHARE) {
    throw new Error("Required environment variables missing: Ensure OF_API_KEY, OF_SHIELD_PUB_KEY, OF_SHIELD_SECRET_KEY, and OF_SHIELD_ENCRYPTION_SHARE are set.");
}

// Initialize Openfort client
const openfort = new Openfort(OF_API_KEY);

// Validate the request body
function validateRequestBody(req: HttpRequest): void {
    if (!req.body
        || !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId) {
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

        // Register recovery session with Openfort Shield
        const session = await openfort.registerRecoverySession(OF_SHIELD_PUB_KEY, OF_SHIELD_SECRET_KEY, OF_SHIELD_ENCRYPTION_SHARE);

        // Build and send success response
        context.res = buildSuccessResponse(session);
        context.log("Function execution successful and response sent.");
    } catch (error) {
        context.log("An error occurred:", error);
        context.res = {
            status: 500,
            body: JSON.stringify(error),
        };
    }
};

// Build success response
function buildSuccessResponse(session: string) {
    return {
        status: 200,
        body: JSON.stringify({ session })
    };
}

export default httpTrigger;