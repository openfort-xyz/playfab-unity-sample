import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";

const CHAIN_ID = 80001; // Mumbai
const openfort = new Openfort(process.env.OF_API_KEY);

function isValidRequestBody(body: any): boolean {
  return body &&
    body.CallerEntityProfile &&
    body.CallerEntityProfile.Lineage &&
    body.CallerEntityProfile.Lineage.MasterPlayerAccountId &&
    body.FunctionArgument &&
    body.FunctionArgument.playerId;
}

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    context.log("Starting HTTP trigger function processing.");
    
    context.log("Request Body:", JSON.stringify(req.body));

    if (!isValidRequestBody(req.body)) {
      context.log("Invalid request body received.");
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const playerId = req.body.FunctionArgument.playerId;
    context.log(`Valid request. Processing for playerId: ${playerId}`);

    async function getPlayerNftInventory(playerId: string) {
      context.log(`Fetching NFT inventory for playerId: ${playerId}`);
      const inventory = await openfort.inventories.getPlayerNftInventory({ playerId: playerId, chainId: CHAIN_ID });
      
      if (!inventory) {
          throw new Error("Failed to retrieve inventory.");
      }
      context.log(`Successfully retrieved NFT inventory for playerId: ${playerId}`);
      return inventory;
    }

    // Call the function to get the player's NFT inventory
    const inventoryResponse = await getPlayerNftInventory(playerId);
    context.log("Inventory Response:", JSON.stringify(inventoryResponse));

    // Extract the 'data' section from the response
    const inventoryData = inventoryResponse.data;
    context.log("Processed Inventory Data:", JSON.stringify(inventoryData));

    context.log(`Sending inventory data response for playerId: ${playerId}`);
    
    context.res = {
      status: 200,
      body: JSON.stringify(inventoryData),
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