$config = 'Debug'
$nuspec_file = 'PostfixTemplates.nuspec'
$package_id = 'ReSharper.Postfix'

# 9.1
# nuget pack $nuspec_file -Exclude 'PostfixTemplates\bin.R92\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[2.0,3.0);PackageId=$package_id.R90"

# 9.2
# nuget pack $nuspec_file -Exclude 'PostfixTemplates\bin.R91\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[3.0,4.0);PackageId=$package_id.R90"