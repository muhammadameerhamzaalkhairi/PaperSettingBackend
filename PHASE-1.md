# Phase 1 — Staff login aur current exam

PowerShell 7 aur .NET 10 ke saath repository root se chalayein. SQL Server LocalDB, sqlcmd aur project ka BIIT_SQL_Package bhi chahiye.

```powershell
$api = './BIITPaperSettingAPI/BIITPaperSettingAPI'
$env:Jwt__SigningKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet build $api
dotnet run --project $api --launch-profile https
```

Key API process ke paas rakhein. Key badalne par purane tokens invalid ho jate hain. Default issuer BIIT.PaperSetting aur audience BIIT.Staff hai. Login procedure ke liye ConnectionStrings__Authentication aur exam procedure ke liye ConnectionStrings__DefaultConnection configure kar sakte hain. Agar pehli setting missing ho to dono calls DefaultConnection use karti hain.

LocalDB operator apne synthetic demo account ka placeholder ek baar set kar sakta hai. Pehle DefaultConnection ka target verify karein. Yeh command interactive PowerShell mein password chupata hai.

```powershell
Read-Host -MaskInput 'Demo password' | dotnet run --no-build --project $api -- --provision-local-demo director@example.invalid
```

POST /api/auth/login ko email aur password JSON mein dein. Kamyab response ka accessToken agle GET /api/exams/current ki Authorization header mein Bearer token ke taur par dein. ActorId signed token se milti hai. Token 30 minute baad expire hota hai.

## Integration check

```powershell
./Phase1.Tests/Run-Local.ps1 -SqlPackage '../SSMS Queries/BIIT_SQL_Package'
```

Script nayi disposable instance banata hai, supplied schema aur synthetic demo install karta hai, API build karta hai aur dotnet test chalata hai. API restricted SQL login use karti hai. Test operator fixture badalta hai. Run ke baad script test database aur instance remove karta hai.

22 November 2026 ko build mein 0 warnings aur 0 errors the. Ek end-to-end test pass hua. Is mein placeholder aur ghalat password, missing token, malformed token, ghalat signature, issuer, audience aur expired token par 401 mila. Valid login ke baad Final aur PUBLISHED mile. Isi token se doosra current exam aur phir empty context bhi mila. Staff disable hone aur role expire hone par 403 mila.
