# Phase 4 — Section coverage aur setter assignment

1. Signed-in user `GET /api/exams/{examId}/coverage` se sections, topic matrix, common topics, eligible CLOs, lock time, locked topics aur setter history dekh sakta hai. Existing SQL procedure exam access check karti hai.
2. Teacher `PUT /api/exams/{examId}/coverage/assignments/{assignmentId}` par us section ki poori topic selection bhejta hai. Assignment ID sections list mein milti hai. Draft ke liye `submit: false` aur submission ke liye `submit: true` dein.

   ```json
   {"topics":[{"topicId":7,"status":"COVERED"},{"topicId":9,"status":"IN_PROGRESS"}],"submit":true}
   ```

   Apne exam ke topic IDs use karein. `NOT_STARTED` bhi valid hai. Missing submission mein `coverage_submission_id` null hoti hai. `{"topics":[],"submit":true}` bhejne par submission ID aur `SUBMITTED` status record hote hain.

3. HOD `POST /api/exams/{examId}/coverage/lock` se common topics lock karta hai. Har required assignment ki submission, har section ke liye teacher, configured exam CLOs aur kam az kam ek shared covered topic zaroori hain.
4. Relevant HOD `POST /api/exams/{examId}/setter-assignments` ko eligible teacher ka `teacherId` aur future `dueAt` (ISO timestamp with offset) deta hai. Reassignment isi route se hoti hai. Response mein `paper[0].paper_id` milta hai. History mein current assignment ki `ended_at` null hoti hai.

## Verification

Existing SQL package par sirf nayi read procedure install karne aur tests reproduce karne ke liye repo root se chalayein.

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -d BIIT_FYP_PaperSetting -v TargetDatabase=BIIT_FYP_PaperSetting -i Sql/004_CoverageWorkspace.sql
./Phase1.Tests/Run-Local.ps1 -SqlPackage '../SSMS Queries/BIIT_SQL_Package'
```

Runner disposable LocalDB mein teen sections ki coverage submit karta hai. Expected results mein missing, empty aur disjoint coverage par 409, doosre teacher ki assignment par 403, aur lock ke baad ek shared topic shamil hain. HOD ke setter badalne par wahi paper ID, do assignment history rows aur teen participants milte hain.

Final run mein build 0 warnings aur 0 errors ke saath pass hua. Phase 1–4 ka combined integration test 22 seconds mein pass hua (1 passed, 0 failed, 0 skipped).
