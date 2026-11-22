# Phase 7 — Submission aur review

Assigned setter latest rowVersion ke saath POST /api/paper-versions/{versionId}/submit karta hai. SQL marks, locked topics aur MAIN sawalon ki CLO alignment check karti hai. Submission ke baad paper freeze hota hai. HOD ka whole-paper review optional hai. Director isi submission par seedha faisla kar sakta hai.

## Request Changes aur correction

GET /api/paper-review/actions se button ka label `Request Changes` aur value `CHANGES_REQUESTED` milti hai. Review screen ke liye submissionId aur versionId dono GET /api/papers/{paperId}/versions se lein. Misal ke taur par Director yeh request bhejta hai.

```http
POST /api/submissions/{submissionId}/versions/{versionId}/decisions
Content-Type: application/json
Authorization: Bearer <access-token>

{"stage":"DIRECTOR","decision":"CHANGES_REQUESTED","reason":"Question 2 mein input size wazeh karein."}
```

Setter POST /api/papers/{paperId}/versions par reason de kar latest frozen revision ki copy banata hai, correction karta hai aur nayi version submit karta hai. Review history mein pehli submission aur us ka reason maujood rehte hain.

## Director ki question edit

POST /api/submissions/{submissionId}/versions/{versionId}/items/{itemId}/revisions par workspace question edit ki poori body, current rowVersion aur reason bhejein. Acting Director apni delegationId bhi deta hai. SQL aik transaction mein paper aur edited question ki nayi revisions banati hai. History mein editor, wajah aur parent revision milte hain. Audit event mein purane aur naye paper aur question version IDs record hote hain.

Nayi revision ka versionId response ke saved[0].paper_version_id se milta hai. Edited MAIN sawal ki CLO mapping par HOD fresh alignment record karta hai. Director isi exact revision par APPROVED decision bhejta hai. Purani revision par 409 aur revoked delegation par 403 milta hai.

## Local verification

Workspace root se synthetic fixtures aur restricted API principal ke saath integration suite chalayein.

```powershell
./PaperSettingBackend/Phase1.Tests/Run-Local.ps1 -SqlPackage './SSMS Queries/BIIT_SQL_Package'
```

Project database ke liye migration pehle existing submissions ko un ke frozen versions se link karti hai. Phir review ki foreign key updated link par lagati hai. Dobara chalane par existing links preserve hote hain.

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -d BIIT_FYP_PaperSetting -v TargetDatabase=BIIT_FYP_PaperSetting -i PaperSettingBackend/Sql/007_ReviewWorkflow.sql
```

Final verification mein build ke 0 warnings aur 0 errors thay. Combined Phase 1–7 integration test 31 seconds mein pass hua. Direct aur optional HOD review, reason ke saath correction aur resubmission, frozen content, fresh CLO check, exact-version approval aur revoked delegation ka 403 verify huay. Migration disposable LocalDB par verify hui. Project LocalDB par apply karna baqi hai.
