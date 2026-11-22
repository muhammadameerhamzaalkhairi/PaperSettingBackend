param([Parameter(Mandatory)][string]$SqlPackage)
$ErrorActionPreference='Stop'
$instance='BIITTest'+[Guid]::NewGuid().ToString('N').Substring(0,12)
$database=$instance
$server="(localdb)\$instance"
$api=Join-Path $PSScriptRoot '../BIITPaperSettingAPI/BIITPaperSettingAPI'
$created=$false
try {
    # Fresh private instance keeps every test write away from the project database.
    sqllocaldb create $instance -s
    if ($LASTEXITCODE -ne 0) { throw 'Test instance creation failed' }
    $created=$true
    foreach ($file in 'Tables.sql','00_Install.sql','11_Dummy_Data.sql') {
        $sql=(Get-Content -Raw (Join-Path $SqlPackage $file)).Replace('[BIIT_FYP_Updated]',"[$database]").Replace('[BIIT_FYP_PaperSetting]',"[$database]")
        $result=$sql | sqlcmd -S $server -E -C -b -x; if ($LASTEXITCODE -ne 0) { $result | Select-String "^Msg |^Sqlcmd:|Error|failed" }
        if ($LASTEXITCODE -ne 0) { throw "Fixture setup failed at $file" }
    }
    sqlcmd -S $server -E -C -b -d $database -v "TargetDatabase=$database" -i "$PSScriptRoot/../Sql/003_DatesheetCourses.sql"
    if ($LASTEXITCODE -ne 0) { throw 'Phase 3 SQL installation failed' }
    sqlcmd -S $server -E -C -b -d $database -v "TargetDatabase=$database" -i "$PSScriptRoot/../Sql/004_CoverageWorkspace.sql"
    if ($LASTEXITCODE -ne 0) { throw 'Phase 4 SQL installation failed' }
    $pipe=((sqllocaldb info $instance | Select-String 'np:\\\\').Line -split 'np:',2)[1].Trim()
    $env:Phase1Admin="Server=$server;Database=$database;Integrated Security=true;Encrypt=false"
    $password='T3st!'+[Guid]::NewGuid().ToString('N')
    $permissionSql="CREATE LOGIN phase1_staff WITH PASSWORD='$password'; CREATE USER phase1_staff FOR LOGIN phase1_staff; ALTER ROLE biit_auth_executor ADD MEMBER phase1_staff; ALTER ROLE biit_api_executor ADD MEMBER phase1_staff;"
    $permissionSql | sqlcmd -S $server -E -C -b -d $database | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Restricted principal setup failed' }
    $env:ConnectionStrings__DefaultConnection="Server=np:$pipe;Database=$database;User ID=phase1_staff;Password=$password;Encrypt=false"
    $env:ConnectionStrings__Authentication=$env:ConnectionStrings__DefaultConnection
    $env:Jwt__SigningKey=[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    $env:Jwt__Issuer='BIIT.PaperSetting'
    $env:Jwt__Audience='BIIT.Staff'
    dotnet build $api
    if ($LASTEXITCODE -ne 0) { throw 'API build failed' }
    $env:Phase1Dll=(Resolve-Path "$api/bin/Debug/net10.0/BIITPaperSettingAPI.dll").Path
    dotnet test "$PSScriptRoot/Phase1.Tests.csproj"
    if ($LASTEXITCODE -ne 0) { throw 'Phase 1 tests failed' }
} finally {
    if ($created) {
        sqlcmd -S $server -E -C -Q "IF DB_ID('$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END" | Out-Null
        sqllocaldb stop $instance -k
        sqllocaldb delete $instance
    }
}
