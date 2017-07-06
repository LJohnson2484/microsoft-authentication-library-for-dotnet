# Updating assemblyInfo and nuspec files with 
#     build number based on date and time 
#     and last submission hash from GitHub
#     and updating copyright information 

$d = Get-Date;
$date=($d.ToString("yy")-13).ToString() + $d.ToString("MMdd.HHmm");
$hash = git rev-parse HEAD

$filename = "src\ADAL.Common\CommonAssemblyInfo.cs"
$content = Get-Content $filename
$newContent = $content -replace "Microsoft Open Technologies", "Microsoft Corporation"
$newContent = $newContent + "`n" + "[assembly: AssemblyInformationalVersionAttribute(""$hash"")]"

$m = $newContent -match 'AssemblyFileVersion\(';
#trying to match something like assembly: AssemblyFileVersion("3.11.0.0").
$m = $m[0] -match 'AssemblyFileVersion\("([^\.]*.[^\.]*.[^\.]*.[^\"]*)';
$version = $matches[1]; #API version in the source	

$versionTokens = $version.Split(".");
$assemblyFileVersion = "{0}.{1}.{2}" -f ($versionTokens[0], $versionTokens[1], $date);
$newContent = $newContent -replace $version, $assemblyFileVersion

#replace last value with build number. Ex:3.10.0.75
$assemblyVersion =  "{0}.{1}.{2}.{3}" -f ($versionTokens[0], $versionTokens[1], $versionTokens[2], $env:BUILD_BUILDNUMBER);
$newContent = $newContent + "`n" + "[assembly: AssemblyVersion(""$assemblyVersion"")]";

Set-Content $filename $newContent
Write-Host "Modifying:" $filename;
Write-Host "Setting assembly version attribute:" $assemblyVersion;

$nugetVersion = "{0}.{1}.{2}" -f ($versionTokens[0], $versionTokens[1], $versionTokens[2]);

$filename = $env:TO_PACK_TARGET + "\Microsoft.IdentityModel.Clients.ActiveDirectory.nuspec"
$content = Get-Content $filename
$newContent = $content -replace "REPLACE_DURING_BUILD", $nugetVersion
Set-Content $filename $newContent
Write-Host "Modifying:" $filename;
Write-Host "Setting NuGet version:" $nugetVersion;