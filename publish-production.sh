#!/bin/bash

# =====================================================
#   Attendance System - Production Publish Script
#   LDAP-Only Authentication (No Database Required)
# =====================================================
# Usage: ./publish-production.sh [output-path]
# Example: ./publish-production.sh ./Publish_Production

set -e

OUTPUT_PATH="${1:-./Publish_Production}"

echo ""
echo "====== Attendance System - Production Deployment ======"
echo "Output Path: $OUTPUT_PATH"
echo "Configuration: Release"
echo "Runtime: win-x64 (Windows Server)"
echo ""

# Clean previous publish
if [ -d "$OUTPUT_PATH" ]; then
    echo "Removing previous publish directory..."
    rm -rf "$OUTPUT_PATH"
fi

# Change to Dashboard directory
cd src/Presentation/Dashboard

echo ""
echo "Building application in Release mode..."
dotnet build --configuration Release --no-incremental

echo ""
echo "Publishing application..."
dotnet publish \
    --configuration Release \
    --framework net10.0 \
    --runtime win-x64 \
    --self-contained \
    --output "$OUTPUT_PATH" \
    -p:EnvironmentName=Production \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false \
    -p:DebugType=none \
    -p:DebugSymbols=false

echo ""
echo "====== Publish Completed Successfully! ======"
echo "Output Directory: $OUTPUT_PATH"
echo ""
echo "Next Steps:"
echo "1. Copy contents to Production IIS server (e.g., C:\inetpub\wwwroot\AttendanceApp)"
echo "2. Set ASPNETCORE_ENVIRONMENT=Production environment variable"
echo "3. Update appsettings.Production.json with production settings:"
echo "   - Verify LDAP Domain configuration (Domain, AdminUsername, AdminPassword if needed)"
echo "   - Verify JWT SecretKey, Issuer, Audience"
echo "   - Update NIFI BaseUrl for production"
echo "4. Configure IIS Application Pool (.NET Runtime v4.0 or higher)"
echo "5. Create Website binding pointing to published directory"
echo "6. Start application via IIS or run Dashboard.exe"
echo ""
echo "Note: No database setup required. Authentication is LDAP-only."
echo ""
