# Phase 3 — Excel datesheet aur current exam

Data Cell pehle exam cycle select kare. `GET /api/datesheets/cycles/{cycleId}/courses` us cycle ke course exam IDs, course codes aur titles deta hai.

## Excel format

Ek worksheet mein pehli row ke chaar headers isi order mein rakhein.

```text
course_exam_id    exam_date     start_time    end_time
17                2028-04-10    09:00         11:00
```

17 ki jagah selected cycle ka asli course exam ID dein. Date Excel date ya ISO date ho sakti hai. Clock time Excel time ya 24-hour text ho sakta hai. Formulas ki jagah values dein. Upload limit 2 MiB aur 5,000 data rows hai.

## Preview aur confirmation

1. `POST /api/datesheets/cycles/{cycleId}/drafts` par multipart field `file` mein workbook bhejein. Malformed file par 400 milta hai. Invalid rows par 422 response mein Excel row, field aur error milte hain. Valid rows ka draft save hota hai. Response mein `draft[0].datesheet_id` aur revision number milte hain.
2. `GET /api/datesheets/{id}/preview` se header, schedule aur missing courses dekhein. Missing course ho to corrected workbook dobara upload karein. Har successful upload apni draft revision banata hai.
3. Confirmation ke baad `POST /api/datesheets/{id}/publish` chalayein. Procedure complete draft publish karke current exam, datesheet aur audit ek transaction mein update karti hai. Response ke `current` mein published context milta hai. Repeat publish par 409 aata hai.
4. `GET /api/exams/current` se authorized staff naya context parh sakta hai. Data Cell `GET /api/datesheets/history` se revisions aur current marker dekh sakta hai.

## Setup aur test

Existing SQL package wali LocalDB ke liye repo root se versioned lookup install karein.

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -d BIIT_FYP_PaperSetting -v TargetDatabase=BIIT_FYP_PaperSetting -i Sql/003_DatesheetCourses.sql
./Phase1.Tests/Run-Local.ps1 -SqlPackage '../SSMS Queries/BIIT_SQL_Package'
```

Runner disposable database aur restricted SQL login ke saath real Excel workbooks HTTP par bhejta hai. Is mein upload errors, incomplete preview, denied publication, previous revision ka supersede hona aur Phase 1 current-exam endpoint check hote hain.

Final check mein build 0 warnings aur 0 errors ke saath pass hua. Combined integration test 20 seconds mein pass hua (1 passed, 0 failed, 0 skipped).
