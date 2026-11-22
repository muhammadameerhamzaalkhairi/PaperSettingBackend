# Phase 2 — Staff authority aur academic setup

Data Cell departments, programs, sessions, sections, courses, exam cycles, staff accounts, offerings aur teaching assignments bana sakta hai. Director staff roles grant/end karta hai aur delegation ka waqt set karta hai. HOD apne department ke draft outlines, topics, CLOs aur course exam settings manage karta hai. OpenAPI mein routes `/api/academic`, `/api/staff` aur `/api/curriculum` ke neeche milenge.

Misal ke taur par Data Cell token ke saath `POST /api/academic/departments` ko `{"code":"BIO","name":"Biology"}` bhejein. Kamyab response mein nayi department ki `id` milti hai. Isi token se `POST /api/staff/roles` par valid role grant bhejne ka expected result `403` hai.

## Verification

Repository root se PowerShell mein existing integration runner chalayein.

```powershell
./Phase1.Tests/Run-Local.ps1 -SqlPackage '../SSMS Queries/BIIT_SQL_Package'
```

Runner disposable LocalDB par API build aur dotnet test chalata hai. Ek end-to-end test pehle staff login karta hai, phir Phase 2 ke HTTP requests restricted SQL connection se chalata hai. Test alag department ke HOD ki request, multi-role staff ki teaching assignment aur curriculum access, aur published outline ki editing check karta hai. Expired delegation ke saath asal approval procedure ko restricted connection se call karta hai aur expiry wali SQL rejection verify karta hai. Phase 1 ke current exam aur token checks bhi isi run ka hissa hain.

Final run mein build ke 0 warnings aur 0 errors aaye. Integration test 18 seconds mein pass hua (1 passed, 0 failed, 0 skipped).
