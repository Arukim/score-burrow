@description('Location for the Container Apps environment')
param location string

@description('Consumption-only Container Apps environment name')
param environmentName string

@description('App Service base URL, no trailing slash')
param appUrl string

var trimCount = endsWith(appUrl, '/') ? 1 : 0
var baseUrl = substring(appUrl, 0, max(length(appUrl) - trimCount, 0))
var image = 'mcr.microsoft.com/powershell:7.5-alpine-3.20'

// Retries cover an F1 cold start and a SQL resume. Exit on the first HTTP 2xx.
var pingScript = '''
$ErrorActionPreference = 'Stop'
$url = $env:PING_URL
$timeout = [int]$env:TIMEOUT_SEC
for ($i = 1; $i -le 3; $i++) {
  try {
    $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec $timeout
    if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
      Write-Output $response.Content
      exit 0
    }
    Write-Output "attempt $i status $($response.StatusCode)"
  } catch {
    Write-Output "attempt $i failed: $($_.Exception.Message)"
  }
  if ($i -lt 3) { Start-Sleep -Seconds 15 }
}
exit 1
'''

// Consumption workload profile only. No Log Analytics workspace.
resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: environmentName
  location: location
  properties: {
    zoneRedundant: false
  }
}

resource pingJob 'Microsoft.App/jobs@2025-01-01' = {
  name: 'score-burrow-ping'
  location: location
  properties: {
    environmentId: environment.id
    configuration: {
      replicaTimeout: 420
      replicaRetryLimit: 1
      triggerType: 'Schedule'
      scheduleTriggerConfig: {
        // Saturday and Sunday, 06:00–16:50 UTC. 16:00–02:50 AEST (17:00–03:50 AEDT).
        cronExpression: '*/10 6-16 * * 0,6'
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'ping'
          image: image
          command: [
            'pwsh'
            '-NoLogo'
            '-NonInteractive'
            '-Command'
            pingScript
          ]
          env: [
            {
              name: 'PING_URL'
              value: '${baseUrl}/health'
            }
            {
              name: 'TIMEOUT_SEC'
              value: '90'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

resource warmSqlJob 'Microsoft.App/jobs@2025-01-01' = {
  name: 'score-burrow-warm-sql'
  location: location
  properties: {
    environmentId: environment.id
    configuration: {
      replicaTimeout: 420
      replicaRetryLimit: 1
      triggerType: 'Schedule'
      scheduleTriggerConfig: {
        // 06:50 UTC = 16:50 AEST, before players arrive. One SQL resume per session.
        cronExpression: '50 6 * * 0,6'
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'ping'
          image: image
          command: [
            'pwsh'
            '-NoLogo'
            '-NonInteractive'
            '-Command'
            pingScript
          ]
          env: [
            {
              name: 'PING_URL'
              value: '${baseUrl}/health/ready'
            }
            {
              name: 'TIMEOUT_SEC'
              value: '120'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

output environmentName string = environment.name
output pingJobName string = pingJob.name
output warmSqlJobName string = warmSqlJob.name
