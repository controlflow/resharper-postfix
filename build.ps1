$config = 'Debug'
$nuspec_file = 'PostfixTemplates.nuspec'
$package_id = 'ReSharper.Postfix'

nuget pack $nuspec_file -Exclude 'PostfixTemplates\bin.R90\**' -Exclude 'PostfixTemplates\bin.R91\**' -Properties "Configuration=$config;ReSharperDep=ReSharper;ReSharperVer=8.0;PackageId=$package_id"

# nuget pack $nuspec_file -Exclude 'PostfixTemplates\bin.R8*\**' -Exclude 'PostfixTemplates\bin.R91\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[1.0,2.0);PackageId=$package_id.R90"

nuget pack $nuspec_file -Exclude 'PostfixTemplates\bin.R8*\**' -Exclude 'PostfixTemplates\bin.R90\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[2.0,3.0);PackageId=$package_id.R90"