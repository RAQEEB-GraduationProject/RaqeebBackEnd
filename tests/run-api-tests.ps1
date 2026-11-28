<#
  RAQEEB backend - API tests for phases 1-3
  (update/create patient, unified patient contract, user account update).

  HOW TO RUN
    1. Start the API in Visual Studio with the "http" profile (F5) -> http://localhost:5150
    2. Open PowerShell in the project folder and run:
         powershell -ExecutionPolicy Bypass -File .\tests\run-api-tests.ps1
    3. Type the admin password when asked (it stays on your PC).
    4. The summary is copied to your clipboard: paste it back to Claude.

  OPTIONS
    -BaseUrl        default http://localhost:5150
    -AdminUserName  default admin
    -DemoPassword   default Demo@12345 (DemoSeed:Password if you changed it)

  IMPORTANT
    This uses the SHARED team database. The script saves the demo patient first and
    restores it at the end (even if a test fails), and deactivates the test accounts it creates.
    Test 1 (build) is manual: build in Visual Studio before running this.
#>
param(
    [string]$BaseUrl = "http://localhost:5150",
    [string]$AdminUserName = "admin",
    [string]$DemoPassword = "Demo@12345"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

$DemoPatientId  = "3f6c2b1a-0001-4d1e-8a00-000000000001"
$DemoPatient2Id = "3f6c2b1a-0002-4d1e-8a00-000000000002"
$RunId          = Get-Date -Format "MMddHHmmss"
$TestPassword   = "Test@12345"
$NineFields     = "dateOfBirth", "gender", "bloodGroup", "medicalRecordNumber", "diagnosis", "stage", "treatment", "protocol", "riskLevel"

# Windows PowerShell 5.1 needs TLS 1.2 switched on for https://localhost.
try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch { }

# Redirects are NOT followed: when .NET follows a redirect it drops the
# Authorization header, so every authenticated call would get 401.
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$client = New-Object System.Net.Http.HttpClient $handler
$client.Timeout = [TimeSpan]::FromSeconds(60)
$results = New-Object System.Collections.Generic.List[object]

# ---------------------------------------------------------------- helpers

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null)

    $httpMethod = New-Object System.Net.Http.HttpMethod $Method
    $request = New-Object System.Net.Http.HttpRequestMessage $httpMethod, ($BaseUrl + $Path)

    if ($Token) {
        $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue "Bearer", $Token
    }

    if ($null -ne $Body) {
        if ($Body -is [string]) { $json = $Body } else { $json = $Body | ConvertTo-Json -Depth 10 -Compress }
        $request.Content = New-Object System.Net.Http.StringContent $json, ([System.Text.Encoding]::UTF8), "application/json"
    }

    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
    }
    catch {
        throw "Cannot reach $BaseUrl$Path - is the API running? ($($_.Exception.Message))"
    }

    $raw = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $parsed = $null
    if ($raw) { try { $parsed = $raw | ConvertFrom-Json } catch { $parsed = $null } }

    $location = $null
    if ($response.Headers.Location) { $location = $response.Headers.Location.ToString() }

    # On 401 ASP.NET explains why in this header, e.g. Bearer error="invalid_token", error_description="..."
    $authError = $null
    if ($response.Headers.WwwAuthenticate.Count -gt 0) { $authError = ($response.Headers.WwwAuthenticate | ForEach-Object { $_.ToString() }) -join "; " }

    [pscustomobject]@{ Status = [int]$response.StatusCode; Json = $parsed; Raw = $raw; Location = $location; AuthError = $authError }
}

# Shows what is inside a JWT (claims only, never the signature) for diagnosing 401s.
function Get-TokenClaims([string]$Token) {
    try {
        $part = $Token.Split(".")[1].Replace("-", "+").Replace("_", "/")
        switch ($part.Length % 4) { 2 { $part += "==" } 3 { $part += "=" } }
        return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($part))
    }
    catch { return "(could not read token)" }
}

function Add-Result {
    param([string]$Id, [string]$Name, [bool]$Ok, [string]$Detail = "", [switch]$Warn)

    if ($Warn) { $state = "WARN" } elseif ($Ok) { $state = "PASS" } else { $state = "FAIL" }
    $results.Add([pscustomobject]@{ Test = $Id; Result = $state; Name = $Name; Detail = $Detail })

    $color = @{ PASS = "Green"; FAIL = "Red"; WARN = "Yellow" }[$state]
    Write-Host ("[{0}] {1,-3} {2}" -f $state, $Id, $Name) -ForegroundColor $color
    if ($Detail -and $state -ne "PASS") { Write-Host ("            " + $Detail) -ForegroundColor DarkGray }
}

function Get-ErrorFields($r) {
    if ($r.Json -and $r.Json.errors) { return @($r.Json.errors | ForEach-Object { [string]$_.field }) }
    return @()
}

function Test-ErrorField($r, [string]$Name) {
    return @(Get-ErrorFields $r | Where-Object { $_ -ieq $Name -or $_ -like "*.$Name" }).Count -gt 0
}

function Get-Short($r) {
    $text = [string]$r.Raw
    if ($text.Length -gt 300) { $text = $text.Substring(0, 300) + "..." }
    $result = "HTTP $($r.Status): $text"
    if ($r.AuthError) { $result += " [WWW-Authenticate: $($r.AuthError)]" }
    return $result
}

function Get-DateText($v) {
    if ($null -eq $v) { return $null }
    if ($v -is [datetime]) { return $v.ToString("yyyy-MM-dd") }
    $s = [string]$v
    if ($s.Length -ge 10) { return $s.Substring(0, 10) }
    return $s
}

function Test-Prop($obj, [string]$Name) {
    return ($null -ne $obj) -and ($obj.PSObject.Properties.Name -contains $Name)
}

function Test-Same($a, $b) {
    return ($a | ConvertTo-Json -Depth 10 -Compress) -eq ($b | ConvertTo-Json -Depth 10 -Compress)
}

function Test-SameProfile($a, $b) {
    foreach ($f in $NineFields) {
        if ($f -eq "dateOfBirth") {
            if ((Get-DateText $a.dateOfBirth) -ne (Get-DateText $b.dateOfBirth)) { return $false }
        }
        elseif ($a.$f -ne $b.$f) { return $false }
    }
    return $true
}

function New-PatientBody($p, [hashtable]$Override = @{}) {
    $body = [ordered]@{
        dateOfBirth         = Get-DateText $p.dateOfBirth
        gender              = $p.gender
        bloodGroup          = $p.bloodGroup
        medicalRecordNumber = $p.medicalRecordNumber
        diagnosis           = $p.diagnosis
        stage               = $p.stage
        treatment           = $p.treatment
        protocol            = $p.protocol
        riskLevel           = $p.riskLevel
    }
    foreach ($k in $Override.Keys) { $body[$k] = $Override[$k] }
    return $body
}

function Get-ExpectedAge($dob) {
    $text = Get-DateText $dob
    if (-not $text) { return $null }
    $birth = [datetime]::ParseExact($text, "yyyy-MM-dd", [Globalization.CultureInfo]::InvariantCulture)
    $today = (Get-Date).Date
    $age = $today.Year - $birth.Year
    if ($birth -gt $today.AddYears(-$age)) { $age-- }
    return $age
}

# ---------------------------------------------------------------- run

Write-Host ""
Write-Host "RAQEEB API tests  -  $BaseUrl  -  run $RunId" -ForegroundColor Cyan
Write-Host "(Test 1 = build in Visual Studio, done by you before this run)" -ForegroundColor DarkGray
Write-Host ""

# The "https" launch profile redirects http://localhost:5150 to https://localhost:7160
# (app.UseHttpsRedirection). Detect that and use the https address directly.
$probe = Invoke-Api GET "/api/Users/me"
if ($probe.Status -ge 300 -and $probe.Status -lt 400 -and $probe.Location) {
    $target = [Uri]$probe.Location
    $BaseUrl = $target.GetLeftPart([UriPartial]::Authority)
    Write-Host "The API redirects to $BaseUrl (https profile) - using that address." -ForegroundColor Yellow
}

$secure = Read-Host "Admin password for '$AdminUserName'" -AsSecureString
$adminPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))

$admin = $null; $doctor = $null; $original = $null
$testUser1 = $null; $testUser2 = $null
$pPath = "/api/Patients/$DemoPatientId"

try {
    # ---------- Setup
    $r = Invoke-Api POST "/api/Auth/login" ([ordered]@{ userName = $AdminUserName; password = $adminPassword })
    Add-Result "2" "API is running and answers" ($r.Status -gt 0) (Get-Short $r)
    $admin = $r.Json.data.token
    Add-Result "3" "Log in as admin" ($r.Status -eq 200 -and [bool]$admin) (Get-Short $r)
    if (-not $admin) { throw "Admin login failed - stopping (check the password)." }

    $r = Invoke-Api GET $pPath -Token $admin
    $original = $r.Json.data
    $detail4 = Get-Short $r
    if ($r.Status -eq 401) { $detail4 += " | token claims: " + (Get-TokenClaims $admin) }
    Add-Result "4" "Save the demo patient's current values" ($r.Status -eq 200 -and $null -ne $original) $detail4
    if ($null -eq $original) { throw "Demo patient $DemoPatientId not found - is the Development demo data seeded?" }

    # ---------- Phase 1: update patient
    $r = Invoke-Api PUT $pPath "{}" $admin
    $notListed = @($NineFields | Where-Object { -not (Test-ErrorField $r $_) })
    $after = (Invoke-Api GET $pPath -Token $admin).Json.data
    Add-Result "5" "PUT {} is rejected and changes nothing" `
        ($r.Status -eq 400 -and $r.Json.errorCode -eq "VALIDATION_ERROR" -and $notListed.Count -eq 0 -and (Test-SameProfile $after $original)) `
        ("not listed in errors: " + ($notListed -join ", ") + " | " + (Get-Short $r))

    $b = New-PatientBody $original
    $b.Remove("gender")
    $r = Invoke-Api PUT $pPath $b $admin
    $fields = Get-ErrorFields $r
    Add-Result "6" "One missing field (gender) is rejected" `
        ($r.Status -eq 400 -and (Test-ErrorField $r "gender") -and $fields.Count -eq 1) `
        ("error fields: " + ($fields -join ", ") + " | " + (Get-Short $r))

    $r = Invoke-Api PUT $pPath (New-PatientBody $original) $admin
    Add-Result "7" "Full body keeps every value" ($r.Status -eq 200 -and (Test-SameProfile $r.Json.data $original)) (Get-Short $r)

    $r = Invoke-Api PUT $pPath (New-PatientBody $original @{ stage = $null }) $admin
    Add-Result "8" "Explicit null clears a value (stage)" ($r.Status -eq 200 -and $null -eq $r.Json.data.stage) (Get-Short $r)

    $r = Invoke-Api PUT $pPath (New-PatientBody $original @{ gender = "   " }) $admin
    Add-Result "9" "Blank text becomes null (gender)" ($r.Status -eq 200 -and $null -eq $r.Json.data.gender) (Get-Short $r)

    $r = Invoke-Api PUT $pPath (New-PatientBody $original @{ gender = "  Female  " }) $admin
    Add-Result "10" "Text is trimmed (gender)" ($r.Status -eq 200 -and $r.Json.data.gender -ceq "Female") (Get-Short $r)

    $future = (Get-Date).AddYears(1).ToString("yyyy-MM-dd")
    $r = Invoke-Api PUT $pPath (New-PatientBody $original @{ dateOfBirth = $future }) $admin
    Add-Result "11" "Future birth date is rejected" ($r.Status -eq 400 -and (Test-ErrorField $r "dateOfBirth")) (Get-Short $r)

    $r = Invoke-Api PUT $pPath (New-PatientBody $original @{ dateOfBirth = "abc" }) $admin
    $msgOk = @($r.Json.errors | Where-Object { $_.message -eq "Invalid value or format." }).Count -gt 0
    Add-Result "12" "Wrong date format gives a clean error" `
        ($r.Status -eq 400 -and (Test-ErrorField $r "dateOfBirth") -and $msgOk -and ($r.Raw -notmatch "RAQEEB\.DTOs")) (Get-Short $r)

    $p2 = (Invoke-Api GET "/api/Patients/$DemoPatient2Id" -Token $admin).Json.data
    if ($p2 -and $p2.medicalRecordNumber) {
        $r = Invoke-Api PUT $pPath (New-PatientBody $original @{ medicalRecordNumber = $p2.medicalRecordNumber }) $admin
        Add-Result "13" "Duplicate medical record number" ($r.Status -eq 409 -and $r.Json.errorCode -eq "MRN_ALREADY_EXISTS") (Get-Short $r)
    }
    else {
        Add-Result "13" "Duplicate medical record number" $false "Demo patient 2 has no medical record number to collide with - skipped" -Warn
    }

    $r1 = Invoke-Api PUT $pPath (New-PatientBody $original @{ bloodGroup = "X+" }) $admin
    $r2 = Invoke-Api PUT $pPath (New-PatientBody $original @{ bloodGroup = " O+ " }) $admin
    Add-Result "14" "Blood group rules (X+ rejected, ' O+ ' saved as O+)" `
        ($r1.Status -eq 400 -and (Test-ErrorField $r1 "bloodGroup") -and $r2.Status -eq 200 -and $r2.Json.data.bloodGroup -ceq "O+") `
        ("X+: " + (Get-Short $r1) + " | O+: " + (Get-Short $r2))

    # test 15 (restore) runs in the finally block below

    # ---------- Phase 1: create patient
    $u1Name = "test.patient1.$RunId"
    $r = Invoke-Api POST "/api/Auth/register" ([ordered]@{
            userName = $u1Name; fullName = "Test Patient"; email = "$u1Name@raqeeb.demo"
            phoneNumber = "01011111111"; password = $TestPassword; role = "Patient" }) $admin
    $testUser1 = $r.Json.data.userId
    Add-Result "16" "Register a test patient account" ($r.Status -eq 200 -and [bool]$testUser1) (Get-Short $r)

    $r = Invoke-Api POST "/api/Patients" ([ordered]@{ userId = $testUser1 }) $admin
    $newPatient = $r.Json.data
    Add-Result "17" "Create profile with userId only (201 + Location, nulls)" `
        ($r.Status -eq 201 -and $r.Location -like "*/api/Patients/*" -and $null -ne $newPatient -and
         $null -eq $newPatient.medicalRecordNumber -and $null -eq $newPatient.gender -and
         $newPatient.phoneNumber -eq "01011111111" -and $null -eq $newPatient.age) `
        ("Location: $($r.Location) | " + (Get-Short $r))

    $r = Invoke-Api POST "/api/Patients" ([ordered]@{ userId = $testUser1 }) $admin
    Add-Result "18" "Second profile for the same account" ($r.Status -eq 409 -and $r.Json.errorCode -eq "PATIENT_PROFILE_EXISTS") (Get-Short $r)

    $u2Name = "test.patient2.$RunId"
    $r = Invoke-Api POST "/api/Auth/register" ([ordered]@{
            userName = $u2Name; fullName = $null; email = "$u2Name@raqeeb.demo"
            phoneNumber = ""; password = $TestPassword; role = "Patient" }) $admin
    $testUser2 = $r.Json.data.userId
    $d = Invoke-Api PATCH "/api/Users/$testUser2/deactivate" -Token $admin
    $r = Invoke-Api POST "/api/Patients" ([ordered]@{ userId = $testUser2 }) $admin
    Add-Result "19" "Inactive account is refused" `
        ([bool]$testUser2 -and $d.Status -eq 204 -and $r.Status -eq 400 -and $r.Json.errorCode -eq "USER_INACTIVE") `
        ("deactivate: HTTP $($d.Status) | " + (Get-Short $r))

    $r = Invoke-Api POST "/api/Patients" ([ordered]@{ userId = $testUser2; dateOfBirth = $future }) $admin
    Add-Result "20" "Future birth date on create" ($r.Status -eq 400 -and (Test-ErrorField $r "dateOfBirth")) (Get-Short $r)

    # ---------- Phase 2: unified patient contract
    $r = Invoke-Api GET "/api/Patients" -Token $admin
    $list = @($r.Json.data)
    $need = "userId", "userName", "fullName", "email", "phoneNumber", "id", "hospitalId", "dateOfBirth", "age", "gender",
            "bloodGroup", "medicalRecordNumber", "diagnosis", "stage", "treatment", "protocol", "riskLevel", "isActive", "createdAt"
    $absent = @($need | Where-Object { -not (Test-Prop $list[0] $_) })
    Add-Result "21" "Patient list has all 19 fields" ($r.Status -eq 200 -and $list.Count -gt 0 -and $absent.Count -eq 0) `
        ("missing: " + ($absent -join ", ") + " | " + (Get-Short $r))

    $byId = (Invoke-Api GET $pPath -Token $admin).Json.data
    $inList = $list | Where-Object { $_.id -eq $DemoPatientId } | Select-Object -First 1
    Add-Result "22" "Same object by id and in the list" ($null -ne $byId -and (Test-Same $byId $inList)) "objects differ"

    $expectedAge = Get-ExpectedAge $byId.dateOfBirth
    Add-Result "23" "Age is calculated from the birth date" ($byId.age -eq $expectedAge) "age=$($byId.age), expected=$expectedAge"

    $listed1 = $list | Where-Object { $_.userId -eq $testUser1 } | Select-Object -First 1
    $user2 = (Invoke-Api GET "/api/Users/$testUser2" -Token $admin).Json.data
    Add-Result "24" "fullName is returned as stored (no userName fallback)" `
        ($listed1.fullName -ceq "Test Patient" -and $null -ne $user2 -and $null -eq $user2.fullName -and $user2.userName -eq $u2Name) `
        "patient1 fullName='$($listed1.fullName)', user2 fullName='$($user2.fullName)'"

    $r = Invoke-Api GET "$pPath/overview" -Token $admin
    $ov = $r.Json.data
    $teamOk = (Test-Prop $ov.careTeam "treatingDoctor") -and (Test-Prop $ov.careTeam "nurse") -and (Test-Prop $ov.careTeam "primaryCaregiver")
    Add-Result "25" "Overview: patient + careTeam + treatmentProgress null" `
        ($r.Status -eq 200 -and (Test-Same $ov.patient $byId) -and $teamOk -and (Test-Prop $ov "treatmentProgress") -and $null -eq $ov.treatmentProgress) `
        (Get-Short $r)

    $names = "$($ov.careTeam.treatingDoctor.userName) / $($ov.careTeam.nurse.userName) / $($ov.careTeam.primaryCaregiver.userName)"
    if ($names -eq "demo.doctor / demo.nurse / demo.caregiver") {
        Add-Result "25b" "Care team = demo.doctor / demo.nurse / demo.caregiver" $true
    }
    else {
        Add-Result "25b" "Care team = demo.doctor / demo.nurse / demo.caregiver" $false "got: $names (demo assignments may have been changed)" -Warn
    }

    Add-Result "26" "Overview: old fields still present" `
        ((Test-Prop $ov "patientHeader") -and (Test-Prop $ov "personalInfo") -and (Test-Prop $ov "clinicalOverview") -and [bool]$ov.patientHeader.fullName) `
        "old properties missing or empty"

    $ov2 = (Invoke-Api GET "/api/Patients/$DemoPatient2Id/overview" -Token $admin).Json.data
    if ($ov2 -and $null -eq $ov2.careTeam.treatingDoctor -and $null -eq $ov2.careTeam.nurse -and $null -eq $ov2.careTeam.primaryCaregiver) {
        Add-Result "27" "Patient with no care team" $true
    }
    else {
        Add-Result "27" "Patient with no care team" $false "demo patient 2 has assignments now (data changed?)" -Warn
    }

    $r = Invoke-Api POST "/api/Auth/login" ([ordered]@{ userName = "demo.doctor"; password = $DemoPassword })
    $doctor = $r.Json.data.token
    if ($doctor) {
        $a = Invoke-Api GET "$pPath/overview" -Token $doctor
        $c = Invoke-Api GET "/api/Patients/$DemoPatient2Id/overview" -Token $doctor
        Add-Result "28" "Access rules unchanged (doctor: 200 assigned, 404 not assigned)" ($a.Status -eq 200 -and $c.Status -eq 404) `
            "assigned: HTTP $($a.Status), not assigned: HTTP $($c.Status)"
    }
    else {
        Add-Result "28" "Access rules unchanged" $false "could not log in as demo.doctor (use -DemoPassword)" -Warn
    }

    # ---------- Phase 3: user account update
    $r = Invoke-Api GET "/api/Users" -Token $admin
    $users = @($r.Json.data)
    $u2InList = $users | Where-Object { $_.id -eq $testUser2 } | Select-Object -First 1
    Add-Result "29" "Users list returns raw fullName / email / phoneNumber" `
        ($r.Status -eq 200 -and (Test-Prop $users[0] "fullName") -and (Test-Prop $users[0] "phoneNumber") -and $null -ne $u2InList -and
         $null -eq $u2InList.fullName -and $null -eq $u2InList.phoneNumber) (Get-Short $r)

    $uPath = "/api/Users/$testUser1"
    $r = Invoke-Api PUT $uPath "{}" $admin
    Add-Result "30" "PUT user {} is rejected" `
        ($r.Status -eq 400 -and (Test-ErrorField $r "fullName") -and (Test-ErrorField $r "email") -and (Test-ErrorField $r "phoneNumber")) (Get-Short $r)

    $newEmail = "$u1Name.new@raqeeb.demo"
    $r = Invoke-Api PUT $uPath ([ordered]@{ fullName = "Test Patient Edited"; email = $newEmail; phoneNumber = "01022222222" }) $admin
    Add-Result "31" "Valid user update" `
        ($r.Status -eq 200 -and $r.Json.data.fullName -ceq "Test Patient Edited" -and $r.Json.data.email -eq $newEmail -and
         $r.Json.data.phoneNumber -eq "01022222222" -and $r.Json.data.userName -eq $u1Name) (Get-Short $r)

    $adminEmail = ($users | Where-Object { $_.userName -eq $AdminUserName } | Select-Object -First 1).email
    $r = Invoke-Api PUT $uPath ([ordered]@{ fullName = "Test Patient Edited"; email = $adminEmail; phoneNumber = "01022222222" }) $admin
    Add-Result "32" "Email already used" ($r.Status -eq 409 -and $r.Json.errorCode -eq "EMAIL_TAKEN") (Get-Short $r)

    $r = Invoke-Api PUT $uPath ([ordered]@{ fullName = "Test Patient Edited"; email = "not-an-email"; phoneNumber = "01022222222" }) $admin
    Add-Result "33" "Invalid email" ($r.Status -eq 400 -and (Test-ErrorField $r "email")) (Get-Short $r)

    $r = Invoke-Api PUT $uPath ([ordered]@{ fullName = $null; email = $newEmail; phoneNumber = "01022222222" }) $admin
    Add-Result "34" "Clear the full name" ($r.Status -eq 200 -and $null -eq $r.Json.data.fullName) (Get-Short $r)

    $r = Invoke-Api POST "/api/Auth/login" ([ordered]@{ userName = $u1Name; password = $TestPassword })
    Add-Result "35" "Login still works after email change" ($r.Status -eq 200 -and [bool]$r.Json.data.token) (Get-Short $r)

    $r = Invoke-Api PUT "/api/Users/does-not-exist" ([ordered]@{ fullName = "X"; email = "nobody.$RunId@raqeeb.demo"; phoneNumber = $null }) $admin
    Add-Result "36" "Unknown user" ($r.Status -eq 404 -and $r.Json.errorCode -eq "USER_NOT_FOUND") (Get-Short $r)

    if ($doctor) {
        $r = Invoke-Api PUT $uPath ([ordered]@{ fullName = "X"; email = $newEmail; phoneNumber = $null }) $doctor
        Add-Result "37" "Only Admin can edit users" ($r.Status -eq 403) (Get-Short $r)
    }
    else {
        Add-Result "37" "Only Admin can edit users" $false "no doctor token (see test 28)" -Warn
    }

    # ---------- Regression
    $r = Invoke-Api GET "/api/PatientAssignments/Assignments" -Token $admin
    Add-Result "38" "Assignments list still works" ($r.Status -eq 200) (Get-Short $r)

    $r = Invoke-Api GET "$pPath/medical-history" -Token $admin
    Add-Result "39" "Medical history unchanged" ($r.Status -eq 200) (Get-Short $r)
}
catch {
    Add-Result "!!" "Run stopped" $false $_.Exception.Message
}
finally {
    # ---------- Clean-up (always runs)
    if ($admin -and $null -ne $original) {
        $r = Invoke-Api PUT $pPath (New-PatientBody $original) $admin
        Add-Result "15" "Restore the demo patient" ($r.Status -eq 200 -and (Test-SameProfile $r.Json.data $original)) (Get-Short $r)
    }
    if ($admin -and $testUser1) {
        $r = Invoke-Api PATCH "/api/Users/$testUser1/deactivate" -Token $admin
        Add-Result "40" "Turn off the test accounts" ($r.Status -eq 204 -or $r.Status -eq 409) (Get-Short $r)
    }
    $adminPassword = $null
}

# ---------------------------------------------------------------- summary

$sorted = $results | Sort-Object { $n = 0; if ([int]::TryParse(($_.Test -replace "[^0-9]", ""), [ref]$n)) { $n } else { 999 } }, Test
$pass = @($results | Where-Object Result -eq "PASS").Count
$fail = @($results | Where-Object Result -eq "FAIL").Count
$warn = @($results | Where-Object Result -eq "WARN").Count

$lines = @()
$lines += "RAQEEB API test run $RunId ($BaseUrl): $pass passed, $fail failed, $warn warnings"
foreach ($x in $sorted) {
    $line = "[$($x.Result)] $($x.Test) $($x.Name)"
    if ($x.Result -ne "PASS" -and $x.Detail) { $line += "  ->  $($x.Detail)" }
    $lines += $line
}
$summary = $lines -join [Environment]::NewLine

Write-Host ""
if ($fail -eq 0) { $c = "Green" } else { $c = "Red" }
Write-Host ("Done: {0} passed, {1} failed, {2} warnings" -f $pass, $fail, $warn) -ForegroundColor $c
try {
    Set-Clipboard -Value $summary
    Write-Host "The full summary is copied to your clipboard - paste it to Claude." -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host $summary
}
