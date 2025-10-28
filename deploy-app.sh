#!/bin/bash

# Azure App Service Deployment Script for ScoreBurrow.Web
# Prerequisites: 
#   - Azure CLI installed and logged in (az login)
#   - Infrastructure deployed (run infrastructure/deploy.sh first)
#   - .NET SDK installed

set -e

# Configuration
RESOURCE_GROUP_NAME="score-burrow-rg"
PROJECT_PATH="src/ScoreBurrow.Web"
PUBLISH_PATH="src/ScoreBurrow.Web/bin/Release/net8.0/publish"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOYMENT_PACKAGE="$SCRIPT_DIR/scoreborrow-web.zip"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== ScoreBurrow.Web Deployment Script ===${NC}"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Please install it from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET SDK is not installed${NC}"
    echo "Please install it from: https://dotnet.microsoft.com/download"
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

# Check if resource group exists
echo -e "${YELLOW}Checking if resource group exists...${NC}"
if ! az group exists --name $RESOURCE_GROUP_NAME | grep -q "true"; then
    echo -e "${RED}Error: Resource group '$RESOURCE_GROUP_NAME' does not exist${NC}"
    echo "Please run infrastructure/deploy.sh first to create the infrastructure"
    exit 1
fi
echo -e "${GREEN}✓ Resource group exists: $RESOURCE_GROUP_NAME${NC}"
echo ""

# Get App Service name from the resource group
echo -e "${YELLOW}Finding App Service...${NC}"
APP_SERVICE_NAME=$(az webapp list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" -o tsv)

if [ -z "$APP_SERVICE_NAME" ]; then
    echo -e "${RED}Error: No App Service found in resource group '$RESOURCE_GROUP_NAME'${NC}"
    echo "Please ensure infrastructure is deployed correctly"
    exit 1
fi

echo -e "${GREEN}✓ Found App Service: $APP_SERVICE_NAME${NC}"
echo ""

# Clean previous publish output
echo -e "${YELLOW}Cleaning previous build artifacts...${NC}"
if [ -d "$PUBLISH_PATH" ]; then
    rm -rf "$PUBLISH_PATH"
fi
if [ -f "$DEPLOYMENT_PACKAGE" ]; then
    rm -f "$DEPLOYMENT_PACKAGE"
fi
echo -e "${GREEN}✓ Clean completed${NC}"
echo ""

# Build and publish the application
echo -e "${YELLOW}Building and publishing application...${NC}"
dotnet publish "$PROJECT_PATH" \
    --configuration Release \
    --output "$PUBLISH_PATH" \
    --runtime linux-x64 \
    --self-contained false

echo -e "${GREEN}✓ Application published successfully${NC}"
echo ""

# Create deployment package
echo -e "${YELLOW}Creating deployment package...${NC}"
cd "$PUBLISH_PATH"
zip -r "$DEPLOYMENT_PACKAGE" . > /dev/null
cd - > /dev/null
echo -e "${GREEN}✓ Deployment package created: $(basename $DEPLOYMENT_PACKAGE)${NC}"
echo ""

# Deploy to Azure App Service
echo -e "${YELLOW}Deploying to Azure App Service...${NC}"
echo -e "${BLUE}This may take a few minutes...${NC}"
echo ""

az webapp deploy \
    --resource-group $RESOURCE_GROUP_NAME \
    --name $APP_SERVICE_NAME \
    --src-path "$DEPLOYMENT_PACKAGE" \
    --type zip \
    --async false

echo ""
echo -e "${GREEN}✓ Deployment completed successfully!${NC}"
echo ""

# Get App Service URL
APP_SERVICE_URL=$(az webapp show \
    --resource-group $RESOURCE_GROUP_NAME \
    --name $APP_SERVICE_NAME \
    --query "defaultHostName" -o tsv)

echo -e "${GREEN}=== Deployment Summary ===${NC}"
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "App Service Name: $APP_SERVICE_NAME"
echo "App Service URL: https://$APP_SERVICE_URL"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Wait a few moments for the app to start"
echo "2. Open your browser and navigate to: https://$APP_SERVICE_URL"
echo "3. Monitor logs: az webapp log tail --resource-group $RESOURCE_GROUP_NAME --name $APP_SERVICE_NAME"
echo ""

# Clean up deployment package
echo -e "${YELLOW}Cleaning up deployment package...${NC}"
rm -f "$DEPLOYMENT_PACKAGE"
echo -e "${GREEN}✓ Cleanup completed${NC}"
echo ""

echo -e "${GREEN}🎉 Deployment process completed!${NC}"
