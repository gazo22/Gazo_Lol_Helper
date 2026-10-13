Ce dossier reçoit, au moment du build, les programmes d'installation Microsoft (non versionnés) :

- `MicrosoftEdgeWebview2Setup.exe` — https://go.microsoft.com/fwlink/p/?LinkId=2124703
- `SQLEXPR_x64_ENU.exe` — média complet SQL Server 2022 Express, obtenu avec le bootstrapper
  https://go.microsoft.com/fwlink/p/?linkid=2216019 :
  `SQL2022-SSEI-Expr.exe /ACTION=Download /MEDIAPATH=<dossier> /MEDIATYPE=Core /LANGUAGE=en-US /QUIET`

Le workflow `.github/workflows/release.yml` fait tout cela automatiquement.
