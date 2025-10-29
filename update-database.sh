#!/bin/bash

# Script to update Entity Framework Core database using ScoreBurrow.Web project for connection strings
# Usage: ./update-database.sh [migration-name]

MIGRATION_NAME=${1:-"(no argument, will update to latest)"}

if [ $# -eq 0 ]; then
    echo "Updating database to latest migration using ScoreBurrow.Web project..."
    MIGRATION_ARG=""
else
    echo "Updating database to migration '$MIGRATION_NAME' using ScoreBurrow.Web project..."
    MIGRATION_ARG="--migration $MIGRATION_NAME"
fi

cd src/ScoreBurrow.Web
dotnet ef database update $MIGRATION_ARG --project ../ScoreBurrow.Data/ScoreBurrow.Data.csproj

if [ $? -eq 0 ]; then
    if [ $# -eq 0 ]; then
        echo "Database updated to latest migration successfully!"
    else
        echo "Database updated to migration '$MIGRATION_NAME' successfully!"
    fi
    echo "Please wait a moment for the database to finish applying changes."
else
    echo "Database update failed."
    exit 1
fi
