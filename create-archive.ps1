param (
  $name,
  $releaseDir,
  $publishDir,
  $releaseFilename,
  $version
)

$resourcesDir = 'resources'
$archiveDir = "$resourcesDir\$name"
$file = "$name" + '.dll'

$version | Out-File -FilePath "$archiveDir\version.txt"
Move-Item -Path "$publishDir\$file" -Destination $archiveDir
New-Item -Path . -Name "$releaseDir" -ItemType "directory" -Force
Compress-Archive -Path "$archiveDir" -DestinationPath "$releaseDir\$releaseFilename" -Force
