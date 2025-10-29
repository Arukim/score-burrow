#!/bin/bash

# Script to add Entity Framework Core migrations using ScoreBurrow.Web project for connection strings
# Usage: ./add-migration.sh <migration-name>

if [ $# -eq 0 ]; then
    echo "Usage: $0 <migration-name>"
    echo "Example: $0 AddNewFeature"
    exit 1
fi

MIGRATION_NAME=$1

echo "Adding migration '$MIGRATION_NAME' using ScoreBurrow.Web project for connection string..."

cd src
dotnet ef migrations add $MIGRATION_NAME --project ScoreBurrow.Data/ScoreBurrow.Data.csproj --startup-project ScoreBurrow.Web/ScoreBurrow.Web.csproj

if [ $? -eq 0 ]; then
    echo "Migration '$MIGRATION_NAME' created successfully!"
    echo "Migration files can be found in: src/ScoreBurrow.Data/Migrations/"
else
    echo "Migration creation failed."
    exit 1
fi
