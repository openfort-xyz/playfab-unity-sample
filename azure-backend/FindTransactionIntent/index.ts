import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";

const openfort = new Openfort(process.env.OF_API_KEY);

function isValidRequestBody(body: any): boolean {
  return body &&
    body.CallerEntityProfile &&
    body.CallerEntityProfile.Lineage &&
    body.CallerEntityProfile.Lineage.MasterPlayerAccountId &&
    body.FunctionArgument &&
    body.FunctionArgument.playerId &&
    body.FunctionArgument.receiverAddress;
}

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  context.log("Starting HTTP trigger function processing.");

  try {
    if (!isValidRequestBody(req.body)) {
      context.log("Invalid request body received.");
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const playerId = req.body.FunctionArgument.playerId;
    const receiverAddress = req.body.FunctionArgument.receiverAddress;

    context.log(`Fetching data for player ID: ${playerId}`);

    const player = await openfort.players
      .get({ id: playerId, expand: ["transactionIntents"] })
      .catch((error) => {
        context.log("Error while fetching player data:", error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return null;
      });

    if (!player) {
      context.log("Failed to retrieve player data or error occurred.");
      return;
    }

    const transactionIntents = player["transactionIntents"];
    if (!transactionIntents || transactionIntents.length === 0) {
      context.log("No transaction intents associated with the player.");
      return;
    }

    const interactions = await transactionIntents[0].interactions;
    if (!interactions || interactions.length !== 1) {
      context.log("Either no interactions or multiple interactions found.");
      return;
    }

    if (
      !interactions[0].functionName ||
      !interactions[0].functionName.includes("mint")
    ) {
      context.log("Interaction doesn't include the 'mint' function name.");
      return;
    }

    let parsedAddress;
    try {
        parsedAddress = JSON.parse(interactions[0].functionArgs[0]);
    } catch (error) {
        context.log('Error parsing the address:', error);
        return;
    }

    if (!parsedAddress || parsedAddress !== receiverAddress) {
        context.log(`Receiver address mismatch. Expected: ${receiverAddress}, Found: ${parsedAddress}`);
        return;
    }

    context.res = {
      status: 200,
      body: JSON.stringify({
        id: transactionIntents[0].id
      }),
    };

    context.log("Function execution successful and response sent.");
  } catch (error) {
    context.log("Unhandled error occurred:", error);
    context.res = {
      status: 500,
      body: JSON.stringify(error),
    };
  }
};

export default httpTrigger;