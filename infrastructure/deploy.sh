#!/bin/bash

# Azure Bicep Deployment Script for Score Burrow
# Prerequisites: Azure CLI installed and logged in (az login)

set -e

# Configuration
RESOURCE_GROUP_NAME="score-burrow-rg"
LOCATION="australiaeast"
DEPLOYMENT_NAME="score-burrow-deployment-$(date +%Y%m%d-%H%M%S)"

# Determine which parameters file to use
if [ -f "parameters.local.json" ]; then
    PARAMETERS_FILE="parameters.local.json"
    echo -e "${GREEN}Using local parameters file: parameters.local.json${NC}"
else
    PARAMETERS_FILE="parameters.json"
    echo -e "${YELLOW}Using default parameters file: parameters.json${NC}"
fi

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Score Burrow Deployment Script ===${NC}"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Please install it from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
echo -e "${YELLOW}Checking Azure login status...${NC}"
az account show &> /dev/null || {
    echo -e "${RED}Error: Not logged in to Azure${NC}"
    echo "Please run: az login"
    exit 1
}

SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
echo -e "${GREEN}✓ Logged in to Azure subscription: $SUBSCRIPTION_NAME${NC}"
echo ""

# Create resource group if it doesn't exist
echo -e "${YELLOW}Creating resource group if needed...${NC}"
az group create \
    --name $RESOURCE_GROUP_NAME \
    --location $LOCATION \
    --output none

echo -e "${GREEN}✓ Resource group ready: $RESOURCE_GROUP_NAME${NC}"
echo ""

# Validate Bicep template
echo -e "${YELLOW}Validating Bicep template...${NC}"
az deployment group validate \
    --resource-group $RESOURCE_GROUP_NAME \
    --template-file main.bicep \
    --parameters $PARAMETERS_FILE \
    --output none

echo -e "${GREEN}✓ Bicep template validation successful${NC}"
echo ""

# Deploy Bicep template
echo -e "${YELLOW}Deploying infrastructure...${NC}"
echo "Deployment name: $DEPLOYMENT_NAME"
echo ""

az deployment group create \
    --resource-group $RESOURCE_GROUP_NAME \
    --name $DEPLOYMENT_NAME \
    --template-file main.bicep \
    --parameters $PARAMETERS_FILE \
    --output json > deployment-output.json

echo ""
echo -e "${GREEN}✓ Deployment completed successfully!${NC}"
echo ""

# Extract and display outputs
APP_SERVICE_URL=$(jq -r '.properties.outputs.appServiceUrl.value' deployment-output.json)
APP_SERVICE_NAME=$(jq -r '.properties.outputs.appServiceName.value' deployment-output.json)

echo -e "${GREEN}=== Deployment Summary ===${NC}"
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "App Service Name: $APP_SERVICE_NAME"
echo "App Service URL: $APP_SERVICE_URL"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Build your .NET application: cd ../src/ScoreBurrow.Web && dotnet publish -c Release"
echo "2. Deploy to Azure: az webapp deploy --resource-group $RESOURCE_GROUP_NAME --name $APP_SERVICE_NAME --src-path <path-to-zip>"
echo ""
