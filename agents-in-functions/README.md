# Agents in Azure Functions with Python 

This folder contains an Azure Function to illustrate how one could use OpenAI Agents SDK to build small smart extensions for existing applications, or to do some hands on with the Agents SDK. 

Additionally, all necessary cloud resources could be deployed using `azd` (Azure Developer CLI). Follow the instructions below for initializing and deploying the application to your Azure Subscription

## Prerequisites

+ [Python 3.13](https://www.python.org/)
+ [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-python#install-the-azure-functions-core-tools)
+ [Azure Developer CLI (AZD)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
+ To use Visual Studio Code to run and debug locally:
  + [Visual Studio Code](https://code.visualstudio.com/)
  + [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)

## Prepare your local environment

Add a file named `local.settings.json` in the root of your project with the following contents:

```json
{
    "IsEncrypted": false,
    "Values": {
      "FUNCTIONS_WORKER_RUNTIME": "python"
      "OPENAI_API_KEY": "<your-open-ai-api-key>"

    }
}
```

## Create a virtual environment

The way that you create your virtual environment depends on your operating system.
Open the terminal, navigate to the project folder, and run these commands:

### Linux / macOS / bash

```bash
python -m venv .venv
source .venv/bin/activate
```

### Windows (Cmd)

```shell
py -m venv .venv
.venv\scripts\activate
```

## Run your app from the terminal

1. To start the Functions host locally, run these commands in the virtual environment:

    ```shell
    pip3 install -r requirements.txt
    func start
    ```

1. Test the POST endpoint, use your favorite HTTP test tool. This example uses the `curl` tool with payload data from the [`testdata.json`](./testdata.json):

    ```shell
    curl -i http://localhost:7071/api/buche -H "Content-Type: text/json" -d @testdata.json
    ```

1. When you're done, press `CTRL+C`` in the terminal window to stop the `func.exe` host process.

1. Run `deactivate` to shut down the virtual environment.

## Run your app using Visual Studio Code

1. Open the root folder in a new terminal.
1. Run the `code .` code command to open the project in Visual Studio Code.
1. Press **Run/Debug (F5)** to run in the debugger. Select **Debug anyway** if prompted about local emulator not running.
1. Send POST requests to the `/api/buche` endpoint using your HTTP test tool. If you have the [RestClient](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension installed, you can execute requests directly from the [`test.http`](./test.http) file.

## Deploy to Azure

Run this command to provision the function app, with any required Azure resources, and deploy your code:

```shell
azd up
```

You're prompted to supply these required deployment parameters:

| Parameter | Description |
| ---- | ---- |
| _Environment name_ | An environment that's used to maintain a unique deployment context for your app. You aren't prompted when you created the local project using `azd init`.|
| _Azure subscription_ | Subscription in which your resources are created.|
| _Azure location_ | Azure region in which to create the resource group that contains the new Azure resources. Only regions that currently support the Flex Consumption plan are shown.|
| `Open AI API Key` | The API Key that should be used by the Azure Functtion when interacting with OpenAI |

## Redeploy your code

You can run the `azd up` command as many times as you need to both provision your Azure resources and deploy code updates to your function app.

>[!NOTE]
>Deployed code files are always overwritten by the latest deployment package.

## Clean up resources

When you're done working with your function app and related resources, you can use this command to delete the function app and its related resources from Azure and avoid incurring any further costs:

```shell
azd down
```
