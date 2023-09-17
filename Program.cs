using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using AzureDevOpsWorkloadIdentity;
using Pulumi;
using Pulumi.AzureAD;
using Pulumi.AzureDevOps;
using Pulumi.AzureDevOps.Inputs;
using Pulumi.AzureNative.Authorization;
using Deployment = Pulumi.Deployment;
using GetClientConfig = Pulumi.AzureNative.Authorization.GetClientConfig;

return await Deployment.RunAsync(() =>
{
    var organizationUrl = Pulumi.AzureDevOps.GetClientConfig.Invoke()
        .Apply(c => c.OrganizationUrl);
    var organisationName = organizationUrl.Apply(url => new Uri(url).Segments.Last().TrimEnd('/'));
    
    var project = new Project("AzureReadyADOProject", new()
    {
        Description = "New project with everything correctly configured to provision Azure resources or deploy applications to Azure",
        Features = new()
        {
            ["boards"] = "disabled",
            ["repositories"] = "enabled",
            ["pipelines"] = "enabled",
            ["testplans"] = "disabled",
            ["artifacts"] = "disabled"
        },
    });
    var repository = GetGitRepository.Invoke(new()
    {
        ProjectId = project.Id,
        Name = project.Name
    });
    // var repository = new Git("AzureReadyADORepository", new()
    // {
    //     ProjectId = project.Id,
    //     Initialization = new GitInitializationArgs()
    //     {
    //         InitType = "Clean",
    //         SourceType = "Git",
    //         SourceUrl = "https://repo.com",
    //         ServiceConnectionId = ""
    //     },
    //     DefaultBranch = "refs/heads/main"
    // });
    
    var pipelineFile = new GitRepositoryFile("AzurePipeline", new()
    {
        File = "azure-pipelines.yaml",
        RepositoryId = repository.Apply(r => r.Id),
        CommitMessage = "Add preconfigured pipeline file",
        Content = File.ReadAllText("azure-pipelines.yml"),
        Branch = "refs/heads/main"
    });

    var azureConfig = GetClientConfig.Invoke();
    var aadApplication = new Application("ADOAzureReadyApp", new()
    {
        DisplayName = "ADO Azure Ready App"
    });
    var servicePrincipal  = new ServicePrincipal("AzureReadyServicePrincipal", new()
    {
        ApplicationId = aadApplication.ApplicationId,
    });

    var subscriptionId = azureConfig.Apply(c => c.SubscriptionId);
    new RoleAssignment("contributor", new()
    {
        PrincipalId= servicePrincipal.Id,
        PrincipalType= PrincipalType.ServicePrincipal,
        RoleDefinitionId = AzureBuiltInRoles.Contributor,
        Scope = Output.Format($"/subscriptions/{subscriptionId}")
    });
    
    var subscriptionName = subscriptionId.Apply(s =>
    {
        var armClient = new ArmClient(new DefaultAzureCredential());
        var subscription = armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{s}")).Get();
        return subscription.Value.Data.DisplayName;
    });
    
    var serviceConnection = new ServiceEndpointAzureRM("AzureServiceConnection", new()
    {
        ProjectId = project.Id,
        ServiceEndpointName = "azure-with-oidc",
        ServiceEndpointAuthenticationScheme = "WorkloadIdentityFederation",
        AzurermSpnTenantid = azureConfig.Apply(c => c.TenantId),
        AzurermSubscriptionId = subscriptionId,
        AzurermSubscriptionName = subscriptionName,
        Credentials = new ServiceEndpointAzureRMCredentialsArgs()
        {
            Serviceprincipalid = servicePrincipal.ApplicationId,
        }
    });
    
    new ApplicationFederatedIdentityCredential("ADOAzureReadyAppFederatedIdentityCredential", new() 
    {
        ApplicationObjectId = aadApplication.ObjectId,
        DisplayName = "AzureReadyDeploys",
        Description = "Deployments for azure-ready-repository",
        Audiences = new(){"api://AzureADTokenExchange" },
        Issuer = serviceConnection.WorkloadIdentityFederationIssuer,
        Subject = Output.Format($"sc://{organisationName}/{project.Name}/{serviceConnection.ServiceEndpointName}")
    });

    var pipeline = new BuildDefinition("deployToAzure", new()
    {
        ProjectId = project.Id,
        Repository = new BuildDefinitionRepositoryArgs()
        {
            RepoId = repository.Apply(r => r.Id),
            BranchName = "refs/heads/main",
            YmlPath = pipelineFile.File,
            RepoType = "TfsGit"
        }
    });
    new PipelineAuthorization("azureOidcPipelineAuthorization", new()
    {
        ProjectId = project.Id,
        Type = "endpoint",
        PipelineId = pipeline.Id.Apply(int.Parse),
        ResourceId = serviceConnection.Id
    });
    
    return new Dictionary<string, object?>
    {
        ["pipelineUrl"] = Output.Format($"{organizationUrl}{project.Name}/_build?definitionId={pipeline.Id}")
    };
});