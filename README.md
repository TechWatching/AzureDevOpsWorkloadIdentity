# Sample code for the article "Deploying to Azure from Azure DevOps without secrets"

## What is it?

This repository contains the code used in this [blog article](https://www.techwatching.dev/posts/ado-workload-identity-federation) that talks about provisioning an Azure DevOps project that has everything correctly configured to provision Azure resources or deploy applications to Azure from an Azure Pipelines workflow.

This code is a Pulumi .NET program that can be executed from the Pulumi CLI. When you execute it, it will provision the following resources:
- an Azure Project configured with a Git repository, an ARM service connection and a pre-configured pipeline
- a Microsoft Entra ID app registration, its associated Service Principal and a Federated Identity Credential

![azuredevopsoidc_schema_2](https://github.com/TechWatching/AzureDevOpsWorkloadIdentity/assets/15186176/c8b5b250-9e1d-4f4e-8e7d-ff685343eb3d)

I suggest you to read [the article](https://www.techwatching.dev/posts/ado-workload-identity-federation) before using this code. And if you are not familiar with Pulumi you should check their [documentation](https://www.pulumi.com/docs/) or [learning pathways](https://www.pulumi.com/learn/) too.

## How to use it ?

### Prerequisites

You can check [Pulumi documentation](https://www.pulumi.com/docs/get-started/azure/begin/) to set up your environment.
You will have to install on your machine:
- Pulumi CLI
- Azure CLI
- .NET

You will need an Azure DevOps organization, an Azure subscription, and access to a Microsoft Entra ID.

You can use any [backend](https://www.pulumi.com/docs/intro/concepts/state/) for your Pulumi program (to store the state and encrypt secrets) but I suggest you to use the default backend: the Pulumi Cloud. It's free for individuals, you will just need to create an account on Pulumi website. If you prefer to use an Azure Blob Storage backend with an Azure Key Vault as the encryption provider you can check [this article](https://www.techwatching.dev/posts/pulumi-azure-backend).

Before executing the program you need to modify the configuration of the stack (contained in the `Pulumi.dev.yaml` file) to set the Pulumi and the GitHub tokens. You can do that by executing the following commands:

```pwsh
pulumi config set --secret pulumiTokenForRepository yourpulumicloudtoken
pulumi config set azuredevops:orgServiceUrl yourazuredevopsorganizationurl --secret
pulumi config set azuredevops:personalAccessToken yourazuredevopspat --secret
```
Ensure you are connected to the Azure CLI with the account and on the subscription where you want to provision the resources (you can run the `az account show` command to display the information).  

You can also modify the `Program.cs` file to use the names you want for your resources.

### Execute the Pulumi program

- clone this repository
- log on to your Azure account using Azure CLI
- log on to your Pulumi backend using Pulumi CLI
- install the dependencies
- run this command `pulumi up`

