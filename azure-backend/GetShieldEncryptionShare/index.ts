import { AzureFunction, Context, HttpRequest } from "@azure/functions";

// Environment variables
const OF_SHIELD_ENCRYPTION_SHARE = process.env.OF_SHIELD_ENCRYPTION_SHARE;

// Ensure required environment variables are set
if (!OF_SHIELD_ENCRYPTION_SHARE) {
    throw new Error("Required environment variables missing: Ensure OF_SHIELD_ENCRYPTION_SHARE is set.");
}

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

        // Build and send success response
        context.res = buildSuccessResponse(OF_SHIELD_ENCRYPTION_SHARE);
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
function buildSuccessResponse(encryptionShare: string) {
    return {
        status: 200,
        body: encryptionShare
    };
}

export default httpTrigger;