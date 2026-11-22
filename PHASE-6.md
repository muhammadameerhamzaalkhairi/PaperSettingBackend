# Phase 6 — Manual CLO review aur exact reuse

HOD workspace se is exam ki question revision ka mapping_id leta hai. POST /api/clo-mappings/{mappingId}/reviews par status aur explanation bhejein. Valid statuses ALIGNED, NOT_ALIGNED aur NEEDS_REVIEW hain. GET /api/paper-versions/{versionId} mein cloMappings se decision parhein. check_status null ka matlab hai ke is mapping par abhi faisla baqi hai.

Misal

```json
{"status":"ALIGNED","explanation":"Sawal mein stack operations samjhana isi exam CLO ka maqsad hai."}
```

POST /api/paper-versions/{versionId}/similarity/exact se run[0].similarity_run_id lein. GET /api/similarity-runs/{runId} mein sources har approved historical paper ka alag result deti hai. Har source ke liye us se match hone wale distinct current MAIN sawalon ki tadaad ko kul current MAIN sawalon se divide karke 100 se multiply karte hain. matches mein sawal ki tafseel hai. Draft badalne par purana report 409 deta hai.

GET /api/checking/semantic ka status DEFERRED hai. Phase 6 local SQL tak mehdood hai. Koi external provider ya exam-content transfer configure nahi hua.

## Verification

Workspace root se disposable LocalDB par build aur HTTP integration suite chalayein.

```powershell
./PaperSettingBackend/Phase1.Tests/Run-Local.ps1 -SqlPackage './SSMS Queries/BIIT_SQL_Package'
```

Fixture mein chaar MAIN sawal aur aik matching EXTRA hai. Spring Final ke saath 3/4 (75%) aur Spring Mid ke saath 1/4 (25%) expected hain. CheckingFlow HOD aur teacher ki permissions, revised question ki fresh mapping aur submission guard bhi check karta hai.

Final run mein API build ke 0 warnings aur 0 errors thay. Phase 1–6 ke combined integration test mein 1 pass, 0 fail aur 0 skip thay. Dono percentages aur revision ke baad submission block verify huay.
