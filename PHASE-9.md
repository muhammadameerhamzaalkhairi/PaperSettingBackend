# Phase 9 — Backend verification

## Clean checkout aur test

Windows par .NET 10 SDK, PowerShell 7, SQL Server LocalDB, sqlcmd (ODBC) aur Arial font chahiye. Pehli NuGet restore ke liye internet ya populated package cache chahiye.

```powershell
git clone --branch biit-development https://github.com/muhammadameerhamzaalkhairi/PaperSettingBackend.git
Set-Location PaperSettingBackend
dotnet build ./BIITPaperSettingAPI/BIITPaperSettingAPI
./Phase1.Tests/Run-Local.ps1
```

Runner har run ke liye unique LocalDB instance banata hai. Sql/Baseline mein package ka schema, installation SQL aur synthetic exam fixture hain. Runner sirf apne disposable database ke naam substitute karta hai, phir 003, 004, 005, 007 aur 008 scripts lagata hai. Baseline files existing project database par chalana zaroori nahi. API process ko restricted SQL principal milta hai. Test operator fixture banata hai aur outcome verify karta hai. Runner instance ko akhir mein remove karta hai.

## Existing configured project par API chalana

Project database BIIT_FYP_PaperSetting par schema aur Phase 8 tak ki migrations honi chahiye. Phase 9 ke routes existing stored procedures use karte hain. ConnectionStrings__DefaultConnection ko API executor aur ConnectionStrings__Authentication ko auth executor ki connection strings dein. Files__PrivateRoot ko persistent private diagrams/PDF folder par set karein. SQL aur file store ka backup saath rakhein. Jwt__SigningKey ASP.NET secret store ya environment mein rakhein. Local development mein neeche wali command is PowerShell session ki key banati hai.

```powershell
$api = './BIITPaperSettingAPI/BIITPaperSettingAPI'
$env:Jwt__SigningKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project $api --launch-profile https
```

Login ke baad accessToken ko Bearer header mein dein. Demo credential setup Phase 1 record mein hai. Actor ID signed token se li jati hai.

## Screen aur API mapping

Handover ke Figma file mein page 0:1 ke frame names read kiye gaye. Mapping backend behavior ki hai. [Project screens](https://www.figma.com/design/HdhLxXQQTCzhV7al4ayoRb/new-hamza-file?node-id=0-1)

| Screen / operation | API |
|---|---|
| Director login, Director Home, HOD/Data Cell/teacher dashboard | POST /api/auth/login, GET /api/exams/current, GET /api/dashboard |
| Current papers, past papers, approved papers list | GET /api/papers with examCycleId, status, search, page, pageSize. List mein setter, due date aur latest revision milti hai. |
| Courses, course details, covered topics, academic setup | GET /api/courses, exam coverage workspace aur academic/curriculum routes (Phases 2, 4) |
| Datesheet upload/preview/publication, import history | Phase 3 datesheet routes, GET /api/datesheets/history, GET /api/imports/history with optional importId for logged batches/errors |
| Paper workspace, MAIN/EXTRA tabs, editor, diagrams | Phase 5 versioned workspace, question edit, order, placement aur attachment routes |
| CLO review list aur similarity | Phase 6 manual HOD mapping reviews aur per-source exact report |
| Question comments/replies, paper decisions aur handover | POST /api/paper-versions/{versionId}/comments, GET workspace for comments, GET /api/papers/{paperId}/versions, Phase 7 decisions aur Phase 2 delegation routes |
| Paper preview, print aur sealed packet | Approved revision se Phase 8 private PDF, saved export aur Data Cell receipt. POST /api/papers/{paperId}/archive sirf RECEIVED paper par. |
| Profile aur edit profile | GET/PUT /api/me/profile. GET profile aur active roles deta hai. PUT fullName, phone aur optional owned photoFileId leta hai. |
| Notifications | GET /api/me/notifications?unreadOnly=true, PUT /api/me/notifications/{id}/read |
| Teacher wishlist | GET/POST /api/me/wishlist, PUT /api/me/wishlist/{id}, PUT /api/me/wishlist/{id}/state/{DONE,UNDONE,TRASH,RESTORE}, GET /api/me/wishlist?trash=true |

Comment body ki misal `{"text":"Part b ka input wazeh karein","paperItemId":7}` hai. Reply ke liye isi version ka parentCommentId dein. Item aur parent IDs workspace se lein. Kisi dusre exam ki access ke liye sirf route ID badalna kaafi nahi.

## Verification aur hudood

Ek hi synthetic course exam ke liye Data Cell datesheet publish karti hai. Teachers coverage complete karte hain, HOD common topics lock aur setter assign karta hai. Workspace mein MAIN/EXTRA, question revision aur attachment changes hote hain. HOD CLO alignment, exact similarity, setter submission, Director ki changes request, corrected submission, final approval, PDF aur sealed receipt real HTTP aur stored procedures se chalte hain. Archive ke baad paper history accessible rehti hai. Purane phases ke stale-token, manual-review, Acting Director revocation, PDF contents aur duplicate-receipt tests bhi runner ka hissa hain.

Semantic checking deferred hai. Academic setup APIs aur datesheet workbook import available hain. General academic multi-sheet import ka final workbook contract aur generic question trash/undo policy khuli hui hain. Public signup, email/push delivery aur reminder scheduler implemented scope mein shamil nahi. Flutter aur frontend delivery alag hai.

Final clean-copy run mein build ke 0 warnings aur 0 errors thay. Combined integration test 1 minute 2 seconds mein pass hua (1 pass, 0 fail, 0 skip). Asal project database par read-only metadata check mein Phase 9 ke 13 procedures aur un ke API EXECUTE grants mile. Workflow writes disposable database mein huin.
