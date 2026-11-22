# Phase 5 — Versioned paper workspace

Setter `POST /api/papers/{paperId}/versions` par `{"reason":"First draft"}` bhejta hai. Response ke `version[0]` se `paper_version_id` aur Base64 `row_version` lein. Workspace parhne ke liye `GET /api/paper-versions/{versionId}` use karein.

## Question save karne ki misal

`POST /api/paper-versions/{versionId}/questions` ke liye example body neeche hai. Real request mein latest token, locked topic ID aur isi exam ki eligible CLO ID dein.

```json
{
  "rowVersion": "<latest Base64 token>",
  "prompt": "Explain how a stack works.",
  "difficulty": "MEDIUM",
  "placement": "EXTRA",
  "parts": [
    {"key":1,"kind":"GROUP","label":"Q","body":"Answer both parts."},
    {"key":2,"parentKey":1,"kind":"ITEM","label":"a","body":"Define push.","topicId":7,"marks":3,"difficulty":"EASY"},
    {"key":3,"parentKey":1,"kind":"ITEM","label":"b","body":"Trace two operations.","topicId":7,"marks":7,"difficulty":"MEDIUM"}
  ],
  "mappings": [{"examCloId":2},{"partKey":3,"examCloId":2}]
}
```

1. Edit ke liye `PUT /api/paper-versions/{versionId}/items/{itemId}/question` par poori updated question bhejein. SQL nayi question revision banati hai. Contributor apna EXTRA item edit kar sakta hai. Setter placement badalne ke liye alag `PUT .../items/{itemId}/placement` call karta hai.
2. Setter `PUT .../order` par rowVersion, placement aur us tab ki tamam itemIds required order mein bhejta hai. `PUT .../instructions` paper instructions save karta hai. Har successful edit ke baad response ka naya token use karein. HTTP 409 par workspace reload karein.
3. `GET /api/papers/{paperId}/versions` se paper history aur `GET /api/question-versions/{questionVersionId}/history` se question revisions, parts aur CLO mappings milti hain.
4. Setter multipart `POST /api/paper-versions/{versionId}/attachments` mein file, rowVersion, questionVersionId aur optional partId/displayOrder bhejta hai. PNG/JPEG diagrams ki limit 5 MiB hai. API extension aur file signature check karke SHA-256 checksum record karti hai. GET isi route se metadata milta hai. GET ya DELETE ke liye attachment ID append karein. DELETE body mein current rowVersion dein.
5. Diagram bytes ke liye private folder `Files__PrivateRoot` se configure kar sakte hain. Windows default local application data ke andar BIITPaperSetting/PrivateDiagrams hai. Database aur is folder ka backup saath lein. Frozen question dobara use ho rahi ho to attachment badalne se pehle question ki nayi revision save karein.

## Verification

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -d BIIT_FYP_PaperSetting -v TargetDatabase=BIIT_FYP_PaperSetting -i Sql/005_AttachDraftFile.sql
./Phase1.Tests/Run-Local.ps1 -SqlPackage '../SSMS Queries/BIIT_SQL_Package'
```

Tests real HTTP requests, restricted SQL login aur temporary private folder use karte hain. Correction test disposable database mein pehli revision freeze karta hai, phir API se doosri revision bana kar dono ka content aur historical diagram check karta hai.

Final build 0 warnings aur 0 errors ke saath pass hua. Combined Phase 1–5 integration test 27 seconds mein pass hua (1 passed, 0 failed, 0 skipped).
