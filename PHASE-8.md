# Phase 8 — Approved PDF aur sealed receipt

## PDF export

Authenticated staff jis ko exam ka access hai, woh `POST /api/approvals/{approvalId}/print-exports` se PDF banata hai. Approval ID Director ke final review ki ID hai. SQL isi review se frozen revision nikalti hai. PDF mein BIIT, exam/session, course, duration, marks, instructions, ordered MAIN questions, parts aur un ke diagrams aate hain. Har call aik naya print event record karti hai.

Response ki file type `application/pdf` hai. Header `X-Print-Export-Id` ki ID se `GET /api/approvals/{approvalId}/print-exports/{exportId}` wohi saved PDF download karta hai. Is GET se naya export event nahi banta. SQL mein approval, user, waqt, private file key aur SHA-256 checksum record hote hain. PDF bytes aur diagrams configured `Files__PrivateRoot` mein rehte hain. Database aur file store ka backup saath rakhein.

## Sealed packet

Jab packet waqai mil jaye, Data Cell apne token ke saath `POST /api/approvals/{approvalId}/sealed-receipt` karta hai. Misal mein deliveredByUserId ki jagah packet dene wale staff ki ID dein.

```json
{"deliveredByUserId":5,"remarks":"Sealed packet received at the examination office."}
```

Response mein receipt ID, receiving aur delivering staff, timestamp, seal status aur remarks milte hain. SQL paper ko RECEIVED karti hai. Duplicate receipt par 409 milta hai aur pehli entry mehfooz rehti hai.

## Verification

```powershell
./PaperSettingBackend/Phase1.Tests/Run-Local.ps1 -SqlPackage './SSMS Queries/BIIT_SQL_Package'
```

Test runner disposable LocalDB par Phase 8 script install karta hai. PrintFlow approved Acting Director revision ka actual PDF parse karta hai. Is mein MAIN order, EXTRA exclusion, diagram, instructions, saved download, permission denials aur do simultaneous receipt requests check hoti hain. PDF renderer PDFsharp/MigraDoc hai. PdfPig sirf test project mein PDF parhta hai.

Project database ke liye versioned script `Sql/008_ApprovedPrint.sql` hai. PDF renderer Windows par installed Arial font use karta hai.

Final run mein API build ke 0 warnings aur 0 errors thay. Phase 1–8 ka combined HTTP/SQL test 28 seconds mein pass hua (1 pass, 0 fail, 0 skip). Synthetic A4 PDF ko render karke chaar MAIN sawalon ka order, diagram ki jagah aur page number bhi dekhe gaye. Phase 8 migration disposable database par test hui.
