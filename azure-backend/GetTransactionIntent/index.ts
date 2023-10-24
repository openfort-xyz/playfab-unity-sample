import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";

const openfort = new Openfort(process.env.OF_API_KEY);

function isValidRequestBody(body: any): boolean {
  return body &&
    body.CallerEntityProfile &&
    body.CallerEntityProfile.Lineage &&
    body.CallerEntityProfile.Lineage.MasterPlayerAccountId &&
    body.FunctionArgument &&
    body.FunctionArgument.transactionIntentId;
}

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    context.log("Starting HTTP trigger function processing.");

    if (!isValidRequestBody(req.body)) {
      context.log("Invalid request body received.");
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const transactionIntentId = req.body.FunctionArgument.transactionIntentId;
    context.log(`Fetching transactionIntent for ID: ${transactionIntentId}`);

    const transactionIntent = await openfort.transactionIntents
      .get({ id: transactionIntentId })
      .catch((error) => {
        context.log("Error while fetching transactionIntent:", error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return null;
      });

    if (!transactionIntent) {
      context.log("TransactionIntent not found or error occurred.");
      return;
    }

    context.res = {
      status: 200,
      body: JSON.stringify({
        minted: transactionIntent.response?.status ? true : false,
        id: transactionIntent.id
      }),
    };

    context.log("API call was successful and response sent.");
  } catch (error) {
    context.log("Unhandled error occurred:", error);
    context.res = {
      status: 500,
      body: JSON.stringify(error),
    };
  }
};

export default httpTrigger;
