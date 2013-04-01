param(
  $googleAnalyticsToken               = $env:JABBR_GOOGLE_ANALYTICS,
  $remoteDesktopAccountExpiration     = $env:JABBR_REMOTE_DESKTOP_ACCOUNT_EXPIRATION,
  $remoteDesktopCertificateThumbprint = $env:JABBR_REMOTE_DESKTOP_CERTIFICATE_THUMBPRINT,
  $remoteDesktopEnctyptedPassword     = $env:JABBR_REMOTE_DESKTOP_ENCRYPTED_PASSWORD,
  $remoteDesktopUsername              = $env:JABBR_REMOTE_DESKTOP_USERNAME,
  $sqlAzureConnectionString           = $env:JABBR_SQL_AZURE_CONNECTION_STRING,
  $sslCertificateThumbprint           = $env:JABBR_SSL_CERTIFICATE_THUMBPRINT,
  $googleKey                          = $env:JABBR_GOOGLE_LOGIN_KEY,
  $googleSecret                       = $env:JABBR_GOOGLE_LOGIN_SECRET,
  $facebookKey                        = $env:JABBR_FACEBOOK_LOGIN_KEY,
  $facebookSecret                     = $env:JABBR_FACEBOOK_LOGIN_SECRET,
  $twitterKey                         = $env:JABBR_TWITTER_LOGIN_KEY,
  $twitterSecret                      = $env:JABBR_TWITTER_LOGIN_SECRET,
  $encryptionKey                      = $env:JABBR_ENCRYPTION_KEY,
  $verificationKey                    = $env:JABBR_VERIFICATION_KEY,
  $blobStorageConnectionString        = $env:JABBR_BLOB_STORAGE_CONNECTION_STRING,
  $maxFileUploadBytes                 = $env:JABBR_MAX_UPLOAD_FILE_BYTES,
  $commitSha,
  $commitBranch
)

# Import Common Stuff
$ScriptRoot = (Split-Path -parent $MyInvocation.MyCommand.Definition)
. $ScriptRoot\_Common.ps1

# Validate Sutff
require-param -value $remoteDesktopAccountExpiration -paramName "remoteDesktopAccountExpiration"
require-param -value $remoteDesktopCertificateThumbprint -paramName "remoteDesktopCertificateThumbprint"
require-param -value $remoteDesktopEnctyptedPassword -paramName "remoteDesktopEnctyptedPassword"
require-param -value $remoteDesktopUsername -paramName "remoteDesktopUsername"
require-param -value $sqlAzureConnectionString -paramName "sqlAzureConnectionString"
require-param -value $encryptionKey -paramName "encryptionKey"
require-param -value $verificationKey -paramName "verificationKey"

# Helper Functions
function set-certificatethumbprint {
  param($path, $name, $value)
  $xml = [xml](get-content $path)
  $certificate = $xml.serviceconfiguration.role.Certificates.Certificate | where { $_.name -eq $name }
  $certificate.thumbprint = "$value"
  $resolvedPath = resolve-path($path) 
  $xml.save($resolvedPath)
} 

function set-configurationsetting {
  param($path, $name, $value)
  $xml = [xml](get-content $path)
  $setting = $xml.serviceconfiguration.role.configurationsettings.setting | where { $_.name -eq $name }
  $setting.value = "$value"
  $resolvedPath = resolve-path($path) 
  $xml.save($resolvedPath)
}

function set-connectionstring {
  param($path, $name, $value)
  $settings = [xml](get-content $path)
  $setting = $settings.configuration.connectionStrings.add | where { $_.name -eq $name }
  $setting.connectionString = "$value"
  $setting.providerName = "System.Data.SqlClient"
  $resolvedPath = resolve-path($path) 
  $settings.save($resolvedPath)
}

function set-appsetting {
    param($path, $name, $value)
    $settings = [xml](get-content $path)
    $setting = $settings.configuration.appSettings.selectsinglenode("add[@key='" + $name + "']")
    $setting.value = $value.toString()
    $resolvedPath = resolve-path($path) 
    $settings.save($resolvedPath)
}

function add-authprovider {
    param($path, $name, $key, $secret)
    $settings = [xml](get-content $path)
    $node = $settings.CreateElement("add")
    $node.SetAttribute("name", $name)
    $node.SetAttribute("key", $key)
    $node.SetAttribute("secret", $secret)
    $settings.configuration.authenticationProviders.providers.appendChild($node) | Out-Null
    $resolvedPath = resolve-path($path) 
    $settings.save($resolvedPath)
}

function set-releasemode {
  param($path)
  $xml = [xml](get-content $path)
  $compilation = $xml.configuration."system.web".compilation
  $compilation.debug = "false"
  $resolvedPath = resolve-path($path) 
  $xml.save($resolvedPath)
}

# Do Work Brah
$scriptPath = split-path $MyInvocation.MyCommand.Path
$rootPath = resolve-path(join-path $scriptPath "..")
$libPath = join-path $rootPath "lib"
$csdefFile = join-path $scriptPath "JabbR.csdef"
$websitePath = join-path $rootPath "JabbR"
$webConfigPath = join-path $websitePath "Web.config"
$webConfigBakPath = join-path $scriptPath "Web.config.bak"
$rolePropertiesPath = join-path $scriptPath "JabbR.RoleProperties.txt"
$cscfgPath = join-path $scriptPath "JabbR.cscfg"
$cscfgBakPath = join-path $scriptPath "JabbR.cscfg.bak"
$cspkgFolder = join-path $rootPath "_AzurePackage"
$cspkgFile = join-path $cspkgFolder "JabbR.cspkg"
$gitPath = join-path (programfiles-dir) "Git\bin\git.exe"
$binPath = join-path $websitePath "bin"

if ($commitSha -eq $null) {
    $commitSha = (& "$gitPath" rev-parse HEAD)
}

if ($commitBranch -eq $null) {
    $commitBranch = (& "$gitPath" name-rev --name-only HEAD)
}

if ((test-path $cspkgFolder) -eq $false) {
  mkdir $cspkgFolder | out-null
}

cp $webConfigPath $webConfigBakPath
cp $cscfgPath $cscfgBakPath
cp $libPath\signalr.exe $binPath\signalr.exe

# Set app settngs
set-appsetting -path $webConfigPath -name "jabbr:requireHttps" -value $true
set-appsetting -path $webConfigPath -name "jabbr:proxyImages" -value $true
set-appsetting -path $webConfigPath -name "jabbr:googleAnalytics" -value $googleAnalyticsToken
set-appsetting -path $webConfigPath -name "jabbr:releaseBranch" -value $commitBranch
set-appsetting -path $webConfigPath -name "jabbr:releaseSha" -value $commitSha
set-appsetting -path $webConfigPath -name "jabbr:releaseTime" -value (Get-Date -format "MM/dd/yyyy HH:mm")

# Set encryption keys
set-appsetting -path $webConfigPath -name "jabbr:encryptionKey" -value $encryptionKey
set-appsetting -path $webConfigPath -name "jabbr:verificationKey" -value $verificationKey

# File upload

if($blobStorageConnectionString)
{
  set-appsetting -path $webConfigPath -name "jabbr:azureblobStorageConnectionString" -value $blobStorageConnectionString
}

if($maxFileUploadBytes)
{
  set-appsetting -path $webConfigPath -name "jabbr:maxFileUploadBytes" -value $maxFileUploadBytes
}

# Set auth providers
if($googleKey -and $googleSecret)
{
  add-authprovider -path $webConfigPath -name "Google" -key $googleKey -secret $googleSecret
}

if($twitterKey -and $twitterSecret)
{
  add-authprovider -path $webConfigPath -name "Twitter" -key $twitterKey -secret $twitterSecret
  
}

if($facebookKey -and $facebookSecret)
{
  add-authprovider -path $webConfigPath -name "Facebook" -key $facebookKey -secret $facebookSecret
}

# Set cscfg settings
set-configurationsetting -path $cscfgPath -name "Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" -value $remoteDesktopAccountExpiration
set-certificatethumbprint -path $cscfgPath -name "Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" -value $remoteDesktopCertificateThumbprint
set-configurationsetting -path $cscfgPath -name "Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" -value $remoteDesktopEnctyptedPassword
set-configurationsetting -path $cscfgPath -name "Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" -value $remoteDesktopUsername
set-connectionstring -path $webConfigPath -name "JabbR" -value $sqlAzureConnectionString
set-releasemode $webConfigPath

if($sslCertificateThumbprint) {
  set-certificatethumbprint -path $cscfgPath -name "jabbr" -value $sslCertificateThumbprint
}

# Find the most recent SDK version
$azureSdkPath = Get-AzureSdkPath $azureSdkPath

& "$azureSdkPath\bin\cspack.exe" "$csdefFile" /out:"$cspkgFile" /role:"Website;$websitePath" /sites:"Website;Web;$websitePath" /rolePropertiesFile:"Website;$rolePropertiesPath"

cp $cscfgPath $cspkgFolder

cp $webConfigBakPath $webConfigPath
cp $cscfgBakPath $cscfgPath

print-success("Azure package and configuration dropped to $cspkgFolder.")
write-host ""

Exit 0
